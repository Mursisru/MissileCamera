using System;
using System.IO;
using MissileCamera.Config;
using UnityEngine;

namespace MissileCamera
{
    internal static class MissileCameraFeedConfig
    {
        private static DateTime _lastWriteUtc = DateTime.MinValue;

        internal static bool Enabled = true;
        internal static float NoseSkinInset = 0.08f;
        internal static float CameraBackOffset = 0.35f;
        internal static float Fov = 60f;
        internal static int FeedWidth = 512;
        internal static int FeedHeight = 512;
        internal static bool HorizonLock = true;
        internal static float TurnLookBankScale = 1f;
        internal static float MaxTurnLookDegrees = 90f;
        internal static float DefaultMissileGLimit = 20f;
        internal static float TurnLookGDeadband = 0.15f;
        internal static float TurnLookGFilterHz = 7f;
        internal static float TurnLookSlewDegPerSec = 120f;
        internal static float TurnLookSmoothTime = 0.18f;
        internal static float PostExplosionHoldSeconds;
        internal static int RenderFps = 30;
        internal static int Revision;

        internal static void Refresh(bool force = false)
        {
            MfdLayoutConfig.EnsureInitialized();
            string modRoot = GetModRoot();
            if (string.IsNullOrEmpty(modRoot))
                return;

            string path = Path.Combine(modRoot, "mod_config.ini");
            if (!File.Exists(path))
                return;

            DateTime writeUtc = File.GetLastWriteTimeUtc(path);
            if (!force && writeUtc <= _lastWriteUtc)
                return;

            _lastWriteUtc = writeUtc;
            Revision++;
            Load(ModIniConfig.Load(modRoot));
        }

        private static string GetModRoot()
        {
            string? location = typeof(MissileCameraFeedConfig).Assembly.Location;
            if (!string.IsNullOrEmpty(location))
            {
                string? dir = Path.GetDirectoryName(location);
                if (!string.IsNullOrEmpty(dir))
                    return dir;
            }

            return string.Empty;
        }

        private static void Load(ModIniConfig cfg)
        {
            Enabled = cfg.GetBool("MissileCameraFeed", "Enabled", true);
            NoseSkinInset = MathfClamp(cfg.GetFloat("MissileCameraFeed", "NoseSkinInset", 0.08f), 0.01f, 2f);
            CameraBackOffset = MathfClamp(cfg.GetFloat("MissileCameraFeed", "CameraBackOffset", 0.35f), 0.01f, 5f);
            Fov = MathfClamp(cfg.GetFloat("MissileCameraFeed", "Fov", 60f), 10f, 120f);
            FeedWidth = MathfClampInt(cfg.GetInt("MissileCameraFeed", "FeedWidth", 512), 128, 2048);
            FeedHeight = MathfClampInt(cfg.GetInt("MissileCameraFeed", "FeedHeight", 512), 128, 2048);
            HorizonLock = cfg.GetBool("MissileCameraFeed", "HorizonLock", true);
            TurnLookBankScale = MathfClamp(cfg.GetFloat("MissileCameraFeed", "TurnLookBankScale", 1f), 0f, 1.5f);
            MaxTurnLookDegrees = MathfClamp(cfg.GetFloat("MissileCameraFeed", "MaxTurnLookDegrees", 90f), 10f, 90f);
            DefaultMissileGLimit = MathfClamp(cfg.GetFloat("MissileCameraFeed", "DefaultMissileGLimit", 20f), 1f, 100f);
            TurnLookGDeadband = MathfClamp(cfg.GetFloat("MissileCameraFeed", "TurnLookGDeadband", 0.15f), 0f, 5f);
            TurnLookGFilterHz = MathfClamp(cfg.GetFloat("MissileCameraFeed", "TurnLookGFilterHz", 7f), 1f, 30f);
            TurnLookSlewDegPerSec = MathfClamp(cfg.GetFloat("MissileCameraFeed", "TurnLookSlewDegPerSec", 120f), 10f, 720f);
            TurnLookSmoothTime = MathfClamp(cfg.GetFloat("MissileCameraFeed", "TurnLookSmoothTime", 0.18f), 0.02f, 1.5f);
            PostExplosionHoldSeconds = MathfClamp(cfg.GetFloat("MissileCameraFeed", "PostExplosionHoldSeconds", 0f), 0f, 10f);
            RenderFps = MathfClampInt(cfg.GetInt("MissileCameraFeed", "RenderFps", 30), 5, 60);
        }

        private static float MathfClamp(float value, float min, float max) =>
            value < min ? min : value > max ? max : value;

        private static int MathfClampInt(int value, int min, int max) =>
            value < min ? min : value > max ? max : value;
    }
}
