using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// Fullscreen: only CombatHUD unit markers (dump: HUDUnitMarker → mainCamera.WorldToScreenPoint).
    /// iconLayer lifted to Overlay sorting 120 (above FLIR Overlay 50).
    /// CombatHUD.UpdateMarkers forced after camera LateTick — FlightHud.EnableCanvas(false)
    /// can park CombatHUD under an inactive canvas and stop LateUpdate.
    /// </summary>
    internal static class MissileCameraVanillaHudBridge
    {
        private const int MarkersSortingOrder = 120;
        private const string MarkersHostName = "MissileCamera.VanillaMarkersHost";

        private static readonly FieldInfo? TopRightPanelField =
            AccessToolsField(typeof(CombatHUD), "topRightPanel");
        private static readonly FieldInfo? ThreatListField =
            AccessToolsField(typeof(CombatHUD), "threatList");
        private static readonly FieldInfo? WeaponStatusField =
            AccessToolsField(typeof(CombatHUD), "weaponStatus");
        private static readonly FieldInfo? CountermeasureBgField =
            AccessToolsField(typeof(CombatHUD), "countermeasureBackground");
        private static readonly FieldInfo? TargetDesignatorField =
            AccessToolsField(typeof(CombatHUD), "targetDesignator");
        private static readonly FieldInfo? TargetInfoField =
            AccessToolsField(typeof(CombatHUD), "targetInfo");
        private static readonly FieldInfo? TargetArrowField =
            AccessToolsField(typeof(CombatHUD), "targetArrow");
        private static readonly FieldInfo? TargetTextField =
            AccessToolsField(typeof(CombatHUD), "targetText");
        private static readonly FieldInfo? FlightHudCanvasField =
            AccessToolsField(typeof(FlightHud), "canvas");
        private static readonly MethodInfo? UpdateMarkersMethod =
            HarmonyLib.AccessTools.Method(typeof(CombatHUD), "UpdateMarkers");
        private static readonly MethodInfo? UpdateHitMarkersMethod =
            HarmonyLib.AccessTools.Method(typeof(CombatHUD), "UpdateHitMarkers");

        private static bool _flightHudWasActive;
        private static bool _mapWasActive;
        private static bool _flightHudSoftHide;
        private static readonly System.Collections.Generic.List<(GameObject go, bool wasActive)> _hiddenChrome =
            new System.Collections.Generic.List<(GameObject, bool)>(32);

        private static GameObject? _markersHostGo;
        private static RectTransform? _markersRoot;
        private static Transform? _iconLayer;
        private static Transform? _iconOriginalParent;
        private static int _iconOriginalSibling;
        private static bool _iconReparented;
        private static Transform? _arrowRoot;
        private static Transform? _arrowOriginalParent;
        private static int _arrowOriginalSibling;
        private static bool _arrowReparented;

        internal static void OnFullscreenEntered()
        {
            _hiddenChrome.Clear();
            HideStubsOnMissilePanel();
            SoftOrHardHideFlightHud();
            SoftHideDynamicMap();
            HideCombatHudChromeExceptMarkers();
            BorrowIconLayerAboveOverlay();
            ForceCombatHudMarkerPass();
            MfdLog.Info("fullscreen markers-only"
                + (_iconReparented ? " iconLayer↑" : " iconLayer miss")
                + (_flightHudSoftHide ? " flightHud soft" : " flightHud hard"));
        }

        internal static void OnFullscreenExited()
        {
            RestoreIconLayer();
            RestoreCombatHudChrome();

            try
            {
                if (_flightHudWasActive && !_flightHudSoftHide)
                    FlightHud.EnableCanvas(true);
            }
            catch
            {
                // ignore
            }

            try
            {
                if (_mapWasActive)
                    DynamicMap.EnableCanvas(true);
                else if (SceneSingleton<CameraStateManager>.i != null
                         && CameraStateManager.cameraMode == CameraMode.cockpit
                         && SceneSingleton<CombatHUD>.i?.aircraft != null)
                {
                    DynamicMap.EnableCanvas(true);
                }
            }
            catch
            {
                // ignore
            }

            _flightHudWasActive = false;
            _mapWasActive = false;
            _flightHudSoftHide = false;
        }

        internal static void ResetForMissionUnload()
        {
            RestoreIconLayer();
            RestoreCombatHudChrome();
            DestroyMarkersHost();
            _flightHudWasActive = false;
            _mapWasActive = false;
            _flightHudSoftHide = false;
        }

        internal static void TickHideStubs()
        {
            if (!MissileCameraFullscreenController.IsActive)
                return;

            HideStubsOnMissilePanel();
        }

        /// <summary>After ViewDriver.LateTick — dump CombatHUD.LateUpdate → UpdateMarkers.</summary>
        internal static void LateTickMarkers()
        {
            if (!MissileCameraFullscreenController.IsActive)
                return;

            if (_markersHostGo != null)
                _markersHostGo.SetActive(true);

            ForceCombatHudMarkerPass();
        }

        private static void SoftOrHardHideFlightHud()
        {
            _flightHudSoftHide = false;
            try
            {
                FlightHud? flightHud = SceneSingleton<FlightHud>.i;
                _flightHudWasActive = IsCanvasActive(flightHud, "canvas");
                if (flightHud == null)
                    return;

                CombatHUD? combatHud = SceneSingleton<CombatHUD>.i;
                Canvas? flightCanvas = FlightHudCanvasField?.GetValue(flightHud) as Canvas;
                bool combatUnderFlight = combatHud != null
                    && flightCanvas != null
                    && (combatHud.transform == flightCanvas.transform
                        || combatHud.transform.IsChildOf(flightCanvas.transform));

                if (combatUnderFlight && flightCanvas != null)
                {
                    // Keep canvas alive so CombatHUD.LateUpdate still runs; hide ILS chrome only.
                    _flightHudSoftHide = true;
                    HideFlightHudVisuals(flightHud, flightCanvas.transform, combatHud!.transform);
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

        private static void HideFlightHudVisuals(FlightHud flightHud, Transform canvasRoot, Transform combatHudRoot)
        {
            for (int i = 0; i < canvasRoot.childCount; i++)
            {
                Transform child = canvasRoot.GetChild(i);
                if (child == combatHudRoot || combatHudRoot.IsChildOf(child) || child.IsChildOf(combatHudRoot))
                    continue;

                HideGo(child.gameObject);
            }

            HideComponent(flightHud.velocityVector);
            HideComponent(flightHud.waterline);
            HideComponent(flightHud.virtualJoystickPos);
            HideComponent(GetFlightHudField(flightHud, "virtualJoystickVector") as Component);
            HideComponent(GetFlightHudField(flightHud, "compass") as Component);
            HideComponent(GetFlightHudField(flightHud, "pitchCompass") as Component);
            HideGo(GetFlightHudField(flightHud, "pitchCompassCenter") as GameObject);
            if (GetFlightHudField(flightHud, "HUDCenter") is Transform hudCenter)
                HideGo(hudCenter.gameObject);
        }

        private static object? GetFlightHudField(FlightHud flightHud, string name) =>
            AccessToolsField(typeof(FlightHud), name)?.GetValue(flightHud);

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

        private static void BorrowIconLayerAboveOverlay()
        {
            CombatHUD? hud = SceneSingleton<CombatHUD>.i;
            if (hud == null || hud.iconLayer == null)
                return;

            EnsureMarkersHost();
            if (_markersRoot == null)
                return;

            _iconLayer = hud.iconLayer;
            _iconOriginalParent = _iconLayer.parent;
            _iconOriginalSibling = _iconLayer.GetSiblingIndex();
            ReparentForScreenOverlay(_iconLayer, _markersRoot);
            _iconLayer.gameObject.SetActive(true);
            _iconReparented = true;

            if (TargetArrowField?.GetValue(hud) is Image arrow && arrow != null)
            {
                Transform borrow = arrow.transform;
                if (TargetTextField?.GetValue(hud) is Text text
                    && text != null
                    && text.transform.parent != null
                    && text.transform.parent == arrow.transform.parent)
                {
                    borrow = arrow.transform.parent;
                }

                _arrowRoot = borrow;
                _arrowOriginalParent = borrow.parent;
                _arrowOriginalSibling = borrow.GetSiblingIndex();
                ReparentForScreenOverlay(borrow, _markersRoot);
                borrow.gameObject.SetActive(true);
                _arrowReparented = true;
            }
        }

        private static void ReparentForScreenOverlay(Transform node, Transform parent)
        {
            // worldPositionStays:false — dump markers assign screen pixels to .position each LateUpdate.
            node.SetParent(parent, worldPositionStays: false);
            node.localPosition = Vector3.zero;
            node.localRotation = Quaternion.identity;
            node.localScale = Vector3.one;

            if (node is RectTransform rt)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }

        private static void RestoreIconLayer()
        {
            if (_iconReparented && _iconLayer != null && _iconOriginalParent != null)
            {
                _iconLayer.SetParent(_iconOriginalParent, worldPositionStays: false);
                _iconLayer.localScale = Vector3.one;
                int max = Mathf.Max(0, _iconOriginalParent.childCount - 1);
                _iconLayer.SetSiblingIndex(Mathf.Clamp(_iconOriginalSibling, 0, max));
            }

            if (_arrowReparented && _arrowRoot != null && _arrowOriginalParent != null)
            {
                _arrowRoot.SetParent(_arrowOriginalParent, worldPositionStays: false);
                _arrowRoot.localScale = Vector3.one;
                int max = Mathf.Max(0, _arrowOriginalParent.childCount - 1);
                _arrowRoot.SetSiblingIndex(Mathf.Clamp(_arrowOriginalSibling, 0, max));
            }

            _iconLayer = null;
            _iconOriginalParent = null;
            _iconReparented = false;
            _arrowRoot = null;
            _arrowOriginalParent = null;
            _arrowReparented = false;
            DestroyMarkersHost();
        }

        private static void ForceCombatHudMarkerPass()
        {
            CombatHUD? hud = SceneSingleton<CombatHUD>.i;
            if (hud == null || hud.aircraft == null)
                return;

            // Ensure the behaviour can run even if a parent canvas was soft-disabled wrongly.
            if (!hud.gameObject.activeInHierarchy)
                hud.gameObject.SetActive(true);

            try
            {
                UpdateMarkersMethod?.Invoke(hud, null);
                UpdateHitMarkersMethod?.Invoke(hud, null);
            }
            catch
            {
                // ignore reflection failures
            }
        }

        private static void EnsureMarkersHost()
        {
            if (_markersHostGo != null && _markersRoot != null)
            {
                _markersHostGo.SetActive(true);
                return;
            }

            DestroyMarkersHost();

            _markersHostGo = new GameObject(MarkersHostName);
            Object.DontDestroyOnLoad(_markersHostGo);
            _markersHostGo.hideFlags = HideFlags.HideAndDontSave;

            Canvas canvas = _markersHostGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = MarkersSortingOrder;
            canvas.pixelPerfect = false;

            var scaler = _markersHostGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            var rootGo = new GameObject("MarkersRoot", typeof(RectTransform));
            rootGo.transform.SetParent(_markersHostGo.transform, false);
            _markersRoot = rootGo.GetComponent<RectTransform>();
            _markersRoot.anchorMin = Vector2.zero;
            _markersRoot.anchorMax = Vector2.one;
            _markersRoot.offsetMin = Vector2.zero;
            _markersRoot.offsetMax = Vector2.zero;
        }

        private static void DestroyMarkersHost()
        {
            if (_markersHostGo != null)
            {
                Object.Destroy(_markersHostGo);
                _markersHostGo = null;
            }

            _markersRoot = null;
        }

        private static void HideCombatHudChromeExceptMarkers()
        {
            CombatHUD? hud = SceneSingleton<CombatHUD>.i;
            if (hud == null)
                return;

            HideGo(TopRightPanelField?.GetValue(hud) as GameObject);
            HideComponent(ThreatListField?.GetValue(hud) as Component);
            HideComponent(WeaponStatusField?.GetValue(hud) as Component);
            HideGo(CountermeasureBgField?.GetValue(hud) as GameObject);
            HideComponent(TargetDesignatorField?.GetValue(hud) as Component);
            HideComponent(TargetInfoField?.GetValue(hud) as Component);
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

            _hiddenChrome.Add((go, go.activeSelf));
            if (go.activeSelf)
                go.SetActive(false);
        }

        private static void RestoreCombatHudChrome()
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
