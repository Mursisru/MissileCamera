using System;
using System.IO;
using MissileCamera.Config;

namespace MissileCamera
{
    internal static class MfdLayoutConfig
    {
        private static string _modRoot = string.Empty;
        private static DateTime _lastWriteUtc = DateTime.MinValue;

        internal static bool Enabled = true;
        internal static string DisplayMode = "split";
        internal static float OverlayMaxWidth = 0.45f;
        internal static float LeftWidth = 0.58f;
        internal static float MissilePanelBottom = 0.38f;
        internal static float WeaponsStripHeight = 0.12f;
        internal static bool ShowDivider = true;
        internal static bool DebugStub;
        internal static string StubLabel = "MISSILE CAMERA";
        internal static int Revision;

        internal static void Init(string modRoot)
        {
            _modRoot = modRoot;
            _lastWriteUtc = DateTime.MinValue;
            Refresh(force: true);
        }

        internal static void EnsureInitialized()
        {
            if (!string.IsNullOrEmpty(_modRoot))
                return;

            if (!string.IsNullOrEmpty(ModPaths.PluginDir))
            {
                Init(ModPaths.PluginDir);
                return;
            }

            string? location = typeof(MfdLayoutConfig).Assembly.Location;
            if (!string.IsNullOrEmpty(location))
            {
                string? dir = Path.GetDirectoryName(location);
                if (!string.IsNullOrEmpty(dir))
                {
                    Init(dir);
                    return;
                }
            }
        }

        internal static void Refresh(bool force = false)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(_modRoot))
                return;

            string path = Path.Combine(_modRoot, "mod_config.ini");
            if (!File.Exists(path))
                return;

            DateTime writeUtc = File.GetLastWriteTimeUtc(path);
            if (!force && writeUtc <= _lastWriteUtc)
                return;

            _lastWriteUtc = writeUtc;
            Revision++;
            Load(ModIniConfig.Load(_modRoot));
        }

        private static void Load(ModIniConfig cfg)
        {
            Enabled = cfg.GetBool("Layout", "Enabled", true);
            DisplayMode = cfg.GetString("Layout", "DisplayMode", "split");
            OverlayMaxWidth = Clamp01(cfg.GetFloat("Layout", "OverlayMaxWidth", 0.45f));
            LeftWidth = Clamp01(cfg.GetFloat("Layout", "LeftWidth", 0.58f));
            MissilePanelBottom = Clamp01(cfg.GetFloat("Layout", "MissilePanelBottom", 0.38f));
            WeaponsStripHeight = Clamp01(cfg.GetFloat("Layout", "WeaponsStripHeight", 0.12f));
            ShowDivider = cfg.GetBool("Layout", "ShowDivider", true);
            DebugStub = cfg.GetBool("Layout", "DebugStub", false);
            StubLabel = cfg.GetString("Layout", "StubLabel", "MISSILE CAMERA");
        }

        private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
