using UnityEngine;

namespace MissileCamera
{
    internal readonly struct HorizonFrame
    {
        internal readonly float BankDeg;
        internal readonly float HudRollDeg;
        internal readonly float HudPitchOffsetDeg;
        internal readonly float AppliedRollDeg;

        internal HorizonFrame(float bankDeg, float hudRollDeg, float hudPitchOffsetDeg, float appliedRollDeg)
        {
            BankDeg = bankDeg;
            HudRollDeg = hudRollDeg;
            HudPitchOffsetDeg = hudPitchOffsetDeg;
            AppliedRollDeg = appliedRollDeg;
        }

        internal static HorizonFrame Empty => new HorizonFrame(0f, 0f, 0f, 0f);

        /// <summary>
        /// Roll around bore: compare level-up to body dorsal in the plane perpendicular to forward.
        /// </summary>
        internal static float ComputeBankDeg(Transform missileTransform)
        {
            Vector3 forward = missileTransform.forward;
            if (forward.sqrMagnitude < Epsilon)
                return 0f;

            forward.Normalize();
            Vector3 levelUp = Vector3.ProjectOnPlane(Vector3.up, forward);
            if (levelUp.sqrMagnitude < Epsilon)
                return 0f;

            levelUp.Normalize();
            Vector3 bodyRef = BodyReferenceInPlane(missileTransform, forward);
            if (bodyRef.sqrMagnitude < Epsilon)
                return 0f;

            return Vector3.SignedAngle(levelUp, bodyRef, forward);
        }

        /// <summary>
        /// Seeker world rotation: body forward + optional bore roll (horizon counter / TurnLook).
        /// Does not rebuild yaw from world-up (that spun on pitch / snapped near vertical).
        /// </summary>
        internal static Quaternion BuildCameraWorldRotation(Transform missileTransform, float rollDeg, bool horizonLock)
        {
            _ = horizonLock;

            Vector3 forward = missileTransform.forward;
            if (forward.sqrMagnitude < Epsilon)
                forward = Vector3.forward;
            else
                forward.Normalize();

            Quaternion baseLook = Quaternion.LookRotation(forward, missileTransform.up);

            if (Mathf.Abs(rollDeg) < 0.001f)
                return baseLook;

            return Quaternion.AngleAxis(rollDeg, forward) * baseLook;
        }

        internal static float ComputeCameraPitchDeg(Camera cam)
        {
            if (cam == null)
                return 0f;

            return Mathf.Asin(Mathf.Clamp(cam.transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
        }

        internal static HorizonFrame FromCamera(Camera cam, float bankDeg, float appliedRollDeg)
        {
            if (cam == null)
                return new HorizonFrame(bankDeg, 0f, 0f, appliedRollDeg);

            Vector3 gravityInCam = cam.transform.InverseTransformDirection(Vector3.up);
            if (gravityInCam.sqrMagnitude < Epsilon)
                return new HorizonFrame(bankDeg, 0f, 0f, appliedRollDeg);

            gravityInCam.Normalize();
            float hudRollDeg = -Mathf.Atan2(gravityInCam.x, gravityInCam.y) * Mathf.Rad2Deg;
            float hudPitchOffsetDeg = Mathf.Asin(Mathf.Clamp(cam.transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;

            return new HorizonFrame(bankDeg, hudRollDeg, hudPitchOffsetDeg, appliedRollDeg);
        }

        private static Vector3 BodyReferenceInPlane(Transform missileTransform, Vector3 forward)
        {
            const float minLenSq = 0.05f * 0.05f;

            Vector3 upProj = Vector3.ProjectOnPlane(missileTransform.up, forward);
            if (upProj.sqrMagnitude >= minLenSq)
                return upProj.normalized;

            Vector3 rightProj = Vector3.ProjectOnPlane(missileTransform.right, forward);
            if (rightProj.sqrMagnitude >= minLenSq)
                return rightProj.normalized;

            Vector3 negRightProj = Vector3.ProjectOnPlane(-missileTransform.right, forward);
            if (negRightProj.sqrMagnitude >= minLenSq)
                return negRightProj.normalized;

            return Vector3.zero;
        }

        private const float Epsilon = 1e-6f;
    }
}
