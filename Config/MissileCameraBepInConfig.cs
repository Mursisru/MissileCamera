using System;
using BepInEx.Configuration;

namespace MissileCamera.Config
{
    /// <summary>
    /// Player-facing BepInEx settings (com.at747.missilecamera.bepinex.cfg).
    /// Tuning junk lives as hardcoded defaults in *Config classes — not exposed here.
    /// </summary>
    internal static class MissileCameraBepInConfig
    {
        internal static bool IsBound { get; private set; }

        // Layout
        internal static ConfigEntry<bool> LayoutEnabled { get; private set; } = null!;
        internal static ConfigEntry<string> DisplayMode { get; private set; } = null!;

        // Feed
        internal static ConfigEntry<bool> FeedEnabled { get; private set; } = null!;
        internal static ConfigEntry<float> Fov { get; private set; } = null!;
        internal static ConfigEntry<int> FeedWidth { get; private set; } = null!;
        internal static ConfigEntry<int> FeedHeight { get; private set; } = null!;
        internal static ConfigEntry<float> PostLossInterferenceSeconds { get; private set; } = null!;
        internal static ConfigEntry<int> RenderFps { get; private set; } = null!;
        internal static ConfigEntry<bool> InfraredAutoEnabled { get; private set; } = null!;
        internal static ConfigEntry<float> InfraredDaylightThreshold { get; private set; } = null!;
        internal static ConfigEntry<float> InfraredAmbientThreshold { get; private set; } = null!;
        internal static ConfigEntry<float> InfraredLightHysteresis { get; private set; } = null!;

        // HUD
        internal static ConfigEntry<bool> HudEnabled { get; private set; } = null!;
        internal static ConfigEntry<bool> ShowCenterCluster { get; private set; } = null!;
        internal static ConfigEntry<bool> ShowTargetMarker { get; private set; } = null!;
        internal static ConfigEntry<bool> HudCockpitPipEnabled { get; private set; } = null!;

        // Controls + keybinds
        internal static ConfigEntry<bool> ControlsEnabled { get; private set; } = null!;
        internal static ConfigEntry<KeyboardShortcut> ControlsNextMissile { get; private set; } = null!;
        internal static ConfigEntry<KeyboardShortcut> ControlsPreviousMissile { get; private set; } = null!;
        internal static ConfigEntry<KeyboardShortcut> ControlsZoomIn { get; private set; } = null!;
        internal static ConfigEntry<KeyboardShortcut> ControlsZoomOut { get; private set; } = null!;
        internal static ConfigEntry<KeyboardShortcut> ControlsResetZoom { get; private set; } = null!;

        // Fullscreen
        internal static ConfigEntry<bool> FullscreenEnabled { get; private set; } = null!;
        internal static ConfigEntry<KeyboardShortcut> FullscreenToggle { get; private set; } = null!;
        internal static ConfigEntry<KeyboardShortcut> FullscreenVisionCycle { get; private set; } = null!;
        internal static ConfigEntry<KeyboardShortcut> FullscreenZoomResetKey { get; private set; } = null!;
        internal static ConfigEntry<int> FullscreenFeedWidth { get; private set; } = null!;
        internal static ConfigEntry<int> FullscreenFeedHeight { get; private set; } = null!;
        internal static ConfigEntry<float> FullscreenZoomMax { get; private set; } = null!;
        internal static ConfigEntry<bool> FullscreenZoomResetOnExit { get; private set; } = null!;
        internal static ConfigEntry<bool> FullscreenPitchLadderEnabled { get; private set; } = null!;

        // Post-FX toggles (intensities hardcoded)
        internal static ConfigEntry<bool> FxScanlinesEnabled { get; private set; } = null!;
        internal static ConfigEntry<bool> FxMotionBlurEnabled { get; private set; } = null!;
        internal static ConfigEntry<bool> FxChromaticEnabled { get; private set; } = null!;
        internal static ConfigEntry<bool> FxBloomEnabled { get; private set; } = null!;

        // Aircraft mini-cam
        internal static ConfigEntry<bool> AircraftCamEnabled { get; private set; } = null!;
        internal static ConfigEntry<string> AircraftCamMode { get; private set; } = null!;
        internal static ConfigEntry<bool> AircraftCamHideInFullscreen { get; private set; } = null!;
        internal static ConfigEntry<KeyboardShortcut> AircraftCamCycle { get; private set; } = null!;

        private static bool _liveRefreshHooked;

