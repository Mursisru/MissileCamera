using UnityEngine;

namespace MissileCamera
{
    internal static class MapGridAccess
    {
        private const float QuantizeMeters = 50f;

        private static int _slot0Qx = int.MinValue;
        private static int _slot0Qz = int.MinValue;
        private static string _slot0Label = "GRID ---";
        private static int _slot1Qx = int.MinValue;
        private static int _slot1Qz = int.MinValue;
        private static string _slot1Label = "GRID ---";
        private static int _writeSlot;

        internal static string GetGridLabel(GlobalPosition position)
        {
            int qx = Mathf.FloorToInt(position.x / QuantizeMeters);
            int qz = Mathf.FloorToInt(position.z / QuantizeMeters);

            if (qx == _slot0Qx && qz == _slot0Qz)
                return _slot0Label;
            if (qx == _slot1Qx && qz == _slot1Qz)
                return _slot1Label;

            string label = ResolveLabel(position);
            if (_writeSlot == 0)
            {
                _slot0Qx = qx;
                _slot0Qz = qz;
                _slot0Label = label;
                _writeSlot = 1;
            }
            else
            {
                _slot1Qx = qx;
                _slot1Qz = qz;
                _slot1Label = label;
                _writeSlot = 0;
            }

            return label;
        }

        private static string ResolveLabel(GlobalPosition position)
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
