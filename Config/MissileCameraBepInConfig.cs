using BepInEx.Configuration;
using UnityEngine;

namespace MissileCamera.Config
{
    /// <summary>BepInEx Configuration Manager bindings (com.at747.missilecamera.bepinex.cfg).</summary>
    internal static class MissileCameraBepInConfig
    {
        internal static bool IsBound { get; private set; }

        internal static ConfigEntry<bool> LayoutEnabled { get; private set; }
        internal static ConfigEntry<string> DisplayMode { get; private set; }
        internal static ConfigEntry<float> OverlayMaxWidth { get; private set; }
        internal static ConfigEntry<float> LeftWidth { get; private set; }
        internal static ConfigEntry<float> MissilePanelBottom { get; private set; }
        internal static ConfigEntry<float> WeaponsStripHeight { get; private set; }
        internal static ConfigEntry<bool> ShowDivider { get; private set; }
        internal static ConfigEntry<bool> DebugStub { get; private set; }
        internal static ConfigEntry<string> StubLabel { get; private set; }

        internal static ConfigEntry<bool> FeedEnabled { get; private set; }
        internal static ConfigEntry<float> NoseSkinInset { get; private set; }
        internal static ConfigEntry<float> CameraBackOffset { get; private set; }
        internal static ConfigEntry<float> Fov { get; private set; }
        internal static ConfigEntry<int> FeedWidth { get; private set; }
        internal static ConfigEntry<int> FeedHeight { get; private set; }
        internal static ConfigEntry<bool> HorizonLock { get; private set; }
        internal static ConfigEntry<float> TurnLookBankScale { get; private set; }
        internal static ConfigEntry<float> MaxTurnLookDegrees { get; private set; }
        internal static ConfigEntry<float> DefaultMissileGLimit { get; private set; }
        internal static ConfigEntry<float> TurnLookGDeadband { get; private set; }
        internal static ConfigEntry<float> TurnLookGFilterHz { get; private set; }
        internal static ConfigEntry<float> TurnLookSlewDegPerSec { get; private set; }
        internal static ConfigEntry<float> TurnLookSmoothTime { get; private set; }
        internal static ConfigEntry<float> PostExplosionHoldSeconds { get; private set; }
        internal static ConfigEntry<int> RenderFps { get; private set; }

        internal static ConfigEntry<bool> HudEnabled { get; private set; }
        internal static ConfigEntry<float> SalvoWindowSeconds { get; private set; }
        internal static ConfigEntry<bool> ShowCenterCluster { get; private set; }
        internal static ConfigEntry<bool> ShowTargetMarker { get; private set; }
        internal static ConfigEntry<string> InterceptColor { get; private set; }
        internal static ConfigEntry<string> ReticleColor { get; private set; }
        internal static ConfigEntry<string> HorizonColor { get; private set; }
        internal static ConfigEntry<string> HorizonOutlineColor { get; private set; }
        internal static ConfigEntry<string> MissileNameColor { get; private set; }
        internal static ConfigEntry<string> TargetNameColor { get; private set; }
        internal static ConfigEntry<string> LabelBackgroundColor { get; private set; }
        internal static ConfigEntry<float> LabelBackgroundAlpha { get; private set; }

        internal static ConfigEntry<bool> ControlsEnabled { get; private set; }
        internal static ConfigEntry<string> ModifierKey { get; private set; }
        internal static ConfigEntry<string> NextMissileKey { get; private set; }
        internal static ConfigEntry<string> PreviousMissileKey { get; private set; }
        internal static ConfigEntry<string> ZoomInKey { get; private set; }
        internal static ConfigEntry<string> ZoomOutKey { get; private set; }
        internal static ConfigEntry<string> ResetZoomModifierKey { get; private set; }
        internal static ConfigEntry<string> ResetZoomKey { get; private set; }
        internal static ConfigEntry<float> ZoomStep { get; private set; }
        internal static ConfigEntry<float> ZoomMin { get; private set; }
        internal static ConfigEntry<float> ZoomMax { get; private set; }
        internal static ConfigEntry<float> ZoomFovDegreesPerUnit { get; private set; }
        internal static ConfigEntry<float> IndicatorSeconds { get; private set; }

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
            RenderFps = config.Bind(feed, "RenderFps", 30,
                new ConfigDescription("Feed refresh rate.", new AcceptableValueRange<int>(5, 60)));

            const string hud = "MissileCameraHud";
            HudEnabled = config.Bind(hud, "Enabled", true, "HUD overlay on feed.");
            SalvoWindowSeconds = config.Bind(hud, "SalvoWindowSeconds", 0.5f,
                new ConfigDescription("Salvo grouping window (seconds).", new AcceptableValueRange<float>(0.05f, 5f)));
            ShowCenterCluster = config.Bind(hud, "ShowCenterCluster", true, "Center reticle / intercept ring.");
            ShowTargetMarker = config.Bind(hud, "ShowTargetMarker", true, "Target diamond marker.");
            InterceptColor = config.Bind(hud, "InterceptColor", "0,1,0,1", "Intercept ring RGBA (0–1), comma-separated.");
            ReticleColor = config.Bind(hud, "ReticleColor", "0,0.4,1,1", "Reticle RGBA (0–1), comma-separated.");
            HorizonColor = config.Bind(hud, "HorizonColor", "0.05,0.35,0.08,1", "Horizon fill RGBA.");
            HorizonOutlineColor = config.Bind(hud, "HorizonOutlineColor", "0.2,1,0.25,1", "Horizon outline RGBA.");
            MissileNameColor = config.Bind(hud, "MissileNameColor", "1,0,1,1", "Missile name label RGBA.");
            TargetNameColor = config.Bind(hud, "TargetNameColor", "0.4,0.9,1,1", "Target name label RGBA.");
            LabelBackgroundColor = config.Bind(hud, "LabelBackgroundColor", "0.18,0.18,0.18,0.62", "Label backdrop RGBA.");
            LabelBackgroundAlpha = config.Bind(hud, "LabelBackgroundAlpha", 0.62f,
                new ConfigDescription("Label backdrop alpha override.", new AcceptableValueRange<float>(0f, 1f)));

            const string controls = "MissileCameraControls";
            const string keyHint = "Unity KeyCode name (e.g. RightAlt, Slash, None).";
            ControlsEnabled = config.Bind(controls, "Enabled", true, "Keyboard missile cycling and zoom.");
            ModifierKey = config.Bind(controls, "ModifierKey", "RightAlt",
                "Hold this key with cycle/zoom actions. Use None for no modifier.");
            NextMissileKey = config.Bind(controls, "NextMissileKey", "Slash",
                "Select next owned in-flight missile (with ModifierKey). " + keyHint);
            PreviousMissileKey = config.Bind(controls, "PreviousMissileKey", "Comma",
                "Select previous owned in-flight missile (with ModifierKey). " + keyHint);
            ZoomInKey = config.Bind(controls, "ZoomInKey", "Semicolon",
                "Zoom in (with ModifierKey). " + keyHint);
            ZoomOutKey = config.Bind(controls, "ZoomOutKey", "Period",
                "Zoom out (with ModifierKey). " + keyHint);
            ResetZoomModifierKey = config.Bind(controls, "ResetZoomModifierKey", "RightShift",
                "Hold this with ResetZoomKey. Use None for no modifier.");
            ResetZoomKey = config.Bind(controls, "ResetZoomKey", "Period",
                "Reset zoom offset to 0. " + keyHint);
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

            IsBound = true;
        }
    }
}
