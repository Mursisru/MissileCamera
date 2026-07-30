using MissileCamera.Config;
using UnityEngine;

namespace MissileCamera
{
    internal enum MissileCameraAircraftCamMode : byte
    {
        Rear = 0,
        TopDown = 1,
        Chase = 2
    }

    internal static class MissileCameraAircraftCamConfig
    {
        // Hardcoded mini-cam size/placement (not player-facing).
        internal const int RenderFps = 10;
        internal const int Width = 256;
        internal const int Height = 256;
        internal const float AnchorMinX = 0.72f;
        internal const float AnchorMinY = 0.72f;
        internal const float AnchorMaxX = 0.98f;
        internal const float AnchorMaxY = 0.98f;

        internal static bool Enabled;
        internal static MissileCameraAircraftCamMode Mode = MissileCameraAircraftCamMode.Rear;
        internal static bool HideInFullscreen;
        internal static int Revision;

        internal static void Refresh(bool force = false)
        {
            if (!MissileCameraBepInConfig.IsBound)
                return;

            bool enabled = MissileCameraBepInConfig.AircraftCamEnabled.Value;
            MissileCameraAircraftCamMode mode = ParseMode(MissileCameraBepInConfig.AircraftCamMode.Value);
            bool hideInFullscreen = MissileCameraBepInConfig.AircraftCamHideInFullscreen.Value;

            if (!force
                && enabled == Enabled
                && mode == Mode
                && hideInFullscreen == HideInFullscreen)
                return;

            Enabled = enabled;
            Mode = mode;
            HideInFullscreen = hideInFullscreen;
            Revision++;
        }

        internal static void CycleMode()
        {
            Mode = (MissileCameraAircraftCamMode)(((int)Mode + 1) % 3);
        }

        private static MissileCameraAircraftCamMode ParseMode(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return MissileCameraAircraftCamMode.Rear;

            if (System.Enum.TryParse(raw, ignoreCase: true, out MissileCameraAircraftCamMode mode))
                return mode;

            return MissileCameraAircraftCamMode.Rear;
        }
    }
}
