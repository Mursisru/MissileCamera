using System;
using System.Globalization;
using System.IO;
using MissileCamera.Config;
using UnityEngine;

namespace MissileCamera
{
    internal static class MissileCameraHudConfig
    {
        private static DateTime _lastWriteUtc = DateTime.MinValue;

        internal static bool Enabled = true;
        internal static float SalvoWindowSeconds = 0.5f;
        internal static bool ShowCenterCluster = true;
        internal static bool ShowTargetMarker = true;
        internal static Color InterceptColor = new Color(0f, 1f, 0f, 1f);
        internal static Color ReticleColor = new Color(0f, 0.4f, 1f, 1f);
        internal static Color HorizonColor = new Color(0.05f, 0.35f, 0.08f, 1f);
        internal static Color HorizonFillColor = DeriveHorizonFillColor(new Color(0.2f, 1f, 0.25f, 1f));
        internal static Color HorizonOutlineColor = new Color(0.2f, 1f, 0.25f, 1f);
        internal static Color MissileNameColor = new Color(1f, 0f, 1f, 1f);
        internal static Color TargetNameColor = new Color(0.4f, 0.9f, 1f, 1f);
        internal static Color LabelBackgroundColor = new Color(0.18f, 0.18f, 0.18f, 0.62f);
        internal static float LabelBackgroundAlpha = 0.62f;
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
            string? location = typeof(MissileCameraHudConfig).Assembly.Location;
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
            Enabled = cfg.GetBool("MissileCameraHud", "Enabled", true);
            SalvoWindowSeconds = MathfClamp(cfg.GetFloat("MissileCameraHud", "SalvoWindowSeconds", 0.5f), 0.05f, 5f);
            ShowCenterCluster = cfg.GetBool("MissileCameraHud", "ShowCenterCluster", true);
            ShowTargetMarker = cfg.GetBool("MissileCameraHud", "ShowTargetMarker", true);
            InterceptColor = ParseColor(cfg.GetString("MissileCameraHud", "InterceptColor", "0,1,0,1"), InterceptColor);
            ReticleColor = ParseColor(cfg.GetString("MissileCameraHud", "ReticleColor", "0,0.4,1,1"), ReticleColor);
            HorizonColor = ParseColor(cfg.GetString("MissileCameraHud", "HorizonColor", "0.05,0.35,0.08,1"), HorizonColor);
            HorizonOutlineColor = ParseColor(
                cfg.GetString("MissileCameraHud", "HorizonOutlineColor", "0.2,1,0.25,1"),
                HorizonOutlineColor);
            HorizonFillColor = DeriveHorizonFillColor(HorizonOutlineColor);
            MissileNameColor = ParseColor(cfg.GetString("MissileCameraHud", "MissileNameColor", "1,0,1,1"), MissileNameColor);
            TargetNameColor = ParseColor(cfg.GetString("MissileCameraHud", "TargetNameColor", "0.4,0.9,1,1"), TargetNameColor);
            LabelBackgroundColor = ParseColor(cfg.GetString("MissileCameraHud", "LabelBackgroundColor", "0.18,0.18,0.18,0.62"), LabelBackgroundColor);
            LabelBackgroundAlpha = MathfClamp(cfg.GetFloat("MissileCameraHud", "LabelBackgroundAlpha", 0.62f), 0f, 1f);
        }

        private static Color ParseColor(string raw, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return fallback;

            string[] parts = raw.Split(',');
            if (parts.Length < 3)
                return fallback;

            if (!TryParse(parts[0], out float r)
                || !TryParse(parts[1], out float g)
                || !TryParse(parts[2], out float b))
                return fallback;

            float a = parts.Length > 3 && TryParse(parts[3], out float parsedA) ? parsedA : 1f;
            return new Color(r, g, b, a);
        }

        private static bool TryParse(string raw, out float value) =>
            float.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        private static float MathfClamp(float value, float min, float max) =>
            value < min ? min : value > max ? max : value;

        internal static Color DeriveHorizonFillColor(Color outline)
        {
            Color.RGBToHSV(outline, out float h, out float s, out float v);
            v = Mathf.Clamp01(v - 0.18f);
            s = Mathf.Clamp01(s * 0.95f);
            Color fill = Color.HSVToRGB(h, s, v);
            fill.a = outline.a;
            return fill;
        }
    }
}
