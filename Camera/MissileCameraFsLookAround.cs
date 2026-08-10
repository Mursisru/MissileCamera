using UnityEngine;

namespace MissileCamera
{
    /// <summary>
    /// Fullscreen only: hold RMB to pan feed camera ±LookAroundMaxDeg (bore-local yaw/pitch).
    /// CAMERA_SAFETY: feed localRotation offset after ApplyPose — never CSM / mainCamera.
    /// Main aim HUD stays on bore (see GetBorePanelOffset); small LookAroundHud stays screen-center.
    /// </summary>
    internal static class MissileCameraFsLookAround
    {
        private const float DegPerUnit = 1.1f;
        private const float Deadzone = 0.015f;
        private const float BoreProjectDist = 2000f;

        private static float _yawDeg;
        private static float _pitchDeg;

        internal static bool IsLooking => _yawDeg * _yawDeg + _pitchDeg * _pitchDeg > 0.25f;

        /// <summary>Viewport of <paramref name="worldPoint"/> as seen on bore (pre–look-offset) camera.</summary>
        internal static bool TryWorldToBoreViewport(Camera cam, Vector3 worldPoint, out Vector3 viewport)
        {
            viewport = default;
            if (cam == null)
                return false;

            Transform ct = cam.transform;
            Transform? parent = ct.parent;
            if (parent == null)
                return false;

            Quaternion lookLocal = IsLooking ? BuildLookOffsetLocal() : Quaternion.identity;
            Quaternion boreWorld = parent.rotation * (ct.localRotation * Quaternion.Inverse(lookLocal));

            Vector3 local = Quaternion.Inverse(boreWorld) * (worldPoint - ct.position);
            if (local.z <= 0.05f)
                return false;

            float fovRad = cam.fieldOfView * Mathf.Deg2Rad;
            float tanHalf = Mathf.Tan(fovRad * 0.5f);
            if (tanHalf < 1e-6f)
                return false;

            float aspect = cam.aspect > 0.01f ? cam.aspect : 1f;
            viewport.x = local.x / (local.z * tanHalf * aspect) * 0.5f + 0.5f;
            viewport.y = local.y / (local.z * tanHalf) * 0.5f + 0.5f;
            viewport.z = local.z;
            return !float.IsNaN(viewport.x) && !float.IsNaN(viewport.y);
        }

        /// <summary>
        /// Where the missile bore appears in the looking camera, as panel-anchored px from center.
        /// Main reticle follows this; free-look center mark stays at (0,0).
        /// </summary>
        internal static Vector2 GetBorePanelOffset(Camera? cam, float panelW, float panelH)
        {
            if (!IsLooking || cam == null || panelW < 1f || panelH < 1f)
                return Vector2.zero;

            try
            {
                Transform ct = cam.transform;
                Transform? parent = ct.parent;
                if (parent == null)
                    return Vector2.zero;

                Quaternion lookLocal = BuildLookOffsetLocal();
                Quaternion boreLocal = ct.localRotation * Quaternion.Inverse(lookLocal);
                Vector3 boreFwdWorld = parent.rotation * (boreLocal * Vector3.forward);
                if (boreFwdWorld.sqrMagnitude < 1e-8f)
                    return Vector2.zero;

                Vector3 worldPt = ct.position + boreFwdWorld.normalized * BoreProjectDist;
                Vector3 vp = cam.WorldToViewportPoint(worldPt);
                if (vp.z <= 0.05f || float.IsNaN(vp.x) || float.IsNaN(vp.y))
                    return Vector2.zero;

                return new Vector2((vp.x - 0.5f) * panelW, (vp.y - 0.5f) * panelH);
            }
            catch
            {
                return Vector2.zero;
            }
        }

        internal static void Reset()
        {
            _yawDeg = 0f;
            _pitchDeg = 0f;
            MissileCameraLookAroundHud.SetVisible(false);
        }

        internal static void Tick()
        {
            if (!MissileCameraFullscreenConfig.Enabled
                || !MissileCameraFullscreenConfig.LookAroundEnabled
                || !MissileCameraFullscreenController.IsActive)
            {
                Reset();
                return;
            }

            if (!Input.GetMouseButton(1))
            {
                if (IsLooking)
                    Reset();
                return;
            }

            if (MissileCameraVisionModeController.IsBlockedByUi())
                return;

            float mx = Input.GetAxisRaw("Mouse X");
            float my = Input.GetAxisRaw("Mouse Y");
            if (mx * mx + my * my >= Deadzone * Deadzone)
            {
                float sens = DegPerUnit;
                _yawDeg += mx * sens;
                // Mouse Y up → look up (Unity +right pitch is nose-down).
                _pitchDeg -= my * sens;
                ClampCone();
            }

            MissileCameraLookAroundHud.SetVisible(true);
        }

        internal static void ApplyToCamera(Camera? camera)
        {
            if (!IsLooking || camera == null)
                return;

            try
            {
                Transform t = camera.transform;
                t.localRotation *= BuildLookOffsetLocal();
            }
            catch
            {
                // ignore
            }
        }

        private static Quaternion BuildLookOffsetLocal()
        {
            return Quaternion.AngleAxis(_yawDeg, Vector3.up)
                * Quaternion.AngleAxis(_pitchDeg, Vector3.right);
        }

        private static void ClampCone()
        {
            float max = Mathf.Max(10f, MissileCameraFullscreenConfig.LookAroundMaxDeg);
            Vector2 v = new Vector2(_yawDeg, _pitchDeg);
            float mag = v.magnitude;
            if (mag > max && mag > 1e-4f)
            {
                v *= max / mag;
                _yawDeg = v.x;
                _pitchDeg = v.y;
            }
        }
    }
}
