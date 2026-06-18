using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    internal static class TargetScreenUiAccess
    {
        private const BindingFlags InstanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;

        internal static Canvas? GetDisplayCanvas(TargetScreenUI instance) =>
            GetField<Canvas>(instance, "displayCanvas");

        internal static Text? GetModeText(TargetScreenUI instance) =>
            GetField<Text>(instance, "modeText");

        internal static Text? GetTypeText(TargetScreenUI instance) =>
            GetField<Text>(instance, "typeText");

        internal static Text? GetMagText(TargetScreenUI instance) =>
            GetField<Text>(instance, "magText");

        private static T? GetField<T>(TargetScreenUI instance, string name) where T : class
        {
            FieldInfo? field = instance.GetType().GetField(name, InstanceNonPublic);
            return field?.GetValue(instance) as T;
        }
    }
}
