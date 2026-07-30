using BepInEx.Logging;

namespace MissileCamera
{
    internal static class MfdLog
    {
        internal static void Info(string message) =>
            MissileCameraPlugin.ModLogger?.LogInfo(message);

        internal static void Warning(string message)
        {
            MissileCameraPlugin.ModLogger?.LogWarning(message);
            UnityEngine.Debug.LogWarning("[MissileCamera] " + message);
        }

        internal static void Error(string message) =>
            MissileCameraPlugin.ModLogger?.LogError(message);
    }
}
