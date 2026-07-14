using BepInEx.Configuration;

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
        internal static ConfigEntry<string> HudStyle { get; private set; } = null!;
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

        internal static ConfigEntry<bool> FullscreenEnabled { get; private set; } = null!;
        internal static ConfigEntry<string> FullscreenToggleKey { get; private set; } = null!;
        internal static ConfigEntry<bool> FullscreenRequireRightAlt { get; private set; } = null!;
        internal static ConfigEntry<float> FullscreenBootstrapSeconds { get; private set; } = null!;
        internal static ConfigEntry<int> FullscreenBootstrapSteps { get; private set; } = null!;
        internal static ConfigEntry<int> FullscreenFeedWidth { get; private set; } = null!;
        internal static ConfigEntry<int> FullscreenFeedHeight { get; private set; } = null!;

        internal static ConfigEntry<bool> TelemetryShowG { get; private set; } = null!;
        internal static ConfigEntry<bool> TelemetryShowFuel { get; private set; } = null!;
        internal static ConfigEntry<bool> TelemetryShowGuidance { get; private set; } = null!;
        internal static ConfigEntry<bool> TelemetryShowMach { get; private set; } = null!;
        internal static ConfigEntry<bool> TelemetryShowTargetRange { get; private set; } = null!;
        internal static ConfigEntry<bool> TelemetryShowTargetAngle { get; private set; } = null!;
        internal static ConfigEntry<float> TelemetrySmoothHz { get; private set; } = null!;

        internal static ConfigEntry<bool> FxInfraredEnabled { get; private set; } = null!;
        internal static ConfigEntry<bool> FxScanlinesEnabled { get; private set; } = null!;
        internal static ConfigEntry<float> FxScanlinesIntensity { get; private set; } = null!;
        internal static ConfigEntry<bool> FxMotionBlurEnabled { get; private set; } = null!;
        internal static ConfigEntry<float> FxMotionBlurIntensity { get; private set; } = null!;
        internal static ConfigEntry<bool> FxChromaticEnabled { get; private set; } = null!;
        internal static ConfigEntry<float> FxChromaticIntensity { get; private set; } = null!;
        internal static ConfigEntry<bool> FxBloomEnabled { get; private set; } = null!;
        internal static ConfigEntry<float> FxBloomIntensity { get; private set; } = null!;

        internal static ConfigEntry<int> MarkersMax { get; private set; } = null!;
        internal static ConfigEntry<bool> MarkersShowTarget { get; private set; } = null!;
        internal static ConfigEntry<bool> MarkersShowAim { get; private set; } = null!;
        internal static ConfigEntry<bool> MarkersShowSceneUnits { get; private set; } = null!;
        internal static ConfigEntry<float> MarkersSceneUnitAlpha { get; private set; } = null!;
        internal static ConfigEntry<bool> MarkersShowThreat { get; private set; } = null!;
        internal static ConfigEntry<bool> MarkersShowAlly { get; private set; } = null!;
        internal static ConfigEntry<bool> MarkersShowWaypoint { get; private set; } = null!;
        internal static ConfigEntry<bool> MarkersShowJam { get; private set; } = null!;
        internal static ConfigEntry<string> MarkersTargetColor { get; private set; } = null!;
        internal static ConfigEntry<string> MarkersAimColor { get; private set; } = null!;
        internal static ConfigEntry<string> MarkersThreatColor { get; private set; } = null!;
        internal static ConfigEntry<string> MarkersAllyColor { get; private set; } = null!;
        internal static ConfigEntry<string> MarkersWaypointColor { get; private set; } = null!;
        internal static ConfigEntry<string> MarkersJamColor { get; private set; } = null!;

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
            LayoutEnabled = config.Bind(layout, "Enabled", true, "Master switch for MFD layout changes.");
            DisplayMode = config.Bind(layout, "DisplayMode", "split",
                new ConfigDescription(
                    "auto = per-aircraft detection; skip = never apply; split = always split layout.",
                    new AcceptableValueList<string>("auto", "skip", "split")));
            OverlayMaxWidth = config.Bind(layout, "OverlayMaxWidth", 0.45f,
                new ConfigDescription("Max normalized width for tac overlay detection.", new AcceptableValueRange<float>(0.1f, 1f)));
            LeftWidth = config.Bind(layout, "LeftWidth", 0.58f,
                new ConfigDescription("Target cam column width (0–1).", new AcceptableValueRange<float>(0.1f, 0.9f)));
            MissilePanelBottom = config.Bind(layout, "MissilePanelBottom", 0.38f,
                new ConfigDescription("Bottom edge of missile panel.", new AcceptableValueRange<float>(0.1f, 0.9f)));
            WeaponsStripHeight = config.Bind(layout, "WeaponsStripHeight", 0.12f,
                new ConfigDescription("Compressed weapons strip height.", new AcceptableValueRange<float>(0.05f, 0.4f)));
            ShowDivider = config.Bind(layout, "ShowDivider", true, "Draw zone divider lines.");
            DebugStub = config.Bind(layout, "DebugStub", false, "Bright magenta test panel.");
            StubLabel = config.Bind(layout, "StubLabel", "MISSILE CAMERA", "Label on debug stub.");

            const string feed = "MissileCameraFeed";
            FeedEnabled = config.Bind(feed, "Enabled", true, "Live missile camera feed on MFD.");
            NoseSkinInset = config.Bind(feed, "NoseSkinInset", 0.08f,
                new ConfigDescription("Keep camera outside nose mesh (meters).", new AcceptableValueRange<float>(0.01f, 2f)));
            CameraBackOffset = config.Bind(feed, "CameraBackOffset", 0.35f,
                new ConfigDescription("Pull camera back from nose point (meters).", new AcceptableValueRange<float>(0.01f, 5f)));
            Fov = config.Bind(feed, "Fov", 60f,
                new ConfigDescription("Base field of view (degrees).", new AcceptableValueRange<float>(10f, 120f)));
            FeedWidth = config.Bind(feed, "FeedWidth", 512,
                new ConfigDescription("RenderTexture width.", new AcceptableValueRange<int>(128, 2048)));
            FeedHeight = config.Bind(feed, "FeedHeight", 512,
                new ConfigDescription("RenderTexture height.", new AcceptableValueRange<int>(128, 2048)));
            HorizonLock = config.Bind(feed, "HorizonLock", true, "World-up roll lock; body-follow pitch/yaw.");
            TurnLookBankScale = config.Bind(feed, "TurnLookBankScale", 1f,
                new ConfigDescription("G-load turn look scale.", new AcceptableValueRange<float>(0f, 1.5f)));
            MaxTurnLookDegrees = config.Bind(feed, "MaxTurnLookDegrees", 90f,
                new ConfigDescription("Max turn-look offset (degrees).", new AcceptableValueRange<float>(10f, 90f)));
            DefaultMissileGLimit = config.Bind(feed, "DefaultMissileGLimit", 20f,
                new ConfigDescription("Fallback G limit.", new AcceptableValueRange<float>(1f, 100f)));
            TurnLookGDeadband = config.Bind(feed, "TurnLookGDeadband", 0.15f,
                new ConfigDescription("G deadband.", new AcceptableValueRange<float>(0f, 5f)));
            TurnLookGFilterHz = config.Bind(feed, "TurnLookGFilterHz", 7f,
                new ConfigDescription("G filter cutoff (Hz).", new AcceptableValueRange<float>(1f, 30f)));
            TurnLookSlewDegPerSec = config.Bind(feed, "TurnLookSlewDegPerSec", 120f,
                new ConfigDescription("Turn-look slew rate (deg/s).", new AcceptableValueRange<float>(10f, 720f)));
            TurnLookSmoothTime = config.Bind(feed, "TurnLookSmoothTime", 0.18f,
                new ConfigDescription("Turn-look smoothing time (seconds).", new AcceptableValueRange<float>(0.02f, 1.5f)));
            PostExplosionHoldSeconds = config.Bind(feed, "PostExplosionHoldSeconds", 0f,
                new ConfigDescription("Hold last frame after missile loss (0 = off).", new AcceptableValueRange<float>(0f, 10f)));
            PostLossInterferenceSeconds = config.Bind(feed, "PostLossInterferenceSeconds", 0.4f,
                new ConfigDescription(
                    "TV-static burst after the last followed missile is destroyed (0 = off).",
                    new AcceptableValueRange<float>(0f, 2f)));
            RenderFps = config.Bind(feed, "RenderFps", 30,
                new ConfigDescription("Feed refresh rate.", new AcceptableValueRange<int>(5, 60)));
            InfraredAutoEnabled = config.Bind(feed, "InfraredAutoEnabled", true,
                "Auto B/W IR from lighting only: low GetDaylightFactor (night / thick clouds) or low GetAmbientLight. Not a fixed clock window.");
            InfraredDaylightThreshold = config.Bind(feed, "InfraredDaylightThreshold", 0.12f,
                new ConfigDescription(
                    "IR ON when GetDaylightFactor at the missile is below this (night / under thick cloud). Lower = rarer IR.",
                    new AcceptableValueRange<float>(0.01f, 1f)));
            InfraredAmbientThreshold = config.Bind(feed, "InfraredAmbientThreshold", 0.06f,
                new ConfigDescription(
                    "IR ON when GetAmbientLight is below this (truly dark ambient). Lower = rarer IR.",
                    new AcceptableValueRange<float>(0.01f, 1f)));
            InfraredLightHysteresis = config.Bind(feed, "InfraredLightHysteresis", 0.03f,
                new ConfigDescription(
                    "Extra light margin before IR turns off (anti-flicker).",
                    new AcceptableValueRange<float>(0f, 0.2f)));
            InfraredContrast = config.Bind(feed, "InfraredContrast", 1f,
                new ConfigDescription("IR contrast for RawImage material.", new AcceptableValueRange<float>(0f, 100f)));
            InfraredBlackPoint = config.Bind(feed, "InfraredBlackPoint", 0.05f,
                new ConfigDescription("IR black clip (0–1).", new AcceptableValueRange<float>(0f, 0.5f)));
            InfraredWhitePoint = config.Bind(feed, "InfraredWhitePoint", 0.95f,
                new ConfigDescription("IR white clip (0–1).", new AcceptableValueRange<float>(0.5f, 1f)));
            InfraredRedWeight = config.Bind(feed, "InfraredRedWeight", 0.55f,
                new ConfigDescription("Red luminance weight for IR.", new AcceptableValueRange<float>(0.1f, 0.9f)));
            InfraredExposureBiasEv = config.Bind(feed, "InfraredExposureBiasEv", 0f,
                new ConfigDescription(
                    "Extra EV vs TargetCam IR (0 = match TargetCam; negative = darker). Highlight compress handles plume.",
                    new AcceptableValueRange<float>(-4f, 2f)));

            const string hud = "MissileCameraHud";
            HudEnabled = config.Bind(hud, "Enabled", true, "HUD overlay on feed.");
            HudStyle = config.Bind(hud, "Style", "Classic",
                new ConfigDescription(
                    "MFD always uses Classic. Fullscreen uses FLIR overlay (Style kept for compatibility).",
                    new AcceptableValueList<string>("Tgp", "Classic")));
            SalvoWindowSeconds = config.Bind(hud, "SalvoWindowSeconds", 0.5f,
                new ConfigDescription("Salvo grouping window (seconds).", new AcceptableValueRange<float>(0.05f, 5f)));
            ShowCenterCluster = config.Bind(hud, "ShowCenterCluster", true, "Center reticle / intercept ring.");
            ShowTargetMarker = config.Bind(hud, "ShowTargetMarker", true, "Target diamond marker.");
            HudCockpitPipEnabled = config.Bind(hud, "CockpitPipEnabled", true, "TGP bottom-left cockpit PiP (TOR: Cockpit View).");
            HudCockpitPipFps = config.Bind(hud, "CockpitPipFps", 15,
                new ConfigDescription("Cockpit PiP render FPS.", new AcceptableValueRange<int>(5, 30)));
            InterceptColor = config.Bind(hud, "InterceptColor", "0,1,0,1", "Intercept ring RGBA (0–1), comma-separated.");
            ReticleColor = config.Bind(hud, "ReticleColor", "1,1,1,1", "Reticle RGBA (0–1), comma-separated.");
            HorizonColor = config.Bind(hud, "HorizonColor", "0.05,0.35,0.08,1", "Horizon fill RGBA.");
            HorizonOutlineColor = config.Bind(hud, "HorizonOutlineColor", "0.2,1,0.25,1", "Horizon outline RGBA.");
            MissileNameColor = config.Bind(hud, "MissileNameColor", "1,0,1,1", "Classic missile name label RGBA.");
            OwnshipNameColor = config.Bind(hud, "OwnshipNameColor", "1,0.15,0.15,1", "TGP ownship name RGBA.");
            TargetNameColor = config.Bind(hud, "TargetNameColor", "0.4,0.9,1,1", "Target name label RGBA.");
            LabelBackgroundColor = config.Bind(hud, "LabelBackgroundColor", "0.18,0.18,0.18,0.62", "Label backdrop RGBA.");
            LabelBackgroundAlpha = config.Bind(hud, "LabelBackgroundAlpha", 0.62f,
                new ConfigDescription("Label backdrop alpha override.", new AcceptableValueRange<float>(0f, 1f)));

            const string controls = "MissileCameraControls";
            ControlsEnabled = config.Bind(controls, "Enabled", true, "Keyboard missile cycling and zoom.");
            ZoomStep = config.Bind(controls, "ZoomStep", 0.5f,
                new ConfigDescription("Zoom offset change per key press.", new AcceptableValueRange<float>(0.05f, 4f)));
            ZoomMin = config.Bind(controls, "ZoomMin", -4f,
                new ConfigDescription("Minimum zoom offset.", new AcceptableValueRange<float>(-20f, 0f)));
            ZoomMax = config.Bind(controls, "ZoomMax", 4f,
                new ConfigDescription("Maximum zoom offset.", new AcceptableValueRange<float>(0f, 20f)));
            ZoomFovDegreesPerUnit = config.Bind(controls, "ZoomFovDegreesPerUnit", 5f,
                new ConfigDescription("FOV delta (degrees) per zoom offset unit.", new AcceptableValueRange<float>(0.5f, 30f)));
            IndicatorSeconds = config.Bind(controls, "IndicatorSeconds", 0.5f,
                new ConfigDescription("Zoom HUD readout duration (seconds).", new AcceptableValueRange<float>(0.1f, 3f)));

            const string fullscreen = "MissileCameraFullscreen";
            FullscreenEnabled = config.Bind(fullscreen, "Enabled", true, "Allow fullscreen missile feed (entire game viewport, not MFD-only).");
            FullscreenToggleKey = config.Bind(fullscreen, "ToggleKey", "F", "KeyCode name for fullscreen toggle.");
            FullscreenRequireRightAlt = config.Bind(fullscreen, "RequireRightAlt", true, "Require RightAlt held with ToggleKey.");
            FullscreenBootstrapSeconds = config.Bind(fullscreen, "BootstrapSeconds", 0.6f,
                new ConfigDescription("First-enter-per-mission bootstrap duration.", new AcceptableValueRange<float>(0.05f, 3f)));
            FullscreenBootstrapSteps = config.Bind(fullscreen, "BootstrapSteps", 4,
                new ConfigDescription("Bootstrap staged UI steps.", new AcceptableValueRange<int>(1, 8)));
            FullscreenFeedWidth = config.Bind(fullscreen, "FeedWidth", 1920,
                new ConfigDescription(
                    "Fullscreen feed RenderTexture width (independent of MFD FeedWidth).",
                    new AcceptableValueRange<int>(640, 3840)));
            FullscreenFeedHeight = config.Bind(fullscreen, "FeedHeight", 1080,
                new ConfigDescription(
                    "Fullscreen feed RenderTexture height (independent of MFD FeedHeight).",
                    new AcceptableValueRange<int>(360, 2160)));

            const string telemetry = "MissileCameraTelemetry";
            TelemetryShowG = config.Bind(telemetry, "ShowG", false, "Reserved extended G readout (off = classic MFD HUD like 1.30.1).");
            TelemetryShowFuel = config.Bind(telemetry, "ShowFuel", false, "Reserved extended fuel readout (off = classic MFD HUD).");
            TelemetryShowGuidance = config.Bind(telemetry, "ShowGuidance", false, "Reserved guidance status on HUD (off = classic MFD HUD).");
            TelemetryShowMach = config.Bind(telemetry, "ShowMach", false, "Reserved Mach readout (off = classic MFD HUD).");
            TelemetryShowTargetRange = config.Bind(telemetry, "ShowTargetRange", true, "Use aim/target range for R: label (classic).");
            TelemetryShowTargetAngle = config.Bind(telemetry, "ShowTargetAngle", false, "Reserved off-bore angle on HUD (off = classic MFD HUD).");
            TelemetrySmoothHz = config.Bind(telemetry, "SmoothHz", 12f,
                new ConfigDescription("Telemetry smoothing Hz (capped by RenderFps).", new AcceptableValueRange<float>(1f, 60f)));

            const string fx = "MissileCameraEffects";
            FxInfraredEnabled = config.Bind(fx, "InfraredEnabled", true, "Allow IR stage (applied in rig; availability probed at startup).");
            FxScanlinesEnabled = config.Bind(fx, "ScanlinesEnabled", false, "Scanlines post-FX (requires shader bundle).");
            FxScanlinesIntensity = config.Bind(fx, "ScanlinesIntensity", 0.35f,
                new ConfigDescription("Scanlines intensity 0–1.", new AcceptableValueRange<float>(0f, 1f)));
            FxMotionBlurEnabled = config.Bind(fx, "MotionBlurEnabled", false, "Motion blur post-FX (requires shader bundle).");
            FxMotionBlurIntensity = config.Bind(fx, "MotionBlurIntensity", 0.25f,
                new ConfigDescription("Motion blur intensity 0–1.", new AcceptableValueRange<float>(0f, 1f)));
            FxChromaticEnabled = config.Bind(fx, "ChromaticEnabled", false, "Chromatic aberration (requires shader bundle).");
            FxChromaticIntensity = config.Bind(fx, "ChromaticIntensity", 0.2f,
                new ConfigDescription("Chromatic intensity 0–1.", new AcceptableValueRange<float>(0f, 1f)));
            FxBloomEnabled = config.Bind(fx, "BloomEnabled", false, "Bloom post-FX (requires shader bundle).");
            FxBloomIntensity = config.Bind(fx, "BloomIntensity", 0.3f,
                new ConfigDescription("Bloom intensity 0–1.", new AcceptableValueRange<float>(0f, 1f)));

            const string markers = "MissileCameraMarkers";
            MarkersMax = config.Bind(markers, "MaxMarkers", 48,
                new ConfigDescription("Max pooled markers projected per frame.", new AcceptableValueRange<int>(1, 64)));
            MarkersShowTarget = config.Bind(markers, "ShowTarget", true, "Show locked target marker from HudSnapshot.");
            MarkersShowAim = config.Bind(markers, "ShowAim", true, "Show aim/intercept marker from HudSnapshot.");
            MarkersShowSceneUnits = config.Bind(markers, "ShowSceneUnits", true,
                "Show translucent unlabeled markers for all other scene units.");
            MarkersSceneUnitAlpha = config.Bind(markers, "SceneUnitAlpha", 0.4f,
                new ConfigDescription("Alpha for ambient unit markers (no labels).", new AcceptableValueRange<float>(0.1f, 1f)));
            MarkersShowThreat = config.Bind(markers, "ShowThreat", false, "Reserved threat markers.");
            MarkersShowAlly = config.Bind(markers, "ShowAlly", false, "Reserved ally markers.");
            MarkersShowWaypoint = config.Bind(markers, "ShowWaypoint", false, "Reserved waypoint markers.");
            MarkersShowJam = config.Bind(markers, "ShowJam", false, "Reserved jam markers.");
            MarkersTargetColor = config.Bind(markers, "TargetColor", "0.35,0.95,1,1", "Target marker RGBA (cyan).");
            MarkersAimColor = config.Bind(markers, "AimColor", "1,0.75,0.12,1", "Aim/IP marker RGBA (amber).");
            MarkersThreatColor = config.Bind(markers, "ThreatColor", "1,0.22,0.18,1", "Threat marker RGBA (red).");
            MarkersAllyColor = config.Bind(markers, "AllyColor", "0.35,1,0.45,1", "Ally marker RGBA (green).");
            MarkersWaypointColor = config.Bind(markers, "WaypointColor", "0.95,0.55,1,1", "Waypoint marker RGBA (violet).");
            MarkersJamColor = config.Bind(markers, "JamColor", "1,0.45,0.05,1", "Jam marker RGBA (orange).");

            const string aircraftCam = "MissileCameraAircraftCam";
            AircraftCamEnabled = config.Bind(aircraftCam, "Enabled", false, "Aircraft mini-cam (off by default). No-op when DisplayMode=skip.");
            AircraftCamMode = config.Bind(aircraftCam, "Mode", "Rear",
                new ConfigDescription("Rear / TopDown / Chase.", new AcceptableValueList<string>("Rear", "TopDown", "Chase")));
            AircraftCamFps = config.Bind(aircraftCam, "RenderFps", 15,
                new ConfigDescription("Mini-cam FPS.", new AcceptableValueRange<int>(5, 30)));
            AircraftCamWidth = config.Bind(aircraftCam, "Width", 256,
                new ConfigDescription("Mini-cam RT width.", new AcceptableValueRange<int>(64, 1024)));
            AircraftCamHeight = config.Bind(aircraftCam, "Height", 256,
                new ConfigDescription("Mini-cam RT height.", new AcceptableValueRange<int>(64, 1024)));
            AircraftCamAnchorMinX = config.Bind(aircraftCam, "AnchorMinX", 0.72f,
                new ConfigDescription("Normalized rect min X.", new AcceptableValueRange<float>(0f, 1f)));
            AircraftCamAnchorMinY = config.Bind(aircraftCam, "AnchorMinY", 0.72f,
                new ConfigDescription("Normalized rect min Y.", new AcceptableValueRange<float>(0f, 1f)));
            AircraftCamAnchorMaxX = config.Bind(aircraftCam, "AnchorMaxX", 0.98f,
                new ConfigDescription("Normalized rect max X.", new AcceptableValueRange<float>(0f, 1f)));
            AircraftCamAnchorMaxY = config.Bind(aircraftCam, "AnchorMaxY", 0.98f,
                new ConfigDescription("Normalized rect max Y.", new AcceptableValueRange<float>(0f, 1f)));
            AircraftCamHideInFullscreen = config.Bind(aircraftCam, "HideInFullscreen", false, "Hide mini-cam while fullscreen.");
            AircraftCamCycleKey = config.Bind(aircraftCam, "CycleKey", "V", "KeyCode to cycle mini-cam mode.");
            AircraftCamRequireRightAlt = config.Bind(aircraftCam, "RequireRightAlt", true, "Require RightAlt with CycleKey.");

            IsBound = true;
        }
    }
}
