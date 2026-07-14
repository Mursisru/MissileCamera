using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// TGP-style quadrant HUD: ownship+RNG/ALT/SPD, HDG/REL/CLOS, MODE/PALETTE, RID/MAG.
    /// </summary>
    internal sealed class MissileCameraTgpHud
    {
        private readonly RectTransform _root;
        private readonly Text _ownship;
        private readonly Text _rng;
        private readonly Text _alt;
        private readonly Text _spd;
        private readonly Text _hdg;
        private readonly Text _rel;
        private readonly Text _clos;
        private readonly Text _mode;
        private readonly Text _palette;
        private readonly Text _rid;
        private readonly Text _mag;
        private readonly Text _pipTitle;
        private float _layoutW = -1f;
        private float _layoutH = -1f;
        private float _nextContentTime;
        private string _lastFingerprint = string.Empty;

        private MissileCameraTgpHud(
            RectTransform root,
            Text ownship,
            Text rng,
            Text alt,
            Text spd,
            Text hdg,
            Text rel,
            Text clos,
            Text mode,
            Text palette,
            Text rid,
            Text mag,
            Text pipTitle)
        {
            _root = root;
            _ownship = ownship;
            _rng = rng;
            _alt = alt;
            _spd = spd;
            _hdg = hdg;
            _rel = rel;
            _clos = clos;
            _mode = mode;
            _palette = palette;
            _rid = rid;
            _mag = mag;
            _pipTitle = pipTitle;
        }

        internal static MissileCameraTgpHud Create(RectTransform parent)
        {
            var rootGo = new GameObject("MissileCameraTgpHud", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            RectTransform root = rootGo.GetComponent<RectTransform>();
            Stretch(root);

            Text ownship = CreateLabel(root, "TgpOwnship", TextAnchor.UpperLeft);
            Text rng = CreateLabel(root, "TgpRng", TextAnchor.UpperLeft);
            Text alt = CreateLabel(root, "TgpAlt", TextAnchor.UpperLeft);
            Text spd = CreateLabel(root, "TgpSpd", TextAnchor.UpperLeft);

            Text hdg = CreateLabel(root, "TgpHdg", TextAnchor.UpperRight);
            Text rel = CreateLabel(root, "TgpRel", TextAnchor.UpperRight);
            Text clos = CreateLabel(root, "TgpClos", TextAnchor.UpperRight);

            Text mode = CreateLabel(root, "TgpMode", TextAnchor.LowerRight);
            Text palette = CreateLabel(root, "TgpPalette", TextAnchor.LowerRight);

            Text rid = CreateLabel(root, "TgpRid", TextAnchor.LowerLeft);
            Text mag = CreateLabel(root, "TgpMag", TextAnchor.LowerLeft);
            Text pipTitle = CreateLabel(root, "TgpPipTitle", TextAnchor.LowerLeft);

            return new MissileCameraTgpHud(
                root, ownship, rng, alt, spd, hdg, rel, clos, mode, palette, rid, mag, pipTitle);
        }

        internal void InvalidateLayout()
        {
            _layoutW = -1f;
            _layoutH = -1f;
            _lastFingerprint = string.Empty;
        }

        internal void SetVisible(bool visible) => _root.gameObject.SetActive(visible);

        internal void Update(MissileCameraHudSnapshot snapshot, MissileCameraPanelMetrics panel)
        {
            float now = Time.unscaledTime;
            bool panelChanged = !Mathf.Approximately(panel.Width, _layoutW)
                || !Mathf.Approximately(panel.Height, _layoutH);

            if (panelChanged)
            {
                _layoutW = panel.Width;
                _layoutH = panel.Height;
                ApplyLayout(panel);
                ApplyFonts(panel);
                ApplyColors();
            }

            if (now < _nextContentTime && !panelChanged)
                return;

            _nextContentTime = now + 0.1f;
            string fingerprint = BuildFingerprint(snapshot);
            if (fingerprint == _lastFingerprint && !panelChanged)
                return;

            _lastFingerprint = fingerprint;
            ApplyContent(snapshot);
        }

        private void ApplyContent(MissileCameraHudSnapshot snapshot)
        {
            _ownship.text = snapshot.OwnshipName;
            _rng.text = snapshot.TgpRngText;
            _alt.text = snapshot.TgpAltText;
            _spd.text = snapshot.TgpSpdText;
            _hdg.text = snapshot.TgpHdgText;
            _rel.text = snapshot.TgpRelText;
            _clos.text = snapshot.TgpClosText;
            _mode.text = snapshot.TgpModeText;
            _palette.text = snapshot.TgpPaletteText;
            _rid.text = snapshot.TgpRidText;
            _mag.text = snapshot.TgpMagText;
            _pipTitle.text = "TOR: Cockpit View";
        }

        private static string BuildFingerprint(MissileCameraHudSnapshot snapshot) =>
            string.Concat(
                snapshot.OwnshipName, "|",
                snapshot.TgpRngText, "|",
                snapshot.TgpAltText, "|",
                snapshot.TgpSpdText, "|",
                snapshot.TgpHdgText, "|",
                snapshot.TgpRelText, "|",
                snapshot.TgpClosText, "|",
                snapshot.TgpModeText, "|",
                snapshot.TgpPaletteText, "|",
                snapshot.TgpMagText, "|",
                snapshot.TgpRidText);

        private void ApplyLayout(MissileCameraPanelMetrics panel)
        {
            float pad = panel.HorizontalInset;
            float vPad = panel.VerticalInset;
            float rowH = MissileCameraPanelMetrics.IsGameFullscreen
                ? 22f
                : Mathf.Clamp(panel.MinSide * 0.055f, 14f, 22f);
            float stackW = Mathf.Clamp(panel.Width * 0.28f, 140f, 320f);
            float pipH = Mathf.Clamp(panel.MinSide * 0.22f, 72f, 180f);
            float pipW = pipH * 1.35f;
            float pipBottom = vPad;
            float pipLeft = pad;

            PlaceStackTopLeft(_ownship, pad, -vPad, stackW, rowH);
            PlaceStackTopLeft(_rng, pad, -(vPad + rowH), stackW, rowH);
            PlaceStackTopLeft(_alt, pad, -(vPad + rowH * 2f), stackW, rowH);
            PlaceStackTopLeft(_spd, pad, -(vPad + rowH * 3f), stackW, rowH);

            PlaceStackTopRight(_hdg, -pad, -vPad, stackW, rowH);
            PlaceStackTopRight(_rel, -pad, -(vPad + rowH), stackW, rowH);
            PlaceStackTopRight(_clos, -pad, -(vPad + rowH * 2f), stackW, rowH);

            PlaceStackBottomRight(_palette, -pad, vPad, stackW, rowH);
            PlaceStackBottomRight(_mode, -pad, vPad + rowH, stackW, rowH);

            float labelBottom = pipBottom + pipH + 4f;
            PlaceBottomLeft(_pipTitle, pipLeft, labelBottom + rowH, pipW, rowH);
            PlaceBottomLeft(_rid, pipLeft + pipW + 8f, pipBottom + rowH, stackW * 0.7f, rowH);
            PlaceBottomLeft(_mag, pipLeft + pipW + 8f, pipBottom, stackW * 0.7f, rowH);
        }

        private void ApplyFonts(MissileCameraPanelMetrics panel)
        {
            int body = MissileCameraPanelMetrics.IsGameFullscreen
                ? 18
                : Mathf.Clamp(Mathf.RoundToInt(panel.MinSide * 0.045f), 11, 18);
            int header = body + 2;
            Font? font = HudFontHelper.GetFont();

            ApplyFont(_ownship, font, header);
            foreach (Text t in new[] { _rng, _alt, _spd, _hdg, _rel, _clos, _mode, _palette, _rid, _mag, _pipTitle })
                ApplyFont(t, font, body);
        }

        private void ApplyColors()
        {
            _ownship.color = MissileCameraHudConfig.OwnshipNameColor;
            Color white = Color.white;
            foreach (Text t in new[] { _rng, _alt, _spd, _hdg, _rel, _clos, _mode, _palette, _rid, _mag, _pipTitle })
                t.color = white;
        }

        private static void ApplyFont(Text text, Font? font, int size)
        {
            if (font != null)
                text.font = font;
            text.fontSize = size;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
        }

        private static void PlaceStackTopLeft(Text text, float x, float y, float w, float h)
        {
            RectTransform rt = text.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            text.alignment = TextAnchor.MiddleLeft;
        }

        private static void PlaceStackTopRight(Text text, float x, float y, float w, float h)
        {
            RectTransform rt = text.rectTransform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            text.alignment = TextAnchor.MiddleRight;
        }

        private static void PlaceStackBottomRight(Text text, float x, float y, float w, float h)
        {
            RectTransform rt = text.rectTransform;
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            text.alignment = TextAnchor.MiddleRight;
        }

        private static void PlaceBottomLeft(Text text, float x, float y, float w, float h)
        {
            RectTransform rt = text.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            text.alignment = TextAnchor.MiddleLeft;
        }

        private static Text CreateLabel(RectTransform parent, string name, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
