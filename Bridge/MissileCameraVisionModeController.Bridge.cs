namespace MissileCamera
{
    // Continued from Camera/MissileCameraVisionModeController.cs
    internal static partial class MissileCameraVisionModeController
    {
        /// <summary>Direct set (Bridge/McBridge.cs SetVisionMode) — same effect as landing on this
        /// mode via Cycle(), just without stepping through the ones in between.</summary>
        internal static void Set(MissileCameraVisionMode mode)
        {
            if (mode == _mode)
                return;
            _mode = mode;
            MfdLog.Info("vision mode → " + _mode);
        }
    }
}
