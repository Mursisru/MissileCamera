using System.Reflection;
using UnityEngine;

namespace MissileCamera
{
    internal static class MissileAccess
    {
        private const BindingFlags InstanceAny = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static FieldInfo? _aimPointField;
        private static FieldInfo? _gLimitField;

        internal static void WarmFieldCache()
        {
            ResolveAimPointField();
            ResolveGLimitField();
            MotorAccess.EnsureFields();
            MissileSeekerAccess.EnsureFields();
        }

        internal static string GetMissileName(Missile missile)
        {
            if (missile.definition != null && !string.IsNullOrEmpty(missile.definition.unitName))
                return missile.definition.unitName;

            return missile.name;
        }

        internal static bool TryGetAimPoint(Missile missile, out GlobalPosition aimPoint)
        {
            aimPoint = default;
            if (missile == null)
                return false;

            FieldInfo? field = ResolveAimPointField();
            if (field == null)
                return false;

            object? value = field.GetValue(missile);
            if (value is not GlobalPosition gp)
                return false;

            aimPoint = gp;
            return true;
        }

        internal static bool TryGetTarget(Missile missile, out Unit? target)
        {
            target = null;
            if (missile == null || !missile.targetID.IsValid)
                return false;

            if (!UnitRegistry.TryGetUnit(missile.targetID, out Unit unit) || unit == null || unit.disabled)
                return false;

            target = unit;
            return true;
        }

        internal static string GetTargetName(Missile missile)
        {
            if (!TryGetTarget(missile, out Unit? target) || target == null)
                return "---";

            if (target.definition != null && !string.IsNullOrEmpty(target.definition.unitName))
                return target.definition.unitName;

            return target.name;
        }

        internal static bool TryGetTargetPosition(Missile missile, out GlobalPosition position)
        {
            position = default;
            if (!TryGetTarget(missile, out Unit? target) || target == null)
                return false;

            if (missile.NetworkHQ != null && missile.NetworkHQ.TryGetKnownPosition(target, out GlobalPosition knownPos))
            {
                position = knownPos;
                return true;
            }

            position = target.GlobalPosition();
            return true;
        }

        internal static bool TryGetGLimit(Missile missile, out float gLimit)
        {
            gLimit = 0f;
            if (missile == null)
                return false;

            FieldInfo? field = ResolveGLimitField();
            if (field == null)
                return false;

            object? value = field.GetValue(missile);
            if (value is not float limit)
                return false;

            gLimit = limit;
            return gLimit > 0f;
        }

        internal static bool TryGetInstantG(Missile missile, out float gLoad)
        {
            gLoad = 0f;
            if (missile == null || missile.rb == null)
                return false;

            if (MissileTurnLoad.TrySampleHorizontalTurn(missile, out float lateralG, out _))
            {
                gLoad = Mathf.Abs(lateralG);
                return true;
            }

            gLoad = 0f;
            return true;
        }

        internal static bool TryGetSpeedMs(Missile missile, out float speedMs)
        {
            speedMs = 0f;
            if (missile == null)
                return false;

            if (missile.rb != null)
            {
                speedMs = missile.rb.velocity.magnitude;
                return true;
            }

            speedMs = Mathf.Abs(missile.speed);
            return true;
        }

        internal static bool TryGetMach(Missile missile, out float mach)
        {
            mach = 0f;
            if (!TryGetSpeedMs(missile, out float speedMs))
                return false;

            float alt = missile.transform.GlobalPosition().y;
            float sos;
            try
            {
                sos = LevelInfo.GetSpeedOfSound(alt);
            }
            catch
            {
                return false;
            }

            if (sos <= 0.001f || float.IsNaN(sos) || float.IsInfinity(sos))
                return false;

            mach = speedMs / sos;
            return true;
        }

        internal static bool TryGetFuelFraction(Missile missile, out float fraction) =>
            MotorAccess.TryGetFuelFraction(missile, out fraction);

        internal static MissileGuidanceStatus GetGuidanceStatus(Missile missile) =>
            MissileSeekerAccess.ResolveGuidance(missile);

        /// <summary>Range meters to aim point, else target position.</summary>
        internal static bool TryGetTargetRangeMeters(Missile missile, out float rangeM)
        {
            rangeM = 0f;
            if (missile == null)
                return false;

            GlobalPosition from = missile.transform.GlobalPosition();
            if (TryGetAimPoint(missile, out GlobalPosition aim))
            {
                rangeM = FastMath.Distance(from, aim);
                return true;
            }

            if (TryGetTargetPosition(missile, out GlobalPosition target))
            {
                rangeM = FastMath.Distance(from, target);
                return true;
            }

            return false;
        }

