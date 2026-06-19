using MissileCamera.Config;

namespace MissileCamera
{
    internal static class MissileCameraControlsConfig
    {
        internal static bool Enabled = true;
        internal static float ZoomStep = 0.5f;
        internal static float ZoomMin = -4f;
        internal static float ZoomMax = 4f;
        internal static float ZoomFovDegreesPerUnit = 5f;
        internal static float IndicatorSeconds = 0.5f;
        internal static int Revision;

        internal static void Refresh(bool force = false)
        {
            if (!MissileCameraBepInConfig.IsBound)
                return;

            bool enabled = MissileCameraBepInConfig.ControlsEnabled.Value;
            float zoomStep = MissileCameraBepInConfig.ZoomStep.Value;
            float zoomMin = MissileCameraBepInConfig.ZoomMin.Value;
            float zoomMax = MissileCameraBepInConfig.ZoomMax.Value;
            float zoomFovDegreesPerUnit = MissileCameraBepInConfig.ZoomFovDegreesPerUnit.Value;
            float indicatorSeconds = MissileCameraBepInConfig.IndicatorSeconds.Value;

            if (!force
                && enabled == Enabled
                && zoomStep == ZoomStep
                && zoomMin == ZoomMin
                && zoomMax == ZoomMax
                && zoomFovDegreesPerUnit == ZoomFovDegreesPerUnit
                && indicatorSeconds == IndicatorSeconds)
                return;

            Enabled = enabled;
            ZoomStep = zoomStep;
            ZoomMin = zoomMin;
            ZoomMax = zoomMax;
            ZoomFovDegreesPerUnit = zoomFovDegreesPerUnit;
            IndicatorSeconds = indicatorSeconds;
            Revision++;
        }

        internal static float ClampZoomOffset(float offset) =>
            offset < ZoomMin ? ZoomMin : offset > ZoomMax ? ZoomMax : offset;

        internal static float ComputeEffectiveFov(float baseFov, float zoomOffset)
        {
            float fov = baseFov - zoomOffset * ZoomFovDegreesPerUnit;
            return fov < 10f ? 10f : fov > 120f ? 120f : fov;
        }
    }
}
