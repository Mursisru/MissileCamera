using UnityEngine;

namespace MissileCamera.Bridge
{
    /// <summary>
    /// Small, stable, PUBLIC surface for third-party mods to read the missile feed and keep it
    /// live without needing the cockpit MFD panel bound or Fullscreen active. RequestCapture adds
    /// a third, independent reason for the feed pipeline to stay live (see
    /// MissileCameraFeedController.IsDisplayPipelineActive / CAMERA_SAFETY.md — this never touches
    /// vanilla cockpit camera state, only the dedicated feed RT/RawImage path that already existed).
    ///
    /// Contract: every member no-ops / returns a safe default when there's no trackable missile or
    /// nobody has called RequestCapture(true) yet, rather than throwing — callers don't need a
    /// try/catch per call. Everything here must be called from the Unity main thread.
    /// </summary>
    public static class McBridge
    {
        /// <summary>Bump on breaking changes only (removed/renamed/resignatured members).</summary>
        public const int ApiVersion = 1;

        /// <summary>An owned missile this mod could track exists right now (independent of
        /// whether anything is currently displaying it).</summary>
        public static bool HasTrackableMissile => MissileCameraFeedController.HasTrackableOwnedMissile();

        /// <summary>The feed camera for the currently-followed missile. Populated once the feed
        /// pipeline is live (cockpit panel bound, Fullscreen active, OR RequestCapture(true) —
        /// see below) AND a trackable missile exists; null otherwise. Same Camera object the
        /// cockpit MFD panel and Fullscreen already render from.</summary>
        public static Camera? FeedCamera => MissileCameraFeedController.TryGetFeedCamera();

        /// <summary>The actual authoritative output texture — same one the cockpit MFD panel /
        /// Fullscreen RawImage displays (already tonemapped from the internal HDR pass when
        /// applicable). Prefer this over reading FeedCamera.targetTexture yourself: the camera's
        /// targetTexture is swapped to an intermediate HDR buffer during part of the render and
        /// restored afterward, so sampling it directly can catch a transient/stale value depending
        /// on exactly when you read it.</summary>
        public static Texture? FeedTexture => MissileCameraFeedController.TryGetFeedTexture();

        /// <summary>The missile the feed is currently following (auto-selected — same "latest
        /// owned missile" logic Fullscreen/the cockpit panel already use).</summary>
        public static Missile? FollowedMissile => MissileCameraFeedController.TryGetFollowedMissile();

        /// <summary>Manual vision-mode palette (Fullscreen's own J-key cycle — Color, NightVision,
        /// WhiteHot, BlackHot, WhiteContour, BlackContour). One shared value: setting it here
        /// affects real Fullscreen too if the pilot enters it, exactly like pressing J would.</summary>
        public static string VisionMode => MissileCameraVisionModeController.Mode.ToString();

        /// <summary>Set the palette by name (same names as VisionMode above). Unrecognized names
        /// are ignored (no-op) rather than throwing.</summary>
        public static void SetVisionMode(string name)
        {
            if (System.Enum.TryParse(name, out MissileCameraVisionMode mode)
                && System.Enum.IsDefined(typeof(MissileCameraVisionMode), mode))
            {
                MissileCameraVisionModeController.Set(mode);
            }
        }

        /// <summary>Cockpit HUD target markers, reprojected onto FeedCamera's viewport — same
        /// marker set (and vanilla faction color) Fullscreen shows reprojected onto its own view.
        /// JSON array, each entry { n (unit name), x, y (viewport 0..1, Unity convention — y=0 is
        /// bottom, flip for CSS top like the aim reticle), sel (bool — this is the locked/selected
        /// target), c (hex color, e.g. "#ff3b30") }. Empty array (not null) when there's nothing to
        /// show — no trackable missile, no feed camera yet, or no markers currently on-screen.
        /// Rebuilt from scratch on every call (walks CombatHUD's live marker list) — a consumer
        /// polling every frame should throttle rather than call this on a tight loop.</summary>
        public static string MarkersJson() =>
            MissileCameraCombatHudMarkerProjection.BuildMarkersJson(MissileCameraFeedController.TryGetFeedCamera());

