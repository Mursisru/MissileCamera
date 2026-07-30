using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// Fullscreen missile feed host. Overlay lives in the active scene (NOT DontDestroyOnLoad).
    /// IsActive is true only while the live overlay exists — prevents sticky HUD/input after sortie change.
    /// Never touches CameraStateManager (see CAMERA_SAFETY.md).
    /// </summary>
    internal static class MissileCameraFullscreenController
    {
        private const int OverlaySortingOrder = 50;
        private const float ToggleDebounceSeconds = 0.15f;

        private static bool _sessionWanted;
        private static bool _deferredExit;
        private static float _nextToggleTimeUnscaled;
        private static GameObject? _overlayGo;
        private static Canvas? _overlayCanvas;
        private static RectTransform? _fullscreenRoot;

        /// <summary>
        /// True only while fullscreen overlay is wanted, alive, AND actually on screen.
        /// Screen-owner rule: hidden/disabled FS host must not steal MFD HUD or marker reproject.
        /// </summary>
        internal static bool IsActive
        {
            get
            {
                if (!_sessionWanted)
                    return false;

                if (!IsOverlayAlive())
                {
                    DropOrphanedSession("IsActive");
                    return false;
                }

                // Hidden host must not steal MFD HUD / marker reproject — soft abandon, not mission unload.
                if (_overlayGo != null && !_overlayGo.activeInHierarchy)
                {
                    SoftAbandonVisibleSession("overlay_inactive");
                    return false;
                }

                if (_overlayCanvas != null && !_overlayCanvas.enabled)
                {
                    SoftAbandonVisibleSession("canvas_disabled");
                    return false;
                }

                return true;
            }
        }

        internal static bool IsDeferredExit
        {
            get
            {
                if (!_deferredExit)
                    return false;

                if (_sessionWanted && !IsOverlayAlive())
                {
                    DropOrphanedSession("deferred");
                    return false;
                }

                return true;
            }
        }

        internal static bool TryGetFullscreenRoot(out RectTransform? root)
        {
            root = IsActive ? _fullscreenRoot : null;
            return root != null;
        }

        /// <summary>Flag-only drop — never Exit→RestoreHiddenChrome (dying/wrong scene).</summary>
        internal static void ResetForMissionUnload()
        {
            _nextToggleTimeUnscaled = 0f;
            DropOrphanedSession("reset");
        }

        internal static void HealIfOrphaned()
        {
            if (_sessionWanted || _deferredExit)
                _ = IsActive;
        }

        internal static void Toggle()
        {
            if (!MissileCameraFullscreenConfig.Enabled)
                return;

            float now = Time.unscaledTime;
            if (now < _nextToggleTimeUnscaled)
                return;

            if (_deferredExit)
            {
                CompleteDeferredExit();
                _nextToggleTimeUnscaled = now + ToggleDebounceSeconds;
                return;
            }

            if (!MissileCameraFeedController.CanToggleFullscreen() && !IsActive)
                return;

            if (IsActive)
                RequestExit();
            else
                Enter();

            _nextToggleTimeUnscaled = now + ToggleDebounceSeconds;
        }

        internal static void ExitIfActive()
        {
            _deferredExit = false;
            if (IsActive)
                Exit(force: true);
            else if (_sessionWanted)
                DropOrphanedSession("ExitIfActive");
        }

        internal static void TickDeferredExit()
        {
            if (!IsDeferredExit)
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
            if (IsActive)
                Exit(force: false);
            else if (_sessionWanted)
                DropOrphanedSession("deferred-complete");
            MfdLog.Info("fullscreen deferred exit complete");
        }

        internal static void TickYieldToVanillaUi()
        {
            if (!IsActive)
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
                MfdLog.Info("fullscreen enter blocked: map maximized");
                return;
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

            _sessionWanted = true;
            MissileCameraFeedController.NotifyFullscreenEntered();
            MissileCameraVanillaHudBridge.OnFullscreenEntered();
            MissileCameraFullscreenBootstrap.StartIfNeeded(MissileCameraFullscreenFeedHost.PanelRt);
            MissileCameraFeedController.NotifyFullscreenChanged();
            MfdLog.Info("fullscreen enter (scene-local overlay, CSM untouched)");
        }

        private static void Exit(bool force)
        {
            _deferredExit = false;
            _sessionWanted = false;

            try
            {
                MissileCameraFullscreenBootstrap.Abort();
            }
            catch
            {
                // ignore
            }

            MissileCameraFullscreenFeedHost.Hide();

            if (_overlayGo != null && _overlayGo)
                _overlayGo.SetActive(false);

            if (_overlayCanvas != null && _overlayCanvas)
                _overlayCanvas.enabled = true;

            try
            {
                MissileCameraFeedController.NotifyFullscreenExited();
                // Only restore vanilla chrome while we still have a live mission HUD scene.
                if (SceneSingleton<CombatHUD>.i != null)
                    MissileCameraVanillaHudBridge.OnFullscreenExited();
                else
                    MissileCameraVanillaHudBridge.ResetForMissionUnload();
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

        private static bool IsOverlayAlive() =>
            _overlayGo != null && _overlayGo;

        private static void SoftAbandonVisibleSession(string reason)
        {
            if (!_sessionWanted && !_deferredExit)
                return;

            _deferredExit = false;
            _sessionWanted = false;
            MissileCameraLossInterference.Stop();

            try { MissileCameraFullscreenFeedHost.Hide(); }
            catch { /* ignore */ }

            try
            {
                if (_overlayGo != null && _overlayGo)
                    _overlayGo.SetActive(false);
            }
            catch { /* ignore */ }

            try
            {
                MissileCameraFeedController.NotifyFullscreenExited();
                if (SceneSingleton<CombatHUD>.i != null)
                    MissileCameraVanillaHudBridge.OnFullscreenExited();
                else
                    MissileCameraVanillaHudBridge.ResetForMissionUnload();
            }
            catch { /* ignore */ }

            try { MissileCameraVisionModeController.Reset(); }
            catch { /* ignore */ }

            try { MissileCameraFeedController.NotifyFullscreenChanged(); }
            catch { /* ignore */ }

            MfdLog.Info("fullscreen soft-abandon reason=" + reason);
        }

        private static void DropOrphanedSession(string reason)
        {
            bool had = _sessionWanted || _deferredExit || _overlayGo != null;
            _deferredExit = false;
            _sessionWanted = false;
            MissileCameraLossInterference.Stop();
            DestroyOverlayHost();
            MissileCameraVanillaHudBridge.ResetForMissionUnload();
            MissileCameraFullscreenBootstrap.ResetForMissionUnload();
            MissileCameraFullscreenFeedHost.ResetForMissionUnload();
            if (had)
                MfdLog.Info("fullscreen session dropped reason=" + reason);
        }

        private static void EnsureOverlayHost()
        {
            if (IsOverlayAlive() && _fullscreenRoot != null && _overlayCanvas != null)
            {
                EnsureOverlayDoesNotBlockRaycasts();
                return;
            }

            DestroyOverlayHost();

            // Scene-local — destroyed with GameWorld. Never DontDestroyOnLoad (sticky FS root cause).
            _overlayGo = new GameObject("MissileCamera.GameFullscreen");
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
