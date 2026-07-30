using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    internal sealed class MissileCameraMarkerView
    {
        private readonly RectTransform _root;
        private readonly HudLineGraphic[] _edges;
        private readonly HudLineGraphic _motionLine;
        private readonly Text _label;
        private bool _inUse;

        private MissileCameraMarkerView(
            RectTransform root,
            HudLineGraphic[] edges,
            HudLineGraphic motionLine,
            Text label)
        {
            _root = root;
            _edges = edges;
            _motionLine = motionLine;
            _label = label;
        }

        internal bool InUse => _inUse;

        internal static MissileCameraMarkerView Create(RectTransform parent, int index)
        {
            var rootGo = new GameObject("Marker_" + index, typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            RectTransform root = rootGo.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = Vector2.zero;
            rootGo.SetActive(false);

            var edges = new HudLineGraphic[4];
            for (int i = 0; i < edges.Length; i++)
            {
                var go = new GameObject("Edge" + i, typeof(RectTransform), typeof(HudLineGraphic));
                go.transform.SetParent(root, false);
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                edges[i] = go.GetComponent<HudLineGraphic>();
            }

            var motionGo = new GameObject("Motion", typeof(RectTransform), typeof(HudLineGraphic));
            motionGo.transform.SetParent(root, false);
            RectTransform motionRt = motionGo.GetComponent<RectTransform>();
            motionRt.anchorMin = new Vector2(0.5f, 0.5f);
            motionRt.anchorMax = new Vector2(0.5f, 0.5f);
            motionRt.pivot = new Vector2(0.5f, 0.5f);
            HudLineGraphic motionLine = motionGo.GetComponent<HudLineGraphic>();
            motionGo.SetActive(false);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(root, false);
            Text label = labelGo.GetComponent<Text>();
            label.alignment = TextAnchor.LowerCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.font = HudFontHelper.GetFont();
            label.fontSize = 14;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform labelRt = label.rectTransform;
            labelRt.anchorMin = new Vector2(0.5f, 0.5f);
            labelRt.anchorMax = new Vector2(0.5f, 0.5f);
            labelRt.pivot = new Vector2(0.5f, 0f);
            labelRt.anchoredPosition = new Vector2(0f, 14f);
            labelRt.sizeDelta = new Vector2(220f, 22f);

            return new MissileCameraMarkerView(root, edges, motionLine, label);
        }

        internal void Show(
            FeedProjection projection,
            float panelMinSide,
            Color color,
            bool diamond,
            string? labelText,
            bool hasMotion,
            Vector2 motionTipAnchored)
        {
            _inUse = true;
            _root.gameObject.SetActive(projection.Valid && projection.InFront);
            if (!_root.gameObject.activeSelf)
                return;

            float size = Mathf.Clamp(panelMinSide * 0.05f, 10f, 28f);
            float thickness = Mathf.Max(1.2f, size * 0.1f);
            float half = size * 0.5f;
            _root.anchoredPosition = projection.AnchoredPosition;

            if (diamond)
            {
                Vector2 top = new Vector2(0f, half);
                Vector2 right = new Vector2(half, 0f);
                Vector2 bottom = new Vector2(0f, -half);
                Vector2 left = new Vector2(-half, 0f);
                _edges[0].SetLine(top, right, thickness, color);
                _edges[1].SetLine(right, bottom, thickness, color);
                _edges[2].SetLine(bottom, left, thickness, color);
                _edges[3].SetLine(left, top, thickness, color);
            }
            else
            {
                Vector2 topLeft = new Vector2(-half, half);
                Vector2 topRight = new Vector2(half, half);
                Vector2 bottomRight = new Vector2(half, -half);
                Vector2 bottomLeft = new Vector2(-half, -half);
                _edges[0].SetLine(topLeft, topRight, thickness, color);
                _edges[1].SetLine(topRight, bottomRight, thickness, color);
                _edges[2].SetLine(bottomRight, bottomLeft, thickness, color);
                _edges[3].SetLine(bottomLeft, topLeft, thickness, color);
            }

            if (hasMotion)
            {
                // Motion tip is in feed-anchored space; convert to local marker space.
                Vector2 localTip = motionTipAnchored - projection.AnchoredPosition;
                float len = localTip.magnitude;
                if (len > 2f)
                {
                    _motionLine.gameObject.SetActive(true);
                    Vector2 dir = localTip / len;
                    Vector2 start = dir * (half + 2f);
                    _motionLine.SetLine(start, localTip, thickness * 1.35f, color);
                }
                else
                    _motionLine.gameObject.SetActive(false);
            }
            else
                _motionLine.gameObject.SetActive(false);

            bool showLabel = !string.IsNullOrEmpty(labelText);
            _label.gameObject.SetActive(showLabel);
            if (showLabel)
            {
                _label.text = labelText;
                _label.color = color;
                _label.fontSize = Mathf.Clamp(Mathf.RoundToInt(panelMinSide * 0.035f), 11, 18);
                _label.rectTransform.anchoredPosition = new Vector2(0f, half + 6f);
            }
        }

        internal void Hide()
        {
            _inUse = false;
            if (_root != null)
                _root.gameObject.SetActive(false);
        }
    }

    internal sealed class MissileCameraMarkerPool
    {
        private readonly List<MissileCameraMarkerView> _pool = new List<MissileCameraMarkerView>();
        private RectTransform? _parent;

        internal void Ensure(RectTransform parent, int capacity)
        {
            if (_parent != parent)
            {
                Clear();
                _parent = parent;
            }

            while (_pool.Count < capacity)
                _pool.Add(MissileCameraMarkerView.Create(parent, _pool.Count));
        }

        internal MissileCameraMarkerView? Rent()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (!_pool[i].InUse)
                    return _pool[i];
            }

            return null;
        }

        internal void ReleaseAll()
        {
            for (int i = 0; i < _pool.Count; i++)
                _pool[i].Hide();
        }

        internal void Clear()
        {
            for (int i = 0; i < _pool.Count; i++)
                _pool[i].Hide();

            _pool.Clear();
            _parent = null;
        }
    }
}
