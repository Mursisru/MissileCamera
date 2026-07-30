using HarmonyLib;
using Rewired;

namespace MissileCamera.Patches
{
    [HarmonyPatch(typeof(Player), nameof(Player.GetAxis), typeof(string))]
    internal static class Rewired_Player_GetAxis_Patch
    {
        [HarmonyPrefix]
        internal static bool Prefix(string actionName, ref float __result)
        {
            if (!MissileCameraFullscreenInputGate.ShouldSuppressRewiredAxis(actionName))
                return true;

            __result = 0f;
            return false;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetAxisRaw), typeof(string))]
    internal static class Rewired_Player_GetAxisRaw_Patch
    {
        [HarmonyPrefix]
        internal static bool Prefix(string actionName, ref float __result)
        {
            if (!MissileCameraFullscreenInputGate.ShouldSuppressRewiredAxis(actionName))
                return true;

            __result = 0f;
            return false;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetButton), typeof(string))]
    internal static class Rewired_Player_GetButton_Patch
    {
        [HarmonyPrefix]
        internal static bool Prefix(string actionName, ref bool __result)
        {
            if (!MissileCameraFullscreenInputGate.ShouldSuppressRewiredButton(actionName))
                return true;

            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetButtonDown), typeof(string))]
    internal static class Rewired_Player_GetButtonDown_Patch
    {
        [HarmonyPrefix]
        internal static bool Prefix(string actionName, ref bool __result)
        {
            if (!MissileCameraFullscreenInputGate.ShouldSuppressRewiredButton(actionName))
                return true;

            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetButtonUp), typeof(string))]
    internal static class Rewired_Player_GetButtonUp_Patch
    {
        [HarmonyPrefix]
        internal static bool Prefix(string actionName, ref bool __result)
        {
            if (!MissileCameraFullscreenInputGate.ShouldSuppressRewiredButton(actionName))
                return true;

            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Player), "GetButtonTimedPressUp", typeof(string), typeof(float))]
    internal static class Rewired_Player_GetButtonTimedPressUp_2_Patch
    {
        [HarmonyPrefix]
        internal static bool Prefix(string actionName, ref bool __result)
        {
            if (!MissileCameraFullscreenInputGate.ShouldSuppressRewiredButton(actionName))
                return true;

            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Player), "GetButtonTimedPressUp", typeof(string), typeof(float), typeof(float))]
    internal static class Rewired_Player_GetButtonTimedPressUp_3_Patch
    {
        [HarmonyPrefix]
        internal static bool Prefix(string actionName, ref bool __result)
        {
            if (!MissileCameraFullscreenInputGate.ShouldSuppressRewiredButton(actionName))
                return true;

            __result = false;
            return false;
        }
    }
}
