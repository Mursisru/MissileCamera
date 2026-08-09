using System.Collections;
using UnityEngine;

namespace MissileCamera
{
    internal static class MfdLayoutRetryHost
    {
        private const float IntervalSeconds = 0.35f;
        private const int MaxAttempts = 8;

        private static MfdLayoutRetryBehaviour? _behaviour;

        internal static void Schedule(TargetCam targetCam, int generation)
        {
            EnsureBehaviour();
            if (_behaviour == null)
                return;

            _behaviour.Begin(targetCam, generation, IntervalSeconds, MaxAttempts);
        }

        /// <summary>Run EnsureLayout after current frame — avoids hitch on missile spawn callback.</summary>
        internal static void ScheduleEnsureLayoutNextFrame()
        {
            EnsureBehaviour();
            _behaviour?.BeginEnsureLayoutNextFrame();
        }

        internal static void KickEnsureLayoutNextFrame()
        {
            EnsureBehaviour();
            _behaviour?.KickEnsureLayoutNextFrame();
        }

        internal static void Cancel()
        {
            try
            {
                if (_behaviour != null)
                    _behaviour.StopRetry();
            }
            catch
            {
                // destroyed MonoBehaviour during scene unload
            }

            if (_behaviour == null)
                _behaviour = null;
        }

        /// <summary>Cancel + drop destroyed scene-local host. Safe during GameWorld unload.</summary>
        internal static void HardReset()
        {
            Cancel();

            try
            {
                if (_behaviour != null)
                    Object.Destroy(_behaviour.gameObject);
            }
            catch
            {
                // ignore
            }

            _behaviour = null;
        }

        private static void EnsureBehaviour()
        {
            if (_behaviour != null)
                return;

            var go = new GameObject("MissileCamera.Retry");
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            _behaviour = go.AddComponent<MfdLayoutRetryBehaviour>();
        }
    }

    internal sealed class MfdLayoutRetryBehaviour : MonoBehaviour
    {
        private TargetCam? _targetCam;
        private Coroutine? _retryCoroutine;
        private Coroutine? _ensureLayoutCoroutine;
        private float _intervalSeconds;
        private int _maxAttempts;
        private int _generation;

        internal void Begin(TargetCam targetCam, int generation, float intervalSeconds, int maxAttempts)
        {
            _targetCam = targetCam;
            _generation = generation;
            _intervalSeconds = intervalSeconds;
            _maxAttempts = maxAttempts;

            if (_retryCoroutine != null)
                return;

            _retryCoroutine = StartCoroutine(RetryLoop());
        }

        internal void BeginEnsureLayoutNextFrame()
        {
            // Allow re-schedule after a failed first attempt (previous coroutine finished null).
            if (_ensureLayoutCoroutine != null)
                return;

            _ensureLayoutCoroutine = StartCoroutine(EnsureLayoutNextFrame());
        }

        /// <summary>Force another next-frame EnsureLayout even if one already ran this session.</summary>
        internal void KickEnsureLayoutNextFrame()
        {
            try
            {
                if (_ensureLayoutCoroutine != null && this != null)
                    StopCoroutine(_ensureLayoutCoroutine);
            }
            catch { /* ignore */ }

            _ensureLayoutCoroutine = null;
            _ensureLayoutCoroutine = StartCoroutine(EnsureLayoutNextFrame());
        }

        internal void StopRetry()
        {
            try
            {
                if (_retryCoroutine != null && this != null)
                    StopCoroutine(_retryCoroutine);
            }
            catch
            {
                // ignore destroyed
            }

            try
            {
                if (_ensureLayoutCoroutine != null && this != null)
                    StopCoroutine(_ensureLayoutCoroutine);
            }
            catch
            {
                // ignore
            }

            _retryCoroutine = null;
            _ensureLayoutCoroutine = null;
            _targetCam = null;
        }

        private IEnumerator EnsureLayoutNextFrame()
        {
            yield return null;
            _ensureLayoutCoroutine = null;
            if (!MissileCameraHost.IsSessionActive)
                yield break;

            MfdLayoutController.EnsureLayoutForMissileFeed();
        }

        private IEnumerator RetryLoop()
        {
            int attempts = 0;
            while (attempts < _maxAttempts && _targetCam != null)
            {
                yield return new WaitForSecondsRealtime(_intervalSeconds);
                attempts++;

                if (!MissileCameraHost.IsMissionReady)
                    break;

                TargetCam? cam = _targetCam;
                if (cam == null)
                    break;

                MfdLayoutController.TryApplyLayoutFromRetry(cam, _generation);
            }

            _retryCoroutine = null;
            _targetCam = null;
        }
    }
}
