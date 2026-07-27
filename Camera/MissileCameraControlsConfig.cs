using System;
using MissileCamera.Config;
using UnityEngine;

namespace MissileCamera
{
    internal static class MissileCameraControlsConfig
    {
        internal static bool Enabled = true;
        internal static KeyCode ModifierKey = KeyCode.RightAlt;
        internal static KeyCode NextMissileKey = KeyCode.Slash;
        internal static KeyCode PreviousMissileKey = KeyCode.Comma;
        internal static KeyCode ZoomInKey = KeyCode.Semicolon;
        internal static KeyCode ZoomOutKey = KeyCode.Period;
        internal static KeyCode ResetZoomModifierKey = KeyCode.RightShift;
        internal static KeyCode ResetZoomKey = KeyCode.Period;
        internal static float ZoomStep = 0.5f;
        internal static float ZoomMin = -4f;
        internal static float ZoomMax = 4f;
        internal static float ZoomFovDegreesPerUnit = 5f;
        internal static float IndicatorSeconds = 0.5f;
        internal static int Revision;

        internal static void Refresh(bool force = false)
        {
            if (!MissileCameraBepInConfig.IsBound)
                return;

            bool enabled = MissileCameraBepInConfig.ControlsEnabled.Value;
            KeyCode modifierKey = ParseKey(MissileCameraBepInConfig.ModifierKey.Value, KeyCode.RightAlt);
            KeyCode nextMissileKey = ParseKey(MissileCameraBepInConfig.NextMissileKey.Value, KeyCode.Slash);
            KeyCode previousMissileKey = ParseKey(MissileCameraBepInConfig.PreviousMissileKey.Value, KeyCode.Comma);
            KeyCode zoomInKey = ParseKey(MissileCameraBepInConfig.ZoomInKey.Value, KeyCode.Semicolon);
            KeyCode zoomOutKey = ParseKey(MissileCameraBepInConfig.ZoomOutKey.Value, KeyCode.Period);
            KeyCode resetZoomModifierKey = ParseKey(MissileCameraBepInConfig.ResetZoomModifierKey.Value, KeyCode.RightShift);
            KeyCode resetZoomKey = ParseKey(MissileCameraBepInConfig.ResetZoomKey.Value, KeyCode.Period);
            float zoomStep = MissileCameraBepInConfig.ZoomStep.Value;
            float zoomMin = MissileCameraBepInConfig.ZoomMin.Value;
            float zoomMax = MissileCameraBepInConfig.ZoomMax.Value;
            float zoomFovDegreesPerUnit = MissileCameraBepInConfig.ZoomFovDegreesPerUnit.Value;
            float indicatorSeconds = MissileCameraBepInConfig.IndicatorSeconds.Value;

            if (!force
                && enabled == Enabled
                && modifierKey == ModifierKey
                && nextMissileKey == NextMissileKey
                && previousMissileKey == PreviousMissileKey
                && zoomInKey == ZoomInKey
                && zoomOutKey == ZoomOutKey
                && resetZoomModifierKey == ResetZoomModifierKey
                && resetZoomKey == ResetZoomKey
                && zoomStep == ZoomStep
                && zoomMin == ZoomMin
                && zoomMax == ZoomMax
                && zoomFovDegreesPerUnit == ZoomFovDegreesPerUnit
                && indicatorSeconds == IndicatorSeconds)
                return;

            Enabled = enabled;
            ModifierKey = modifierKey;
            NextMissileKey = nextMissileKey;
            PreviousMissileKey = previousMissileKey;
            ZoomInKey = zoomInKey;
            ZoomOutKey = zoomOutKey;
            ResetZoomModifierKey = resetZoomModifierKey;
            ResetZoomKey = resetZoomKey;
            ZoomStep = zoomStep;
            ZoomMin = zoomMin;
            ZoomMax = zoomMax;
            ZoomFovDegreesPerUnit = zoomFovDegreesPerUnit;
            IndicatorSeconds = indicatorSeconds;
            Revision++;
        }

        internal static float ClampZoomOffset(float offset) =>
            offset < ZoomMin ? ZoomMin : offset > ZoomMax ? ZoomMax : offset;

        internal static float ComputeEffectiveFov(float baseFov, float zoomOffset)
        {
            float fov = baseFov - zoomOffset * ZoomFovDegreesPerUnit;
            return fov < 10f ? 10f : fov > 120f ? 120f : fov;
        }

        internal static KeyCode ParseKey(string? value, KeyCode fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            string trimmed = value.Trim();
            if (string.Equals(trimmed, "None", StringComparison.OrdinalIgnoreCase)
                || string.Equals(trimmed, "Off", StringComparison.OrdinalIgnoreCase)
                || string.Equals(trimmed, "Disabled", StringComparison.OrdinalIgnoreCase))
                return KeyCode.None;

            return Enum.TryParse(trimmed, ignoreCase: true, out KeyCode parsed)
                ? parsed
                : fallback;
        }
    }
}
