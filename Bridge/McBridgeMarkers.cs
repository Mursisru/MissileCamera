using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace MissileCamera.Bridge
{
    /// <summary>
    /// Read-only sibling of MissileCameraCombatHudMarkerProjection.cs (Fullscreen/) — same source
    /// data (CombatHUD.markers) and same feed-camera reprojection math, but for an external
    /// consumer (Bridge/McBridge.cs MarkersJson) instead of the in-game Canvas. Never touches
    /// marker.image, marker.selected, CombatHUD, or CameraStateManager — this only reads.
    /// </summary>
    internal static class McBridgeMarkers
    {
        private static readonly FieldInfo? HiddenField =
            AccessTools.Field(typeof(HUDUnitMarker), "hidden");
        private static readonly FieldInfo? MarkersField =
            AccessTools.Field(typeof(CombatHUD), "markers");

        /// <summary>JSON array of currently-visible (in front of the feed camera AND within its
        /// viewport) CombatHUD markers, each: {"x":0..1,"y":0..1 (viewport, same convention as the
        /// aim reticle — flip Y for CSS top like that), "sel":bool, "col":"#rrggbb" (marker's own
        /// vanilla faction color, unmodified)}. Empty array (not null) when there's no feed camera
        /// or no markers — a missing feed is already reported via HasTrackableMissile/FeedTexture,
        /// this doesn't duplicate that signal.</summary>
        internal static string Build()
        {
            Camera? feed = MissileCameraFeedController.TryGetFeedCamera();
            if (feed == null || MarkersField == null)
                return "[]";

            if (MarkersField.GetValue(SceneSingletonAccess.CombatHud()) is not List<HUDUnitMarker> markers
                || markers.Count == 0)
                return "[]";

            var sb = new StringBuilder("[");
            bool first = true;
            for (int i = 0; i < markers.Count; i++)
            {
                HUDUnitMarker? marker = markers[i];
                if (marker == null || marker.image == null || marker.unit == null)
                    continue;
                if (marker.unit is Missile)
                    continue;   // matches vanilla FS reprojection: skip friendly missile ghosts
                if (HiddenField != null && HiddenField.GetValue(marker) is true)
                    continue;
                if (!TryResolveWorld(marker, out Vector3 world))
                    continue;

                Vector3 vp = feed.WorldToViewportPoint(world);
                if (vp.z <= 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f)
                    continue;   // off-screen — no edge-pinned arrow here, just what's actually in frame

                Color32 c = marker.image.color;
                if (!first) sb.Append(',');
                first = false;
                sb.Append("{\"x\":").Append(vp.x.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture))
                  .Append(",\"y\":").Append(vp.y.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture))
                  .Append(",\"sel\":").Append(marker.selected ? "true" : "false")
                  .Append(",\"col\":\"#").Append(c.r.ToString("X2")).Append(c.g.ToString("X2")).Append(c.b.ToString("X2"))
                  .Append("\"}");
            }
            return sb.Append(']').ToString();
        }

        private static bool TryResolveWorld(HUDUnitMarker marker, out Vector3 world)
        {
            world = default;
            GlobalPosition global = marker.unit.GlobalPosition();
            if (marker.outdated)
            {
                FactionHQ? hq = null;
                try { hq = SceneSingleton<DynamicMap>.i?.HQ; }
                catch { /* ignore */ }

                if (hq == null || !hq.TryGetKnownPosition(marker.unit, out global))
                    return false;
            }

            world = global.ToLocalPosition();
            return true;
        }
    }

    // CombatHUD's own singleton accessor is what MissileCameraCombatHudMarkerProjection.cs uses
    // (SceneSingleton<CombatHUD>.i) — isolated here as its own one-liner only so McBridgeMarkers
    // above reads like the rest of this file (Build() as the single public entry point).
    internal static class SceneSingletonAccess
    {
        internal static CombatHUD? CombatHud() => SceneSingleton<CombatHUD>.i;
    }
}
