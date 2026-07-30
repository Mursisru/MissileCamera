using UnityEngine;

namespace MissileCamera
{
    /// <summary>
    /// Aircraft MFD layouts differ (see reference screenshots):
    /// - Dedicated split: wide target display тАФ apply mod.
    /// - Tac overlay: small radar widget on corner тАФ skip mod (size-based only).
    /// </summary>
    internal enum MfdLayoutProfile
    {
        Skip,
        DedicatedSplit
    }

    internal static class MfdDisplayMode
    {
        internal static MfdLayoutProfile Resolve(TargetCam targetCam)
        {
            string mode = MfdLayoutConfig.DisplayMode;
            if (string.Equals(mode, "skip", System.StringComparison.OrdinalIgnoreCase))
                return MfdLayoutProfile.Skip;
            if (string.Equals(mode, "split", System.StringComparison.OrdinalIgnoreCase))
                return MfdLayoutProfile.DedicatedSplit;

            string? jsonKey = GetAircraftJsonKey(targetCam);

            TacScreen? tacScreen = ResolveTacScreen(targetCam);
            if (tacScreen == null)
                return MfdLayoutProfile.DedicatedSplit;

            GameObject? display = TacScreenAccess.GetTargetCamDisplay(tacScreen);
            if (display == null || !display.activeInHierarchy)
                return MfdLayoutProfile.DedicatedSplit;

            RectTransform? rt = display.GetComponent<RectTransform>();
            if (rt == null)
                return MfdLayoutProfile.DedicatedSplit;

            float width = rt.anchorMax.x - rt.anchorMin.x;
            float height = rt.anchorMax.y - rt.anchorMin.y;

            // Bomber tac MFD (AB-4 Alkyon / SFB-81 Darkreach): weapon UI may live off TacScreen root.
            if (IsBomberTacAircraft(jsonKey)
                && MfdWeaponsZoneAccess.HasBomberBayMarkersForTacScreen(tacScreen, jsonKey))
            {
                return MfdLayoutProfile.DedicatedSplit;
            }

            // Darkreach-only: right tac weapon strip can exist without bay markers on tacScreen.
            if (string.Equals(jsonKey, "Darkreach", System.StringComparison.OrdinalIgnoreCase)
                && MfdWeaponsZoneAccess.HasDarkreachTacWeaponUi(tacScreen.gameObject))
            {
                return MfdLayoutProfile.DedicatedSplit;
            }

            // SAH-46 Chicane: small tac radar would size-Skip; MC replaces TURBINE column on engine MFD.
            // jsonKey-gated only — no early Tac trigger (avoids ApplyHidden race).
            if (TacScreenAccess.IsChicaneAircraft(jsonKey))
                return MfdLayoutProfile.DedicatedSplit;

            // EW-25 Medusa: RADOME/LASER armed strip on right tac MFD.
            if (MfdWeaponsZoneAccess.HasMedusaWeaponsMarkers(tacScreen.gameObject))
            {
                return MfdLayoutProfile.DedicatedSplit;
            }

            // Corner radar widget only тАФ not a half-screen target feed (even if roughly square).
            if (width < 0.32f && height < 0.42f)
                return MfdLayoutProfile.Skip;

            return MfdLayoutProfile.DedicatedSplit;
        }

        private static bool IsBomberTacAircraft(string? jsonKey)
        {
            if (string.IsNullOrEmpty(jsonKey))
                return false;

            return string.Equals(jsonKey, "FastBomber1", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(jsonKey, "Darkreach", System.StringComparison.OrdinalIgnoreCase);
        }

        internal static string? GetAircraftJsonKey(TargetCam targetCam)
        {
            Component? aircraft = TargetCamAccess.GetAircraft(targetCam);
            return aircraft != null ? GetJsonKey(aircraft) : null;
        }

        internal static TacScreen? ResolveTacScreen(TargetCam targetCam)
        {
            Component? aircraft = TargetCamAccess.GetAircraft(targetCam);
            return TacScreenAccess.Resolve(aircraft);
        }

        private static string? GetJsonKey(Component aircraft)
        {
            object? definition = GetField<object>(aircraft, "definition");
            if (definition == null)
                return null;

            System.Reflection.FieldInfo? field = definition.GetType().GetField(
                "jsonKey",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            return field?.GetValue(definition) as string;
        }

        private static T? GetField<T>(Component instance, string name) where T : class
        {
            System.Reflection.FieldInfo? field = instance.GetType().GetField(
                name,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            return field?.GetValue(instance) as T;
        }
    }
}
