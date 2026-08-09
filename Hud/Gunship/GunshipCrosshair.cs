using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// COD AC-130 reticle (~15% under previous span):
    /// gapped arms + T-caps + ticks; 4 axis dots at inner tips (not corner square).
    /// N/E/S/W orbit OUTSIDE the cross (on map), rotate with HDG.
    /// </summary>
    internal sealed class GunshipCrosshair
    {
        private const int TicksPerArm = 2;
        private static readonly string[] Labels = { "N", "E", "S", "W" };
        private static readonly float[] WorldBearing = { 0f, 90f, 180f, 270f };

        private readonly Text _area;
        private readonly Text[] _nesw;
        private readonly Image _armN;
        private readonly Image _armS;
        private readonly Image _armE;
        private readonly Image _armW;
        private readonly Image _capN;
        private readonly Image _capS;
        private readonly Image _capE;
        private readonly Image _capW;
        private readonly Image[] _ticks;
        // COD: one dot at inner tip of each arm (axis-aligned)
        private readonly Image _dotN;
        private readonly Image _dotS;
        private readonly Image _dotE;
        private readonly Image _dotW;
        private float _cardinalR = 120f;
        private string _lastArea = "";
        private float _lastHdg = float.NaN;

        private GunshipCrosshair(
            Text area, Text[] nesw,
            Image armN, Image armS, Image armE, Image armW,
            Image capN, Image capS, Image capE, Image capW,
            Image[] ticks,
            Image dotN, Image dotS, Image dotE, Image dotW)
        {
            _area = area;
            _nesw = nesw;
            _armN = armN;
            _armS = armS;
            _armE = armE;
            _armW = armW;
            _capN = capN;
            _capS = capS;
            _capE = capE;
            _capW = capW;
            _ticks = ticks;
            _dotN = dotN;
            _dotS = dotS;
            _dotE = dotE;
            _dotW = dotW;
        }

        internal static GunshipCrosshair Create(RectTransform parent)
        {
            var rootGo = new GameObject("GunshipCrosshair", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            RectTransform root = rootGo.GetComponent<RectTransform>();
            GunshipChrome.Stretch(root);

            Image armN = Line(root, "ArmN");
            Image armS = Line(root, "ArmS");
            Image armE = Line(root, "ArmE");
            Image armW = Line(root, "ArmW");
            Image capN = Line(root, "CapN");
            Image capS = Line(root, "CapS");
            Image capE = Line(root, "CapE");
            Image capW = Line(root, "CapW");

            var ticks = new Image[TicksPerArm * 4];
            for (int i = 0; i < ticks.Length; i++)
                ticks[i] = Line(root, "Tk" + i);

            Image dotN = Line(root, "DotN");
            Image dotS = Line(root, "DotS");
            Image dotE = Line(root, "DotE");
            Image dotW = Line(root, "DotW");

            var nesw = new Text[4];
            for (int i = 0; i < 4; i++)
            {
                nesw[i] = GunshipChrome.CreateText(root, "Card" + Labels[i], TextAnchor.MiddleCenter, 26);
                nesw[i].text = Labels[i];
                nesw[i].fontStyle = FontStyle.Bold;
                nesw[i].color = GunshipChrome.White;
                GunshipChrome.Place(nesw[i].rectTransform,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(34f, 34f));
            }

            Text area = GunshipChrome.CreateText(root, "Area", TextAnchor.UpperCenter, GunshipChrome.FontBody);
            area.fontStyle = FontStyle.Normal;

            return new GunshipCrosshair(
                area, nesw,
                armN, armS, armE, armW,
                capN, capS, capE, capW,
                ticks, dotN, dotS, dotE, dotW);
        }

        internal void Place(MissileCameraPanelMetrics panel)
        {
            float min = panel.MinSide;
            float thick = 1.15f;
            // Previous 0.275 → −15% ≈ 0.234 of min
            float halfSpan = Mathf.Clamp(min * 0.234f, 120f, 220f);
            // COD: clear center gap; dots sit at inner tips
            float gap = Mathf.Clamp(min * 0.028f, 14f, 24f);
            float arm = halfSpan - gap;
            float capLen = Mathf.Clamp(min * 0.016f, 8f, 13f);
            float tickLen = Mathf.Clamp(min * 0.011f, 5.5f, 9f);
            float dot = Mathf.Clamp(min * 0.0055f, 2.6f, 4.2f);

            float armMid = gap + arm * 0.5f;
            float tip = gap + arm;
            Size(_armN, 0f, armMid, thick, arm);
            Size(_armS, 0f, -armMid, thick, arm);
            Size(_armE, armMid, 0f, arm, thick);
            Size(_armW, -armMid, 0f, arm, thick);

            Size(_capN, 0f, tip, capLen, thick);
            Size(_capS, 0f, -tip, capLen, thick);
            Size(_capE, tip, 0f, thick, capLen);
            Size(_capW, -tip, 0f, thick, capLen);

            for (int i = 0; i < TicksPerArm; i++)
            {
                float u = (i + 1) / (float)(TicksPerArm + 1);
                float d = gap + arm * u;
                int b = i * 4;
                Size(_ticks[b], 0f, d, tickLen, thick);
                Size(_ticks[b + 1], 0f, -d, tickLen, thick);
                Size(_ticks[b + 2], d, 0f, thick, tickLen);
                Size(_ticks[b + 3], -d, 0f, thick, tickLen);
            }

            // 4 axis dots at inner tips of arms (COD — not diagonal corners)
            float dr = gap * 0.72f;
            Size(_dotN, 0f, dr, dot, dot);
            Size(_dotS, 0f, -dr, dot, dot);
            Size(_dotE, dr, 0f, dot, dot);
            Size(_dotW, -dr, 0f, dot, dot);

            // Outside cross onto map — beyond T-caps
            _cardinalR = tip + Mathf.Clamp(min * 0.055f, 28f, 52f);
            _lastHdg = float.NaN;

            float areaY = -(_cardinalR + Mathf.Clamp(min * 0.03f, 16f, 30f));
            GunshipChrome.Place(_area.rectTransform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f),
                new Vector2(0f, areaY), new Vector2(720f, 24f));
        }

        internal void Update(MissileCameraHudSnapshot snapshot)
        {
            string area;
            if (snapshot.HasTarget)
            {
                string grid = !string.IsNullOrEmpty(snapshot.TargetGridText)
                    ? snapshot.TargetGridText
                    : (!string.IsNullOrEmpty(snapshot.TargetName) ? snapshot.TargetName.ToUpperInvariant() : "TARGET");
                area = "TARGETING AREA: " + grid;
            }
            else if (snapshot.HasAimPoint)
                area = "TARGETING AREA: AIM";
            else
                area = "TARGETING AREA: ---";

            if (area != _lastArea) { _lastArea = area; _area.text = area; }

            UpdateCardinals(snapshot);
        }

        private void UpdateCardinals(MissileCameraHudSnapshot snapshot)
        {
            bool show = snapshot.HasFeed;
            for (int i = 0; i < 4; i++)
            {
                if (_nesw[i].enabled != show)
                    _nesw[i].enabled = show;
            }
            if (!show) return;

            float hdg = snapshot.MissileHeadingDeg;
            if (!float.IsNaN(_lastHdg) && Mathf.Abs(Mathf.DeltaAngle(_lastHdg, hdg)) < 0.4f)
                return;
            _lastHdg = hdg;

            for (int i = 0; i < 4; i++)
            {
                float screenDeg = WorldBearing[i] - hdg;
                float rad = screenDeg * Mathf.Deg2Rad;
                _nesw[i].rectTransform.anchoredPosition = new Vector2(
                    Mathf.Sin(rad) * _cardinalR,
                    Mathf.Cos(rad) * _cardinalR);
            }
        }

        private static Image Line(RectTransform parent, string name)
        {
            Image img = GunshipChrome.CreateImage(parent, name, GunshipChrome.White);
            RectTransform rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            return img;
        }

        private static void Size(Image img, float x, float y, float w, float h)
        {
            RectTransform rt = img.rectTransform;
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }
    }
}
