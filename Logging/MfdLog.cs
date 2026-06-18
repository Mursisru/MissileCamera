using BepInEx.Logging;

namespace MissileCamera
{
    internal static class MfdLog
    {
        internal static void Info(string message)
        {
            MissileCameraPlugin.ModLogger?.LogInfo(message);
            UnityEngine.Debug.Log("[MissileCamera] " + message);
        }

        internal static void Error(string message) =>
            MissileCameraPlugin.ModLogger?.LogError(message);
    }
}
