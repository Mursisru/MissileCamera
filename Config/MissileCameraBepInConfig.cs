using BepInEx.Configuration;
using UnityEngine;

namespace MissileCamera.Config
{
    /// <summary>BepInEx Configuration Manager bindings (com.at747.missilecamera.bepinex.cfg).</summary>
    internal static class MissileCameraBepInConfig
    {
        internal static bool IsBound { get; private set; }

        internal static ConfigEntry<bool> LayoutEnabled { get; private set; } = null!;
        internal static ConfigEntry<string> DisplayMode { get; private set; } = null!;
        internal static ConfigEntry<float> OverlayMaxWidth { get; private set; } = null!;
        internal static ConfigEntry<float> LeftWidth { get; private set; } = null!;
        internal static ConfigEntry<float> MissilePanelBottom { get; private set; } = null!;
        internal static ConfigEntry<float> WeaponsStripHeight { get; private set; } = null!;
        internal static ConfigEntry<bool> ShowDivider { get; private set; } = null!;
        internal static ConfigEntry<bool> DebugStub { get; private set; } = null!;
        internal static ConfigEntry<string> StubLabel { get; private set; } = null!;

        internal static ConfigEntry<bool> FeedEnabled { get; private set; } = null!;
        internal static ConfigEntry<float> NoseSkinInset { get; private set; } = null!;
        internal static ConfigEntry<float> CameraBackOffset { get; private set; } = null!;
        internal static ConfigEntry<float> Fov { get; private set; } = null!;
        internal static ConfigEntry<int> FeedWidth { get; private set; } = null!;
        internal static ConfigEntry<int> FeedHeight { get; private set; } = null!;
        internal static ConfigEntry<bool> HorizonLock { get; private set; } = null!;
        internal static ConfigEntry<float> TurnLookBankScale { get; private set; } = null!;
        internal static ConfigEntry<float> MaxTurnLookDegrees { get; private set; } = null!;
        internal static ConfigEntry<float> DefaultMissileGLimit { get; private set; } = null!;
        internal static ConfigEntry<float> TurnLookGDeadband { get; private set; } = null!;
        internal static ConfigEntry<float> TurnLookGFilterHz { get; private set; } = null!;
        internal static ConfigEntry<float> TurnLookSlewDegPerSec { get; private set; } = null!;
        internal static ConfigEntry<float> TurnLookSmoothTime { get; private set; } = null!;
        internal static ConfigEntry<float> PostExplosionHoldSeconds { get; private set; } = null!;
        internal static ConfigEntry<float> PostLossInterferenceSeconds { get; private set; } = null!;
        internal static ConfigEntry<int> RenderFps { get; private set; } = null!;
        internal static ConfigEntry<bool> InfraredAutoEnabled { get; private set; } = null!;
        internal static ConfigEntry<float> InfraredDaylightThreshold { get; private set; } = null!;
        internal static ConfigEntry<float> InfraredAmbientThreshold { get; private set; } = null!;
        internal static ConfigEntry<float> InfraredLightHysteresis { get; private set; } = null!;
        internal static ConfigEntry<float> InfraredContrast { get; private set; } = null!;
        internal static ConfigEntry<float> InfraredBlackPoint { get; private set; } = null!;
        internal static ConfigEntry<float> InfraredWhitePoint { get; private set; } = null!;
        internal static ConfigEntry<float> InfraredRedWeight { get; private set; } = null!;
        internal static ConfigEntry<float> InfraredExposureBiasEv { get; private set; } = null!;

        internal static ConfigEntry<bool> HudEnabled { get; private set; } = null!;
        internal static ConfigEntry<float> SalvoWindowSeconds { get; private set; } = null!;
        internal static ConfigEntry<bool> ShowCenterCluster { get; private set; } = null!;
        internal static ConfigEntry<bool> ShowTargetMarker { get; private set; } = null!;
        internal static ConfigEntry<bool> HudCockpitPipEnabled { get; private set; } = null!;
        internal static ConfigEntry<int> HudCockpitPipFps { get; private set; } = null!;
        internal static ConfigEntry<string> InterceptColor { get; private set; } = null!;
        internal static ConfigEntry<string> ReticleColor { get; private set; } = null!;
        internal static ConfigEntry<string> HorizonColor { get; private set; } = null!;
        internal static ConfigEntry<string> HorizonOutlineColor { get; private set; } = null!;
        internal static ConfigEntry<string> MissileNameColor { get; private set; } = null!;
        internal static ConfigEntry<string> OwnshipNameColor { get; private set; } = null!;
        internal static ConfigEntry<string> TargetNameColor { get; private set; } = null!;
        internal static ConfigEntry<string> LabelBackgroundColor { get; private set; } = null!;
        internal static ConfigEntry<float> LabelBackgroundAlpha { get; private set; } = null!;

