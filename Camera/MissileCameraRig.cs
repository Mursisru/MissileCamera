using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MissileCamera
{
    internal sealed class MissileCameraRig
    {
        private const float FarClipPlane = 60000f;

        private const float NvgGainMin = 0.5f;
        private const float NvgGainMax = 3f;
        private const float NvgBloomThresholdMin = 0.2f;
        private const float NvgBloomThresholdMax = 1.2f;

        private readonly GameObject _root;
        private readonly Camera _camera;
        private readonly Volume _irVolume;
        private readonly ColorAdjustments _colorAdjustments;
        private readonly Bloom _bloom;
        private RenderTexture? _renderTexture;
        private RenderTexture? _hdrRenderTexture;
        private Missile? _missile;
        private int _textureWidth;
        private int _textureHeight;
        private float _localNoseZ;
        private float _boreRollDeg;
        private float _rollVelocity;
        private int _lastRollAdvanceFrame = -1;
        private float _filteredLateralG;
        private float _filteredTurnSign;
        private float _zoomOffset;
        private float _fullscreenMagnification = 1f;
        private MissileCameraVisionMode _visionMode = MissileCameraVisionMode.Color;
        private bool _infraredVolumeActive;
        private bool _nightVisionActive;
        private bool _infraredVolumeEnabledDuringRender;
        private bool _renderPostProcessingEnabled;
        private float _infraredBlitExposure;
        private float _infraredBlitContrast = 1f;
        private bool _infraredSyncedFromVanilla;
        private bool _lastFrameUsedBlit;
        private float _lastPolicyExposure;
        private float _nvgGainLastUpdated = -100f;
        private InfraredExposureBreakdown _lastExposureBreakdown;
        private bool _pipelineDriven;

        internal bool IsInfraredVolumePrimed => _infraredVolumeActive;
        internal bool IsInfraredVolumeEnabledDuringRender => _infraredVolumeEnabledDuringRender;
        internal bool IsRenderPostProcessingEnabled => _renderPostProcessingEnabled;
        internal bool LastFrameUsedBlit => _lastFrameUsedBlit;
        internal bool InfraredSyncedFromVanilla => _infraredSyncedFromVanilla;
        internal float InfraredBlitExposure => _infraredBlitExposure;
        internal float InfraredBlitContrast => _infraredBlitContrast;
        internal float LastPolicyExposure => _lastPolicyExposure;
        internal InfraredExposureBreakdown LastExposureBreakdown => _lastExposureBreakdown;
        internal float InfraredSaturation => _infraredVolumeActive ? -100f : 0f;
        internal float InfraredPostExposure => _infraredBlitExposure;
        internal float InfraredContrast => _infraredBlitContrast;

        internal MissileCameraRig()
        {
            _root = new GameObject("MissileCamera.Rig");
            Object.DontDestroyOnLoad(_root);

            _camera = _root.AddComponent<Camera>();
            _camera.enabled = false;
            _camera.stereoTargetEye = StereoTargetEyeMask.None;
            _camera.depth = -100f;
            _camera.nearClipPlane = 0.15f;
            _camera.farClipPlane = FarClipPlane;
            _camera.useOcclusionCulling = false;
            _camera.clearFlags = CameraClearFlags.Skybox;

            UniversalAdditionalCameraData urp = _camera.GetUniversalAdditionalCameraData();
            urp.renderType = CameraRenderType.Base;
            urp.requiresColorOption = CameraOverrideOption.UsePipelineSettings;
            urp.requiresDepthOption = CameraOverrideOption.UsePipelineSettings;
            urp.volumeTrigger = _camera.transform;

            _irVolume = _root.AddComponent<Volume>();
            _irVolume.isGlobal = false;
            _irVolume.priority = 1000f;
            _irVolume.blendDistance = 0f;
            _irVolume.enabled = false;
            _irVolume.weight = 1f;
            // No SphereCollider — huge local volumes tint/break world when Camera.main is null/spectate.
            // NVG enables isGlobal only for the duration of Camera.Render() then clears.

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.hideFlags = HideFlags.HideAndDontSave;
            _colorAdjustments = profile.Add<ColorAdjustments>(true);
            _colorAdjustments.saturation.Override(-100f);
            _colorAdjustments.saturation.overrideState = false;
            _colorAdjustments.postExposure.Override(0f);
            _colorAdjustments.contrast.Override(1f);
            _bloom = profile.Add<Bloom>(true);
            _bloom.threshold.Override(0.9f);
            _bloom.intensity.Override(0.35f);
            _bloom.threshold.overrideState = false;
            _bloom.intensity.overrideState = false;
            _irVolume.profile = profile;

            ApplyConfig();
        }

        internal RenderTexture? Texture => _renderTexture;
        internal Missile? Missile => _missile;
        internal Camera FeedCamera => _camera;
        internal float BoreRollDeg => _boreRollDeg;
        internal bool IsPipelineDriven => _pipelineDriven;
        internal HorizonFrame LastHorizonFrame { get; private set; } = HorizonFrame.Empty;

        internal float ZoomOffset => _zoomOffset;
        internal float FullscreenMagnification => _fullscreenMagnification;
        internal MissileCameraVisionMode VisionMode => _visionMode;

        internal void SetZoomOffset(float offset)
        {
            _zoomOffset = offset;
            ApplyEffectiveFov();
        }

        internal void SetFullscreenMagnification(float magnification)
        {
            _fullscreenMagnification = MissileCameraControlsConfig.ClampFullscreenMagnification(magnification);
            ApplyEffectiveFov();
        }

        internal void SetVisionMode(MissileCameraVisionMode mode, float infraredExposure)
        {
            if (!IsRootAlive)
                return;

            bool blitIr = MissileCameraVisionModeController.UsesInfraredBlit(mode);
            bool nvg = MissileCameraVisionModeController.UsesNightVisionVolume(mode);

            if (_visionMode == mode)
            {
                if (blitIr)
                {
                    if (!Mathf.Approximately(_lastPolicyExposure, infraredExposure)
                        || !_infraredVolumeActive)
                        SetInfraredVolume(true, infraredExposure);
                }
                else if (nvg)
                {
                    // Re-arm Volume/gain — pipeline wipe or early-out must not leave NVG dead.
                    EnableNightVisionVolume();
                }

                return;
            }

            _visionMode = mode;
            _nightVisionActive = nvg;
            if (blitIr)
            {
                SetInfraredVolume(true, infraredExposure);
                DisableNightVisionVolume();
            }
            else if (nvg)
            {
                SetInfraredVolume(false, 0f);
                EnableNightVisionVolume();
            }
            else
            {
                SetInfraredVolume(false, 0f);
                DisableNightVisionVolume();
            }

            MissileCameraRenderPrep.SetPipelineNightVision(nvg);
        }

        /// <summary>
        /// URP Base when pipeline-driven. Manual path keeps camera enabled (ParticleSystem Automatic
        /// culling ignores disabled cams) as orphan Overlay so URP does not auto-draw; Camera.Render fills RT.
        /// </summary>
        internal void SetPipelineDriven(bool enabled)
        {
            if (!IsRootAlive)
                return;

            if (_pipelineDriven == enabled)
            {
                if (enabled)
                {
                    ApplyConfigIfNeeded();
                    ApplyPose();
                    ApplyPipelineInfraredState();
                    MissileCameraRenderPrep.SetPipelineDriven(
                        _camera, true, forceLdr: false, infrared: _infraredVolumeActive);
                    ApplyFeedCameraActiveState();
                }

                return;
            }

            _pipelineDriven = enabled;
            if (!enabled)
            {
                MissileCameraRenderPrep.SetPipelineDriven(null, false);
                ApplyPipelineInfraredState();
                ApplyFeedCameraActiveState();
                return;
            }

            ApplyConfigIfNeeded();
            ApplyPose();
            _camera.targetTexture = _renderTexture;
            ApplyPipelineInfraredState();
            MissileCameraRenderPrep.SetPipelineDriven(
                _camera, true, forceLdr: false, infrared: _infraredVolumeActive);
            ApplyFeedCameraActiveState();
        }

        /// <summary>
        /// Keep feed Camera.enabled while following so world VFX/particles near the missile stay simulated.
        /// Manual (non-pipeline) path uses Overlay to avoid a second URP Base pass on the same RT.
        /// </summary>
        private void ApplyFeedCameraActiveState()
        {
            if (!IsRootAlive)
                return;

            UniversalAdditionalCameraData urp = _camera.GetUniversalAdditionalCameraData();

            if (_pipelineDriven)
            {
                urp.renderType = CameraRenderType.Base;
                _camera.targetTexture = _renderTexture;
                _camera.enabled = true;
                return;
            }

            // Manual IR/NVG: Overlay + Camera.Render only. Leaving enabled=true made URP
            // treat it as a live camera alongside main/aircraft mini-cam (issue #3 FPS).
            // Particles: COLOR path is pipeline-driven (enabled Base) — not this branch.
            urp.renderType = CameraRenderType.Overlay;
            _camera.targetTexture = _renderTexture;
            _camera.enabled = false;
        }

        /// <summary>
        /// TargetCam IR parity. Pipeline mode uses URP Volume (like vanilla). MFD uses HDR blit.
        /// </summary>
        internal void SetInfraredVolume(bool infrared, float exposure)
        {
            if (!IsRootAlive)
                return;

            if (!infrared && !_infraredVolumeActive)
                return;

            if (infrared
                && _infraredVolumeActive
                && Mathf.Approximately(_lastPolicyExposure, exposure))
                return;

            _infraredVolumeActive = infrared;
            _lastPolicyExposure = exposure;

            if (!infrared)
            {
                _infraredBlitExposure = 0f;
                _infraredBlitContrast = 1f;
                _infraredSyncedFromVanilla = false;
                _lastExposureBreakdown = default;
                _irVolume.enabled = false;
                _colorAdjustments.saturation.overrideState = false;
                MissileCameraRenderPrep.SetPipelineInfrared(false);
                return;
            }

            ResolveInfraredRenderParams(exposure);
            ApplyPipelineInfraredState();
        }

        private void ApplyPipelineInfraredState()
        {
            if (!IsRootAlive)
                return;

            if (_pipelineDriven && _infraredVolumeActive)
            {
                // Local volume + volumeTrigger only — never global (would tint main cam / stick IR look).
                _irVolume.isGlobal = false;
                _irVolume.enabled = true;
                _colorAdjustments.saturation.Override(-100f);
                _colorAdjustments.saturation.overrideState = true;
                _colorAdjustments.postExposure.Override(_infraredBlitExposure);
                _colorAdjustments.postExposure.overrideState = true;
                _colorAdjustments.contrast.Override(_infraredBlitContrast);
                _colorAdjustments.contrast.overrideState = true;
                UniversalAdditionalCameraData urp = _camera.GetUniversalAdditionalCameraData();
                urp.renderPostProcessing = true;
                urp.volumeTrigger = _camera.transform;
                _infraredVolumeEnabledDuringRender = true;
                _renderPostProcessingEnabled = true;
                MissileCameraRenderPrep.SetPipelineInfrared(true);
                return;
            }

            _irVolume.isGlobal = false;
            _irVolume.enabled = false;
            _colorAdjustments.saturation.Override(0f);
            _colorAdjustments.saturation.overrideState = false;
            _colorAdjustments.postExposure.Override(0f);
            _colorAdjustments.postExposure.overrideState = false;
            _colorAdjustments.contrast.Override(0f);
            _colorAdjustments.contrast.overrideState = false;
            _infraredVolumeEnabledDuringRender = false;
            _renderPostProcessingEnabled = false;
            if (_pipelineDriven)
            {
                // COLOR: do not inherit TargetCam IR post stack — feed stays full color.
                UniversalAdditionalCameraData urp = _camera.GetUniversalAdditionalCameraData();
                urp.renderPostProcessing = false;
                MissileCameraRenderPrep.SetPipelineInfrared(false);
            }
        }

        private void ResolveInfraredRenderParams(float policyExposure)
        {
            float contrast = 1f;
            if (TargetCamAccess.TryGetVanillaIrSnapshot(out bool vanillaIr, out _, out float vanillaContrast) && vanillaIr)
                contrast = vanillaContrast;

            float finalExposure = MissileCameraInfraredExposure.Resolve(
                _camera,
                policyExposure,
                out _lastExposureBreakdown);

            _infraredBlitExposure = finalExposure;
            _infraredBlitContrast = contrast;
            _infraredSyncedFromVanilla = _lastExposureBreakdown.SyncedVanilla;
        }

        internal void Attach(Missile missile)
        {
            if (!IsRootAlive)
                return;

            if (_missile == missile)
                return;

            _missile = missile;
            _boreRollDeg = 0f;
            _rollVelocity = 0f;
            _filteredLateralG = 0f;
            _filteredTurnSign = 0f;
            _lastRollAdvanceFrame = -1;
            LastHorizonFrame = HorizonFrame.Empty;

            MissileCameraNoseResolveResult nose = MissileCameraNoseResolver.Resolve(missile);
            _localNoseZ = nose.CameraLocalZ;

            _root.transform.SetParent(missile.transform, false);
            _root.transform.localPosition = new Vector3(0f, 0f, _localNoseZ);
            _root.transform.localRotation = Quaternion.identity;
            ApplyFeedCameraActiveState();

            string unitName = missile.definition != null ? missile.definition.unitName : missile.name;
            MfdLog.Info(
                $"missileCam nose id={missile.persistentID} name={unitName} " +
                $"meshZ={nose.MeshMaxLocalZ:F2} colliderZ={nose.ColliderMaxLocalZ:F2} " +
                $"defL={(missile.definition != null ? missile.definition.length : 0f):F2} " +
                $"pivot={nose.PivotMode} source={nose.Source} cameraLocalZ={nose.CameraLocalZ:F2}");
        }

        internal bool IsRootAlive => _root != null;

        internal void Detach()
        {
            _missile = null;
            _boreRollDeg = 0f;
            _rollVelocity = 0f;
            _filteredLateralG = 0f;
            _filteredTurnSign = 0f;
            _lastRollAdvanceFrame = -1;
            LastHorizonFrame = HorizonFrame.Empty;
            if (!IsRootAlive)
                return;

            if (_root.transform.parent != null)
                _root.transform.SetParent(null, true);

            ApplyFeedCameraActiveState();
        }

        internal void RenderFrame(bool managePrep = true)
        {
            if (_pipelineDriven)
                return;

            if (!IsRootAlive || _missile == null || _missile.disabled || _missile.rb == null || _renderTexture == null)
                return;

            ApplyConfigIfNeeded();
            ApplyPose();

            bool useBlit = _infraredVolumeActive
                && MissileCameraVisionModeController.UsesInfraredBlit(_visionMode);
            bool useNvg = _nightVisionActive
                && MissileCameraVisionModeController.UsesNightVisionVolume(_visionMode);

            bool prevFog = RenderSettings.fog;
            RenderTexture? prevActive = RenderTexture.active;
            RenderTexture? prevCameraTarget = _camera.targetTexture;
            bool prevAllowHdr = _camera.allowHDR;
            UniversalAdditionalCameraData? urp = null;
            _infraredVolumeEnabledDuringRender = false;
            _renderPostProcessingEnabled = false;
            _lastFrameUsedBlit = false;
            try
            {
                RenderSettings.fog = !useBlit;
                if (useBlit)
                {
                    EnsureHdrTexture();
                    _camera.allowHDR = true;
                    _camera.targetTexture = _hdrRenderTexture;
                }
                else
                {
                    _camera.allowHDR = useNvg;
                    _camera.targetTexture = _renderTexture;
                }

                if (managePrep)
                    MissileCameraRenderPrep.BeforeRender(_camera, forceLdr: false);

                urp = _camera.GetUniversalAdditionalCameraData();
                if (useBlit)
                {
                    urp.renderPostProcessing = false;
                    _camera.allowHDR = true;
                    if (_irVolume != null)
                        _irVolume.enabled = false;
                }
                else if (useNvg)
                {
                    TickNightVisionGain();
                    if (_irVolume != null)
                    {
                        // Scoped global only for this Camera.Render — no persistent world tint.
                        _irVolume.isGlobal = true;
                        _irVolume.enabled = true;
                    }
                    urp.renderPostProcessing = true;
                    urp.volumeTrigger = _camera.transform;
                    _renderPostProcessingEnabled = true;
                    _infraredVolumeEnabledDuringRender = true;
                }
                else
                {
                    if (_irVolume != null)
                        _irVolume.enabled = false;
                    urp.renderPostProcessing = false;
                }

                // Overlay keeps URP from auto-drawing; force Base for this manual submit so particles/VFX render.
                CameraRenderType prevType = urp.renderType;
                urp.renderType = CameraRenderType.Base;
                _camera.enabled = true;
                try
                {
                    _camera.Render();
                }
                finally
                {
                    urp.renderType = prevType;
                }

                if (useBlit && _hdrRenderTexture != null && _renderTexture != null)
                {
                    MissileCameraInfraredBlit.Apply(
                        _hdrRenderTexture,
                        _renderTexture,
                        _infraredBlitExposure,
                        _infraredBlitContrast,
                        _visionMode);
                    _lastFrameUsedBlit = MissileCameraInfraredBlit.IsAvailable;
                    MissileCameraInfraredAudit.LogAfterRender(this, _renderTexture);
                }
            }
            finally
            {
                if (urp != null)
                    urp.renderPostProcessing = false;
                if (_irVolume != null)
                {
                    _irVolume.enabled = false;
                    _irVolume.isGlobal = false;
                }

                _camera.targetTexture = prevCameraTarget ?? _renderTexture;
                _camera.allowHDR = prevAllowHdr;
                _infraredVolumeEnabledDuringRender = false;
                _renderPostProcessingEnabled = false;
                if (managePrep)
                    MissileCameraRenderPrep.AfterRender();
                RenderSettings.fog = prevFog;
                RenderTexture.active = prevActive;
                ApplyFeedCameraActiveState();
            }
        }

        internal void Destroy()
        {
            SetPipelineDriven(false);
            Detach();
            SetInfraredVolume(false, 0f);
            ReleaseTexture();
            MissileCameraInfraredBlit.Shutdown();
            if (_irVolume != null && _irVolume.profile != null)
                Object.Destroy(_irVolume.profile);
            if (_root != null)
                Object.Destroy(_root);
        }

        internal void AdvanceRoll(float deltaTime)
        {
            if (!IsRootAlive || _missile == null || deltaTime <= 0f)
                return;

            if (_lastRollAdvanceFrame == Time.frameCount)
                return;

            _lastRollAdvanceFrame = Time.frameCount;

            MissileTurnLoad.TrySampleHorizontalTurn(_missile, out float rawLateralG, out float rawTurnSign);
            float filterT = 1f - Mathf.Exp(-MissileCameraFeedConfig.TurnLookGFilterHz * deltaTime);
            _filteredLateralG = Mathf.Lerp(_filteredLateralG, rawLateralG, filterT);

            if (Mathf.Abs(rawTurnSign) < 0.01f)
                _filteredTurnSign = Mathf.MoveTowards(_filteredTurnSign, 0f, deltaTime * 2.5f);
            else
                _filteredTurnSign = Mathf.Lerp(_filteredTurnSign, rawTurnSign, filterT);

            float targetRoll = MissileTurnLoad.ComputeTargetRollDeg(
                _missile,
                _filteredLateralG,
                _filteredTurnSign);

            float smoothTime = MissileCameraFeedConfig.TurnLookSmoothTime;
            if (smoothTime <= 0.001f)
            {
                _boreRollDeg = targetRoll;
                _rollVelocity = 0f;
            }
            else
            {
                _boreRollDeg = Mathf.SmoothDamp(
                    _boreRollDeg,
                    targetRoll,
                    ref _rollVelocity,
                    smoothTime,
                    MissileCameraFeedConfig.TurnLookSlewDegPerSec,
                    deltaTime);
            }
        }

        internal void SyncPose()
        {
            ApplyPose();
        }

        private void ApplyPose()
        {
            if (!IsRootAlive || _missile == null)
                return;

            _root.transform.localPosition = new Vector3(0f, 0f, _localNoseZ);
            _root.transform.localRotation = Quaternion.identity;

            Transform missileTransform = _missile.transform;

            Quaternion desiredWorld = HorizonFrame.BuildCameraWorldRotation(
                missileTransform,
                _boreRollDeg,
                MissileCameraFeedConfig.HorizonLock);

            Quaternion bodyWorld = missileTransform.rotation;
            _camera.transform.localRotation = Quaternion.Inverse(bodyWorld) * desiredWorld;

            LastHorizonFrame = HorizonFrame.FromCamera(_camera, _filteredLateralG, _boreRollDeg);
        }

        private void ApplyConfigIfNeeded()
        {
            if (!IsRootAlive)
                return;

            MissileCameraFeedConfig.ResolveActiveFeedSize(out int w, out int h);
            if (_renderTexture != null && _textureWidth == w && _textureHeight == h)
            {
                ApplyEffectiveFov();
                return;
            }

            ApplyConfig();
        }

        private void ApplyEffectiveFov()
        {
            if (!IsRootAlive)
                return;

            float baseFov = MissileCameraFeedConfig.Fov;
            if (MissileCameraFullscreenController.IsActive)
            {
                _camera.fieldOfView = MissileCameraControlsConfig.ComputeFullscreenFov(
                    baseFov,
                    _fullscreenMagnification);
                return;
            }

            _camera.fieldOfView = MissileCameraControlsConfig.ComputeEffectiveFov(baseFov, _zoomOffset);
        }

        private void EnableNightVisionVolume()
        {
            if (!IsRootAlive)
                return;

            _nightVisionActive = true;
            _irVolume.isGlobal = false;
            _irVolume.enabled = false; // armed in RenderFrame only
            _colorAdjustments.saturation.overrideState = false;
            _colorAdjustments.contrast.Override(5f);
            _colorAdjustments.contrast.overrideState = true;
            // Soft green tint (stock NVG feel) without hijacking cockpit NightVision.Toggle.
            _colorAdjustments.colorFilter.Override(new Color(0.55f, 1f, 0.55f, 1f));
            _colorAdjustments.colorFilter.overrideState = true;
            _bloom.intensity.Override(0.35f);
            _bloom.intensity.overrideState = true;
            _bloom.threshold.overrideState = true;
            TickNightVisionGain();
            MissileCameraRenderPrep.SetPipelineNightVision(true);
        }

        private void DisableNightVisionVolume()
        {
            _nightVisionActive = false;
            _bloom.threshold.overrideState = false;
            _bloom.intensity.overrideState = false;
            _colorAdjustments.contrast.overrideState = false;
            _colorAdjustments.postExposure.overrideState = false;
            _colorAdjustments.colorFilter.overrideState = false;
            if (_irVolume != null)
            {
                _irVolume.enabled = false;
                _irVolume.isGlobal = false;
            }
            if (IsRootAlive)
            {
                UniversalAdditionalCameraData urp = _camera.GetUniversalAdditionalCameraData();
                urp.renderPostProcessing = false;
            }
            MissileCameraRenderPrep.SetPipelineNightVision(false);
        }

        private void TickNightVisionGain()
        {
            if (Time.unscaledTime - _nvgGainLastUpdated < 1f)
                return;

            _nvgGainLastUpdated = Time.unscaledTime;
            float ambient = 0.2f;
            try
            {
                if (LevelInfoAccess.TryGetAmbientLight(out float a))
                    ambient = a;
            }
            catch
            {
                // ignore
            }

            float t = Mathf.InverseLerp(0.01f, 0.4f, ambient);
            // Dump NightVision.UpdateGain: postExposure + bloom.threshold from ambient.
            _colorAdjustments.postExposure.Override(Mathf.Lerp(NvgGainMax, NvgGainMin, t));
            _colorAdjustments.postExposure.overrideState = true;
            _bloom.threshold.Override(Mathf.Lerp(NvgBloomThresholdMin, NvgBloomThresholdMax, t));
        }

        private void ApplyConfig()
        {
            if (!IsRootAlive)
                return;

            MissileCameraFeedConfig.ResolveActiveFeedSize(out _textureWidth, out _textureHeight);
            ApplyEffectiveFov();
            _camera.nearClipPlane = 0.15f;

            ReleaseTexture();
            int msaa = MissileCameraRenderPrep.ResolvePipelineMsaaSampleCount();
            bool fullscreen = MissileCameraFullscreenController.IsActive;
            bool bilinear = !fullscreen || _fullscreenMagnification > 1.01f;
            _renderTexture = new RenderTexture(_textureWidth, _textureHeight, 16, RenderTextureFormat.ARGB32)
            {
                name = "MissileCameraFeed",
                antiAliasing = msaa,
                useMipMap = false,
                autoGenerateMips = false,
                filterMode = bilinear ? FilterMode.Bilinear : FilterMode.Point
            };
            _renderTexture.Create();
            _camera.targetTexture = _renderTexture;
            ReleaseHdrTexture();
        }

        private void EnsureHdrTexture()
        {
            if (!IsRootAlive || _renderTexture == null)
                return;

            int w = _renderTexture.width;
            int h = _renderTexture.height;
            if (_hdrRenderTexture != null && _hdrRenderTexture.width == w && _hdrRenderTexture.height == h)
                return;

            ReleaseHdrTexture();
            _hdrRenderTexture = new RenderTexture(w, h, 16, RenderTextureFormat.ARGBHalf)
            {
                name = "MissileCameraFeed.HDR",
                antiAliasing = 1,
                useMipMap = false,
                autoGenerateMips = false
            };
            _hdrRenderTexture.Create();
        }

        private void ReleaseHdrTexture()
        {
            if (_hdrRenderTexture == null)
                return;

            _hdrRenderTexture.Release();
            Object.Destroy(_hdrRenderTexture);
            _hdrRenderTexture = null;
        }

        private void ReleaseTexture()
        {
            if (!IsRootAlive || _renderTexture == null)
                return;

            _camera.targetTexture = null;
            ReleaseHdrTexture();
            _renderTexture.Release();
            Object.Destroy(_renderTexture);
            _renderTexture = null;
        }
    }
}
