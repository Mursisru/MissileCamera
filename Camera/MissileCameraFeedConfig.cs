using MissileCamera.Config;
using UnityEngine;

namespace MissileCamera
{
    internal static class MissileCameraFeedConfig
    {
        // Hardcoded seeker / IR picture defaults (not player-facing).
        internal const float NoseSkinInset = 0.08f;
        internal const float CameraBackOffset = 0.35f;
        // Soft horizon: counter body bank around bore when not near-vertical (calm = level picture).
        // Full world LookRotation rebuild is forbidden (pitch spin / 180° singularity).
        internal const bool HorizonLock = true;
        /// <summary>|forward.y| above this → fade horizon counter to body-fixed.</summary>
        internal const float HorizonLevelFadeStart = 0.82f;
        internal const float HorizonLevelFadeEnd = 0.97f;
        internal const float HorizonLevelSmoothTime = 0.22f;
        internal const float HorizonLevelSlewDegPerSec = 180f;
        internal const float TurnLookBankScale = 1f;
        // Visible bank on turns without near-inverting the view (±90 was saturating on light stick).
        internal const float MaxTurnLookDegrees = 42f;
        internal const float DefaultMissileGLimit = 20f;
        internal const float TurnLookGDeadband = 0.4f;
        /// <summary>Reach MaxTurnLook at this fraction of missile gLimit (ease-in below).</summary>
        internal const float TurnLookFullGFraction = 0.85f;
        /// <summary>Opposite-turn G must exceed this × gLimit before bank sign may reverse.</summary>
        internal const float TurnLookReverseHysteresis = 0.28f;
        internal const float TurnLookGFilterHz = 5f;
        internal const float TurnLookSlewDegPerSec = 55f;
        internal const float TurnLookSmoothTime = 0.28f;
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
        /// MFD feed uses cfg size; fullscreen uses fixed FeedWidth/Height (zoom = FOV only — no RT upscale lag).
        /// </summary>
        internal static void ResolveActiveFeedSize(out int width, out int height)
        {
            if (!MissileCameraFullscreenController.IsActive && !MissileCameraFeedController.IsBridgeCaptureActive)
            {
                width = Mathf.Clamp(FeedWidth, 128, 2048);
                height = Mathf.Clamp(FeedHeight, 128, 2048);
                return;
            }

            MissileCameraFullscreenConfig.Refresh();
            width = EvenClamp(Mathf.Clamp(MissileCameraFullscreenConfig.FeedWidth, 640, 3840), 640, 3840);
            height = EvenClamp(Mathf.Clamp(MissileCameraFullscreenConfig.FeedHeight, 360, 2160), 360, 2160);
        }

        /// <summary>Kept for callers; always 1 — optical zoom must not recreate RT buckets.</summary>
        internal static float ResolveFullscreenQualityScale(float magnification) => 1f;

        private static int EvenClamp(int value, int min, int max)
        {
            int v = Mathf.Clamp(value, min, max);
            if ((v & 1) != 0)
                v--;
            return Mathf.Max(v, min);
        }
    }
}
