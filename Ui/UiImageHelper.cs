using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    internal static class UiImageHelper
    {
        private static Sprite? _whiteSprite;

        internal static void ApplySolid(Image image, Color color)
        {
            image.sprite = GetWhiteSprite();
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
        }

        private static Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null)
                return _whiteSprite;

            Texture2D tex = Texture2D.whiteTexture;
            _whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            return _whiteSprite;
        }
    }
}
