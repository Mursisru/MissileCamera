using UnityEngine;

namespace MissileCamera
{
    internal static class MapGridAccess
    {
        internal static string GetGridLabel(GlobalPosition position)
        {
            try
            {
                DynamicMap? map = SceneSingleton<DynamicMap>.i;
                if (map == null || map.gridLabels == null)
                    return "GRID ---";

                string grid = map.gridLabels.GetGridPosition(position);
                return string.IsNullOrEmpty(grid) ? "GRID ---" : grid;
            }
            catch
            {
                return "GRID ---";
            }
        }
    }
}
