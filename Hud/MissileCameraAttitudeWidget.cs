using UnityEngine;

namespace MissileCamera
{
    internal sealed class MissileCameraAttitudeWidget
    {
        private readonly RectTransform _root;
        private readonly HudLineGraphic _horizonLineLeft;
        private readonly HudLineGraphic _horizonLineRight;
        private readonly HudRingGraphic _reticleRing;
        private readonly HudLineGraphic[] _reticleTicks;
        private readonly HudLineGraphic _crosshairH;
        private readonly HudLineGraphic _crosshairV;
        private float _lastRollDeg = float.NaN;
        private float _lastWidgetSize = -1f;

        private MissileCameraAttitudeWidget(
            RectTransform root,
            HudLineGraphic horizonLineLeft,
            HudLineGraphic horizonLineRight,
            HudRingGraphic reticleRing,
            HudLineGraphic[] reticleTicks,
            HudLineGraphic crosshairH,
            HudLineGraphic crosshairV)
        {
            _root = root;
            _horizonLineLeft = horizonLineLeft;
            _horizonLineRight = horizonLineRight;
            _reticleRing = reticleRing;
            _reticleTicks = reticleTicks;
            _crosshairH = crosshairH;
            _crosshairV = crosshairV;
        }

        internal static MissileCameraAttitudeWidget Create(RectTransform parent)
        {
            var rootGo = new GameObject("MissileCameraHudAttitude", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            RectTransform root = rootGo.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(120f, 120f);

            HudLineGraphic horizonLineLeft = CreateLine(root, "HorizonLineLeft");
            HudLineGraphic horizonLineRight = CreateLine(root, "HorizonLineRight");
            HudRingGraphic reticleRing = CreateRing(root, "ReticleRing");
            HudLineGraphic crosshairH = CreateLine(root, "CrosshairH");
            HudLineGraphic crosshairV = CreateLine(root, "CrosshairV");

            var ticks = new HudLineGraphic[4];
            for (int i = 0; i < ticks.Length; i++)
                ticks[i] = CreateLine(root, $"ReticleTick{i}");

            return new MissileCameraAttitudeWidget(
                root, horizonLineLeft, horizonLineRight, reticleRing, ticks, crosshairH, crosshairV);
        }

        internal void Update(MissileCameraHudSnapshot snapshot, float panelMinSide)
        {
            float widgetSize = Mathf.Clamp(panelMinSide * 0.32f, 48f, 140f);
            if (Mathf.Approximately(snapshot.RollDeg, _lastRollDeg)
                && Mathf.Approximately(widgetSize, _lastWidgetSize))
            {
                return;
            }

            _lastRollDeg = snapshot.RollDeg;
            _lastWidgetSize = widgetSize;

            _root.sizeDelta = new Vector2(widgetSize, widgetSize);

            _root.anchoredPosition = Vector2.zero;

            float wingLength = widgetSize * 0.76f;
            float lineThickness = Mathf.Max(3f, widgetSize * 0.065f - 2f);
            float outlineThickness = Mathf.Max(1.5f, lineThickness * 0.22f);

            float bankRad = snapshot.RollDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(bankRad);
            float sin = Mathf.Sin(bankRad);

            float reticleRadius = widgetSize * 0.18f;
            float lineGapHalf = (reticleRadius + widgetSize * 0.1f) * 2f;

            Vector2 leftFar = RotatePoint(new Vector2(-(lineGapHalf + wingLength), 0f), cos, sin);
            Vector2 leftNear = RotatePoint(new Vector2(-lineGapHalf, 0f), cos, sin);
            Vector2 rightNear = RotatePoint(new Vector2(lineGapHalf, 0f), cos, sin);
            Vector2 rightFar = RotatePoint(new Vector2(lineGapHalf + wingLength, 0f), cos, sin);

            Color outline = MissileCameraHudConfig.HorizonOutlineColor;
            Color fill = MissileCameraHudConfig.DeriveHorizonFillColor(outline);
            _horizonLineLeft.SetHorizonBar(leftFar, leftNear, lineThickness, fill, outline, outlineThickness);
            _horizonLineRight.SetHorizonBar(rightNear, rightFar, lineThickness, fill, outline, outlineThickness);

            float reticleThickness = Mathf.Max(2.4f, widgetSize * 0.04f);
            _reticleRing.SetRing(reticleRadius, reticleThickness, MissileCameraHudConfig.ReticleColor);

            float tickLen = widgetSize * 0.05f;
            float tickDist = reticleRadius + tickLen * 0.35f;
            for (int i = 0; i < _reticleTicks.Length; i++)
            {
                float angle = (45f + i * 90f) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 start = dir * tickDist;
                Vector2 end = dir * (tickDist + tickLen);
                _reticleTicks[i].SetLine(start, end, reticleThickness, MissileCameraHudConfig.ReticleColor);
            }

            float crossHalf = reticleRadius * 0.55f;
            float crossThickness = Mathf.Max(1.6f, reticleThickness * 0.7f);
            Color crossColor = MissileCameraHudConfig.ReticleColor;
            _crosshairH.SetLine(new Vector2(-crossHalf, 0f), new Vector2(crossHalf, 0f), crossThickness, crossColor);
            _crosshairV.SetLine(new Vector2(0f, -crossHalf), new Vector2(0f, crossHalf), crossThickness, crossColor);
        }

        internal void SetVisible(bool visible)
        {
            if (visible)
            {
                _lastRollDeg = float.NaN;
                _lastWidgetSize = -1f;
            }

            _root.gameObject.SetActive(visible);
        }

        private static Vector2 RotatePoint(Vector2 point, float cos, float sin) =>
            new Vector2(point.x * cos - point.y * sin, point.x * sin + point.y * cos);

        private static HudLineGraphic CreateLine(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HudLineGraphic));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            return go.GetComponent<HudLineGraphic>();
        }

        private static HudRingGraphic CreateRing(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HudRingGraphic));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            return go.GetComponent<HudRingGraphic>();
        }
    }
}