        internal static ConfigEntry<bool> ControlsEnabled { get; private set; } = null!;
        internal static ConfigEntry<float> ZoomStep { get; private set; } = null!;
        internal static ConfigEntry<float> ZoomMin { get; private set; } = null!;
        internal static ConfigEntry<float> ZoomMax { get; private set; } = null!;
        internal static ConfigEntry<float> ZoomFovDegreesPerUnit { get; private set; } = null!;
        internal static ConfigEntry<float> IndicatorSeconds { get; private set; } = null!;
        internal static ConfigEntry<KeyboardShortcut> ControlsNextMissile { get; private set; } = null!;
        internal static ConfigEntry<KeyboardShortcut> ControlsPreviousMissile { get; private set; } = null!;
        internal static ConfigEntry<KeyboardShortcut> ControlsZoomIn { get; private set; } = null!;
        internal static ConfigEntry<KeyboardShortcut> ControlsZoomOut { get; private set; } = null!;
        internal static ConfigEntry<KeyboardShortcut> ControlsResetZoom { get; private set; } = null!;
        internal static ConfigEntry<KeyboardShortcut> FullscreenZoomResetKey { get; private set; } = null!;

        internal static ConfigEntry<bool> FullscreenEnabled { get; private set; } = null!;
        internal static ConfigEntry<string> FullscreenToggleKey { get; private set; } = null!;
        internal static ConfigEntry<bool> FullscreenRequireRightAlt { get; private set; } = null!;
        internal static ConfigEntry<int> FullscreenFeedWidth { get; private set; } = null!;
        internal static ConfigEntry<int> FullscreenFeedHeight { get; private set; } = null!;
        internal static ConfigEntry<float> FullscreenZoomMax { get; private set; } = null!;
        internal static ConfigEntry<float> FullscreenZoomWheelFactor { get; private set; } = null!;
        internal static ConfigEntry<string> FullscreenVisionCycleKey { get; private set; } = null!;
        internal static ConfigEntry<bool> FullscreenZoomResetOnExit { get; private set; } = null!;
        internal static ConfigEntry<bool> FullscreenPitchLadderEnabled { get; private set; } = null!;
        internal static ConfigEntry<string> FullscreenPitchLadderTint { get; private set; } = null!;
        internal static ConfigEntry<float> FullscreenPitchLadderIntensity { get; private set; } = null!;

        internal static ConfigEntry<float> TelemetrySmoothHz { get; private set; } = null!;

        internal static ConfigEntry<bool> FxScanlinesEnabled { get; private set; } = null!;
        internal static ConfigEntry<float> FxScanlinesIntensity { get; private set; } = null!;
        internal static ConfigEntry<bool> FxMotionBlurEnabled { get; private set; } = null!;
        internal static ConfigEntry<float> FxMotionBlurIntensity { get; private set; } = null!;
        internal static ConfigEntry<bool> FxChromaticEnabled { get; private set; } = null!;
        internal static ConfigEntry<float> FxChromaticIntensity { get; private set; } = null!;
        internal static ConfigEntry<bool> FxBloomEnabled { get; private set; } = null!;
        internal static ConfigEntry<float> FxBloomIntensity { get; private set; } = null!;

        internal static ConfigEntry<bool> AircraftCamEnabled { get; private set; } = null!;
        internal static ConfigEntry<string> AircraftCamMode { get; private set; } = null!;
        internal static ConfigEntry<int> AircraftCamFps { get; private set; } = null!;
        internal static ConfigEntry<int> AircraftCamWidth { get; private set; } = null!;
        internal static ConfigEntry<int> AircraftCamHeight { get; private set; } = null!;
        internal static ConfigEntry<float> AircraftCamAnchorMinX { get; private set; } = null!;
        internal static ConfigEntry<float> AircraftCamAnchorMinY { get; private set; } = null!;
        internal static ConfigEntry<float> AircraftCamAnchorMaxX { get; private set; } = null!;
        internal static ConfigEntry<float> AircraftCamAnchorMaxY { get; private set; } = null!;
        internal static ConfigEntry<bool> AircraftCamHideInFullscreen { get; private set; } = null!;
        internal static ConfigEntry<string> AircraftCamCycleKey { get; private set; } = null!;
        internal static ConfigEntry<bool> AircraftCamRequireRightAlt { get; private set; } = null!;

