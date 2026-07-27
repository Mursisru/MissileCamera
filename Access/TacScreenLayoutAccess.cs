using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    internal readonly struct PanelRectState
    {
        internal PanelRectState(Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            AnchorMin = anchorMin;
            AnchorMax = anchorMax;
            OffsetMin = offsetMin;
            OffsetMax = offsetMax;
        }

        internal Vector2 AnchorMin { get; }
        internal Vector2 AnchorMax { get; }
        internal Vector2 OffsetMin { get; }
        internal Vector2 OffsetMax { get; }
    }

    internal readonly struct RightColumnBounds
    {
        internal RightColumnBounds(float minX, float maxX)
        {
            MinX = minX;
            MaxX = maxX;
        }

        internal float MinX { get; }
        internal float MaxX { get; }
    }

    internal static class PanelRectNormalizer
    {
        internal static PanelRectState CaptureOnCanvas(RectTransform panel, Canvas canvas)
        {
            RectTransform canvasRt = canvas.GetComponent<RectTransform>();
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRt, panel);
            Rect cr = canvasRt.rect;

            if (cr.width <= 1f || cr.height <= 1f)
                return new PanelRectState(panel.anchorMin, panel.anchorMax, panel.offsetMin, panel.offsetMax);

            float minX = (bounds.min.x - cr.xMin) / cr.width;
            float maxX = (bounds.max.x - cr.xMin) / cr.width;
            float minY = (bounds.min.y - cr.yMin) / cr.height;
            float maxY = (bounds.max.y - cr.yMin) / cr.height;

            minX = Mathf.Clamp01(minX);
            maxX = Mathf.Clamp01(maxX);
            minY = Mathf.Clamp01(minY);
            maxY = Mathf.Clamp01(maxY);

            if (maxX - minX < 0.01f || maxY - minY < 0.01f)
                return new PanelRectState(panel.anchorMin, panel.anchorMax, panel.offsetMin, panel.offsetMax);

            return new PanelRectState(
                new Vector2(minX, minY),
                new Vector2(maxX, maxY),
                Vector2.zero,
                Vector2.zero);
        }

        internal static bool IsTopRightZone(PanelRectState rect, float minX = 0.38f, float minY = 0.25f)
        {
            float w = rect.AnchorMax.x - rect.AnchorMin.x;
            float h = rect.AnchorMax.y - rect.AnchorMin.y;
            return rect.AnchorMin.x >= minX
                && rect.AnchorMax.x > rect.AnchorMin.x + 0.08f
                && rect.AnchorMin.y >= minY
                && w >= 0.12f
                && h >= 0.12f
                && rect.AnchorMax.x <= 1.01f;
        }

        /// <summary>Separate left MFD: weapon bay canvas fills the display.</summary>
        internal static bool IsDarkreachFullMfdZone(PanelRectState rect)
        {
            float w = rect.AnchorMax.x - rect.AnchorMin.x;
            float h = rect.AnchorMax.y - rect.AnchorMin.y;
            return w >= 0.65f
                && h >= 0.50f
                && rect.AnchorMin.x <= 0.10f
                && rect.AnchorMax.x >= 0.85f;
        }

        /// <summary>SFB-81 Darkreach: far-left Bay Armed bezel on shared cockpit canvas.</summary>
        internal static bool IsDarkreachLeftBayZone(PanelRectState rect)
        {
            float w = rect.AnchorMax.x - rect.AnchorMin.x;
            float h = rect.AnchorMax.y - rect.AnchorMin.y;
            return rect.AnchorMax.x <= 0.25f
                && rect.AnchorMin.x <= 0.15f
                && w >= 0.08f
                && h >= 0.50f
                && rect.AnchorMax.y <= 1.01f;
        }

        /// <summary>Loose filter for collecting left-bay UI nodes before hide-root selection.</summary>
        internal static bool IsDarkreachLeftBayCandidate(PanelRectState rect) =>
            rect.AnchorMin.x < 0.20f && rect.AnchorMax.x <= 0.25f;

        /// <summary>Darkreach Bay Armed section (structural — no left-x assumption; bays logged at x~0.86).</summary>
        internal static bool IsDarkreachBaySectionZone(PanelRectState rect)
        {
            float w = rect.AnchorMax.x - rect.AnchorMin.x;
            float h = rect.AnchorMax.y - rect.AnchorMin.y;
            return w >= 0.06f
                && h >= 0.35f
                && rect.AnchorMax.y <= 1.01f;
        }

        /// <summary>SFB-81 Darkreach: weaponPanel + bay strip on left MFD (logged: 0.21-0.50 x 0-1).</summary>
        internal static bool IsDarkreachWeaponPanelZone(PanelRectState rect)
        {
            float w = rect.AnchorMax.x - rect.AnchorMin.x;
            float h = rect.AnchorMax.y - rect.AnchorMin.y;
            return rect.AnchorMax.x <= 0.56f
                && rect.AnchorMin.x >= 0.08f
                && w >= 0.20f
                && h >= 0.55f
                && rect.AnchorMax.y <= 1.01f;
        }

        /// <summary>SFB-81 Darkreach: Weapon Armed left half on right tac MFD.</summary>
        internal static bool IsBomberLeftHalfZone(PanelRectState rect)
        {
            float w = rect.AnchorMax.x - rect.AnchorMin.x;
            float h = rect.AnchorMax.y - rect.AnchorMin.y;
            return rect.AnchorMax.x <= 0.54f
                && rect.AnchorMin.x <= 0.20f
                && rect.AnchorMin.y >= 0.05f
                && w >= 0.15f
                && w <= 0.55f
                && h >= 0.40f
                && h <= 0.95f;
        }

        /// <summary>AB-4 Alkyon: full-height right column on 3-col tac MFD.</summary>
        internal static bool IsBomberRightColumnZone(PanelRectState rect)
        {
            float w = rect.AnchorMax.x - rect.AnchorMin.x;
            float h = rect.AnchorMax.y - rect.AnchorMin.y;
            return rect.AnchorMin.x >= 0.50f
                && rect.AnchorMax.x <= 1.01f
                && rect.AnchorMin.y >= 0.05f
                && w >= 0.15f
                && w <= 0.45f
                && h >= 0.40f
                && h <= 0.95f;
        }

        /// <summary>Compass / Brawler: engine gauge block under right-column silhouette.</summary>
        internal static bool IsCompassEngineZone(PanelRectState rect)
        {
            float w = rect.AnchorMax.x - rect.AnchorMin.x;
            float h = rect.AnchorMax.y - rect.AnchorMin.y;
            if (w < 0.06f || h < 0.08f)
                return false;

            return rect.AnchorMin.x >= 0.50f
                && rect.AnchorMax.x <= 1.01f
                && rect.AnchorMin.y >= 0.25f
                && rect.AnchorMax.y <= 0.82f
                && h <= 0.55f;
        }

        /// <summary>
        /// VT-7 Vagrant: stacked NOZZLE + ENGINE on right MFD column under weapons silhouette.
        /// Accept discovery-sized unions; FitVagrantOverlayZone only soft-clamps bleed.
        /// </summary>
        internal static bool IsVagrantNozzleEngineZone(PanelRectState rect)
        {
            float w = rect.AnchorMax.x - rect.AnchorMin.x;
            float h = rect.AnchorMax.y - rect.AnchorMin.y;
            if (w < 0.12f || h < 0.15f)
                return false;

            return rect.AnchorMin.x >= 0.55f
                && rect.AnchorMax.x <= 1.01f
                && rect.AnchorMin.y >= 0.18f
                && rect.AnchorMax.y <= 0.83f
                && h <= 0.60f
                && w <= 0.48f;
        }

        /// <summary>CI-22 Cricket: EngPanel on shared canvas (maps to bottom-left Engines MFD).</summary>
        internal static bool IsCricketEngineZone(PanelRectState rect)
        {
            float w = rect.AnchorMax.x - rect.AnchorMin.x;
            float h = rect.AnchorMax.y - rect.AnchorMin.y;
            if (w < 0.08f || h < 0.08f)
                return false;

            return rect.AnchorMin.x >= 0.70f
                && rect.AnchorMax.x <= 1.01f
                && rect.AnchorMin.y >= 0.28f
                && rect.AnchorMax.y <= 0.80f
                && h <= 0.50f
                && w <= 0.35f;
        }

        /// <summary>SAH-46 Chicane: L/R TURBINE blocks on right column (TAIL DUCT excluded).</summary>
        internal static bool IsChicaneEngineZone(PanelRectState rect)
        {
            float w = rect.AnchorMax.x - rect.AnchorMin.x;
            float h = rect.AnchorMax.y - rect.AnchorMin.y;
            if (w < 0.06f || h < 0.10f)
                return false;

            return rect.AnchorMin.x >= 0.40f
                && rect.AnchorMax.x <= 1.01f
                && rect.AnchorMin.y >= 0.30f
                && h <= 0.55f
                && w <= 0.45f;
        }

        /// <summary>UH-90 Ibis: Weapon Armed strip on left MFD bezel (canvas Y = screen horizontal).</summary>
        internal static bool IsIbisWeaponArmedZone(PanelRectState rect)
        {
            float w = rect.AnchorMax.x - rect.AnchorMin.x;
            float h = rect.AnchorMax.y - rect.AnchorMin.y;
            if (w < 0.06f || h < 0.08f)
                return false;

            return rect.AnchorMin.x >= 0.50f
                && rect.AnchorMax.x <= 0.78f
                && rect.AnchorMin.y >= 0.01f
                && rect.AnchorMax.y <= 0.48f
                && h >= 0.28f
                && w <= 0.22f;
        }

        /// <summary>VL-49 Tarantula: central Weapon Armed block (GUN / pylons / cargo).</summary>
        internal static bool IsTarantulaWeaponsZone(PanelRectState rect)
        {
            float w = rect.AnchorMax.x - rect.AnchorMin.x;
            float h = rect.AnchorMax.y - rect.AnchorMin.y;
            if (w < 0.12f || h < 0.10f)
                return false;

            return rect.AnchorMin.x >= 0.10f
                && rect.AnchorMax.x <= 0.98f
                && rect.AnchorMin.y >= 0.20f
                && rect.AnchorMax.y <= 0.78f
                && h <= 0.55f
                && w <= 0.85f;
        }

        /// <summary>EW-25 Medusa: upper Weapon Armed block (RADOME/LASER + hardpoint row), above FUEL/HEAT/THROTTLE.</summary>
        internal static bool IsMedusaWeaponsZone(PanelRectState rect)
        {
            float w = rect.AnchorMax.x - rect.AnchorMin.x;
            float h = rect.AnchorMax.y - rect.AnchorMin.y;
            if (w < 0.08f || h < 0.08f)
                return false;

            return rect.AnchorMin.x >= 0.38f
                && rect.AnchorMax.x <= 1.01f
                && rect.AnchorMin.y >= 0.28f
                && h <= 0.62f
                && w <= 0.58f;
        }

        /// <summary>Canvas rect of discovered WEAPON ARMED panel — Revoker (top-right) or Ifrit (right column strip).</summary>
        internal static bool IsWeaponsReplacementZone(PanelRectState rect)
        {
            float w = rect.AnchorMax.x - rect.AnchorMin.x;
            float h = rect.AnchorMax.y - rect.AnchorMin.y;
            if (w < 0.06f || h < 0.06f)
                return false;

            if (IsIbisWeaponArmedZone(rect))
                return true;

            if (rect.AnchorMax.x < 0.45f)
                return false;

            if (IsTopRightZone(rect))
                return true;

            if (IsMedusaWeaponsZone(rect))
                return true;

            if (IsTarantulaWeaponsZone(rect))
                return true;

            if (IsCricketEngineZone(rect))
                return true;

            if (IsChicaneEngineZone(rect))
                return true;

            if (IsCompassEngineZone(rect))
                return true;

            if (IsVagrantNozzleEngineZone(rect))
                return true;

            if (IsBomberRightColumnZone(rect))
                return true;

            if (IsBomberLeftHalfZone(rect))
                return true;

            if (IsDarkreachFullMfdZone(rect))
                return true;

            if (IsDarkreachWeaponPanelZone(rect))
                return true;

            if (IsDarkreachLeftBayZone(rect))
                return true;

            if (w > 0.55f || h > 0.55f)
                return false;

            if (rect.AnchorMin.x < 0.48f || rect.AnchorMax.x > 0.80f)
                return false;

            return true;
        }

        internal static PanelRectState UnionOnCanvas(Canvas canvas, IReadOnlyList<RectTransform> nodes)
        {
            RectTransform canvasRt = canvas.GetComponent<RectTransform>();
            Rect cr = canvasRt.rect;
            bool any = false;
            Vector3 min = Vector3.zero;
            Vector3 max = Vector3.zero;

            foreach (RectTransform node in nodes)
            {
                if (node == null)
                    continue;

                Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRt, node);
                if (!any)
                {
                    min = bounds.min;
                    max = bounds.max;
                    any = true;
                    continue;
                }

                min = Vector3.Min(min, bounds.min);
                max = Vector3.Max(max, bounds.max);
            }

            if (!any || cr.width <= 1f || cr.height <= 1f)
                return new PanelRectState(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            float minX = Mathf.Clamp01((min.x - cr.xMin) / cr.width);
            float maxX = Mathf.Clamp01((max.x - cr.xMin) / cr.width);
            float minY = Mathf.Clamp01((min.y - cr.yMin) / cr.height);
            float maxY = Mathf.Clamp01((max.y - cr.yMin) / cr.height);

            if (maxX - minX < 0.01f || maxY - minY < 0.01f)
                return new PanelRectState(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            return new PanelRectState(
                new Vector2(minX, minY),
                new Vector2(maxX, maxY),
                Vector2.zero,
                Vector2.zero);
        }

        /// <summary>Map a canvas-normalized rect into parent-local 0..1 anchors (EW-25 WeaponPanel overlay).</summary>
        internal static PanelRectState CanvasZoneToParentZone(
            PanelRectState zoneOnCanvas,
            RectTransform parent,
            Canvas canvas)
        {
            PanelRectState parentOnCanvas = CaptureOnCanvas(parent, canvas);
            float pw = parentOnCanvas.AnchorMax.x - parentOnCanvas.AnchorMin.x;
            float ph = parentOnCanvas.AnchorMax.y - parentOnCanvas.AnchorMin.y;
            if (pw < 0.01f || ph < 0.01f)
                return zoneOnCanvas;

            float minX = (zoneOnCanvas.AnchorMin.x - parentOnCanvas.AnchorMin.x) / pw;
            float maxX = (zoneOnCanvas.AnchorMax.x - parentOnCanvas.AnchorMin.x) / pw;
            float minY = (zoneOnCanvas.AnchorMin.y - parentOnCanvas.AnchorMin.y) / ph;
            float maxY = (zoneOnCanvas.AnchorMax.y - parentOnCanvas.AnchorMin.y) / ph;

            return new PanelRectState(
                new Vector2(Mathf.Clamp01(minX), Mathf.Clamp01(minY)),
                new Vector2(Mathf.Clamp01(maxX), Mathf.Clamp01(maxY)),
                Vector2.zero,
                Vector2.zero);
        }
    }
}
