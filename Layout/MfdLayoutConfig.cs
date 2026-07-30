using MissileCamera.Config;

namespace MissileCamera
{
    internal static class MfdLayoutConfig
    {
        // Hardcoded layout geometry (not player-facing — airframe discovery owns real zones).
        internal const float OverlayMaxWidth = 0.45f;
        internal const float LeftWidth = 0.58f;
        internal const float MissilePanelBottom = 0.38f;
        internal const float WeaponsStripHeight = 0.12f;
        internal const bool ShowDivider = true;
        // Dev switch — flip in code only (not in cfg). Kept non-const so stub branches stay reachable for compilers.
        internal static bool DebugStub = false;
        internal const string StubLabel = "MISSILE CAMERA";

        internal static bool Enabled = true;
        internal static string DisplayMode = "split";
        internal static int Revision;

        internal static void Init(string modRoot)
        {
            ModPaths.Init(modRoot);
            Refresh(force: true);
        }

        internal static void EnsureInitialized()
        {
            if (MissileCameraBepInConfig.IsBound)
                return;

            if (!string.IsNullOrEmpty(ModPaths.PluginDir))
                Init(ModPaths.PluginDir);
        }

        internal static void Refresh(bool force = false)
        {
            if (!MissileCameraBepInConfig.IsBound)
                return;

            bool enabled = MissileCameraBepInConfig.LayoutEnabled.Value;
            string displayMode = MissileCameraBepInConfig.DisplayMode.Value;

            if (!force && enabled == Enabled && displayMode == DisplayMode)
                return;

            Enabled = enabled;
            DisplayMode = displayMode;
            Revision++;
        }
    }
}
