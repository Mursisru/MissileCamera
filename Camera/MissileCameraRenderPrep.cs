using System;
using System.Reflection;
using HarmonyLib;
using NuclearOption.Effects;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MissileCamera
{
    /// <summary>
    /// Nuclear Option terrain/detail shaders read per-camera globals from <see cref="ShaderGlobalManager"/>.
    /// Trees/grass (DetailRenderer) cull against Camera.main — sync to feed before each seeker render.
    /// Mirrors URP settings from vanilla TargetCam / main — no manual AA/MSAA overrides.
    /// </summary>
    internal static class MissileCameraRenderPrep
    {
        private static readonly int WindowDataId = Shader.PropertyToID("_WindowData");
        private static readonly int HeightMapId = Shader.PropertyToID("_HeightMap");
        private static readonly int BlockerMapId = Shader.PropertyToID("_BlockerMap");

        private static readonly FieldInfo? DetailCameraField =
            AccessTools.Field(typeof(DetailRenderer), "camera");
        private static readonly MethodInfo? DetailLateUpdateMethod =
            AccessTools.Method(typeof(DetailRenderer), "LateUpdate");
        private static readonly FieldInfo? RendererIndexField =
            AccessTools.Field(typeof(UniversalAdditionalCameraData), "m_RendererIndex");

        private static Vector2Int _lastBakedWindow = new(int.MinValue, int.MinValue);
        private static CommandBuffer? _terrainWindowCmd;
        private static bool _pipelineHooksRegistered;
        private static Camera? _pipelineFeedCamera;
        private static bool _pipelineForceLdr;
        private static bool _pipelineInfrared;
        private static bool _pipelineNightVision;
        private static bool _pipelineFogPrev;
        private static bool _pipelineFogActive;
        private static int _cachedWindowSize = -1;
        private static int _cachedWindowSnapping = -1;
        private static float _nextWindowCacheTime;
        private const float WindowCacheInterval = 1f;
        private static Camera? _lastMirrorReference;
        private static int _lastMirrorCulling = int.MinValue;
        private static bool _lastMirrorAllowHdr;
        private static bool _lastMirrorAllowMsaa;
        private static CameraClearFlags _lastMirrorClearFlags;
        private static bool _lastMirrorForceLdr;
        private static bool _lastMirrorInfrared;
        private static bool _lastMirrorNightVision;
        private static int _lastMirrorRendererIndex = int.MinValue;
        private static bool _lastMirrorRenderShadows;
        private static AntialiasingMode _lastMirrorAa;
        private static AntialiasingQuality _lastMirrorAaQuality;
        private static bool _lastMirrorDithering;
        private static bool _lastMirrorStopNaN;

        internal static void BeforeRender(Camera feedCamera, bool forceLdr = false)
        {
            ApplyShaderGlobalsForCamera(feedCamera);
            MirrorUrpFromReference(feedCamera, forceLdr);
            BakeTerrainWindowForCamera(feedCamera);
            SyncDetailsToCamera(feedCamera);
        }

        internal static void AfterRender()
        {
            Camera? main = Camera.main;
            if (main == null)
                return;

            ApplyShaderGlobalsForCamera(main);
            BakeTerrainWindowForCamera(main);
        }

        /// <summary>
        /// Fullscreen: let URP render the feed camera itself (like TargetCam). Prep on begin/endCameraRendering.
        /// </summary>
        internal static void SetPipelineDriven(Camera? feedCamera, bool active, bool forceLdr = false, bool infrared = false)
        {
            if (!active || feedCamera == null)
            {
                UnregisterPipelineHooks();
                _pipelineFeedCamera = null;
                _pipelineInfrared = false;
                _pipelineNightVision = false;
                return;
            }

            _pipelineFeedCamera = feedCamera;
            _pipelineForceLdr = forceLdr;
            _pipelineInfrared = infrared;
            RegisterPipelineHooks();
            MirrorUrpFromReference(feedCamera, forceLdr);
        }

        internal static void SetPipelineInfrared(bool infrared) => _pipelineInfrared = infrared;

        internal static void SetPipelineNightVision(bool nightVision) => _pipelineNightVision = nightVision;

        internal static int ResolvePipelineMsaaSampleCount()
        {
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset asset)
            {
                int samples = asset.msaaSampleCount;
                if (samples >= 8)
                    return 8;
                if (samples >= 4)
                    return 4;
                if (samples >= 2)
                    return 2;
            }

            return 1;
        }

        internal static void ForceRestoreWorldState()
        {
            if (_pipelineFogActive)
            {
                RenderSettings.fog = _pipelineFogPrev;
                _pipelineFogActive = false;
            }

            _pipelineFeedCamera = null;
        }

        private static void RegisterPipelineHooks()
        {
            if (_pipelineHooksRegistered)
                return;

            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
            _pipelineHooksRegistered = true;
        }

        private static void UnregisterPipelineHooks()
        {
            if (!_pipelineHooksRegistered)
                return;

            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            _pipelineHooksRegistered = false;
        }

        private static void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (_pipelineFeedCamera == null || camera != _pipelineFeedCamera)
                return;

            _pipelineFogPrev = RenderSettings.fog;
            _pipelineFogActive = true;
            RenderSettings.fog = !_pipelineInfrared;
            ApplyShaderGlobalsForCamera(camera);
            MirrorUrpFromReference(camera, _pipelineForceLdr);
            BakeTerrainWindowForCamera(camera);
            SyncDetailsToCamera(camera);
        }

        private static void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (_pipelineFeedCamera == null || camera != _pipelineFeedCamera)
                return;

            if (_pipelineFogActive)
            {
                RenderSettings.fog = _pipelineFogPrev;
                _pipelineFogActive = false;
            }

            AfterRender();
        }

        /// <summary>
        /// Dump: DetailRenderer.LateUpdate culls trees/grass with its private camera (Camera.main).
        /// Point it at the seeker briefly so ComputeFrustumCulling + RenderMeshIndirect match the feed.
        /// </summary>
        private static void SyncDetailsToCamera(Camera feedCamera)
        {
            if (feedCamera == null || DetailCameraField == null || DetailLateUpdateMethod == null)
                return;

            DetailRenderer? detail = null;
            try
            {
                detail = SceneSingleton<DetailRenderer>.i;
            }
            catch
            {
                return;
            }

            if (detail == null || !detail.isActiveAndEnabled)
                return;

            object? previous = DetailCameraField.GetValue(detail);
            try
            {
                DetailCameraField.SetValue(detail, feedCamera);
                DetailLateUpdateMethod.Invoke(detail, null);
            }
            catch (Exception ex)
            {
                MfdLog.Info("detail sync error: " + ex.Message);
            }
            finally
            {
                try
                {
                    DetailCameraField.SetValue(detail, previous);
                }
                catch
                {
                    // ignore
                }
            }
        }

        private static void ApplyShaderGlobalsForCamera(Camera camera)
        {
            int maxTargetOffset = GetMaxTargetOffset();
            ShaderGlobalManager.SetCameraPlanes(camera, maxTargetOffset, out _);

            Vector2Int windowIndex = GetWindowIndex(camera.transform.position);
            Shader.SetGlobalVector(
                WindowDataId,
                new Vector4(windowIndex.x, windowIndex.y, GetWindowSnapping(), GetWindowSize()));
        }

        private static void BakeTerrainWindowForCamera(Camera camera)
        {
            TerrainHeightMap? terrainHeightMap = SceneSingleton<TerrainHeightMap>.i;
            if (terrainHeightMap == null || terrainHeightMap.heightMap == null)
                return;

            Vector2Int windowIndex = GetWindowIndex(camera.transform.position);
            if (windowIndex != _lastBakedWindow)
            {
                _lastBakedWindow = windowIndex;
                if (_terrainWindowCmd == null)
                    _terrainWindowCmd = new CommandBuffer { name = "MissileCamera.TerrainWindow" };
                else
                    _terrainWindowCmd.Clear();

                terrainHeightMap.BakeWindow(_terrainWindowCmd, windowIndex);
                Graphics.ExecuteCommandBuffer(_terrainWindowCmd);
            }

            Shader.SetGlobalTexture(HeightMapId, terrainHeightMap.heightMap);
            Shader.SetGlobalTexture(BlockerMapId, terrainHeightMap.blockerMap);
        }

        private static void MirrorUrpFromReference(Camera feedCamera, bool forceLdr)
        {
            Camera? reference = ResolveReferenceCamera() ?? Camera.main;
            if (reference == null)
                return;

            UniversalAdditionalCameraData feedUrp = feedCamera.GetUniversalAdditionalCameraData();
            UniversalAdditionalCameraData refUrp = reference.GetUniversalAdditionalCameraData();
            int rendererIndex = GetRendererIndex(refUrp);
            bool wantHdr = !forceLdr && reference.allowHDR;
            bool wantPp = _pipelineInfrared || _pipelineNightVision;
            int desiredCulling = reference.cullingMask
                | (int)PhysicsLayers.EffectsMask
                | (int)PhysicsLayers.TransparentFXMask;

            bool dirty = !ReferenceEquals(reference, _lastMirrorReference)
                || _lastMirrorCulling != desiredCulling
                || _lastMirrorAllowHdr != wantHdr
                || _lastMirrorAllowMsaa != reference.allowMSAA
                || _lastMirrorClearFlags != reference.clearFlags
                || _lastMirrorForceLdr != forceLdr
                || _lastMirrorInfrared != _pipelineInfrared
                || _lastMirrorNightVision != _pipelineNightVision
                || _lastMirrorRendererIndex != rendererIndex
                || _lastMirrorRenderShadows != refUrp.renderShadows
                || _lastMirrorAa != refUrp.antialiasing
                || _lastMirrorAaQuality != refUrp.antialiasingQuality
                || _lastMirrorDithering != refUrp.dithering
                || _lastMirrorStopNaN != refUrp.stopNaN
                || feedUrp.renderPostProcessing != wantPp;

            if (!dirty)
            {
                feedUrp.volumeTrigger = feedCamera.transform;
                return;
            }

            feedCamera.cullingMask = desiredCulling;
            feedCamera.allowHDR = wantHdr;
            feedCamera.allowMSAA = reference.allowMSAA;
            feedCamera.clearFlags = reference.clearFlags;

            feedUrp.SetRenderer(rendererIndex);
            feedUrp.renderShadows = refUrp.renderShadows;
            // IR blit path: no PP. NightVision: local feed Volume only (never toggle stock NVG).
            feedUrp.renderPostProcessing = wantPp;
            feedUrp.volumeTrigger = feedCamera.transform;
            feedUrp.antialiasing = refUrp.antialiasing;
            feedUrp.antialiasingQuality = refUrp.antialiasingQuality;
            feedUrp.dithering = refUrp.dithering;
            feedUrp.stopNaN = refUrp.stopNaN;
            feedUrp.requiresDepthOption = CameraOverrideOption.UsePipelineSettings;
            feedUrp.requiresColorOption = CameraOverrideOption.UsePipelineSettings;

            _lastMirrorReference = reference;
            _lastMirrorCulling = desiredCulling;
            _lastMirrorAllowHdr = wantHdr;
            _lastMirrorAllowMsaa = reference.allowMSAA;
            _lastMirrorClearFlags = reference.clearFlags;
            _lastMirrorForceLdr = forceLdr;
            _lastMirrorInfrared = _pipelineInfrared;
            _lastMirrorNightVision = _pipelineNightVision;
            _lastMirrorRendererIndex = rendererIndex;
            _lastMirrorRenderShadows = refUrp.renderShadows;
            _lastMirrorAa = refUrp.antialiasing;
            _lastMirrorAaQuality = refUrp.antialiasingQuality;
            _lastMirrorDithering = refUrp.dithering;
            _lastMirrorStopNaN = refUrp.stopNaN;
        }

        private static Camera? ResolveReferenceCamera()
        {
            if (!GameManager.GetLocalAircraft(out Aircraft aircraft))
                return null;

            TargetCam? targetCam = TargetCamAccess.GetTargetCam(aircraft);
            if (targetCam == null)
                return null;

            return TargetCamAccess.GetCam(targetCam);
        }

        private static int GetRendererIndex(UniversalAdditionalCameraData cameraData)
        {
            if (RendererIndexField?.GetValue(cameraData) is int index)
                return index;

            return 0;
        }

        private static int GetMaxTargetOffset()
        {
            EnsureWindowCache();
            return Mathf.Max(0, _cachedWindowSize / 2 - _cachedWindowSnapping * 2);
        }

        private static int GetWindowSize()
        {
            EnsureWindowCache();
            return _cachedWindowSize;
        }

        private static int GetWindowSnapping()
        {
            EnsureWindowCache();
            return _cachedWindowSnapping;
        }

        private static void EnsureWindowCache()
        {
            float now = Time.unscaledTime;
            if (_cachedWindowSize > 0 && now < _nextWindowCacheTime)
                return;

            _nextWindowCacheTime = now + WindowCacheInterval;
            DetailRenderer? detail = null;
            try
            {
                detail = SceneSingleton<DetailRenderer>.i;
            }
            catch
            {
                // keep previous / defaults
            }

            _cachedWindowSize = detail != null ? detail.windowSize : 1024;
            _cachedWindowSnapping = detail != null ? detail.windowSnapping : 64;
        }

        private static Vector2Int GetWindowIndex(Vector3 localPosition)
        {
            GlobalPosition global = localPosition.ToGlobalPosition();
            int snapping = GetWindowSnapping();
            return new Vector2Int(
                Mathf.FloorToInt(global.x / snapping),
                Mathf.FloorToInt(global.z / snapping));
        }
    }
}
