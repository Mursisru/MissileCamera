using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// Short TV-static burst on the MFD feed after the followed missile is destroyed.
    /// CPU noise texture — no shader bundle dependency. Reuses buffers (no GC per frame).
    /// </summary>
    internal static class MissileCameraLossInterference
    {
        private const int NoiseWidth = 96;
        private const int NoiseHeight = 72;

        private static Texture2D? _texture;
        private static Color32[]? _pixels;
        private static float _endsAtUnscaled = -1f;
        private static float _durationSeconds = 0.4f;
        private static uint _rngState = 1u;

        internal static bool IsActive =>
            _endsAtUnscaled > 0f && Time.unscaledTime < _endsAtUnscaled;

        internal static void Begin(float durationSeconds)
        {
            if (durationSeconds <= 0f)
            {
                Stop();
                return;
            }

            EnsureBuffers();
            _durationSeconds = durationSeconds;
            _endsAtUnscaled = Time.unscaledTime + durationSeconds;
            _rngState ^= (uint)(Time.frameCount * 2654435761u + 1u);
            if (_rngState == 0u)
                _rngState = 1u;
        }

        internal static bool Tick(RawImage? feedImage)
        {
            if (_endsAtUnscaled < 0f)
                return false;

            if (Time.unscaledTime >= _endsAtUnscaled)
            {
                Stop();
                return false;
            }

            ApplyNoiseFrame(feedImage);
            return true;
        }

        internal static void Stop()
        {
            _endsAtUnscaled = -1f;
        }

        internal static void Shutdown()
        {
            Stop();
            if (_texture != null)
            {
                Object.Destroy(_texture);
                _texture = null;
            }

            _pixels = null;
        }

        private static void ApplyNoiseFrame(RawImage? feedImage)
        {
            if (feedImage == null)
                return;

            EnsureBuffers();
            Color32[] pixels = _pixels!;
            Texture2D texture = _texture!;

            float progress = 1f - Mathf.Clamp01((_endsAtUnscaled - Time.unscaledTime)
                / Mathf.Max(_durationSeconds, 0.001f));
            byte snowFloor = (byte)Mathf.RoundToInt(Mathf.Lerp(18f, 8f, progress));
            byte snowCeil = (byte)Mathf.RoundToInt(Mathf.Lerp(235f, 160f, progress));

            int tearRow = (int)(NextFloat() * NoiseHeight);
            int tearHeight = 1 + (int)(NextFloat() * 4f);
            byte tearValue = NextFloat() > 0.5f ? (byte)255 : (byte)0;

            int bandCol = (int)(NextFloat() * NoiseWidth);
            int bandWidth = 2 + (int)(NextFloat() * 6f);

            for (int y = 0; y < NoiseHeight; y++)
            {
                bool inTear = y >= tearRow && y < tearRow + tearHeight;
                int row = y * NoiseWidth;
                for (int x = 0; x < NoiseWidth; x++)
                {
                    byte v;
                    if (inTear)
                    {
                        v = tearValue;
                    }
                    else if (x >= bandCol && x < bandCol + bandWidth && NextFloat() > 0.35f)
                    {
                        v = (byte)(NextFloat() > 0.5f ? 255 : 0);
                    }
                    else
                    {
                        v = (byte)(snowFloor + (int)(NextFloat() * (snowCeil - snowFloor + 1)));
                    }

                    pixels[row + x] = new Color32(v, v, v, 255);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            feedImage.texture = texture;
            feedImage.enabled = true;
            feedImage.color = Color.white;
        }

        private static void EnsureBuffers()
        {
            if (_pixels == null)
                _pixels = new Color32[NoiseWidth * NoiseHeight];

            if (_texture != null)
                return;

            _texture = new Texture2D(NoiseWidth, NoiseHeight, TextureFormat.RGB24, mipChain: false)
            {
                name = "MissileCamera.LossInterference",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private static float NextFloat()
        {
            _rngState = _rngState * 1664525u + 1013904223u;
            return (_rngState & 0x00FFFFFFu) / 16777216f;
        }
    }
}
