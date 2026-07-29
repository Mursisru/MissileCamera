using System.Globalization;
using MissileCamera.Config;
using UnityEngine;

namespace MissileCamera
{
    internal static class MissileCameraFullscreenConfig
    {
        private static readonly Color DefaultPitchLadderTint = new Color(0.55f, 1f, 0.9f, 1f);

        internal static Color DefaultPitchLadderTintValue => DefaultPitchLadderTint;

        internal static bool Enabled = true;
        internal static KeyCode ToggleKey = KeyCode.F;
        internal static bool RequireRightAlt = true;
        internal static float BootstrapSeconds = 0.6f;
        internal static int BootstrapSteps = 4;
        internal static int FeedWidth = 1920;
        internal static int FeedHeight = 1080;
        internal static float ZoomMax = 50f;
        internal static float ZoomWheelFactor = 1.12f;
        internal static KeyCode VisionCycleKey = KeyCode.J;
        internal static bool ZoomResetOnExit = true;
        internal static bool PitchLadderEnabled = true;
        internal static Color PitchLadderTint = DefaultPitchLadderTint;
        internal static float PitchLadderIntensity = 3.2f;
        internal static int Revision;

        internal static void Refresh(bool force = false)
        {
            if (!MissileCameraBepInConfig.IsBound)
                return;

            bool enabled = MissileCameraBepInConfig.FullscreenEnabled.Value;
            KeyCode toggleKey = ParseKey(MissileCameraBepInConfig.FullscreenToggleKey.Value, KeyCode.F);
            bool requireRightAlt = MissileCameraBepInConfig.FullscreenRequireRightAlt.Value;
            float bootstrapSeconds = MissileCameraBepInConfig.FullscreenBootstrapSeconds.Value;
            int bootstrapSteps = MissileCameraBepInConfig.FullscreenBootstrapSteps.Value;
            int feedWidth = MissileCameraBepInConfig.FullscreenFeedWidth.Value;
            int feedHeight = MissileCameraBepInConfig.FullscreenFeedHeight.Value;
            float zoomMax = MissileCameraBepInConfig.FullscreenZoomMax.Value;
            float zoomWheelFactor = MissileCameraBepInConfig.FullscreenZoomWheelFactor.Value;
            KeyCode visionCycleKey = ParseKey(MissileCameraBepInConfig.FullscreenVisionCycleKey.Value, KeyCode.J);
            bool zoomResetOnExit = MissileCameraBepInConfig.FullscreenZoomResetOnExit.Value;
            bool pitchLadderEnabled = MissileCameraBepInConfig.FullscreenPitchLadderEnabled.Value;
            Color pitchLadderTint = ParseColor(MissileCameraBepInConfig.FullscreenPitchLadderTint.Value, DefaultPitchLadderTint);
            float pitchLadderIntensity = MissileCameraBepInConfig.FullscreenPitchLadderIntensity.Value;

            if (!force
                && enabled == Enabled
                && toggleKey == ToggleKey
                && requireRightAlt == RequireRightAlt
                && bootstrapSeconds == BootstrapSeconds
                && bootstrapSteps == BootstrapSteps
                && feedWidth == FeedWidth
                && feedHeight == FeedHeight
                && zoomMax == ZoomMax
                && zoomWheelFactor == ZoomWheelFactor
                && visionCycleKey == VisionCycleKey
                && zoomResetOnExit == ZoomResetOnExit
                && pitchLadderEnabled == PitchLadderEnabled
                && pitchLadderTint == PitchLadderTint
                && Mathf.Approximately(pitchLadderIntensity, PitchLadderIntensity))
                return;

            Enabled = enabled;
            ToggleKey = toggleKey;
            RequireRightAlt = requireRightAlt;
            BootstrapSeconds = bootstrapSeconds;
            BootstrapSteps = bootstrapSteps;
            FeedWidth = feedWidth;
            FeedHeight = feedHeight;
            ZoomMax = Mathf.Clamp(zoomMax, 2f, 50f);
            ZoomWheelFactor = Mathf.Clamp(zoomWheelFactor, 1.02f, 1.5f);
            VisionCycleKey = visionCycleKey;
            ZoomResetOnExit = zoomResetOnExit;
            PitchLadderEnabled = pitchLadderEnabled;
            PitchLadderTint = pitchLadderTint;
            PitchLadderIntensity = Mathf.Clamp(pitchLadderIntensity, 1f, 4f);
            Revision++;
        }

        private static Color ParseColor(string raw, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return fallback;

            string[] parts = raw.Split(',');
            if (parts.Length < 3)
                return fallback;

            if (!float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float r)
                || !float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float g)
                || !float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float b))
            {
                return fallback;
            }

            float a = parts.Length > 3
                && float.TryParse(parts[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedA)
                ? parsedA
                : 1f;

            return new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), Mathf.Clamp01(a));
        }

        private static KeyCode ParseKey(string raw, KeyCode fallback)
        {
            if (string.IsNullOrEmpty(raw))
                return fallback;

            return System.Enum.TryParse(raw, ignoreCase: true, out KeyCode key) ? key : fallback;
        }
    }
}
