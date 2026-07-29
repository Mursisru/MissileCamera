using System;
using UnityEngine;

namespace MissileCamera
{
    /// <summary>
    /// Fullscreen: block only vanilla Zoom/FOV Rewired actions while mod zoom keys/wheel are active.
    /// Never zero pitch/roll/throttle — that made aircraft "fall" when RAlt/etc was held in FS.
    /// </summary>
    internal static class MissileCameraFullscreenInputGate
    {
        private const float ScrollGateThreshold = 0.01f;

        private static readonly StringComparer ActionComparer = StringComparer.Ordinal;

        internal static bool IsFullscreenContext =>
            MissileCameraFullscreenController.IsActive || MissileCameraFullscreenController.IsDeferredExit;

        internal static bool ShouldSuppressRewiredAxis(string actionName)
        {
            if (!IsFullscreenContext || !IsZoomAxis(actionName))
                return false;

            return IsWheelGateActive() || IsKeyGateActive();
        }

        internal static bool ShouldSuppressRewiredButton(string actionName)
        {
            if (!IsFullscreenContext || !IsZoomButton(actionName))
                return false;

            return IsKeyGateActive();
        }

        private static bool IsWheelGateActive() =>
            Mathf.Abs(Input.mouseScrollDelta.y) > ScrollGateThreshold;

        private static bool IsKeyGateActive() =>
            Input.GetKey(KeyCode.RightShift)
            || Input.GetKey(KeyCode.RightControl)
            || Input.GetKey(KeyCode.RightAlt)
            || Input.GetKey(KeyCode.Comma)
            || Input.GetKey(KeyCode.Period)
            || Input.GetKey(KeyCode.Slash)
            || Input.GetKey(KeyCode.Semicolon);

        private static bool IsZoomAxis(string actionName)
        {
            if (string.IsNullOrEmpty(actionName))
                return false;

            return ActionComparer.Equals(actionName, "Zoom View")
                || ActionComparer.Equals(actionName, "FOV");
        }

        private static bool IsZoomButton(string actionName)
        {
            if (string.IsNullOrEmpty(actionName))
                return false;

            return ActionComparer.Equals(actionName, "Zoom View")
                || ActionComparer.Equals(actionName, "FOV")
                || ActionComparer.Equals(actionName, "Zoom In")
                || ActionComparer.Equals(actionName, "Zoom Out");
        }
    }
}
