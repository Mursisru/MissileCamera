using MissileCamera.Config;
using UnityEngine;

namespace MissileCamera
{
    internal static class MissileCameraFeedConfig
    {
        // Hardcoded seeker / IR picture defaults (not player-facing).
        internal const float NoseSkinInset = 0.08f;
        internal const float CameraBackOffset = 0.35f;
        internal const bool HorizonLock = true;
        internal const float TurnLookBankScale = 1f;
        internal const float MaxTurnLookDegrees = 90f;
        internal const float DefaultMissileGLimit = 20f;
        internal const float TurnLookGDeadband = 0.15f;
        internal const float TurnLookGFilterHz = 7f;
        internal const float TurnLookSlewDegPerSec = 120f;
        internal const float TurnLookSmoothTime = 0.18f;
        internal const float PostExplosionHoldSeconds = 0f;
        internal const float InfraredContrast = 1f;
        internal const float InfraredBlackPoint = 0.05f;
        internal const float InfraredWhitePoint = 0.95f;
        internal const float InfraredRedWeight = 0.55f;
        internal const float InfraredExposureBiasEv = 0f;

        internal static bool Enabled = true;
        internal static float Fov = 60f;
        internal static int FeedWidth = 512;
        internal static int FeedHeight = 512;
        internal static float PostLossInterferenceSeconds = 0.5f;
        internal static int RenderFps = 30;
        internal static bool InfraredAutoEnabled = true;
        internal static float InfraredDaylightThreshold = 0.12f;
        internal static float InfraredAmbientThreshold = 0.06f;
        internal static float InfraredLightHysteresis = 0.03f;
        internal static int Revision;

        internal static void Refresh(bool force = false)
        {
            if (!MissileCameraBepInConfig.IsBound)
                return;

            bool enabled = MissileCameraBepInConfig.FeedEnabled.Value;
            float fov = MissileCameraBepInConfig.Fov.Value;
            int feedWidth = MissileCameraBepInConfig.FeedWidth.Value;
            int feedHeight = MissileCameraBepInConfig.FeedHeight.Value;
            float postLossInterferenceSeconds = MissileCameraBepInConfig.PostLossInterferenceSeconds.Value;
            int renderFps = MissileCameraBepInConfig.RenderFps.Value;
            bool infraredAutoEnabled = MissileCameraBepInConfig.InfraredAutoEnabled.Value;
            float infraredDaylightThreshold = MissileCameraBepInConfig.InfraredDaylightThreshold.Value;
            float infraredAmbientThreshold = MissileCameraBepInConfig.InfraredAmbientThreshold.Value;
            float infraredLightHysteresis = MissileCameraBepInConfig.InfraredLightHysteresis.Value;

            if (!force
                && enabled == Enabled
                && fov == Fov
                && feedWidth == FeedWidth
                && feedHeight == FeedHeight
                && postLossInterferenceSeconds == PostLossInterferenceSeconds
                && renderFps == RenderFps
                && infraredAutoEnabled == InfraredAutoEnabled
                && infraredDaylightThreshold == InfraredDaylightThreshold
                && infraredAmbientThreshold == InfraredAmbientThreshold
                && infraredLightHysteresis == InfraredLightHysteresis)
                return;

            Enabled = enabled;
            Fov = fov;
            FeedWidth = feedWidth;
            FeedHeight = feedHeight;
            PostLossInterferenceSeconds = postLossInterferenceSeconds;
            RenderFps = renderFps;
            InfraredAutoEnabled = infraredAutoEnabled;
            InfraredDaylightThreshold = infraredDaylightThreshold;
            InfraredAmbientThreshold = infraredAmbientThreshold;
            InfraredLightHysteresis = infraredLightHysteresis;
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
