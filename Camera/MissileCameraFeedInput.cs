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

            // Same path as pre-config hardcodes: UnityEngine.Input (not BepInEx KeyboardShortcut).
            if (IsComboDown(
                    MissileCameraControlsConfig.ResetZoomModifierKey,
                    MissileCameraControlsConfig.ResetZoomKey))
            {
                MissileCameraFeedController.ResetZoom();
                return;
            }

            if (!IsModifierHeld(MissileCameraControlsConfig.ModifierKey))
                return;

            if (IsActionDown(MissileCameraControlsConfig.NextMissileKey))
                MissileCameraFeedController.SelectNextMissile();
            else if (IsActionDown(MissileCameraControlsConfig.PreviousMissileKey))
                MissileCameraFeedController.SelectPreviousMissile();
            else if (IsActionDown(MissileCameraControlsConfig.ZoomInKey))
                MissileCameraFeedController.AdjustZoom(MissileCameraControlsConfig.ZoomStep);
            else if (IsActionDown(MissileCameraControlsConfig.ZoomOutKey))
                MissileCameraFeedController.AdjustZoom(-MissileCameraControlsConfig.ZoomStep);
        }

        private static bool IsModifierHeld(KeyCode modifier) =>
            modifier == KeyCode.None || Input.GetKey(modifier);

        private static bool IsActionDown(KeyCode action) =>
            action != KeyCode.None && Input.GetKeyDown(action);

        private static bool IsComboDown(KeyCode modifier, KeyCode action) =>
            IsActionDown(action) && IsModifierHeld(modifier);
    }
}
