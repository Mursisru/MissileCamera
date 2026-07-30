using BepInEx.Configuration;
using UnityEngine;

namespace MissileCamera.Config
{
    /// <summary>Runtime cache for all KeyboardShortcut binds (refreshed ~1 Hz).</summary>
    internal static class MissileCameraKeybindConfig
    {
        internal static KeyboardShortcut NextMissile = DefaultNextMissile;
        internal static KeyboardShortcut PreviousMissile = DefaultPreviousMissile;
        internal static KeyboardShortcut MfdZoomIn = DefaultMfdZoomIn;
        internal static KeyboardShortcut MfdZoomOut = DefaultMfdZoomOut;
        internal static KeyboardShortcut MfdZoomReset = DefaultMfdZoomReset;
        internal static KeyboardShortcut FullscreenToggle = DefaultFullscreenToggle;
        internal static KeyboardShortcut VisionCycle = DefaultVisionCycle;
        internal static KeyboardShortcut FullscreenZoomReset = DefaultFullscreenZoomReset;
        internal static KeyboardShortcut AircraftCamCycle = DefaultAircraftCamCycle;

        internal static int Revision;

        // --- defaults (also used by Config.Bind) ---
        internal static readonly KeyboardShortcut DefaultNextMissile =
            new KeyboardShortcut(KeyCode.Slash, KeyCode.RightAlt);

        internal static readonly KeyboardShortcut DefaultPreviousMissile =
            new KeyboardShortcut(KeyCode.Comma, KeyCode.RightAlt);

        internal static readonly KeyboardShortcut DefaultMfdZoomIn =
            new KeyboardShortcut(KeyCode.Semicolon, KeyCode.RightAlt);

        internal static readonly KeyboardShortcut DefaultMfdZoomOut =
            new KeyboardShortcut(KeyCode.Period, KeyCode.RightAlt);

        internal static readonly KeyboardShortcut DefaultMfdZoomReset =
            new KeyboardShortcut(KeyCode.Period, KeyCode.RightShift);

        /// <summary>Fullscreen missile feed toggle — bare K (no modifiers).</summary>
        internal static readonly KeyboardShortcut DefaultFullscreenToggle =
            new KeyboardShortcut(KeyCode.K);

        internal static readonly KeyboardShortcut DefaultVisionCycle =
            new KeyboardShortcut(KeyCode.J);

        internal static readonly KeyboardShortcut DefaultFullscreenZoomReset =
            new KeyboardShortcut(KeyCode.Mouse2);

        internal static readonly KeyboardShortcut DefaultAircraftCamCycle =
            new KeyboardShortcut(KeyCode.V, KeyCode.RightAlt);

        internal static void Refresh(bool force = false)
        {
            if (!MissileCameraBepInConfig.IsBound)
                return;

            KeyboardShortcut next = MissileCameraBepInConfig.ControlsNextMissile.Value;
            KeyboardShortcut prev = MissileCameraBepInConfig.ControlsPreviousMissile.Value;
            KeyboardShortcut zoomIn = MissileCameraBepInConfig.ControlsZoomIn.Value;
            KeyboardShortcut zoomOut = MissileCameraBepInConfig.ControlsZoomOut.Value;
            KeyboardShortcut zoomReset = MissileCameraBepInConfig.ControlsResetZoom.Value;
            KeyboardShortcut fsToggle = MissileCameraBepInConfig.FullscreenToggle.Value;
            KeyboardShortcut vision = MissileCameraBepInConfig.FullscreenVisionCycle.Value;
            KeyboardShortcut fsZoomReset = MissileCameraBepInConfig.FullscreenZoomResetKey.Value;
            KeyboardShortcut acCycle = MissileCameraBepInConfig.AircraftCamCycle.Value;

            if (!force
                && next.Equals(NextMissile)
                && prev.Equals(PreviousMissile)
                && zoomIn.Equals(MfdZoomIn)
                && zoomOut.Equals(MfdZoomOut)
                && zoomReset.Equals(MfdZoomReset)
                && fsToggle.Equals(FullscreenToggle)
                && vision.Equals(VisionCycle)
                && fsZoomReset.Equals(FullscreenZoomReset)
                && acCycle.Equals(AircraftCamCycle))
                return;

            NextMissile = next;
            PreviousMissile = prev;
            MfdZoomIn = zoomIn;
            MfdZoomOut = zoomOut;
            MfdZoomReset = zoomReset;
            FullscreenToggle = fsToggle;
            VisionCycle = vision;
            FullscreenZoomReset = fsZoomReset;
            AircraftCamCycle = acCycle;
            Revision++;
        }
    }
}
