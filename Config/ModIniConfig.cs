using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace MissileCamera.Config
{
    /// <summary>INI reader for mod_config.ini next to the plugin DLL.</summary>
    public sealed class ModIniConfig
    {
        private readonly Dictionary<string, Dictionary<string, string>> _sections =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public static ModIniConfig Load(string modRoot, string fileName = "mod_config.ini")
        {
            var path = Path.Combine(modRoot, fileName);
            var cfg = new ModIniConfig();
            if (!File.Exists(path))
                return cfg;

            string currentSection = string.Empty;
            foreach (var rawLine in File.ReadAllLines(path))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith(";", StringComparison.Ordinal))
                    continue;

                if (line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal))
                {
                    currentSection = line.Substring(1, line.Length - 2).Trim();
                    cfg.EnsureSection(currentSection);
                    continue;
                }

                var eq = line.IndexOf('=');
                if (eq <= 0)
                    continue;

                var key = line.Substring(0, eq).Trim();
                var value = line.Substring(eq + 1).Trim();
                cfg.Set(currentSection, key, value);
            }

            return cfg;
        }

        public static void EnsureDefault(string modRoot, string embeddedDefaults, string fileName = "mod_config.ini")
        {
            var path = Path.Combine(modRoot, fileName);
            if (File.Exists(path))
                return;

            Directory.CreateDirectory(modRoot);
            File.WriteAllText(path, embeddedDefaults, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        public bool GetBool(string section, string key, bool defaultValue)
        {
            if (!TryGet(section, key, out var raw))
                return defaultValue;
            if (bool.TryParse(raw, out var b))
                return b;
            if (raw == "1") return true;
            if (raw == "0") return false;
            return defaultValue;
        }

        public int GetInt(string section, string key, int defaultValue)
        {
            if (!TryGet(section, key, out var raw))
                return defaultValue;
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : defaultValue;
        }

        public float GetFloat(string section, string key, float defaultValue)
        {
            if (!TryGet(section, key, out var raw))
                return defaultValue;
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : defaultValue;
        }

        public string GetString(string section, string key, string defaultValue)
        {
            return TryGet(section, key, out var raw) ? raw : defaultValue;
        }

        private void EnsureSection(string section)
        {
            if (!_sections.ContainsKey(section))
                _sections[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        private void Set(string section, string key, string value)
        {
            EnsureSection(section);
            _sections[section][key] = value;
        }

        private bool TryGet(string section, string key, out string value)
        {
            value = string.Empty;
            if (!_sections.TryGetValue(section, out var map))
                return false;
            return map.TryGetValue(key, out value!);
        }
    }
}
