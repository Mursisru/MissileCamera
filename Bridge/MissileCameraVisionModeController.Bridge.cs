namespace MissileCamera
{
    // Partial-class extension of Camera/MissileCameraVisionModeController.cs — the
    // external-consumer half lives here so that file stays Mursisru's own vision-cycle code with
    // nothing added inline. Shares the private _mode field via the partial class.
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
