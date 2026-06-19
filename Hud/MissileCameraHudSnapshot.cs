using System.Collections.Generic;
using UnityEngine;

namespace MissileCamera
{
    internal readonly struct MissileCameraHudSnapshot
    {
        internal readonly bool HasFeed;
        internal readonly bool HasTarget;
        internal readonly bool HasAimPoint;
        internal readonly string MissileName;
        internal readonly string TargetName;
        internal readonly string SpeedText;
        internal readonly string AltitudeText;
        internal readonly string RangeText;
        internal readonly int SalvoIndex;
        internal readonly int SalvoTotal;
        internal readonly GlobalPosition AimPoint;
        internal readonly GlobalPosition TargetPosition;
        internal readonly float PitchDeg;
        internal readonly float RollDeg;

        private MissileCameraHudSnapshot(
            bool hasFeed,
            bool hasTarget,
            bool hasAimPoint,
            string missileName,
            string targetName,
            string speedText,
            string altitudeText,
            string rangeText,
            int salvoIndex,
            int salvoTotal,
            GlobalPosition aimPoint,
            GlobalPosition targetPosition,
            float pitchDeg,
            float rollDeg)
        {
            HasFeed = hasFeed;
            HasTarget = hasTarget;
            HasAimPoint = hasAimPoint;
            MissileName = missileName;
            TargetName = targetName;
            SpeedText = speedText;
            AltitudeText = altitudeText;
            RangeText = rangeText;
            SalvoIndex = salvoIndex;
            SalvoTotal = salvoTotal;
            AimPoint = aimPoint;
            TargetPosition = targetPosition;
            PitchDeg = pitchDeg;
            RollDeg = rollDeg;
        }

        internal static MissileCameraHudSnapshot Empty => new MissileCameraHudSnapshot(
            hasFeed: false,
            hasTarget: false,
            hasAimPoint: false,
            missileName: string.Empty,
            targetName: "---",
            speedText: "---",
            altitudeText: "---",
            rangeText: "---",
            salvoIndex: 1,
            salvoTotal: 1,
            aimPoint: default,
            targetPosition: default,
            pitchDeg: 0f,
            rollDeg: 0f);

        internal static MissileCameraHudSnapshot Build(
            Missile? missile,
            MissileCameraRig? rig,
            IReadOnlyList<Missile> ownedActive)
        {
            if (missile == null || missile.disabled || missile.rb == null)
                return Empty;

            MissileCameraSalvoTracker.GetSalvoInfo(missile, ownedActive, out int salvoIndex, out int salvoTotal);

            bool hasTarget = MissileAccess.TryGetTargetPosition(missile, out GlobalPosition targetPosition);
            bool hasAimPoint = MissileAccess.TryGetAimPoint(missile, out GlobalPosition aimPoint);

            float boreRollDeg = rig != null ? rig.BoreRollDeg : 0f;
            float pitchDeg = rig != null ? -HorizonFrame.ComputeCameraPitchDeg(rig.FeedCamera) : 0f;
            float rollDeg = -boreRollDeg;

            return new MissileCameraHudSnapshot(
                hasFeed: rig?.Texture != null,
                hasTarget: hasTarget,
                hasAimPoint: hasAimPoint,
                missileName: MissileAccess.GetMissileName(missile),
                targetName: MissileAccess.GetTargetName(missile),
                speedText: MissileCameraTelemetry.FormatSpeed(missile),
                altitudeText: MissileCameraTelemetry.FormatAltitude(missile),
                rangeText: MissileCameraTelemetry.FormatRange(missile),
                salvoIndex: salvoIndex,
                salvoTotal: salvoTotal,
                aimPoint: aimPoint,
                targetPosition: targetPosition,
                pitchDeg: pitchDeg,
                rollDeg: rollDeg);
        }
    }
}
