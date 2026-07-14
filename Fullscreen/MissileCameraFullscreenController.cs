using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// Fullscreen missile feed host. On pause / maximized map — Exit() (restore panel to MFD)
    /// so vanilla UI is never covered and the feed is never stolen into a hidden canvas.
    /// </summary>
    internal static class MissileCameraFullscreenController
    {
        private const int OverlaySortingOrder = 50;

        private static bool _active;
        private static GameObject? _overlayGo;
        private static Canvas? _overlayCanvas;
        private static RectTransform? _fullscreenRoot;
        private static RectTransform? _panelOriginalParent;
        private static int _panelOriginalSibling;
        private static Vector2 _panelAnchorMin;
        private static Vector2 _panelAnchorMax;
        private static Vector2 _panelOffsetMin;
        private static Vector2 _panelOffsetMax;
        private static Vector3 _panelLocalScale;
        private static Quaternion _panelLocalRotation;

        internal static bool IsActive => _active;

        internal static void ResetForMissionUnload()
        {
            if (_active)
                Exit(force: true);

            MissileCameraFullscreenBootstrap.ResetForMissionUnload();
            DestroyOverlayHost();
        }

        internal static void Toggle()
        {
            if (!MissileCameraFullscreenConfig.Enabled)
                return;

            if (!MissileCameraFeedController.HasOverlayInputContext() && !_active)
                return;

            if (_active)
                Exit(force: false);
            else
                Enter();
        }

        internal static void ExitIfActive()
        {
            if (_active)
                Exit(force: true);
        }

        /// <summary>
        /// While fullscreen: if pause or full map is open, exit fullscreen so UI is usable
        /// and the MFD panel is restored (never leave the panel parented under a disabled canvas).
        /// </summary>
        internal static void TickYieldToVanillaUi()
        {
            if (!_active)
                return;

            if (ShouldDeferToVanillaUi())
            {
                MfdLog.Info("fullscreen auto-exit → vanilla UI (pause/map)");
                Exit(force: false);
            }
        }

        /// <summary>Only real blocking UIs — not menuCanvas.enabled alone (false positives).</summary>
        private static bool ShouldDeferToVanillaUi()
        {
            if (GameplayUI.GameIsPaused)
                return true;

            if (DynamicMap.mapMaximized)
                return true;

            return false;
        }

        private static void Enter()
        {
            if (GameplayUI.GameIsPaused)
            {
                MfdLog.Info("fullscreen enter blocked: paused");
                return;
            }

            // Full map open → close it so we can show the feed (same key-flow as player expects).
            if (DynamicMap.mapMaximized)
            {
                try
                {
                    SceneSingleton<DynamicMap>.i?.Minimize();
                }
                catch
                {
                    MfdLog.Info("fullscreen enter blocked: map maximized");
                    return;
                }
            }

            RectTransform? panelRt = MissileCameraFeedController.TryGetPanelRt();
            if (panelRt == null)
            {
                MfdLog.Info("fullscreen enter blocked: no panel");
                return;
            }

            EnsureOverlayHost();
            if (_fullscreenRoot == null || _overlayGo == null || _overlayCanvas == null)
                return;

            _panelOriginalParent = panelRt.parent as RectTransform;
            _panelOriginalSibling = panelRt.GetSiblingIndex();
            _panelAnchorMin = panelRt.anchorMin;
            _panelAnchorMax = panelRt.anchorMax;
            _panelOffsetMin = panelRt.offsetMin;
            _panelOffsetMax = panelRt.offsetMax;
            _panelLocalScale = panelRt.localScale;
            _panelLocalRotation = panelRt.localRotation;

            _overlayGo.SetActive(true);
            _overlayCanvas.enabled = true;
            _fullscreenRoot.SetAsLastSibling();

            panelRt.SetParent(_fullscreenRoot, false);
            Stretch(panelRt);
            panelRt.localScale = Vector3.one;
            panelRt.localRotation = Quaternion.identity;

            _active = true;
            MissileCameraFullscreenBootstrap.StartIfNeeded(panelRt);
            MissileCameraFeedController.NotifyFullscreenChanged();
            MfdLog.Info("fullscreen enter (game viewport overlay)");
        }

        private static void Exit(bool force)
        {
            MissileCameraFullscreenBootstrap.Abort();

            RectTransform? panelRt = MissileCameraFeedController.TryGetPanelRt();
            if (panelRt != null && _panelOriginalParent != null)
            {
                panelRt.SetParent(_panelOriginalParent, false);
                int maxSibling = Mathf.Max(0, _panelOriginalParent.childCount - 1);
                panelRt.SetSiblingIndex(Mathf.Clamp(_panelOriginalSibling, 0, maxSibling));
                panelRt.anchorMin = _panelAnchorMin;
                panelRt.anchorMax = _panelAnchorMax;
                panelRt.offsetMin = _panelOffsetMin;
                panelRt.offsetMax = _panelOffsetMax;
                panelRt.localScale = _panelLocalScale;
                panelRt.localRotation = _panelLocalRotation;
            }

            if (_overlayGo != null)
                _overlayGo.SetActive(false);

            if (_overlayCanvas != null)
                _overlayCanvas.enabled = true;

            _active = false;
            MissileCameraFeedController.NotifyFullscreenChanged();
            if (!force)
                MfdLog.Info("fullscreen exit");
        }

        private static void EnsureOverlayHost()
        {
            if (_overlayGo != null && _fullscreenRoot != null && _overlayCanvas != null)
                return;

            DestroyOverlayHost();

            _overlayGo = new GameObject("MissileCamera.GameFullscreen");
            Object.DontDestroyOnLoad(_overlayGo);
            _overlayGo.hideFlags = HideFlags.HideAndDontSave;

            _overlayCanvas = _overlayGo.AddComponent<Canvas>();
            _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _overlayCanvas.sortingOrder = OverlaySortingOrder;
            _overlayCanvas.pixelPerfect = false;
            _overlayCanvas.overrideSorting = true;

            var scaler = _overlayGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var rootGo = new GameObject("FullscreenRoot", typeof(RectTransform));
            rootGo.transform.SetParent(_overlayGo.transform, false);
            _fullscreenRoot = rootGo.GetComponent<RectTransform>();
            Stretch(_fullscreenRoot);

            var bgGo = new GameObject("FullscreenBackdrop", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(_fullscreenRoot, false);
            bgGo.transform.SetAsFirstSibling();
            RectTransform bgRt = bgGo.GetComponent<RectTransform>();
            Stretch(bgRt);
            Image bg = bgGo.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 1f);
            bg.raycastTarget = false;
        }

        private static void DestroyOverlayHost()
        {
            if (_overlayGo != null)
            {
                Object.Destroy(_overlayGo);
                _overlayGo = null;
            }

            _overlayCanvas = null;
            _fullscreenRoot = null;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }
    }
}
