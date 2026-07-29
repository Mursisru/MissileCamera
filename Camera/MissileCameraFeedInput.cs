using BepInEx.Configuration;
using MissileCamera.Config;
using UnityEngine;

namespace MissileCamera
{
    internal static class MissileCameraFeedInput
    {
        internal static void ProcessFullscreenToggle()
        {
            if (!MissileCameraFullscreenConfig.Enabled)
                return;

            MissileCameraFullscreenConfig.Refresh();
            bool altOk = !MissileCameraFullscreenConfig.RequireRightAlt || Input.GetKey(KeyCode.RightAlt);
            if (altOk && Input.GetKeyDown(MissileCameraFullscreenConfig.ToggleKey))
                MissileCameraFullscreenController.Toggle();
        }

        internal static void Process()
        {
            if (!MissileCameraControlsConfig.Enabled && !MissileCameraFullscreenConfig.Enabled)
                return;

            MissileCameraFullscreenConfig.Refresh();
            MissileCameraAircraftCamConfig.Refresh();
            MissileCameraControlsConfig.Refresh();
            MissileCameraKeybindConfig.Refresh();

            ProcessFullscreenToggle();

            if (!MissileCameraControlsConfig.Enabled)
                return;

            if (MissileCameraAircraftCamConfig.Enabled)
            {
                bool altOk = !MissileCameraAircraftCamConfig.RequireRightAlt || Input.GetKey(KeyCode.RightAlt);
                if (altOk && Input.GetKeyDown(MissileCameraAircraftCamConfig.CycleKey))
                {
                    MissileCameraAircraftCamController.CycleMode();
                    return;
                }
            }

            bool fullscreen = MissileCameraFullscreenController.IsActive;
            if (!MissileCameraFeedController.HasOverlayInputContext() && !fullscreen)
                return;

            if (fullscreen)
            {
                ProcessFullscreenZoom();
                ProcessFullscreenVision();
            }
            else
            {
                ProcessMfdZoomKeys();
            }

            if (fullscreen || MissileCameraFeedController.HasOverlayInputContext())
                ProcessMissileCycle();
        }

        private static void ProcessFullscreenZoom()
        {
            if (MissileCameraVisionModeController.IsBlockedByUi())
                return;

            if (IsShortcutDown(MissileCameraKeybindConfig.FullscreenZoomReset))
            {
                MissileCameraFeedController.ResetFullscreenMagnification();
                return;
            }

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) < 0.01f)
                return;

            float factor = MissileCameraFullscreenConfig.ZoomWheelFactor;
            if (factor < 1.01f)
                factor = 1.12f;

            float mul = scroll > 0f ? factor : 1f / factor;
            MissileCameraFeedController.MultiplyFullscreenMagnification(mul);
        }

        private static void ProcessFullscreenVision()
        {
            if (MissileCameraVisionModeController.IsBlockedByUi())
                return;

            if (Input.GetKeyDown(MissileCameraFullscreenConfig.VisionCycleKey))
                MissileCameraVisionModeController.Cycle();
        }

        private static void ProcessMfdZoomKeys()
        {
            if (IsShortcutDown(MissileCameraKeybindConfig.MfdZoomReset))
            {
                MissileCameraFeedController.ResetZoom();
                return;
            }

            if (IsShortcutDown(MissileCameraKeybindConfig.MfdZoomIn))
                MissileCameraFeedController.AdjustZoom(MissileCameraControlsConfig.ZoomStep);
            else if (IsShortcutDown(MissileCameraKeybindConfig.MfdZoomOut))
                MissileCameraFeedController.AdjustZoom(-MissileCameraControlsConfig.ZoomStep);
        }

        private static void ProcessMissileCycle()
        {
            if (IsShortcutDown(MissileCameraKeybindConfig.NextMissile))
                MissileCameraFeedController.SelectNextMissile();
            else if (IsShortcutDown(MissileCameraKeybindConfig.PreviousMissile))
                MissileCameraFeedController.SelectPreviousMissile();
        }

        /// <summary>
        /// UnityEngine.Input path (same as working main/GitHub hardcodes).
        /// Do not use BepInEx KeyboardShortcut.IsDown/IsPressed — UnityInput.Current misses keys under Rewired.
        /// </summary>
        private static bool IsShortcutDown(KeyboardShortcut shortcut)
        {
            KeyCode main = shortcut.MainKey;
            if (main == KeyCode.None)
                return false;

            if (!Input.GetKeyDown(main))
                return false;

            foreach (KeyCode mod in shortcut.Modifiers)
            {
                if (mod == KeyCode.None)
                    continue;
                if (!Input.GetKey(mod))
                    return false;
            }

            return true;
        }
    }
}
