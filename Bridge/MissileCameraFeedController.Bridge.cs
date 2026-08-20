namespace MissileCamera
{
    // Partial-class extension of Camera/MissileCameraFeedController.cs — the external-consumer
    // half lives here: the _bridgeCaptureActive flag's own accessors, and the uncached telemetry
    // builder a headless bridge caller needs. The three places the existing pipeline logic itself
    // has to widen to account for _bridgeCaptureActive (reset, vision-mode gating,
    // IsDisplayPipelineActive) stay in the other file, since they're edits inside Mursisru's own
    // existing methods rather than new standalone members.
    internal static partial class MissileCameraFeedController
    {
        // Set by RequestCapture (below) when an external consumer — e.g. NOXMFD's browser MFD —
        // wants live frames but neither the cockpit MFD panel nor Fullscreen is up. Read directly
        // by the other half of this partial class at the three points noted above.
        private static bool _bridgeCaptureActive;

        // Bridge/McBridge.RequestCapture forwards here. Idempotent — safe to call every frame
        // with the same value (which is exactly how RcFeed-style callers use it: "do I still want
        // frames" polled continuously, not an edge-triggered toggle).
        internal static void SetBridgeCaptureActive(bool active) => _bridgeCaptureActive = active;

        // Read side — used by MissileCameraFeedConfig.ResolveActiveFeedSize to decide feed
        // resolution: an external bridge consumer counts as "wants fullscreen-grade quality", the
        // same as Fullscreen itself, rather than falling back to the small cockpit-panel size.
        internal static bool IsBridgeCaptureActive => _bridgeCaptureActive;

        /// <summary>Fresh (uncached) telemetry snapshot for an external consumer — see
        /// Bridge/McBridge.cs TelemetryJson. Deliberately bypasses ResolveHudSnapshot's cache: that
        /// cache is refreshed from call sites tied to a real UI panel/Fullscreen being drawn, which
        /// a headless bridge-only caller wouldn't otherwise trigger.</summary>
        internal static MissileCameraHudSnapshot? TryBuildTelemetry()
        {
            if (_rig == null || !_rig.IsRootAlive)
                return null;

            Missile? missile = TryGetFollowedMissile();
            if (missile == null)
                return null;

            try
            {
                return MissileCameraHudSnapshot.Build(missile, _rig, OwnedActive);
            }
            catch
            {
                // Headless external callers (Bridge/McBridge.cs) can land here between an ordinary
                // per-frame Update and a missile despawning/disabling mid-read — Build() isn't
                // written to expect that race (its other call sites all run inside Tick(), already
                // past a fresher disabled-check that frame). Null here is a normal "nothing to show
                // this tick" from the caller's perspective, not a fatal error.
                return null;
            }
        }
    }
}
