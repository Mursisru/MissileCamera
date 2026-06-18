using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    internal sealed class MissileCameraHudOverlay
    {
        private const string RootName = "MissileCameraHudOverlay";

        private RectTransform? _root;
        private MissileCameraCornerHud? _corners;
        private MissileCameraAttitudeWidget? _attitude;
        private MissileCameraTargetMarker? _targetMarker;
        private HudRingGraphic? _interceptRing;
        private RectTransform? _interceptRoot;
        private TargetScreenUI? _screenUi;
        private float _nextDynamicTime;
        private const float DynamicInterval = 1f / 15f;

        internal void EnsureBuilt(RectTransform layoutRt, TargetScreenUI? screenUi)
        {
            MissileCameraHudConfig.Refresh();
            _screenUi = screenUi;

            float contentRotationZ = MfdLayoutController.ActiveStubContentRotationZ;
            RectTransform viewRt = MissileCameraFeedLayout.EnsureRotatedView(layoutRt, contentRotationZ);

            if (_root != null && _root.parent == viewRt)
            {
                _corners?.BindScreenUi(screenUi);
                RectTransform? panelRt = FindMissileCameraPanel(layoutRt);
                ApplyLegacyStubVisibility(panelRt ?? layoutRt, hide: MissileCameraHudConfig.Enabled);
                return;
            }

            Destroy();

            var rootGo = new GameObject(RootName, typeof(RectTransform));
            rootGo.transform.SetParent(viewRt, false);
            _root = rootGo.GetComponent<RectTransform>();
            Stretch(_root);

            _corners = MissileCameraCornerHud.Create(_root, screenUi);
            _attitude = MissileCameraAttitudeWidget.Create(_root);
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

            bool hudEnabled = MissileCameraHudConfig.Enabled;
            _root.gameObject.SetActive(hudEnabled && snapshot.HasFeed);
            if (!_root.gameObject.activeSelf)
                return;

            if (updateCorners)
                _corners?.Update(snapshot, panel);

            if (!updateDynamic)
                return;

            _nextDynamicTime = Time.unscaledTime + DynamicInterval;

            bool showCenter = MissileCameraHudConfig.ShowCenterCluster;
            _attitude?.SetVisible(showCenter);
            if (showCenter)
                _attitude?.Update(snapshot, panel.MinSide);

            UpdateIntercept(snapshot, viewRt, feedCamera, panel.MinSide, showCenter);

            bool showMarker = MissileCameraHudConfig.ShowTargetMarker && snapshot.HasTarget;
            FeedProjection targetProjection = showMarker && feedCamera != null
                ? FeedScreenProjector.Project(feedCamera, viewRt, snapshot.TargetPosition)
                : FeedProjection.Invalid;
            _targetMarker?.Update(targetProjection, panel.MinSide, showMarker);
        }

        internal void InvalidateDynamicSchedule() => _nextDynamicTime = 0f;

        internal void InvalidateCornerLayout() => _corners?.InvalidateLayout();

        internal void Destroy()
        {
            if (_root != null)
                Object.Destroy(_root.gameObject);

            _root = null;
            _corners = null;
            _attitude = null;
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
            SetChildActive(searchRoot, "MissileCameraTitle", !hide);
            SetChildActive(searchRoot, "MissileCameraColor", !hide);
            SetChildActive(searchRoot, "MissileTelemetry", !hide);
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

        private void UpdateIntercept(MissileCameraHudSnapshot snapshot, RectTransform viewRt, Camera? feedCamera, float minSide, bool showCenter)
        {
            if (_interceptRing == null || _interceptRoot == null)
                return;

            bool show = showCenter && snapshot.HasAimPoint && feedCamera != null;
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

            float radius = Mathf.Clamp(minSide * 0.028f, 4f, 12f);
            float thickness = Mathf.Max(1.2f, radius * 0.35f);
            _interceptRing.SetRing(radius, thickness, MissileCameraHudConfig.InterceptColor, filled: true);
        }

        private static void SetChildActive(RectTransform layoutRt, string childName, bool active)
        {
            Transform? child = layoutRt.Find(childName);
            if (child != null)
                child.gameObject.SetActive(active);
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
