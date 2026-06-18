using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    internal sealed class HudRingGraphic : MaskableGraphic
    {
        private const int SegmentCount = 48;

        private float _radius = 12f;
        private float _thickness = 2f;
        private bool _filled;

        internal void SetRing(float radius, float thickness, Color ringColor, bool filled = false)
        {
            float clampedRadius = Mathf.Max(1f, radius);
            float clampedThickness = Mathf.Max(0.5f, thickness);
            if (Mathf.Approximately(_radius, clampedRadius)
                && Mathf.Approximately(_thickness, clampedThickness)
                && _filled == filled
                && ColorsMatch(color, ringColor))
            {
                return;
            }

            _radius = clampedRadius;
            _thickness = clampedThickness;
            _filled = filled;
            color = ringColor;
            SetVerticesDirty();
        }

        private static bool ColorsMatch(Color a, Color b) =>
            Mathf.Approximately(a.r, b.r)
            && Mathf.Approximately(a.g, b.g)
            && Mathf.Approximately(a.b, b.b)
            && Mathf.Approximately(a.a, b.a);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_radius <= 0.5f)
                return;

            if (_filled)
            {
                AddFilledCircle(vh);
                return;
            }

            float outer = _radius + _thickness * 0.5f;
            float inner = Mathf.Max(0.5f, _radius - _thickness * 0.5f);
            float step = Mathf.PI * 2f / SegmentCount;

            for (int i = 0; i < SegmentCount; i++)
            {
                float a0 = step * i;
                float a1 = step * (i + 1);
                Vector2 o0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * outer;
                Vector2 o1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * outer;
                Vector2 i0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * inner;
                Vector2 i1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * inner;
                AddQuad(vh, o0, o1, i1, i0, color);
            }
        }

        private void AddFilledCircle(VertexHelper vh)
        {
            UIVertex center = UIVertex.simpleVert;
            center.color = color;
            center.position = Vector2.zero;
            int centerIndex = vh.currentVertCount;
            vh.AddVert(center);

            float step = Mathf.PI * 2f / SegmentCount;
            for (int i = 0; i <= SegmentCount; i++)
            {
                float angle = step * i;
                UIVertex vert = UIVertex.simpleVert;
                vert.color = color;
                vert.position = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _radius;
                vh.AddVert(vert);
            }

            for (int i = 0; i < SegmentCount; i++)
                vh.AddTriangle(centerIndex, centerIndex + i + 1, centerIndex + i + 2);
        }

        private static void AddQuad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color quadColor)
        {
            int start = vh.currentVertCount;
            AddVert(vh, a, quadColor);
            AddVert(vh, b, quadColor);
            AddVert(vh, c, quadColor);
            AddVert(vh, d, quadColor);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        private static void AddVert(VertexHelper vh, Vector2 pos, Color vertColor)
        {
            UIVertex vert = UIVertex.simpleVert;
            vert.color = vertColor;
            vert.position = pos;
            vh.AddVert(vert);
        }
    }
}
