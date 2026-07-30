using UnityEngine;

namespace MissileCamera
{
    /// <summary>
    /// Auto IR from lighting only: low GetDaylightFactor (night / thick clouds at missile)
    /// or truly low GetAmbientLight. No fixed time-of-day window.
    /// </summary>
    internal static class MissileCameraInfraredPolicy
    {
        private const float AmbientExposureMin = 0.02f;
        private const float AmbientExposureMax = 0.4f;
        private const float PolicyIntervalSeconds = 1f;

        private static float _nextEvaluateUnscaled;
        private static bool _infraredActive;
        private static float _cachedExposure;
        private static float _cachedAmbient = 1f;
        private static float _cachedDaylight = 1f;

        internal static bool InfraredActive => _infraredActive;

        internal static float Exposure => _cachedExposure;

        internal static float CachedAmbient => _cachedAmbient;

        internal static float CachedDaylight => _cachedDaylight;

        internal static void Reset()
        {
            _nextEvaluateUnscaled = 0f;
            _infraredActive = false;
            _cachedExposure = 0f;
            _cachedAmbient = 1f;
            _cachedDaylight = 1f;
        }

        internal static bool Evaluate(Vector3 missileWorldPosition, out float exposure)
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

            if (!LevelInfoAccess.TryGetAmbientLight(out float ambient)
                || !LevelInfoAccess.TryGetDaylightFactor(missileWorldPosition, out float daylight))
            {
                _infraredActive = false;
                _cachedExposure = 0f;
                _cachedAmbient = 1f;
                _cachedDaylight = 1f;
                exposure = 0f;
                return false;
            }

            _cachedAmbient = ambient;
            _cachedDaylight = daylight;

            float hysteresis = Mathf.Max(0f, MissileCameraFeedConfig.InfraredLightHysteresis);
            bool darkDaylight = IsBelowThreshold(
                daylight,
                MissileCameraFeedConfig.InfraredDaylightThreshold,
                hysteresis,
                _infraredActive);
            bool darkAmbient = IsBelowThreshold(
                ambient,
                MissileCameraFeedConfig.InfraredAmbientThreshold,
                hysteresis,
                _infraredActive);

            bool nextInfrared = darkDaylight || darkAmbient;
            if (nextInfrared != _infraredActive)
            {
                MfdLog.Info(
                    $"IR policy {(nextInfrared ? "ON" : "OFF")} " +
                    $"daylight={daylight:F3} ambient={ambient:F3} " +
                    $"darkDay={darkDaylight} darkAmb={darkAmbient} posY={missileWorldPosition.y:F0}");
            }

            _infraredActive = nextInfrared;
            _cachedExposure = _infraredActive ? ComputeExposure(ambient) : 0f;
            exposure = _cachedExposure;
            return _infraredActive;
        }

        private static bool IsBelowThreshold(float value, float onThreshold, float hysteresis, bool currentlyInfrared)
        {
            float offThreshold = onThreshold + hysteresis;
            if (currentlyInfrared)
                return value < offThreshold;

            return value < onThreshold;
        }

        /// <summary>Same exposure curve as TargetCam.UpdateExposure in IR mode.</summary>
        internal static float ComputeExposure(float ambient)
        {
            float t = Mathf.InverseLerp(AmbientExposureMin, AmbientExposureMax, ambient);
            return Mathf.Lerp(3f, -0.5f, t);
        }
    }
}
