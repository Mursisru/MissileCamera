using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    internal sealed class HudLineGraphic : MaskableGraphic
    {
        private const int CapsuleCapSegments = 8;
        private const float PositionEpsilon = 0.05f;

        private readonly List<Vector2> _capsuleOutline = new List<Vector2>(24);
        private readonly List<Vector2> _discOutline = new List<Vector2>(12);

        private Vector2 _start;
        private Vector2 _end;
        private float _thickness = 2f;
        private Color _fillColor = Color.white;
        private Color _outlineColor = Color.white;
        private float _outlineThickness;
        private bool _horizonOutline;

        internal void SetLine(Vector2 start, Vector2 end, float thickness, Color lineColor)
        {
            SetStyledLine(start, end, thickness, lineColor, lineColor, 0f, horizonOutline: false);
        }

        internal void SetHorizonBar(
            Vector2 start,
            Vector2 end,
            float thickness,
            Color fillColor,
            Color outlineColor,
            float outlineThickness)
        {
            SetStyledLine(start, end, thickness, fillColor, outlineColor, outlineThickness, horizonOutline: true);
        }

        private void SetStyledLine(
            Vector2 start,
            Vector2 end,
            float thickness,
            Color fillColor,
            Color outlineColor,
            float outlineThickness,
            bool horizonOutline)
        {
            float clampedThickness = Mathf.Max(0.5f, thickness);
            float clampedOutline = Mathf.Max(0f, outlineThickness);
            if (StyledLineMatches(
                    start,
                    end,
                    clampedThickness,
                    fillColor,
                    outlineColor,
                    clampedOutline,
                    horizonOutline))
            {
                return;
            }

            _start = start;
            _end = end;
            _thickness = clampedThickness;
            _fillColor = fillColor;
            _outlineColor = outlineColor;
            _outlineThickness = clampedOutline;
            _horizonOutline = horizonOutline;
            color = Color.white;
            SetVerticesDirty();
        }

        private bool StyledLineMatches(
            Vector2 start,
            Vector2 end,
            float thickness,
            Color fillColor,
            Color outlineColor,
            float outlineThickness,
            bool horizonOutline)
        {
            return (start - _start).sqrMagnitude <= PositionEpsilon * PositionEpsilon
                && (end - _end).sqrMagnitude <= PositionEpsilon * PositionEpsilon
                && Mathf.Approximately(_thickness, thickness)
                && ColorsMatch(_fillColor, fillColor)
                && ColorsMatch(_outlineColor, outlineColor)
                && Mathf.Approximately(_outlineThickness, outlineThickness)
                && _horizonOutline == horizonOutline;
        }

        private static bool ColorsMatch(Color a, Color b) =>
            Mathf.Approximately(a.r, b.r)
            && Mathf.Approximately(a.g, b.g)
            && Mathf.Approximately(a.b, b.b)
            && Mathf.Approximately(a.a, b.a);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Vector2 delta = _end - _start;
            if (delta.sqrMagnitude < 0.0001f)
                return;

            if (_horizonOutline && _outlineThickness > 0.01f)
            {
                AddCapsule(vh, _start, _end, _thickness + _outlineThickness * 2f, _outlineColor, _capsuleOutline);
                AddCapsule(vh, _start, _end, _thickness, _fillColor, _capsuleOutline);
                return;
            }

            if (_outlineThickness > 0.01f)
                AddCapsule(vh, _start, _end, _thickness + _outlineThickness * 2f, _outlineColor, _capsuleOutline);

            AddCapsule(vh, _start, _end, _thickness, _fillColor, _capsuleOutline);
        }

        private static void AddCapsule(
            VertexHelper vh,
            Vector2 start,
            Vector2 end,
            float thickness,
            Color color,
            List<Vector2> outline)
        {
            float radius = thickness * 0.5f;
            Vector2 delta = end - start;
            float length = delta.magnitude;
            if (length < 0.001f)
            {
                AddDisc(vh, start, radius, color, outline);
                return;
            }

            Vector2 dir = delta / length;
            Vector2 normal = new Vector2(-dir.y, dir.x);

            if (length <= radius * 2f)
            {
                AddDisc(vh, (start + end) * 0.5f, radius, color, outline);
                return;
            }

            outline.Clear();
            outline.Add(start + dir * radius + normal * radius);
            outline.Add(end - dir * radius + normal * radius);
            AppendArc(outline, end, dir, normal, radius, -Mathf.PI * 0.5f, Mathf.PI * 0.5f);
            outline.Add(end - dir * radius - normal * radius);
            outline.Add(start + dir * radius - normal * radius);
            AppendArc(outline, start, -dir, normal, radius, -Mathf.PI * 0.5f, Mathf.PI * 0.5f);

            AddConvexPolygon(vh, outline, color);
        }

        private static void AppendArc(
            List<Vector2> points,
            Vector2 center,
            Vector2 axis,
            Vector2 normal,
            float radius,
            float angleMin,
            float angleMax)
        {
            for (int i = 1; i < CapsuleCapSegments; i++)
            {
                float t = i / (float)CapsuleCapSegments;
                float angle = Mathf.Lerp(angleMin, angleMax, t);
                points.Add(center + axis * (Mathf.Cos(angle) * radius) + normal * (Mathf.Sin(angle) * radius));
            }
        }

        private static void AddDisc(VertexHelper vh, Vector2 center, float radius, Color color, List<Vector2> outline)
        {
            outline.Clear();
            for (int i = 0; i <= CapsuleCapSegments; i++)
            {
                float angle = i / (float)CapsuleCapSegments * Mathf.PI * 2f;
                outline.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }

            AddConvexPolygon(vh, outline, color);
        }

        private static void AddConvexPolygon(VertexHelper vh, List<Vector2> points, Color polygonColor)
        {
            if (points.Count < 3)
                return;

            int startIndex = vh.currentVertCount;
            for (int i = 0; i < points.Count; i++)
            {
                UIVertex vertex = UIVertex.simpleVert;
                vertex.color = polygonColor;
                vertex.position = points[i];
                vh.AddVert(vertex);
            }

            for (int i = 1; i < points.Count - 1; i++)
                vh.AddTriangle(startIndex, startIndex + i, startIndex + i + 1);
        }
    }
}
