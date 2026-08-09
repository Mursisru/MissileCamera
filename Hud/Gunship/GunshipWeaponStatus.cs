using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// Bottom-right gunship status: single neat column (value + label per line).
    /// No selection frame. FUEL/THR as %.
    /// </summary>
    internal sealed class GunshipWeaponStatus
    {
        private static readonly StringBuilder Sb = new StringBuilder(96);
        private const float RowH = 20f;
        private const float Pad = 4f;

        private readonly RectTransform _root;
        private readonly Text _body;
        private string _last = "";

        private GunshipWeaponStatus(RectTransform root, Text body)
        {
            _root = root;
            _body = body;
        }

        internal static GunshipWeaponStatus Create(RectTransform parent)
        {
            var go = new GameObject("GunshipWeapon", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform root = go.GetComponent<RectTransform>();

            Text body = GunshipChrome.CreateText(root, "Body", TextAnchor.LowerRight, GunshipChrome.FontBody);
            body.fontStyle = FontStyle.Normal;
            body.lineSpacing = 1.08f;
            return new GunshipWeaponStatus(root, body);
        }

        internal void Place(MissileCameraPanelMetrics panel)
        {
            float px = GunshipChrome.PadX(panel);
            float py = GunshipChrome.PadY(panel);
            const int rows = 4;
            float h = RowH * rows + Pad * 2f;
            GunshipChrome.Place(_root, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-px, py), new Vector2(160f, h));

            GunshipChrome.Place(_body.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            _body.rectTransform.offsetMin = new Vector2(Pad, Pad);
            _body.rectTransform.offsetMax = new Vector2(-Pad, -Pad);
        }

        internal void Update(MissileCameraHudSnapshot snapshot)
        {
            int fuel = Mathf.RoundToInt(Mathf.Clamp01(snapshot.FuelFraction) * 100f);
            int thr = Mathf.RoundToInt(Mathf.Clamp01(snapshot.ThrottleFraction) * 100f);
            string mode = MissileCameraVisionModeController.Mode switch
            {
                MissileCameraVisionMode.NightVision => "NVG",
                MissileCameraVisionMode.WhiteHot => "WH",
                MissileCameraVisionMode.BlackHot => "BH",
                MissileCameraVisionMode.WhiteContour => "EDGE+",
                MissileCameraVisionMode.BlackContour => "EDGE-",
                _ => "TV"
            };

            // Single right-aligned column (Arial isn't mono — no space padding)
            Sb.Length = 0;
            Sb.Append("1 MISSILE");
            Sb.Append('\n').Append(fuel.ToString(CultureInfo.InvariantCulture)).Append("% FUEL");
            Sb.Append('\n').Append(thr.ToString(CultureInfo.InvariantCulture)).Append("% THR");
            Sb.Append('\n').Append(mode).Append(" MODE");

            string text = Sb.ToString();
            if (text == _last) return;
            _last = text;
            _body.text = text;
        }
    }
}
