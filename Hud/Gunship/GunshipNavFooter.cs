using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>Bottom-left compact NAV line.</summary>
    internal sealed class GunshipNavFooter
    {
        private readonly Text _line;
        private string _last = "";

        private GunshipNavFooter(Text line) => _line = line;

        internal static GunshipNavFooter Create(RectTransform parent)
        {
            Text line = GunshipChrome.CreateText(parent, "GunshipNav", TextAnchor.LowerLeft, 12);
            line.color = GunshipChrome.WhiteDim;
            return new GunshipNavFooter(line);
        }

        internal void Place(MissileCameraPanelMetrics panel)
        {
            float pad = Mathf.Max(panel.HorizontalInset, 24f) + 36f;
            float bottom = Mathf.Max(panel.VerticalInset, 16f) + 14f;
            GunshipChrome.Place(_line.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(pad, bottom), new Vector2(420f, 72f));
            _line.fontSize = GunshipChrome.FontBody;
            _line.fontStyle = FontStyle.Normal;
        }

        internal void Update(MissileCameraHudSnapshot snapshot)
        {
            string grid = string.IsNullOrEmpty(snapshot.GridText) ? "---" : snapshot.GridText;
            string tgt = snapshot.HasTarget
                ? (string.IsNullOrEmpty(snapshot.TargetGridText) ? "TGT" : snapshot.TargetGridText)
                : "---";
            string text = "GEOPOINT  " + grid
                + "\nNAV PRG  " + (snapshot.HasTarget ? "1.00" : "0.00")
                + "\nNAV CORR  " + tgt;
            if (text == _last)
                return;
            _last = text;
            _line.text = text;
        }
    }
}
