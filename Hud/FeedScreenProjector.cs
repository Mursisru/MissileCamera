using UnityEngine;

namespace MissileCamera
{
    internal readonly struct FeedProjection
    {
        internal readonly bool Valid;
        internal readonly bool InFront;
        internal readonly Vector2 AnchoredPosition;

        internal FeedProjection(bool valid, bool inFront, Vector2 anchoredPosition)
        {
            Valid = valid;
            InFront = inFront;
            AnchoredPosition = anchoredPosition;
        }

        internal static FeedProjection Invalid => new FeedProjection(false, false, Vector2.zero);
    }

    internal static class FeedScreenProjector
    {
        internal static FeedProjection Project(Camera cam, RectTransform feedRect, Vector3 worldPoint)
        {
            if (cam == null || feedRect == null)
                return FeedProjection.Invalid;

            Vector3 viewport = cam.WorldToViewportPoint(worldPoint);
            if (viewport.z <= 0f)
                return new FeedProjection(false, false, Vector2.zero);

            Vector2 size = feedRect.rect.size;
            if (size.x < 1f || size.y < 1f)
                return FeedProjection.Invalid;

            Vector2 anchored = new Vector2(
                (viewport.x - 0.5f) * size.x,
                (viewport.y - 0.5f) * size.y);

            return new FeedProjection(true, true, anchored);
        }

        internal static FeedProjection Project(Camera cam, RectTransform feedRect, GlobalPosition globalPoint) =>
            Project(cam, feedRect, globalPoint.ToLocalPosition());
    }
}
