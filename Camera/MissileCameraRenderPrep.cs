using System.Globalization;
using System.Reflection;
using NuclearOption.Effects;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MissileCamera
{
    /// <summary>
    /// Nuclear Option terrain/detail shaders read per-camera globals from <see cref="ShaderGlobalManager"/>.
    /// Mirrors URP settings from vanilla TargetCam / main — no manual AA/MSAA overrides.
    /// </summary>
    internal static class MissileCameraRenderPrep
    {
        private static readonly int WindowDataId = Shader.PropertyToID("_WindowData");
        private static readonly int HeightMapId = Shader.PropertyToID("_HeightMap");
        private static readonly int BlockerMapId = Shader.PropertyToID("_BlockerMap");

        private static Vector2Int _lastBakedWindow = new(int.MinValue, int.MinValue);
        private static bool _pipelineHooksRegistered;
        private static Camera? _pipelineFeedCamera;
        private static bool _pipelineForceLdr;
        private static bool _pipelineInfrared;
        private static bool _pipelineFogPrev;
        private static bool _pipelineFogActive;

        internal static void BeforeRender(Camera feedCamera, bool forceLdr = false)
        {
            ApplyShaderGlobalsForCamera(feedCamera);
            MirrorUrpFromReference(feedCamera, forceLdr);
            BakeTerrainWindowForCamera(feedCamera);
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
                return;
            }

            _pipelineFeedCamera = feedCamera;
            _pipelineForceLdr = forceLdr;
            _pipelineInfrared = infrared;
            RegisterPipelineHooks();
            MirrorUrpFromReference(feedCamera, forceLdr);
        }

        internal static void SetPipelineInfrared(bool infrared) => _pipelineInfrared = infrared;

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
                CommandBuffer cmd = new() { name = "MissileCamera.TerrainWindow" };
                try
                {
                    terrainHeightMap.BakeWindow(cmd, windowIndex);
                    Graphics.ExecuteCommandBuffer(cmd);
                }
                finally
                {
                    cmd.Release();
                }
            }

            Shader.SetGlobalTexture(HeightMapId, terrainHeightMap.heightMap);
            Shader.SetGlobalTexture(BlockerMapId, terrainHeightMap.blockerMap);
        }

        private static void MirrorUrpFromReference(Camera feedCamera, bool forceLdr)
        {
            Camera? reference = ResolveReferenceCamera() ?? Camera.main;
            if (reference == null)
                return;

            feedCamera.cullingMask = reference.cullingMask;
            feedCamera.allowHDR = forceLdr ? false : reference.allowHDR;
            feedCamera.allowMSAA = reference.allowMSAA;
            feedCamera.clearFlags = reference.clearFlags;

            UniversalAdditionalCameraData feedUrp = feedCamera.GetUniversalAdditionalCameraData();
            UniversalAdditionalCameraData refUrp = reference.GetUniversalAdditionalCameraData();
            feedUrp.SetRenderer(GetRendererIndex(refUrp));
            feedUrp.renderShadows = refUrp.renderShadows;
            // Never inherit TargetCam postFX — IR uses our local Volume only when policy says ON.
            feedUrp.renderPostProcessing = _pipelineInfrared;
            feedUrp.volumeTrigger = feedCamera.transform;
            feedUrp.antialiasing = refUrp.antialiasing;
            feedUrp.antialiasingQuality = refUrp.antialiasingQuality;
            feedUrp.dithering = refUrp.dithering;
            feedUrp.stopNaN = refUrp.stopNaN;
            feedUrp.requiresDepthOption = CameraOverrideOption.UsePipelineSettings;
            feedUrp.requiresColorOption = CameraOverrideOption.UsePipelineSettings;
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
            FieldInfo? field = typeof(UniversalAdditionalCameraData).GetField(
                "m_RendererIndex",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field?.GetValue(cameraData) is int index)
                return index;

            return 0;
        }

        private static int GetMaxTargetOffset()
        {
            int windowSize = GetWindowSize();
            int windowSnapping = GetWindowSnapping();
            return Mathf.Max(0, windowSize / 2 - windowSnapping * 2);
        }

        private static int GetWindowSize()
        {
            DetailRenderer? detail = SceneSingleton<DetailRenderer>.i;
            return detail != null ? detail.windowSize : 1024;
        }

        private static int GetWindowSnapping()
        {
            DetailRenderer? detail = SceneSingleton<DetailRenderer>.i;
            return detail != null ? detail.windowSnapping : 64;
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
