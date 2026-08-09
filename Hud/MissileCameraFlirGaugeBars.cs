using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// Laconic white edge gauges for gunship HUD.
    /// RC contract: Update(HudSnapshot, PanelMetrics) + _displayThrottle / _displayReady / _lastThrottle.
    /// </summary>
    internal sealed class MissileCameraFlirGaugeBars
    {
        private static readonly Color BarWhite = new Color(1f, 1f, 1f, 0.88f);
        private static readonly Color FuelLow = new Color(1f, 0.55f, 0.22f, 0.92f);
        private static readonly Color Track = new Color(1f, 1f, 1f, 0.18f);

        private const float FuelWarnFraction = 0.25f;
        private const float FractionEpsilon = 0.002f;

        private readonly Gauge _fuel;
        private readonly Gauge _throttle;
        private float _layoutW = -1f;
        private float _layoutH = -1f;
        private float _lastFuel = -1f;
        private float _lastThrottle = -1f;
        private float _displayFuel = -1f;
        private float _displayThrottle = -1f;
        private bool _displayReady;
        private int _lastFuelPct = int.MinValue;
        private int _lastThrPct = int.MinValue;

        private MissileCameraFlirGaugeBars(Gauge fuel, Gauge throttle)
        {
            _fuel = fuel;
            _throttle = throttle;
        }

        internal static MissileCameraFlirGaugeBars Create(RectTransform parent)
        {
            Gauge fuel = CreateGauge(parent, "FlirFuelGauge", "FUEL");
            Gauge throttle = CreateGauge(parent, "FlirThrottleGauge", "THR");
            return new MissileCameraFlirGaugeBars(fuel, throttle);
        }

        internal void Update(MissileCameraHudSnapshot snapshot, MissileCameraPanelMetrics panel)
        {
            if (!Mathf.Approximately(panel.Width, _layoutW)
                || !Mathf.Approximately(panel.Height, _layoutH))
            {
                _layoutW = panel.Width;
                _layoutH = panel.Height;
                ApplyLayout(panel);
            }

            float fuelTarget = Mathf.Clamp01(snapshot.FuelFraction);
            float thrTarget = Mathf.Clamp01(snapshot.ThrottleFraction);

            // No display smoothing — RC Harmony may still snap _displayThrottle / read _displayReady.
            if (!_displayReady)
                _displayReady = true;
            _displayFuel = fuelTarget;
            _displayThrottle = thrTarget;

            SetFill(_fuel, _displayFuel, fuelStyle: true, ref _lastFuel);
            SetFill(_throttle, _displayThrottle, fuelStyle: false, ref _lastThrottle);
            SetPercent(_fuel, _displayFuel, ref _lastFuelPct);
            SetPercent(_throttle, _displayThrottle, ref _lastThrPct);
        }

        private void ApplyLayout(MissileCameraPanelMetrics panel)
        {
            // Thin edge rails mid-height — sit clear of telemetery / weapon block.
            float barH = Mathf.Clamp(panel.Height * 0.22f, 90f, 150f);
            float barW = 3f;
            float edgeInset = Mathf.Max(panel.HorizontalInset, 8f) + 2f;
            float labelGap = 5f;
            int fontSize = 10;

            float barX = edgeInset + barW * 0.5f;
            PlaceBar(_fuel.Root, leftEdge: true, barX, barW, barH);
            PlaceBar(_throttle.Root, leftEdge: false, barX, barW, barH);

            PlaceSideLabel(_fuel.Label, leftEdge: true, barW, labelGap, fontSize);
            PlaceSideLabel(_throttle.Label, leftEdge: false, barW, labelGap, fontSize);
            PlacePercent(_fuel.Value, barH, fontSize);
            PlacePercent(_throttle.Value, barH, fontSize);

            Font font = HudFontHelper.GetFont();
            ApplyTextStyle(_fuel.Label, font, fontSize, BarWhite);
            ApplyTextStyle(_throttle.Label, font, fontSize, BarWhite);
            ApplyTextStyle(_fuel.Value, font, fontSize, BarWhite);
            ApplyTextStyle(_throttle.Value, font, fontSize, BarWhite);

            SetFill(_fuel, Mathf.Max(0f, _lastFuel), fuelStyle: true, ref _lastFuel, force: true);
            SetFill(_throttle, Mathf.Max(0f, _lastThrottle), fuelStyle: false, ref _lastThrottle, force: true);
        }

        private static void PlaceBar(RectTransform root, bool leftEdge, float edgeOffset, float barW, float barH)
        {
            float anchorX = leftEdge ? 0f : 1f;
            root.anchorMin = new Vector2(anchorX, 0.5f);
            root.anchorMax = new Vector2(anchorX, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = new Vector2(leftEdge ? edgeOffset : -edgeOffset, 0f);
            root.sizeDelta = new Vector2(barW, barH);
        }

        private static void PlaceSideLabel(Text label, bool leftEdge, float barW, float gap, int fontSize)
        {
            RectTransform rt = label.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            float side = (barW * 0.5f) + gap + 7f;
            rt.anchoredPosition = new Vector2(leftEdge ? side : -side, 0f);
            rt.sizeDelta = new Vector2(48f, 12f);
            rt.localEulerAngles = new Vector3(0f, 0f, leftEdge ? -90f : 90f);
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static void PlacePercent(Text value, float barH, int fontSize)
        {
            RectTransform rt = value.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -(barH * 0.5f) - 2f);
            rt.sizeDelta = new Vector2(36f, 12f);
            value.fontSize = fontSize;
            value.alignment = TextAnchor.UpperCenter;
            value.horizontalOverflow = HorizontalWrapMode.Overflow;
            value.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static void ApplyTextStyle(Text text, Font font, int fontSize, Color color)
        {
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;
        }

        private static void SetPercent(Gauge gauge, float fraction, ref int lastPct)
        {
            int pct = Mathf.RoundToInt(fraction * 100f);
            if (pct == lastPct)
                return;
            lastPct = pct;
            gauge.Value.text = pct.ToString(CultureInfo.InvariantCulture) + '%';
        }

        private static void SetFill(
            Gauge gauge,
            float fraction,
            bool fuelStyle,
            ref float last,
            bool force = false)
        {
            fraction = Mathf.Clamp01(fraction);
            if (!force && Mathf.Abs(fraction - last) < FractionEpsilon)
                return;

            last = fraction;
            RectTransform fillRt = gauge.Fill.rectTransform;
            float top = Mathf.Max(fraction, 0.001f);
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(1f, top);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            gauge.Fill.enabled = fraction > 0.001f;

            Color c = fuelStyle && fraction <= FuelWarnFraction ? FuelLow : BarWhite;
            gauge.Fill.color = c;
            gauge.Value.color = c;
        }

        private static Gauge CreateGauge(RectTransform parent, string name, string labelText)
        {
            var rootGo = new GameObject(name, typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            RectTransform root = rootGo.GetComponent<RectTransform>();

            Image track = CreateImage(root, "Track", Track);
            Stretch(track.rectTransform);

            Image fill = CreateImage(root, "Fill", BarWhite);
            RectTransform fillRt = fill.rectTransform;
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(1f, 0f);
            fillRt.pivot = new Vector2(0.5f, 0f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            Text label = CreateLabel(root, "Label", labelText);
            Text value = CreateLabel(root, "Value", "0%");
            return new Gauge(root, fill, label, value);
        }

        private static Image CreateImage(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            UiImageHelper.ApplySolid(img, color);
            img.raycastTarget = false;
            return img;
        }

        private static Text CreateLabel(RectTransform parent, string name, string text)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text label = go.GetComponent<Text>();
            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = BarWhite;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.7f);
            outline.effectDistance = new Vector2(0.5f, 0.5f);
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

        private readonly struct Gauge
        {
            internal Gauge(RectTransform root, Image fill, Text label, Text value)
            {
                Root = root;
                Fill = fill;
                Label = label;
                Value = value;
            }

            internal RectTransform Root { get; }
            internal Image Fill { get; }
            internal Text Label { get; }
            internal Text Value { get; }
        }
    }
}
