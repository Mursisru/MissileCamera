using UnityEngine;

namespace MissileCamera
{
    /// <summary>
    /// Horizontal turn load for TurnLook bore-roll.
    /// Uses ω×v lateral accel in the world-horizontal plane (flight-path), faded by climb/dive
    /// weight — not forward-azimuth / flatSq (that blew up on pitch and saturated tiny yaws).
    /// </summary>
    internal static class MissileTurnLoad
    {
        private const float MinSpeed = 15f;
        private const float MinFlatSpeed = 10f;
        /// <summary>|cos(flight-path pitch)| below this → no TurnLook (steep climb/dive).</summary>
        private const float MinHorizWeight = 0.45f;

        internal static bool TrySampleHorizontalTurn(Missile missile, out float lateralG, out float turnSign)
        {
            lateralG = 0f;
            turnSign = 0f;
            if (missile == null || missile.rb == null)
                return false;

            if (!TrySampleSignedLateralG(missile, out float signedG))
                return false;

            lateralG = Mathf.Abs(signedG);
            if (lateralG < 1e-4f)
                return true;

            turnSign = Mathf.Sign(signedG);
            return true;
        }

        /// <summary>Signed horizontal-turn G (left positive). 0 when pitch-dominated / too slow.</summary>
        internal static bool TrySampleSignedLateralG(Missile missile, out float signedG)
        {
            signedG = 0f;
            if (missile == null || missile.rb == null)
                return false;

            Vector3 vel = missile.rb.velocity;
            float speed = vel.magnitude;
            if (speed < MinSpeed)
                return true;

            Vector3 flatVel = Vector3.ProjectOnPlane(vel, Vector3.up);
            float flatSpeed = flatVel.magnitude;
            if (flatSpeed < MinFlatSpeed)
                return true;

            // cos(γ): 1=level, 0=vertical. Squared fade kills pitch-coupled false yaw.
            float horizWeight = flatSpeed / speed;
            if (horizWeight < MinHorizWeight)
                return true;

            Vector3 flatDir = flatVel / flatSpeed;
            Vector3 horizLeft = Vector3.Cross(Vector3.up, flatDir);
            if (horizLeft.sqrMagnitude < 1e-8f)
                return true;

            horizLeft.Normalize();
            Vector3 aRot = Vector3.Cross(missile.rb.angularVelocity, vel);
            float fade = horizWeight * horizWeight;
            signedG = Vector3.Dot(aRot, horizLeft) * fade / 9.81f;
            return true;
        }

        internal static float ComputeTargetRollDeg(Missile missile, float filteredSignedG)
        {
            if (missile == null)
                return 0f;

            float absG = Mathf.Abs(filteredSignedG);
            if (absG < MissileCameraFeedConfig.TurnLookGDeadband)
                return 0f;

            float gLimit = MissileAccess.TryGetGLimit(missile, out float limit) && limit > 0f
                ? limit
                : MissileCameraFeedConfig.DefaultMissileGLimit;

            // Full camera bank only near true g-limit; light sticks stay small (ease-in).
            float fullAt = Mathf.Max(0.35f, gLimit * MissileCameraFeedConfig.TurnLookFullGFraction);
            float linear = Mathf.Clamp01(absG / fullAt);
            float shaped = linear * linear; // ease-in: minimal yaw ≠ near-max bank
            float signed = Mathf.Clamp(filteredSignedG / Mathf.Max(absG, 1e-4f), -1f, 1f);
            return -signed * shaped * MissileCameraFeedConfig.MaxTurnLookDegrees
                * MissileCameraFeedConfig.TurnLookBankScale;
        }

        /// <summary>Legacy API used by AdvanceRoll path that still passes abs G + sign.</summary>
        internal static float ComputeTargetRollDeg(Missile missile, float filteredLateralG, float filteredTurnSign)
        {
            float signed = Mathf.Clamp(filteredTurnSign, -1f, 1f) * Mathf.Max(0f, filteredLateralG);
            return ComputeTargetRollDeg(missile, signed);
        }
    }
}
