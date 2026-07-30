using UnityEngine;

namespace MissileCamera
{
    /// <summary>MFD seeker-feed locked-target diamond (orange).</summary>
    internal sealed class MissileCameraTargetMarker
    {
        private readonly RectTransform _root;
        private readonly HudLineGraphic[] _edges;
        private float _size;

        private MissileCameraTargetMarker(RectTransform root, HudLineGraphic[] edges)
        {
            _root = root;
            _edges = edges;
        }

        internal static MissileCameraTargetMarker Create(RectTransform parent)
        {
            var rootGo = new GameObject("MissileCameraHudTargetMarker", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            RectTransform root = rootGo.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = Vector2.zero;

            var edges = new HudLineGraphic[4];
            for (int i = 0; i < edges.Length; i++)
            {
                var go = new GameObject($"TargetMarkerEdge{i}", typeof(RectTransform), typeof(HudLineGraphic));
                go.transform.SetParent(root, false);
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                edges[i] = go.GetComponent<HudLineGraphic>();
            }

            return new MissileCameraTargetMarker(root, edges);
        }

        internal void Update(FeedProjection projection, float panelMinSide, bool visible)
        {
            try
            {
                if (_root == null)
                    return;

                bool show = visible && projection.Valid && projection.InFront;
                if (_root.gameObject.activeSelf != show)
                    _root.gameObject.SetActive(show);
                if (!show)
                    return;

                _size = Mathf.Clamp(panelMinSide * 0.05f, 10f, 24f);
                float thickness = Mathf.Max(1.2f, _size * 0.08f);
                float half = _size * 0.5f;
                _root.anchoredPosition = projection.AnchoredPosition;

                // Diamond (rhombus): N / E / S / W tips.
                Vector2 north = new Vector2(0f, half);
                Vector2 east = new Vector2(half, 0f);
                Vector2 south = new Vector2(0f, -half);
                Vector2 west = new Vector2(-half, 0f);

                Color color = MissileCameraHudConfig.TargetMarkerColor;
                _edges[0].SetLine(north, east, thickness, color);
                _edges[1].SetLine(east, south, thickness, color);
                _edges[2].SetLine(south, west, thickness, color);
                _edges[3].SetLine(west, north, thickness, color);
            }
            catch
            {
                // Scene-destroyed graphic — never abort MFD HUD update.
            }
        }

        internal void SetVisible(bool visible)
        {
            try
            {
                if (_root != null && _root.gameObject.activeSelf != visible)
                    _root.gameObject.SetActive(visible);
            }
            catch
            {
                // ignore
            }
        }
    }
}
