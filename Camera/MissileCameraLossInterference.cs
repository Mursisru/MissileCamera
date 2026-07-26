using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// Loss / switch / exit burst: black screen + "NO SIGNAL" in a bordered rectangle.
    /// Fullscreen uses Overlay canvas; MFD covers the feed RawImage.
    /// </summary>
    internal static class MissileCameraLossInterference
    {
        private const int OverlaySortingOrder = 200;
        private const string FullscreenHostName = "MissileCamera.NoSignalOverlay";
        private const string MfdHostName = "MissileCamera.NoSignalMfd";
        private const string SignalLabel = "NO SIGNAL";
        private const float BoxWidth = 420f;
        private const float BoxHeight = 120f;
        private const float BorderThickness = 3f;
        private static readonly Color BorderColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        private static readonly Color LabelColor = new Color(0.95f, 0.95f, 0.95f, 1f);

        internal enum BurstKind : byte
        {
            None = 0,
            Switch = 1,
            Destroy = 2,
            ExitShutdown = 3
        }

        private static float _endsAtUnscaled = -1f;
        private static BurstKind _kind = BurstKind.None;
        private static GameObject? _fullscreenGo;
        private static GameObject? _mfdGo;
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

            _kind = kind;
            _endsAtUnscaled = Time.unscaledTime + durationSeconds;
            _exitCompletionPending = false;
        }

        /// <summary>
        /// Keep NO SIGNAL visible. Returns true while burst is still playing.
        /// When ExitShutdown ends, sets completion flag for FullscreenController.
        /// </summary>
        internal static bool Tick(RawImage? mfdFeedImage)
        {
            if (_endsAtUnscaled < 0f)
                return false;

            if (Time.unscaledTime >= _endsAtUnscaled)
            {
                BurstKind finished = _kind;
                StopVisual(mfdFeedImage);
                _kind = BurstKind.None;
                _endsAtUnscaled = -1f;
                if (finished == BurstKind.ExitShutdown)
                    _exitCompletionPending = true;
                return false;
            }

            Show(mfdFeedImage);
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
            StopVisual(null);
            _endsAtUnscaled = -1f;
            _kind = BurstKind.None;
            _exitCompletionPending = false;
        }

        internal static void Shutdown()
        {
            Stop();
            DestroyFullscreenHost();
            DestroyMfdHost();
        }

        private static void Show(RawImage? mfdFeedImage)
        {
            bool useFullscreen = MissileCameraFullscreenController.IsActive
                || _kind == BurstKind.ExitShutdown;

            if (useFullscreen)
            {
                HideMfdHost();
                EnsureFullscreenHost();
                if (_fullscreenGo != null)
                    _fullscreenGo.SetActive(true);
                return;
            }

            HideFullscreenHost();
            if (mfdFeedImage == null)
                return;

            mfdFeedImage.enabled = false;
            EnsureMfdHost(mfdFeedImage);
            if (_mfdGo != null)
                _mfdGo.SetActive(true);
        }

        private static void StopVisual(RawImage? mfdFeedImage)
        {
            HideFullscreenHost();
            HideMfdHost();
            _ = mfdFeedImage;
        }

        private static void HideFullscreenHost()
        {
            if (_fullscreenGo != null)
                _fullscreenGo.SetActive(false);
        }

        private static void HideMfdHost()
        {
            if (_mfdGo != null)
                _mfdGo.SetActive(false);
        }

        private static void EnsureFullscreenHost()
        {
            if (_fullscreenGo != null)
                return;

            _fullscreenGo = new GameObject(FullscreenHostName);
            Object.DontDestroyOnLoad(_fullscreenGo);
            _fullscreenGo.hideFlags = HideFlags.HideAndDontSave;

            Canvas canvas = _fullscreenGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = OverlaySortingOrder;
            canvas.pixelPerfect = false;

            var scaler = _fullscreenGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            BuildNoSignalVisual(_fullscreenGo.transform, fullscreenLayout: true);
            _fullscreenGo.SetActive(false);
        }

        private static void EnsureMfdHost(RawImage feedImage)
        {
            Transform? parent = feedImage.transform.parent;
            if (parent == null)
                return;

            if (_mfdGo != null)
            {
                if (_mfdGo.transform.parent != parent)
                    _mfdGo.transform.SetParent(parent, false);
                StretchFull(_mfdGo.GetComponent<RectTransform>());
                _mfdGo.transform.SetAsLastSibling();
                return;
            }

            _mfdGo = new GameObject(MfdHostName, typeof(RectTransform));
            _mfdGo.transform.SetParent(parent, false);
            _mfdGo.hideFlags = HideFlags.HideAndDontSave;
            StretchFull(_mfdGo.GetComponent<RectTransform>());
            BuildNoSignalVisual(_mfdGo.transform, fullscreenLayout: false);
            _mfdGo.transform.SetAsLastSibling();
            _mfdGo.SetActive(false);
        }

        private static void BuildNoSignalVisual(Transform root, bool fullscreenLayout)
        {
            var bgGo = new GameObject("BlackBg", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(root, false);
            StretchFull(bgGo.GetComponent<RectTransform>());
            Image bg = bgGo.GetComponent<Image>();
            bg.color = Color.black;
            bg.raycastTarget = false;

            float boxW = fullscreenLayout ? BoxWidth : BoxWidth * 0.72f;
            float boxH = fullscreenLayout ? BoxHeight : BoxHeight * 0.72f;
            int fontSize = fullscreenLayout ? 42 : 28;

            var frameGo = new GameObject("SignalBox", typeof(RectTransform));
            frameGo.transform.SetParent(root, false);
            RectTransform frameRt = frameGo.GetComponent<RectTransform>();
            frameRt.anchorMin = new Vector2(0.5f, 0.5f);
            frameRt.anchorMax = new Vector2(0.5f, 0.5f);
            frameRt.pivot = new Vector2(0.5f, 0.5f);
            frameRt.anchoredPosition = Vector2.zero;
            frameRt.sizeDelta = new Vector2(boxW, boxH);

            var borderGo = new GameObject("Border", typeof(RectTransform), typeof(Image));
            borderGo.transform.SetParent(frameGo.transform, false);
            StretchFull(borderGo.GetComponent<RectTransform>());
            Image border = borderGo.GetComponent<Image>();
            border.color = BorderColor;
            border.raycastTarget = false;

            var innerGo = new GameObject("Inner", typeof(RectTransform), typeof(Image));
            innerGo.transform.SetParent(frameGo.transform, false);
            RectTransform innerRt = innerGo.GetComponent<RectTransform>();
            StretchFull(innerRt);
            float inset = BorderThickness;
            innerRt.offsetMin = new Vector2(inset, inset);
            innerRt.offsetMax = new Vector2(-inset, -inset);
            Image inner = innerGo.GetComponent<Image>();
            inner.color = Color.black;
            inner.raycastTarget = false;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(frameGo.transform, false);
            StretchFull(labelGo.GetComponent<RectTransform>());
            Text label = labelGo.GetComponent<Text>();
            label.font = HudFontHelper.GetFont();
            label.text = SignalLabel;
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = LabelColor;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        private static void DestroyFullscreenHost()
        {
            if (_fullscreenGo != null)
            {
                Object.Destroy(_fullscreenGo);
                _fullscreenGo = null;
            }
        }

        private static void DestroyMfdHost()
        {
            if (_mfdGo != null)
            {
                Object.Destroy(_mfdGo);
                _mfdGo = null;
            }
        }
    }
}
