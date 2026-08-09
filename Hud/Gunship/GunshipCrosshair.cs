using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// Reference gunship reticle: full vertical bar, gapped horizontal with tick marks,
    /// tiny center +, N/E/S/W rotating with heading.
    /// </summary>
    internal sealed class GunshipCrosshair
    {
        private readonly RectTransform _compassRoot;
        private readonly Text _area;
        private readonly Text _mag;
        private readonly Image _vBar;
        private readonly Image _hLeft;
        private readonly Image _hRight;
        private readonly Image _cH;
        private readonly Image _cV;
        private readonly Image[] _hTicks;
        private readonly Text[] _cardinals;
        private string _lastArea = "";
        private string _lastMag = "";
        private float _lastHdg = float.NaN;

        private GunshipCrosshair(
            RectTransform compassRoot,
            Text area,
            Text mag,
            Image vBar,
            Image hLeft,
            Image hRight,
            Image cH,
            Image cV,
            Image[] hTicks,
            Text[] cardinals)
        {
            _compassRoot = compassRoot;
            _area = area;
            _mag = mag;
            _vBar = vBar;
            _hLeft = hLeft;
            _hRight = hRight;
            _cH = cH;
            _cV = cV;
            _hTicks = hTicks;
            _cardinals = cardinals;
        }

        internal static GunshipCrosshair Create(RectTransform parent)
        {
            var rootGo = new GameObject("GunshipCrosshair", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            RectTransform root = rootGo.GetComponent<RectTransform>();
            GunshipChrome.Stretch(root);

            Image vBar = MakeLine(root, "V");
            Image hLeft = MakeLine(root, "HL");
            Image hRight = MakeLine(root, "HR");
            Image cH = MakeLine(root, "CH");
            Image cV = MakeLine(root, "CV");

            var ticks = new Image[10];
            for (int i = 0; i < ticks.Length; i++)
                ticks[i] = MakeLine(root, "T" + i);

            var compassGo = new GameObject("GunshipCompass", typeof(RectTransform));
            compassGo.transform.SetParent(root, false);
            RectTransform compassRoot = compassGo.GetComponent<RectTransform>();
            compassRoot.anchorMin = compassRoot.anchorMax = new Vector2(0.5f, 0.5f);
            compassRoot.pivot = new Vector2(0.5f, 0.5f);
            compassRoot.sizeDelta = Vector2.zero;

            var cardinals = new Text[4];
            string[] letters = { "N", "E", "S", "W" };
            for (int i = 0; i < 4; i++)
            {
                cardinals[i] = GunshipChrome.CreateText(compassRoot, "Card" + letters[i], TextAnchor.MiddleCenter, GunshipChrome.FontSmall);
                cardinals[i].text = letters[i];
                cardinals[i].fontStyle = FontStyle.Bold;
                cardinals[i].color = i == 0 ? GunshipChrome.White : GunshipChrome.WhiteDim;
            }

            Text area = GunshipChrome.CreateText(root, "GunshipArea", TextAnchor.UpperCenter, GunshipChrome.FontBody);
            Text mag = GunshipChrome.CreateText(root, "GunshipMag", TextAnchor.UpperCenter, GunshipChrome.FontSmall);
            mag.color = GunshipChrome.WhiteDim;
            return new GunshipCrosshair(compassRoot, area, mag, vBar, hLeft, hRight, cH, cV, ticks, cardinals);
        }

        internal void Place(MissileCameraPanelMetrics panel)
        {
            float min = panel.MinSide;
            float halfH = Mathf.Clamp(min * 0.42f, 220f, 420f);
            float halfArm = Mathf.Clamp(min * 0.28f, 160f, 320f);
            float gap = Mathf.Clamp(min * 0.018f, 14f, 22f);
            float t = 1.6f;

            // Full vertical through center (reference style)
            _vBar.rectTransform.anchoredPosition = Vector2.zero;
            _vBar.rectTransform.sizeDelta = new Vector2(t, halfH * 2f);

            // Horizontal arms with center gap
            _hLeft.rectTransform.anchoredPosition = new Vector2(-(gap + halfArm * 0.5f), 0f);
            _hLeft.rectTransform.sizeDelta = new Vector2(halfArm, t);
            _hRight.rectTransform.anchoredPosition = new Vector2(gap + halfArm * 0.5f, 0f);
            _hRight.rectTransform.sizeDelta = new Vector2(halfArm, t);

            // Tiny center +
            _cH.rectTransform.anchoredPosition = Vector2.zero;
            _cH.rectTransform.sizeDelta = new Vector2(10f, t);
            _cV.rectTransform.anchoredPosition = Vector2.zero;
            _cV.rectTransform.sizeDelta = new Vector2(t, 10f);

            // Tick marks along horizontal arms
            int n = _hTicks.Length / 2;
            for (int i = 0; i < n; i++)
            {
                float u = (i + 1) / (float)(n + 1);
                float xL = -(gap + halfArm * u);
                float xR = gap + halfArm * u;
                float tickH = i % 2 == 0 ? 10f : 6f;
                PlaceTick(_hTicks[i], xL, tickH, t);
                PlaceTick(_hTicks[i + n], xR, tickH, t);
            }

            float cardR = Mathf.Clamp(min * 0.12f, 70f, 110f);
            PlaceCard(_cardinals[0], 0f, cardR);
            PlaceCard(_cardinals[1], cardR, 0f);
            PlaceCard(_cardinals[2], 0f, -cardR);
            PlaceCard(_cardinals[3], -cardR, 0f);

            GunshipChrome.Place(_area.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f),
                new Vector2(0f, -halfH * 0.22f - 36f), new Vector2(720f, 28f));
            GunshipChrome.Place(_mag.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f),
                new Vector2(0f, -halfH * 0.22f - 62f), new Vector2(240f, 22f));
        }

        internal void Update(MissileCameraHudSnapshot snapshot)
        {
            string area;
            if (snapshot.HasTarget)
            {
                string grid = string.IsNullOrEmpty(snapshot.TargetGridText)
                    ? (string.IsNullOrEmpty(snapshot.TargetName) ? "TARGET" : snapshot.TargetName.ToUpperInvariant())
                    : snapshot.TargetGridText;
                area = "TARGETING AREA: " + grid;
            }
            else if (snapshot.HasAimPoint)
                area = "TARGETING AREA: AIM";
            else
                area = "TARGETING AREA: ---";

            if (area != _lastArea)
            {
                _lastArea = area;
                _area.text = area;
            }

            float mag = MissileCameraFeedController.FullscreenMagnification;
            string magText = mag <= 1.01f
                ? string.Empty
                : "MAG  " + mag.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "×";
            if (magText != _lastMag)
            {
                _lastMag = magText;
                _mag.text = magText;
            }

            float hdg = snapshot.MissileHeadingDeg;
            if (float.IsNaN(_lastHdg) || Mathf.Abs(Mathf.DeltaAngle(_lastHdg, hdg)) > 0.15f)
            {
                _lastHdg = hdg;
                _compassRoot.localEulerAngles = new Vector3(0f, 0f, hdg);
                for (int i = 0; i < _cardinals.Length; i++)
                    _cardinals[i].rectTransform.localEulerAngles = new Vector3(0f, 0f, -hdg);
            }
        }

        private static Image MakeLine(RectTransform parent, string name)
        {
            Image img = GunshipChrome.CreateImage(parent, name, GunshipChrome.White);
            RectTransform rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            return img;
        }

        private static void PlaceTick(Image img, float x, float h, float w)
        {
            RectTransform rt = img.rectTransform;
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static void PlaceCard(Text label, float x, float y)
        {
            RectTransform rt = label.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(28f, 20f);
        }
    }
}
