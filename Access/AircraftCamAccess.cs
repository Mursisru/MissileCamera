using UnityEngine;

namespace MissileCamera
{
    internal static class AircraftCamAccess
    {
        internal static bool TryGetLocalAircraft(out Aircraft aircraft) =>
            GameManager.GetLocalAircraft(out aircraft);

        internal static Transform? GetTransform(Aircraft aircraft) =>
            aircraft != null ? aircraft.transform : null;

        internal static string GetOwnshipName(Aircraft? aircraft)
        {
            if (aircraft == null)
                return "---";

            if (aircraft.definition != null && !string.IsNullOrEmpty(aircraft.definition.unitName))
                return aircraft.definition.unitName;

            if (!string.IsNullOrEmpty(aircraft.unitName))
                return aircraft.unitName;

            return aircraft.name;
        }

        internal static bool TryGetOwnshipAltitudeMeters(Aircraft aircraft, out float altM)
        {
            altM = 0f;
            if (aircraft == null)
                return false;

            altM = aircraft.GlobalPosition().y;
            return true;
        }

        internal static bool TryGetOwnshipSpeedMs(Aircraft aircraft, out float speedMs)
        {
            speedMs = 0f;
            if (aircraft == null)
                return false;

            if (aircraft.rb != null)
            {
                speedMs = aircraft.rb.velocity.magnitude;
                return true;
            }

            speedMs = Mathf.Abs(aircraft.speed);
            return true;
        }

        internal static Transform? TryGetCockpitViewPoint(Aircraft aircraft)
        {
            if (aircraft == null)
                return null;

            Transform? cockpit = aircraft.cockpitViewPoint;
            if (cockpit != null)
                return cockpit;

            return aircraft.transform;
        }
    }
}