        internal static void Bind(ConfigFile config)
        {
            const string layout = "Layout";
            LayoutEnabled = config.Bind(layout, "Enabled", true,
                "Turns MFD layout splitting on/off. Off = vanilla MFD never touched.");
            DisplayMode = config.Bind(layout, "DisplayMode", "auto",
                new ConfigDescription(
                    "Which aircraft get the missile panel: auto = per airframe (recommended), skip = never, split = force on all.",
                    new AcceptableValueList<string>("auto", "skip", "split")));

            const string feed = "MissileCameraFeed";
            FeedEnabled = config.Bind(feed, "Enabled", true,
                "Turns the live missile nose camera on the MFD on/off.");
            Fov = config.Bind(feed, "Fov", 60f,
                new ConfigDescription("Seeker base FOV in degrees (before MFD zoom offset).", new AcceptableValueRange<float>(10f, 120f)));
            FeedWidth = config.Bind(feed, "FeedWidth", 512,
                new ConfigDescription("MFD feed render texture width (pixels).", new AcceptableValueRange<int>(128, 2048)));
            FeedHeight = config.Bind(feed, "FeedHeight", 512,
                new ConfigDescription("MFD feed render texture height (pixels).", new AcceptableValueRange<int>(128, 2048)));
            PostLossInterferenceSeconds = config.Bind(feed, "PostLossInterferenceSeconds", 0.5f,
                new ConfigDescription(
                    "NO SIGNAL flash length (seconds) on missile switch/destroy/FS exit with no missiles. 0 = off.",
                    new AcceptableValueRange<float>(0f, 2f)));
            RenderFps = config.Bind(feed, "RenderFps", 30,
                new ConfigDescription("MFD feed render rate (Hz). Fullscreen video stays every frame.", new AcceptableValueRange<int>(5, 60)));
            InfraredAutoEnabled = config.Bind(feed, "InfraredAutoEnabled", true,
                "Auto B/W IR on MFD when lighting at the missile is dark (not a clock).");
            InfraredDaylightThreshold = config.Bind(feed, "InfraredDaylightThreshold", 0.12f,
                new ConfigDescription(
                    "Auto IR ON when daylight factor at missile is below this. Lower = IR less often.",
                    new AcceptableValueRange<float>(0.01f, 1f)));
            InfraredAmbientThreshold = config.Bind(feed, "InfraredAmbientThreshold", 0.06f,
                new ConfigDescription(
                    "Auto IR ON when ambient light is below this. Lower = IR less often.",
                    new AcceptableValueRange<float>(0.01f, 1f)));
            InfraredLightHysteresis = config.Bind(feed, "InfraredLightHysteresis", 0.03f,
                new ConfigDescription(
                    "Extra light margin before auto IR turns off (reduces flicker).",
                    new AcceptableValueRange<float>(0f, 0.2f)));

            const string hud = "MissileCameraHud";
            HudEnabled = config.Bind(hud, "Enabled", true,
                "Turns the MFD HUD overlay (corners / FLIR chrome) on/off.");
            ShowCenterCluster = config.Bind(hud, "ShowCenterCluster", true,
                "Shows MFD center reticle and hollow intercept ring at aim point.");
            ShowTargetMarker = config.Bind(hud, "ShowTargetMarker", true,
                "Shows MFD target diamond marker.");
            HudCockpitPipEnabled = config.Bind(hud, "CockpitPipEnabled", true,
                "Shows fullscreen FLIR ownship nose PiP (bottom-left). MFD classic has no separate cockpit PiP.");

            const string controls = "MissileCameraControls";
            ControlsEnabled = config.Bind(controls, "Enabled", true,
                "Turns keyboard missile cycling and MFD zoom on/off.");
            ControlsNextMissile = config.Bind(controls, "NextMissile",
                MissileCameraKeybindConfig.DefaultNextMissile,
                "Keybind: next owned in-flight missile (MFD or fullscreen). Default: RightAlt + /");
            ControlsPreviousMissile = config.Bind(controls, "PreviousMissile",
                MissileCameraKeybindConfig.DefaultPreviousMissile,
                "Keybind: previous owned in-flight missile. Default: RightAlt + ,");
            ControlsZoomIn = config.Bind(controls, "ZoomIn",
                MissileCameraKeybindConfig.DefaultMfdZoomIn,
                "Keybind: MFD seeker zoom in (narrower FOV). Default: RightAlt + ;");
            ControlsZoomOut = config.Bind(controls, "ZoomOut",
                MissileCameraKeybindConfig.DefaultMfdZoomOut,
                "Keybind: MFD seeker zoom out (wider FOV). Default: RightAlt + .");
            ControlsResetZoom = config.Bind(controls, "ResetZoom",
                MissileCameraKeybindConfig.DefaultMfdZoomReset,
                "Keybind: reset MFD zoom offset to 0. Default: RightShift + .");

            const string fullscreen = "MissileCameraFullscreen";
            FullscreenEnabled = config.Bind(fullscreen, "Enabled", true,
                "Turns fullscreen missile feed (whole viewport) on/off.");
            FullscreenToggle = config.Bind(fullscreen, "Toggle",
                MissileCameraKeybindConfig.DefaultFullscreenToggle,
                "Keybind: enter/exit fullscreen missile camera. Default: K");
            FullscreenVisionCycle = config.Bind(fullscreen, "VisionCycle",
                MissileCameraKeybindConfig.DefaultVisionCycle,
                "Keybind: cycle Color / NVG / WhiteHot / BlackHot / Contour. Default: J");
            FullscreenZoomResetKey = config.Bind(fullscreen, "ZoomResetKey",
                MissileCameraKeybindConfig.DefaultFullscreenZoomReset,
                "Keybind: reset fullscreen optical zoom to 1x. Default: Middle Mouse");
            FullscreenFeedWidth = config.Bind(fullscreen, "FeedWidth", 1920,
                new ConfigDescription(
                    "Fullscreen feed render texture width (independent of MFD).",
                    new AcceptableValueRange<int>(640, 3840)));
            FullscreenFeedHeight = config.Bind(fullscreen, "FeedHeight", 1080,
                new ConfigDescription(
                    "Fullscreen feed render texture height (independent of MFD).",
                    new AcceptableValueRange<int>(360, 2160)));
            FullscreenZoomMax = config.Bind(fullscreen, "ZoomMax", 50f,
                new ConfigDescription("Max fullscreen optical magnification (mouse wheel).", new AcceptableValueRange<float>(2f, 50f)));
            FullscreenZoomResetOnExit = config.Bind(fullscreen, "ZoomResetOnExit", true,
                "Reset magnification to 1x when leaving fullscreen.");
            FullscreenPitchLadderEnabled = config.Bind(fullscreen, "PitchLadderEnabled", true,
                "Shows stock FlightHud pitch ladder on fullscreen FLIR.");

            const string fx = "MissileCameraEffects";
            FxScanlinesEnabled = config.Bind(fx, "ScanlinesEnabled", true,
                "TV scanline overlay on the missile feed (above IR, below HUD). Default on.");
            FxMotionBlurEnabled = config.Bind(fx, "MotionBlurEnabled", false,
                "MFD post-FX: motion blur (needs shader bundle).");
            FxChromaticEnabled = config.Bind(fx, "ChromaticEnabled", false,
                "MFD post-FX: chromatic aberration (needs shader bundle).");
            FxBloomEnabled = config.Bind(fx, "BloomEnabled", false,
                "MFD post-FX: bloom (needs shader bundle).");

            const string aircraftCam = "MissileCameraAircraftCam";
            AircraftCamEnabled = config.Bind(aircraftCam, "Enabled", false,
                "Turns aircraft mini-cam overlay on/off (no-op when DisplayMode=skip).");
            AircraftCamMode = config.Bind(aircraftCam, "Mode", "Rear",
                new ConfigDescription("Mini-cam view: Rear / TopDown / Chase.", new AcceptableValueList<string>("Rear", "TopDown", "Chase")));
            AircraftCamHideInFullscreen = config.Bind(aircraftCam, "HideInFullscreen", false,
                "Hide mini-cam while fullscreen missile feed is active.");
            AircraftCamCycle = config.Bind(aircraftCam, "CycleMode",
                MissileCameraKeybindConfig.DefaultAircraftCamCycle,
                "Keybind: cycle mini-cam mode. Default: RightAlt + V");

            IsBound = true;
            HookLiveRefresh(config);
        }

        /// <summary>Apply cfg changes immediately (Configuration Manager) — not only on 1s Tick poll.</summary>
        private static void HookLiveRefresh(ConfigFile config)
        {
            if (_liveRefreshHooked)
                return;

            _liveRefreshHooked = true;
            config.SettingChanged += (_, __) =>
            {
                try
                {
                    MissileCameraLayoutConfigRefresh();
                }
                catch
                {
                    // ignore mid-teardown
                }
            };
        }

        private static void MissileCameraLayoutConfigRefresh()
        {
            MfdLayoutConfig.Refresh(force: true);
            MissileCameraFeedConfig.Refresh(force: true);
            MissileCameraHudConfig.Refresh(force: true);
            MissileCameraControlsConfig.Refresh(force: true);
            MissileCameraKeybindConfig.Refresh(force: true);
            MissileCameraFullscreenConfig.Refresh(force: true);
            MissileCameraTelemetryConfig.Refresh(force: true);
            MissileCameraEffectsConfig.Refresh(force: true);
            MissileCameraAircraftCamConfig.Refresh(force: true);
        }
    }
}
