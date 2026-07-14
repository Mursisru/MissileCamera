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
        internal static bool Enabled;
        internal static MissileCameraAircraftCamMode Mode = MissileCameraAircraftCamMode.Rear;
        internal static int RenderFps = 15;
        internal static int Width = 256;
        internal static int Height = 256;
        internal static float AnchorMinX = 0.72f;
        internal static float AnchorMinY = 0.72f;
        internal static float AnchorMaxX = 0.98f;
        internal static float AnchorMaxY = 0.98f;
        internal static bool HideInFullscreen;
        internal static KeyCode CycleKey = KeyCode.V;
        internal static bool RequireRightAlt = true;
        internal static int Revision;

        internal static void Refresh(bool force = false)
        {
            if (!MissileCameraBepInConfig.IsBound)
                return;

            bool enabled = MissileCameraBepInConfig.AircraftCamEnabled.Value;
            MissileCameraAircraftCamMode mode = ParseMode(MissileCameraBepInConfig.AircraftCamMode.Value);
            int fps = MissileCameraBepInConfig.AircraftCamFps.Value;
            int width = MissileCameraBepInConfig.AircraftCamWidth.Value;
            int height = MissileCameraBepInConfig.AircraftCamHeight.Value;
            float minX = MissileCameraBepInConfig.AircraftCamAnchorMinX.Value;
            float minY = MissileCameraBepInConfig.AircraftCamAnchorMinY.Value;
            float maxX = MissileCameraBepInConfig.AircraftCamAnchorMaxX.Value;
            float maxY = MissileCameraBepInConfig.AircraftCamAnchorMaxY.Value;
            bool hideInFullscreen = MissileCameraBepInConfig.AircraftCamHideInFullscreen.Value;
            KeyCode cycleKey = ParseKey(MissileCameraBepInConfig.AircraftCamCycleKey.Value, KeyCode.V);
            bool requireRightAlt = MissileCameraBepInConfig.AircraftCamRequireRightAlt.Value;

            if (!force
                && enabled == Enabled
                && mode == Mode
                && fps == RenderFps
                && width == Width
                && height == Height
                && minX == AnchorMinX
                && minY == AnchorMinY
                && maxX == AnchorMaxX
                && maxY == AnchorMaxY
                && hideInFullscreen == HideInFullscreen
                && cycleKey == CycleKey
                && requireRightAlt == RequireRightAlt)
                return;

            Enabled = enabled;
            Mode = mode;
            RenderFps = fps;
            Width = width;
            Height = height;
            AnchorMinX = minX;
            AnchorMinY = minY;
            AnchorMaxX = maxX;
            AnchorMaxY = maxY;
            HideInFullscreen = hideInFullscreen;
            CycleKey = cycleKey;
            RequireRightAlt = requireRightAlt;
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

        private static KeyCode ParseKey(string raw, KeyCode fallback)
        {
            if (string.IsNullOrEmpty(raw))
                return fallback;

            return System.Enum.TryParse(raw, ignoreCase: true, out KeyCode key) ? key : fallback;
        }
    }
}
