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
        private RenderTexture? _renderTexture;
        private Missile? _missile;
        private int _textureWidth;
        private int _textureHeight;
        private float _localNoseZ;
        private float _boreRollDeg;
        private float _rollVelocity;
        private int _lastRollAdvanceFrame = -1;
        private float _filteredLateralG;
        private float _filteredTurnSign;

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
            urp.requiresColorOption = CameraOverrideOption.Off;
            urp.requiresDepthOption = CameraOverrideOption.Off;

            ApplyConfig();
        }

        internal RenderTexture? Texture => _renderTexture;
        internal Missile? Missile => _missile;
        internal Camera FeedCamera => _camera;
        internal float BoreRollDeg => _boreRollDeg;
        internal HorizonFrame LastHorizonFrame { get; private set; } = HorizonFrame.Empty;

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
            try
            {
                RenderSettings.fog = false;
                _camera.Render();
            }
            finally
            {
                RenderSettings.fog = prevFog;
                RenderTexture.active = prevActive;
            }
        }

        internal void Destroy()
        {
            Detach();
            ReleaseTexture();
            if (_root != null)
                Object.Destroy(_root);
        }

        /// <summary>
        /// Smooth bore-axis roll — call once per frame before render/HUD.
        /// </summary>
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
                _camera.fieldOfView = MissileCameraFeedConfig.Fov;
                return;
            }

            ApplyConfig();
        }

        private void ApplyConfig()
        {
            if (!IsRootAlive)
                return;

            _textureWidth = MissileCameraFeedConfig.FeedWidth;
            _textureHeight = MissileCameraFeedConfig.FeedHeight;
            _camera.fieldOfView = MissileCameraFeedConfig.Fov;
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
        }

        private void ReleaseTexture()
        {
            if (!IsRootAlive || _renderTexture == null)
                return;

            _camera.targetTexture = null;
            _renderTexture.Release();
            Object.Destroy(_renderTexture);
            _renderTexture = null;
        }
    }
}
