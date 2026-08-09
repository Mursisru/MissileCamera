using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>Bottom-left COD NAV: GEOPOINT / NAV PRG / NAV CORR (EN).</summary>
    internal sealed class GunshipNavFooter
    {
        private readonly Text _line;
        private string _last = "";
        private float _prgSmooth;

        private GunshipNavFooter(Text line) => _line = line;

        internal static GunshipNavFooter Create(RectTransform parent)
        {
            Text line = GunshipChrome.CreateText(parent, "GunshipNav", TextAnchor.LowerLeft, GunshipChrome.FontBody);
            line.fontStyle = FontStyle.Normal;
            line.color = GunshipChrome.White;
            line.lineSpacing = 1.08f;
            return new GunshipNavFooter(line);
        }

        internal void Place(MissileCameraPanelMetrics panel)
        {
            float px = GunshipChrome.PadX(panel);
            float py = GunshipChrome.PadY(panel);
            GunshipChrome.Place(_line.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(px, py), new Vector2(380f, 70f));
        }

        internal void Update(MissileCameraHudSnapshot snapshot)
        {
            string geo = string.IsNullOrEmpty(snapshot.GridText) ? "---" : snapshot.GridText;
            string corr = snapshot.HasTarget
                ? (string.IsNullOrEmpty(snapshot.TargetGridText) ? "TGT" : snapshot.TargetGridText)
                : "";

            float targetPrg = snapshot.HasTarget ? 1f : 0f;
            // Live drift like COD NAV PRG
            float noise = (Mathf.PerlinNoise(Time.unscaledTime * 0.55f, 8.2f) - 0.5f) * 0.04f;
            _prgSmooth = Mathf.MoveTowards(_prgSmooth, targetPrg, Time.unscaledDeltaTime * 0.35f);
            float prg = Mathf.Clamp01(_prgSmooth + (snapshot.HasTarget ? noise : 0f));

            string text = "GEOPOINT  " + geo
                + "\nNAV PRG  " + prg.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                + "\nNAV CORR  " + corr;
            if (text == _last) return;
            _last = text;
            _line.text = text;
        }
    }
}
