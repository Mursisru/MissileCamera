using System.Reflection;
using UnityEngine;

namespace MissileCamera
{
    /// <summary>Nested Motor reflection — FieldInfo cached once (Phase 0 pattern).</summary>
    internal static class MotorAccess
    {
        private const BindingFlags InstanceAny = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static FieldInfo? _motorsField;
        private static FieldInfo? _burnTimeField;
        private static bool _fieldsResolved;

        internal static void EnsureFields()
        {
            if (_fieldsResolved)
                return;

            _fieldsResolved = true;
            _motorsField = typeof(Missile).GetField("motors", InstanceAny);
            System.Type? motorType = typeof(Missile).GetNestedType("Motor", InstanceAny);
            if (motorType != null)
                _burnTimeField = motorType.GetField("burnTime", InstanceAny);
        }

        /// <summary>0–1 remaining motor burn fraction; false if no motors.</summary>
        internal static bool TryGetFuelFraction(Missile missile, out float fraction)
        {
            fraction = 1f;
            if (missile == null)
                return false;

            EnsureFields();
            float totalBurn = 0f;
            if (_motorsField != null && _burnTimeField != null)
            {
                object? motorsObj = _motorsField.GetValue(missile);
                if (motorsObj is System.Array arr)
                {
                    for (int i = 0; i < arr.Length; i++)
                    {
                        object? motor = arr.GetValue(i);
                        if (motor == null)
                            continue;

                        if (_burnTimeField.GetValue(motor) is float burn && burn > 0f)
                            totalBurn += burn;
                    }
                }
            }

            if (totalBurn <= 0.0001f)
                return false;

            float remaining;
            try
            {
                remaining = missile.GetRemainingBurnTime();
            }
            catch
            {
                return false;
            }

            if (float.IsNaN(remaining) || float.IsInfinity(remaining))
                return false;

            fraction = Mathf.Clamp01(remaining / totalBurn);
            return true;
        }
    }
}
