using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// TV-static burst: fullscreen overlay (preferred) or MFD RawImage fallback.
    /// Switch / destroy / exit-no-missile — default 0.5s.
    /// </summary>
    internal static class MissileCameraLossInterference
    {
        private const int NoiseWidth = 128;
        private const int NoiseHeight = 96;
        private const int FullscreenNoiseSortingOrder = 200;
        private const string FullscreenNoiseName = "MissileCamera.FullscreenInterference";

        internal enum BurstKind : byte
        {
            None = 0,
            Switch = 1,
            Destroy = 2,
            ExitShutdown = 3
        }

        private static Texture2D? _texture;
        private static Color32[]? _pixels;
        private static float _endsAtUnscaled = -1f;
        private static float _durationSeconds = 0.5f;
        private static uint _rngState = 1u;
        private static BurstKind _kind = BurstKind.None;
        private static GameObject? _fullscreenNoiseGo;
        private static RawImage? _fullscreenNoiseImage;
        private static bool _exitCompletionPending;

        internal static bool IsActive =>
            _endsAtUnscaled > 0f && Time.unscaledTime < _endsAtUnscaled;

        internal static BurstKind ActiveKind => IsActive ? _kind : BurstKind.None;

        internal static bool IsExitShutdown =>
            IsActive && _kind == BurstKind.ExitShutdown;

        internal static void BeginSwitch(float durationSeconds) =>
            Begin(BurstKind.Switch, durationSeconds);

        internal static void BeginDestroy(float durationSeconds) =>
            Begin(BurstKind.Destroy, durationSeconds);

        internal static void BeginExitShutdown(float durationSeconds) =>
            Begin(BurstKind.ExitShutdown, durationSeconds);

        /// <summary>Legacy entry — destroy burst.</summary>
        internal static void Begin(float durationSeconds) =>
            BeginDestroy(durationSeconds);

        private static void Begin(BurstKind kind, float durationSeconds)
        {
            if (durationSeconds <= 0f)
            {
                Stop();
                return;
            }

            EnsureBuffers();
            _kind = kind;
            _durationSeconds = durationSeconds;
            _endsAtUnscaled = Time.unscaledTime + durationSeconds;
            _exitCompletionPending = false;
            _rngState ^= (uint)(Time.frameCount * 2654435761u + 1u);
            if (_rngState == 0u)
                _rngState = 1u;

            if (MissileCameraFullscreenController.IsActive || kind == BurstKind.ExitShutdown)
                EnsureFullscreenNoiseVisible(true);
        }

        /// <summary>
        /// Paint noise. Returns true while burst is still playing.
        /// When ExitShutdown ends, sets completion flag for FullscreenController.
        /// </summary>
        internal static bool Tick(RawImage? mfdFeedImage)
        {
            if (_endsAtUnscaled < 0f)
                return false;

            if (Time.unscaledTime >= _endsAtUnscaled)
            {
                BurstKind finished = _kind;
                StopVisual();
                _kind = BurstKind.None;
                _endsAtUnscaled = -1f;
                if (finished == BurstKind.ExitShutdown)
                    _exitCompletionPending = true;
                return false;
            }

            RawImage? target = ResolveTarget(mfdFeedImage);
            ApplyNoiseFrame(target);
            return true;
        }

        internal static bool ConsumeExitCompletion()
        {
            if (!_exitCompletionPending)
                return false;

            _exitCompletionPending = false;
            return true;
        }

        internal static void Stop()
        {
            StopVisual();
            _endsAtUnscaled = -1f;
            _kind = BurstKind.None;
            _exitCompletionPending = false;
        }

        internal static void Shutdown()
        {
            Stop();
            DestroyFullscreenNoise();
            if (_texture != null)
            {
                Object.Destroy(_texture);
                _texture = null;
            }

            _pixels = null;
        }

        private static RawImage? ResolveTarget(RawImage? mfdFeedImage)
        {
            if (MissileCameraFullscreenController.IsActive || _kind == BurstKind.ExitShutdown)
            {
                EnsureFullscreenNoiseVisible(true);
                return _fullscreenNoiseImage;
            }

            EnsureFullscreenNoiseVisible(false);
            return mfdFeedImage;
        }

        private static void EnsureFullscreenNoiseVisible(bool visible)
        {
            if (!visible)
            {
                if (_fullscreenNoiseGo != null)
                    _fullscreenNoiseGo.SetActive(false);
                return;
            }

            EnsureFullscreenNoiseHost();
            if (_fullscreenNoiseGo != null)
                _fullscreenNoiseGo.SetActive(true);
        }

        private static void EnsureFullscreenNoiseHost()
        {
            if (_fullscreenNoiseGo != null && _fullscreenNoiseImage != null)
                return;

            DestroyFullscreenNoise();

            _fullscreenNoiseGo = new GameObject(FullscreenNoiseName);
            Object.DontDestroyOnLoad(_fullscreenNoiseGo);
            _fullscreenNoiseGo.hideFlags = HideFlags.HideAndDontSave;

            Canvas canvas = _fullscreenNoiseGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = FullscreenNoiseSortingOrder;
            canvas.pixelPerfect = false;

            var scaler = _fullscreenNoiseGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var imageGo = new GameObject("Noise", typeof(RectTransform), typeof(RawImage));
            imageGo.transform.SetParent(_fullscreenNoiseGo.transform, false);
            RectTransform rt = imageGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);

            _fullscreenNoiseImage = imageGo.GetComponent<RawImage>();
            _fullscreenNoiseImage.raycastTarget = false;
            _fullscreenNoiseImage.color = Color.white;
        }

        private static void DestroyFullscreenNoise()
        {
            if (_fullscreenNoiseGo != null)
            {
                Object.Destroy(_fullscreenNoiseGo);
                _fullscreenNoiseGo = null;
            }

            _fullscreenNoiseImage = null;
        }

        private static void StopVisual()
        {
            EnsureFullscreenNoiseVisible(false);
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
                name = "MissileCamera.InterferenceNoise",
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
