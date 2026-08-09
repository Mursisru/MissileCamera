using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// COD right range tape: 0 / 250 / 500 m, left caret + live value at caret.
    /// Zoom = secondary dim caret when mag &gt; 1.
    /// </summary>
    internal sealed class GunshipRangeScale
    {
        private const float MaxRangeM = 500f;

        private readonly RectTransform _root;
        private readonly Image _rail;
        private readonly Image _caret;
        private readonly Image _zoomCaret;
        private readonly Text _top;
        private readonly Text _mid;
        private readonly Text _bot;
        private readonly Text _value;
        private readonly Image[] _ticks;
        private float _layoutH = -1f;
        private float _lastRange = -1f;

        private GunshipRangeScale(
            RectTransform root, Image rail, Image caret, Image zoomCaret,
            Text top, Text mid, Text bot, Text value, Image[] ticks)
        {
            _root = root;
            _rail = rail;
            _caret = caret;
            _zoomCaret = zoomCaret;
            _top = top;
            _mid = mid;
            _bot = bot;
            _value = value;
            _ticks = ticks;
        }

        internal static GunshipRangeScale Create(RectTransform parent)
        {
            var rootGo = new GameObject("GunshipRangeScale", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            RectTransform root = rootGo.GetComponent<RectTransform>();

            Image rail = GunshipChrome.CreateImage(root, "Rail", GunshipChrome.White);
            Image caret = GunshipChrome.CreateImage(root, "Caret", GunshipChrome.White);
            Image zoomCaret = GunshipChrome.CreateImage(root, "ZCaret", GunshipChrome.WhiteDim);
            Text top = GunshipChrome.CreateText(root, "Top", TextAnchor.MiddleLeft, GunshipChrome.FontSmall);
            Text mid = GunshipChrome.CreateText(root, "Mid", TextAnchor.MiddleLeft, GunshipChrome.FontSmall);
            Text bot = GunshipChrome.CreateText(root, "Bot", TextAnchor.MiddleLeft, GunshipChrome.FontSmall);
            Text value = GunshipChrome.CreateText(root, "Val", TextAnchor.MiddleRight, GunshipChrome.FontBody);
            top.fontStyle = mid.fontStyle = bot.fontStyle = FontStyle.Normal;
            top.color = mid.color = bot.color = GunshipChrome.WhiteDim;
            top.text = "0 m";
            mid.text = "250 m";
            bot.text = "500 m";

            var ticks = new Image[9];
            for (int i = 0; i < ticks.Length; i++)
                ticks[i] = GunshipChrome.CreateImage(root, "Tk" + i, GunshipChrome.WhiteSoft);

            return new GunshipRangeScale(root, rail, caret, zoomCaret, top, mid, bot, value, ticks);
        }

        internal void Place(MissileCameraPanelMetrics panel)
        {
            float h = Mathf.Clamp(panel.Height * 0.46f, 240f, 360f);
            float inset = GunshipChrome.PadX(panel) * 0.7f;
            _layoutH = h;

            GunshipChrome.Place(_root, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-inset - 4f, 8f), new Vector2(140f, h));

            // Rail near left of local root so labels sit to the right (COD)
            RectTransform railRt = _rail.rectTransform;
            railRt.anchorMin = new Vector2(0.55f, 0.04f);
            railRt.anchorMax = new Vector2(0.55f, 0.96f);
            railRt.pivot = new Vector2(0.5f, 0.5f);
            railRt.anchoredPosition = Vector2.zero;
            railRt.sizeDelta = new Vector2(1.5f, 0f);

            for (int i = 0; i < _ticks.Length; i++)
            {
                float yn = i / (float)(_ticks.Length - 1);
                RectTransform tr = _ticks[i].rectTransform;
                tr.anchorMin = tr.anchorMax = new Vector2(0.55f, yn * 0.92f + 0.04f);
                tr.pivot = new Vector2(0f, 0.5f);
                tr.anchoredPosition = Vector2.zero;
                bool major = i % 2 == 0;
                tr.sizeDelta = new Vector2(major ? 10f : 6f, 1.2f);
                _ticks[i].color = major ? GunshipChrome.WhiteDim : GunshipChrome.WhiteSoft;
            }

            Label(_top, 0.96f);
            Label(_mid, 0.5f);
            Label(_bot, 0.04f);

            RectTransform cRt = _caret.rectTransform;
            cRt.anchorMin = cRt.anchorMax = new Vector2(0.55f, 0.5f);
            cRt.pivot = new Vector2(1f, 0.5f);
            cRt.sizeDelta = new Vector2(16f, 12f);
            BuildChevron(_caret);

            RectTransform zRt = _zoomCaret.rectTransform;
            zRt.anchorMin = zRt.anchorMax = new Vector2(0.55f, 0.5f);
            zRt.pivot = new Vector2(0f, 0.5f);
            zRt.sizeDelta = new Vector2(10f, 2f);
            _zoomCaret.enabled = false;

            RectTransform vRt = _value.rectTransform;
            vRt.anchorMin = vRt.anchorMax = new Vector2(0.48f, 0.5f);
            vRt.pivot = new Vector2(1f, 0.5f);
            vRt.sizeDelta = new Vector2(72f, 20f);
        }

        internal void Update(MissileCameraHudSnapshot snapshot)
        {
            float range = ResolveRange(snapshot);
            range = Mathf.Clamp(range, 0f, MaxRangeM * 1.05f);

            float railH = _layoutH * 0.92f;
            if (Mathf.Abs(range - _lastRange) >= 0.25f || _lastRange < 0f)
            {
                _lastRange = range;
                float t = Mathf.Clamp01(range / MaxRangeM);
                // COD: 0 top → 500 bottom
                float y = Mathf.Lerp(railH * 0.5f, -railH * 0.5f, t);
                _caret.rectTransform.anchoredPosition = new Vector2(0f, y);
                _value.rectTransform.anchoredPosition = new Vector2(-6f, y);
                _value.text = Mathf.RoundToInt(Mathf.Min(range, MaxRangeM)).ToString(CultureInfo.InvariantCulture) + " m";
            }

            float mag = MissileCameraFeedController.FullscreenMagnification;
            bool zoomed = mag > 1.02f;
            _zoomCaret.enabled = zoomed;
            if (zoomed)
            {
                float magMax = Mathf.Max(2f, MissileCameraFullscreenConfig.ZoomMax);
                float zt = Mathf.Clamp01((mag - 1f) / (magMax - 1f));
                float zy = Mathf.Lerp(railH * 0.5f, -railH * 0.5f, zt);
                _zoomCaret.rectTransform.anchoredPosition = new Vector2(8f, zy);
            }
        }

        private static float ResolveRange(MissileCameraHudSnapshot snapshot)
        {
            if (snapshot.HasTarget || snapshot.HasAimPoint)
                return Mathf.Max(0f, snapshot.TargetRangeMeters);

            float fov = Mathf.Max(1f, snapshot.FeedFovDeg);
            float alt = 200f;
            try
            {
                Missile? m = MissileCameraFeedController.TryGetFollowedMissile();
                if (m != null) alt = Mathf.Max(20f, m.transform.GlobalPosition().y);
            }
            catch { /* ignore */ }
            return Mathf.Min(MaxRangeM, 2f * alt * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad));
        }

        private static void Label(Text label, float yNorm)
        {
            RectTransform rt = label.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.62f, yNorm);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(6f, 0f);
            rt.sizeDelta = new Vector2(64f, 18f);
        }

        private static void BuildChevron(Image host)
        {
            host.color = new Color(1f, 1f, 1f, 0f);
            if (host.transform.Find("A") != null) return;
            Image a = GunshipChrome.CreateImage(host.rectTransform, "A", GunshipChrome.White);
            Image b = GunshipChrome.CreateImage(host.rectTransform, "B", GunshipChrome.White);
            RectTransform ar = a.rectTransform;
            RectTransform br = b.rectTransform;
            ar.anchorMin = ar.anchorMax = br.anchorMin = br.anchorMax = new Vector2(0.5f, 0.5f);
            ar.pivot = br.pivot = new Vector2(0.5f, 0.5f);
            ar.sizeDelta = br.sizeDelta = new Vector2(11f, 1.6f);
            ar.localEulerAngles = new Vector3(0f, 0f, 38f);
            br.localEulerAngles = new Vector3(0f, 0f, -38f);
            ar.anchoredPosition = new Vector2(-1f, 2.8f);
            br.anchoredPosition = new Vector2(-1f, -2.8f);
        }
    }
}
