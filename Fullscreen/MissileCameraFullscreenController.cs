using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// Fullscreen missile feed host. On pause / maximized map — Exit() (restore panel to MFD)
    /// so vanilla UI is never covered and the feed is never stolen into a hidden canvas.
    /// Exit with no missiles: 0.5s NO SIGNAL, then auto-disable.
    /// Never touches CameraStateManager (see CAMERA_SAFETY.md).
    /// </summary>
    internal static class MissileCameraFullscreenController
    {
        private const int OverlaySortingOrder = 50;

        private static bool _active;
        private static bool _deferredExit;
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
        internal static bool IsDeferredExit => _deferredExit;

        internal static void ResetForMissionUnload()
        {
            _deferredExit = false;
            MissileCameraLossInterference.Stop();
            if (_active)
                Exit(force: true);

            MissileCameraVanillaHudBridge.ResetForMissionUnload();
            MissileCameraFullscreenBootstrap.ResetForMissionUnload();
            DestroyOverlayHost();
        }

        internal static void Toggle()
        {
            if (!MissileCameraFullscreenConfig.Enabled)
                return;

            if (_deferredExit)
            {
                CompleteDeferredExit();
                return;
            }

            if (!MissileCameraFeedController.HasOverlayInputContext() && !_active)
                return;

            if (_active)
                RequestExit();
            else
                Enter();
        }

        internal static void ExitIfActive()
        {
            _deferredExit = false;
            if (_active)
                Exit(force: true);
        }

        /// <summary>Called each Tick after interference — finishes exit-no-missile burst.</summary>
        internal static void TickDeferredExit()
        {
            if (!_deferredExit)
                return;

            if (MissileCameraLossInterference.ConsumeExitCompletion())
                CompleteDeferredExit();
        }

        private static void RequestExit()
        {
            // No live missiles → NO SIGNAL then auto-off. Otherwise leave immediately.
            if (!MissileCameraFeedController.HasTrackableOwnedMissile())
            {
                BeginDeferredExit();
                return;
            }

            Exit(force: false);
        }

        private static void BeginDeferredExit()
        {
            if (_deferredExit)
                return;

            _deferredExit = true;
            float seconds = Mathf.Max(MissileCameraFeedConfig.PostLossInterferenceSeconds, 0.05f);
            MissileCameraLossInterference.BeginExitShutdown(seconds);
            MfdLog.Info("fullscreen deferred exit → interference");
        }

        private static void CompleteDeferredExit()
        {
            _deferredExit = false;
            MissileCameraLossInterference.Stop();
            if (_active)
                Exit(force: false);
            MfdLog.Info("fullscreen deferred exit complete");
        }

        internal static void TickYieldToVanillaUi()
        {
            if (!_active)
                return;

            if (ShouldDeferToVanillaUi())
            {
                MfdLog.Info("fullscreen auto-exit → vanilla UI (pause/map)");
                _deferredExit = false;
                MissileCameraLossInterference.Stop();
                Exit(force: false);
            }
        }

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

            _deferredExit = false;
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
            MissileCameraVanillaHudBridge.OnFullscreenEntered();
            MissileCameraFullscreenBootstrap.StartIfNeeded(panelRt);
            MissileCameraFeedController.NotifyFullscreenChanged();
            MfdLog.Info("fullscreen enter (RawImage feed, CSM untouched)");
        }

        private static void Exit(bool force)
        {
            _deferredExit = false;
            _active = false;

            try
            {
                MissileCameraFullscreenBootstrap.Abort();
            }
            catch
            {
                // ignore
            }

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

            try
            {
                MissileCameraVanillaHudBridge.OnFullscreenExited();
            }
            catch (System.Exception ex)
            {
                MfdLog.Info("fullscreen hud exit error: " + ex.Message);
            }

            MissileCameraFeedController.NotifyFullscreenChanged();
            if (!force)
                MfdLog.Info("fullscreen exit");
        }

        private static void EnsureOverlayHost()
        {
            if (_overlayGo != null && _fullscreenRoot != null && _overlayCanvas != null)
            {
                EnsureOverlayDoesNotBlockRaycasts();
                return;
            }

            DestroyOverlayHost();

            _overlayGo = new GameObject("MissileCamera.GameFullscreen");
            Object.DontDestroyOnLoad(_overlayGo);
            _overlayGo.hideFlags = HideFlags.HideAndDontSave;

            _overlayCanvas = _overlayGo.AddComponent<Canvas>();
            _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _overlayCanvas.sortingOrder = OverlaySortingOrder;
            _overlayCanvas.pixelPerfect = false;
            _overlayCanvas.overrideSorting = true;

            EnsureOverlayDoesNotBlockRaycasts();

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
            // Opaque black under RawImage — never rely on hijacking the cockpit camera.
            bg.color = Color.black;
            bg.raycastTarget = false;
        }

        private static void EnsureOverlayDoesNotBlockRaycasts()
        {
            if (_overlayGo == null)
                return;

            CanvasGroup group = _overlayGo.GetComponent<CanvasGroup>();
            if (group == null)
                group = _overlayGo.AddComponent<CanvasGroup>();

            group.interactable = false;
            group.blocksRaycasts = false;
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
