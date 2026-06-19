using UnityEngine;

namespace MissileCamera
{
    internal static class MissileCameraFeedInput
    {
        internal static void Process()
        {
            if (!MissileCameraControlsConfig.Enabled)
                return;

            if (!MissileCameraFeedController.HasOverlayInputContext())
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
