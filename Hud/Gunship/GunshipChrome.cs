using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// Shared white gunship chrome — large COD-style type, thin lines.
    /// </summary>
    internal static class GunshipChrome
    {
        internal static readonly Color White = new Color(1f, 1f, 1f, 0.95f);
        internal static readonly Color WhiteDim = new Color(1f, 1f, 1f, 0.72f);
        internal static readonly Color WhiteSoft = new Color(1f, 1f, 1f, 0.35f);
        internal static readonly Color PanelBg = new Color(0f, 0f, 0f, 0.38f);
        internal static readonly Color FuelWarn = new Color(1f, 0.55f, 0.2f, 0.95f);

        internal const int FontCallsign = 36;
        internal const int FontStatus = 18;
        internal const int FontBody = 17;
        internal const int FontSmall = 15;

        internal static Text CreateText(RectTransform parent, string name, TextAnchor align, int fontSize = 17)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text t = go.GetComponent<Text>();
            t.font = HudFontHelper.GetFont();
            t.fontSize = fontSize;
            t.color = White;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            // COD HUD look: semi-bold weight via style when available
            t.fontStyle = FontStyle.Bold;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.82f);
            outline.effectDistance = new Vector2(1.1f, 1.1f);
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            return t;
        }

        internal static Image CreateImage(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            UiImageHelper.ApplySolid(img, color);
            img.raycastTarget = false;
            return img;
        }

        internal static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        internal static void Place(
            RectTransform rt,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPos,
            Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }
    }
}
