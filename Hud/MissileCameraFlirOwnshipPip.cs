using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// Fullscreen FLIR bottom-left ownship nose mini-cam with ALT/SPD labels.
    /// </summary>
    internal sealed class MissileCameraFlirOwnshipPip
    {
        private static readonly Color FlirGreen = new Color(0.55f, 1f, 0.9f, 1f);

        private const float BorderPx = 2.0f;
        private const float LabelPad = 4f;
        private const float RenderFps = 30f;
        private const int TexSize = 256;

        private readonly RectTransform _root;
        private readonly RawImage _feed;
        private readonly Text _alt;
        private readonly Text _spd;
        private readonly Text _title;
        private MissileCameraAircraftRig? _rig;
        private float _layoutW = -1f;
        private float _layoutH = -1f;
        private float _nextRenderUnscaled;
        private string _lastAlt = string.Empty;
        private string _lastSpd = string.Empty;
        private readonly StringBuilder _sb = new StringBuilder(32);

        private MissileCameraFlirOwnshipPip(
            RectTransform root,
            RawImage feed,
            Text title,
            Text alt,
            Text spd)
        {
            _root = root;
            _feed = feed;
            _title = title;
            _alt = alt;
            _spd = spd;
        }

        internal static MissileCameraFlirOwnshipPip Create(RectTransform parent)
        {
            var rootGo = new GameObject("FlirOwnshipPip", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            RectTransform root = rootGo.GetComponent<RectTransform>();

            CreateBorderEdge(root, "EdgeT", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -BorderPx), Vector2.zero);
            CreateBorderEdge(root, "EdgeB", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, BorderPx));
            CreateBorderEdge(root, "EdgeL", new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(BorderPx, 0f));
            CreateBorderEdge(root, "EdgeR", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-BorderPx, 0f), Vector2.zero);

            var feedGo = new GameObject("Feed", typeof(RectTransform), typeof(RawImage));
            feedGo.transform.SetParent(root, false);
            RectTransform feedRt = feedGo.GetComponent<RectTransform>();
            feedRt.anchorMin = Vector2.zero;
            feedRt.anchorMax = Vector2.one;
            feedRt.offsetMin = new Vector2(BorderPx, BorderPx);
            feedRt.offsetMax = new Vector2(-BorderPx, -BorderPx);
            RawImage feed = feedGo.GetComponent<RawImage>();
            feed.raycastTarget = false;
            feed.color = Color.white;

            Text title = CreateLabel(root, "Title", "OWN");
            Text alt = CreateLabel(root, "Alt", "ALT ---");
            Text spd = CreateLabel(root, "Spd", "SPD ---");

            return new MissileCameraFlirOwnshipPip(root, feed, title, alt, spd);
        }

        internal float Size { get; private set; } = 160f;

        internal void Place(MissileCameraPanelMetrics panel, float pad)
        {
            if (_root != null && !_root.gameObject.activeSelf)
                _root.gameObject.SetActive(true);

            if (Mathf.Approximately(panel.Width, _layoutW)
                && Mathf.Approximately(panel.Height, _layoutH))
                return;

            _layoutW = panel.Width;
            _layoutH = panel.Height;

            Size = Mathf.Clamp(panel.MinSide * 0.22f, 160f, 260f);
            _root.anchorMin = new Vector2(0f, 0f);
            _root.anchorMax = new Vector2(0f, 0f);
            _root.pivot = new Vector2(0f, 0f);
            _root.anchoredPosition = new Vector2(pad, pad);
            _root.sizeDelta = new Vector2(Size, Size);

            PlaceLabel(_title, 0f, 1f, LabelPad, -LabelPad, Size - LabelPad * 2f, 14f, TextAnchor.UpperLeft);
            PlaceLabel(_alt, 0f, 0f, LabelPad, LabelPad + 14f, Size - LabelPad * 2f, 14f, TextAnchor.LowerLeft);
            PlaceLabel(_spd, 0f, 0f, LabelPad, LabelPad, Size - LabelPad * 2f, 14f, TextAnchor.LowerLeft);

            Font font = HudFontHelper.GetFont();
            const int fontSize = 11;
            foreach (Text t in new[] { _title, _alt, _spd })
            {
                t.font = font;
                t.fontSize = fontSize;
                t.color = FlirGreen;
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.verticalOverflow = VerticalWrapMode.Truncate;
                t.raycastTarget = false;
            }
        }

        internal void Update()
        {
            if (!_root.gameObject.activeInHierarchy)
                return;

            if (!AircraftCamAccess.TryGetLocalAircraft(out Aircraft aircraft) || aircraft == null)
            {
                _feed.enabled = false;
                _rig?.Detach();
                SetText(_alt, "ALT ---", ref _lastAlt);
                SetText(_spd, "SPD ---", ref _lastSpd);
                return;
            }

            if (_rig == null)
            {
                _rig = new MissileCameraAircraftRig();
                _rig.SetForceNose(true);
                _rig.ConfigureTexture(TexSize, TexSize);
            }

            _rig.Attach(aircraft);
            UpdateTelemetry(aircraft);

            if (Time.unscaledTime < _nextRenderUnscaled)
            {
                if (_feed.texture != _rig.Texture && _rig.Texture != null)
                    _feed.texture = _rig.Texture;
                _feed.enabled = _rig.Texture != null;
                return;
            }

            _nextRenderUnscaled = Time.unscaledTime + (1f / Mathf.Max(RenderFps, 1f));
            try
            {
                _rig.RenderFrame(managePrep: true);
            }
            catch (System.Exception ex)
            {
                MfdLog.Info("ownship pip render: " + ex.Message);
            }

            if (_rig.Texture != null)
            {
                _feed.texture = _rig.Texture;
                _feed.enabled = true;
            }
        }

        internal void Hide()
        {
            try
            {
                if (_root != null && _root.gameObject.activeSelf)
                    _root.gameObject.SetActive(false);
            }
            catch { /* ignore */ }

            // Scene may have already destroyed RawImage — never touch enabled on a dead Behaviour.
            try
            {
                if (_feed != null)
                {
                    _feed.enabled = false;
                    _feed.texture = null;
                }
            }
            catch
            {
                // ignore destroyed Unity objects
            }

            try { _rig?.Detach(); }
            catch { /* ignore */ }

            _nextRenderUnscaled = 0f;
        }

        internal void Shutdown()
        {
            Hide();
            try { _rig?.Destroy(); }
            catch { /* ignore */ }
            _rig = null;
            _layoutW = -1f;
            _layoutH = -1f;
            _lastAlt = string.Empty;
            _lastSpd = string.Empty;
        }

        private void UpdateTelemetry(Aircraft aircraft)
        {
            if (AircraftCamAccess.TryGetOwnshipAltitudeMeters(aircraft, out float altM))
            {
                _sb.Length = 0;
                _sb.Append("ALT ").Append(UnitConverter.AltitudeReading(altM));
                SetText(_alt, _sb.ToString(), ref _lastAlt);
            }
            else
            {
                SetText(_alt, "ALT ---", ref _lastAlt);
            }

            if (AircraftCamAccess.TryGetOwnshipSpeedMs(aircraft, out float spdMs))
            {
                _sb.Length = 0;
                _sb.Append("SPD ").Append(UnitConverter.SpeedReading(spdMs));
                SetText(_spd, _sb.ToString(), ref _lastSpd);
            }
            else
            {
                SetText(_spd, "SPD ---", ref _lastSpd);
            }
        }

        private static void SetText(Text text, string value, ref string last)
        {
            if (last == value)
                return;
            last = value;
            text.text = value;
        }

        private static void PlaceLabel(
            Text text,
            float ax,
            float ay,
            float x,
            float y,
            float w,
            float h,
            TextAnchor align)
        {
            RectTransform rt = text.rectTransform;
            rt.anchorMin = new Vector2(ax, ay);
            rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = new Vector2(ax, ay);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            text.alignment = align;
        }

        private static Text CreateLabel(RectTransform parent, string name, string text)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text label = go.GetComponent<Text>();
            label.text = text;
            label.alignment = TextAnchor.LowerLeft;
            label.color = FlirGreen;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(0.7f, 0.7f);
            label.raycastTarget = false;
            return label;
        }

        private static void CreateBorderEdge(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            UiImageHelper.ApplySolid(img, FlirGreen);
            RectTransform rt = img.rectTransform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }
    }
}
