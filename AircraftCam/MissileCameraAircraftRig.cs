using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MissileCamera
{
    /// <summary>Second Camera + RT for aircraft mini-cam. Separate from missile FX ping-pong.</summary>
    internal sealed class MissileCameraAircraftRig
    {
        private const float FarClipPlane = 60000f;

        private readonly GameObject _root;
        private readonly Camera _camera;
        private RenderTexture? _renderTexture;
        private Aircraft? _aircraft;
        private int _texW;
        private int _texH;
        private bool _forceCockpit;

        internal MissileCameraAircraftRig()
        {
            _root = new GameObject("MissileCamera.AircraftRig");
            Object.DontDestroyOnLoad(_root);

            _camera = _root.AddComponent<Camera>();
            _camera.enabled = false;
            _camera.stereoTargetEye = StereoTargetEyeMask.None;
            _camera.depth = -99f;
            _camera.nearClipPlane = 0.3f;
            _camera.farClipPlane = FarClipPlane;
            _camera.useOcclusionCulling = false;
            _camera.allowHDR = false;
            _camera.allowMSAA = false;
            _camera.clearFlags = CameraClearFlags.Skybox;
            _camera.fieldOfView = 55f;

            UniversalAdditionalCameraData urp = _camera.GetUniversalAdditionalCameraData();
            urp.renderType = CameraRenderType.Base;
            urp.renderPostProcessing = false;
            urp.antialiasing = AntialiasingMode.None;
            urp.renderShadows = true;
        }

        internal RenderTexture? Texture => _renderTexture;
        internal Camera FeedCamera => _camera;
        internal bool IsRootAlive => _root != null;

        internal void SetForceCockpit(bool force) => _forceCockpit = force;

        internal void Attach(Aircraft aircraft)
        {
            _aircraft = aircraft;
            EnsureTexture();
        }

        internal void Detach()
        {
            _aircraft = null;
        }

        internal void SyncPose()
        {
            if (_aircraft == null || !IsRootAlive)
                return;

            if (_forceCockpit)
            {
                Transform? cockpit = AircraftCamAccess.TryGetCockpitViewPoint(_aircraft);
                if (cockpit != null)
                {
                    _root.transform.SetPositionAndRotation(cockpit.position, cockpit.rotation);
                    _camera.fieldOfView = 60f;
                    _camera.nearClipPlane = 0.15f;
                    return;
                }
            }

            Transform t = _aircraft.transform;
            Vector3 pos;
            Quaternion rot;
            switch (MissileCameraAircraftCamConfig.Mode)
            {
                case MissileCameraAircraftCamMode.TopDown:
                    pos = t.position + Vector3.up * 80f;
                    rot = Quaternion.LookRotation(Vector3.down, t.forward);
                    break;
                case MissileCameraAircraftCamMode.Chase:
                    pos = t.position - t.forward * 18f + t.up * 4f;
                    rot = Quaternion.LookRotation(t.position - pos, Vector3.up);
                    break;
                default:
                    pos = t.position - t.forward * 12f + t.up * 2.5f;
                    rot = Quaternion.LookRotation(t.forward, t.up);
                    break;
            }

            _root.transform.SetPositionAndRotation(pos, rot);
            _camera.nearClipPlane = 0.3f;
        }

        /// <param name="managePrep">False when FrameRenderContext already prepared this camera.</param>
        internal void RenderFrame(bool managePrep = true)
        {
            if (!IsRootAlive || _aircraft == null || _renderTexture == null)
                return;

            EnsureTexture();
            SyncPose();

            RenderTexture? prevActive = RenderTexture.active;
            RenderTexture? prevTarget = _camera.targetTexture;
            try
            {
                _camera.targetTexture = _renderTexture;
                if (managePrep)
                    MissileCameraRenderPrep.BeforeRender(_camera, forceLdr: true);

                _camera.Render();
            }
            finally
            {
                _camera.targetTexture = prevTarget;
                if (managePrep)
                    MissileCameraRenderPrep.AfterRender();

                RenderTexture.active = prevActive;
            }
        }

        internal void Destroy()
        {
            Detach();
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Object.Destroy(_renderTexture);
                _renderTexture = null;
            }

            if (_root != null)
                Object.Destroy(_root);
        }

        private void EnsureTexture()
        {
            int w = Mathf.Clamp(MissileCameraAircraftCamConfig.Width, 64, 1024);
            int h = Mathf.Clamp(MissileCameraAircraftCamConfig.Height, 64, 1024);
            if (_renderTexture != null && _texW == w && _texH == h)
                return;

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Object.Destroy(_renderTexture);
            }

            _texW = w;
            _texH = h;
            _renderTexture = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32)
            {
                name = "MissileCamera.AircraftFeed",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            _renderTexture.Create();
        }
    }
}
