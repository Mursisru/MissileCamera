using System;
using System.Collections;
using UnityEngine;

namespace MissileCamera
{
    internal sealed class MissileCameraFeedDriver : MonoBehaviour
    {
        private Coroutine? _loop;

        private void Update()
        {
            // Mod keys must poll in Update — Tick runs WaitForEndOfFrame (or 0.2s idle),
            // which felt like input lag vs vanilla Rewired.
            try { MissileCameraFeedController.PollInputEarly(); }
            catch { /* never kill driver */ }
        }

        private void OnEnable()
        {
            if (_loop == null)
                _loop = StartCoroutine(RenderLoop());
        }

        private void OnDisable()
        {
            if (_loop != null)
            {
                StopCoroutine(_loop);
                _loop = null;
            }
        }

        private void OnDestroy() => MissileCameraFeedController.Shutdown();

        private static IEnumerator RenderLoop()
        {
            var endOfFrame = new WaitForEndOfFrame();
            var idleWait = new WaitForSeconds(0.2f);
            while (true)
            {
                // One Tick exception must NEVER kill the DDOL feed loop for the whole mission.
                try
                {
                    MissileCameraFeedController.Tick();
                }
                catch (Exception ex)
                {
                    MissileCameraMissionLifecycleDiag.Warn(
                        "FeedDriver Tick exception: " + ex.GetType().Name + ": " + ex.Message);
                    try { MissileCameraFeedController.HealAfterTickFailure(); }
                    catch { /* ignore */ }
                }

                if (MissileCameraFeedController.UseIdleDriverWait)
                    yield return idleWait;
                else
                    yield return endOfFrame;
            }
        }
    }

    internal static class MissileCameraFeedDriverHost
    {
        private static MissileCameraFeedDriver? _driver;

        internal static void Ensure()
        {
            if (_driver != null)
                return;

            var go = new GameObject("MissileCamera.Feed");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _driver = go.AddComponent<MissileCameraFeedDriver>();
        }

        internal static Coroutine? StartCoroutineSafe(IEnumerator routine)
        {
            Ensure();
            return _driver != null ? _driver.StartCoroutine(routine) : null;
        }

        internal static void StopCoroutineSafe(Coroutine? routine)
        {
            if (_driver == null || routine == null)
                return;

            _driver.StopCoroutine(routine);
        }

        internal static void Shutdown()
        {
            MissileCameraFullscreenBootstrap.ResetForMissionUnload();
            MissileCameraFeedController.Shutdown();
            if (_driver == null)
                return;

            UnityEngine.Object.Destroy(_driver.gameObject);
            _driver = null;
        }
    }
}
