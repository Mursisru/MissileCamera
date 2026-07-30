using System.Collections;
using UnityEngine;

namespace MissileCamera
{
    internal static class MfdLayoutRetryHost
    {
        private const float IntervalSeconds = 0.5f;
        private const int MaxAttempts = 2;

        private static MfdLayoutRetryBehaviour? _behaviour;

        internal static void Schedule(TargetCam targetCam, int generation)
        {
            if (_behaviour == null)
            {
                var go = new GameObject("MissileCamera.Retry");
                _behaviour = go.AddComponent<MfdLayoutRetryBehaviour>();
            }

            _behaviour.Begin(targetCam, generation, IntervalSeconds, MaxAttempts);
        }

        internal static void Cancel()
        {
            _behaviour?.StopRetry();
        }
    }

    internal sealed class MfdLayoutRetryBehaviour : MonoBehaviour
    {
        private TargetCam? _targetCam;
        private Coroutine? _retryCoroutine;
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

        internal void StopRetry()
        {
            if (_retryCoroutine != null)
            {
                StopCoroutine(_retryCoroutine);
                _retryCoroutine = null;
            }

            _targetCam = null;
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
