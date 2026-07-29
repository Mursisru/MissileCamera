using System;
using UnityEngine;

namespace MissileCamera
{
    /// <summary>
    /// Fullscreen: swallow vanilla Rewired only for mod-owned inputs (wheel + RShift/RCtrl/RAlt + , . / ;).
    /// MissileCamera reads UnityEngine.Input directly and is unaffected.
    /// </summary>
    internal static class MissileCameraFullscreenInputGate
    {
        private const float ScrollGateThreshold = 0.01f;

        private static readonly StringComparer ActionComparer = StringComparer.Ordinal;

        internal static bool IsFullscreenContext =>
            MissileCameraFullscreenController.IsActive || MissileCameraFullscreenController.IsDeferredExit;

        internal static bool ShouldSuppressRewiredAxis(string actionName)
        {
            if (!IsFullscreenContext)
                return false;

            if (IsWheelGateActive() && IsZoomAxis(actionName))
                return true;

            return IsKeyGateActive();
        }

        internal static bool ShouldSuppressRewiredButton(string actionName)
        {
            if (!IsFullscreenContext)
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
    }
}
