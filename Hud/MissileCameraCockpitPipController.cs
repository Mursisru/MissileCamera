using UnityEngine;

namespace MissileCamera
{
    /// <summary>Reserved cockpit PiP (disabled while FLIR fullscreen HUD is active).</summary>
    internal static class MissileCameraCockpitPipController
    {
        private static MissileCameraAircraftRig? _rig;

        internal static bool IsActive => false;

        internal static void Tick(RectTransform? hudRoot, MissileCameraPanelMetrics panel)
        {
        }

        internal static void RenderIfDue(bool useSharedPrep)
        {
        }

        internal static void Shutdown()
        {
            _rig?.Destroy();
            _rig = null;
        }
    }
}
