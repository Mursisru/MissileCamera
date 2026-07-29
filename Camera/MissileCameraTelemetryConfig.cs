using MissileCamera.Config;

namespace MissileCamera
{
    internal static class MissileCameraTelemetryConfig
    {
        internal const bool ShowG = false;
        internal const bool ShowFuel = false;
        internal const bool ShowGuidance = false;
        internal const bool ShowMach = false;
        internal const bool ShowTargetRange = true;
        internal const bool ShowTargetAngle = false;

        internal static float SmoothHz = 10f;
        internal static int Revision;

        internal static void Refresh(bool force = false)
        {
            if (!MissileCameraBepInConfig.IsBound)
                return;

            float smoothHz = MissileCameraBepInConfig.TelemetrySmoothHz.Value;

            if (!force && smoothHz == SmoothHz)
                return;

            SmoothHz = smoothHz;
            Revision++;
        }
    }
}
