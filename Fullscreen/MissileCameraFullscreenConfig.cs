using MissileCamera.Config;
using UnityEngine;

namespace MissileCamera
{
    internal static class MissileCameraFullscreenConfig
    {
        internal static bool Enabled = true;
        internal static KeyCode ToggleKey = KeyCode.F;
        internal static bool RequireRightAlt = true;
        internal static float BootstrapSeconds = 0.6f;
        internal static int BootstrapSteps = 4;
        internal static int FeedWidth = 1920;
        internal static int FeedHeight = 1080;
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

            if (!force
                && enabled == Enabled
                && toggleKey == ToggleKey
                && requireRightAlt == RequireRightAlt
                && bootstrapSeconds == BootstrapSeconds
                && bootstrapSteps == BootstrapSteps
                && feedWidth == FeedWidth
                && feedHeight == FeedHeight)
                return;

            Enabled = enabled;
            ToggleKey = toggleKey;
            RequireRightAlt = requireRightAlt;
            BootstrapSeconds = bootstrapSeconds;
            BootstrapSteps = bootstrapSteps;
            FeedWidth = feedWidth;
            FeedHeight = feedHeight;
            Revision++;
        }

        private static KeyCode ParseKey(string raw, KeyCode fallback)
        {
            if (string.IsNullOrEmpty(raw))
                return fallback;

            return System.Enum.TryParse(raw, ignoreCase: true, out KeyCode key) ? key : fallback;
        }
    }
}
