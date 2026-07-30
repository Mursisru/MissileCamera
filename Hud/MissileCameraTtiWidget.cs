using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>Center Time-To-Impact progress bar + label.</summary>
    internal sealed class MissileCameraTtiWidget
    {
        private static readonly Color BarBack = new Color(0.15f, 0.15f, 0.18f, 0.75f);
        private static readonly Color BarFillCyan = new Color(0.15f, 0.85f, 0.95f, 0.95f);
        private static readonly Color BarFillRed = new Color(0.95f, 0.2f, 0.15f, 0.95f);

        private readonly RectTransform _root;
        private readonly Image _back;
        private readonly Image _fill;
        private readonly Text _title;
        private readonly Text _value;
        private float _lastFraction = -1f;
        private string _lastText = string.Empty;

        private MissileCameraTtiWidget(RectTransform root, Image back, Image fill, Text title, Text value)
        {
            _root = root;
            _back = back;
            _fill = fill;
            _title = title;
            _value = value;
        }

        internal static MissileCameraTtiWidget Create(RectTransform parent)
        {
            var rootGo = new GameObject("MissileCameraTti", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            RectTransform root = rootGo.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = new Vector2(0f, -48f);
            root.sizeDelta = new Vector2(280f, 40f);

            Image back = CreateBar(root, "TtiBack", BarBack);
            Stretch(back.rectTransform);
            Image fill = CreateBar(root, "TtiFill", BarFillCyan);
            RectTransform fillRt = fill.rectTransform;
            fillRt.anchorMin = new Vector2(0f, 0.35f);
            fillRt.anchorMax = new Vector2(0f, 0.75f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.anchoredPosition = Vector2.zero;
            fillRt.sizeDelta = new Vector2(0f, 0f);

            Text title = CreateText(root, "TtiTitle", "Time-To-Impact", TextAnchor.UpperCenter);
            RectTransform titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 0.75f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;

            Text value = CreateText(root, "TtiValue", string.Empty, TextAnchor.LowerCenter);
            RectTransform valueRt = value.rectTransform;
            valueRt.anchorMin = new Vector2(0f, 0f);
            valueRt.anchorMax = new Vector2(1f, 0.35f);
            valueRt.offsetMin = Vector2.zero;
            valueRt.offsetMax = Vector2.zero;

            return new MissileCameraTtiWidget(root, back, fill, title, value);
        }

        internal void Update(MissileCameraHudSnapshot snapshot, MissileCameraPanelMetrics panel)
        {
            bool show = snapshot.HasTimeToImpact && !string.IsNullOrEmpty(snapshot.TgpTtiText);
            _root.gameObject.SetActive(show);
            if (!show)
                return;

            float barW = Mathf.Clamp(panel.MinSide * 0.42f, 160f, 360f);
            _root.sizeDelta = new Vector2(barW, 42f);
            _root.anchoredPosition = new Vector2(0f, -Mathf.Clamp(panel.MinSide * 0.12f, 36f, 72f));

            float fraction = Mathf.Clamp01(snapshot.TimeToImpactFraction);
            if (!Mathf.Approximately(fraction, _lastFraction))
            {
                _lastFraction = fraction;
                RectTransform fillRt = _fill.rectTransform;
                fillRt.anchorMin = new Vector2(0f, 0.35f);
                fillRt.anchorMax = new Vector2(fraction, 0.75f);
                fillRt.offsetMin = Vector2.zero;
                fillRt.offsetMax = Vector2.zero;
                _fill.color = Color.Lerp(BarFillCyan, BarFillRed, fraction);
            }

            if (snapshot.TgpTtiText != _lastText)
            {
                _lastText = snapshot.TgpTtiText;
                _value.text = snapshot.TgpTtiText;
            }

            int font = MissileCameraPanelMetrics.IsGameFullscreen ? 16 : Mathf.Clamp(Mathf.RoundToInt(panel.MinSide * 0.035f), 10, 16);
            Font? fontAsset = HudFontHelper.GetFont();
            if (fontAsset != null)
            {
                _title.font = fontAsset;
                _value.font = fontAsset;
            }

            _title.fontSize = font;
            _value.fontSize = font;
        }

        internal void SetVisible(bool visible)
        {
            if (!visible)
                _root.gameObject.SetActive(false);
        }

        private static Image CreateBar(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static Text CreateText(RectTransform parent, string name, string text, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text label = go.GetComponent<Text>();
            label.text = text;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
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
