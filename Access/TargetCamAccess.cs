using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MissileCamera
{
    internal static class TargetCamAccess
    {
        private const BindingFlags InstanceAny = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Dictionary<FieldKey, FieldInfo?> FieldCache = new Dictionary<FieldKey, FieldInfo?>(16);
        private static FieldInfo? _irModeField;
        private static FieldInfo? _currentModeField;
        private static bool _irModeResolved;
        private static bool _currentModeResolved;

        internal static TargetCam? GetTargetCam(Component aircraft) =>
            GetFieldCached<TargetCam>(aircraft, "targetCam");

        internal static Camera? GetCam(TargetCam instance) =>
            GetFieldCached<Camera>(instance, "cam");

        internal static Camera? GetUiCam(TargetCam instance) =>
            GetFieldCached<Camera>(instance, "UICam");

        internal static TargetScreenUI? GetTargetScreenUi(TargetCam instance) =>
            GetFieldCached<TargetScreenUI>(instance, "targetScreenUI");

        internal static Component? GetAircraft(TargetCam instance) =>
            GetFieldCached<Component>(instance, "aircraft");

        internal static Renderer? GetTargetScreenRenderer(TargetCam instance) =>
            GetFieldCached<Renderer>(instance, "targetScreenRenderer");

        internal static Volume? GetScreenVolume(TargetCam instance) =>
            GetFieldCached<Volume>(instance, "screenVolume");

        internal static bool IsIrMode(TargetCam instance)
        {
            if (!_irModeResolved)
            {
                _irModeField = typeof(TargetCam).GetField("IRMode", InstanceAny);
                _irModeResolved = true;
            }

            return _irModeField?.GetValue(instance) is bool ir && ir;
        }

        internal static bool TryGetColorAdjustments(TargetCam instance, out ColorAdjustments? adjustments)
        {
            adjustments = GetFieldCached<ColorAdjustments>(instance, "colorAdjustments");
            return adjustments != null;
        }

        /// <summary>
        /// Live vanilla TargetCam IR state for audit / parity sync (local aircraft only).
        /// </summary>
        internal static bool TryGetVanillaIrSnapshot(out bool irMode, out float postExposure, out float contrast)
        {
            irMode = false;
            postExposure = 0f;
            contrast = 1f;

            if (!GameManager.GetLocalAircraft(out Aircraft aircraft))
                return false;

            TargetCam? targetCam = GetTargetCam(aircraft);
            if (targetCam == null)
                return false;

            irMode = IsIrMode(targetCam);
            if (!TryGetColorAdjustments(targetCam, out ColorAdjustments? adjustments) || adjustments == null)
                return false;

            postExposure = adjustments.postExposure.value;
            contrast = adjustments.contrast.value;
            return true;
        }

        internal static TargetCam.CamMode GetCurrentMode(TargetCam instance)
        {
            if (!_currentModeResolved)
            {
                _currentModeField = typeof(TargetCam).GetField("currentMode", InstanceAny);
                _currentModeResolved = true;
            }

            if (_currentModeField == null)
                return TargetCam.CamMode.targetForward;

            object? value = _currentModeField.GetValue(instance);
            return value is TargetCam.CamMode mode ? mode : TargetCam.CamMode.targetForward;
        }

        internal static bool IsLandingMode(TargetCam instance) =>
            GetCurrentMode(instance) == TargetCam.CamMode.landingMode;

        private static T? GetFieldCached<T>(Component instance, string name) where T : class
        {
            if (instance == null)
                return null;

            Type type = instance.GetType();
            var key = new FieldKey(type, name);
            if (!FieldCache.TryGetValue(key, out FieldInfo? field))
            {
                field = type.GetField(name, InstanceAny);
                FieldCache[key] = field;
            }

            return field?.GetValue(instance) as T;
        }

        private static T? GetFieldCached<T>(TargetCam instance, string name) where T : class
        {
            if (instance == null)
                return null;

            Type type = typeof(TargetCam);
            var key = new FieldKey(type, name);
            if (!FieldCache.TryGetValue(key, out FieldInfo? field))
            {
                field = type.GetField(name, InstanceAny);
                FieldCache[key] = field;
            }

            return field?.GetValue(instance) as T;
        }

        private readonly struct FieldKey : IEquatable<FieldKey>
        {
            private readonly Type _type;
            private readonly string _name;

            internal FieldKey(Type type, string name)
            {
                _type = type;
                _name = name;
            }

            public bool Equals(FieldKey other) =>
                ReferenceEquals(_type, other._type) && _name == other._name;

            public override bool Equals(object? obj) =>
                obj is FieldKey other && Equals(other);

            public override int GetHashCode() =>
                (_type.GetHashCode() * 397) ^ (_name != null ? _name.GetHashCode() : 0);
        }
    }
}
