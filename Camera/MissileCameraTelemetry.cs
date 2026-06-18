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

            if (missile.targetID.IsValid
                && UnitRegistry.TryGetUnit(missile.targetID, out Unit target)
                && target != null
                && !target.disabled)
            {
                if (missile.NetworkHQ != null && missile.NetworkHQ.TryGetKnownPosition(target, out GlobalPosition knownPos))
                    return UnitConverter.DistanceReading(FastMath.Distance(missilePos, knownPos));

                return UnitConverter.DistanceReading(FastMath.Distance(missilePos, target.GlobalPosition()));
            }

            return "---";
        }

        private static string FormatLegacyLine(Missile missile) =>
            $"{FormatLabeledRow("A", FormatAltitude(missile))} / {FormatLabeledRow("R", FormatRange(missile))} / {FormatLabeledRow("S", FormatSpeed(missile))}";
    }
}
