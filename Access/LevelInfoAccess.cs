using UnityEngine;

namespace MissileCamera
{
    /// <summary>Safe access to <see cref="LevelInfo"/> for local IR policy (works in SP and MP).</summary>
    internal static class LevelInfoAccess
    {
        internal static bool TryGet(out LevelInfo levelInfo)
        {
            levelInfo = null!;
            try
            {
                LevelInfo? instance = NetworkSceneSingleton<LevelInfo>.i;
                if (instance == null)
                    return false;

                levelInfo = instance;
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static bool TryGetTimeOfDay(out float timeOfDay)
        {
            timeOfDay = 12f;
            if (!TryGet(out LevelInfo levelInfo))
                return false;

            timeOfDay = levelInfo.timeOfDay;
            if (float.IsNaN(timeOfDay) || float.IsInfinity(timeOfDay))
            {
                timeOfDay = 12f;
                return false;
            }

            return true;
        }

        internal static bool TryGetAmbientLight(out float ambient)
        {
            ambient = 1f;
            if (!TryGet(out LevelInfo levelInfo))
                return false;

            try
            {
                ambient = levelInfo.GetAmbientLight();
            }
            catch
            {
                ambient = 1f;
                return false;
            }

            if (float.IsNaN(ambient) || float.IsInfinity(ambient))
            {
                ambient = 1f;
                return false;
            }

            return true;
        }
    }
}
