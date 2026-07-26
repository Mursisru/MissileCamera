using UnityEngine;

namespace MissileCamera
{
    internal static class MissileCameraFeedInput
    {
        /// <summary>
        /// Fullscreen toggle only — called from CameraStateManager.LateUpdate Postfix
        /// before missile pose overlay, so Exit stops writing in the same LateUpdate.
        /// </summary>
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

            // Fullscreen toggle is handled in LateUpdate (ViewDriver.LateTick) — not here.

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

            if (!MissileCameraFeedController.HasOverlayInputContext()
                && !MissileCameraFullscreenController.IsActive)
                return;

            if (Input.GetKey(KeyCode.RightShift) && Input.GetKeyDown(KeyCode.Period))
            {
                MissileCameraFeedController.ResetZoom();
                return;
            }

            if (!Input.GetKey(KeyCode.RightAlt))
                return;

            if (Input.GetKeyDown(KeyCode.Slash))
                MissileCameraFeedController.SelectNextMissile();
            else if (Input.GetKeyDown(KeyCode.Comma))
                MissileCameraFeedController.SelectPreviousMissile();
            else if (Input.GetKeyDown(KeyCode.Semicolon))
                MissileCameraFeedController.AdjustZoom(MissileCameraControlsConfig.ZoomStep);
            else if (Input.GetKeyDown(KeyCode.Period))
                MissileCameraFeedController.AdjustZoom(-MissileCameraControlsConfig.ZoomStep);
        }
    }
}
