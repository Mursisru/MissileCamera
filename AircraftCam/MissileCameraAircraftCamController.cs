using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// Aircraft mini-cam UI + render. No-ops when DisplayMode resolves to Skip.
    /// </summary>
    internal static class MissileCameraAircraftCamController
    {
        private static MissileCameraAircraftRig? _rig;
        private static RawImage? _image;
        private static RectTransform? _root;
        private static float _nextRenderUnscaled;
        private static bool _loggedSkipNoOp;
        private static int _layoutRevision = -1;

        internal static bool IsActive =>
            MissileCameraAircraftCamConfig.Enabled
            && _image != null
            && _image.enabled;

        internal static void Tick()
        {
            MissileCameraAircraftCamConfig.Refresh();

            if (!MissileCameraAircraftCamConfig.Enabled)
            {
                TeardownUi();
                return;
            }

            if (!MfdLayoutController.IsLayoutActive)
            {
                if (!_loggedSkipNoOp)
                {
                    _loggedSkipNoOp = true;
                    MfdLog.Info("aircraft mini-cam no-op: MFD layout inactive (DisplayMode skip or not applied)");
                }

                TeardownUi();
                return;
            }

            _loggedSkipNoOp = false;

            if (MissileCameraFullscreenController.IsActive && MissileCameraAircraftCamConfig.HideInFullscreen)
            {
                if (_image != null)
                    _image.enabled = false;
                return;
            }

            if (!AircraftCamAccess.TryGetLocalAircraft(out Aircraft aircraft))
            {
                TeardownUi();
                return;
            }

            EnsureUi();
            ApplyLayoutRect();

            if (_rig == null)
                _rig = new MissileCameraAircraftRig();

            _rig.Attach(aircraft);

            // Render is driven from FeedController multi-render path when overlay active.
            if (_image != null && _rig.Texture != null)
            {
                _image.texture = _rig.Texture;
                _image.enabled = true;
            }
        }

        internal static void RenderIfDue(bool useSharedPrep)
        {
            if (!MissileCameraAircraftCamConfig.Enabled || _rig == null || !MfdLayoutController.IsLayoutActive)
                return;

            if (MissileCameraFullscreenController.IsActive && MissileCameraAircraftCamConfig.HideInFullscreen)
                return;

            if (Time.unscaledTime < _nextRenderUnscaled)
                return;

            float interval = 1f / Mathf.Max(MissileCameraAircraftCamConfig.RenderFps, 1);
            _nextRenderUnscaled = Time.unscaledTime + interval;

            if (useSharedPrep)
            {
                MissileCameraFrameRenderContext.PrepareCamera(_rig.FeedCamera, forceLdr: true);
                _rig.RenderFrame(managePrep: false);
            }
            else
            {
                _rig.RenderFrame(managePrep: true);
            }

            if (_image != null)
            {
                _image.texture = _rig.Texture;
                _image.enabled = true;
            }
        }

        internal static void CycleMode()
        {
            if (!MissileCameraAircraftCamConfig.Enabled || !MfdLayoutController.IsLayoutActive)
                return;

            MissileCameraAircraftCamConfig.CycleMode();
            MfdLog.Info("aircraft mini-cam mode=" + MissileCameraAircraftCamConfig.Mode);
        }

        internal static void Shutdown()
        {
            TeardownUi();
            _rig?.Destroy();
            _rig = null;
            _loggedSkipNoOp = false;
        }

        private static void EnsureUi()
        {
            if (_root != null && _image != null)
                return;

            GameObject? stub = GameObject.Find("MissileCamera.TacStub");
            if (stub == null)
                return;

            var go = new GameObject("MissileCamera.AircraftMiniCam", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(stub.transform, false);
            _root = go.GetComponent<RectTransform>();
            _image = go.GetComponent<RawImage>();
            _image.raycastTarget = false;
            _image.color = Color.white;
            ApplyLayoutRect();
        }

        private static void ApplyLayoutRect()
        {
            if (_root == null)
                return;

            if (_layoutRevision == MissileCameraAircraftCamConfig.Revision)
                return;

            _layoutRevision = MissileCameraAircraftCamConfig.Revision;
            _root.anchorMin = new Vector2(MissileCameraAircraftCamConfig.AnchorMinX, MissileCameraAircraftCamConfig.AnchorMinY);
            _root.anchorMax = new Vector2(MissileCameraAircraftCamConfig.AnchorMaxX, MissileCameraAircraftCamConfig.AnchorMaxY);
            _root.offsetMin = Vector2.zero;
            _root.offsetMax = Vector2.zero;
        }

        private static void TeardownUi()
        {
            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
                _root = null;
            }

            _image = null;
            _layoutRevision = -1;
            _rig?.Detach();
        }
    }
}
