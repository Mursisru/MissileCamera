using UnityEngine;

namespace MissileCamera
{
    /// <summary>
    /// Feed unit-marker defaults (hardcoded — not exposed in slim BepIn cfg).
    /// Opaque + high contrast; Lost keeps full alpha.
    /// </summary>
    internal static class MissileCameraMarkersConfig
    {
        internal static int MaxMarkers = 48;
        internal static bool ShowTarget = true;
        internal static bool ShowAim = true;
        internal static bool ShowSceneUnits = true;
        internal const float SceneUnitAlpha = 1f;
        internal const float LostAlphaScale = 1f;
        internal static bool ShowThreat = true;
        internal static bool ShowAlly = true;
        internal static readonly bool ShowWaypoint = false;
        internal static readonly bool ShowJam = false;
        // High-contrast RGB (a forced to 1 in Show).
        internal static Color TargetColor = new Color(0.2f, 1f, 1f, 1f);
        internal static Color AimColor = new Color(1f, 0.85f, 0.05f, 1f);
        internal static Color ThreatColor = new Color(1f, 0.12f, 0.08f, 1f);
        internal static Color AllyColor = new Color(0.15f, 1f, 0.35f, 1f);
        internal static Color WaypointColor = new Color(0.95f, 0.55f, 1f, 1f);
        internal static Color JamColor = new Color(1f, 0.45f, 0.05f, 1f);
        internal static int Revision;

        internal static void Refresh(bool force = false)
        {
            if (!force)
                return;

            Revision++;
        }
    }
}
