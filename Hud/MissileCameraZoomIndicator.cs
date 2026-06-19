using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    internal sealed class MissileCameraZoomIndicator
    {
        private readonly RectTransform _root;
        private readonly Image _backdrop;
        private readonly Text _label;
        private float _hideAtUnscaled = -1f;

        private MissileCameraZoomIndicator(RectTransform root, Image backdrop, Text label)
        {
            _root = root;
            _backdrop = backdrop;
            _label = label;
        }

        internal static MissileCameraZoomIndicator Create(RectTransform parent, TargetScreenUI? screenUi)
        {
            var rootGo = new GameObject("MissileCameraZoomIndicator", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            RectTransform root = rootGo.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0.5f, 0.55f);
            root.anchorMax = new Vector2(0.5f, 0.55f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(72f, 22f);

            Image backdrop = HudBackdropHelper.CreateBackdrop(root, "MissileCameraZoomIndicatorBackdrop");
            HudBackdropHelper.StretchToBlock(backdrop);

            var textGo = new GameObject("MissileCameraZoomIndicatorText", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(root, false);
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(MissileCameraPanelMetrics.RowEdgePad, 0f);
            textRt.offsetMax = new Vector2(-MissileCameraPanelMetrics.RowEdgePad, 0f);

            Text label = textGo.GetComponent<Text>();
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            ApplyFont(label, screenUi);

            var indicator = new MissileCameraZoomIndicator(root, backdrop, label);
            indicator.SetVisible(false);
            return indicator;
        }

        internal void BindScreenUi(TargetScreenUI? screenUi) => ApplyFont(_label, screenUi);

        internal void Show(float zoomOffset)
        {
            _label.text = FormatOffset(zoomOffset);
            _hideAtUnscaled = Time.unscaledTime + MissileCameraControlsConfig.IndicatorSeconds;
            SetVisible(true);
        }

        internal void UpdateVisibility()
        {
            if (_hideAtUnscaled < 0f)
                return;

            if (Time.unscaledTime >= _hideAtUnscaled)
            {
                _hideAtUnscaled = -1f;
                SetVisible(false);
            }
        }

        internal void Destroy()
        {
            if (_root != null)
                Object.Destroy(_root.gameObject);
        }

        private void SetVisible(bool visible) => _root.gameObject.SetActive(visible);

        private static string FormatOffset(float offset) =>
            offset.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

        private static void ApplyFont(Text label, TargetScreenUI? screenUi)
        {
            if (screenUi != null)
            {
                TargetScreenUiStyle.ApplyScaledStubText(label, screenUi, 14, 14, StubTextRole.Telemetry);
                return;
            }

            label.font = HudFontHelper.GetFont();
            label.fontSize = 14;
        }
    }
}
