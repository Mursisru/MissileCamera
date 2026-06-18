using UnityEngine;

namespace MissileCamera
{
    /// <summary>
    /// Horizontal turn load — world yaw rate × speed, same units as Missile.Steering g-limit.
    /// </summary>
    internal static class MissileTurnLoad
    {
        private const float WorldYawDeadband = 0.035f;

        internal static bool TrySampleHorizontalTurn(Missile missile, out float lateralG, out float turnSign)
        {
            lateralG = 0f;
            turnSign = 0f;
            if (missile == null || missile.rb == null)
                return false;

            float speed = missile.speed;
            if (speed < 1f)
                return true;

            float worldYawRate = Vector3.Dot(missile.rb.angularVelocity, Vector3.up);
            float turnRate = Mathf.Abs(worldYawRate);
            lateralG = speed * turnRate / 9.81f;

            if (turnRate < WorldYawDeadband)
                return true;

            turnSign = Mathf.Sign(worldYawRate);
            return true;
        }

        internal static float ComputeTargetRollDeg(Missile missile, float filteredLateralG, float filteredTurnSign)
        {
            if (missile == null)
                return 0f;

            if (filteredLateralG < MissileCameraFeedConfig.TurnLookGDeadband)
                return 0f;

            if (Mathf.Abs(filteredTurnSign) < 0.2f)
                return 0f;

            float gLimit = MissileAccess.TryGetGLimit(missile, out float limit) && limit > 0f
                ? limit
                : MissileCameraFeedConfig.DefaultMissileGLimit;

            float normalized = Mathf.Clamp01(filteredLateralG / gLimit);
            return -Mathf.Sign(filteredTurnSign) * normalized * MissileCameraFeedConfig.MaxTurnLookDegrees
                * MissileCameraFeedConfig.TurnLookBankScale;
        }
    }
}
