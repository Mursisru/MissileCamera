using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace MissileCamera
{
    /// <summary>
    /// SSOT for HUD + markers: target/aim resolved once here.
    /// </summary>
    internal readonly struct MissileCameraHudSnapshot
    {
        internal readonly bool HasFeed;
        internal readonly bool HasTarget;
        internal readonly bool HasAimPoint;
        internal readonly string MissileName;
        internal readonly string TargetName;
        internal readonly string OwnshipName;
        internal readonly string SpeedText;
        internal readonly string AltitudeText;
        internal readonly string RangeText;
        internal readonly string GText;
        internal readonly string FuelText;
        internal readonly string MachText;
        internal readonly string GuidanceText;
        internal readonly string TargetAngleText;
        internal readonly string TgpRngText;
        internal readonly string TgpAltText;
        internal readonly string TgpSpdText;
        internal readonly string TgpHdgText;
        internal readonly string TgpRelText;
        internal readonly string TgpClosText;
        internal readonly string TgpTargetSpdText;
        internal readonly string TgpModeText;
        internal readonly string TgpPaletteText;
        internal readonly string TgpMagText;
        internal readonly string TgpRidText;
        internal readonly string TgpTtiText;
        internal readonly string OwnshipHdgText;
        /// <summary>Missile yaw degrees (continuous, not rounded).</summary>
        internal readonly float MissileHeadingDeg;
        internal readonly bool HasTimeToImpact;
        internal readonly float TimeToImpactSec;
        internal readonly float TimeToImpactFraction;
        internal readonly bool InfraredActive;
        internal readonly int SalvoIndex;
        internal readonly int SalvoTotal;
        internal readonly GlobalPosition AimPoint;
        internal readonly GlobalPosition TargetPosition;
        internal readonly float TargetRangeMeters;
        internal readonly float TargetAngleDeg;
        internal readonly float InstantG;
        internal readonly float FuelFraction;
        internal readonly float Mach;
        internal readonly MissileGuidanceStatus Guidance;
        internal readonly float PitchDeg;
        internal readonly float RollDeg;
        internal readonly float FeedFovDeg;
        internal readonly float BaseFovDeg;
        internal readonly float TargetBearingDeg;
        internal readonly string GridText;
        internal readonly string TargetGridText;
        internal readonly float ZoomOffset;
        internal readonly float InfraredExposure;
        internal readonly float ClosingSpeedMs;
        internal readonly float RelativeAltitudeMeters;

        private static readonly StringBuilder Scratch = new StringBuilder(64);
        private static float _smoothG;
        private static float _smoothFuel = 1f;
        private static float _smoothMach;
        private static float _smoothRange;
        private static float _smoothAngle;
        private static float _smoothClos;
        private static float _smoothRel;
        private static float _smoothTti;
        private static int _smoothMissileId = int.MinValue;

        private MissileCameraHudSnapshot(
            bool hasFeed,
            bool hasTarget,
            bool hasAimPoint,
            string missileName,
            string targetName,
            string ownshipName,
            string speedText,
            string altitudeText,
            string rangeText,
            string gText,
            string fuelText,
            string machText,
            string guidanceText,
            string targetAngleText,
            string tgpRngText,
            string tgpAltText,
            string tgpSpdText,
            string tgpHdgText,
            string tgpRelText,
            string tgpClosText,
            string tgpTargetSpdText,
            string tgpModeText,
            string tgpPaletteText,
            string tgpMagText,
            string tgpRidText,
            string tgpTtiText,
            string ownshipHdgText,
            float missileHeadingDeg,
            bool hasTimeToImpact,
            float timeToImpactSec,
            float timeToImpactFraction,
            bool infraredActive,
            int salvoIndex,
            int salvoTotal,
            GlobalPosition aimPoint,
            GlobalPosition targetPosition,
            float targetRangeMeters,
            float targetAngleDeg,
            float instantG,
            float fuelFraction,
            float mach,
            MissileGuidanceStatus guidance,
            float pitchDeg,
            float rollDeg,
            float feedFovDeg,
            float baseFovDeg,
            float targetBearingDeg,
            string gridText,
            string targetGridText,
            float zoomOffset,
            float infraredExposure,
            float closingSpeedMs,
            float relativeAltitudeMeters)
        {
            HasFeed = hasFeed;
            HasTarget = hasTarget;
            HasAimPoint = hasAimPoint;
            MissileName = missileName;
            TargetName = targetName;
            OwnshipName = ownshipName;
            SpeedText = speedText;
            AltitudeText = altitudeText;
            RangeText = rangeText;
            GText = gText;
            FuelText = fuelText;
            MachText = machText;
            GuidanceText = guidanceText;
            TargetAngleText = targetAngleText;
            TgpRngText = tgpRngText;
            TgpAltText = tgpAltText;
            TgpSpdText = tgpSpdText;
            TgpHdgText = tgpHdgText;
            TgpRelText = tgpRelText;
            TgpClosText = tgpClosText;
            TgpTargetSpdText = tgpTargetSpdText;
            TgpModeText = tgpModeText;
            TgpPaletteText = tgpPaletteText;
            TgpMagText = tgpMagText;
            TgpRidText = tgpRidText;
            TgpTtiText = tgpTtiText;
            OwnshipHdgText = ownshipHdgText;
            MissileHeadingDeg = missileHeadingDeg;
            HasTimeToImpact = hasTimeToImpact;
            TimeToImpactSec = timeToImpactSec;
            TimeToImpactFraction = timeToImpactFraction;
            InfraredActive = infraredActive;
            SalvoIndex = salvoIndex;
            SalvoTotal = salvoTotal;
            AimPoint = aimPoint;
            TargetPosition = targetPosition;
            TargetRangeMeters = targetRangeMeters;
            TargetAngleDeg = targetAngleDeg;
            InstantG = instantG;
            FuelFraction = fuelFraction;
            Mach = mach;
            Guidance = guidance;
            PitchDeg = pitchDeg;
            RollDeg = rollDeg;
            FeedFovDeg = feedFovDeg;
            BaseFovDeg = baseFovDeg;
            TargetBearingDeg = targetBearingDeg;
            GridText = gridText;
            TargetGridText = targetGridText;
            ZoomOffset = zoomOffset;
            InfraredExposure = infraredExposure;
            ClosingSpeedMs = closingSpeedMs;
            RelativeAltitudeMeters = relativeAltitudeMeters;
        }

        internal static MissileCameraHudSnapshot Empty => new MissileCameraHudSnapshot(
            hasFeed: false,
            hasTarget: false,
            hasAimPoint: false,
            missileName: string.Empty,
            targetName: "---",
            ownshipName: "---",
            speedText: "---",
            altitudeText: "---",
            rangeText: "---",
            gText: "---",
            fuelText: "---",
            machText: "---",
            guidanceText: "---",
            targetAngleText: "---",
            tgpRngText: "RNG ---",
            tgpAltText: "ALT ---",
            tgpSpdText: "SPD ---",
            tgpHdgText: "HDG ---",
            tgpRelText: "REL ---",
            tgpClosText: "CLOS ---",
            tgpTargetSpdText: "SPD ---",
            tgpModeText: "MODE: COLOR",
            tgpPaletteText: "PALETTE: ---",
            tgpMagText: "MAG x1.0",
            tgpRidText: "RID: ---",
            tgpTtiText: string.Empty,
            ownshipHdgText: "HDG ---",
            missileHeadingDeg: 0f,
            hasTimeToImpact: false,
            timeToImpactSec: 0f,
            timeToImpactFraction: 0f,
            infraredActive: false,
            salvoIndex: 1,
            salvoTotal: 1,
            aimPoint: default,
            targetPosition: default,
            targetRangeMeters: 0f,
            targetAngleDeg: 0f,
            instantG: 0f,
            fuelFraction: 0f,
            mach: 0f,
            guidance: MissileGuidanceStatus.Ballistic,
            pitchDeg: 0f,
            rollDeg: 0f,
            feedFovDeg: 60f,
            baseFovDeg: 60f,
            targetBearingDeg: 0f,
            gridText: "GRID ---",
            targetGridText: "GRID ---",
            zoomOffset: 0f,
            infraredExposure: 0f,
            closingSpeedMs: 0f,
            relativeAltitudeMeters: 0f);

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

            float rangeM = 0f;
            MissileAccess.TryGetTargetRangeMeters(missile, out rangeM);
            float angleDeg = 0f;
            MissileAccess.TryGetTargetAngleDeg(missile, out angleDeg);
            float gLoad = 0f;
            MissileAccess.TryGetInstantG(missile, out gLoad);
            float fuel = 1f;
            MissileAccess.TryGetFuelFraction(missile, out fuel);
            float mach = 0f;
            MissileAccess.TryGetMach(missile, out mach);
            MissileGuidanceStatus guidance = MissileAccess.GetGuidanceStatus(missile);

            float closMs = 0f;
            bool hasClos = MissileAccess.TryGetClosingSpeedMs(missile, out closMs);
            float relAlt = 0f;
            bool hasRel = MissileAccess.TryGetRelativeAltitudeMeters(missile, out relAlt);
            float ttiSec = 0f;
            bool hasTti = MissileAccess.TryGetTimeToImpactSec(missile, out ttiSec);

            Smooth(
                missile.GetInstanceID(),
                ref gLoad,
                ref fuel,
                ref mach,
                ref rangeM,
                ref angleDeg,
                ref closMs,
                ref relAlt,
                ref ttiSec,
                hasClos,
                hasRel,
                hasTti);

            float boreRollDeg = rig != null ? rig.BoreRollDeg : 0f;
            float pitchDeg = rig != null ? -HorizonFrame.ComputeCameraPitchDeg(rig.FeedCamera) : 0f;
            float rollDeg = -boreRollDeg;

            AircraftCamAccess.TryGetLocalAircraft(out Aircraft ownship);
            string ownshipName = AircraftCamAccess.GetOwnshipName(ownship);
            // Seeker HUD: missile kinematics (not launcher aircraft).
            float altM = missile.transform.GlobalPosition().y;
            float spdMs = missile.rb.velocity.magnitude;

            float headingDeg = 0f;
            MissileAccess.TryGetTargetHeadingDeg(missile, out headingDeg);
            float missileHdg = missile.transform.eulerAngles.y;

            float baseFov = Mathf.Max(MissileCameraFeedConfig.Fov, 10f);
            float fov = rig != null && rig.FeedCamera != null ? Mathf.Max(rig.FeedCamera.fieldOfView, 0.1f) : baseFov;
            float mag = baseFov / fov;

            float targetBearingDeg = missileHdg;
            if (hasTarget)
            {
                Vector3 to = FastMath.Direction(missile.transform.GlobalPosition(), targetPosition);
                if (to.sqrMagnitude > 0.0001f)
                    targetBearingDeg = Mathf.Repeat(Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, 360f);
            }

            bool fullscreen = MissileCameraFullscreenController.IsActive;
            MissileCameraVisionMode fsVision = MissileCameraVisionModeController.Mode;
            bool infrared = fullscreen
                ? MissileCameraVisionModeController.UsesInfraredBlit(fsVision)
                    || MissileCameraVisionModeController.UsesNightVisionVolume(fsVision)
                : MissileCameraInfraredPolicy.InfraredActive;
            float exposure = MissileCameraInfraredPolicy.Exposure;
            string tgpMode = fullscreen
                ? MissileCameraVisionModeController.ModeLabel(fsVision)
                : MissileCameraTelemetry.FormatTgpMode(infrared);
            string tgpPalette = fullscreen
                ? MissileCameraVisionModeController.PaletteLabel(fsVision)
                : MissileCameraTelemetry.FormatTgpPalette(infrared);
            bool showTti = hasTti && ttiSec > 0.05f && closMs >= 1f;
            float ttiFraction = 0f;
            if (showTti)
            {
                const float ttiBarFullSec = 12f;
                ttiFraction = Mathf.Clamp01(1f - (ttiSec / ttiBarFullSec));
            }

            string missileHdgText = MissileCameraTelemetry.FormatTgpHdg(missileHdg);
            float tgtSpdMs = 0f;
            bool hasTgtSpd = MissileAccess.TryGetTargetSpeedMs(missile, out tgtSpdMs);
            string gridText = MapGridAccess.GetGridLabel(missile.transform.GlobalPosition());
            string targetGridText = hasTarget
                ? MapGridAccess.GetGridLabel(targetPosition)
                : "GRID ---";
            float zoomOffset = rig != null ? rig.ZoomOffset : 0f;

            return new MissileCameraHudSnapshot(
                hasFeed: rig?.Texture != null,
                hasTarget: hasTarget,
                hasAimPoint: hasAimPoint,
                missileName: MissileAccess.GetMissileName(missile),
                targetName: MissileAccess.GetTargetName(missile),
                ownshipName: ownshipName,
                speedText: MissileCameraTelemetry.FormatSpeed(missile),
                altitudeText: MissileCameraTelemetry.FormatAltitude(missile),
                rangeText: MissileCameraTelemetry.FormatRange(missile),
                gText: string.Empty,
                fuelText: string.Empty,
                machText: string.Empty,
                guidanceText: string.Empty,
                targetAngleText: string.Empty,
                tgpRngText: MissileCameraTelemetry.FormatTgpRng(missile),
                tgpAltText: MissileCameraTelemetry.FormatTgpAlt(altM),
                tgpSpdText: MissileCameraTelemetry.FormatTgpSpd(spdMs),
                tgpHdgText: hasTarget
                    ? MissileCameraTelemetry.FormatTgpHdg(headingDeg)
                    : "HDG ---",
                tgpRelText: hasRel
                    ? MissileCameraTelemetry.FormatTgpRel(relAlt)
                    : "REL ---",
                tgpClosText: hasClos
                    ? MissileCameraTelemetry.FormatTgpClos(Mathf.Max(0f, closMs))
                    : "CLOS ---",
                tgpTargetSpdText: hasTgtSpd
                    ? MissileCameraTelemetry.FormatTgpSpd(tgtSpdMs)
                    : "SPD ---",
                tgpModeText: tgpMode,
                tgpPaletteText: tgpPalette,
                tgpMagText: MissileCameraTelemetry.FormatTgpMag(mag),
                tgpRidText: MissileCameraTelemetry.FormatTgpRid(MissileAccess.GetTargetRid(missile)),
                tgpTtiText: showTti ? MissileCameraTelemetry.FormatTgpTti(ttiSec) : string.Empty,
                ownshipHdgText: missileHdgText,
                missileHeadingDeg: Mathf.Repeat(missileHdg, 360f),
                hasTimeToImpact: showTti,
                timeToImpactSec: ttiSec,
                timeToImpactFraction: ttiFraction,
                infraredActive: infrared,
                salvoIndex: salvoIndex,
                salvoTotal: salvoTotal,
                aimPoint: aimPoint,
                targetPosition: targetPosition,
                targetRangeMeters: rangeM,
                targetAngleDeg: angleDeg,
                instantG: gLoad,
                fuelFraction: fuel,
                mach: mach,
                guidance: guidance,
                pitchDeg: pitchDeg,
                rollDeg: rollDeg,
                feedFovDeg: fov,
                baseFovDeg: baseFov,
                targetBearingDeg: targetBearingDeg,
                gridText: gridText,
                targetGridText: targetGridText,
                zoomOffset: zoomOffset,
                infraredExposure: exposure,
                closingSpeedMs: hasClos ? Mathf.Max(0f, closMs) : 0f,
                relativeAltitudeMeters: hasRel ? relAlt : 0f);
        }

        private static void Smooth(
            int missileId,
            ref float g,
            ref float fuel,
            ref float mach,
            ref float range,
            ref float angle,
            ref float clos,
            ref float rel,
            ref float tti,
            bool hasClos,
            bool hasRel,
            bool hasTti)
        {
            float hz = Mathf.Max(MissileCameraTelemetryConfig.SmoothHz, 1f);
            float renderHz = Mathf.Max(MissileCameraFeedConfig.RenderFps, 1);
            float useHz = Mathf.Min(hz, renderHz);
            float t = 1f - Mathf.Exp(-useHz * Time.unscaledDeltaTime);

            if (_smoothMissileId != missileId)
            {
                _smoothMissileId = missileId;
                _smoothG = g;
                _smoothFuel = fuel;
                _smoothMach = mach;
                _smoothRange = range;
                _smoothAngle = angle;
                _smoothClos = clos;
                _smoothRel = rel;
                _smoothTti = tti;
                return;
            }

            _smoothG = Mathf.Lerp(_smoothG, g, t);
            _smoothFuel = Mathf.Lerp(_smoothFuel, fuel, t);
            _smoothMach = Mathf.Lerp(_smoothMach, mach, t);
            _smoothRange = Mathf.Lerp(_smoothRange, range, t);
            _smoothAngle = Mathf.Lerp(_smoothAngle, angle, t);
            if (hasClos)
                _smoothClos = Mathf.Lerp(_smoothClos, clos, t);
            if (hasRel)
                _smoothRel = Mathf.Lerp(_smoothRel, rel, t);
            if (hasTti)
                _smoothTti = Mathf.Lerp(_smoothTti, tti, t);

            g = _smoothG;
            fuel = _smoothFuel;
            mach = _smoothMach;
            range = _smoothRange;
            angle = _smoothAngle;
            clos = _smoothClos;
            rel = _smoothRel;
            tti = _smoothTti;
        }

        private static string FormatG(float g) =>
            ScratchClear().Append("G:").Append(g.ToString("0.0", CultureInfo.InvariantCulture)).ToString();

        private static string FormatFuel(float fraction) =>
            ScratchClear().Append("FUEL:").Append(Mathf.RoundToInt(fraction * 100f).ToString(CultureInfo.InvariantCulture)).Append('%').ToString();

        private static string FormatMach(float mach) =>
            ScratchClear().Append("M:").Append(mach.ToString("0.00", CultureInfo.InvariantCulture)).ToString();

        private static string FormatGuidance(MissileGuidanceStatus status) =>
            status switch
            {
                MissileGuidanceStatus.Guided => "GUIDED",
                MissileGuidanceStatus.LostLock => "LOST LOCK",
                _ => "BALLISTIC"
            };

        private static string FormatAngle(float deg) =>
            ScratchClear().Append("ANG:").Append(deg.ToString("0", CultureInfo.InvariantCulture)).Append('°').ToString();

        private static string FormatRange(float meters)
        {
            if (meters >= 1000f)
                return ScratchClear().Append('R').Append(':').Append((meters * 0.001f).ToString("0.0", CultureInfo.InvariantCulture)).Append('k').ToString();

            return ScratchClear().Append('R').Append(':').Append(meters.ToString("0", CultureInfo.InvariantCulture)).Append('m').ToString();
        }

        private static StringBuilder ScratchClear()
        {
            Scratch.Length = 0;
            return Scratch;
        }
    }
}
