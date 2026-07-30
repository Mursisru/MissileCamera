using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>Grouped FLIR telemetry block — hollow green frame + title/body (no fill).</summary>
    internal sealed class MissileCameraFlirPanel
    {
        private static readonly Color FlirGreen = new Color(0.55f, 1f, 0.9f, 1f);

        private const float BorderPx = 2.0f;
        private const float TitleH = 15f;
        private const float Pad = 4f;

        private readonly RectTransform _root;
        private readonly Text _title;
        private readonly Text _body;
        private string _lastTitle = string.Empty;
        private string _lastBody = string.Empty;

        private MissileCameraFlirPanel(RectTransform root, Text title, Text body)
        {
            _root = root;
            _title = title;
            _body = body;
        }

        internal static MissileCameraFlirPanel Create(RectTransform parent, string name, TextAnchor align)
        {
            var rootGo = new GameObject(name, typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            RectTransform root = rootGo.GetComponent<RectTransform>();

            CreateBorderEdge(root, "EdgeT", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -BorderPx), Vector2.zero);
            CreateBorderEdge(root, "EdgeB", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, BorderPx));
            CreateBorderEdge(root, "EdgeL", new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(BorderPx, 0f));
            CreateBorderEdge(root, "EdgeR", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-BorderPx, 0f), Vector2.zero);
            // Title rule under header — visual grouping without fill.
            CreateBorderEdge(
                root,
                "TitleRule",
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(BorderPx, -(BorderPx + TitleH)),
                new Vector2(-BorderPx, -(BorderPx + TitleH - BorderPx)));

            Text title = CreateText(root, "Title", align);
            RectTransform titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.offsetMin = new Vector2(Pad + BorderPx, -(BorderPx + TitleH));
            titleRt.offsetMax = new Vector2(-(Pad + BorderPx), -BorderPx);
            title.fontStyle = FontStyle.Bold;

            Text body = CreateText(root, "Body", align);
            RectTransform bodyRt = body.rectTransform;
            bodyRt.anchorMin = Vector2.zero;
            bodyRt.anchorMax = Vector2.one;
            bodyRt.offsetMin = new Vector2(Pad + BorderPx, Pad + BorderPx);
            bodyRt.offsetMax = new Vector2(-(Pad + BorderPx), -(BorderPx + TitleH + 2f));

            return new MissileCameraFlirPanel(root, title, body);
        }

        internal RectTransform Root => _root;

        internal void Place(
            float anchorX,
            float anchorY,
            float x,
            float y,
            float w,
            float h,
            TextAnchor titleAlign,
            TextAnchor bodyAlign)
        {
            _root.anchorMin = new Vector2(anchorX, anchorY);
            _root.anchorMax = new Vector2(anchorX, anchorY);
            _root.pivot = new Vector2(anchorX, anchorY);
            _root.anchoredPosition = new Vector2(x, y);
            _root.sizeDelta = new Vector2(w, h);
            _title.alignment = titleAlign;
            _body.alignment = bodyAlign;
        }

        internal void ApplyFont(Font font, int titleSize, int bodySize)
        {
            _title.font = font;
            _body.font = font;
            _title.fontSize = titleSize;
            _body.fontSize = bodySize;
            _title.color = FlirGreen;
            _body.color = FlirGreen;
            _title.lineSpacing = 1f;
            _body.lineSpacing = 1.05f;
            _title.horizontalOverflow = HorizontalWrapMode.Overflow;
            _body.horizontalOverflow = HorizontalWrapMode.Overflow;
            _title.verticalOverflow = VerticalWrapMode.Truncate;
            _body.verticalOverflow = VerticalWrapMode.Truncate;
            _title.raycastTarget = false;
            _body.raycastTarget = false;
        }

        internal void SetTitle(string title)
        {
            if (_lastTitle == title)
                return;
            _lastTitle = title;
            _title.text = title;
        }

        internal void SetBody(string body)
        {
            if (_lastBody == body)
                return;
            _lastBody = body;
            _body.text = body;
        }

        internal void SetBody(StringBuilder sb) => SetBody(sb.ToString());

        private static void CreateBorderEdge(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            UiImageHelper.ApplySolid(img, FlirGreen);
            RectTransform rt = img.rectTransform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        private static Text CreateText(RectTransform parent, string name, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.alignment = align;
            text.color = FlirGreen;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(0.7f, 0.7f);
            text.raycastTarget = false;
            return text;
        }
    }
}
