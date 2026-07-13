using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MissileCamera
{
    internal sealed class MissileCameraRig
    {
        private const float FarClipPlane = 60000f;

        private readonly GameObject _root;
        private readonly Camera _camera;
        private readonly Volume _irVolume;
        private readonly ColorAdjustments _colorAdjustments;
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
        private bool _infraredVolumeActive;
        private bool _infraredVolumeEnabledDuringRender;
        private bool _renderPostProcessingEnabled;
        private float _infraredBlitExposure;
        private float _infraredBlitContrast = 1f;
        private bool _infraredSyncedFromVanilla;
        private bool _lastFrameUsedBlit;
        private float _lastPolicyExposure;
        private InfraredExposureBreakdown _lastExposureBreakdown;

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
            _camera.allowHDR = false;
            _camera.allowMSAA = false;
            _camera.clearFlags = CameraClearFlags.Skybox;

            UniversalAdditionalCameraData urp = _camera.GetUniversalAdditionalCameraData();
            urp.renderType = CameraRenderType.Base;
            urp.renderPostProcessing = false;
            urp.antialiasing = AntialiasingMode.None;
            urp.requiresColorOption = CameraOverrideOption.UsePipelineSettings;
            urp.requiresDepthOption = CameraOverrideOption.UsePipelineSettings;
            urp.renderShadows = true;

            _irVolume = _root.AddComponent<Volume>();
            _irVolume.isGlobal = false;
            _irVolume.priority = 1000f;
            _irVolume.blendDistance = 100000f;
            _irVolume.enabled = false;
            _irVolume.weight = 1f;

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.hideFlags = HideFlags.HideAndDontSave;
            _colorAdjustments = profile.Add<ColorAdjustments>(true);
            _colorAdjustments.saturation.Override(-100f);
            _colorAdjustments.saturation.overrideState = false;
            _colorAdjustments.postExposure.Override(0f);
            _colorAdjustments.contrast.Override(1f);
            _irVolume.profile = profile;

            ApplyConfig();
        }

        internal RenderTexture? Texture => _renderTexture;
        internal Missile? Missile => _missile;
        internal Camera FeedCamera => _camera;
        internal float BoreRollDeg => _boreRollDeg;
        internal HorizonFrame LastHorizonFrame { get; private set; } = HorizonFrame.Empty;

        internal void SetZoomOffset(float offset)
        {
            _zoomOffset = offset;
            ApplyEffectiveFov();
        }

        /// <summary>
        /// TargetCam IR parity: HDR scene render + blit ColorAdjustments (URP Volume on manual Render is unreliable).
        /// </summary>
        internal void SetInfraredVolume(bool infrared, float exposure)
        {
            if (!IsRootAlive)
                return;

            _infraredVolumeActive = infrared;
            _lastPolicyExposure = exposure;
            _irVolume.enabled = false;
            _colorAdjustments.saturation.overrideState = false;

            if (!infrared)
            {
                _infraredBlitExposure = 0f;
                _infraredBlitContrast = 1f;
                _infraredSyncedFromVanilla = false;
                _lastExposureBreakdown = default;
                return;
            }

            ResolveInfraredRenderParams(exposure);
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
        }

        internal void RenderFrame()
        {
            if (!IsRootAlive || _missile == null || _missile.disabled || _missile.rb == null || _renderTexture == null)
                return;

            ApplyConfigIfNeeded();
            ApplyPose();

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
                RenderSettings.fog = !_infraredVolumeActive;
                if (_infraredVolumeActive)
                {
                    EnsureHdrTexture();
                    _camera.allowHDR = true;
                    _camera.targetTexture = _hdrRenderTexture;
                }
                else
                {
                    _camera.allowHDR = false;
                    _camera.targetTexture = _renderTexture;
                }

                MissileCameraRenderPrep.BeforeRender(_camera, forceLdr: false);
                if (_infraredVolumeActive)
                {
                    urp = _camera.GetUniversalAdditionalCameraData();
                    urp.renderPostProcessing = false;
                    _camera.allowHDR = true;
                }

                _camera.Render();

                if (_infraredVolumeActive && _hdrRenderTexture != null && _renderTexture != null)
                {
                    MissileCameraInfraredBlit.Apply(
                        _hdrRenderTexture,
                        _renderTexture,
                        _infraredBlitExposure,
                        _infraredBlitContrast);
                    _lastFrameUsedBlit = MissileCameraInfraredBlit.IsAvailable;
                    MissileCameraInfraredAudit.LogAfterRender(this, _renderTexture);
                }
            }
            finally
            {
                if (urp != null)
                    urp.renderPostProcessing = false;
                if (_irVolume != null)
                    _irVolume.enabled = false;

                _camera.targetTexture = prevCameraTarget ?? _renderTexture;
                _camera.allowHDR = prevAllowHdr;
                _infraredVolumeEnabledDuringRender = false;
                _renderPostProcessingEnabled = false;
                MissileCameraRenderPrep.AfterRender();
                RenderSettings.fog = prevFog;
                RenderTexture.active = prevActive;
            }
        }

        internal void Destroy()
        {
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
            MissileTurnLoad.TrySampleHorizontalTurn(_missile, out float lateralG, out _);

            Quaternion desiredWorld = HorizonFrame.BuildCameraWorldRotation(
                missileTransform,
                _boreRollDeg,
                MissileCameraFeedConfig.HorizonLock);

            Quaternion bodyWorld = missileTransform.rotation;
            _camera.transform.localRotation = Quaternion.Inverse(bodyWorld) * desiredWorld;

            LastHorizonFrame = HorizonFrame.FromCamera(_camera, lateralG, _boreRollDeg);
        }

        private void ApplyConfigIfNeeded()
        {
            if (!IsRootAlive)
                return;

            int w = MissileCameraFeedConfig.FeedWidth;
            int h = MissileCameraFeedConfig.FeedHeight;
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

            _camera.fieldOfView = MissileCameraControlsConfig.ComputeEffectiveFov(
                MissileCameraFeedConfig.Fov,
                _zoomOffset);
        }

        private void ApplyConfig()
        {
            if (!IsRootAlive)
                return;

            _textureWidth = MissileCameraFeedConfig.FeedWidth;
            _textureHeight = MissileCameraFeedConfig.FeedHeight;
            ApplyEffectiveFov();
            _camera.nearClipPlane = 0.15f;

            ReleaseTexture();
            _renderTexture = new RenderTexture(_textureWidth, _textureHeight, 16, RenderTextureFormat.ARGB32)
            {
                name = "MissileCameraFeed",
                antiAliasing = 1,
                useMipMap = false,
                autoGenerateMips = false
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
