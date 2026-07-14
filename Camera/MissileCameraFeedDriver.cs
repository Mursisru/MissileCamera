using System.Collections;
using UnityEngine;

namespace MissileCamera
{
    internal sealed class MissileCameraFeedDriver : MonoBehaviour
    {
        private Coroutine? _loop;

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
                MissileCameraFeedController.Tick();
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
            Object.DontDestroyOnLoad(go);
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

            Object.Destroy(_driver.gameObject);
            _driver = null;
        }
    }
}
