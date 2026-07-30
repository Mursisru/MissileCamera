using MissileCamera.Config;
using UnityEngine;

namespace MissileCamera
{
    internal static class MissileCameraMarkersConfig
    {
        internal static int MaxMarkers = 48;
        internal static bool ShowTarget = true;
        internal static bool ShowAim = true;
        internal static bool ShowSceneUnits = true;
        internal static float SceneUnitAlpha = 0.4f;
        internal static bool ShowThreat;
        internal static bool ShowAlly;
        internal static bool ShowWaypoint;
        internal static bool ShowJam;
        internal static Color TargetColor = new Color(0.35f, 0.95f, 1f, 1f);
        internal static Color AimColor = new Color(1f, 0.75f, 0.12f, 1f);
        internal static Color ThreatColor = new Color(1f, 0.22f, 0.18f, 1f);
        internal static Color AllyColor = new Color(0.35f, 1f, 0.45f, 1f);
        internal static Color WaypointColor = new Color(0.95f, 0.55f, 1f, 1f);
        internal static Color JamColor = new Color(1f, 0.45f, 0.05f, 1f);
        internal static int Revision;

        internal static void Refresh(bool force = false)
        {
            if (!MissileCameraBepInConfig.IsBound)
                return;

            int maxMarkers = MissileCameraBepInConfig.MarkersMax.Value;
            bool showTarget = MissileCameraBepInConfig.MarkersShowTarget.Value;
            bool showAim = MissileCameraBepInConfig.MarkersShowAim.Value;
            bool showSceneUnits = MissileCameraBepInConfig.MarkersShowSceneUnits.Value;
            float sceneUnitAlpha = MissileCameraBepInConfig.MarkersSceneUnitAlpha.Value;
            bool showThreat = MissileCameraBepInConfig.MarkersShowThreat.Value;
            bool showAlly = MissileCameraBepInConfig.MarkersShowAlly.Value;
            bool showWaypoint = MissileCameraBepInConfig.MarkersShowWaypoint.Value;
            bool showJam = MissileCameraBepInConfig.MarkersShowJam.Value;
            Color targetColor = Parse(MissileCameraBepInConfig.MarkersTargetColor.Value, TargetColor);
            Color aimColor = Parse(MissileCameraBepInConfig.MarkersAimColor.Value, AimColor);
            Color threatColor = Parse(MissileCameraBepInConfig.MarkersThreatColor.Value, ThreatColor);
            Color allyColor = Parse(MissileCameraBepInConfig.MarkersAllyColor.Value, AllyColor);
            Color waypointColor = Parse(MissileCameraBepInConfig.MarkersWaypointColor.Value, WaypointColor);
            Color jamColor = Parse(MissileCameraBepInConfig.MarkersJamColor.Value, JamColor);

            if (!force
                && maxMarkers == MaxMarkers
                && showTarget == ShowTarget
                && showAim == ShowAim
                && showSceneUnits == ShowSceneUnits
                && Mathf.Approximately(sceneUnitAlpha, SceneUnitAlpha)
                && showThreat == ShowThreat
                && showAlly == ShowAlly
                && showWaypoint == ShowWaypoint
                && showJam == ShowJam
                && targetColor == TargetColor
                && aimColor == AimColor
                && threatColor == ThreatColor
                && allyColor == AllyColor
                && waypointColor == WaypointColor
                && jamColor == JamColor)
                return;

            MaxMarkers = Mathf.Clamp(maxMarkers, 1, 64);
            ShowTarget = showTarget;
            ShowAim = showAim;
            ShowSceneUnits = showSceneUnits;
            SceneUnitAlpha = Mathf.Clamp(sceneUnitAlpha, 0.1f, 1f);
            ShowThreat = showThreat;
            ShowAlly = showAlly;
            ShowWaypoint = showWaypoint;
            ShowJam = showJam;
            TargetColor = targetColor;
            AimColor = aimColor;
            ThreatColor = threatColor;
            AllyColor = allyColor;
            WaypointColor = waypointColor;
            JamColor = jamColor;
            Revision++;
        }

        private static Color Parse(string raw, Color fallback)
        {
            if (string.IsNullOrEmpty(raw))
                return fallback;

            string[] parts = raw.Split(',');
            if (parts.Length < 3)
                return fallback;

            if (!float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float r)
                || !float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float g)
                || !float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float b))
                return fallback;

            float a = 1f;
            if (parts.Length >= 4)
                float.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out a);

            return new Color(r, g, b, a);
        }
    }
}