        /// <summary>Off-bore angle degrees from missile forward to aim/target.</summary>
        internal static bool TryGetTargetAngleDeg(Missile missile, out float angleDeg)
        {
            angleDeg = 0f;
            if (missile == null)
                return false;

            GlobalPosition from = missile.transform.GlobalPosition();
            GlobalPosition to;
            if (TryGetAimPoint(missile, out GlobalPosition aim))
                to = aim;
            else if (TryGetTargetPosition(missile, out GlobalPosition target))
                to = target;
            else
                return false;

            Vector3 dir = FastMath.Direction(from, to);
            if (dir.sqrMagnitude < 0.0001f)
                return false;

            angleDeg = Vector3.Angle(missile.transform.forward, dir);
            return true;
        }

        /// <summary>Relative altitude (target − missile) meters along world up.</summary>
        internal static bool TryGetRelativeAltitudeMeters(Missile missile, out float relAltM)
        {
            relAltM = 0f;
            if (missile == null || !TryGetTargetPosition(missile, out GlobalPosition targetPos))
                return false;

            relAltM = targetPos.y - missile.transform.GlobalPosition().y;
            return true;
        }

        /// <summary>Target heading degrees (yaw). Falls back to missile yaw.</summary>
        internal static bool TryGetTargetHeadingDeg(Missile missile, out float headingDeg)
        {
            headingDeg = 0f;
            if (missile == null)
                return false;

            if (TryGetTarget(missile, out Unit? target) && target != null)
            {
                headingDeg = target.transform.eulerAngles.y;
                return true;
            }

            headingDeg = missile.transform.eulerAngles.y;
            return true;
        }

        /// <summary>
        /// Closing speed (m/s) along LOS missile→aim/target. Positive = closing.
        /// </summary>
        internal static bool TryGetClosingSpeedMs(Missile missile, out float closingMs)
        {
            closingMs = 0f;
            if (missile == null || missile.rb == null)
                return false;

            GlobalPosition from = missile.transform.GlobalPosition();
            GlobalPosition to;
            if (TryGetAimPoint(missile, out GlobalPosition aim))
                to = aim;
            else if (TryGetTargetPosition(missile, out GlobalPosition targetPos))
                to = targetPos;
            else
                return false;

            Vector3 los = FastMath.Direction(from, to);
            if (los.sqrMagnitude < 0.0001f)
                return false;

            float missileAlong = Vector3.Dot(missile.rb.velocity, los);
            float targetAlong = 0f;
            if (TryGetTarget(missile, out Unit? target) && target != null && target.rb != null)
                targetAlong = Vector3.Dot(target.rb.velocity, los);

            closingMs = missileAlong - targetAlong;
            return true;
        }

        /// <summary>Heuristic time-to-impact seconds from range / closing speed.</summary>
        internal static bool TryGetTimeToImpactSec(Missile missile, out float ttiSec)
        {
            ttiSec = 0f;
            if (!TryGetTargetRangeMeters(missile, out float rangeM) || rangeM <= 0.5f)
                return false;

            if (!TryGetClosingSpeedMs(missile, out float closingMs) || closingMs < 1f)
                return false;

            ttiSec = rangeM / closingMs;
            return ttiSec > 0f && ttiSec < 600f && !float.IsNaN(ttiSec) && !float.IsInfinity(ttiSec);
        }

        /// <summary>Target unit speed (m/s) when locked.</summary>
        internal static bool TryGetTargetSpeedMs(Missile missile, out float speedMs)
        {
            speedMs = 0f;
            if (!TryGetTarget(missile, out Unit? target) || target == null)
                return false;

            if (target.rb != null)
            {
                speedMs = target.rb.velocity.magnitude;
                return true;
            }

            speedMs = Mathf.Abs(target.speed);
            return true;
        }

        internal static string GetTargetRid(Missile missile)
        {
            if (!TryGetTarget(missile, out Unit? target) || target == null)
                return "---";

            return "I-" + target.persistentID.Id.ToString();
        }

        private static FieldInfo? ResolveGLimitField()
        {
            if (_gLimitField != null)
                return _gLimitField;

            _gLimitField = typeof(Missile).GetField("gLimit", InstanceAny);
            return _gLimitField;
        }

        private static FieldInfo? ResolveAimPointField()
        {
            if (_aimPointField != null)
                return _aimPointField;

            _aimPointField = typeof(Missile).GetField("aimPoint", InstanceAny);
            return _aimPointField;
        }
    }
}
