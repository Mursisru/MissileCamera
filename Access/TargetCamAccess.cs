using System.Reflection;
using UnityEngine;

namespace MissileCamera
{
    internal static class TargetCamAccess
    {
        private const BindingFlags InstanceAny = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal static TargetCam? GetTargetCam(Component aircraft) =>
            GetField<TargetCam>(aircraft, "targetCam");

        internal static Camera? GetCam(TargetCam instance) =>
            GetField<Camera>(instance, "cam");

        internal static Camera? GetUiCam(TargetCam instance) =>
            GetField<Camera>(instance, "UICam");

        internal static TargetScreenUI? GetTargetScreenUi(TargetCam instance) =>
            GetField<TargetScreenUI>(instance, "targetScreenUI");

        internal static Component? GetAircraft(TargetCam instance) =>
            GetField<Component>(instance, "aircraft");

        internal static Renderer? GetTargetScreenRenderer(TargetCam instance) =>
            GetField<Renderer>(instance, "targetScreenRenderer");

        internal static TargetCam.CamMode GetCurrentMode(TargetCam instance)
        {
            FieldInfo? field = instance.GetType().GetField("currentMode", InstanceAny);
            if (field == null)
                return TargetCam.CamMode.targetForward;

            object? value = field.GetValue(instance);
            return value is TargetCam.CamMode mode ? mode : TargetCam.CamMode.targetForward;
        }

        internal static bool IsLandingMode(TargetCam instance) =>
            GetCurrentMode(instance) == TargetCam.CamMode.landingMode;

        private static T? GetField<T>(Component instance, string name) where T : class
        {
            FieldInfo? field = instance.GetType().GetField(name, InstanceAny);
            return field?.GetValue(instance) as T;
        }

        private static T? GetField<T>(TargetCam instance, string name) where T : class
        {
            FieldInfo? field = instance.GetType().GetField(name, InstanceAny);
            return field?.GetValue(instance) as T;
        }
    }
}
