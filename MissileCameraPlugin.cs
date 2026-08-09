using System.IO;
using BepInEx;
using BepInEx.Logging;
using MissileCamera.Config;

namespace MissileCamera
{
    [BepInPlugin(PluginGuid, PluginName, AppVersion.BepInSemVer)]
    public sealed class MissileCameraPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.at747.missilecamera.bepinex";
        public const string PluginName = "Missile Camera";

        internal static ManualLogSource? ModLogger { get; private set; }

        private void Awake()
        {
            ModLogger = base.Logger;

            MissileCameraBepInConfig.Bind(Config);
            MissileCameraConfigLiveRefresh.Subscribe(Config);

            string? pluginDir = Path.GetDirectoryName(Info.Location);
            if (string.IsNullOrEmpty(pluginDir))
            {
                ModLogger.LogError("Could not resolve plugin directory.");
                return;
            }

            ModPaths.Init(pluginDir);
            MissileCameraHost.Ensure(pluginDir, ModLogger);
            ModLogger.LogInfo($"{PluginName} {AppVersion.DisplayVersion} loaded.");
        }
    }
}