        /// <summary>Missile telemetry as a compact JSON object — same pre-formatted strings the
        /// cockpit MFD panel / Fullscreen HUD text renders (SpeedText, AltitudeText, etc. — same
        /// units/rounding/labels, so a consumer's readout matches the in-game one exactly rather
        /// than reformatting the underlying numbers itself). Never null — see visionMode note below
        /// for what you get when there's no trackable missile.
        /// Fields: missile, target, ownship (names) · speed, alt, range, g, fuel, mach, guidance,
        /// tgtAngle (pre-formatted text) · hasTarget, hasTti (bools) · ttiSec, ttiFrac, closMs,
        /// relAltM (floats, meaningful only when hasTarget/hasTti as noted) · infrared (bool) ·
        /// salvoIdx, salvoTotal (ints, 0 when not part of a salvo) · visionMode (pre-formatted
        /// text, e.g. "MODE: IR" — same label Fullscreen's own HUD shows; see CycleVisionMode).
        /// visionMode is included even when everything else is unavailable — the mode is a global
        /// selection, not something that needs a trackable missile — so this method still returns
        /// a (partial) object with just that field set when there's no missile, rather than null.
        /// Rebuilt (string-formatted) on every call — a consumer polling every frame should
        /// throttle rather than call this on a tight loop.</summary>
        public static string TelemetryJson()
        {
            string visionMode = Esc(MissileCameraVisionModeController.ModeLabel(MissileCameraVisionModeController.Mode));

            MissileCameraHudSnapshot? snap = MissileCameraFeedController.TryBuildTelemetry();
            if (snap == null)
                return "{\"visionMode\":\"" + visionMode + "\"}";

            MissileCameraHudSnapshot s = snap.Value;
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{{\"missile\":\"{0}\",\"target\":\"{1}\",\"ownship\":\"{2}\"," +
                "\"speed\":\"{3}\",\"alt\":\"{4}\",\"range\":\"{5}\",\"g\":\"{6}\",\"fuel\":\"{7}\",\"mach\":\"{8}\"," +
                "\"guidance\":\"{9}\",\"tgtAngle\":\"{10}\"," +
                "\"hasTarget\":{11},\"hasTti\":{12},\"ttiSec\":{13:0.0},\"ttiFrac\":{14:0.000}," +
                "\"closMs\":{15:0.0},\"relAltM\":{16:0.0},\"infrared\":{17},\"salvoIdx\":{18},\"salvoTotal\":{19}," +
                "\"visionMode\":\"{20}\"}}",
                Esc(s.MissileName), Esc(s.TargetName), Esc(s.OwnshipName),
                Esc(s.SpeedText), Esc(s.AltitudeText), Esc(s.RangeText), Esc(s.GText), Esc(s.FuelText), Esc(s.MachText),
                Esc(s.GuidanceText), Esc(s.TargetAngleText),
                s.HasTarget ? "true" : "false", s.HasTimeToImpact ? "true" : "false", s.TimeToImpactSec, s.TimeToImpactFraction,
                s.ClosingSpeedMs, s.RelativeAltitudeMeters, s.InfraredActive ? "true" : "false", s.SalvoIndex, s.SalvoTotal,
                visionMode);
        }

        // Shared with Fullscreen/MissileCameraCombatHudMarkerProjection.cs's own JSON producer —
        // one escape helper for both, not two copies.
        private static string Esc(string? s) => MissileCameraCombatHudMarkerProjection.EscapeJson(s);

        /// <summary>Cycle the Fullscreen-style vision filter (Color → NightVision → WhiteHot →
        /// BlackHot → WhiteContour → BlackContour → ...) — same cycle the in-game J key drives.
        /// Affects headless bridge capture too: RequestCapture(true) now follows this mode rather
        /// than lighting-only auto-IR (see MissileCameraFeedController.Tick
        /// "visionUsesFullscreenMode"), so switching it here changes what the MFD shows.</summary>
        public static void CycleVisionMode() => MissileCameraVisionModeController.Cycle();

        /// <summary>Keep the feed pipeline live for an external consumer, independent of the
        /// cockpit MFD panel and Fullscreen. Level-triggered, not edge-triggered — call every tick
        /// with your current "do I still need frames" state (RequestCapture(false) once nobody's
        /// watching), the same way the cockpit panel's own binding is a live state, not a one-shot
        /// trigger. Harmless to call with the same value repeatedly.</summary>
        public static void RequestCapture(bool active) => MissileCameraFeedController.SetBridgeCaptureActive(active);

        /// <summary>True while at least one consumer currently has capture active via
        /// RequestCapture(true) above. Distinct from HasTrackableMissile — that's true whenever
        /// ANY owned missile is trackable, regardless of whether anyone is watching; this is the
        /// actual "a bridge consumer is keeping the feed alive right now" signal. Anything gating
        /// its own behavior on "is a headless bridge consumer active" should use this, not
        /// HasTrackableMissile.</summary>
        public static bool IsCaptureActive => MissileCameraFeedController.IsBridgeCaptureActive;
    }
}
