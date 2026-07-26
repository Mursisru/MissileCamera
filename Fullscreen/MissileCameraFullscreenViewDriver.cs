using UnityEngine;

namespace MissileCamera
{
    /// <summary>
    /// Fullscreen: drive vanilla CameraStateManager.mainCamera to the missile nose
    /// (same pivot parenting idea as CameraCockpitState), not RT + RawImage.
    /// </summary>
    internal static class MissileCameraFullscreenViewDriver
    {
        private static bool _hijacked;
        private static Transform? _savedPivotParent;
        private static Vector3 _savedPivotLocalPos;
        private static Quaternion _savedPivotLocalRot;
        private static Vector3 _savedCamLocalPos;
        private static Quaternion _savedCamLocalRot;
        private static float _savedFov;
        private static float _savedNear;
        private static float _localNoseZ = 0.5f;
        private static Missile? _missile;
        private static float _zoomOffset;

        internal static bool IsHijacked => _hijacked;

        internal static void Enter(Missile? missile)
        {
            if (_hijacked)
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

            _savedPivotParent = csm.cameraPivot.parent;
            _savedPivotLocalPos = csm.cameraPivot.localPosition;
            _savedPivotLocalRot = csm.cameraPivot.localRotation;
            _savedCamLocalPos = csm.transform.localPosition;
            _savedCamLocalRot = csm.transform.localRotation;
            _savedFov = csm.mainCamera.fieldOfView;
            _savedNear = csm.mainCamera.nearClipPlane;

            MissileCameraNoseResolveResult nose = MissileCameraNoseResolver.Resolve(missile);
            _localNoseZ = nose.CameraLocalZ;
            _missile = missile;
            _hijacked = true;

            ApplyPose(csm, missile);
            csm.mainCamera.nearClipPlane = 0.15f;
            MfdLog.Info("fullscreen view: mainCamera → missile nose (vanilla path)");
        }

        internal static void Tick(Missile? missile, float zoomOffset)
        {
            if (!_hijacked)
                return;

            _zoomOffset = zoomOffset;
            CameraStateManager? csm = SceneSingleton<CameraStateManager>.i;
            if (csm == null || csm.mainCamera == null || csm.cameraPivot == null)
                return;

            if (missile == null || missile.disabled)
                return;

            if (!ReferenceEquals(_missile, missile))
            {
                _missile = missile;
                MissileCameraNoseResolveResult nose = MissileCameraNoseResolver.Resolve(missile);
                _localNoseZ = nose.CameraLocalZ;
            }

            ApplyPose(csm, missile);
            csm.mainCamera.fieldOfView = MissileCameraControlsConfig.ComputeEffectiveFov(
                MissileCameraFeedConfig.Fov,
                zoomOffset);
            csm.mainCamera.nearClipPlane = 0.15f;
        }

        /// <summary>Re-apply after LateUpdate if needed (blocked UpdateState path).</summary>
        internal static void LateTick()
        {
            if (!_hijacked || _missile == null || _missile.disabled)
                return;

            CameraStateManager? csm = SceneSingleton<CameraStateManager>.i;
            if (csm == null || csm.mainCamera == null)
                return;

            ApplyPose(csm, _missile);
            csm.mainCamera.fieldOfView = MissileCameraControlsConfig.ComputeEffectiveFov(
                MissileCameraFeedConfig.Fov,
                _zoomOffset);
        }

        internal static void Exit()
        {
            if (!_hijacked)
                return;

            CameraStateManager? csm = SceneSingleton<CameraStateManager>.i;
            if (csm != null && csm.cameraPivot != null)
            {
                csm.cameraPivot.SetParent(_savedPivotParent, false);
                csm.cameraPivot.localPosition = _savedPivotLocalPos;
                csm.cameraPivot.localRotation = _savedPivotLocalRot;
                csm.transform.localPosition = _savedCamLocalPos;
                csm.transform.localRotation = _savedCamLocalRot;
                if (csm.mainCamera != null)
                {
                    csm.mainCamera.fieldOfView = _savedFov;
                    csm.mainCamera.nearClipPlane = _savedNear;
                }
            }

            _hijacked = false;
            _missile = null;
            _savedPivotParent = null;
            MfdLog.Info("fullscreen view: mainCamera restored");
        }

        internal static bool ShouldBlockVanillaCameraState() => _hijacked;

        private static void ApplyPose(CameraStateManager csm, Missile missile)
        {
            Transform missileTransform = missile.transform;
            csm.cameraPivot.SetParent(missileTransform, false);
            csm.cameraPivot.localPosition = new Vector3(0f, 0f, _localNoseZ);
            csm.cameraPivot.localRotation = Quaternion.identity;
            csm.transform.localPosition = Vector3.zero;

            float boreRoll = MissileCameraFeedController.TryGetBoreRollDeg();
            Quaternion desiredWorld = HorizonFrame.BuildCameraWorldRotation(
                missileTransform,
                boreRoll,
                MissileCameraFeedConfig.HorizonLock);
            Quaternion bodyWorld = missileTransform.rotation;
            csm.transform.localRotation = Quaternion.Inverse(bodyWorld) * desiredWorld;
        }
    }
}
