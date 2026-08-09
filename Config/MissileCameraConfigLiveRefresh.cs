using BepInEx.Configuration;

namespace MissileCamera.Config
{
    /// <summary>
    /// Live cfg refresh via BepInEx SettingChanged — no sortie restart required.
    /// </summary>
    internal static class MissileCameraConfigLiveRefresh
    {
        internal static void Subscribe(ConfigFile config)
        {
            config.SettingChanged += OnSettingChanged;
        }

        private static void OnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            RefreshAll(force: true);
            ApplyRuntimeReactions();
        }

        internal static void RefreshAll(bool force = false)
        {
            MissileCameraKeybindConfig.Refresh(force);
            MfdLayoutConfig.Refresh(force);
            MissileCameraFeedConfig.Refresh(force);
            MissileCameraHudConfig.Refresh(force);
            MissileCameraControlsConfig.Refresh(force);
            MissileCameraFullscreenConfig.Refresh(force);
            MissileCameraTelemetryConfig.Refresh(force);
            MissileCameraEffectsConfig.Refresh(force);
            MissileCameraAircraftCamConfig.Refresh(force);
        }

        private static void ApplyRuntimeReactions()
        {
            if (!MissileCameraHost.IsSessionActive)
                return;

            if (!MissileCameraFeedConfig.Enabled || !MfdLayoutConfig.Enabled)
            {
                MfdLayoutController.ReleaseFully(
                    !MissileCameraFeedConfig.Enabled ? "feed_disabled_cfg" : "layout_disabled_cfg");
                return;
            }

            if (MissileCameraFeedController.HasTrackableOwnedMissile())
                MfdLayoutController.EnsureLayoutForMissileFeed();
        }
    }
}
