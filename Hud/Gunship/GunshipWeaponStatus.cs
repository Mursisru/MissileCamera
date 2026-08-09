using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// Bottom-right ammo-style table. FUEL/THR live here (edge gauges hidden — RC Update still runs).
    /// </summary>
    internal sealed class GunshipWeaponStatus
    {
        private static readonly StringBuilder Sb = new StringBuilder(96);

        private readonly Image _bg;
        private readonly RectTransform _sel;
        private readonly Text _body;
        private string _last = "";

        private GunshipWeaponStatus(Image bg, RectTransform sel, Text body)
        {
            _bg = bg;
            _sel = sel;
            _body = body;
        }

        internal static GunshipWeaponStatus Create(RectTransform parent)
        {
            Image bg = GunshipChrome.CreateImage(parent, "GunshipWeaponBg", GunshipChrome.PanelBg);
            var selGo = new GameObject("GunshipWeaponSel", typeof(RectTransform));
            selGo.transform.SetParent(bg.rectTransform, false);
            RectTransform sel = selGo.GetComponent<RectTransform>();
            // Hollow selection border around bottom row (active weapon analogue)
            CreateEdge(sel, "T", true);
            CreateEdge(sel, "B", true);
            CreateEdge(sel, "L", false);
            CreateEdge(sel, "R", false);

            Text body = GunshipChrome.CreateText(bg.rectTransform, "GunshipWeaponBody", TextAnchor.UpperLeft, GunshipChrome.FontBody);
            body.fontStyle = FontStyle.Bold;
            body.lineSpacing = 1.15f;
            return new GunshipWeaponStatus(bg, sel, body);
        }

        internal void Place(MissileCameraPanelMetrics panel)
        {
            float pad = Mathf.Max(panel.HorizontalInset, 18f) + 10f;
            float bottom = Mathf.Max(panel.VerticalInset, 16f) + 14f;
            GunshipChrome.Place(_bg.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-pad, bottom), new Vector2(200f, 108f));
            GunshipChrome.Place(_body.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            _body.rectTransform.offsetMin = new Vector2(16f, 10f);
            _body.rectTransform.offsetMax = new Vector2(-12f, -10f);

            _sel.anchorMin = new Vector2(0.04f, 0.04f);
            _sel.anchorMax = new Vector2(0.96f, 0.36f);
            _sel.offsetMin = Vector2.zero;
            _sel.offsetMax = Vector2.zero;
        }

        internal void Update(MissileCameraHudSnapshot snapshot)
        {
            int fuelPct = Mathf.RoundToInt(Mathf.Clamp01(snapshot.FuelFraction) * 100f);
            int thrPct = Mathf.RoundToInt(Mathf.Clamp01(snapshot.ThrottleFraction) * 100f);
            string mode = MissileCameraVisionModeController.Mode switch
            {
                MissileCameraVisionMode.NightVision => "NVG",
                MissileCameraVisionMode.WhiteHot => "WH",
                MissileCameraVisionMode.BlackHot => "BH",
                MissileCameraVisionMode.WhiteContour => "EDGE+",
                MissileCameraVisionMode.BlackContour => "EDGE-",
                _ => "COLOR"
            };

            Sb.Length = 0;
            Sb.Append("1    MISSILE");
            Sb.Append('\n').Append(fuelPct.ToString(CultureInfo.InvariantCulture).PadLeft(3)).Append("   FUEL");
            Sb.Append('\n').Append(thrPct.ToString(CultureInfo.InvariantCulture).PadLeft(3)).Append("   THR");
            Sb.Append('\n').Append(mode.PadLeft(3)).Append("   MODE");

            string text = Sb.ToString();
            if (text == _last)
                return;
            _last = text;
            _body.text = text;
        }

        private static void CreateEdge(RectTransform parent, string name, bool horizontal)
        {
            Image img = GunshipChrome.CreateImage(parent, name, GunshipChrome.White);
            RectTransform rt = img.rectTransform;
            if (horizontal)
            {
                bool top = name == "T";
                rt.anchorMin = new Vector2(0f, top ? 1f : 0f);
                rt.anchorMax = new Vector2(1f, top ? 1f : 0f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(0f, 1.5f);
                rt.anchoredPosition = Vector2.zero;
            }
            else
            {
                bool left = name == "L";
                rt.anchorMin = new Vector2(left ? 0f : 1f, 0f);
                rt.anchorMax = new Vector2(left ? 0f : 1f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(1.5f, 0f);
                rt.anchoredPosition = Vector2.zero;
            }
        }
    }
}
