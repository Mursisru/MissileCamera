using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// Safe TV look on FS: scrollable scanline Texture2D overlay (no RT blit / no pink risk).
    /// Sits above feed RawImage, below gunship HUD chrome.
    /// </summary>
    internal sealed class GunshipTvOverlay
    {
        private const int TexH = 256;
        private const float ScrollSpeed = 28f;
        private const float LineAlpha = 0.14f;

        private readonly RawImage _image;
        private readonly Texture2D _tex;
        private float _scroll;

        private GunshipTvOverlay(RawImage image, Texture2D tex)
        {
            _image = image;
            _tex = tex;
        }

        internal static GunshipTvOverlay Create(RectTransform parent)
        {
            var go = new GameObject("GunshipTvOverlay", typeof(RectTransform), typeof(RawImage), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling();
            RectTransform rt = go.GetComponent<RectTransform>();
            GunshipChrome.Stretch(rt);

            Texture2D tex = BuildScanTex();
            RawImage img = go.GetComponent<RawImage>();
            img.texture = tex;
            img.color = new Color(1f, 1f, 1f, 1f);
            img.raycastTarget = false;
            img.uvRect = new Rect(0f, 0f, 1f, 4f);
            return new GunshipTvOverlay(img, tex);
        }

        internal void Update()
        {
            if (_image == null)
                return;

            _scroll += Time.unscaledDeltaTime * ScrollSpeed;
            float v = (_scroll % TexH) / TexH;
            Rect uv = _image.uvRect;
            uv.y = -v;
            _image.uvRect = uv;
        }

        internal void Shutdown()
        {
            try
            {
                if (_image != null)
                    Object.Destroy(_image.gameObject);
            }
            catch { /* ignore */ }

            try
            {
                if (_tex != null)
                    Object.Destroy(_tex);
            }
            catch { /* ignore */ }
        }

        private static Texture2D BuildScanTex()
        {
            var tex = new Texture2D(2, TexH, TextureFormat.RGBA32, false)
            {
                name = "MC.TvScan",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };

            for (int y = 0; y < TexH; y++)
            {
                // Soft dark band every other line — almost transparent.
                bool dark = (y % 2) == 0;
                float a = dark ? LineAlpha : LineAlpha * 0.25f;
                // Mild rolling thicker beam every ~32 lines.
                if (y % 32 < 2)
                    a = Mathf.Min(0.22f, a + 0.08f);

                var c = new Color(0f, 0f, 0f, a);
                tex.SetPixel(0, y, c);
                tex.SetPixel(1, y, c);
            }

            tex.Apply(false, true);
            return tex;
        }
    }
}
