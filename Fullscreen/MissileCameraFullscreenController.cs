using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// Fullscreen missile feed host. Uses dedicated overlay feed — never reparents MFD panel.
    /// On pause / maximized map — Exit() so vanilla UI is never covered.
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

        internal static bool IsActive => _active;
        internal static bool IsDeferredExit => _deferredExit;

        internal static bool TryGetFullscreenRoot(out RectTransform? root)
        {
            root = _fullscreenRoot;
            return root != null;
        }

        internal static void ResetForMissionUnload()
        {
            _deferredExit = false;
            MissileCameraLossInterference.Stop();
            if (_active)
                Exit(force: true);

            MissileCameraVanillaHudBridge.ResetForMissionUnload();
            MissileCameraFullscreenBootstrap.ResetForMissionUnload();
            MissileCameraFullscreenFeedHost.ResetForMissionUnload();
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

            if (!MissileCameraFeedController.CanToggleFullscreen() && !_active)
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

        internal static void TickDeferredExit()
        {
            if (!_deferredExit)
                return;

            if (MissileCameraLossInterference.ConsumeExitCompletion())
                CompleteDeferredExit();
        }

        private static void RequestExit()
        {
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

            if (!MissileCameraFeedController.HasTrackableOwnedMissile())
            {
                MfdLog.Info("fullscreen enter blocked: no missiles");
                return;
            }

            _deferredExit = false;
            EnsureOverlayHost();
            if (_fullscreenRoot == null || _overlayGo == null || _overlayCanvas == null)
                return;

            MissileCameraFullscreenFeedHost.EnsureBuilt(_fullscreenRoot);
            if (MissileCameraFullscreenFeedHost.PanelRt == null)
            {
                MfdLog.Info("fullscreen enter blocked: feed host failed");
                return;
            }

            _overlayGo.SetActive(true);
            _overlayCanvas.enabled = true;
            _fullscreenRoot.SetAsLastSibling();
            MissileCameraFullscreenFeedHost.Show();

            _active = true;
            MissileCameraFeedController.NotifyFullscreenEntered();
            MissileCameraVanillaHudBridge.OnFullscreenEntered();
            MissileCameraFullscreenBootstrap.StartIfNeeded(MissileCameraFullscreenFeedHost.PanelRt);
            MissileCameraFeedController.NotifyFullscreenChanged();
            MfdLog.Info("fullscreen enter (independent feed host, CSM untouched)");
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

            MissileCameraFullscreenFeedHost.Hide();

            if (_overlayGo != null)
                _overlayGo.SetActive(false);

            if (_overlayCanvas != null)
                _overlayCanvas.enabled = true;

            try
            {
                MissileCameraFeedController.NotifyFullscreenExited();
                MissileCameraVanillaHudBridge.OnFullscreenExited();
            }
            catch (System.Exception ex)
            {
                MfdLog.Info("fullscreen hud exit error: " + ex.Message);
            }

            MissileCameraFullscreenConfig.Refresh();
            if (MissileCameraFullscreenConfig.ZoomResetOnExit)
                MissileCameraFeedController.ResetFullscreenMagnification();

            MissileCameraVisionModeController.Reset();
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
