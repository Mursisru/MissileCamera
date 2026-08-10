using MissileCamera.Config;
using UnityEngine;

namespace MissileCamera
{
    internal static class MissileCameraFullscreenConfig
    {
        private static readonly Color DefaultPitchLadderTint = new Color(0.55f, 1f, 0.9f, 1f);

        internal static Color DefaultPitchLadderTintValue => DefaultPitchLadderTint;

        // Hardcoded FS feel / ladder look (not player-facing).
        internal const float BootstrapSeconds = 0.6f;
        internal const int BootstrapSteps = 4;
        internal const float ZoomWheelFactor = 1.12f;
        internal const float PitchLadderIntensity = 3.2f;
        internal static readonly Color PitchLadderTint = DefaultPitchLadderTint;

        internal static bool Enabled = true;
        internal static int FeedWidth = 1920;
        internal static int FeedHeight = 1080;
        internal static float ZoomMax = 50f;
        internal static bool ZoomResetOnExit = true;
        internal static bool PitchLadderEnabled = true;
        internal static bool LookAroundEnabled = true;
        internal static float LookAroundMaxDeg = 70f;
        internal static int Revision;

        internal static void Refresh(bool force = false)
        {
            if (!MissileCameraBepInConfig.IsBound)
                return;

            bool enabled = MissileCameraBepInConfig.FullscreenEnabled.Value;
            int feedWidth = MissileCameraBepInConfig.FullscreenFeedWidth.Value;
            int feedHeight = MissileCameraBepInConfig.FullscreenFeedHeight.Value;
            float zoomMax = MissileCameraBepInConfig.FullscreenZoomMax.Value;
            bool zoomResetOnExit = MissileCameraBepInConfig.FullscreenZoomResetOnExit.Value;
            bool pitchLadderEnabled = MissileCameraBepInConfig.FullscreenPitchLadderEnabled.Value;
            bool lookAroundEnabled = MissileCameraBepInConfig.FullscreenLookAroundEnabled.Value;
            float lookAroundMaxDeg = MissileCameraBepInConfig.FullscreenLookAroundMaxDeg.Value;

            if (!force
                && enabled == Enabled
                && feedWidth == FeedWidth
                && feedHeight == FeedHeight
                && zoomMax == ZoomMax
                && zoomResetOnExit == ZoomResetOnExit
                && pitchLadderEnabled == PitchLadderEnabled
                && lookAroundEnabled == LookAroundEnabled
                && Mathf.Approximately(lookAroundMaxDeg, LookAroundMaxDeg))
                return;

            Enabled = enabled;
            FeedWidth = feedWidth;
            FeedHeight = feedHeight;
            ZoomMax = Mathf.Clamp(zoomMax, 2f, 50f);
            ZoomResetOnExit = zoomResetOnExit;
            PitchLadderEnabled = pitchLadderEnabled;
            LookAroundEnabled = lookAroundEnabled;
            LookAroundMaxDeg = Mathf.Clamp(lookAroundMaxDeg, 15f, 89f);
            Revision++;
        }
    }
}
