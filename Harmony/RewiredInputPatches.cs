using HarmonyLib;
using Rewired;

namespace MissileCamera.Patches
{
    // DISABLED: Rewired prefixes caused 2nd-mission / flight-control breakage.
    // Working MissileCamera-main has no Rewired patches. Keep classes so PatchAll is stable,
    // but every Prefix is a no-op pass-through.

    [HarmonyPatch(typeof(Player), nameof(Player.GetAxis), typeof(string))]
    internal static class Rewired_Player_GetAxis_Patch
    {
        [HarmonyPrefix]
        internal static bool Prefix() => true;
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetAxisRaw), typeof(string))]
    internal static class Rewired_Player_GetAxisRaw_Patch
    {
        [HarmonyPrefix]
        internal static bool Prefix() => true;
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetButton), typeof(string))]
    internal static class Rewired_Player_GetButton_Patch
    {
        [HarmonyPrefix]
        internal static bool Prefix() => true;
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetButtonDown), typeof(string))]
    internal static class Rewired_Player_GetButtonDown_Patch
    {
        [HarmonyPrefix]
        internal static bool Prefix() => true;
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetButtonUp), typeof(string))]
    internal static class Rewired_Player_GetButtonUp_Patch
    {
        [HarmonyPrefix]
        internal static bool Prefix() => true;
    }

    [HarmonyPatch(typeof(Player), "GetButtonTimedPressUp", typeof(string), typeof(float))]
    internal static class Rewired_Player_GetButtonTimedPressUp_2_Patch
    {
        [HarmonyPrefix]
        internal static bool Prefix() => true;
    }

    [HarmonyPatch(typeof(Player), "GetButtonTimedPressUp", typeof(string), typeof(float), typeof(float))]
    internal static class Rewired_Player_GetButtonTimedPressUp_3_Patch
    {
        [HarmonyPrefix]
        internal static bool Prefix() => true;
    }
}
