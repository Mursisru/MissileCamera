using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    internal static class MissileCameraTelemetry
    {
        private const float UpdateInterval = 0.1f;
        private static float _nextUpdateTime;

        internal static void Update(Text? label, Missile? missile)
        {
            if (label == null)
                return;

            if (MissileCameraHudConfig.Enabled)
            {
                label.text = string.Empty;
                return;
            }

            if (missile == null || missile.disabled || missile.rb == null)
            {
                label.text = FormatIdleTelemetryLine();
                return;
            }

            float now = Time.unscaledTime;
            if (now < _nextUpdateTime)
                return;

            _nextUpdateTime = now + UpdateInterval;
            label.text = FormatLegacyLine(missile);
        }

        internal static void ResetThrottle() => _nextUpdateTime = 0f;

        internal static string FormatLabeledRow(string label, string value) =>
            $"{label}:{(string.IsNullOrEmpty(value) ? "---" : value)}";

        internal static string FormatIdleTelemetryLine() => "A:--- / R:--- / S:---";

        internal static string FormatSpeed(Missile missile)
        {
            float speed = missile.rb != null ? missile.rb.velocity.magnitude : missile.speed;
            return UnitConverter.SpeedReading(speed);
        }

        internal static string FormatAltitude(Missile missile) =>
            UnitConverter.DistanceReading(missile.transform.GlobalPosition().y);

        internal static string FormatRange(Missile missile)
        {
            GlobalPosition missilePos = missile.transform.GlobalPosition();

            if (MissileAccess.TryGetAimPoint(missile, out GlobalPosition aimPoint))
                return UnitConverter.DistanceReading(FastMath.Distance(missilePos, aimPoint));

            if (MissileAccess.TryGetTarget(missile, out Unit? target) && target != null)
                return UnitConverter.DistanceReading(FastMath.Distance(missilePos, target.GlobalPosition()));

            return "---";
        }

        internal static string FormatTgpRng(Missile missile) =>
            "RNG " + FormatRange(missile);

        internal static string FormatTgpAlt(float altitudeMeters) =>
            "ALT " + UnitConverter.AltitudeReading(altitudeMeters);

        internal static string FormatTgpSpd(float speedMs) =>
            "SPD " + UnitConverter.SpeedReading(speedMs);

        internal static string FormatTgpHdg(float headingDeg) =>
            string.Format(System.Globalization.CultureInfo.InvariantCulture, "HDG {0:F0}°", headingDeg);

        internal static string FormatTgpRel(float relAltMeters) =>
            "REL " + UnitConverter.AltitudeReading(relAltMeters);

        internal static string FormatTgpClos(float closingMs)
        {
            if (closingMs < 0.5f)
                return "CLOS ---";

            return "CLOS " + UnitConverter.SpeedReading(closingMs);
        }

        internal static string FormatTgpMag(float magnification) =>
            string.Format(System.Globalization.CultureInfo.InvariantCulture, "MAG x{0:F1}", magnification);

        internal static string FormatTgpRid(string rid) =>
            "RID: " + (string.IsNullOrEmpty(rid) ? "---" : rid);

        internal static string FormatTgpMode(bool infraredActive) =>
            infraredActive ? "MODE: AUTO IR" : "MODE: COLOR";

        internal static string FormatTgpPalette(bool infraredActive) =>
            infraredActive ? "PALETTE: WhiteHot" : "PALETTE: ---";

        internal static string FormatTgpTti(float ttiSec) =>
            string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:F1}s to Impact", ttiSec);

        private static string FormatLegacyLine(Missile missile) =>
            $"{FormatLabeledRow("A", FormatAltitude(missile))} / {FormatLabeledRow("R", FormatRange(missile))} / {FormatLabeledRow("S", FormatSpeed(missile))}";
    }
}
