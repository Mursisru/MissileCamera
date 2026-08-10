using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using MissileCamera.Config;

namespace MissileCamera
{
    /// <summary>
    /// Compares AppVersion to GitHub latest non-prerelease. Failures stay silent (no UI).
    /// </summary>
    internal static class MissileCameraUpdateChecker
    {
        private const string ReleasesLatestUrl =
            "https://api.github.com/repos/Mursisru/MissileCamera/releases/latest";
        private const int TimeoutMs = 8000;

        private static int _started;
        private static volatile bool _completed;
        private static volatile bool _outdated;
        private static string _latestTag = string.Empty;
        private static string _releaseUrl = string.Empty;

        internal static bool IsCompleted => _completed;
        internal static bool IsOutdated => _outdated;
        internal static string LatestTag => _latestTag;
        internal static string ReleaseUrl => _releaseUrl;

        internal static void StartIfNeeded()
        {
            if (Interlocked.Exchange(ref _started, 1) != 0)
                return;

            if (!MissileCameraBepInConfig.IsBound
                || !MissileCameraBepInConfig.CheckForUpdates.Value
                || MissileCameraBepInConfig.UpdatePromptDontShowAgain.Value)
            {
                _completed = true;
                _outdated = false;
                return;
            }

            ThreadPool.QueueUserWorkItem(_ => RunCheck());
        }

        private static void RunCheck()
        {
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                var req = (HttpWebRequest)WebRequest.Create(ReleasesLatestUrl);
                req.Method = "GET";
                req.Timeout = TimeoutMs;
                req.ReadWriteTimeout = TimeoutMs;
                req.UserAgent = "MissileCamera/" + AppVersion.DisplayVersion;
                req.Accept = "application/vnd.github+json";
                req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                using var resp = (HttpWebResponse)req.GetResponse();
                if (resp.StatusCode != HttpStatusCode.OK)
                    return;

                string json;
                using (var stream = resp.GetResponseStream())
                {
                    if (stream == null)
                        return;
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    json = reader.ReadToEnd();
                }

                if (string.IsNullOrEmpty(json))
                    return;

                string tag = ExtractJsonString(json, "tag_name");
                string url = ExtractJsonString(json, "html_url");
                if (string.IsNullOrEmpty(tag))
                    return;

                if (!TryParseSemVer(AppVersion.DisplayVersion, out Version local)
                    || !TryParseSemVer(tag, out Version remote))
                    return;

                if (remote <= local)
                    return;

                _latestTag = tag.Trim();
                _releaseUrl = string.IsNullOrEmpty(url)
                    ? "https://github.com/Mursisru/MissileCamera/releases/latest"
                    : url;
                _outdated = true;
            }
            catch
            {
                // Offline / API error — no UI.
            }
            finally
            {
                _completed = true;
            }
        }

        private static string ExtractJsonString(string json, string key)
        {
            // "key": "value"
            var m = Regex.Match(
                json,
                "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]+)\"",
                RegexOptions.CultureInvariant);
            return m.Success ? m.Groups[1].Value : string.Empty;
        }

        private static bool TryParseSemVer(string raw, out Version version)
        {
            version = new Version(0, 0, 0);
            if (string.IsNullOrEmpty(raw))
                return false;

            string s = raw.Trim();
            if (s.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                s = s.Substring(1).Trim();

            // Strip pre-release suffix if any slipped through (1.9.9-rc).
            int dash = s.IndexOfAny(new[] { '-', '+' });
            if (dash > 0)
                s = s.Substring(0, dash);

            string[] parts = s.Split('.');
            if (parts.Length < 2)
                return false;

            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int maj)
                || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int min))
                return false;

            int pat = 0;
            if (parts.Length >= 3
                && !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out pat))
                return false;

            version = new Version(maj, min, pat);
            return true;
        }
    }
}
