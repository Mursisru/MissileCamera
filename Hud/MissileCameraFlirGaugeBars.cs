using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// Fullscreen FLIR vertical FUEL / THR bars at screen edges.
    /// Side labels (rotated, toward center) + percent under the bar.
    /// </summary>
    internal sealed class MissileCameraFlirGaugeBars
    {
        private static readonly Color FlirGreen = new Color(0.2f, 1f, 0.45f, 1f);
        private static readonly Color FuelLow = new Color(1f, 0.55f, 0.12f, 1f);
        private static readonly Color FrameHole = new Color(0f, 0f, 0f, 0.94f);
        private static readonly Color TickDim = new Color(0.2f, 1f, 0.45f, 0.35f);

        private const float BorderPx = 1.5f;
        private const float FuelWarnFraction = 0.25f;
        private const float FractionEpsilon = 0.004f;
        private const int TickCount = 5;

        private readonly Gauge _fuel;
        private readonly Gauge _throttle;
        private float _layoutW = -1f;
        private float _layoutH = -1f;
        private float _lastFuel = -1f;
        private float _lastThrottle = -1f;
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

            float fuel = Mathf.Clamp01(snapshot.FuelFraction);
            float thr = Mathf.Clamp01(snapshot.ThrottleFraction);
            SetFill(_fuel, fuel, fuelStyle: true, ref _lastFuel);
            SetFill(_throttle, thr, fuelStyle: false, ref _lastThrottle);
            SetPercent(_fuel, fuel, ref _lastFuelPct);
            SetPercent(_throttle, thr, ref _lastThrPct);
        }

        private void ApplyLayout(MissileCameraPanelMetrics panel)
        {
            float barH = Mathf.Clamp(panel.Height * 0.28f, 130f, 220f);
            float barW = 14f;
            float edgeInset = Mathf.Max(panel.HorizontalInset, 10f);
            float labelGap = 6f;
            float labelW = 56f;
            float labelH = 14f;
            float valueH = 14f;
            int fontSize = 11;

            // Bar hugs the edge; label sits toward center of screen.
            float barX = edgeInset + barW * 0.5f;
            PlaceBar(_fuel.Root, leftEdge: true, barX, barW, barH);
            PlaceBar(_throttle.Root, leftEdge: false, barX, barW, barH);

            PlaceSideLabel(_fuel.Label, insideTowardCenter: true, leftEdge: true, barW, labelGap, labelW, labelH, fontSize);
            PlaceSideLabel(_throttle.Label, insideTowardCenter: true, leftEdge: false, barW, labelGap, labelW, labelH, fontSize);
            PlacePercent(_fuel.Value, barH, valueH, fontSize);
            PlacePercent(_throttle.Value, barH, valueH, fontSize);
            PlaceTicks(_fuel, barW, barH);
            PlaceTicks(_throttle, barW, barH);

            Font font = HudFontHelper.GetFont();
            ApplyTextStyle(_fuel.Label, font, fontSize, FlirGreen);
            ApplyTextStyle(_throttle.Label, font, fontSize, FlirGreen);
            ApplyTextStyle(_fuel.Value, font, fontSize, FlirGreen);
            ApplyTextStyle(_throttle.Value, font, fontSize, FlirGreen);

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

        private static void PlaceSideLabel(
            Text label,
            bool insideTowardCenter,
            bool leftEdge,
            float barW,
            float gap,
            float labelW,
            float labelH,
            int fontSize)
        {
            RectTransform rt = label.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            float side = (barW * 0.5f) + gap + (labelH * 0.5f);
            bool positiveX = leftEdge == insideTowardCenter;
            rt.anchoredPosition = new Vector2(positiveX ? side : -side, 0f);
            rt.sizeDelta = new Vector2(labelW, labelH);
            rt.localEulerAngles = new Vector3(0f, 0f, positiveX ? -90f : 90f);
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
        }

        /// <summary>Percent sits just under the bar (parent is bar root, center-pivoted).</summary>
        private static void PlacePercent(Text value, float barH, float valueH, int fontSize)
        {
            RectTransform rt = value.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -(barH * 0.5f) - 3f);
            rt.sizeDelta = new Vector2(40f, valueH);
            value.fontSize = fontSize;
            value.alignment = TextAnchor.UpperCenter;
            value.horizontalOverflow = HorizontalWrapMode.Overflow;
            value.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static void PlaceTicks(Gauge gauge, float barW, float barH)
        {
            float innerH = barH - BorderPx * 2f;
            for (int i = 0; i < TickCount; i++)
            {
                float frac = (i + 1) / (float)(TickCount + 1);
                float y = -barH * 0.5f + BorderPx + innerH * frac;
                Image tick = gauge.Ticks[i];
                RectTransform rt = tick.rectTransform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, y);
                rt.sizeDelta = new Vector2(Mathf.Max(2f, barW - BorderPx * 2f - 2f), 1f);
                tick.color = TickDim;
            }
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
            fillRt.offsetMin = new Vector2(BorderPx + 1f, BorderPx + 1f);
            fillRt.offsetMax = new Vector2(-(BorderPx + 1f), -(BorderPx + 1f));
            gauge.Fill.enabled = fraction > 0.001f;

            if (fuelStyle)
            {
                Color c = fraction <= FuelWarnFraction ? FuelLow : FlirGreen;
                gauge.Fill.color = c;
                gauge.Value.color = c;
            }
            else
            {
                gauge.Fill.color = FlirGreen;
                gauge.Value.color = FlirGreen;
            }
        }

        private static Gauge CreateGauge(RectTransform parent, string name, string labelText)
        {
            var rootGo = new GameObject(name, typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            RectTransform root = rootGo.GetComponent<RectTransform>();

            Image frame = CreateImage(root, "Frame", FlirGreen);
            Stretch(frame.rectTransform);

            Image hole = CreateImage(root, "Hole", FrameHole);
            RectTransform holeRt = hole.rectTransform;
            holeRt.anchorMin = Vector2.zero;
            holeRt.anchorMax = Vector2.one;
            holeRt.offsetMin = new Vector2(BorderPx, BorderPx);
            holeRt.offsetMax = new Vector2(-BorderPx, -BorderPx);

            var ticks = new Image[TickCount];
            for (int i = 0; i < TickCount; i++)
                ticks[i] = CreateImage(root, "Tick" + i, TickDim);

            Image fill = CreateImage(root, "Fill", FlirGreen);
            RectTransform fillRt = fill.rectTransform;
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(1f, 0f);
            fillRt.pivot = new Vector2(0.5f, 0f);
            fillRt.offsetMin = new Vector2(BorderPx + 1f, BorderPx + 1f);
            fillRt.offsetMax = new Vector2(-(BorderPx + 1f), 0f);

            Text label = CreateLabel(root, "Label", labelText);
            Text value = CreateLabel(root, "Value", "0%");
            return new Gauge(root, fill, label, value, ticks);
        }

        private static Image CreateImage(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            UiImageHelper.ApplySolid(img, color);
            return img;
        }

        private static Text CreateLabel(RectTransform parent, string name, string text)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text label = go.GetComponent<Text>();
            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = FlirGreen;
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
            internal Gauge(RectTransform root, Image fill, Text label, Text value, Image[] ticks)
            {
                Root = root;
                Fill = fill;
                Label = label;
                Value = value;
                Ticks = ticks;
            }

            internal RectTransform Root { get; }
            internal Image Fill { get; }
            internal Text Label { get; }
            internal Text Value { get; }
            internal Image[] Ticks { get; }
        }
    }
}
