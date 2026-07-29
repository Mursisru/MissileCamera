using BepInEx.Configuration;
using UnityEngine;

namespace MissileCamera.Config
{
    /// <summary>Runtime cache for MFD/control KeyboardShortcut entries (1 Hz refresh).</summary>
    internal static class MissileCameraKeybindConfig
    {
        internal static KeyboardShortcut NextMissile = DefaultNextMissile;
        internal static KeyboardShortcut PreviousMissile = DefaultPreviousMissile;
        internal static KeyboardShortcut MfdZoomIn = DefaultMfdZoomIn;
        internal static KeyboardShortcut MfdZoomOut = DefaultMfdZoomOut;
        internal static KeyboardShortcut MfdZoomReset = DefaultMfdZoomReset;
        internal static KeyboardShortcut FullscreenZoomReset = DefaultFullscreenZoomReset;

        internal static int Revision;

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

        internal static readonly KeyboardShortcut DefaultFullscreenZoomReset =
            new KeyboardShortcut(KeyCode.Mouse2);

        internal static void Refresh(bool force = false)
        {
            if (!MissileCameraBepInConfig.IsBound)
                return;

            KeyboardShortcut next = MissileCameraBepInConfig.ControlsNextMissile.Value;
            KeyboardShortcut prev = MissileCameraBepInConfig.ControlsPreviousMissile.Value;
            KeyboardShortcut zoomIn = MissileCameraBepInConfig.ControlsZoomIn.Value;
            KeyboardShortcut zoomOut = MissileCameraBepInConfig.ControlsZoomOut.Value;
            KeyboardShortcut zoomReset = MissileCameraBepInConfig.ControlsResetZoom.Value;
            KeyboardShortcut fsZoomReset = MissileCameraBepInConfig.FullscreenZoomResetKey.Value;

            if (!force
                && next.Equals(NextMissile)
                && prev.Equals(PreviousMissile)
                && zoomIn.Equals(MfdZoomIn)
                && zoomOut.Equals(MfdZoomOut)
                && zoomReset.Equals(MfdZoomReset)
                && fsZoomReset.Equals(FullscreenZoomReset))
                return;

            NextMissile = next;
            PreviousMissile = prev;
            MfdZoomIn = zoomIn;
            MfdZoomOut = zoomOut;
            MfdZoomReset = zoomReset;
            FullscreenZoomReset = fsZoomReset;
            Revision++;
        }
    }
}
