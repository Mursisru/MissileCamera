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

            ProcessMissileCycle();
        }

        private static void ProcessFullscreenZoom()
        {
            if (MissileCameraVisionModeController.IsBlockedByUi())
                return;

            if (Input.GetMouseButtonDown(2))
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
            if (Input.GetKey(KeyCode.RightShift) && Input.GetKeyDown(KeyCode.Period))
            {
                MissileCameraFeedController.ResetZoom();
                return;
            }

            if (!Input.GetKey(KeyCode.RightAlt))
                return;

            if (Input.GetKeyDown(KeyCode.Semicolon))
                MissileCameraFeedController.AdjustZoom(MissileCameraControlsConfig.ZoomStep);
            else if (Input.GetKeyDown(KeyCode.Period))
                MissileCameraFeedController.AdjustZoom(-MissileCameraControlsConfig.ZoomStep);
        }

        private static void ProcessMissileCycle()
        {
            if (!Input.GetKey(KeyCode.RightAlt))
                return;

            if (Input.GetKeyDown(KeyCode.Slash))
                MissileCameraFeedController.SelectNextMissile();
            else if (Input.GetKeyDown(KeyCode.Comma))
                MissileCameraFeedController.SelectPreviousMissile();
        }
    }
}
