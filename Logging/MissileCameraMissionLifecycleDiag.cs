using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MissileCamera
{
    /// <summary>
    /// TEMP detailed lifecycle diagnostics for multi-sortie MFD/marker bugs.
    /// Writes BepInEx/plugins/MissileCamera.lifecycle.diag.log (and mirrors to MfdLog).
    /// Remove or set Enabled=false after the issues are confirmed fixed.
    /// </summary>
    internal static class MissileCameraMissionLifecycleDiag
    {
        // TEMP — turn off once multi-sortie MFD + missile markers are verified stable.
        internal const bool Enabled = true;

        private static readonly object Gate = new object();
        private static readonly Dictionary<string, float> ThrottleUntil = new Dictionary<string, float>(16);
        private static string _logPath = string.Empty;
        private static bool _initialized;

        internal static string LogPath => _logPath;

        internal static void Init(string pluginDir)
        {
            if (!Enabled || string.IsNullOrEmpty(pluginDir))
                return;

            try
            {
                _logPath = Path.Combine(pluginDir, "MissileCamera.lifecycle.diag.log");
                _initialized = true;
                WriteRaw(
                    "==== lifecycle diag start"
                    + " v" + AppVersion.DisplayVersion
                    + " utc=" + DateTime.UtcNow.ToString("o")
                    + " ====");
            }
            catch (Exception ex)
            {
                _initialized = false;
                MfdLog.Warning("lifecycle diag init failed: " + ex.Message);
            }
        }

        internal static void Info(string message) => Write("INFO", message);

        internal static void Warn(string message) => Write("WARN", message);

        internal static void WarnThrottled(string key, string message, float intervalSec = 2f)
        {
            if (!Enabled || !_initialized || string.IsNullOrEmpty(key))
                return;

            float now = 0f;
            try { now = Time.unscaledTime; }
            catch { return; }

            if (ThrottleUntil.TryGetValue(key, out float until) && now < until)
                return;

            ThrottleUntil[key] = now + intervalSec;
            Warn(message);
        }

        internal static void Snapshot(string tag)
        {
            if (!Enabled || !_initialized)
                return;

            Scene scene = SceneManager.GetActiveScene();
            string sceneLabel = string.IsNullOrEmpty(scene.path) ? scene.name : scene.path;
            Write(
                "SNAP",
                tag
                + " session=" + MissileCameraHost.IsSessionActive
                + " missionReady=" + MissileCameraHost.IsMissionReady
                + " teardown=" + MissileCameraHost.IsTeardownInProgress
                + " teardownEpoch=" + MissileCameraHost.TeardownEpoch
                + " layout=" + MfdLayoutController.IsLayoutActive
                + " overlay=" + MissileCameraFeedController.IsOverlayActiveForDiag
                + " fs=" + MissileCameraFullscreenController.IsActive
                + " scene=" + sceneLabel);
        }

        private static void Write(string level, string message)
        {
            if (!Enabled || !_initialized || string.IsNullOrEmpty(message))
                return;

            float t = 0f;
            int frame = 0;
            try
            {
                t = Time.unscaledTime;
                frame = Time.frameCount;
            }
            catch { /* quit */ }

            string line = "[" + DateTime.UtcNow.ToString("HH:mm:ss.fff")
                + " t=" + t.ToString("F3")
                + " f=" + frame
                + "] " + level + " " + message;

            WriteRaw(line);

            if (string.Equals(level, "WARN", StringComparison.Ordinal))
                MfdLog.Warning("[lifecycle] " + message);
            else
                MfdLog.Info("[lifecycle] " + message);
        }

        private static void WriteRaw(string line)
        {
            lock (Gate)
            {
                try
                {
                    File.AppendAllText(_logPath, line + Environment.NewLine, Encoding.UTF8);
                }
                catch
                {
                    // Never break gameplay for diag I/O.
                }
            }
        }
    }
}
