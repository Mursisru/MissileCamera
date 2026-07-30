using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    internal sealed class MissileCameraHudOverlay
    {
        private const string RootName = "MissileCameraHudOverlay";

        private RectTransform? _root;
        private MissileCameraCornerHud? _corners;
        private MissileCameraFlirHud? _flir;
        private MissileCameraAttitudeWidget? _attitude;
        private MissileCameraZoomIndicator? _zoomIndicator;
        private MissileCameraTargetMarker? _targetMarker;
        private HudRingGraphic? _interceptRing;
        private RectTransform? _interceptRoot;
        private TargetScreenUI? _screenUi;
        private float _nextDynamicTime;
        private const float DynamicInterval = 1f / 10f;

        internal RectTransform? Root => _root;

        internal static RectTransform? TryGetFlirRoot() => _flirRootStatic;

        private static RectTransform? _flirRootStatic;

        internal void EnsureBuilt(RectTransform layoutRt, TargetScreenUI? screenUi, float? contentRotationZOverride = null)
        {
            MissileCameraHudConfig.Refresh();
            _screenUi = screenUi;

            float contentRotationZ = contentRotationZOverride ?? MfdLayoutController.ActiveStubContentRotationZ;
            RectTransform viewRt = MissileCameraFeedLayout.EnsureRotatedView(layoutRt, contentRotationZ);

            if (_root != null && _root.parent == viewRt)
            {
                MissileCameraFeedLayout.ApplyContentRotation(layoutRt, contentRotationZ);
                _corners?.BindScreenUi(screenUi);
                _zoomIndicator?.BindScreenUi(screenUi);
                RectTransform? panelRt = FindMissileCameraPanel(layoutRt);
                ApplyLegacyStubVisibility(panelRt ?? layoutRt, hide: MissileCameraHudConfig.Enabled);
                return;
            }

            Destroy();

            try
            {
                var rootGo = new GameObject(RootName, typeof(RectTransform));
                rootGo.transform.SetParent(viewRt, false);
                _root = rootGo.GetComponent<RectTransform>();
                Stretch(_root);

                _corners = MissileCameraCornerHud.Create(_root, screenUi);
                _flir = MissileCameraFlirHud.Create(_root);
                _flirRootStatic = _flir.Root;
                _attitude = MissileCameraAttitudeWidget.Create(_root);
                _zoomIndicator = MissileCameraZoomIndicator.Create(_root, screenUi);
                _targetMarker = MissileCameraTargetMarker.Create(_root);

                var interceptGo = new GameObject("MissileCameraHudIntercept", typeof(RectTransform), typeof(HudRingGraphic));
                interceptGo.transform.SetParent(_root, false);
                _interceptRoot = interceptGo.GetComponent<RectTransform>();
                _interceptRoot.anchorMin = new Vector2(0.5f, 0.5f);
                _interceptRoot.anchorMax = new Vector2(0.5f, 0.5f);
                _interceptRoot.pivot = new Vector2(0.5f, 0.5f);
                _interceptRoot.anchoredPosition = Vector2.zero;
                _interceptRoot.sizeDelta = Vector2.zero;
                _interceptRing = interceptGo.GetComponent<HudRingGraphic>();

                ApplyLegacyStubVisibility(FindMissileCameraPanel(layoutRt) ?? layoutRt, hide: MissileCameraHudConfig.Enabled);
                MissileCameraMissionLifecycleDiag.Info("HudOverlay.EnsureBuilt ok");
            }
            catch (System.Exception ex)
            {
                MissileCameraMissionLifecycleDiag.Warn(
                    "HudOverlay.EnsureBuilt failed: " + ex.GetType().Name + ": " + ex.Message
                    + " | " + ex.StackTrace);
                // Clear half-built chrome; feed RawImage must still work without HUD.
                Destroy();
            }
        }

        internal void Update(
            MissileCameraHudSnapshot snapshot,
            RectTransform layoutRt,
            RectTransform viewRt,
            Camera? feedCamera,
            MissileCameraPanelMetrics panel,
            RectTransform? panelRt = null,
            bool updateCorners = true,
            bool updateDynamic = false)
        {
            if (_root == null)
                return;

            UpdateZoomIndicatorVisibility();

            // Screen-owner rule: keep MC HUD chrome alive whenever this overlay is driven.
            // Do NOT gate the whole root on snapshot.HasFeed — RawImage can show pixels while
            // RT/rig briefly reports no Texture, which previously blanked CornerHud entirely.
            bool hudEnabled = MissileCameraHudConfig.Enabled;
            if (_root.gameObject.activeSelf != hudEnabled)
                _root.gameObject.SetActive(hudEnabled);
            if (!_root.gameObject.activeSelf)
            {
                MissileCameraCockpitPipController.Tick(null, panel);
                return;
            }

            bool bootPlaying = MissileCameraFullscreenController.IsActive
                && MissileCameraFullscreenBootstrap.IsRunning;
            bool flir = MissileCameraHudConfig.UseFullscreenFlirHud;

            // During FS boot, BootSequence owns FlirHud — hide leftover center chrome on FS panel only.
            if (bootPlaying)
            {
                _corners?.SetVisible(false);
                if (flir)
                    _flir?.UpdateGaugeBarsOnly(snapshot, panel);

                _attitude?.SetVisible(false);
                _targetMarker?.SetVisible(false);
                if (_interceptRoot != null)
                    _interceptRoot.gameObject.SetActive(false);
                _zoomIndicator?.UpdateVisibility();
                MissileCameraCockpitPipController.Tick(null, panel);
                return;
            }

            // MFD classic HUD: force corners on whenever FLIR is not the owner.
            if (!flir)
            {
                _corners?.SetVisible(true);
                Transform? cornersNode = _root.Find("MissileCameraHudCorners");
                if (cornersNode != null && !cornersNode.gameObject.activeSelf)
                    cornersNode.gameObject.SetActive(true);
            }
            else
            {
                _corners?.SetVisible(false);
            }

            _flir?.SetVisible(flir);

            if (flir)
            {
                if (updateCorners || updateDynamic)
                    _flir?.Update(snapshot, panel);
            }
            else if (updateCorners)
            {
                _corners?.Update(snapshot, panel);
            }

            MissileCameraCockpitPipController.Tick(null, panel);

            if (!updateDynamic)
                return;

            _nextDynamicTime = Time.unscaledTime + DynamicInterval;

            bool showCenter = MissileCameraHudConfig.ShowCenterCluster && !flir;
            _attitude?.SetVisible(showCenter);
            if (showCenter)
                _attitude?.Update(snapshot, panel.MinSide);

            // Intercept aimPoint: MFD = filled green; FS FLIR = hollow green ring only.
            bool showIntercept = MissileCameraHudConfig.ShowCenterCluster && snapshot.HasFeed;
            UpdateIntercept(
                snapshot,
                viewRt,
                feedCamera,
                panel.MinSide,
                showIntercept,
                filled: !flir);

            UpdateTargetMarker(snapshot, viewRt, feedCamera, panel.MinSide, !flir);
        }

        private void UpdateTargetMarker(
            MissileCameraHudSnapshot snapshot,
            RectTransform viewRt,
            Camera? feedCamera,
            float minSide,
            bool mfdClassic)
        {
            if (_targetMarker == null)
                return;

            bool show = mfdClassic
                && MissileCameraHudConfig.ShowTargetMarker
                && snapshot.HasFeed
                && snapshot.HasTarget
                && feedCamera != null;

            if (!show)
            {
                _targetMarker.SetVisible(false);
                return;
            }

            FeedProjection projection = FeedScreenProjector.Project(
                feedCamera!,
                viewRt,
                snapshot.TargetPosition);
            _targetMarker.Update(projection, minSide, visible: true);
        }

        internal void InvalidateDynamicSchedule() => _nextDynamicTime = 0f;

        internal void InvalidateCornerLayout()
        {
            _corners?.InvalidateLayout();
            _flir?.InvalidateLayout();
        }

        internal void NotifyZoomChanged(float zoomOffset) => _zoomIndicator?.Show(zoomOffset);

        internal void UpdateZoomIndicatorVisibility() => _zoomIndicator?.UpdateVisibility();

        internal void ForceFlirUpdate(MissileCameraHudSnapshot snapshot, MissileCameraPanelMetrics panel)
        {
            if (_flir == null)
                return;

            _flir.SetVisible(true);
            _flir.Update(snapshot, panel);
            _flirRootStatic = _flir.Root;
        }

        internal void Destroy()
        {
            // Always drop C# refs even if Unity objects were already scene-destroyed.
            try { MissileCameraCockpitPipController.Shutdown(); }
            catch { /* ignore */ }

            try { _flir?.Shutdown(); }
            catch { /* ignore */ }

            try
            {
                if (_root != null)
                    Object.Destroy(_root.gameObject);
            }
            catch { /* ignore */ }

            _root = null;
            _corners = null;
            _flir = null;
            _flirRootStatic = null;
            _attitude = null;
            _zoomIndicator = null;
            _targetMarker = null;
            _interceptRing = null;
            _interceptRoot = null;
        }

        private static RectTransform? FindMissileCameraPanel(RectTransform layoutRt)
        {
            Transform? node = layoutRt;
            while (node != null)
            {
                if (node.name == "MissileCameraPanel" && node.TryGetComponent(out RectTransform panelRt))
                    return panelRt;

                node = node.parent;
            }

            return null;
        }

        internal static void ApplyLegacyStubVisibility(RectTransform searchRoot, bool hide)
        {
            if (searchRoot == null)
                return;

            // Fullscreen must never show legacy stubs regardless of HudConfig.
            if (MissileCameraFullscreenController.IsActive)
                hide = true;

            SetChildActiveDeep(searchRoot, "MissileCameraTitle", !hide);
            SetChildActiveDeep(searchRoot, "MissileCameraColor", !hide);
            SetChildActiveDeep(searchRoot, "MissileTelemetry", !hide);
        }

        internal static void ApplyPanelBackground(Image? panelImage, TargetScreenUI? screenUi)
        {
            if (panelImage == null)
                return;

            if (MfdLayoutConfig.DebugStub)
            {
                if (screenUi != null)
                    UiImageHelper.ApplySolid(panelImage, TargetScreenUiStyle.GetStubPanelColor(screenUi));
                return;
            }

            if (MissileCameraHudConfig.Enabled)
            {
                Color transparent = screenUi != null
                    ? TargetScreenUiStyle.GetStubPanelColor(screenUi)
                    : new Color(0.05f, 0.08f, 0.14f, 1f);
                transparent.a = 0f;
                UiImageHelper.ApplySolid(panelImage, transparent);
                return;
            }

            if (screenUi != null)
                UiImageHelper.ApplySolid(panelImage, TargetScreenUiStyle.GetStubPanelColor(screenUi));
        }

        private void UpdateIntercept(
            MissileCameraHudSnapshot snapshot,
            RectTransform viewRt,
            Camera? feedCamera,
            float minSide,
            bool showIntercept,
            bool filled)
        {
            if (_interceptRing == null || _interceptRoot == null)
                return;

            bool show = showIntercept && snapshot.HasAimPoint && feedCamera != null;
            if (!show)
            {
                _interceptRoot.gameObject.SetActive(false);
                return;
            }

            FeedProjection projection = FeedScreenProjector.Project(feedCamera!, viewRt, snapshot.AimPoint);
            if (!projection.Valid || !projection.InFront)
            {
                _interceptRoot.gameObject.SetActive(false);
                return;
            }

            _interceptRoot.gameObject.SetActive(true);
            _interceptRoot.anchoredPosition = projection.AnchoredPosition;

            float radius = Mathf.Clamp(minSide * 0.022f, 4f, 10f);
            float thickness = Mathf.Max(1.2f, radius * 0.18f);
            Color color = filled
                ? MissileCameraHudConfig.InterceptColor
                : MissileCameraHudConfig.FsInterceptRingColor;
            _interceptRing.SetRing(radius, thickness, color, filled);
        }

        private static void SetChildActiveDeep(RectTransform layoutRt, string childName, bool active)
        {
            Transform? child = FindDeep(layoutRt, childName);
            if (child == null)
                return;

            child.gameObject.SetActive(active);
            if (!active && child.TryGetComponent(out Text text))
            {
                text.text = string.Empty;
                text.enabled = false;
            }
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

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
