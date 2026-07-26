using MissileCamera.Config;
using UnityEngine;

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
        internal static float PostLossInterferenceSeconds = 0.5f;
        internal static int RenderFps = 30;
        internal static bool InfraredAutoEnabled = true;
        internal static float InfraredDaylightThreshold = 0.12f;
        internal static float InfraredAmbientThreshold = 0.06f;
        internal static float InfraredLightHysteresis = 0.03f;
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
            float postLossInterferenceSeconds = MissileCameraBepInConfig.PostLossInterferenceSeconds.Value;
            int renderFps = MissileCameraBepInConfig.RenderFps.Value;
            bool infraredAutoEnabled = MissileCameraBepInConfig.InfraredAutoEnabled.Value;
            float infraredDaylightThreshold = MissileCameraBepInConfig.InfraredDaylightThreshold.Value;
            float infraredAmbientThreshold = MissileCameraBepInConfig.InfraredAmbientThreshold.Value;
            float infraredLightHysteresis = MissileCameraBepInConfig.InfraredLightHysteresis.Value;
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
                && postLossInterferenceSeconds == PostLossInterferenceSeconds
                && renderFps == RenderFps
                && infraredAutoEnabled == InfraredAutoEnabled
                && infraredDaylightThreshold == InfraredDaylightThreshold
                && infraredAmbientThreshold == InfraredAmbientThreshold
                && infraredLightHysteresis == InfraredLightHysteresis
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
            PostLossInterferenceSeconds = postLossInterferenceSeconds;
            RenderFps = renderFps;
            InfraredAutoEnabled = infraredAutoEnabled;
            InfraredDaylightThreshold = infraredDaylightThreshold;
            InfraredAmbientThreshold = infraredAmbientThreshold;
            InfraredLightHysteresis = infraredLightHysteresis;
            InfraredContrast = infraredContrast;
            InfraredBlackPoint = infraredBlackPoint;
            InfraredWhitePoint = infraredWhitePoint;
            InfraredRedWeight = infraredRedWeight;
            InfraredExposureBiasEv = infraredExposureBiasEv;
            Revision++;
        }

        /// <summary>
        /// MFD feed uses cfg size; fullscreen uses FeedWidth/Height scaled by optical mag buckets (cap 3840).
        /// </summary>
        internal static void ResolveActiveFeedSize(out int width, out int height)
        {
            if (!MissileCameraFullscreenController.IsActive)
            {
                width = Mathf.Clamp(FeedWidth, 128, 2048);
                height = Mathf.Clamp(FeedHeight, 128, 2048);
                return;
            }

            MissileCameraFullscreenConfig.Refresh();
            int baseW = Mathf.Clamp(MissileCameraFullscreenConfig.FeedWidth, 640, 3840);
            int baseH = Mathf.Clamp(MissileCameraFullscreenConfig.FeedHeight, 360, 2160);
            float scale = ResolveFullscreenQualityScale(MissileCameraFeedController.FullscreenMagnification);
            width = EvenClamp(Mathf.RoundToInt(baseW * scale), 640, 3840);
            height = EvenClamp(Mathf.RoundToInt(baseH * scale), 360, 2160);
        }

        internal static float ResolveFullscreenQualityScale(float magnification)
        {
            float mag = Mathf.Max(magnification, 1f);
            float raw = Mathf.Sqrt(mag);
            if (raw < 1.25f)
                return 1f;
            if (raw < 1.75f)
                return 1.5f;
            if (raw < 2.25f)
                return 2f;
            return 2.5f;
        }

        private static int EvenClamp(int value, int min, int max)
        {
            int v = Mathf.Clamp(value, min, max);
            if ((v & 1) != 0)
                v--;
            return Mathf.Max(v, min);
        }
    }
}
