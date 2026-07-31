using UnityEngine;

namespace MissileCamera
{
    /// <summary>
    /// Datalink detection states (same as DynamicMap / TrackingInfo):
    /// Undetected = not on map; Tracked = live (+ motion); Lost = last-known freeze.
    /// </summary>
    internal enum UnitDatalinkState : byte
    {
        Undetected = 0,
        Tracked = 1,
        Lost = 2,
        Friendly = 3
    }

    /// <summary>Datalink visibility for FLIR ambient markers — mirrors map icon set.</summary>
    internal static class UnitRegistryAccess
    {
        /// <summary>
        /// HQ for feed markers — seeker NetworkHQ first so markers work without ownship.
        /// </summary>
        internal static FactionHQ? ResolveOwnHq(Missile? missile)
        {
            if (missile != null && !missile.disabled && missile.NetworkHQ != null)
                return missile.NetworkHQ;

            try
            {
                DynamicMap? map = SceneSingleton<DynamicMap>.i;
                if (map != null && map.HQ != null)
                    return map.HQ;
            }
            catch
            {
                // ignore
            }

            try
            {
                CombatHUD? hud = SceneSingleton<CombatHUD>.i;
                if (hud != null && hud.aircraft != null && !hud.aircraft.disabled && hud.aircraft.NetworkHQ != null)
                    return hud.aircraft.NetworkHQ;
            }
            catch
            {
                // ignore
            }

            if (AircraftCamAccess.TryGetLocalAircraft(out Aircraft aircraft)
                && aircraft != null
                && !aircraft.disabled
                && aircraft.NetworkHQ != null)
                return aircraft.NetworkHQ;

            return null;
        }

        internal static bool IsUsableMarkerUnit(Unit? unit, Missile? seekerMissile, Unit? lockedTarget)
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
        /// Resolve pose + datalink state for a contact.
        /// Uses TrackingInfo.GetPosition()/Observed() exactly like the map.
        /// </summary>
        internal static bool TryResolveDatalinkContact(
            TrackingInfo info,
            Missile? seekerMissile,
            Unit? lockedTarget,
            FactionHQ ownHq,
            out Unit? unit,
            out GlobalPosition pose,
            out UnitDatalinkState state,
            out Vector3 velocity)
        {
            unit = null;
            pose = default;
            state = UnitDatalinkState.Undetected;
            velocity = Vector3.zero;

            if (info == null || ownHq == null)
                return false;

            info.TryGetUnit(out unit);
            if (unit == null)
                UnitRegistry.TryGetUnit(new PersistentID?(info.id), out unit);

            if (unit != null)
            {
                if (seekerMissile != null && ReferenceEquals(unit, seekerMissile))
                    return false;
                if (lockedTarget != null && ReferenceEquals(unit, lockedTarget))
                    return false;
                if (unit.disabled)
                    return false;

                if (IsAlly(unit, ownHq))
                {
                    pose = unit.GlobalPosition();
                    state = UnitDatalinkState.Friendly;
                    velocity = TryGetUnitVelocity(unit);
                    return true;
                }
            }

            // Hostile / unknown: must be in tracking DB (info came from there).
            // Tracked = Observed(); Lost = stale lastSpotted → frozen lastKnownPosition.
            pose = info.GetPosition();
            if (info.Observed())
            {
                state = UnitDatalinkState.Tracked;
                if (unit != null)
                    velocity = TryGetUnitVelocity(unit);
            }
            else
            {
                state = UnitDatalinkState.Lost;
                velocity = Vector3.zero;
            }

            return true;
        }

        /// <summary>Friendly faction unit (always live — not a datalink "detect" problem).</summary>
        internal static bool TryResolveFriendlyUnit(
            Unit unit,
            Missile? seekerMissile,
            Unit? lockedTarget,
            out GlobalPosition pose,
            out Vector3 velocity)
        {
            pose = default;
            velocity = Vector3.zero;
            if (!IsUsableMarkerUnit(unit, seekerMissile, lockedTarget))
                return false;

            pose = unit.GlobalPosition();
            velocity = TryGetUnitVelocity(unit);
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
