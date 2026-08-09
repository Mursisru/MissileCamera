using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// Right-edge scale: target range (m) with arrow pointer.
    /// When zoomed, a second caret tracks magnification (1× top … max bottom).
    /// </summary>
    internal sealed class GunshipRangeScale
    {
        private readonly RectTransform _root;
        private readonly Image _rail;
        private readonly Image _ptrRange;
        private readonly Image _ptrZoom;
        private readonly Text _top;
        private readonly Text _mid;
        private readonly Text _bot;
        private readonly Text _value;
        private readonly Text _zoomLbl;
        private readonly Image[] _ticks;
        private float _layoutH = -1f;
        private float _lastRange = -1f;
        private float _lastMag = -1f;
        private float _maxRange = 500f;

        private GunshipRangeScale(
            RectTransform root,
            Image rail,
            Image ptrRange,
            Image ptrZoom,
            Text top,
            Text mid,
            Text bot,
            Text value,
            Text zoomLbl,
            Image[] ticks)
        {
            _root = root;
            _rail = rail;
            _ptrRange = ptrRange;
            _ptrZoom = ptrZoom;
            _top = top;
            _mid = mid;
            _bot = bot;
            _value = value;
            _zoomLbl = zoomLbl;
            _ticks = ticks;
        }

        internal static GunshipRangeScale Create(RectTransform parent)
        {
            var rootGo = new GameObject("GunshipRangeScale", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            RectTransform root = rootGo.GetComponent<RectTransform>();

            Image rail = GunshipChrome.CreateImage(root, "Rail", GunshipChrome.White);
            Image ptrRange = GunshipChrome.CreateImage(root, "PtrRange", GunshipChrome.White);
            Image ptrZoom = GunshipChrome.CreateImage(root, "PtrZoom", GunshipChrome.WhiteDim);
            Text top = GunshipChrome.CreateText(root, "Top", TextAnchor.MiddleLeft, GunshipChrome.FontSmall);
            Text mid = GunshipChrome.CreateText(root, "Mid", TextAnchor.MiddleLeft, GunshipChrome.FontSmall);
            Text bot = GunshipChrome.CreateText(root, "Bot", TextAnchor.MiddleLeft, GunshipChrome.FontSmall);
            Text value = GunshipChrome.CreateText(root, "Val", TextAnchor.MiddleRight, GunshipChrome.FontBody);
            Text zoomLbl = GunshipChrome.CreateText(root, "ZoomVal", TextAnchor.MiddleLeft, GunshipChrome.FontSmall);
            zoomLbl.color = GunshipChrome.WhiteDim;
            top.color = GunshipChrome.WhiteDim;
            mid.color = GunshipChrome.WhiteDim;
            bot.color = GunshipChrome.WhiteDim;

            var ticks = new Image[5];
            for (int i = 0; i < ticks.Length; i++)
                ticks[i] = GunshipChrome.CreateImage(root, "Tick" + i, GunshipChrome.WhiteSoft);

            return new GunshipRangeScale(root, rail, ptrRange, ptrZoom, top, mid, bot, value, zoomLbl, ticks);
        }

        internal void Place(MissileCameraPanelMetrics panel)
        {
            float h = Mathf.Clamp(panel.Height * 0.45f, 200f, 340f);
            float inset = Mathf.Max(panel.HorizontalInset, 14f) + 6f;
            _layoutH = h;

            GunshipChrome.Place(_root, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-inset - 8f, 20f), new Vector2(110f, h));

            RectTransform railRt = _rail.rectTransform;
            railRt.anchorMin = new Vector2(0.62f, 0.02f);
            railRt.anchorMax = new Vector2(0.62f, 0.98f);
            railRt.pivot = new Vector2(0.5f, 0.5f);
            railRt.anchoredPosition = Vector2.zero;
            railRt.sizeDelta = new Vector2(1.4f, 0f);

            for (int i = 0; i < _ticks.Length; i++)
            {
                float yN = i / (float)(_ticks.Length - 1);
                RectTransform tr = _ticks[i].rectTransform;
                tr.anchorMin = new Vector2(0.62f, yN * 0.96f + 0.02f);
                tr.anchorMax = new Vector2(0.62f, yN * 0.96f + 0.02f);
                tr.pivot = new Vector2(0f, 0.5f);
                tr.anchoredPosition = Vector2.zero;
                tr.sizeDelta = new Vector2(i % 2 == 0 ? 8f : 5f, 1.2f);
            }

            PlaceLabel(_top, 0.98f, "0 m");
            PlaceLabel(_mid, 0.5f, "");
            PlaceLabel(_bot, 0.02f, "");

            // Triangle-ish caret via wide short bar pointing left at rail
            SetupPtr(_ptrRange.rectTransform, towardLeft: true, size: new Vector2(11f, 2.2f));
            SetupPtr(_ptrZoom.rectTransform, towardLeft: false, size: new Vector2(8f, 1.6f));

            RectTransform valRt = _value.rectTransform;
            valRt.anchorMin = new Vector2(0f, 0.5f);
            valRt.anchorMax = new Vector2(0.55f, 0.5f);
            valRt.pivot = new Vector2(1f, 0.5f);
            valRt.sizeDelta = new Vector2(52f, 16f);

            RectTransform zRt = _zoomLbl.rectTransform;
            zRt.anchorMin = new Vector2(0.68f, 0.5f);
            zRt.anchorMax = new Vector2(1f, 0.5f);
            zRt.pivot = new Vector2(0f, 0.5f);
            zRt.sizeDelta = new Vector2(40f, 14f);
        }

        internal void Update(MissileCameraHudSnapshot snapshot)
        {
            float range = snapshot.HasTarget || snapshot.HasAimPoint
                ? Mathf.Max(0f, snapshot.TargetRangeMeters)
                : EstimateFovGroundMeters(snapshot);

            if (range > _maxRange * 0.92f)
                _maxRange = Mathf.Ceil(range / 100f) * 100f;
            else if (range < _maxRange * 0.35f && _maxRange > 500f)
                _maxRange = Mathf.Max(500f, Mathf.Ceil(range / 50f) * 50f);

            float max = Mathf.Max(100f, _maxRange);
            _top.text = "0 m";
            _mid.text = Mathf.RoundToInt(max * 0.5f).ToString(CultureInfo.InvariantCulture) + " m";
            _bot.text = Mathf.RoundToInt(max).ToString(CultureInfo.InvariantCulture) + " m";

            float railH = _layoutH * 0.96f;
            if (Mathf.Abs(range - _lastRange) >= 0.35f || _lastRange < 0f)
            {
                _lastRange = range;
                float t = max > 0.01f ? Mathf.Clamp01(range / max) : 0f;
                float y = Mathf.Lerp(railH * 0.5f, -railH * 0.5f, t);
                _ptrRange.rectTransform.anchoredPosition = new Vector2(0f, y);
                _value.rectTransform.anchoredPosition = new Vector2(-2f, y);
                _value.text = Mathf.RoundToInt(range).ToString(CultureInfo.InvariantCulture) + " m";
            }

            float mag = MissileCameraFeedController.FullscreenMagnification;
            float magMax = Mathf.Max(2f, MissileCameraFullscreenConfig.ZoomMax);
            bool zoomed = mag > 1.02f;
            _ptrZoom.enabled = zoomed;
            _zoomLbl.enabled = zoomed;
            if (zoomed && (Mathf.Abs(mag - _lastMag) > 0.02f || _lastMag < 0f))
            {
                _lastMag = mag;
                float zt = Mathf.Clamp01((mag - 1f) / (magMax - 1f));
                float zy = Mathf.Lerp(railH * 0.5f, -railH * 0.5f, zt);
                _ptrZoom.rectTransform.anchoredPosition = new Vector2(0f, zy);
                _zoomLbl.rectTransform.anchoredPosition = new Vector2(4f, zy);
                _zoomLbl.text = mag.ToString("0.0", CultureInfo.InvariantCulture) + "×";
            }
        }

        private static float EstimateFovGroundMeters(MissileCameraHudSnapshot snapshot)
        {
            float fov = Mathf.Max(1f, snapshot.FeedFovDeg);
            float alt = 200f;
            try
            {
                Missile? m = MissileCameraFeedController.TryGetFollowedMissile();
                if (m != null)
                    alt = Mathf.Max(20f, m.transform.GlobalPosition().y);
            }
            catch { /* ignore */ }

            // Half-width of view on ground ≈ alt * tan(fov/2)
            return 2f * alt * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
        }

        private static void SetupPtr(RectTransform rt, bool towardLeft, Vector2 size)
        {
            rt.anchorMin = new Vector2(0.62f, 0.5f);
            rt.anchorMax = new Vector2(0.62f, 0.5f);
            rt.pivot = towardLeft ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
            rt.sizeDelta = size;
        }

        private static void PlaceLabel(Text label, float yNorm, string text)
        {
            RectTransform rt = label.rectTransform;
            rt.anchorMin = new Vector2(0.68f, yNorm);
            rt.anchorMax = new Vector2(0.68f, yNorm);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(6f, 0f);
            rt.sizeDelta = new Vector2(56f, 14f);
            if (!string.IsNullOrEmpty(text))
                label.text = text;
        }
    }
}
