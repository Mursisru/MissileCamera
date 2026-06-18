using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    internal static class MissileCameraFeedLayout
    {
        private const string RotatedViewName = "MissileCameraRotatedView";

        internal static RectTransform EnsureRotatedView(RectTransform layoutRt, float contentRotationZ)
        {
            Transform? existing = layoutRt.Find(RotatedViewName);
            RectTransform viewRt;
            if (existing == null)
            {
                var go = new GameObject(RotatedViewName, typeof(RectTransform));
                go.transform.SetParent(layoutRt, false);
                go.transform.SetAsFirstSibling();
                viewRt = go.GetComponent<RectTransform>();
            }
            else
            {
                viewRt = (RectTransform)existing;
            }

            ApplyRotatedViewTransform(viewRt, layoutRt, contentRotationZ);
            ReparentRotatedContent(layoutRt, viewRt);
            return viewRt;
        }

        internal static RectTransform ResolveProjectionRect(RectTransform layoutRt)
        {
            Transform? view = layoutRt.Find(RotatedViewName);
            if (view != null)
                return (RectTransform)view;

            Transform? feed = layoutRt.Find("MissileCameraFeed");
            return feed != null ? (RectTransform)feed : layoutRt;
        }

        internal static void Apply(RectTransform layoutRt, bool portraitColumnLayout, float contentRotationZ)
        {
            RectTransform viewRt = EnsureRotatedView(layoutRt, contentRotationZ);

            Transform? feed = viewRt.Find("MissileCameraFeed");
            if (feed != null && feed.TryGetComponent(out RectTransform feedRt))
            {
                Stretch(feedRt);
                feedRt.localEulerAngles = Vector3.zero;
                feedRt.SetAsFirstSibling();
            }

            Transform? hud = viewRt.Find("MissileCameraHudOverlay");
            if (hud != null)
            {
                if (hud.TryGetComponent(out RectTransform hudRt))
                {
                    Stretch(hudRt);
                    hudRt.localEulerAngles = Vector3.zero;
                }

                hud.SetAsLastSibling();
            }

            Transform? color = layoutRt.Find("MissileCameraColor");
            if (!MissileCameraHudConfig.Enabled
                && color != null
                && color.TryGetComponent(out RectTransform colorRt))
                ApplyColorBadge(colorRt, portraitColumnLayout);

            ApplyDrawOrder(layoutRt, viewRt);
        }

        private static void ApplyDrawOrder(RectTransform layoutRt, RectTransform viewRt)
        {
            if (MissileCameraHudConfig.Enabled)
            {
                RectTransform? panelRt = FindMissileCameraPanel(layoutRt);
                if (panelRt != null)
                    MissileCameraHudOverlay.ApplyLegacyStubVisibility(panelRt, hide: true);

                viewRt.SetAsLastSibling();
                return;
            }

            BringChildToFront(layoutRt, "MissileCameraHudOverlay");
            BringChildToFront(layoutRt, "MissileCameraTitle");
            BringChildToFront(layoutRt, "MissileTelemetry");
            BringChildToFront(layoutRt, "MissileCameraColor");
        }

        internal static void ApplyContentRotation(RectTransform layoutRt, float contentRotationZ)
        {
            Transform? view = layoutRt.Find(RotatedViewName);
            if (view != null && view.TryGetComponent(out RectTransform viewRt))
                ApplyRotatedViewTransform(viewRt, layoutRt, contentRotationZ);
            else
                EnsureRotatedView(layoutRt, contentRotationZ);
        }

        private static void ReparentRotatedContent(RectTransform layoutRt, RectTransform viewRt)
        {
            RectTransform? panelRt = FindMissileCameraPanel(layoutRt);
            foreach (string childName in new[] { "MissileCameraFeed", "MissileCameraHudOverlay" })
            {
                Transform? child = FindFeedChild(layoutRt, panelRt, childName);
                if (child == null || child.parent == viewRt)
                    continue;

                child.SetParent(viewRt, false);
            }
        }

        private static Transform? FindFeedChild(RectTransform layoutRt, RectTransform? panelRt, string childName)
        {
            Transform? child = layoutRt.Find(childName);
            if (child != null)
                return child;

            if (panelRt == null || panelRt == layoutRt)
                return null;

            return panelRt.Find(childName);
        }

        private static void ApplyRotatedViewTransform(RectTransform viewRt, RectTransform layoutRt, float contentRotationZ)
        {
            ResolveDrawableSize(layoutRt, out float pw, out float ph);

            if (Mathf.Abs(contentRotationZ) < 0.5f)
            {
                viewRt.localEulerAngles = Vector3.zero;
                Stretch(viewRt);
                return;
            }

            viewRt.pivot = new Vector2(0.5f, 0.5f);
            viewRt.anchorMin = new Vector2(0.5f, 0.5f);
            viewRt.anchorMax = new Vector2(0.5f, 0.5f);
            viewRt.anchoredPosition = Vector2.zero;
            viewRt.sizeDelta = IsPerpendicularRotation(contentRotationZ)
                ? new Vector2(ph, pw)
                : new Vector2(pw, ph);
            viewRt.localEulerAngles = new Vector3(0f, 0f, contentRotationZ);
        }

        private static void ResolveDrawableSize(RectTransform layoutRt, out float width, out float height)
        {
            float lw = Mathf.Abs(layoutRt.rect.width);
            float lh = Mathf.Abs(layoutRt.rect.height);

            RectTransform? panelRt = FindMissileCameraPanel(layoutRt);
            if (panelRt == null)
            {
                width = Mathf.Max(lw, 1f);
                height = Mathf.Max(lh, 1f);
                return;
            }

            float panelW = Mathf.Abs(panelRt.rect.width);
            float panelH = Mathf.Abs(panelRt.rect.height);

            // Full-band content root can report an unresolved rect before layout, or stay undersized.
            bool partialBand = layoutRt != panelRt && lh < panelH * 0.85f;
            if (partialBand)
            {
                width = Mathf.Max(lw, 1f);
                height = Mathf.Max(lh, 1f);
                return;
            }

            bool undersized = lw < panelW * 0.55f || lh < panelH * 0.55f;
            if (undersized)
            {
                width = Mathf.Max(panelW, 1f);
                height = Mathf.Max(panelH, 1f);
                return;
            }

            width = Mathf.Max(lw, 1f);
            height = Mathf.Max(lh, 1f);
        }

        private static RectTransform? FindMissileCameraPanel(RectTransform layoutRt)
        {
            Transform? node = layoutRt;
            while (node != null)
            {
                if (node.name == "MissileCameraPanel" && node.TryGetComponent(out RectTransform panelRt))
                    return panelRt;

                node = node.parent;
            }

            return null;
        }

        private static bool IsPerpendicularRotation(float rotationZ)
        {
            float delta = Mathf.Abs(Mathf.DeltaAngle(0f, rotationZ));
            return Mathf.Abs(delta - 90f) < 5f;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void BringChildToFront(RectTransform layoutRt, string childName)
        {
            Transform? child = layoutRt.Find(childName);
            child?.SetAsLastSibling();
        }

        private static void ApplyColorBadge(RectTransform colorRt, bool portraitColumnLayout)
        {
            colorRt.pivot = new Vector2(0.5f, 0.5f);
            colorRt.anchoredPosition = Vector2.zero;

            if (portraitColumnLayout)
            {
                colorRt.anchorMin = new Vector2(0.70f, 0.84f);
                colorRt.anchorMax = new Vector2(0.96f, 0.96f);
            }
            else
            {
                colorRt.anchorMin = new Vector2(0.70f, 0.80f);
                colorRt.anchorMax = new Vector2(0.97f, 0.93f);
            }

            colorRt.offsetMin = Vector2.zero;
            colorRt.offsetMax = Vector2.zero;

            if (colorRt.TryGetComponent(out Text colorText))
            {
                colorText.alignment = TextAnchor.MiddleCenter;
                colorText.fontStyle = FontStyle.Bold;
            }
        }
    }
}
