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
    /// After vanilla LateUpdate: markers HUD suppress only.
    /// Never write CameraStateManager camera pose (CAMERA_SAFETY.md).
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
            try
            {
                // Markers-only HUD suppress. Never call camera pose writers here (CAMERA_SAFETY.md).
                MissileCameraFullscreenController.HealIfOrphaned();
                MissileCameraVanillaHudBridge.LateTickMarkers();
            }
            catch (Exception ex)
            {
                MfdLog.Info("LateTickMarkers failed: " + ex.Message);
            }
        }
    }

    /// <summary>
    /// Fullscreen: vanilla UpdatePosition projects via cockpit mainCamera (center-stuck).
    /// Reproject onto seeker feed viewport → Overlay screen. CSM untouched.
    /// Opaque contrast after every vanilla color write.
    /// </summary>
    [HarmonyPatch]
    internal static class HUDUnitMarker_UpdatePosition_Patch
    {
        internal static MethodBase TargetMethod() =>
            AccessTools.Method(GameAssembly.RequireType("HUDUnitMarker"), "UpdatePosition")
            ?? throw new InvalidOperationException("HUDUnitMarker.UpdatePosition not found.");

        [HarmonyPostfix]
        internal static void Postfix(object __instance)
        {
            if (__instance is not HUDUnitMarker marker)
                return;

            // Reproject + opaque contrast are FS-only — never touch cockpit glass markers.
            if (!MissileCameraFullscreenController.IsActive)
                return;

            MissileCameraCombatHudMarkerProjection.ReprojectIfFullscreen(marker);
        }
    }

    [HarmonyPatch]
    internal static class HUDUnitMarker_UpdateColor_Patch
    {
        internal static MethodBase TargetMethod() =>
            AccessTools.Method(GameAssembly.RequireType("HUDUnitMarker"), "UpdateColor")
            ?? throw new InvalidOperationException("HUDUnitMarker.UpdateColor not found.");

        [HarmonyPostfix]
        internal static void Postfix(object __instance)
        {
            // No opaque contrast — leave vanilla faction colors alone.
        }
    }

    [HarmonyPatch]
    internal static class HUDUnitMarker_JammingDistortion_Patch
    {
        internal static MethodBase TargetMethod() =>
            AccessTools.Method(GameAssembly.RequireType("HUDUnitMarker"), "JammingDistortion")
            ?? throw new InvalidOperationException("HUDUnitMarker.JammingDistortion not found.");

        [HarmonyPostfix]
        internal static void Postfix(object __instance)
        {
            // No opaque contrast — leave vanilla faction colors alone.
        }
    }

    /// <summary>
    /// When CombatHUD.aircraft is null, vanilla LateUpdate returns early — keep existing markers
    /// ticking in FS only (ctor still needs aircraft for new CreateMarker).
    /// </summary>
    [HarmonyPatch]
    internal static class CombatHUD_LateUpdate_Patch
    {
        internal static MethodBase TargetMethod() =>
            AccessTools.Method(GameAssembly.RequireType("CombatHUD"), "LateUpdate")
            ?? throw new InvalidOperationException("CombatHUD.LateUpdate not found.");

        [HarmonyPrefix]
        internal static bool Prefix(CombatHUD __instance)
        {
            if (__instance == null || __instance.aircraft != null)
                return true;

            if (!MissileCameraFullscreenController.IsActive)
                return true;

            try
            {
                MissileCameraVanillaHudBridge.ForceCombatHudMarkerPass();
            }
            catch
            {
                // ignore
            }

            return false;
        }
    }

    /// <summary>
    /// CreateMarker no-ops without aircraft; HUDUnitMarker ctor also needs aircraft.
    /// FS: temporarily bind missile owner / HQ aircraft as proxy for the call.
    /// </summary>
    [HarmonyPatch]
    internal static class CombatHUD_CreateMarker_Patch
    {
        internal static MethodBase TargetMethod() =>
            AccessTools.Method(GameAssembly.RequireType("CombatHUD"), "CreateMarker")
            ?? throw new InvalidOperationException("CombatHUD.CreateMarker not found.");

        [HarmonyPrefix]
        internal static void Prefix(CombatHUD __instance, ref Aircraft? __state)
        {
            __state = null;
            if (__instance == null || __instance.aircraft != null)
                return;

            if (!MissileCameraFullscreenController.IsActive)
                return;

            Aircraft? proxy = MissileCameraVanillaHudBridge.TryResolveMarkerAircraftProxy();
            if (proxy == null)
                return;

            __instance.aircraft = proxy;
            __state = proxy;
        }

        [HarmonyPostfix]
        internal static void Postfix(CombatHUD __instance, Aircraft? __state)
        {
            if (__state == null || __instance == null)
                return;

            if (ReferenceEquals(__instance.aircraft, __state))
                __instance.aircraft = null;
        }
    }
}
