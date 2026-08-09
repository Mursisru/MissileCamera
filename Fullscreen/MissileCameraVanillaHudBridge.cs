using System;
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
        private static readonly FieldInfo? FlightHudCanvasField =
            AccessToolsField(typeof(FlightHud), "canvas");
        private static readonly FieldInfo? FlightHudCenterField =
            AccessToolsField(typeof(FlightHud), "HUDCenter");
        private static readonly FieldInfo? FlightHudCompassField =
            AccessToolsField(typeof(FlightHud), "compass");
        private static readonly FieldInfo? FlightHudPitchCompassField =
            AccessToolsField(typeof(FlightHud), "pitchCompass");
        private static readonly FieldInfo? FlightHudPitchCompassCenterField =
            AccessToolsField(typeof(FlightHud), "pitchCompassCenter");
        private static readonly FieldInfo? ObjectiveOverlaysField =
            AccessToolsField(typeof(ObjectiveOverlayManager), "overlays");
        private static readonly MethodInfo? ObjectiveHideOverlayMethod =
            HarmonyLib.AccessTools.Method(typeof(ObjectiveOverlay), "HideOverlay");
        private static readonly MethodInfo? UpdateMarkersMethod =
            HarmonyLib.AccessTools.Method(typeof(CombatHUD), "UpdateMarkers");

        private const float MarkerPassInterval = 1f / 10f;
        private const float SuppressIlsInterval = 1f / 10f;
        private const float HideStubsInterval = 1f / 5f;
        private static float _nextMarkerPassUnscaled;
        private static float _nextSuppressIlsUnscaled;
        private static float _nextHideStubsUnscaled;
        private static bool _flightHudWasActive;
        private static bool _objectiveMgrWasEnabled;
        /// <summary>True only while FS actually disabled ObjectiveOverlayManager — never force-disable on unload.</summary>
        private static bool _objectivesSuppressedByUs;
        /// <summary>True only while FS SuppressIls hid FlightHud chrome — gates ForceOn restore.</summary>
        private static bool _ilsSuppressedByUs;
        private static bool _designatorWasEnabled;
        private static readonly List<(GameObject go, bool wasActive)> _hiddenChrome =
            new List<(GameObject, bool)>(64);

        private static Canvas? _combatCanvas;
        private static bool _canvasElevated;
        private static RenderMode _savedRenderMode;
        private static int _savedSortingOrder;
        private static bool _savedOverrideSorting;
        private static Camera? _savedWorldCamera;
        private static bool _savedPixelPerfect;

        internal static void OnFullscreenEntered()
        {
            // Never drop a prior hide list without unhide (re-enter / failed exit leak).
            try
            {
                RestoreObjectiveManager();
                RestoreDesignatorVisual();
                RestoreCombatHudCanvas();
                RestoreHiddenChrome();
                RestoreFlightHudVisuals();
            }
            catch
            {
                // ignore
            }

            _objectiveMgrWasEnabled = false;
            _objectivesSuppressedByUs = false;
            _ilsSuppressedByUs = false;
            _designatorWasEnabled = false;
            _nextMarkerPassUnscaled = 0f;
            _nextSuppressIlsUnscaled = 0f;
            _nextHideStubsUnscaled = 0f;

            try
            {
                HideStubsOnMissilePanel();
                ElevateCombatHudCanvas();
                ApplyMarkersOnlyVisibility();
                SuppressIlsAndObjectives();
                _nextSuppressIlsUnscaled = Time.unscaledTime + SuppressIlsInterval;
                SyncMarkersFromHqIfNeeded();
                ForceCombatHudMarkerPass();
                _nextMarkerPassUnscaled = Time.unscaledTime + MarkerPassInterval;
                MissileCameraFullscreenTargetLock.OnFullscreenEntered();
            }
            catch (Exception ex)
            {
                MfdLog.Info("fullscreen enter hud failed: " + ex.Message);
            }

            MfdLog.Info("fullscreen markers-only"
                + (_canvasElevated ? " canvas↑" : " canvas miss")
                + $" hidden={_hiddenChrome.Count}");
        }

        internal static void OnFullscreenExited()
        {
            MissileCameraFullscreenTargetLock.OnFullscreenExited();
            RestoreObjectiveManager();
            RestoreDesignatorVisual();
            RestoreCombatHudCanvas();
            RestoreHiddenChrome();
            RestoreFlightHudVisuals();
            // Outside FS: refresh markers only if aircraft alive — never SetActive chrome.
            ForceCombatHudMarkerPass();
            MissileCameraCombatHudMarkerProjection.RestoreMarkerImages();

            _flightHudWasActive = false;
            _ilsSuppressedByUs = false;
        }

        internal static bool HasStickyHiddenChrome =>
            _hiddenChrome.Count > 0 || _canvasElevated || _objectivesSuppressedByUs || _ilsSuppressedByUs;

        /// <summary>
        /// Off-session / orphan FS: if hide list still holds live refs, unhide immediately.
        /// </summary>
        internal static void HealStickyIfNeeded()
        {
            if (!HasStickyHiddenChrome)
                return;

            if (MissileCameraFullscreenController.IsActive)
                return;

            MfdLog.Info("vanilla hud sticky heal hidden=" + _hiddenChrome.Count);
            ResetForMissionUnload();
        }

        internal static void ResetForMissionUnload()
        {
            _nextMarkerPassUnscaled = 0f;
            _nextSuppressIlsUnscaled = 0f;
            _nextHideStubsUnscaled = 0f;
            // Always attempt live unhide first — never abandon SetActive(false) chrome.
            try
            {
                RestoreObjectiveManager();
                RestoreDesignatorVisual();
                RestoreCombatHudCanvas();
                RestoreHiddenChrome();
                RestoreFlightHudVisuals();
                // Unload heal must NOT SetActive CombatHUD/FlightHud — that lights ILS on faction select.
                MissileCameraCombatHudMarkerProjection.RestoreMarkerImages();
            }
            catch (Exception ex)
            {
                MfdLog.Info("fullscreen unload chrome restore failed: " + ex.Message);
            }

            // Restore marker selections when HUD still exists; never bare-Abandon mid-scene.
            MissileCameraFullscreenTargetLock.ResetForMissionUnload();
            _hiddenChrome.Clear();
            _combatCanvas = null;
            _canvasElevated = false;
            _flightHudWasActive = false;
            _objectiveMgrWasEnabled = false;
            _objectivesSuppressedByUs = false;
            _ilsSuppressedByUs = false;
            _designatorWasEnabled = false;
        }

        internal static void TickHideStubs()
        {
            if (!MissileCameraFullscreenController.IsActive)
                return;

            // Stubs stay off — 5 Hz heal is enough (invisible vs every feed Tick).
            float now = Time.unscaledTime;
            if (now < _nextHideStubsUnscaled)
                return;

            _nextHideStubsUnscaled = now + HideStubsInterval;
            HideStubsOnMissilePanel();
        }

        internal static void LateTickMarkers()
        {
            if (!MissileCameraFullscreenController.IsActive)
                return;

            // Full suppress only on interval or when vanilla re-enabled chrome (dirty heal).
            SuppressIlsAndObjectivesIfDue();
            ForceCombatHudMarkerPassIfDue();
            MissileCameraFullscreenTargetLock.Maintain();
        }

        /// <summary>
        /// Shared 10 Hz slot for Force markers (LateTickMarkers + CombatHUD Prefix when aircraft null).
        /// </summary>
        internal static void ForceCombatHudMarkerPassIfDue()
        {
            if (!MissileCameraFullscreenController.IsActive)
                return;

            float now = Time.unscaledTime;
            if (now < _nextMarkerPassUnscaled)
                return;

            _nextMarkerPassUnscaled = now + MarkerPassInterval;
            SyncMarkersFromHqIfNeeded();
            ForceCombatHudMarkerPass();
        }

        private static void SuppressIlsAndObjectivesIfDue()
        {
            float now = Time.unscaledTime;
            if (now < _nextSuppressIlsUnscaled && !NeedsSuppressIlsHeal())
                return;

            _nextSuppressIlsUnscaled = now + SuppressIlsInterval;
            SuppressIlsAndObjectives();
        }

        /// <summary>Cheap check — vanilla Update may turn ILS / Target TMP / objectives back on.</summary>
        private static bool NeedsSuppressIlsHeal()
        {
            try
            {
                FlightHud? flightHud = SceneSingleton<FlightHud>.i;
                if (flightHud != null)
                {
                    if (flightHud.velocityVector != null && flightHud.velocityVector.gameObject.activeSelf)
                        return true;
                    if (flightHud.waterline != null && flightHud.waterline.gameObject.activeSelf)
                        return true;
                    if (flightHud.virtualJoystickPos != null && flightHud.virtualJoystickPos.gameObject.activeSelf)
                        return true;
                    if (FlightHudCenterField?.GetValue(flightHud) is Transform hudCenter
                        && hudCenter != null
                        && hudCenter.gameObject.activeSelf)
                        return true;
                }

                CombatHUD? hud = SceneSingleton<CombatHUD>.i;
                if (hud == null)
                    return false;

                if (hud.targetDesignator != null
                    && (hud.targetDesignator.enabled || hud.targetDesignator.color.a > 0.01f))
                    return true;

                if (TargetArrowField?.GetValue(hud) is Image arrow
                    && arrow != null
                    && (arrow.enabled || arrow.gameObject.activeSelf))
                    return true;

                if (TargetTextField?.GetValue(hud) is Behaviour targetText
                    && targetText != null
                    && (targetText.enabled || targetText.gameObject.activeSelf))
                    return true;

                if (TargetInfoField?.GetValue(hud) is Behaviour targetInfo
                    && targetInfo != null
                    && targetInfo.gameObject.activeSelf)
                    return true;

                if (ObjectiveOverlayField?.GetValue(hud) is ObjectiveOverlayManager mgr
                    && mgr != null
                    && mgr.enabled)
                    return true;
            }
            catch
            {
                return true;
            }

            return false;
        }

        private static void SoftHideDynamicMap()
        {
            // Never DynamicMap.EnableCanvas — sticky across sorties. FS yields when mapMaximized.
        }

        private static void RestoreFlightHud()
        {
            // Never FlightHud.EnableCanvas.
        }

        private static void RestoreDynamicMap()
        {
            // Never DynamicMap.EnableCanvas.
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

                // Never HideGo(entire FlightHud canvas) — that sticky-kills glass chrome/markers
                // across sorties when restore races. Shared canvas: branch-walk keep iconLayer only.
                // Separate FlightHud canvas: leave it; SuppressIlsAndObjectives handles ILS each LateUpdate.
                if (flightCanvas != null && combatCanvas != null && flightCanvas == combatCanvas)
                    HideBranchesExcept(flightCanvas.transform, keep);
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
            // targetText is TMP — never cast to UI.Text (always missed → "Target" stuck on).
            HideComponent(TargetTextField?.GetValue(hud) as Component);

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

            // Snapshot once per FS session — do not overwrite with false on repeated LateUpdate suppress.
            if (!_objectivesSuppressedByUs)
            {
                _objectiveMgrWasEnabled = mgr.enabled;
                _objectivesSuppressedByUs = true;
            }

            mgr.enabled = false;
            HideObjectiveOverlays(mgr);
        }

        private static void RestoreObjectiveManager()
        {
            try
            {
                CombatHUD? hud = SceneSingleton<CombatHUD>.i;
                if (hud != null
                    && ObjectiveOverlayField?.GetValue(hud) is ObjectiveOverlayManager mgr
                    && mgr != null)
                {
                    if (_objectivesSuppressedByUs)
                    {
                        // We hid WayPoint / MissionTarget for FS — put manager back.
                        mgr.enabled = _objectiveMgrWasEnabled;
                    }
                    else if (!mgr.enabled)
                    {
                        // Heal: HardReset/unload used to assign enabled=false even when FS never ran,
                        // which permanently killed ObjectiveOverlay (Waypoint / MissionTarget).
                        mgr.enabled = true;
                    }
                }
            }
            catch
            {
                // ignore
            }

            _objectivesSuppressedByUs = false;
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
                    // HideOverlay only — never SetActive(false) on overlay GO.
                    // Pointer/info are reparented under iconLayer; killing the root sticky-breaks restore.
                    ObjectiveHideOverlayMethod?.Invoke(overlay, null);
                }
                catch
                {
                    // ignore
                }
            }
        }

        /// <summary>
        /// Soft-rate + dirty heal: FlightHud.Update may turn velocityVector back on;
        /// ObjectiveOverlay may re-enable mission icons. HideGo tracked — never ForceOff without restore.
        /// </summary>
        private static void SuppressIlsAndObjectives()
        {
            try
            {
                FlightHud? flightHud = SceneSingleton<FlightHud>.i;
                if (flightHud != null)
                {
                    _ilsSuppressedByUs = true;
                    HideGo(flightHud.velocityVector != null ? flightHud.velocityVector.gameObject : null);
                    HideGo(flightHud.waterline != null ? flightHud.waterline.gameObject : null);
                    HideGo(flightHud.virtualJoystickPos != null ? flightHud.virtualJoystickPos.gameObject : null);
                    if (FlightHudCenterField?.GetValue(flightHud) is Transform hudCenter)
                        HideGo(hudCenter.gameObject);
                    HideComponent(FlightHudCompassField?.GetValue(flightHud) as Component);
                    HideComponent(FlightHudPitchCompassField?.GetValue(flightHud) as Component);
                    object? pitchCenter = FlightHudPitchCompassCenterField?.GetValue(flightHud);
                    if (pitchCenter is GameObject pitchGo)
                        HideGo(pitchGo);
                    else if (pitchCenter is Component pitchComp)
                        HideGo(pitchComp.gameObject);
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
                HideGo(arrow.gameObject);
            }

            // Re-suppress every LateUpdate — SetTargetArrow / ShowTargetInfo can re-enable TMP.
            HideComponent(TargetTextField?.GetValue(hud) as Component);
            HideComponent(TargetInfoField?.GetValue(hud) as Component);

            if (ObjectiveOverlayField?.GetValue(hud) is ObjectiveOverlayManager mgr && mgr != null)
            {
                if (!_objectivesSuppressedByUs)
                {
                    _objectiveMgrWasEnabled = mgr.enabled;
                    _objectivesSuppressedByUs = true;
                }

                if (mgr.enabled)
                    mgr.enabled = false;
                HideObjectiveOverlays(mgr);
            }

            // Do not deactivate iconLayer children — that permanently broke mission/ILS icons on exit.
            // Objectives: disable manager + HideOverlay only (no SetActive on overlay roots).
        }

        private static void RestoreFlightHudVisuals()
        {
            // Only undo what SuppressIls hid during a real FS session — never ForceOn at mission boot.
            if (!_ilsSuppressedByUs)
                return;

            try
            {
                FlightHud? flightHud = SceneSingleton<FlightHud>.i;
                if (flightHud == null)
                    return;

                ForceOn(flightHud.velocityVector != null ? flightHud.velocityVector.gameObject : null);
                ForceOn(flightHud.waterline != null ? flightHud.waterline.gameObject : null);
                ForceOn(flightHud.virtualJoystickPos != null ? flightHud.virtualJoystickPos.gameObject : null);
                if (FlightHudCenterField?.GetValue(flightHud) is Transform hudCenter)
                    ForceOn(hudCenter.gameObject);
                ForceOn(AccessToolsField(typeof(FlightHud), "compass")?.GetValue(flightHud) as Component);
                ForceOn(AccessToolsField(typeof(FlightHud), "pitchCompass")?.GetValue(flightHud) as Component);
                object? pitchCenter = AccessToolsField(typeof(FlightHud), "pitchCompassCenter")?.GetValue(flightHud);
                if (pitchCenter is GameObject pitchGo)
                    ForceOn(pitchGo);
                else if (pitchCenter is Component pitchComp)
                    ForceOn(pitchComp.gameObject);
            }
            catch
            {
                // ignore
            }
            finally
            {
                _ilsSuppressedByUs = false;
            }
        }

        private static void ForceOn(Component? c)
        {
            if (c != null)
                ForceOn(c.gameObject);
        }

        private static void ForceOn(GameObject? go)
        {
            if (go != null && !go.activeSelf)
                go.SetActive(true);
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
            if (!_canvasElevated)
                return;

            Canvas? canvas = _combatCanvas;
            if (canvas == null)
            {
                try
                {
                    CombatHUD? hud = SceneSingleton<CombatHUD>.i;
                    canvas = ResolveCombatCanvas(hud);
                }
                catch
                {
                    canvas = null;
                }
            }

            if (canvas == null)
            {
                _canvasElevated = false;
                _combatCanvas = null;
                return;
            }

            try
            {
                canvas.renderMode = _savedRenderMode;
                canvas.sortingOrder = _savedSortingOrder;
                canvas.overrideSorting = _savedOverrideSorting;
                canvas.worldCamera = _savedWorldCamera;
                canvas.pixelPerfect = _savedPixelPerfect;
            }
            catch
            {
                // ignore destroyed
            }

            _canvasElevated = false;
            _combatCanvas = null;
            _savedWorldCamera = null;
        }

        /// <summary>
        /// FS: ensure iconLayer + tick markers. Outside FS: UpdateMarkers only if aircraft set — never SetActive chrome.
        /// </summary>
        internal static void ForceCombatHudMarkerPass()
        {
            CombatHUD? hud = SceneSingleton<CombatHUD>.i;
            if (hud == null)
                return;

            bool fs = MissileCameraFullscreenController.IsActive;
            if (!fs)
            {
                if (hud.aircraft == null)
                    return;

                try { UpdateMarkersMethod?.Invoke(hud, null); }
                catch { /* ignore */ }
                return;
            }

            // FS markers-only: iconLayer only — never force whole CombatHUD (weapons/ILS siblings).
            if (hud.iconLayer != null && !hud.iconLayer.gameObject.activeSelf)
                hud.iconLayer.gameObject.SetActive(true);

            try
            {
                if (hud.aircraft != null)
                    UpdateMarkersMethod?.Invoke(hud, null);
                else
                    TickMarkersWithoutAircraft(hud);
            }
            catch
            {
                // ignore
            }
        }

        /// <summary>FS without ownship: seed vanilla markers from seeker/DynamicMap HQ (CreateMarker needs aircraft proxy).</summary>
        internal static void SyncMarkersFromHqIfNeeded()
        {
            if (!MissileCameraFullscreenController.IsActive)
                return;

            CombatHUD? hud = SceneSingleton<CombatHUD>.i;
            if (hud == null)
                return;

            if (MarkersListField?.GetValue(hud) is System.Collections.Generic.List<HUDUnitMarker> existing
                && existing.Count > 0)
                return;

            FactionHQ? hq = ResolveMarkerHq(hud.aircraft);
            if (hq == null)
                return;

            try
            {
                for (int i = 0; i < hq.factionUnits.Count; i++)
                    hud.CreateMarker(hq.factionUnits[i]);

                foreach (KeyValuePair<PersistentID, TrackingInfo> pair in hq.trackingDatabase)
                    hud.CreateMarker(pair.Key);
            }
            catch (Exception ex)
            {
                MfdLog.Info("FS SyncMarkersFromHq failed: " + ex.Message);
            }
        }

        private static void TickMarkersWithoutAircraft(CombatHUD hud)
        {
            FactionHQ? hq = ResolveMarkerHq(null);
            if (hq == null || MarkersListField == null)
                return;

            if (MarkersListField.GetValue(hud) is not System.Collections.Generic.List<HUDUnitMarker> markers
                || markers.Count == 0)
                return;

            Camera? feed = MissileCameraFeedController.TryGetFeedCamera();
            Transform viewTf;
            if (feed != null)
            {
                viewTf = feed.transform;
            }
            else
            {
                CameraStateManager? csm = SceneSingleton<CameraStateManager>.i;
                if (csm == null)
                    return;
                viewTf = csm.transform;
            }

            GlobalPosition viewPos = viewTf.GlobalPosition();
            Vector3 forward = viewTf.forward;
            for (int i = 0; i < markers.Count; i++)
            {
                HUDUnitMarker? m = markers[i];
                if (m == null)
                    continue;

                try
                {
                    m.UpdatePosition(hq, viewPos, forward);
                }
                catch
                {
                    // ignore single marker
                }
            }
        }

        private static FactionHQ? ResolveMarkerHq(Aircraft? aircraft)
        {
            if (aircraft != null && aircraft.NetworkHQ != null)
                return aircraft.NetworkHQ;

            Missile? seeker = MissileCameraFeedController.TryGetFollowedMissile();
            if (seeker != null && !seeker.disabled && seeker.NetworkHQ != null)
                return seeker.NetworkHQ;

            try
            {
                DynamicMap? map = SceneSingleton<DynamicMap>.i;
                if (map != null && map.HQ != null)
                    return map.HQ;
            }
            catch
            {
                // ignore
            }

            return null;
        }

        /// <summary>Aircraft proxy for HUDUnitMarker ctor when CombatHUD.aircraft is null (FS only).</summary>
        internal static Aircraft? TryResolveMarkerAircraftProxy()
        {
            if (!MissileCameraFullscreenController.IsActive)
                return null;

            Missile? seeker = MissileCameraFeedController.TryGetFollowedMissile();
            if (seeker == null || seeker.disabled)
                return null;

            if (seeker.owner is Aircraft ownerAc)
                return ownerAc;

            FactionHQ? hq = seeker.NetworkHQ;
            if (hq == null)
                return null;

            for (int i = 0; i < hq.factionUnits.Count; i++)
            {
                if (!UnitRegistry.TryGetUnit(new PersistentID?(hq.factionUnits[i]), out Unit unit))
                    continue;
                if (unit is Aircraft ac)
                    return ac;
            }

            return null;
        }

        private static readonly FieldInfo? MarkersListField =
            AccessToolsField(typeof(CombatHUD), "markers");

        /// <summary>TEMP diag: count CombatHUD markers that are Missile units + image state.</summary>
        internal static void DiagLogMissileMarkers(string tag)
        {
            try
            {
                CombatHUD? hud = SceneSingleton<CombatHUD>.i;
                if (hud == null)
                {
                    MissileCameraMissionLifecycleDiag.Info("markers " + tag + " CombatHUD=null");
                    return;
                }

                FieldInfo? markersField = AccessToolsField(typeof(CombatHUD), "markers");
                if (markersField?.GetValue(hud) is not System.Collections.Generic.List<HUDUnitMarker> markers)
                {
                    MissileCameraMissionLifecycleDiag.Info("markers " + tag + " list=null");
                    return;
                }

                int missile = 0;
                int enabled = 0;
                int hasSprite = 0;
                int selected = 0;
                for (int i = 0; i < markers.Count; i++)
                {
                    HUDUnitMarker? m = markers[i];
                    if (m?.unit == null || !(m.unit is Missile))
                        continue;

                    missile++;
                    if (m.selected)
                        selected++;
                    if (m.image != null && m.image.enabled)
                        enabled++;
                    if (m.image != null && m.image.sprite != null)
                        hasSprite++;
                }

                MissileCameraMissionLifecycleDiag.Info(
                    "markers " + tag
                    + " total=" + markers.Count
                    + " missile=" + missile
                    + " selected=" + selected
                    + " imgOn=" + enabled
                    + " sprite=" + hasSprite
                    + " iconLayer=" + (hud.iconLayer != null && hud.iconLayer.gameObject.activeInHierarchy));
            }
            catch (Exception ex)
            {
                MissileCameraMissionLifecycleDiag.Warn("markers diag failed: " + ex.Message);
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
                if (_hiddenChrome[i].go != go)
                    continue;

                // FlightHud.Update may re-enable velocityVector — keep suppressed while fullscreen.
                if (go.activeSelf)
                    go.SetActive(false);
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
                try
                {
                    (GameObject go, bool wasActive) = _hiddenChrome[i];
                    if (go != null)
                        go.SetActive(wasActive);
                }
                catch
                {
                    // ignore destroyed
                }
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
