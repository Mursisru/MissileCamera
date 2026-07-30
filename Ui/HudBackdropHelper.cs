using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    internal static class HudBackdropHelper
    {
        private static Sprite? _whiteSprite;

        private readonly struct TextWidthCacheEntry
        {
            internal TextWidthCacheEntry(string text, int fontSize, float width)
            {
                Text = text;
                FontSize = fontSize;
                Width = width;
            }

            internal string Text { get; }
            internal int FontSize { get; }
            internal float Width { get; }
        }

        private static readonly Dictionary<int, TextWidthCacheEntry> TextWidthCache = new Dictionary<int, TextWidthCacheEntry>();

        internal static Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null)
                return _whiteSprite;

            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 100f);
            return _whiteSprite;
        }

        internal static void Reset()
        {
            _whiteSprite = null;
            TextWidthCache.Clear();
        }

        internal static Image CreateBackdrop(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling();

            Image image = go.GetComponent<Image>();
            image.sprite = GetWhiteSprite();
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            image.material = null;
            return image;
        }

        internal static void StretchToBlock(Image backdrop)
        {
            if (backdrop == null)
                return;

            RectTransform rt = backdrop.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            float alpha = Mathf.Clamp01(MissileCameraHudConfig.LabelBackgroundAlpha);
            backdrop.enabled = alpha > 0.001f;
            if (!backdrop.enabled)
                return;

            Color fill = MissileCameraHudConfig.LabelBackgroundColor;
            fill.a = alpha;
            backdrop.color = fill;
        }

        internal static void InvalidateTextWidthCache() => TextWidthCache.Clear();

        internal static float MeasureTextWidth(Text text)
        {
            if (text == null)
                return 0f;

            string value = text.text;
            if (string.IsNullOrEmpty(value))
                return 0f;

            int id = text.GetInstanceID();
            if (TextWidthCache.TryGetValue(id, out TextWidthCacheEntry cached)
                && cached.Text == value
                && cached.FontSize == text.fontSize)
            {
                return cached.Width;
            }

            text.horizontalOverflow = HorizontalWrapMode.Overflow;

            RectTransform rt = text.rectTransform;
            Vector2 anchorMin = rt.anchorMin;
            Vector2 anchorMax = rt.anchorMax;
            Vector2 pivot = rt.pivot;
            Vector2 sizeDelta = rt.sizeDelta;
            Vector2 offsetMin = rt.offsetMin;
            Vector2 offsetMax = rt.offsetMax;
            Vector2 anchoredPosition = rt.anchoredPosition;

            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            Canvas.ForceUpdateCanvases();
            float width = text.preferredWidth;

            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = sizeDelta;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            rt.anchoredPosition = anchoredPosition;

            TextWidthCache[id] = new TextWidthCacheEntry(value, text.fontSize, width);
            return width;
        }
    }
}
