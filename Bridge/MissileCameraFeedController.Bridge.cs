using MissileCamera.Bridge;

namespace MissileCamera
{
    // Continued from Camera/MissileCameraFeedController.cs
    internal static partial class MissileCameraFeedController
    {
        // Set by RequestCapture (below) when an external consumer wants live frames but neither
        // the cockpit MFD panel nor Fullscreen is up.
        private static bool _bridgeCaptureActive;

        // Idempotent — safe to call every frame with the same value ("do I still want frames"
        // polled continuously, not an edge-triggered toggle).
        internal static void SetBridgeCaptureActive(bool active)
        {
            MissileCameraBridgeConfig.Refresh();
            if (active && !MissileCameraBridgeConfig.Enabled)
                active = false;

            bool was = _bridgeCaptureActive;
            _bridgeCaptureActive = active;

            if (!was && active)
                SuppressCockpitMfdForBridge();
        }

        // Read side — used by MissileCameraFeedConfig.ResolveActiveFeedSize to decide feed
        // resolution: an external bridge consumer counts as "wants fullscreen-grade quality", the
        // same as Fullscreen itself, rather than falling back to the small cockpit-panel size.
        internal static bool IsBridgeCaptureActive => _bridgeCaptureActive;

        internal static bool BridgeSuppressesCockpitDisplay()
        {
            MissileCameraBridgeConfig.Refresh();
            return _bridgeCaptureActive
                && MissileCameraBridgeConfig.SuppressCockpitMfd
                && !MissileCameraFullscreenController.IsActive;
        }

        // Hide cockpit MFD UI while bridge renders — rig stays attached for headless feed.
        internal static void SuppressCockpitMfdForBridge()
        {
            if (!BridgeSuppressesCockpitDisplay())
                return;

            if (!_overlayActive && !MfdLayoutController.IsLayoutActive)
                return;

            if (_overlayActive)
            {
                _overlayActive = false;

                try { CancelPostLossSequence(); }
                catch { /* ignore */ }

                try { MissileCameraInfraredEffect.Clear(_feedImage, _rig); }
                catch { /* ignore */ }

                try { MissileCameraPostFxStack.Release(); }
                catch { /* ignore */ }

                _cachedPanelW = -1f;
                _cachedPanelH = -1f;

                try { HudOverlay.Park(); }
                catch { /* ignore */ }

                _manualFollowActive = false;
                _zoomOffset = 0f;
            }

            try { MfdLayoutController.ReleaseForExternalBridge(); }
            catch { /* ignore */ }
        }

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
