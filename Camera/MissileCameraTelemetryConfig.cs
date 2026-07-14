using MissileCamera.Config;

namespace MissileCamera
{
    internal static class MissileCameraTelemetryConfig
    {
        internal static bool ShowG;
        internal static bool ShowFuel;
        internal static bool ShowGuidance;
        internal static bool ShowMach;
        internal static bool ShowTargetRange = true;
        internal static bool ShowTargetAngle;
        internal static float SmoothHz = 12f;
        internal static int Revision;

        internal static void Refresh(bool force = false)
        {
            if (!MissileCameraBepInConfig.IsBound)
                return;

            bool showG = MissileCameraBepInConfig.TelemetryShowG.Value;
            bool showFuel = MissileCameraBepInConfig.TelemetryShowFuel.Value;
            bool showGuidance = MissileCameraBepInConfig.TelemetryShowGuidance.Value;
            bool showMach = MissileCameraBepInConfig.TelemetryShowMach.Value;
            bool showTargetRange = MissileCameraBepInConfig.TelemetryShowTargetRange.Value;
            bool showTargetAngle = MissileCameraBepInConfig.TelemetryShowTargetAngle.Value;
            float smoothHz = MissileCameraBepInConfig.TelemetrySmoothHz.Value;

            if (!force
                && showG == ShowG
                && showFuel == ShowFuel
                && showGuidance == ShowGuidance
                && showMach == ShowMach
                && showTargetRange == ShowTargetRange
                && showTargetAngle == ShowTargetAngle
                && smoothHz == SmoothHz)
                return;

            ShowG = showG;
            ShowFuel = showFuel;
            ShowGuidance = showGuidance;
            ShowMach = showMach;
            ShowTargetRange = showTargetRange;
            ShowTargetAngle = showTargetAngle;
            SmoothHz = smoothHz;
            Revision++;
        }
    }
}
