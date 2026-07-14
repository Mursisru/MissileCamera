using UnityEngine;

namespace MissileCamera
{
    /// <summary>
    /// Serializes multi-camera manual renders in one frame:
    /// Prepare each camera, never restore main until Finish.
    /// Missile then aircraft both share this gate (separate RTs).
    /// </summary>
    internal static class MissileCameraFrameRenderContext
    {
        private static bool _multiActive;
        private static int _preparedCount;

        internal static bool IsMultiRenderActive => _multiActive;

        internal static void BeginMultiRender()
        {
            _multiActive = true;
            _preparedCount = 0;
        }

        internal static void PrepareCamera(Camera camera, bool forceLdr = false)
        {
            if (camera == null)
                return;

            MissileCameraRenderPrep.BeforeRender(camera, forceLdr);
            _preparedCount++;
        }

        internal static void FinishMultiRender()
        {
            if (!_multiActive)
                return;

            if (_preparedCount > 0)
                MissileCameraRenderPrep.AfterRender();

            _multiActive = false;
            _preparedCount = 0;
        }
    }
}
