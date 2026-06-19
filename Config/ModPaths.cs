using System.IO;

namespace MissileCamera.Config
{
    internal static class ModPaths
    {
        private static string _pluginDir = string.Empty;

        internal static string PluginDir => _pluginDir;

        internal static void Init(string pluginDir) => _pluginDir = pluginDir;

        internal static string ConfigFilePath =>
            Path.Combine(BepInEx.Paths.ConfigPath, MissileCamera.MissileCameraPlugin.PluginGuid + ".cfg");
    }
}
