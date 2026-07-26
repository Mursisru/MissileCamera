using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// Fullscreen: ONLY CombatHUD unit markers (HUDUnitMarker on iconLayer).
    /// Dump: FlightHud.Update re-enables velocityVector; ObjectiveOverlay.UpdateOverlay re-enables mission pointers;
    /// targetDesignator is the aiming pipper — hide Image, keep GO for TargetSelect position.
    /// </summary>
    internal static class MissileCameraVanillaHudBridge
    {
        private const int MarkersSortingOrder = 120;

        private static readonly FieldInfo? TopRightPanelField =
            AccessToolsField(typeof(CombatHUD), "topRightPanel");
        private static readonly FieldInfo? ThreatListField =
            AccessToolsField(typeof(CombatHUD), "threatList");
        private static readonly FieldInfo? WeaponStatusField =
            AccessToolsField(typeof(CombatHUD), "weaponStatus");
        private static readonly FieldInfo? CountermeasureBgField =
            AccessToolsField(typeof(CombatHUD), "countermeasureBackground");
        private static readonly FieldInfo? TargetInfoField =
            AccessToolsField(typeof(CombatHUD), "targetInfo");
        private static readonly FieldInfo? TargetArrowField =
            AccessToolsField(typeof(CombatHUD), "targetArrow");
        private static readonly FieldInfo? TargetTextField =
            AccessToolsField(typeof(CombatHUD), "targetText");
        private static readonly FieldInfo? WeaponStateField =
            AccessToolsField(typeof(CombatHUD), "weaponState");
        private static readonly FieldInfo? ObjectiveOverlayField =
            AccessToolsField(typeof(CombatHUD), "objectiveOverlay");
        private static readonly FieldInfo? MarkersField =
            AccessToolsField(typeof(CombatHUD), "markers");
        private static readonly FieldInfo? HitMarkersField =
            AccessToolsField(typeof(CombatHUD), "hitMarkers");
        private static readonly FieldInfo? FlightHudCanvasField =
            AccessToolsField(typeof(FlightHud), "canvas");
        private static readonly FieldInfo? FlightHudCenterField =
            AccessToolsField(typeof(FlightHud), "HUDCenter");
        private static readonly FieldInfo? ObjectiveOverlaysField =
            AccessToolsField(typeof(ObjectiveOverlayManager), "overlays");
        private static readonly MethodInfo? ObjectiveHideOverlayMethod =
            HarmonyLib.AccessTools.Method(typeof(ObjectiveOverlay), "HideOverlay");
        private static readonly MethodInfo? UpdateMarkersMethod =
            HarmonyLib.AccessTools.Method(typeof(CombatHUD), "UpdateMarkers");

        private static bool _flightHudWasActive;
        private static bool _mapWasActive;
        private static bool _objectiveMgrWasEnabled;
        private static bool _designatorWasEnabled;
        private static readonly List<(GameObject go, bool wasActive)> _hiddenChrome =
            new List<(GameObject, bool)>(64);
        private static readonly HashSet<Transform> _unitMarkerKeep = new HashSet<Transform>();

        private static Canvas? _combatCanvas;
        private static bool _canvasElevated;
        private static RenderMode _savedRenderMode;
        private static int _savedSortingOrder;
        private static bool _savedOverrideSorting;
        private static Camera? _savedWorldCamera;
        private static bool _savedPixelPerfect;

        internal static void OnFullscreenEntered()
        {
            _hiddenChrome.Clear();
            _unitMarkerKeep.Clear();
            _objectiveMgrWasEnabled = false;
            _designatorWasEnabled = false;

            HideStubsOnMissilePanel();
            SoftHideDynamicMap();
            ElevateCombatHudCanvas();
            ApplyMarkersOnlyVisibility();
            SuppressIlsAndObjectives();
            ForceCombatHudMarkerPass();
            MfdLog.Info("fullscreen markers-only"
                + (_canvasElevated ? " canvas↑" : " canvas miss")
                + $" hidden={_hiddenChrome.Count}");
        }

        internal static void OnFullscreenExited()
        {
            RestoreObjectiveManager();
            RestoreDesignatorVisual();
            RestoreCombatHudCanvas();
            RestoreHiddenChrome();
            RestoreFlightHud();
            RestoreDynamicMap();
            ForceCombatHudMarkerPass();

            _flightHudWasActive = false;
            _mapWasActive = false;
            _unitMarkerKeep.Clear();
        }

        internal static void ResetForMissionUnload()
        {
            RestoreObjectiveManager();
            RestoreDesignatorVisual();
            RestoreCombatHudCanvas();
            RestoreHiddenChrome();
            _flightHudWasActive = false;
            _mapWasActive = false;
            _unitMarkerKeep.Clear();
        }

        internal static void TickHideStubs()
        {
            if (!MissileCameraFullscreenController.IsActive)
                return;

            HideStubsOnMissilePanel();
        }

        internal static void LateTickMarkers()
        {
            if (!MissileCameraFullscreenController.IsActive)
                return;

            // FlightHud / ObjectiveOverlay re-enable themselves in Update — suppress every LateUpdate.
            SuppressIlsAndObjectives();
            ForceCombatHudMarkerPass();
        }

        private static void SoftHideDynamicMap()
        {
            try
            {
                DynamicMap? map = SceneSingleton<DynamicMap>.i;
                _mapWasActive = IsCanvasActive(map, "mapCanvas");
                DynamicMap.EnableCanvas(false);
            }
            catch
            {
                _mapWasActive = false;
            }
        }

        private static void RestoreFlightHud()
        {
            try
            {
                if (_flightHudWasActive
                    || (SceneSingleton<CombatHUD>.i?.aircraft != null
                        && CameraStateManager.cameraMode == CameraMode.cockpit))
                {
                    FlightHud.EnableCanvas(true);
                }
            }
            catch
            {
                // ignore
            }
        }

        private static void RestoreDynamicMap()
        {
            try
            {
                if (_mapWasActive
                    || (SceneSingleton<CombatHUD>.i?.aircraft != null
                        && CameraStateManager.cameraMode == CameraMode.cockpit))
                {
                    DynamicMap.EnableCanvas(true);
                }
            }
            catch
            {
                // ignore
            }
        }

        private static void ApplyMarkersOnlyVisibility()
        {
            CombatHUD? hud = SceneSingleton<CombatHUD>.i;
            if (hud == null)
                return;

            // Keep ONLY iconLayer in the branch walk — designator/arrow are visuals to kill.
            var keep = new HashSet<Transform>();
            AddKeep(keep, hud.iconLayer);

            try
            {
                FlightHud? flightHud = SceneSingleton<FlightHud>.i;
                _flightHudWasActive = IsCanvasActive(flightHud, "canvas");
                Canvas? flightCanvas = flightHud != null
                    ? FlightHudCanvasField?.GetValue(flightHud) as Canvas
                    : null;
                Canvas? combatCanvas = _combatCanvas ?? ResolveCombatCanvas(hud);

                if (flightCanvas != null && combatCanvas != null && flightCanvas == combatCanvas)
                {
                    HideBranchesExcept(flightCanvas.transform, keep);
                }
                else if (_flightHudWasActive)
                {
                    FlightHud.EnableCanvas(false);
                }
            }
            catch
            {
                _flightHudWasActive = false;
            }

            try
            {
                HeadMountedDisplay? hmd = SceneSingleton<HeadMountedDisplay>.i;
                if (hmd != null)
                    HideGo(hmd.gameObject);
            }
            catch
            {
                // ignore
            }

            try
            {
                HUDAppManager? apps = SceneSingleton<HUDAppManager>.i;
                if (apps != null)
                    HideGo(apps.gameObject);
            }
            catch
            {
                // ignore
            }

            if (_combatCanvas != null)
                HideBranchesExcept(_combatCanvas.transform, keep);

            HideGo(TopRightPanelField?.GetValue(hud) as GameObject);
            HideComponent(ThreatListField?.GetValue(hud) as Component);
            HideComponent(WeaponStatusField?.GetValue(hud) as Component);
            HideGo(CountermeasureBgField?.GetValue(hud) as GameObject);
            HideComponent(TargetInfoField?.GetValue(hud) as Component);
            HideComponent(WeaponStateField?.GetValue(hud) as Component);

            if (TargetArrowField?.GetValue(hud) is Image arrow && arrow != null)
                HideGo(arrow.gameObject);
            if (TargetTextField?.GetValue(hud) is Text arrowText && arrowText != null)
                HideGo(arrowText.gameObject);

            // Aiming pipper: keep transform alive for TargetSelect, kill Image draw.
            if (hud.targetDesignator != null)
            {
                _designatorWasEnabled = hud.targetDesignator.enabled;
                hud.targetDesignator.enabled = false;
                Color c = hud.targetDesignator.color;
                c.a = 0f;
                hud.targetDesignator.color = c;
            }

            if (hud.iconLayer != null)
                hud.iconLayer.gameObject.SetActive(true);

            DisableObjectiveManager(hud);
        }

        private static void DisableObjectiveManager(CombatHUD hud)
        {
            if (ObjectiveOverlayField?.GetValue(hud) is not ObjectiveOverlayManager mgr || mgr == null)
                return;

            _objectiveMgrWasEnabled = mgr.enabled;
            mgr.enabled = false;
            HideObjectiveOverlays(mgr);
        }

        private static void RestoreObjectiveManager()
        {
            try
            {
                CombatHUD? hud = SceneSingleton<CombatHUD>.i;
                if (hud == null)
                    return;

                if (ObjectiveOverlayField?.GetValue(hud) is ObjectiveOverlayManager mgr && mgr != null)
                    mgr.enabled = _objectiveMgrWasEnabled;
            }
            catch
            {
                // ignore
            }

            _objectiveMgrWasEnabled = false;
        }

        private static void RestoreDesignatorVisual()
        {
            try
            {
                CombatHUD? hud = SceneSingleton<CombatHUD>.i;
                if (hud?.targetDesignator == null)
                    return;

                hud.targetDesignator.enabled = _designatorWasEnabled;
                Color c = hud.targetDesignator.color;
                c.a = 1f;
                hud.targetDesignator.color = c;
            }
            catch
            {
                // ignore
            }

            _designatorWasEnabled = false;
        }

        private static void HideObjectiveOverlays(ObjectiveOverlayManager mgr)
        {
            if (ObjectiveOverlaysField?.GetValue(mgr) is not IList overlays)
                return;

            for (int i = 0; i < overlays.Count; i++)
            {
                if (overlays[i] is not ObjectiveOverlay overlay || overlay == null)
                    continue;

                try
                {
                    ObjectiveHideOverlayMethod?.Invoke(overlay, null);
                }
                catch
                {
                    // ignore
                }

                HideGo(overlay.gameObject);
            }
        }

        /// <summary>
        /// Per-frame: FlightHud.Update turns velocityVector back on; ObjectiveOverlay re-enables mission icons.
        /// </summary>
        private static void SuppressIlsAndObjectives()
        {
            try
            {
                FlightHud? flightHud = SceneSingleton<FlightHud>.i;
                if (flightHud != null)
                {
                    ForceOff(flightHud.velocityVector != null ? flightHud.velocityVector.gameObject : null);
                    ForceOff(flightHud.waterline != null ? flightHud.waterline.gameObject : null);
                    ForceOff(flightHud.virtualJoystickPos != null ? flightHud.virtualJoystickPos.gameObject : null);
                    if (FlightHudCenterField?.GetValue(flightHud) is Transform hudCenter)
                        ForceOff(hudCenter.gameObject);
                    ForceOff(AccessToolsField(typeof(FlightHud), "compass")?.GetValue(flightHud) as Component);
                    ForceOff(AccessToolsField(typeof(FlightHud), "pitchCompass")?.GetValue(flightHud) as Component);
                    object? pitchCenter = AccessToolsField(typeof(FlightHud), "pitchCompassCenter")?.GetValue(flightHud);
                    if (pitchCenter is GameObject pitchGo)
                        ForceOff(pitchGo);
                    else if (pitchCenter is Component pitchComp)
                        ForceOff(pitchComp.gameObject);
                }
            }
            catch
            {
                // ignore
            }

            CombatHUD? hud = SceneSingleton<CombatHUD>.i;
            if (hud == null)
                return;

            if (hud.targetDesignator != null)
            {
                hud.targetDesignator.enabled = false;
                Color c = hud.targetDesignator.color;
                if (c.a > 0.01f)
                {
                    c.a = 0f;
                    hud.targetDesignator.color = c;
                }
            }

            if (TargetArrowField?.GetValue(hud) is Image arrow && arrow != null)
            {
                arrow.enabled = false;
                ForceOff(arrow.gameObject);
            }

            if (ObjectiveOverlayField?.GetValue(hud) is ObjectiveOverlayManager mgr && mgr != null)
            {
                if (mgr.enabled)
                    mgr.enabled = false;
                HideObjectiveOverlays(mgr);
            }

            TrimIconLayerToUnitMarkersOnly(hud);
            HideHitMarkers(hud);
        }

        private static void TrimIconLayerToUnitMarkersOnly(CombatHUD hud)
        {
            if (hud.iconLayer == null)
                return;

            _unitMarkerKeep.Clear();
            if (MarkersField?.GetValue(hud) is IList markers)
            {
                for (int i = 0; i < markers.Count; i++)
                {
                    if (markers[i] is HUDUnitMarker unitMarker && unitMarker != null && unitMarker.image != null)
                        _unitMarkerKeep.Add(unitMarker.image.transform);
                }
            }

            Transform layer = hud.iconLayer;
            for (int i = 0; i < layer.childCount; i++)
            {
                Transform child = layer.GetChild(i);
                if (IsUnitMarkerBranch(child, _unitMarkerKeep))
                    continue;

                ForceOff(child.gameObject);
            }
        }

        private static bool IsUnitMarkerBranch(Transform node, HashSet<Transform> markerRoots)
        {
            foreach (Transform root in markerRoots)
            {
                if (root == null)
                    continue;
                if (node == root || node.IsChildOf(root) || root.IsChildOf(node))
                    return true;
            }

            return false;
        }

        private static void HideHitMarkers(CombatHUD hud)
        {
            if (HitMarkersField?.GetValue(hud) is not IList hitMarkers)
                return;

            for (int i = 0; i < hitMarkers.Count; i++)
            {
                object? entry = hitMarkers[i];
                if (entry == null)
                    continue;

                FieldInfo? markerField = entry.GetType().GetField(
                    "marker",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (markerField?.GetValue(entry) is GameObject markerGo)
                    ForceOff(markerGo);
            }
        }

        private static void ForceOff(Component? c)
        {
            if (c != null)
                ForceOff(c.gameObject);
        }

        private static void ForceOff(GameObject? go)
        {
            if (go != null && go.activeSelf)
                go.SetActive(false);
        }

        private static void AddKeep(HashSet<Transform> keep, Transform? t)
        {
            if (t != null)
                keep.Add(t);
        }

        private static void HideBranchesExcept(Transform root, HashSet<Transform> keep)
        {
            if (root == null || keep.Count == 0)
                return;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (IsKeepOrUnderKeep(child, keep))
                    continue;

                if (IsAncestorOfKeep(child, keep))
                {
                    HideBranchesExcept(child, keep);
                    continue;
                }

                HideGo(child.gameObject);
            }
        }

        private static bool IsKeepOrUnderKeep(Transform node, HashSet<Transform> keep)
        {
            foreach (Transform k in keep)
            {
                if (k == null)
                    continue;
                if (node == k || node.IsChildOf(k))
                    return true;
            }

            return false;
        }

        private static bool IsAncestorOfKeep(Transform node, HashSet<Transform> keep)
        {
            foreach (Transform k in keep)
            {
                if (k != null && k.IsChildOf(node))
                    return true;
            }

            return false;
        }

        private static void HideStubsOnMissilePanel()
        {
            RectTransform? panelRt = MissileCameraFeedController.TryGetPanelRt();
            if (panelRt == null)
                return;

            MissileCameraHudOverlay.ApplyLegacyStubVisibility(panelRt, hide: true);
            ForceStubGone(panelRt, "MissileCameraTitle");
            ForceStubGone(panelRt, "MissileCameraColor");
            ForceStubGone(panelRt, "MissileTelemetry");

            if (panelRt.TryGetComponent(out Image panelImage))
            {
                Color c = panelImage.color;
                c.a = 0f;
                panelImage.color = c;
                panelImage.raycastTarget = false;
            }
        }

        private static void ForceStubGone(RectTransform panelRt, string childName)
        {
            Transform? node = FindDeep(panelRt, childName);
            if (node == null)
                return;

            node.gameObject.SetActive(false);
            CanvasGroup? cg = node.GetComponent<CanvasGroup>();
            if (cg != null)
                cg.alpha = 0f;

            if (node.TryGetComponent(out Text text))
            {
                text.text = string.Empty;
                text.enabled = false;
            }
        }

        private static Canvas? ResolveCombatCanvas(CombatHUD? hud)
        {
            if (hud == null)
                return null;

            if (hud.iconLayer != null)
            {
                Canvas? fromIcons = hud.iconLayer.GetComponentInParent<Canvas>();
                if (fromIcons != null)
                    return fromIcons;
            }

            return hud.GetComponentInParent<Canvas>();
        }

        private static void ElevateCombatHudCanvas()
        {
            CombatHUD? hud = SceneSingleton<CombatHUD>.i;
            Canvas? canvas = ResolveCombatCanvas(hud);
            if (canvas == null)
                return;

            _combatCanvas = canvas;
            _savedRenderMode = canvas.renderMode;
            _savedSortingOrder = canvas.sortingOrder;
            _savedOverrideSorting = canvas.overrideSorting;
            _savedWorldCamera = canvas.worldCamera;
            _savedPixelPerfect = canvas.pixelPerfect;

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = MarkersSortingOrder;
            canvas.pixelPerfect = false;
            _canvasElevated = true;
        }

        private static void RestoreCombatHudCanvas()
        {
            if (!_canvasElevated || _combatCanvas == null)
            {
                _canvasElevated = false;
                _combatCanvas = null;
                return;
            }

            _combatCanvas.renderMode = _savedRenderMode;
            _combatCanvas.sortingOrder = _savedSortingOrder;
            _combatCanvas.overrideSorting = _savedOverrideSorting;
            _combatCanvas.worldCamera = _savedWorldCamera;
            _combatCanvas.pixelPerfect = _savedPixelPerfect;

            _canvasElevated = false;
            _combatCanvas = null;
            _savedWorldCamera = null;
        }

        private static void ForceCombatHudMarkerPass()
        {
            CombatHUD? hud = SceneSingleton<CombatHUD>.i;
            if (hud == null || hud.aircraft == null)
                return;

            if (!hud.gameObject.activeSelf)
                hud.gameObject.SetActive(true);

            if (hud.iconLayer != null && !hud.iconLayer.gameObject.activeSelf)
                hud.iconLayer.gameObject.SetActive(true);

            // Designator GO must stay active for TargetSelect range checks (dump).
            if (hud.targetDesignator != null && !hud.targetDesignator.gameObject.activeSelf)
                hud.targetDesignator.gameObject.SetActive(true);

            try
            {
                UpdateMarkersMethod?.Invoke(hud, null);
            }
            catch
            {
                // ignore
            }
        }

        private static void HideComponent(Component? c)
        {
            if (c != null)
                HideGo(c.gameObject);
        }

        private static void HideGo(GameObject? go)
        {
            if (go == null)
                return;

            for (int i = 0; i < _hiddenChrome.Count; i++)
            {
                if (_hiddenChrome[i].go == go)
                    return;
            }

            _hiddenChrome.Add((go, go.activeSelf));
            if (go.activeSelf)
                go.SetActive(false);
        }

        private static void RestoreHiddenChrome()
        {
            for (int i = 0; i < _hiddenChrome.Count; i++)
            {
                (GameObject go, bool wasActive) = _hiddenChrome[i];
                if (go != null)
                    go.SetActive(wasActive);
            }

            _hiddenChrome.Clear();
        }

        private static bool IsCanvasActive(object? owner, string fieldName)
        {
            if (owner == null)
                return false;

            FieldInfo? field = owner.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field?.GetValue(owner) is not Canvas canvas || canvas == null)
                return false;

            return canvas.gameObject.activeSelf;
        }

        private static Transform? FindDeep(Transform root, string name)
        {
            if (root.name == name)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform? found = FindDeep(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static FieldInfo? AccessToolsField(System.Type type, string name) =>
            HarmonyLib.AccessTools.Field(type, name);
    }
}
