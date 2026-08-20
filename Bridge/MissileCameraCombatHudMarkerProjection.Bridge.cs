using UnityEngine;

namespace MissileCamera
{
    // Partial-class extension of Fullscreen/MissileCameraCombatHudMarkerProjection.cs — the
    // external-consumer half of marker projection lives here instead, so that file stays
    // Mursisru's own vanilla-HUD-reprojection code with nothing added inline. Shares MarkersField,
    // HiddenField and TryResolveWorld with the other half via the partial class, no visibility
    // changes needed on either side.
    internal static partial class MissileCameraCombatHudMarkerProjection
    {
        /// <summary>Bridge/McBridge.cs MarkersJson — same marker list/eligibility as
        /// ReprojectIfFullscreen (skip hidden, skip friendly-missile ghosts, skip anything
        /// behind the camera), but for an external consumer: viewport-space (0..1, Unity convention
        /// — y=0 bottom) instead of screen pixels, and clipped to the visible 0..1 range rather than
        /// edge-pinned — no off-screen arrow behavior here, that's a Fullscreen-specific affordance
        /// this doesn't try to replicate (yet). Reads marker.image.color as-is (vanilla faction
        /// theme) rather than re-deriving faction ourselves, so colors match the in-game HUD exactly.</summary>
        internal static string BuildMarkersJson(Camera? feed)
        {
            if (feed == null)
                return "[]";

            try
            {
                CombatHUD? hud = SceneSingleton<CombatHUD>.i;
                if (hud == null || MarkersField == null)
                    return "[]";

                if (MarkersField.GetValue(hud) is not System.Collections.Generic.List<HUDUnitMarker> markers)
                    return "[]";

                var sb = new System.Text.StringBuilder("[");
                bool first = true;
                for (int i = 0; i < markers.Count; i++)
                {
                    HUDUnitMarker? marker = markers[i];
                    if (marker == null || marker.unit == null || marker.unit is Missile)
                        continue;
                    if (HiddenField != null && HiddenField.GetValue(marker) is true)
                        continue;
                    if (!TryResolveWorld(marker, out Vector3 world))
                        continue;

                    Vector3 vp = feed.WorldToViewportPoint(world);
                    if (vp.z <= 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f)
                        continue;   // behind camera or off-frame — no edge-pinning in this pass

                    string name;
                    try { name = marker.unit.name ?? string.Empty; }
                    catch { name = string.Empty; }

                    string colorHex = "#ffffff";
                    try
                    {
                        if (marker.image != null)
                        {
                            Color32 c = marker.image.color;
                            colorHex = "#" + c.r.ToString("x2") + c.g.ToString("x2") + c.b.ToString("x2");
                        }
                    }
                    catch { /* keep default white */ }

                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "{{\"n\":\"{0}\",\"x\":{1:0.000},\"y\":{2:0.000},\"sel\":{3},\"c\":\"{4}\"}}",
                        EscapeJson(name), vp.x, vp.y, marker.selected ? "true" : "false", colorHex));
                }
                sb.Append(']');
                return sb.ToString();
            }
            catch
            {
                return "[]";   // marker failures must never block the caller (same principle as ReprojectIfFullscreen)
            }
        }

        /// <summary>Shared with Bridge/McBridge.cs's TelemetryJson — one JSON-string-escape helper
        /// for both bridge JSON producers rather than two copies.</summary>
        internal static string EscapeJson(string? s) =>
            string.IsNullOrEmpty(s) ? string.Empty : s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
