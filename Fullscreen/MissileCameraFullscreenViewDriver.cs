using UnityEngine;

namespace MissileCamera
{
    /// <summary>
    /// Fullscreen view without reparenting cameraPivot onto the missile (that stuck the camera on exit).
    /// Dump: CameraCockpitState.UpdateState always runs and snaps pivot to cockpitViewPoint.
    /// We only overwrite world pose AFTER vanilla LateUpdate; Exit = stop overwriting.
    /// </summary>
    internal static class MissileCameraFullscreenViewDriver
    {
        private static bool _active;
        private static float _savedFov;
        private static float _savedNear;
        private static float _localNoseZ = 0.5f;
        private static Missile? _missile;
        private static float _zoomOffset;

        internal static bool IsActive => _active;

        internal static void Enter(Missile? missile)
        {
            if (_active)
                return;

            CameraStateManager? csm = SceneSingleton<CameraStateManager>.i;
            if (csm == null || csm.mainCamera == null || csm.cameraPivot == null)
            {
                MfdLog.Info("fullscreen view: CameraStateManager missing");
                return;
            }

            if (missile == null || missile.disabled)
            {
                MfdLog.Info("fullscreen view: no missile");
                return;
            }

            _savedFov = csm.mainCamera.fieldOfView;
            _savedNear = csm.mainCamera.nearClipPlane;
            MissileCameraNoseResolveResult nose = MissileCameraNoseResolver.Resolve(missile);
            _localNoseZ = nose.CameraLocalZ;
            _missile = missile;
            _zoomOffset = 0f;
            _active = true;

            ApplyWorldPose(csm, missile);
            csm.mainCamera.nearClipPlane = 0.15f;
            MfdLog.Info("fullscreen view: pose overlay on (no reparent)");
        }

        internal static void TickZoom(float zoomOffset) => _zoomOffset = zoomOffset;

        /// <summary>
        /// Runs in CameraStateManager.LateUpdate Postfix — AFTER vanilla UpdateState.
        /// Toggle is processed here so Exit stops pose write in the same LateUpdate.
        /// </summary>
        internal static void LateTick()
        {
            // Toggle first: Exit clears _active before we would ApplyWorldPose.
            MissileCameraFeedInput.ProcessFullscreenToggle();

            if (!_active)
                return;

            CameraStateManager? csm = SceneSingleton<CameraStateManager>.i;
            if (csm == null || csm.mainCamera == null || csm.cameraPivot == null)
                return;

            Missile? missile = _missile;
            if (missile == null || missile.disabled)
            {
                // Never leave pose-overlay stuck after missile death.
                MissileCameraFullscreenController.ExitIfActive();
                return;
            }

            // Prefer live followed missile from feed controller when available.
            Missile? followed = MissileCameraFeedController.TryGetFollowedMissile();
            if (followed != null && !followed.disabled)
            {
                if (!ReferenceEquals(_missile, followed))
                {
                    _missile = followed;
                    MissileCameraNoseResolveResult nose = MissileCameraNoseResolver.Resolve(followed);
                    _localNoseZ = nose.CameraLocalZ;
                }

                missile = followed;
            }

            ApplyWorldPose(csm, missile);
            csm.mainCamera.fieldOfView = MissileCameraControlsConfig.ComputeEffectiveFov(
                MissileCameraFeedConfig.Fov,
                _zoomOffset);
            csm.mainCamera.nearClipPlane = 0.15f;
        }

        internal static void Exit()
        {
            if (!_active)
                return;

            _active = false;
            _missile = null;

            CameraStateManager? csm = SceneSingleton<CameraStateManager>.i;
            if (csm != null)
            {
                // Vanilla UpdateState already ran this LateUpdate (cockpit snap) before Postfix
                // when Exit is called from LateTick toggle. If Exit is called from EndOfFrame /
                // other paths, nudge FOV/near; next LateUpdate UpdateState restores pivot.
                if (csm.mainCamera != null)
                {
                    float fov = _savedFov > 1f ? _savedFov : PlayerSettings.defaultFoV;
                    csm.mainCamera.fieldOfView = fov;
                    csm.mainCamera.nearClipPlane = _savedNear > 0.01f ? _savedNear : 0.2f;
                    try
                    {
                        csm.SetDesiredFoV(PlayerSettings.defaultFoV, fov);
                    }
                    catch
                    {
                        // ignore
                    }
                }

                if (csm.cockpitCamRender != null)
                    csm.cockpitCamRender.enabled = true;

                CameraStateManager.cameraMode = CameraMode.cockpit;
            }

            MfdLog.Info("fullscreen view: pose overlay off → vanilla cockpit");
        }

        /// <summary>World pose only — never SetParent(missile). Parent stays vanilla cockpit chain.</summary>
        private static void ApplyWorldPose(CameraStateManager csm, Missile missile)
        {
            Transform body = missile.transform;
            Vector3 noseWorld = body.TransformPoint(new Vector3(0f, 0f, _localNoseZ));
            float boreRoll = MissileCameraFeedController.TryGetBoreRollDeg();
            Quaternion desiredWorld = HorizonFrame.BuildCameraWorldRotation(
                body,
                boreRoll,
                MissileCameraFeedConfig.HorizonLock);

            Transform pivot = csm.cameraPivot;
            pivot.position = noseWorld;
            pivot.rotation = desiredWorld;

            Transform cam = csm.transform;
            if (cam.parent != pivot)
                cam.SetParent(pivot, false);

            cam.localPosition = Vector3.zero;
            cam.localRotation = Quaternion.identity;
            cam.localScale = Vector3.one;
        }
    }
}
