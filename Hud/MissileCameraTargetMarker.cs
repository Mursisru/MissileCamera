using UnityEngine;

namespace MissileCamera
{
    internal sealed class MissileCameraTargetMarker
    {
        private static readonly Color MarkerColor = new Color(0.4f, 0.9f, 1f, 1f);

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
            _root.gameObject.SetActive(visible && projection.Valid && projection.InFront);
            if (!_root.gameObject.activeSelf)
                return;

            _size = Mathf.Clamp(panelMinSide * 0.05f, 10f, 24f);
            float thickness = Mathf.Max(1.2f, _size * 0.08f);
            float half = _size * 0.5f;
            _root.anchoredPosition = projection.AnchoredPosition;

            Vector2 topLeft = new Vector2(-half, half);
            Vector2 topRight = new Vector2(half, half);
            Vector2 bottomRight = new Vector2(half, -half);
            Vector2 bottomLeft = new Vector2(-half, -half);

            _edges[0].SetLine(topLeft, topRight, thickness, MarkerColor);
            _edges[1].SetLine(topRight, bottomRight, thickness, MarkerColor);
            _edges[2].SetLine(bottomRight, bottomLeft, thickness, MarkerColor);
            _edges[3].SetLine(bottomLeft, topLeft, thickness, MarkerColor);
        }
    }
}
