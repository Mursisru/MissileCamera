using MissileCamera.Config;

namespace MissileCamera
{
    internal static class MfdLayoutConfig
    {
        internal static bool Enabled = true;
        internal static string DisplayMode = "split";
        internal static float OverlayMaxWidth = 0.45f;
        internal static float LeftWidth = 0.58f;
        internal static float MissilePanelBottom = 0.38f;
        internal static float WeaponsStripHeight = 0.12f;
        internal static bool ShowDivider = true;
        internal static bool DebugStub;
        internal static string StubLabel = "MISSILE CAMERA";
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
            float overlayMaxWidth = MissileCameraBepInConfig.OverlayMaxWidth.Value;
            float leftWidth = MissileCameraBepInConfig.LeftWidth.Value;
            float missilePanelBottom = MissileCameraBepInConfig.MissilePanelBottom.Value;
            float weaponsStripHeight = MissileCameraBepInConfig.WeaponsStripHeight.Value;
            bool showDivider = MissileCameraBepInConfig.ShowDivider.Value;
            bool debugStub = MissileCameraBepInConfig.DebugStub.Value;
            string stubLabel = MissileCameraBepInConfig.StubLabel.Value;

            if (!force
                && enabled == Enabled
                && displayMode == DisplayMode
                && overlayMaxWidth == OverlayMaxWidth
                && leftWidth == LeftWidth
                && missilePanelBottom == MissilePanelBottom
                && weaponsStripHeight == WeaponsStripHeight
                && showDivider == ShowDivider
                && debugStub == DebugStub
                && stubLabel == StubLabel)
                return;

            Enabled = enabled;
            DisplayMode = displayMode;
            OverlayMaxWidth = overlayMaxWidth;
            LeftWidth = leftWidth;
            MissilePanelBottom = missilePanelBottom;
            WeaponsStripHeight = weaponsStripHeight;
            ShowDivider = showDivider;
            DebugStub = debugStub;
            StubLabel = stubLabel;
            Revision++;
        }
    }
}
