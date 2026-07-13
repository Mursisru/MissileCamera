using MissileCamera.Config;

namespace MissileCamera
{
    internal static class MissileCameraFeedConfig
    {
        internal static bool Enabled = true;
        internal static float NoseSkinInset = 0.08f;
        internal static float CameraBackOffset = 0.35f;
        internal static float Fov = 60f;
        internal static int FeedWidth = 512;
        internal static int FeedHeight = 512;
        internal static bool HorizonLock = true;
        internal static float TurnLookBankScale = 1f;
        internal static float MaxTurnLookDegrees = 90f;
        internal static float DefaultMissileGLimit = 20f;
        internal static float TurnLookGDeadband = 0.15f;
        internal static float TurnLookGFilterHz = 7f;
        internal static float TurnLookSlewDegPerSec = 120f;
        internal static float TurnLookSmoothTime = 0.18f;
        internal static float PostExplosionHoldSeconds;
        internal static int RenderFps = 30;
        internal static bool InfraredAutoEnabled = true;
        internal static float InfraredDarkAmbientThreshold = 0.12f;
        internal static float InfraredDarkAmbientHysteresis = 0.02f;
        internal static float InfraredContrast = 1f;
        internal static float InfraredBlackPoint = 0.05f;
        internal static float InfraredWhitePoint = 0.95f;
        internal static float InfraredRedWeight = 0.55f;
        internal static float InfraredExposureBiasEv = 0f;
        internal static int Revision;

        internal static void Refresh(bool force = false)
        {
            if (!MissileCameraBepInConfig.IsBound)
                return;

            bool enabled = MissileCameraBepInConfig.FeedEnabled.Value;
            float noseSkinInset = MissileCameraBepInConfig.NoseSkinInset.Value;
            float cameraBackOffset = MissileCameraBepInConfig.CameraBackOffset.Value;
            float fov = MissileCameraBepInConfig.Fov.Value;
            int feedWidth = MissileCameraBepInConfig.FeedWidth.Value;
            int feedHeight = MissileCameraBepInConfig.FeedHeight.Value;
            bool horizonLock = MissileCameraBepInConfig.HorizonLock.Value;
            float turnLookBankScale = MissileCameraBepInConfig.TurnLookBankScale.Value;
            float maxTurnLookDegrees = MissileCameraBepInConfig.MaxTurnLookDegrees.Value;
            float defaultMissileGLimit = MissileCameraBepInConfig.DefaultMissileGLimit.Value;
            float turnLookGDeadband = MissileCameraBepInConfig.TurnLookGDeadband.Value;
            float turnLookGFilterHz = MissileCameraBepInConfig.TurnLookGFilterHz.Value;
            float turnLookSlewDegPerSec = MissileCameraBepInConfig.TurnLookSlewDegPerSec.Value;
            float turnLookSmoothTime = MissileCameraBepInConfig.TurnLookSmoothTime.Value;
            float postExplosionHoldSeconds = MissileCameraBepInConfig.PostExplosionHoldSeconds.Value;
            int renderFps = MissileCameraBepInConfig.RenderFps.Value;
            bool infraredAutoEnabled = MissileCameraBepInConfig.InfraredAutoEnabled.Value;
            float infraredDarkAmbientThreshold = MissileCameraBepInConfig.InfraredDarkAmbientThreshold.Value;
            float infraredDarkAmbientHysteresis = MissileCameraBepInConfig.InfraredDarkAmbientHysteresis.Value;
            float infraredContrast = MissileCameraBepInConfig.InfraredContrast.Value;
            float infraredBlackPoint = MissileCameraBepInConfig.InfraredBlackPoint.Value;
            float infraredWhitePoint = MissileCameraBepInConfig.InfraredWhitePoint.Value;
            float infraredRedWeight = MissileCameraBepInConfig.InfraredRedWeight.Value;
            float infraredExposureBiasEv = MissileCameraBepInConfig.InfraredExposureBiasEv.Value;

            if (!force
                && enabled == Enabled
                && noseSkinInset == NoseSkinInset
                && cameraBackOffset == CameraBackOffset
                && fov == Fov
                && feedWidth == FeedWidth
                && feedHeight == FeedHeight
                && horizonLock == HorizonLock
                && turnLookBankScale == TurnLookBankScale
                && maxTurnLookDegrees == MaxTurnLookDegrees
                && defaultMissileGLimit == DefaultMissileGLimit
                && turnLookGDeadband == TurnLookGDeadband
                && turnLookGFilterHz == TurnLookGFilterHz
                && turnLookSlewDegPerSec == TurnLookSlewDegPerSec
                && turnLookSmoothTime == TurnLookSmoothTime
                && postExplosionHoldSeconds == PostExplosionHoldSeconds
                && renderFps == RenderFps
                && infraredAutoEnabled == InfraredAutoEnabled
                && infraredDarkAmbientThreshold == InfraredDarkAmbientThreshold
                && infraredDarkAmbientHysteresis == InfraredDarkAmbientHysteresis
                && infraredContrast == InfraredContrast
                && infraredBlackPoint == InfraredBlackPoint
                && infraredWhitePoint == InfraredWhitePoint
                && infraredRedWeight == InfraredRedWeight
                && infraredExposureBiasEv == InfraredExposureBiasEv)
                return;

            Enabled = enabled;
            NoseSkinInset = noseSkinInset;
            CameraBackOffset = cameraBackOffset;
            Fov = fov;
            FeedWidth = feedWidth;
            FeedHeight = feedHeight;
            HorizonLock = horizonLock;
            TurnLookBankScale = turnLookBankScale;
            MaxTurnLookDegrees = maxTurnLookDegrees;
            DefaultMissileGLimit = defaultMissileGLimit;
            TurnLookGDeadband = turnLookGDeadband;
            TurnLookGFilterHz = turnLookGFilterHz;
            TurnLookSlewDegPerSec = turnLookSlewDegPerSec;
            TurnLookSmoothTime = turnLookSmoothTime;
            PostExplosionHoldSeconds = postExplosionHoldSeconds;
            RenderFps = renderFps;
            InfraredAutoEnabled = infraredAutoEnabled;
            InfraredDarkAmbientThreshold = infraredDarkAmbientThreshold;
            InfraredDarkAmbientHysteresis = infraredDarkAmbientHysteresis;
            InfraredContrast = infraredContrast;
            InfraredBlackPoint = infraredBlackPoint;
            InfraredWhitePoint = infraredWhitePoint;
            InfraredRedWeight = infraredRedWeight;
            InfraredExposureBiasEv = infraredExposureBiasEv;
            Revision++;
        }
    }
}
