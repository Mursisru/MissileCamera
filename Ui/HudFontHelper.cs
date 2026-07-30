using UnityEngine;

namespace MissileCamera
{
    internal static class HudFontHelper
    {
        private static Font? _font;

        internal static Font GetFont()
        {
            if (_font != null)
                return _font;

            Font legacy = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (legacy != null)
            {
                _font = legacy;
                return _font;
            }

            _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return _font;
        }

        internal static void Reset() => _font = null;
    }
}
