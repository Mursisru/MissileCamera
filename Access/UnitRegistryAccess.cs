using UnityEngine;

namespace MissileCamera
{
    /// <summary>
    /// Scene-unit markers mirror DynamicMap visibility:
    /// allies = same HQ; hostiles = FactionHQ.trackingDatabase (map icons), not Unobserved-only.
    /// </summary>
    internal static class UnitRegistryAccess
    {
        /// <summary>Prefer DynamicMap.HQ (what the player map uses), then missile/aircraft HQ.</summary>
        internal static FactionHQ? ResolveOwnHq(Missile? missile)
        {
            try
            {
                DynamicMap? map = SceneSingleton<DynamicMap>.i;
                if (map != null && map.HQ != null)
                    return map.HQ;
            }
            catch
            {
                // SceneSingleton may throw if scene not ready.
            }

            if (missile != null && !missile.disabled && missile.NetworkHQ != null)
                return missile.NetworkHQ;

            if (AircraftCamAccess.TryGetLocalAircraft(out Aircraft aircraft)
                && aircraft != null
                && aircraft.NetworkHQ != null)
                return aircraft.NetworkHQ;

            return null;
        }

        internal static bool IsUsableMarkerUnit(Unit unit, Missile? seekerMissile, Unit? lockedTarget)
        {
            if (unit == null || unit.disabled)
                return false;

            if (seekerMissile != null && ReferenceEquals(unit, seekerMissile))
                return false;

            if (lockedTarget != null && ReferenceEquals(unit, lockedTarget))
                return false;

            return true;
        }

        internal static bool IsAlly(Unit unit, FactionHQ? ownHq)
        {
            if (ownHq == null || unit == null)
                return false;

            return unit.NetworkHQ != null && unit.NetworkHQ == ownHq;
        }

        /// <summary>
        /// Map-parity detection: ally always; hostile only if in own HQ trackingDatabase
        /// (same set DynamicMap uses for icons — not live wallhack of UnitRegistry).
        /// </summary>
        internal static bool TryGetMapVisibleMarkerPose(
            Unit unit,
            FactionHQ? ownHq,
            out GlobalPosition knownPosition,
            out bool isAlly)
        {
            knownPosition = default;
            isAlly = false;
            if (unit == null || ownHq == null)
                return false;

            isAlly = IsAlly(unit, ownHq);
            if (isAlly)
            {
                knownPosition = unit.GlobalPosition();
                return true;
            }

            // Hostile / unknown-faction: must be a tracked map contact.
            if (!ownHq.TryGetKnownPosition(unit, out knownPosition))
                return false;

            // Extra guard: entry must actually exist in trackingDatabase
            // (GetKnownPosition only returns live pos for same-HQ; for others needs DB).
            if (!ownHq.trackingDatabase.ContainsKey(unit.persistentID))
                return false;

            return true;
        }

        /// <summary>True when another missile's targetID points at our seeker.</summary>
        internal static bool IsInboundAtSeeker(Missile inbound, Missile seeker)
        {
            if (inbound == null || seeker == null || inbound.disabled || seeker.disabled)
                return false;

            if (ReferenceEquals(inbound, seeker))
                return false;

            PersistentID seekerId = seeker.persistentID;
            if (!seekerId.IsValid)
                return false;

            PersistentID targetId = inbound.targetID;
            return targetId.IsValid && targetId == seekerId;
        }

        internal static Vector3 TryGetUnitVelocity(Unit unit)
        {
            if (unit == null)
                return Vector3.zero;

            if (unit.rb != null)
                return unit.rb.velocity;

            return Vector3.zero;
        }
    }
}
