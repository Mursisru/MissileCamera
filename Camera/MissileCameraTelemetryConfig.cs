using MissileCamera.Config;

namespace MissileCamera
{
    internal static class MissileCameraTelemetryConfig
    {
        // Hardcoded telemetry flags / rate (not player-facing).
        internal const bool ShowG = false;
        internal const bool ShowFuel = false;
        internal const bool ShowGuidance = false;
        internal const bool ShowMach = false;
        internal const bool ShowTargetRange = true;
        internal const bool ShowTargetAngle = false;
        internal const float SmoothHz = 10f;

        internal static int Revision;

        internal static void Refresh(bool force = false)
        {
            // No cfg surface — keep API for Host/FeedController refresh cascade.
            if (force)
                Revision++;
        }
    }
}