        internal static void Bind(ConfigFile config)
        {
            const string layout = "Layout";
            LayoutEnabled = config.Bind(layout, "Enabled", true,
                "Turns MFD layout splitting on/off. When off, vanilla MFD is never modified.");
            DisplayMode = config.Bind(layout, "DisplayMode", "split",
                new ConfigDescription(
                    "Changes which aircraft get the missile panel. auto = detect per airframe; skip = never apply; split = always apply.",
                    new AcceptableValueList<string>("auto", "skip", "split")));
            OverlayMaxWidth = config.Bind(layout, "OverlayMaxWidth", 0.45f,
                new ConfigDescription("Changes small-tac detection: max normalized width treated as overlay (not split).", new AcceptableValueRange<float>(0.1f, 1f)));
            LeftWidth = config.Bind(layout, "LeftWidth", 0.58f,
                new ConfigDescription("Changes MFD split: target camera column width (0 = left, 1 = right).", new AcceptableValueRange<float>(0.1f, 0.9f)));
            MissilePanelBottom = config.Bind(layout, "MissilePanelBottom", 0.38f,
                new ConfigDescription("Changes MFD split: bottom edge of the missile feed zone.", new AcceptableValueRange<float>(0.1f, 0.9f)));
            WeaponsStripHeight = config.Bind(layout, "WeaponsStripHeight", 0.12f,
                new ConfigDescription("Changes MFD split: height of the compressed weapons strip.", new AcceptableValueRange<float>(0.05f, 0.4f)));
            ShowDivider = config.Bind(layout, "ShowDivider", true,
                "Changes MFD split: draws divider lines between layout zones.");
            DebugStub = config.Bind(layout, "DebugStub", false,
                "Dev only: shows a bright magenta test panel instead of the real feed.");
            StubLabel = config.Bind(layout, "StubLabel", "MISSILE CAMERA",
                "Dev only: text on the debug stub panel when DebugStub is on.");

            const string feed = "MissileCameraFeed";
            FeedEnabled = config.Bind(feed, "Enabled", true,
                "Turns the live missile nose camera on the MFD on/off.");
            NoseSkinInset = config.Bind(feed, "NoseSkinInset", 0.08f,
                new ConfigDescription("Changes seeker camera: distance kept outside the nose mesh (meters).", new AcceptableValueRange<float>(0.01f, 2f)));
            CameraBackOffset = config.Bind(feed, "CameraBackOffset", 0.35f,
                new ConfigDescription("Changes seeker camera: pull-back from the nose aim point (meters).", new AcceptableValueRange<float>(0.01f, 5f)));
            Fov = config.Bind(feed, "Fov", 60f,
                new ConfigDescription("Changes seeker camera: base FOV before MFD zoom offset (degrees).", new AcceptableValueRange<float>(10f, 120f)));
            FeedWidth = config.Bind(feed, "FeedWidth", 512,
                new ConfigDescription("Changes MFD feed sharpness: render texture width (pixels).", new AcceptableValueRange<int>(128, 2048)));
            FeedHeight = config.Bind(feed, "FeedHeight", 512,
                new ConfigDescription("Changes MFD feed sharpness: render texture height (pixels).", new AcceptableValueRange<int>(128, 2048)));
            HorizonLock = config.Bind(feed, "HorizonLock", true,
                "Changes seeker camera: locks roll to world up while following missile pitch/yaw.");
            TurnLookBankScale = config.Bind(feed, "TurnLookBankScale", 1f,
                new ConfigDescription("[Advanced] Changes G-load camera sway strength.", new AcceptableValueRange<float>(0f, 1.5f)));
            MaxTurnLookDegrees = config.Bind(feed, "MaxTurnLookDegrees", 90f,
                new ConfigDescription("[Advanced] Changes max G-load camera offset (degrees).", new AcceptableValueRange<float>(10f, 90f)));
            DefaultMissileGLimit = config.Bind(feed, "DefaultMissileGLimit", 20f,
                new ConfigDescription("[Advanced] Changes fallback G-limit when missile data is missing.", new AcceptableValueRange<float>(1f, 100f)));
            TurnLookGDeadband = config.Bind(feed, "TurnLookGDeadband", 0.15f,
                new ConfigDescription("[Advanced] Changes G deadband before turn-look reacts.", new AcceptableValueRange<float>(0f, 5f)));
            TurnLookGFilterHz = config.Bind(feed, "TurnLookGFilterHz", 7f,
                new ConfigDescription("[Advanced] Changes G-load filter cutoff (Hz).", new AcceptableValueRange<float>(1f, 30f)));
            TurnLookSlewDegPerSec = config.Bind(feed, "TurnLookSlewDegPerSec", 120f,
                new ConfigDescription("[Advanced] Changes turn-look slew speed (deg/s).", new AcceptableValueRange<float>(10f, 720f)));
            TurnLookSmoothTime = config.Bind(feed, "TurnLookSmoothTime", 0.18f,
                new ConfigDescription("[Advanced] Changes turn-look smoothing time (seconds).", new AcceptableValueRange<float>(0.02f, 1.5f)));
            PostExplosionHoldSeconds = config.Bind(feed, "PostExplosionHoldSeconds", 0f,
                new ConfigDescription("Changes post-loss behavior: freeze last frame N seconds (0 = off).", new AcceptableValueRange<float>(0f, 10f)));
            PostLossInterferenceSeconds = config.Bind(feed, "PostLossInterferenceSeconds", 0.5f,
                new ConfigDescription(
                    "Changes NO SIGNAL flash length (s) on missile switch, destroy, or fullscreen exit with no missiles (0 = off).",
                    new AcceptableValueRange<float>(0f, 2f)));
            RenderFps = config.Bind(feed, "RenderFps", 30,
                new ConfigDescription("Changes MFD feed render rate (Hz). Fullscreen video stays per-frame.", new AcceptableValueRange<int>(5, 60)));
            InfraredAutoEnabled = config.Bind(feed, "InfraredAutoEnabled", true,
                "Changes MFD auto IR: switches to B/W IR when dark at the missile (not a time-of-day clock).");
            InfraredDaylightThreshold = config.Bind(feed, "InfraredDaylightThreshold", 0.12f,
                new ConfigDescription(
                    "Changes MFD auto IR: ON when daylight factor at missile is below this. Lower = IR less often.",
                    new AcceptableValueRange<float>(0.01f, 1f)));
            InfraredAmbientThreshold = config.Bind(feed, "InfraredAmbientThreshold", 0.06f,
                new ConfigDescription(
                    "Changes MFD auto IR: ON when ambient light is below this. Lower = IR less often.",
                    new AcceptableValueRange<float>(0.01f, 1f)));
            InfraredLightHysteresis = config.Bind(feed, "InfraredLightHysteresis", 0.03f,
                new ConfigDescription(
                    "Changes MFD auto IR: extra light margin before IR turns off (reduces flicker).",
                    new AcceptableValueRange<float>(0f, 0.2f)));
            InfraredContrast = config.Bind(feed, "InfraredContrast", 1f,
                new ConfigDescription("Changes MFD IR picture: contrast on the feed material.", new AcceptableValueRange<float>(0f, 100f)));
            InfraredBlackPoint = config.Bind(feed, "InfraredBlackPoint", 0.05f,
                new ConfigDescription("Changes MFD IR picture: black clip level (0–1).", new AcceptableValueRange<float>(0f, 0.5f)));
            InfraredWhitePoint = config.Bind(feed, "InfraredWhitePoint", 0.95f,
                new ConfigDescription("Changes MFD IR picture: white clip level (0–1).", new AcceptableValueRange<float>(0.5f, 1f)));
            InfraredRedWeight = config.Bind(feed, "InfraredRedWeight", 0.55f,
                new ConfigDescription("Changes MFD IR picture: red channel weight in luminance.", new AcceptableValueRange<float>(0.1f, 0.9f)));
            InfraredExposureBiasEv = config.Bind(feed, "InfraredExposureBiasEv", 0f,
                new ConfigDescription(
                    "Changes MFD IR exposure vs TargetCam (0 = match vanilla; negative = darker).",
                    new AcceptableValueRange<float>(-4f, 2f)));

            const string hud = "MissileCameraHud";
            HudEnabled = config.Bind(hud, "Enabled", true,
                "Turns the MFD HUD overlay (classic S/A/R corners) on/off.");
            SalvoWindowSeconds = config.Bind(hud, "SalvoWindowSeconds", 0.5f,
                new ConfigDescription("Changes salvo label: groups rapid launches within this window (seconds).", new AcceptableValueRange<float>(0.05f, 5f)));
            ShowCenterCluster = config.Bind(hud, "ShowCenterCluster", true,
                "Changes MFD HUD: shows center reticle and hollow intercept ring at aim point.");
            ShowTargetMarker = config.Bind(hud, "ShowTargetMarker", true,
                "Changes MFD HUD: shows target diamond marker.");
            HudCockpitPipEnabled = config.Bind(hud, "CockpitPipEnabled", true,
                "Changes MFD HUD: shows bottom-left cockpit picture-in-picture.");
            HudCockpitPipFps = config.Bind(hud, "CockpitPipFps", 10,
                new ConfigDescription("Changes cockpit PiP render rate (Hz).", new AcceptableValueRange<int>(5, 30)));
            InterceptColor = config.Bind(hud, "InterceptColor", "0,1,0,1",
                "Changes MFD HUD: intercept ring color (RGBA 0–1, comma-separated).");
            ReticleColor = config.Bind(hud, "ReticleColor", "1,1,1,1",
                "Changes MFD HUD: center reticle color (RGBA 0–1).");
            HorizonColor = config.Bind(hud, "HorizonColor", "0.05,0.35,0.08,1",
                "Changes MFD HUD: attitude horizon fill color (RGBA 0–1).");
            HorizonOutlineColor = config.Bind(hud, "HorizonOutlineColor", "0.2,1,0.25,1",
                "Changes MFD HUD: attitude horizon outline color (RGBA 0–1).");
            MissileNameColor = config.Bind(hud, "MissileNameColor", "1,0,1,1",
                "Changes MFD HUD: missile name label color (RGBA 0–1).");
            OwnshipNameColor = config.Bind(hud, "OwnshipNameColor", "1,0.15,0.15,1",
                "Changes MFD HUD: ownship name label color (RGBA 0–1).");
            TargetNameColor = config.Bind(hud, "TargetNameColor", "0.4,0.9,1,1",
                "Changes MFD HUD: target name label color (RGBA 0–1).");
            LabelBackgroundColor = config.Bind(hud, "LabelBackgroundColor", "0.18,0.18,0.18,0.62",
                "Changes MFD HUD: label backdrop color (RGBA 0–1).");
            LabelBackgroundAlpha = config.Bind(hud, "LabelBackgroundAlpha", 0.62f,
                new ConfigDescription("Changes MFD HUD: label backdrop opacity.", new AcceptableValueRange<float>(0f, 1f)));

            const string controls = "MissileCameraControls";
            ControlsEnabled = config.Bind(controls, "Enabled", true,
                "Turns keyboard missile cycling and MFD zoom on/off.");
            ZoomStep = config.Bind(controls, "ZoomStep", 0.5f,
                new ConfigDescription("Changes MFD zoom: offset step per key press.", new AcceptableValueRange<float>(0.05f, 4f)));
            ZoomMin = config.Bind(controls, "ZoomMin", -4f,
                new ConfigDescription("Changes MFD zoom: minimum offset.", new AcceptableValueRange<float>(-20f, 0f)));
            ZoomMax = config.Bind(controls, "ZoomMax", 4f,
                new ConfigDescription("Changes MFD zoom: maximum offset.", new AcceptableValueRange<float>(0f, 20f)));
            ZoomFovDegreesPerUnit = config.Bind(controls, "ZoomFovDegreesPerUnit", 5f,
                new ConfigDescription("Changes MFD zoom: FOV change per offset unit (degrees).", new AcceptableValueRange<float>(0.5f, 30f)));
            IndicatorSeconds = config.Bind(controls, "IndicatorSeconds", 0.5f,
                new ConfigDescription("Changes MFD zoom HUD readout duration (seconds).", new AcceptableValueRange<float>(0.1f, 3f)));
            ControlsNextMissile = config.Bind(controls, "NextMissile",
                MissileCameraKeybindConfig.DefaultNextMissile,
                "Changes keybind: next owned in-flight missile (MFD or fullscreen). Default: RightAlt + /.");
            ControlsPreviousMissile = config.Bind(controls, "PreviousMissile",
                MissileCameraKeybindConfig.DefaultPreviousMissile,
                "Changes keybind: previous owned in-flight missile. Default: RightAlt + ,.");
            ControlsZoomIn = config.Bind(controls, "ZoomIn",
                MissileCameraKeybindConfig.DefaultMfdZoomIn,
                "Changes keybind: MFD seeker zoom in (narrower FOV). Default: RightAlt + ;.");
            ControlsZoomOut = config.Bind(controls, "ZoomOut",
                MissileCameraKeybindConfig.DefaultMfdZoomOut,
                "Changes keybind: MFD seeker zoom out (wider FOV). Default: RightAlt + .");
            ControlsResetZoom = config.Bind(controls, "ResetZoom",
                MissileCameraKeybindConfig.DefaultMfdZoomReset,
                "Changes keybind: reset MFD zoom offset to 0. Default: RightShift + .");

            const string fullscreen = "MissileCameraFullscreen";
            FullscreenEnabled = config.Bind(fullscreen, "Enabled", true,
                "Turns fullscreen missile feed (entire viewport, not MFD-only) on/off.");
            FullscreenToggleKey = config.Bind(fullscreen, "ToggleKey", "F",
                "Changes keybind: KeyCode name for fullscreen toggle (with RequireRightAlt if set).");
            FullscreenRequireRightAlt = config.Bind(fullscreen, "RequireRightAlt", true,
                "Changes keybind: require RightAlt held with ToggleKey for fullscreen.");
            FullscreenFeedWidth = config.Bind(fullscreen, "FeedWidth", 1920,
                new ConfigDescription(
                    "Changes fullscreen feed sharpness: render texture width (independent of MFD FeedWidth).",
                    new AcceptableValueRange<int>(640, 3840)));
            FullscreenFeedHeight = config.Bind(fullscreen, "FeedHeight", 1080,
                new ConfigDescription(
                    "Changes fullscreen feed sharpness: render texture height (independent of MFD FeedHeight).",
                    new AcceptableValueRange<int>(360, 2160)));
            FullscreenZoomMax = config.Bind(fullscreen, "ZoomMax", 50f,
                new ConfigDescription("Changes fullscreen optical zoom: max magnification (mouse wheel).", new AcceptableValueRange<float>(2f, 50f)));
            FullscreenZoomWheelFactor = config.Bind(fullscreen, "ZoomWheelFactor", 1.12f,
                new ConfigDescription("Changes fullscreen optical zoom: multiply per mouse-wheel notch.", new AcceptableValueRange<float>(1.02f, 1.5f)));
            FullscreenVisionCycleKey = config.Bind(fullscreen, "VisionCycleKey", "J",
                "Changes keybind: cycle fullscreen vision (Color / NVG / WhiteHot / BlackHot / Contour).");
            FullscreenZoomResetOnExit = config.Bind(fullscreen, "ZoomResetOnExit", true,
                "Changes fullscreen zoom: reset magnification to 1× when leaving fullscreen.");
            FullscreenPitchLadderEnabled = config.Bind(fullscreen, "PitchLadderEnabled", true,
                "Changes fullscreen FLIR: shows stock FlightHud pitch ladder (cloned texture, FLIR tint).");
            FullscreenPitchLadderTint = config.Bind(fullscreen, "PitchLadderTint", "0.55,1,0.9,1",
                "Changes fullscreen pitch ladder color (RGBA 0–1, comma-separated).");
            FullscreenPitchLadderIntensity = config.Bind(fullscreen, "PitchLadderIntensity", 3.2f,
                new ConfigDescription("Changes fullscreen pitch ladder brightness (higher = brighter FLIR overlay).", new AcceptableValueRange<float>(1f, 4f)));
            FullscreenZoomResetKey = config.Bind(fullscreen, "ZoomResetKey",
                MissileCameraKeybindConfig.DefaultFullscreenZoomReset,
                "Changes keybind: reset fullscreen optical zoom to 1×. Default: Middle Mouse.");

            const string telemetry = "MissileCameraTelemetry";
            TelemetrySmoothHz = config.Bind(telemetry, "SmoothHz", 10f,
                new ConfigDescription("Changes telemetry smoothing rate (Hz; capped by RenderFps).", new AcceptableValueRange<float>(1f, 60f)));

            const string fx = "MissileCameraEffects";
            FxScanlinesEnabled = config.Bind(fx, "ScanlinesEnabled", false,
                "Changes MFD post-FX: scanlines overlay (requires shader bundle).");
            FxScanlinesIntensity = config.Bind(fx, "ScanlinesIntensity", 0.35f,
                new ConfigDescription("Changes scanlines post-FX strength (0–1).", new AcceptableValueRange<float>(0f, 1f)));
            FxMotionBlurEnabled = config.Bind(fx, "MotionBlurEnabled", false,
                "Changes MFD post-FX: motion blur (requires shader bundle).");
            FxMotionBlurIntensity = config.Bind(fx, "MotionBlurIntensity", 0.25f,
                new ConfigDescription("Changes motion blur strength (0–1).", new AcceptableValueRange<float>(0f, 1f)));
            FxChromaticEnabled = config.Bind(fx, "ChromaticEnabled", false,
                "Changes MFD post-FX: chromatic aberration (requires shader bundle).");
            FxChromaticIntensity = config.Bind(fx, "ChromaticIntensity", 0.2f,
                new ConfigDescription("Changes chromatic aberration strength (0–1).", new AcceptableValueRange<float>(0f, 1f)));
            FxBloomEnabled = config.Bind(fx, "BloomEnabled", false,
                "Changes MFD post-FX: bloom (requires shader bundle).");
            FxBloomIntensity = config.Bind(fx, "BloomIntensity", 0.3f,
                new ConfigDescription("Changes bloom strength (0–1).", new AcceptableValueRange<float>(0f, 1f)));

            const string aircraftCam = "MissileCameraAircraftCam";
            AircraftCamEnabled = config.Bind(aircraftCam, "Enabled", false,
                "Turns the aircraft mini-cam overlay on/off (no-op when DisplayMode=skip).");
            AircraftCamMode = config.Bind(aircraftCam, "Mode", "Rear",
                new ConfigDescription("Changes mini-cam view: Rear / TopDown / Chase.", new AcceptableValueList<string>("Rear", "TopDown", "Chase")));
            AircraftCamFps = config.Bind(aircraftCam, "RenderFps", 10,
                new ConfigDescription("Changes mini-cam render rate (Hz).", new AcceptableValueRange<int>(5, 30)));
            AircraftCamWidth = config.Bind(aircraftCam, "Width", 256,
                new ConfigDescription("Changes mini-cam render texture width (pixels).", new AcceptableValueRange<int>(64, 1024)));
            AircraftCamHeight = config.Bind(aircraftCam, "Height", 256,
                new ConfigDescription("Changes mini-cam render texture height (pixels).", new AcceptableValueRange<int>(64, 1024)));
            AircraftCamAnchorMinX = config.Bind(aircraftCam, "AnchorMinX", 0.72f,
                new ConfigDescription("Changes mini-cam screen position: normalized rect min X.", new AcceptableValueRange<float>(0f, 1f)));
            AircraftCamAnchorMinY = config.Bind(aircraftCam, "AnchorMinY", 0.72f,
                new ConfigDescription("Changes mini-cam screen position: normalized rect min Y.", new AcceptableValueRange<float>(0f, 1f)));
            AircraftCamAnchorMaxX = config.Bind(aircraftCam, "AnchorMaxX", 0.98f,
                new ConfigDescription("Changes mini-cam screen position: normalized rect max X.", new AcceptableValueRange<float>(0f, 1f)));
            AircraftCamAnchorMaxY = config.Bind(aircraftCam, "AnchorMaxY", 0.98f,
                new ConfigDescription("Changes mini-cam screen position: normalized rect max Y.", new AcceptableValueRange<float>(0f, 1f)));
            AircraftCamHideInFullscreen = config.Bind(aircraftCam, "HideInFullscreen", false,
                "Changes mini-cam: hide while fullscreen missile feed is active.");
            AircraftCamCycleKey = config.Bind(aircraftCam, "CycleKey", "V",
                "Changes keybind: KeyCode to cycle mini-cam mode (with RequireRightAlt if set).");
            AircraftCamRequireRightAlt = config.Bind(aircraftCam, "RequireRightAlt", true,
                "Changes keybind: require RightAlt held with CycleKey for mini-cam mode cycle.");

            IsBound = true;
        }
    }
}
