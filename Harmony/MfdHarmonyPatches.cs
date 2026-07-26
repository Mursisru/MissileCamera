using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MissileCamera.Config;
using UnityEngine;

namespace MissileCamera.Patches
{
    internal static class GameAssembly
    {
        internal static Assembly CSharp
        {
            get
            {
                Assembly? asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, "Assembly-CSharp", StringComparison.Ordinal));
                if (asm == null)
                    throw new InvalidOperationException("Assembly-CSharp is not loaded.");
                return asm;
            }
        }

        internal static Type RequireType(string name) =>
            CSharp.GetType(name) ?? throw new InvalidOperationException("Type not found: " + name);
    }

    [HarmonyPatch]
    internal static class TargetScreenUI_SetupCamera_Patch
    {
        internal static MethodBase TargetMethod() =>
            AccessTools.Method(GameAssembly.RequireType("TargetScreenUI"), "SetupCamera")
            ?? throw new InvalidOperationException("TargetScreenUI.SetupCamera not found.");

        [HarmonyPostfix]
        internal static void Postfix(TargetScreenUI __instance, Camera cam, Camera UICam, Aircraft aircraft) =>
            MfdHarmonyHooks.SetupCameraPostfix(__instance, cam, UICam, aircraft!);
    }

    [HarmonyPatch]
    internal static class TargetCam_SetLandingCam_Patch
    {
        internal static MethodBase TargetMethod() =>
            AccessTools.Method(GameAssembly.RequireType("TargetCam"), "SetLandingCam")
            ?? throw new InvalidOperationException("TargetCam.SetLandingCam not found.");

        [HarmonyPostfix]
        internal static void Postfix(TargetCam __instance) =>
            MfdHarmonyHooks.SetLandingCamPostfix(__instance);
    }

    [HarmonyPatch]
    internal static class TargetCam_CancelTarget_Patch
    {
        internal static MethodBase TargetMethod() =>
            AccessTools.Method(GameAssembly.RequireType("TargetCam"), "CancelTarget")
            ?? throw new InvalidOperationException("TargetCam.CancelTarget not found.");

        [HarmonyPostfix]
        internal static void Postfix(TargetCam __instance) =>
            MfdHarmonyHooks.CancelTargetPostfix(__instance);
    }

    [HarmonyPatch]
    internal static class TargetCam_OnDestroy_Patch
    {
        internal static MethodBase TargetMethod() =>
            AccessTools.Method(GameAssembly.RequireType("TargetCam"), "OnDestroy")
            ?? throw new InvalidOperationException("TargetCam.OnDestroy not found.");

        [HarmonyPostfix]
        internal static void Postfix(TargetCam __instance) =>
            MfdHarmonyHooks.OnDestroyPostfix(__instance);
    }

    [HarmonyPatch]
    internal static class TacScreen_Initialize_Patch
    {
        internal static MethodBase TargetMethod() =>
            AccessTools.Method(GameAssembly.RequireType("TacScreen"), "Initialize")
            ?? throw new InvalidOperationException("TacScreen.Initialize not found.");

        [HarmonyPostfix]
        internal static void Postfix(TacScreen __instance) =>
            MfdHarmonyHooks.TacScreenInitializePostfix(__instance);
    }

    [HarmonyPatch]
    internal static class TacScreen_OnCamToggle_Patch
    {
        internal static MethodBase TargetMethod() =>
            AccessTools.Method(GameAssembly.RequireType("TacScreen"), "TacScreen_OnCamToggle")
            ?? throw new InvalidOperationException("TacScreen.TacScreen_OnCamToggle not found.");

        [HarmonyPostfix]
        internal static void Postfix(TacScreen __instance, TargetCam.OnCamToggle e) =>
            MfdHarmonyHooks.TacScreenOnCamTogglePostfix(__instance, e);
    }

    [HarmonyPatch]
    internal static class WeaponManager_TargetListChanged_Patch
    {
        internal static MethodBase TargetMethod() =>
            AccessTools.Method(GameAssembly.RequireType("WeaponManager"), "TargetListChanged")
            ?? throw new InvalidOperationException("WeaponManager.TargetListChanged not found.");

        [HarmonyPostfix]
        internal static void Postfix(WeaponManager __instance) =>
            MfdHarmonyHooks.TargetListChangedPostfix(__instance);
    }

    /// <summary>
    /// After vanilla UpdateState snaps cockpit, optionally overlay missile nose pose.
    /// Never blocks UpdateState — that caused sticky missile camera on exit.
    /// </summary>
    [HarmonyPatch]
    internal static class CameraStateManager_LateUpdate_Patch
    {
        internal static MethodBase TargetMethod() =>
            AccessTools.Method(GameAssembly.RequireType("CameraStateManager"), "LateUpdate")
            ?? throw new InvalidOperationException("CameraStateManager.LateUpdate not found.");

        [HarmonyPostfix]
        internal static void Postfix()
        {
            MissileCameraFullscreenViewDriver.LateTick();
            MissileCameraVanillaHudBridge.LateTickMarkers();
        }
    }
}
