using UnityEngine;

namespace MissileCamera
{
    /// <summary>
    /// Auto IR decision matching vanilla TargetCam night window (6–18) plus ambient darkness.
    /// Local-only visual policy — safe in multiplayer (no network writes).
    /// </summary>
    internal static class MissileCameraInfraredPolicy
    {
        private const float NightEndHour = 6f;
        private const float NightStartHour = 18f;
        private const float AmbientExposureMin = 0.02f;
        private const float AmbientExposureMax = 0.4f;
        private const float PolicyIntervalSeconds = 0.5f;

        private static float _nextEvaluateUnscaled;
        private static bool _infraredActive;
        private static float _cachedExposure;
        private static float _cachedAmbient = 1f;

        internal static bool InfraredActive => _infraredActive;

        internal static float Exposure => _cachedExposure;

        internal static void Reset()
        {
            _nextEvaluateUnscaled = 0f;
            _infraredActive = false;
            _cachedExposure = 0f;
            _cachedAmbient = 1f;
        }

        internal static bool Evaluate(out float exposure)
        {
            float now = Time.unscaledTime;
            if (now < _nextEvaluateUnscaled)
            {
                exposure = _cachedExposure;
                return _infraredActive;
            }

            _nextEvaluateUnscaled = now + PolicyIntervalSeconds;

            if (!MissileCameraFeedConfig.InfraredAutoEnabled)
            {
                _infraredActive = false;
                _cachedExposure = 0f;
                exposure = 0f;
                return false;
            }

            if (!LevelInfoAccess.TryGetTimeOfDay(out float timeOfDay)
                || !LevelInfoAccess.TryGetAmbientLight(out float ambient))
            {
                _infraredActive = false;
                _cachedExposure = 0f;
                _cachedAmbient = 1f;
                exposure = 0f;
                return false;
            }

            _cachedAmbient = ambient;
            bool night = IsNight(timeOfDay);
            bool dark = IsDark(ambient, _infraredActive);
            bool nextInfrared = night || dark;
            if (nextInfrared != _infraredActive)
            {
                MfdLog.Info(
                    $"IR policy {(nextInfrared ? "ON" : "OFF")} tod={timeOfDay:F2} ambient={ambient:F3} night={night} dark={dark}");
            }

            _infraredActive = nextInfrared;
            _cachedExposure = _infraredActive ? ComputeExposure(ambient) : 0f;
            exposure = _cachedExposure;
            return _infraredActive;
        }

        internal static bool IsNight(float timeOfDay) =>
            timeOfDay < NightEndHour || timeOfDay > NightStartHour;

        private static bool IsDark(float ambient, bool currentlyInfrared)
        {
            float onThreshold = MissileCameraFeedConfig.InfraredDarkAmbientThreshold;
            float hysteresis = Mathf.Max(0f, MissileCameraFeedConfig.InfraredDarkAmbientHysteresis);
            float offThreshold = onThreshold + hysteresis;

            if (currentlyInfrared)
                return ambient < offThreshold;

            return ambient < onThreshold;
        }

        /// <summary>Same exposure curve as TargetCam.UpdateExposure in IR mode.</summary>
        internal static float ComputeExposure(float ambient)
        {
            float t = Mathf.InverseLerp(AmbientExposureMin, AmbientExposureMax, ambient);
            return Mathf.Lerp(3f, -0.5f, t);
        }
    }
}
