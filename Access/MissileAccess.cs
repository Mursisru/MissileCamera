using System.Reflection;
using UnityEngine;

namespace MissileCamera
{
    internal static class MissileAccess
    {
        private const BindingFlags InstanceAny = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static FieldInfo? _aimPointField;
        private static FieldInfo? _gLimitField;

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

            FieldInfo field = ResolveAimPointField();
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

            FieldInfo field = ResolveGLimitField();
            if (field == null)
                return false;

            object? value = field.GetValue(missile);
            if (value is not float limit)
                return false;

            gLimit = limit;
            return gLimit > 0f;
        }

        private static FieldInfo ResolveGLimitField()
        {
            if (_gLimitField != null)
                return _gLimitField;

            _gLimitField = typeof(Missile).GetField("gLimit", InstanceAny);
            return _gLimitField;
        }

        private static FieldInfo ResolveAimPointField()
        {
            if (_aimPointField != null)
                return _aimPointField;

            _aimPointField = typeof(Missile).GetField("aimPoint", InstanceAny);
            return _aimPointField;
        }
    }
}
