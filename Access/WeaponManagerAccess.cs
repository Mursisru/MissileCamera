using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MissileCamera
{
    internal static class WeaponManagerAccess
    {
        private const BindingFlags InstanceAny = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal static Component? GetAircraft(Component weaponManager) =>
            GetField<Component>(weaponManager, "aircraft");

        internal static int GetTargetCount(Component weaponManager)
        {
            List<Unit>? list = GetField<List<Unit>>(weaponManager, "targetList");
            return list?.Count ?? 0;
        }

        private static T? GetField<T>(Component instance, string name) where T : class
        {
            FieldInfo? field = instance.GetType().GetField(name, InstanceAny);
            return field?.GetValue(instance) as T;
        }
    }
}
