using MissileCamera.Config;
using UnityEngine;

namespace MissileCamera
{
    internal static class MissileCameraControlsConfig
    {
        private const float MfdMinFov = 10f;
        private const float MfdMaxFov = 120f;

        // Hardcoded MFD zoom feel (not player-facing — change keybinds instead).
        internal const float ZoomStep = 0.5f;
        internal const float ZoomMin = -4f;
        internal const float ZoomMax = 4f;
        internal const float ZoomFovDegreesPerUnit = 5f;
        internal const float IndicatorSeconds = 0.5f;

        internal static bool Enabled = true;
        internal static int Revision;

        internal static void Refresh(bool force = false)
        {
            if (!MissileCameraBepInConfig.IsBound)
                return;

            bool enabled = MissileCameraBepInConfig.ControlsEnabled.Value;
            if (!force && enabled == Enabled)
                return;

            Enabled = enabled;
            Revision++;
        }

        internal static float ClampZoomOffset(float offset) =>
            offset < ZoomMin ? ZoomMin : offset > ZoomMax ? ZoomMax : offset;

        internal static float ComputeEffectiveFov(float baseFov, float zoomOffset)
        {
            float fov = baseFov - zoomOffset * ZoomFovDegreesPerUnit;
            return fov < MfdMinFov ? MfdMinFov : fov > MfdMaxFov ? MfdMaxFov : fov;
        }

        /// <summary>Fullscreen optical zoom: fov = baseFov / mag, mag in [1, ZoomMax].</summary>
        internal static float ComputeFullscreenFov(float baseFov, float magnification)
        {
            float maxMag = Mathf.Max(MissileCameraFullscreenConfig.ZoomMax, 1f);
            float mag = Mathf.Clamp(magnification, 1f, maxMag);
            float safeBase = Mathf.Max(baseFov, 1f);
            float minFov = safeBase / maxMag;
            float maxFov = Mathf.Min(MfdMaxFov, safeBase);
            float fov = safeBase / mag;
            return Mathf.Clamp(fov, minFov, maxFov);
        }

        internal static float ClampFullscreenMagnification(float magnification)
        {
            float maxMag = Mathf.Max(MissileCameraFullscreenConfig.ZoomMax, 1f);
            return Mathf.Clamp(magnification, 1f, maxMag);
        }
    }
}
