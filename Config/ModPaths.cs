using System.IO;

namespace MissileCamera.Config
{
    /// <summary>BepInEx plugin folder: .../BepInEx/plugins/MissileCamera/</summary>
    internal static class ModPaths
    {
        private static string _pluginDir = string.Empty;

        internal static string PluginDir => _pluginDir;

        internal static void Init(string pluginDir) => _pluginDir = pluginDir;

        internal static string ConfigIniPath => Path.Combine(_pluginDir, "mod_config.ini");
    }
}
