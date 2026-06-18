using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MissileCamera
{
    internal static class TacScreenAccess
    {
        private const BindingFlags InstanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly Dictionary<int, TacScreen> CachedByAircraftId = new Dictionary<int, TacScreen>();

        internal static void Register(Component aircraft, TacScreen tacScreen)
        {
            if (aircraft == null)
                return;

            CachedByAircraftId[aircraft.GetInstanceID()] = tacScreen;
        }

        internal static TacScreen? Resolve(Component? aircraft)
        {
            if (aircraft == null)
                return null;

            int id = aircraft.GetInstanceID();
            if (CachedByAircraftId.TryGetValue(id, out TacScreen cached) && cached != null)
                return cached;

            Transform root = aircraft.transform;
            TacScreen? fromCockpit = root.GetComponentInChildren<TacScreen>(true);
            if (fromCockpit != null)
            {
                CachedByAircraftId[id] = fromCockpit;
                return fromCockpit;
            }

            TacScreen? fromScene = FindTacScreenForAircraft(aircraft);
            if (fromScene != null)
            {
                CachedByAircraftId[id] = fromScene;
                return fromScene;
            }

            return null;
        }

        /// <summary>TacScreen often lives on cockpit canvas, not under Aircraft root (Initialize may predate Harmony).</summary>
        private static TacScreen? FindTacScreenForAircraft(Component aircraft)
        {
            int id = aircraft.GetInstanceID();
            TacScreen[] screens = Object.FindObjectsOfType<TacScreen>();
            for (int i = 0; i < screens.Length; i++)
            {
                TacScreen tac = screens[i];
                if (tac == null)
                    continue;

                Component? tacAircraft = GetAircraft(tac);
                if (tacAircraft == null)
                    continue;

                if (tacAircraft == aircraft || tacAircraft.GetInstanceID() == id)
                    return tac;
            }

            return null;
        }

        internal static GameObject GetMfdRoot(TacScreen instance) => instance.gameObject;

        /// <summary>
        /// Darkreach weapon-bay UI lives outside TacScreen (left bezel HUD), not on tacScreen_SFB.
        /// </summary>
        internal static GameObject ResolveDiscoveryRoot(TacScreen tacScreen, string? aircraftJsonKey)
        {
            GameObject tacRoot = tacScreen.gameObject;

            if (IsDarkreachAircraft(aircraftJsonKey))
            {
                Component? aircraft = GetAircraft(tacScreen);
                if (aircraft != null)
                {
                    GameObject? cockpitRoot = FindCockpitWeaponRoot(aircraft.transform, tacRoot);
                    if (cockpitRoot != null)
                    {
                        MfdLog.Info($"cockpitWeaponRoot={cockpitRoot.name} tacRoot={tacRoot.name}");
                        return cockpitRoot;
                    }
                }
            }

            if (IsIbisAircraft(aircraftJsonKey))
            {
                Component? aircraft = GetAircraft(tacScreen);
                if (aircraft != null)
                {
                    GameObject? cockpitRoot = FindIbisCockpitWeaponRoot(aircraft.transform, tacRoot);
                    if (cockpitRoot != null)
                    {
                        MfdLog.Info($"ibisWeaponRoot={cockpitRoot.name} tacRoot={tacRoot.name}");
                        return cockpitRoot;
                    }
                }
            }

            if (IsEngineSectionMfdAircraft(aircraftJsonKey))
            {
                Component? aircraft = GetAircraft(tacScreen);
                if (aircraft != null)
                {
                    GameObject? hudRoot = FindEngineSectionHudRoot(aircraft.transform, tacRoot, GetCanvas(tacScreen));
                    if (hudRoot != null)
                    {
                        MfdLog.Info($"engineHudRoot={hudRoot.name} tacRoot={tacRoot.name} jsonKey={aircraftJsonKey}");
                        return hudRoot;
                    }
                }
            }

            return tacRoot;
        }

        /// <summary>T/A-30 Compass (trainer), A-19 Brawler (CAS1), VL-49 Tarantula (QuadVTOL1).</summary>
        internal static bool IsEngineSectionMfdAircraft(string? jsonKey) =>
            string.Equals(jsonKey, "trainer", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(jsonKey, "CAS1", System.StringComparison.OrdinalIgnoreCase);

        internal static bool IsTarantulaAircraft(string? jsonKey) =>
            string.Equals(jsonKey, "QuadVTOL1", System.StringComparison.OrdinalIgnoreCase);

        /// <summary>SAH-46 Chicane attack helicopter.</summary>
        internal static bool IsChicaneAircraft(string? jsonKey) =>
            string.Equals(jsonKey, "AttackHelo1", System.StringComparison.OrdinalIgnoreCase);

        /// <summary>UH-90 Ibis utility helicopter.</summary>
        internal static bool IsIbisAircraft(string? jsonKey) =>
            string.Equals(jsonKey, "UtilityHelo1", System.StringComparison.OrdinalIgnoreCase);

        internal static bool IsCricketAircraft(string? jsonKey) =>
            string.Equals(jsonKey, "COIN", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(jsonKey, "Cricket", System.StringComparison.OrdinalIgnoreCase);

        internal static bool UsesEarlyTacLayoutTrigger(string? jsonKey) =>
            IsEngineSectionMfdAircraft(jsonKey)
            || IsTarantulaAircraft(jsonKey)
            || IsCricketAircraft(jsonKey)
            || IsIbisAircraft(jsonKey);

        /// <summary>CI-22 Cricket: shared cockpit canvas (MFDAppManager / TacScreen).</summary>
        internal static GameObject? FindCricketEngineMfdRoot(TacScreen tacScreen, Transform aircraftRoot)
        {
            Canvas? tacCanvas = GetCanvas(tacScreen);
            if (tacCanvas != null)
                return tacCanvas.gameObject;

            MFDAppManager? manager = MFDAppManager.i;
            if (manager != null)
            {
                Canvas? managerCanvas = manager.GetComponentInChildren<Canvas>(true);
                if (managerCanvas != null)
                    return managerCanvas.gameObject;

                return manager.gameObject;
            }

            foreach (PropGauge prop in aircraftRoot.GetComponentsInChildren<PropGauge>(true))
            {
                if (!prop.TryGetComponent(out RectTransform propRt))
                    continue;

                GameObject? root = GetHudCanvasRoot(propRt);
                if (root != null)
                    return root;
            }

            foreach (RPMGauge rpm in aircraftRoot.GetComponentsInChildren<RPMGauge>(true))
            {
                if (!rpm.TryGetComponent(out RectTransform rpmRt))
                    continue;

                GameObject? root = GetHudCanvasRoot(rpmRt);
                if (root != null)
                    return root;
            }

            foreach (EngineTelemetry telemetry in aircraftRoot.GetComponentsInChildren<EngineTelemetry>(true))
            {
                if (!telemetry.TryGetComponent(out RectTransform telemetryRt))
                    continue;

                GameObject? root = GetHudCanvasRoot(telemetryRt);
                if (root != null)
                    return root;
            }

            return null;
        }

        /// <summary>
        /// Compass / Brawler: weapon wireframe + ENGINE gauges often live on a cockpit HUD canvas,
        /// not under tacScreen_* (which only hosts TargetCam UI).
        /// </summary>
        private static GameObject? FindEngineSectionHudRoot(Transform aircraftRoot, GameObject tacRoot, Canvas? tacCanvas)
        {
            foreach (EngineTelemetry telemetry in aircraftRoot.GetComponentsInChildren<EngineTelemetry>(true))
            {
                if (telemetry == null || !telemetry.TryGetComponent(out RectTransform telemetryRt))
                    continue;

                if (IsLeftColumnUiNode(telemetryRt, tacCanvas))
                    continue;

                GameObject? root = GetHudCanvasRoot(telemetryRt);
                if (root != null)
                    return root;
            }

            RectTransform? weaponPanel = FindDescendantByName(aircraftRoot, "WeaponPanel")
                ?? FindDescendantByName(aircraftRoot, "weaponPanel")
                ?? FindDescendantByName(aircraftRoot, "weaponStations");
            if (weaponPanel != null)
            {
                GameObject? root = GetHudCanvasRoot(weaponPanel);
                if (root != null)
                    return root;
            }

            foreach (RPMGauge rpm in aircraftRoot.GetComponentsInChildren<RPMGauge>(true))
            {
                if (rpm == null || !rpm.TryGetComponent(out RectTransform gaugeRt))
                    continue;

                if (IsLeftColumnUiNode(gaugeRt, tacCanvas))
                    continue;

                GameObject? root = GetHudCanvasRoot(gaugeRt);
                if (root != null)
                    return root;
            }

            RectTransform? profile = FindDescendantByName(aircraftRoot, "frontProfile");
            if (profile != null && !IsLeftColumnUiNode(profile, tacCanvas))
            {
                GameObject? root = GetHudCanvasRoot(profile);
                if (root != null)
                    return root;
            }

            return null;
        }

        private static bool IsLeftColumnUiNode(RectTransform rt, Canvas? referenceCanvas)
        {
            if (referenceCanvas == null)
                return false;

            PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(rt, referenceCanvas);
            return zone.AnchorMax.x < 0.48f;
        }

        private static RectTransform? FindDescendantByName(Transform searchRoot, string objectName)
        {
            foreach (RectTransform rt in searchRoot.GetComponentsInChildren<RectTransform>(true))
            {
                if (string.Equals(rt.name, objectName, System.StringComparison.OrdinalIgnoreCase))
                    return rt;
            }

            return null;
        }

        internal static bool HasBomberBayMarkersForAircraft(TacScreen tacScreen, string? aircraftJsonKey)
        {
            if (IsDarkreachAircraft(aircraftJsonKey))
                return MfdWeaponsZoneAccess.HasBomberBayMarkers(tacScreen.gameObject);

            GameObject root = ResolveDiscoveryRoot(tacScreen, aircraftJsonKey);
            return MfdWeaponsZoneAccess.HasBomberBayMarkers(root);
        }

        internal static Canvas? GetCanvasForRoot(GameObject root)
        {
            Canvas? canvas = root.GetComponent<Canvas>();
            if (canvas != null)
                return canvas;

            return root.GetComponentInChildren<Canvas>(true);
        }

        internal static bool IsDedicatedWeaponMfdRoot(GameObject discoveryRoot, TacScreen tacScreen) =>
            discoveryRoot != tacScreen.gameObject;

        private static bool IsDarkreachAircraft(string? jsonKey) =>
            string.Equals(jsonKey, "Darkreach", System.StringComparison.OrdinalIgnoreCase);

        private static GameObject? FindIbisCockpitWeaponRoot(Transform aircraftRoot, GameObject tacRoot)
        {
            Transform tacTransform = tacRoot.transform;

            RectTransform? weaponPanel = FindDescendantOutside(aircraftRoot, tacTransform, "WeaponPanel")
                ?? FindDescendantOutside(aircraftRoot, tacTransform, "weaponPanel");
            if (weaponPanel != null && HasIbisWeaponStripLayout(weaponPanel))
            {
                GameObject? root = GetHudCanvasRoot(weaponPanel);
                if (root != null && root != tacRoot)
                    return root;
            }

            RectTransform? topView = FindDescendantOutside(aircraftRoot, tacTransform, "TopView");
            if (topView != null)
            {
                GameObject? root = GetHudCanvasRoot(topView);
                if (root != null && root != tacRoot)
                    return root;
            }

            return null;
        }

        private static bool HasIbisWeaponStripLayout(RectTransform weaponPanel)
        {
            if (weaponPanel.Find("TopView") != null || weaponPanel.Find("topView") != null)
                return true;

            int boxCount = 0;
            for (int i = 0; i < weaponPanel.childCount; i++)
            {
                string name = weaponPanel.GetChild(i).name;
                if (name.StartsWith("Box_", System.StringComparison.OrdinalIgnoreCase))
                    boxCount++;
            }

            return boxCount >= 2;
        }

        private static GameObject? FindCockpitWeaponRoot(Transform aircraftRoot, GameObject tacRoot)
        {
            Transform tacTransform = tacRoot.transform;

            RectTransform? profile = FindDescendantOutside(aircraftRoot, tacTransform, "rearProfile")
                ?? FindDescendantOutside(aircraftRoot, tacTransform, "frontProfile");
            if (profile != null)
            {
                GameObject? root = GetHudCanvasRoot(profile);
                if (root != null && root != tacRoot)
                    return root;
            }

            RectTransform? weaponStations = FindDescendantOutside(aircraftRoot, tacTransform, "weaponStations");
            if (weaponStations != null)
            {
                GameObject? root = GetHudCanvasRoot(weaponStations);
                if (root != null && root != tacRoot && MfdWeaponsZoneAccess.HasBomberBayMarkers(root))
                    return root;
            }

            RectTransform? weaponPanel = FindDescendantOutside(aircraftRoot, tacTransform, "WeaponPanel")
                ?? FindDescendantOutside(aircraftRoot, tacTransform, "weaponPanel");
            if (weaponPanel != null)
            {
                GameObject? root = GetHudCanvasRoot(weaponPanel);
                if (root != null && root != tacRoot && MfdWeaponsZoneAccess.HasBomberBayMarkers(root))
                    return root;
            }

            foreach (UnityEngine.UI.Text label in aircraftRoot.GetComponentsInChildren<UnityEngine.UI.Text>(true))
            {
                if (label == null || !MfdWeaponsZoneAccess.IsBomberBayMarkerText(label.text))
                    continue;

                if (!IsOutsideTac(label.rectTransform, tacTransform))
                    continue;

                GameObject? root = GetHudCanvasRoot(label.rectTransform);
                if (root != null && root != tacRoot)
                    return root;
            }

            return null;
        }

        private static bool IsOutsideTac(RectTransform rt, Transform tacTransform) =>
            rt != null && !rt.IsChildOf(tacTransform);

        private static RectTransform? FindDescendantOutside(Transform searchRoot, Transform tacTransform, string objectName)
        {
            foreach (RectTransform rt in searchRoot.GetComponentsInChildren<RectTransform>(true))
            {
                if (!IsOutsideTac(rt, tacTransform))
                    continue;

                if (string.Equals(rt.name, objectName, System.StringComparison.OrdinalIgnoreCase))
                    return rt;
            }

            return null;
        }

        private static GameObject? GetHudCanvasRoot(RectTransform node)
        {
            Canvas? canvas = GetOverlayCanvas(node);
            return canvas != null ? canvas.gameObject : null;
        }

        internal static Canvas? GetOverlayCanvas(RectTransform panel)
        {
            Transform? current = panel.transform;
            while (current != null)
            {
                Canvas? canvas = current.GetComponent<Canvas>();
                if (canvas != null)
                    return canvas;

                current = current.parent;
            }

            return null;
        }

        internal static GameObject? GetTargetCamDisplay(TacScreen instance)
        {
            FieldInfo? field = typeof(TacScreen).GetField("targetCamDisplay", InstanceNonPublic);
            return field?.GetValue(instance) as GameObject;
        }

        internal static Canvas? GetCanvas(TacScreen instance)
        {
            FieldInfo? field = typeof(TacScreen).GetField("canvas", InstanceNonPublic);
            return field?.GetValue(instance) as Canvas;
        }

        internal static Component? GetAircraft(TacScreen instance)
        {
            FieldInfo? field = typeof(TacScreen).GetField("aircraft", InstanceNonPublic);
            return field?.GetValue(instance) as Component;
        }
    }
}
