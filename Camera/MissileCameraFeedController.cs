using System.Collections.Generic;
using MissileCamera.Config;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    internal static class MissileCameraFeedController
    {
        private static readonly List<Missile> OwnedActive = new List<Missile>();

        private static Aircraft? _subscribedAircraft;
        private static MissileCameraRig? _rig;
        private static RawImage? _feedImage;
        private static Text? _telemetryText;
        private static Text? _colorLabel;
        private static RectTransform? _layoutRoot;
        private static RectTransform? _panelRt;
        private static readonly MissileCameraHudOverlay HudOverlay = new MissileCameraHudOverlay();
        private static bool _overlayActive;
        private static Missile? _followedMissile;
        private static bool _manualFollowActive;
        private static float _zoomOffset;
        private static float _fullscreenMagnification = 1f;
        private static float _restoreAfterLossAtUnscaled = -1f;
        private static bool _postLossSequenceActive;
        private static float _nextRenderTimeUnscaled;
        private static float _nextReconcileTimeUnscaled;
        private static bool _loggedBind;
        private static RectTransform? _cachedLayoutRoot;
        private static float _cachedLayoutRotationZ = float.NaN;
        private static float _cachedPanelW = -1f;
        private static float _cachedPanelH = -1f;
        private static MissileCameraPanelMetrics _cachedPanelMetrics;
        private static float _nextReconcileBackoff = 2f;
        private static MissileCameraHudSnapshot _cachedSnapshot = MissileCameraHudSnapshot.Empty;
        private static float _nextHudSnapshotTime;
        private static float _nextHudVisualTime;
        private static float _nextCornerHudTime;
        private static float _nextConfigRefreshTime;
        private const float HudSnapshotInterval = 1f / 10f;
        private const float FullscreenHudSnapshotInterval = 1f / 10f;
        private const float CornerHudInterval = 1f / 10f;
        private const float ConfigRefreshInterval = 1f;

        internal static bool UseIdleDriverWait { get; private set; }

        internal static void Shutdown()
        {
            ResetForMissionUnload();
            MissileCameraInfraredEffect.Shutdown();
            MissileCameraLossInterference.Shutdown();
            MissileCameraAircraftCamController.Shutdown();
            MissileCameraCockpitPipController.Shutdown();
            MissileCameraFullscreenController.ExitIfActive();
            _rig?.Destroy();
            _rig = null;
        }

        /// <summary>
        /// Drop DDOL feed/layout session without EnableCanvas / chrome restore.
        /// Call after FullscreenController.ResetForMissionUnload.
        /// </summary>
        internal static void ResetForMissionUnload() => HardResetForMissionUnload();

        internal static void HardResetForMissionUnload()
        {
            try { NotifyOverlayGone(); }
            catch { /* ignore */ }

            try { TryUnbindAircraft(); }
            catch { /* ignore */ }

            OwnedActive.Clear();

            try { MissileCameraSalvoTracker.Reset(); }
            catch { /* ignore */ }

            try { MissileCameraPostFxStack.Release(); }
            catch { /* ignore */ }

            _postLossSequenceActive = false;
            _manualFollowActive = false;
            _followedMissile = null;
            _zoomOffset = 0f;
            _fullscreenMagnification = 1f;
            _restoreAfterLossAtUnscaled = -1f;
            _nextRenderTimeUnscaled = 0f;
            _nextReconcileTimeUnscaled = 0f;
            _nextHudSnapshotTime = 0f;
            _nextHudVisualTime = 0f;
            _nextCornerHudTime = 0f;
            _nextConfigRefreshTime = 0f;
            _nextReconcileBackoff = 2f;
            _loggedBind = false;
            _cachedSnapshot = MissileCameraHudSnapshot.Empty;
            _panelRt = null;
            _feedImage = null;
            _layoutRoot = null;
            _telemetryText = null;
            _colorLabel = null;
            _cachedLayoutRoot = null;
            _rig = null;
            UseIdleDriverWait = true;

            try { MissileCameraVisionModeController.Reset(); }
            catch { /* ignore */ }

            try { MissileCameraInfraredPolicy.Reset(); }
            catch { /* ignore */ }

            try { MissileCameraHudSnapshot.ResetSmoothing(); }
            catch { /* ignore */ }

            try { MissileCameraRenderPrep.ResetAll(); }
            catch { /* ignore */ }
        }

        /// <summary>
        /// Off-session: if weapons/FS chrome hide lists still hold live objects, unhide.
        /// </summary>
        private static void HealStickyVanillaHides()
        {
            try { MfdWeaponsZoneAccess.HealStickyIfNeeded(); }
            catch { /* ignore */ }

            try { MissileCameraVanillaHudBridge.HealStickyIfNeeded(); }
            catch { /* ignore */ }
        }

        internal static void SelectNextMissile()
        {
            if (!IsDisplayPipelineActive() || !HasTrackableOwnedMissile())
                return;

            Missile? current = _followedMissile ?? PickLatestMissile();
            Missile? next = MissileCameraFeedSelection.CycleCurrent(OwnedActive, current, 1);
            ApplyManualSelection(next);
            MissileCameraFullscreenTargetLock.OnFollowedMissileChanged();
            NotifyCameraSwitchInterference(current, next);
        }

        internal static void SelectPreviousMissile()
        {
            if (!IsDisplayPipelineActive() || !HasTrackableOwnedMissile())
                return;

            Missile? current = _followedMissile ?? PickLatestMissile();
            Missile? previous = MissileCameraFeedSelection.CycleCurrent(OwnedActive, current, -1);
            ApplyManualSelection(previous);
            MissileCameraFullscreenTargetLock.OnFollowedMissileChanged();
            NotifyCameraSwitchInterference(current, previous);
        }

        private static void NotifyCameraSwitchInterference(Missile? from, Missile? to)
        {
            if (to == null || ReferenceEquals(from, to))
                return;

            float seconds = Mathf.Max(MissileCameraFeedConfig.PostLossInterferenceSeconds, 0.05f);
            MissileCameraLossInterference.BeginSwitch(seconds);
        }

        internal static float FullscreenMagnification => _fullscreenMagnification;

        internal static void AdjustZoom(float delta)
        {
            if (!_overlayActive || MissileCameraFullscreenController.IsActive)
                return;

            float newOffset = MissileCameraControlsConfig.ClampZoomOffset(_zoomOffset + delta);
            if (Mathf.Approximately(newOffset, _zoomOffset))
                return;

            _zoomOffset = newOffset;
            EnsureRig().SetZoomOffset(_zoomOffset);
            HudOverlay.NotifyZoomChanged(_zoomOffset);
        }

        internal static void ResetZoom()
        {
            if (!_overlayActive || MissileCameraFullscreenController.IsActive)
                return;

            _zoomOffset = 0f;
            if (_rig != null)
                _rig.SetZoomOffset(_zoomOffset);

            HudOverlay.NotifyZoomChanged(_zoomOffset);
        }

        internal static void MultiplyFullscreenMagnification(float multiplier)
        {
            if (!_overlayActive || !MissileCameraFullscreenController.IsActive)
                return;

            if (multiplier <= 0f || float.IsNaN(multiplier) || float.IsInfinity(multiplier))
                return;

            float next = MissileCameraControlsConfig.ClampFullscreenMagnification(_fullscreenMagnification * multiplier);
            if (Mathf.Approximately(next, _fullscreenMagnification))
                return;

            _fullscreenMagnification = next;
            ApplyFullscreenOptics();
            ResolveHudOverlay().NotifyZoomChanged(_fullscreenMagnification);
        }

        internal static void ResetFullscreenMagnification()
        {
            if (!Mathf.Approximately(_fullscreenMagnification, 1f))
            {
                _fullscreenMagnification = 1f;
                if (_overlayActive && MissileCameraFullscreenController.IsActive)
                {
                    ApplyFullscreenOptics();
                    ResolveHudOverlay().NotifyZoomChanged(_fullscreenMagnification);
                }
            }
            else if (_overlayActive && MissileCameraFullscreenController.IsActive)
            {
                ApplyFullscreenOptics();
            }
        }

        private static void ApplyFullscreenOptics()
        {
            MissileCameraRig rig = EnsureRig();
            rig.SetFullscreenMagnification(_fullscreenMagnification);
            _nextRenderTimeUnscaled = 0f;
            NotifyFullscreenChanged();
        }

        internal static void Tick()
        {
            // DDOL driver keeps ticking across scenes — never EnsureLayout/ApplyHidden off-session.
            if (!MissileCameraHost.IsSessionActive)
            {
                UseIdleDriverWait = true;
                MissileCameraFullscreenController.HealIfOrphaned();
                HealStickyVanillaHides();
                return;
            }

            RefreshConfigsIfDue();
            MissileCameraFullscreenController.HealIfOrphaned();
            MissileCameraFullscreenController.TickYieldToVanillaUi();

            // Mid-sortie sticky chrome from FS markers-only — heal while session is alive.
            if (!MissileCameraFullscreenController.IsActive)
            {
                try { MissileCameraVanillaHudBridge.HealStickyIfNeeded(); }
                catch { /* ignore */ }
            }

            // Fullscreen/MFD NO SIGNAL burst (switch / destroy / exit-no-missile).
            MissileCameraLossInterference.Tick(ResolveFeedImage());
            MissileCameraFullscreenController.TickDeferredExit();

            if (!MissileCameraFeedConfig.Enabled)
            {
                UseIdleDriverWait = true;
                DetachRig();
                UpdateDisplay(null);
                TryUnbindAircraft();
                return;
            }

            TryBindLocalAircraft();
            if (_subscribedAircraft != null)
                ReconcileOwnedMissiles(_subscribedAircraft);
            PruneOwnedMissiles();

            // Scene may have destroyed TacStub while DDOL left _overlayActive sticky.
            if (_overlayActive && (_panelRt == null || !_panelRt))
                NotifyOverlayGone();

            if (HasTrackableOwnedMissile() && !_overlayActive)
                MfdLayoutController.EnsureLayoutForMissileFeed();

            if (!IsDisplayPipelineActive())
            {
                UseIdleDriverWait = !HasTrackableOwnedMissile();
                if (!MissileCameraFullscreenController.IsActive)
                    DetachRig();
                UpdateDisplay(null);
                return;
            }

            UseIdleDriverWait = false;

            MissileCameraFeedInput.Process();

            bool fullscreen = MissileCameraFullscreenController.IsActive;

            Missile? missile = ResolveFollowedMissile();
            if (missile == null)
            {
                HandleMissileLost();
                return;
            }

            if (_postLossSequenceActive)
                CancelPostLossSequence();

            _restoreAfterLossAtUnscaled = -1f;
            if (_followedMissile != missile)
            {
                _followedMissile = missile;
                _nextHudSnapshotTime = 0f;
                _nextCornerHudTime = 0f;
                if (fullscreen)
                    MissileCameraFullscreenTargetLock.OnFollowedMissileChanged();
            }

            MissileCameraRig rig = EnsureRig();
            if (fullscreen)
                rig.SetFullscreenMagnification(_fullscreenMagnification);
            else
                rig.SetZoomOffset(_zoomOffset);
            rig.Attach(missile);
            rig.AdvanceRoll(Time.deltaTime);

            Vector3 missilePos = missile.transform.position;
            bool autoInfrared = MissileCameraInfraredPolicy.Evaluate(missilePos, out float exposure);

            // Dedicated feed RT → RawImage only. Never touch CameraStateManager (CAMERA_SAFETY).
            // COLOR/NVG: pipeline-driven (camera enabled → ParticleSystem culling + URP draw).
            // IR blit modes: manual RenderFrame (HDR→blit); camera stays enabled as Overlay between frames.
            bool needIrBlit = fullscreen
                ? MissileCameraVisionModeController.UsesInfraredBlit(MissileCameraVisionModeController.Mode)
                : autoInfrared;
            rig.SetPipelineDriven(!needIrBlit);

            if (fullscreen)
                MissileCameraVanillaHudBridge.TickHideStubs();

            RawImage? feedImage = ResolveFeedImage();
            if (fullscreen && MissileCameraLossInterference.IsActive)
            {
                MissileCameraInfraredEffect.Clear(feedImage, rig);
            }
            else if (fullscreen)
            {
                MissileCameraInfraredEffect.ApplyFullscreenVision(
                    feedImage,
                    rig,
                    MissileCameraVisionModeController.Mode,
                    exposure);
            }
            else
            {
                MissileCameraInfraredEffect.Apply(feedImage, _rig, autoInfrared, exposure);
            }

            MissileCameraAircraftCamController.Tick();
            RectTransform? pipPanel = TryGetPanelRt();
            MissileCameraHudOverlay activeHud = ResolveHudOverlay();
            if (pipPanel != null && activeHud.Root != null)
                MissileCameraCockpitPipController.Tick(activeHud.Root, GetPanelMetrics(pipPanel));

            if (Time.unscaledTime >= _nextRenderTimeUnscaled
                || (fullscreen && !MissileCameraLossInterference.IsActive))
            {
                float interval = 1f / Mathf.Max(MissileCameraFeedConfig.RenderFps, 1);
                _nextRenderTimeUnscaled = Time.unscaledTime + interval;
                rig.SyncPose();

                if (!rig.IsPipelineDriven)
                {
                    bool aircraftMulti = MissileCameraAircraftCamConfig.Enabled && MfdLayoutController.IsLayoutActive;
                    bool cockpitMulti = MissileCameraCockpitPipController.IsActive;
                    bool multi = !fullscreen && (aircraftMulti || cockpitMulti);
                    if (multi)
                        MissileCameraFrameRenderContext.BeginMultiRender();

                    try
                    {
                        if (multi)
                            MissileCameraFrameRenderContext.PrepareCamera(rig.FeedCamera, forceLdr: false);

                        rig.RenderFrame(managePrep: !multi);
                        if (!fullscreen)
                        {
                            MissileCameraAircraftCamController.RenderIfDue(useSharedPrep: multi);
                            MissileCameraCockpitPipController.RenderIfDue(useSharedPrep: multi);
                        }
                    }
                    finally
                    {
                        if (multi)
                            MissileCameraFrameRenderContext.FinishMultiRender();
                    }
                }
                else if (!fullscreen)
                {
                    MissileCameraAircraftCamController.RenderIfDue(useSharedPrep: false);
                    MissileCameraCockpitPipController.RenderIfDue(useSharedPrep: false);
                }
            }

            UpdateDisplay(missile);
        }

        internal static RectTransform? TryGetPanelRt() =>
            MissileCameraFullscreenController.IsActive
                ? MissileCameraFullscreenFeedHost.PanelRt ?? _panelRt
                : _panelRt;

        internal static bool CanToggleFullscreen() => HasTrackableOwnedMissile();

        internal static void NotifyFullscreenEntered()
        {
            _cachedPanelW = -1f;
            _cachedPanelH = -1f;
            MissileCameraFullscreenFeedHost.Hud.InvalidateCornerLayout();
            MissileCameraFullscreenFeedHost.Hud.InvalidateDynamicSchedule();
        }

        internal static void NotifyFullscreenExited()
        {
            _cachedPanelW = -1f;
            _cachedPanelH = -1f;
            HudOverlay.InvalidateCornerLayout();
            HudOverlay.InvalidateDynamicSchedule();
            _nextCornerHudTime = 0f;
            _nextHudVisualTime = 0f;

            // Re-bind MFD HUD after FS; TargetCam may have disabled during FS yield.
            if (HasTrackableOwnedMissile())
            {
                try { MfdLayoutController.EnsureLayoutForMissileFeed(); }
                catch { /* ignore */ }

                if (_panelRt != null && _layoutRoot != null)
                {
                    try
                    {
                        HudOverlay.EnsureBuilt(_layoutRoot, MfdLayoutController.GetActiveScreenUi());
                        MissileCameraHudOverlay.ApplyLegacyStubVisibility(_panelRt, hide: true);
                    }
                    catch { /* ignore */ }
                }
            }

            try { MissileCameraVanillaHudBridge.HealStickyIfNeeded(); }
            catch { /* ignore */ }

            MissileCameraCombatHudMarkerProjection.RestoreMarkerImages();
        }

        internal static Texture? TryGetFeedTexture()
        {
            RawImage? feed = ResolveFeedImage();
            if (_rig != null && _rig.IsRootAlive)
                return _rig.Texture;
            return feed != null ? feed.texture : null;
        }

        /// <summary>Attach + render one seeker frame so boot puzzle has a live RT.</summary>
        internal static Texture? EnsureFeedReadyForBoot()
        {
            Missile? missile = ResolveFollowedMissile();
            if (missile == null)
                return TryGetFeedTexture();

            MissileCameraRig rig = EnsureRig();
            if (MissileCameraFullscreenController.IsActive)
                rig.SetFullscreenMagnification(_fullscreenMagnification);
            else
                rig.SetZoomOffset(_zoomOffset);

            rig.Attach(missile);
            rig.SyncPose();
            rig.SetPipelineDriven(false);
            try
            {
                rig.RenderFrame(managePrep: true);
            }
            catch (System.Exception ex)
            {
                MfdLog.Info("boot feed render: " + ex.Message);
            }

            RenderTexture? tex = rig.Texture;
            RawImage? feedImage = ResolveFeedImage();
            if (feedImage != null && tex != null)
            {
                feedImage.texture = tex;
                feedImage.enabled = true;
            }

            return tex;
        }

        /// <summary>One-shot FLIR fill for boot text capture (skips when boot not using overlay).</summary>
        internal static void RefreshFlirHudOnce()
        {
            RectTransform? panelRt = TryGetPanelRt();
            if (ResolveLayoutRoot() == null || panelRt == null)
                return;

            Missile? missile = _followedMissile;
            if (missile == null || missile.disabled)
                missile = PickLatestMissile();

            if (!MissileCameraFullscreenController.IsActive)
                SyncFeedLayout();

            MissileCameraHudSnapshot snapshot = ResolveHudSnapshot(missile);
            MissileCameraPanelMetrics panel = GetPanelMetrics(panelRt);
            ResolveHudOverlay().ForceFlirUpdate(snapshot, panel);
        }

        internal static Missile? TryGetFollowedMissile() =>
            MissileCameraFeedSelection.IsStillTrackable(_followedMissile) ? _followedMissile : null;

        internal static float TryGetBoreRollDeg() =>
            _rig != null && _rig.IsRootAlive ? _rig.BoreRollDeg : 0f;

        /// <summary>Active seeker feed camera while the rig exists (MFD or fullscreen).</summary>
        internal static Camera? TryGetFeedCamera()
        {
            if (_rig == null || !_rig.IsRootAlive)
                return null;

            Camera feed = _rig.FeedCamera;
            return feed != null ? feed : null;
        }

        internal static void NotifyFullscreenChanged()
        {
            _cachedPanelW = -1f;
            _cachedPanelH = -1f;
            _nextRenderTimeUnscaled = 0f;
            _nextHudSnapshotTime = 0f;
            _nextCornerHudTime = 0f;
            HudOverlay.InvalidateCornerLayout();
            HudOverlay.InvalidateDynamicSchedule();
            MissileCameraFullscreenFeedHost.Hud.InvalidateCornerLayout();
            MissileCameraFullscreenFeedHost.Hud.InvalidateDynamicSchedule();

            if (_rig != null)
            {
                // Force RT recreate at fullscreen / MFD resolution on next render.
                MissileCameraFeedConfig.ResolveActiveFeedSize(out int w, out int h);
                MfdLog.Info($"fullscreen layout feedRT={w}x{h}");
            }
        }

        internal static void NotifyOverlayReady(RectTransform panelRt)
        {
            if (!MissileCameraHost.IsSessionActive || panelRt == null)
                return;

            _overlayActive = true;
            _loggedBind = false;
            BindPanel(panelRt);
        }

        internal static void NotifyOverlayGone()
        {
            _overlayActive = false;

            try { CancelPostLossSequence(); }
            catch { /* ignore */ }

            try { MissileCameraInfraredEffect.Clear(_feedImage, _rig); }
            catch { /* ignore */ }

            try { MissileCameraPostFxStack.Release(); }
            catch { /* ignore */ }

            _feedImage = null;
            _telemetryText = null;
            _colorLabel = null;
            _layoutRoot = null;
            _panelRt = null;
            _cachedLayoutRoot = null;
            _cachedLayoutRotationZ = float.NaN;
            _cachedPanelW = -1f;
            _cachedPanelH = -1f;

            try { HudOverlay.Destroy(); }
            catch { /* ignore */ }

            _manualFollowActive = false;
            _zoomOffset = 0f;

            try { DetachRig(); }
            catch { /* ignore */ }

            try { MissileCameraTelemetry.ResetThrottle(); }
            catch { /* ignore */ }
        }

        private static void BindPanel(RectTransform panelRt)
        {
            RectTransform layoutRt = MfdLayoutController.ResolveFeedLayoutRoot(panelRt);
            bool portrait = IsPortraitFeedLayout(layoutRt);
            float contentRotationZ = MfdLayoutController.ActiveStubContentRotationZ;
            RectTransform viewRt = MissileCameraFeedLayout.EnsureRotatedView(layoutRt, contentRotationZ);
            RawImage feed = EnsureFeedImage(layoutRt, viewRt);
            MissileCameraFeedLayout.Apply(layoutRt, portrait, contentRotationZ);
            _feedImage = feed;
            _layoutRoot = layoutRt;
            _panelRt = panelRt;
            _cachedLayoutRoot = null;
            _cachedLayoutRotationZ = float.NaN;
            _cachedPanelW = -1f;
            _cachedPanelH = -1f;
            _telemetryText = FindChildText(panelRt, "MissileTelemetry");
            _colorLabel = FindChildText(panelRt, "MissileCameraColor");
            if (_colorLabel != null)
                _colorLabel.text = "COLOR";

            HudOverlay.EnsureBuilt(layoutRt, MfdLayoutController.GetActiveScreenUi());
            HudOverlay.InvalidateDynamicSchedule();
            if (MissileCameraHudConfig.Enabled)
                MissileCameraHudOverlay.ApplyLegacyStubVisibility(panelRt, hide: true);
            if (panelRt.TryGetComponent(out Image panelImage))
                MissileCameraHudOverlay.ApplyPanelBackground(panelImage, MfdLayoutController.GetActiveScreenUi());

            if (!_loggedBind)
            {
                _loggedBind = true;
                MfdLog.Info($"missileCam feed bind portrait={portrait}");
            }
        }

        private static bool IsPortraitFeedLayout(RectTransform layoutRt)
        {
            Transform? title = layoutRt.Find("MissileCameraTitle");
            if (title != null && title.TryGetComponent(out RectTransform titleRt))
                return Mathf.Approximately(titleRt.anchorMin.x, titleRt.anchorMax.x);

            float w = Mathf.Max(layoutRt.rect.width, 1f);
            float h = Mathf.Max(layoutRt.rect.height, 1f);
            return h >= w * 1.2f;
        }

        private static RawImage EnsureFeedImage(RectTransform layoutRt, RectTransform viewRt)
        {
            Transform? existing = viewRt.Find("MissileCameraFeed");
            if (existing == null)
                existing = layoutRt.Find("MissileCameraFeed");

            if (existing != null && existing.TryGetComponent(out RawImage existingImage))
            {
                if (existing.parent != viewRt)
                    existing.SetParent(viewRt, false);

                return existingImage;
            }

            var feedGo = new GameObject("MissileCameraFeed", typeof(RectTransform), typeof(RawImage));
            feedGo.transform.SetParent(viewRt, false);
            feedGo.transform.SetAsFirstSibling();

            RawImage feed = feedGo.GetComponent<RawImage>();
            feed.raycastTarget = false;
            feed.color = Color.white;
            return feed;
        }

        private static Text? FindChildText(RectTransform searchRoot, string childName)
        {
            Transform? child = searchRoot.Find(childName);
            return child != null && child.TryGetComponent(out Text text) ? text : null;
        }

        private static void SyncFeedLayout()
        {
            if (_layoutRoot == null)
                return;

            float contentRotationZ = MfdLayoutController.ActiveStubContentRotationZ;
            bool layoutDirty = _layoutRoot != _cachedLayoutRoot
                || !Mathf.Approximately(_cachedLayoutRotationZ, contentRotationZ);

            if (layoutDirty)
            {
                bool portrait = IsPortraitFeedLayout(_layoutRoot);
                MissileCameraFeedLayout.Apply(_layoutRoot, portrait, contentRotationZ);
                _cachedLayoutRoot = _layoutRoot;
                _cachedLayoutRotationZ = contentRotationZ;
                _cachedPanelW = -1f;
                _cachedPanelH = -1f;
                HudOverlay.InvalidateCornerLayout();
                return;
            }

            MissileCameraFeedLayout.ApplyContentRotation(_layoutRoot, contentRotationZ);
            _cachedLayoutRotationZ = contentRotationZ;
        }

        private static void UpdateDisplay(Missile? missile)
        {
            RenderTexture? texture = missile != null && _rig != null ? _rig.Texture : null;
            if (texture != null && missile != null)
            {
                bool infrared = MissileCameraInfraredPolicy.InfraredActive;
                texture = MissileCameraPostFxStack.Apply(texture, infrared, MissileCameraInfraredPolicy.Exposure) ?? texture;
            }

            RawImage? feedImage = ResolveFeedImage();
            if (feedImage != null)
            {
                if (MissileCameraLossInterference.IsActive
                    && MissileCameraLossInterference.ActiveKind == MissileCameraLossInterference.BurstKind.ExitShutdown)
                {
                    feedImage.enabled = false;
                }
                else if (MissileCameraLossInterference.IsActive || _postLossSequenceActive)
                {
                    // Destroy/switch NO SIGNAL covers feed (MFD host or fullscreen overlay).
                }
                else
                {
                    if (feedImage.texture != texture)
                        feedImage.texture = texture;
                    bool show = texture != null;
                    if (feedImage.enabled != show)
                        feedImage.enabled = show;
                }
            }

            if (missile == null || texture == null)
                MissileCameraInfraredEffect.Apply(feedImage, _rig, infrared: false, exposure: 0f);

            if (_telemetryText != null)
            {
                if (MissileCameraFullscreenController.IsActive || MissileCameraHudConfig.Enabled)
                {
                    _telemetryText.text = string.Empty;
                    _telemetryText.enabled = false;
                }
                else
                    MissileCameraTelemetry.Update(_telemetryText, missile);
            }

            if (_colorLabel != null)
            {
                if (MissileCameraFullscreenController.IsActive || MissileCameraHudConfig.Enabled)
                {
                    _colorLabel.text = string.Empty;
                    _colorLabel.enabled = false;
                }
            }

            RectTransform? panelRt = TryGetPanelRt();
            RectTransform? layoutRoot = ResolveLayoutRoot();
            if (layoutRoot != null && panelRt != null)
            {
                bool fullscreen = MissileCameraFullscreenController.IsActive;
                bool updateCorners = fullscreen
                    || missile == null
                    || Time.unscaledTime >= _nextCornerHudTime;
                bool updateDynamic = fullscreen
                    || (missile != null && Time.unscaledTime >= _nextHudVisualTime);
                if (missile == null || updateCorners || updateDynamic)
                {
                    if (missile != null && !fullscreen)
                    {
                        if (updateCorners)
                            _nextCornerHudTime = Time.unscaledTime + CornerHudInterval;
                        if (updateDynamic)
                            _nextHudVisualTime = Time.unscaledTime + HudSnapshotInterval;
                    }

                    if (!fullscreen)
                        SyncFeedLayout();

                    RectTransform viewRt = ResolveViewRt() ?? layoutRoot;
                    MissileCameraHudSnapshot snapshot = ResolveHudSnapshot(missile);
                    Camera? feedCamera = _rig?.FeedCamera;
                    MissileCameraPanelMetrics panel = GetPanelMetrics(panelRt);
                    ResolveHudOverlay().Update(
                        snapshot,
                        layoutRoot,
                        viewRt,
                        feedCamera,
                        panel,
                        panelRt,
                        updateCorners,
                        updateDynamic);
                }
            }
        }

        private static void RefreshConfigsIfDue()
        {
            float now = Time.unscaledTime;
            if (now < _nextConfigRefreshTime)
                return;

            _nextConfigRefreshTime = now + ConfigRefreshInterval;
            MissileCameraKeybindConfig.Refresh();
            MissileCameraFeedConfig.Refresh();
            MissileCameraHudConfig.Refresh();
            MissileCameraControlsConfig.Refresh();
            MissileCameraFullscreenConfig.Refresh();
            MissileCameraTelemetryConfig.Refresh();
            MissileCameraEffectsConfig.Refresh();
            MissileCameraAircraftCamConfig.Refresh();
        }

        private static void ApplyManualSelection(Missile? missile)
        {
            if (missile == null)
                return;

            _manualFollowActive = true;
            if (_followedMissile == missile)
                return;

            _followedMissile = missile;
            _nextHudSnapshotTime = 0f;
            _nextCornerHudTime = 0f;
        }

        private static Missile? ResolveFollowedMissile()
        {
            if (_manualFollowActive)
            {
                if (MissileCameraFeedSelection.IsStillTrackable(_followedMissile))
                    return _followedMissile;

                return MissileCameraFeedSelection.ResolveFallbackNewest(OwnedActive);
            }

            return PickLatestMissile();
        }

        private static MissileCameraHudSnapshot ResolveHudSnapshot(Missile? missile)
        {
            if (missile == null)
            {
                _cachedSnapshot = MissileCameraHudSnapshot.Empty;
                _nextHudSnapshotTime = 0f;
                return MissileCameraHudSnapshot.Empty;
            }

            float now = Time.unscaledTime;
            if (now >= _nextHudSnapshotTime)
            {
                float interval = MissileCameraFullscreenController.IsActive
                    ? FullscreenHudSnapshotInterval
                    : HudSnapshotInterval;
                _nextHudSnapshotTime = now + interval;
                _cachedSnapshot = MissileCameraHudSnapshot.Build(missile, _rig, OwnedActive);
            }

            return _cachedSnapshot;
        }

        private static MissileCameraPanelMetrics GetPanelMetrics(RectTransform panelRt)
        {
            float w = Mathf.Abs(panelRt.rect.width);
            float h = Mathf.Abs(panelRt.rect.height);
            if (Mathf.Approximately(w, _cachedPanelW) && Mathf.Approximately(h, _cachedPanelH))
                return _cachedPanelMetrics;

            _cachedPanelW = w;
            _cachedPanelH = h;
            _cachedPanelMetrics = MissileCameraPanelMetrics.From(panelRt, forceCanvasUpdate: true);
            return _cachedPanelMetrics;
        }

        private static void HandleMissileLost()
        {
            if (!_overlayActive && !MissileCameraFullscreenController.IsActive)
            {
                FinishPostLossCleanup();
                return;
            }

            if (!_postLossSequenceActive)
                BeginPostLossSequence();

            // Interference is ticked globally in Tick(); wait while destroy burst plays.
            if (MissileCameraLossInterference.IsActive
                && MissileCameraLossInterference.ActiveKind == MissileCameraLossInterference.BurstKind.Destroy)
            {
                UseIdleDriverWait = false;
                UpdatePostLossHud();
                return;
            }

            float linger = MissileCameraFeedConfig.PostExplosionHoldSeconds;
            if (linger > 0f)
            {
                if (_restoreAfterLossAtUnscaled < 0f)
                    _restoreAfterLossAtUnscaled = Time.unscaledTime + linger;

                if (Time.unscaledTime < _restoreAfterLossAtUnscaled)
                {
                    UseIdleDriverWait = false;
                    return;
                }
            }

            FinishPostLossCleanup();
        }

        private static void BeginPostLossSequence()
        {
            _postLossSequenceActive = true;
            _restoreAfterLossAtUnscaled = -1f;
            DetachRig();
            RawImage? feedImage = ResolveFeedImage();
            MissileCameraInfraredEffect.Clear(feedImage, null);

            if (feedImage != null)
            {
                feedImage.enabled = true;
                feedImage.color = Color.white;
            }

            if (_colorLabel != null)
                _colorLabel.text = "LOST";

            float interferenceSeconds = Mathf.Max(MissileCameraFeedConfig.PostLossInterferenceSeconds, 0.05f);
            if (interferenceSeconds > 0f)
                MissileCameraLossInterference.BeginDestroy(interferenceSeconds);
            else
                MissileCameraLossInterference.Stop();
        }

        private static void CancelPostLossSequence()
        {
            _postLossSequenceActive = false;
            _restoreAfterLossAtUnscaled = -1f;
            MissileCameraLossInterference.Stop();
            if (_colorLabel != null)
                _colorLabel.text = "COLOR";
        }

        private static void FinishPostLossCleanup()
        {
            CancelPostLossSequence();
            RawImage? feedImage = ResolveFeedImage();
            if (feedImage != null)
            {
                feedImage.texture = null;
                feedImage.enabled = false;
            }

            // After impact NO SIGNAL: leave fullscreen (cockpit camera was never hijacked).
            MissileCameraFullscreenController.ExitIfActive();

            UpdateDisplay(null);
            TryReleaseLayout();
        }

        private static void UpdatePostLossHud()
        {
            MissileCameraTelemetry.Update(_telemetryText, null);

            RectTransform? panelRt = TryGetPanelRt();
            RectTransform? layoutRoot = ResolveLayoutRoot();
            if (layoutRoot == null || panelRt == null)
                return;

            if (!MissileCameraFullscreenController.IsActive)
                SyncFeedLayout();

            RectTransform viewRt = ResolveViewRt() ?? layoutRoot;
            MissileCameraPanelMetrics panel = GetPanelMetrics(panelRt);
            ResolveHudOverlay().Update(
                MissileCameraHudSnapshot.Empty,
                layoutRoot,
                viewRt,
                feedCamera: null,
                panel,
                panelRt,
                updateCorners: true,
                updateDynamic: true);
        }

        private static void TryReleaseLayout()
        {
            if (!ShouldRetainLayoutForMissileFeed())
                MfdLayoutController.ReleaseLayoutIfNoMissileFeed();
        }

        /// <summary>Keep MFD overlay while missiles fly (match working main — no FS/interference retain).</summary>
        internal static bool ShouldRetainLayoutForMissileFeed() =>
            HasTrackableOwnedMissile();

        private static void DetachRig()
        {
            _followedMissile = null;

            try { MissileCameraRenderPrep.ForceRestoreWorldState(); }
            catch { /* ignore */ }

            MissileCameraRig? rig = _rig;
            _rig = null;
            if (rig == null)
                return;

            try { rig.SetPipelineDriven(false); }
            catch { /* ignore */ }

            try { rig.Detach(); }
            catch { /* ignore */ }
        }

        internal static bool HasTrackableOwnedMissile()
        {
            for (int i = 0; i < OwnedActive.Count; i++)
            {
                if (IsTrackableMissile(OwnedActive[i]))
                    return true;
            }

            return false;
        }

        internal static bool HasOverlayInputContext() =>
            _overlayActive && HasTrackableOwnedMissile();

        private static MissileCameraRig EnsureRig()
        {
            if (_rig != null && !_rig.IsRootAlive)
                _rig = null;

            if (_rig == null)
                _rig = new MissileCameraRig();
            return _rig;
        }

        private static Missile? PickLatestMissile()
        {
            Missile? newest = null;
            float youngestAge = float.MaxValue;

            for (int i = OwnedActive.Count - 1; i >= 0; i--)
            {
                Missile missile = OwnedActive[i];
                if (!IsTrackableMissile(missile))
                {
                    OwnedActive.RemoveAt(i);
                    continue;
                }

                if (missile.timeSinceSpawn < youngestAge)
                {
                    youngestAge = missile.timeSinceSpawn;
                    newest = missile;
                }
            }

            return newest;
        }

        private static void PruneOwnedMissiles()
        {
            for (int i = OwnedActive.Count - 1; i >= 0; i--)
            {
                Missile? missile = OwnedActive[i];
                if (missile == null || missile.disabled || !HasRigidbody(missile))
                    OwnedActive.RemoveAt(i);
            }
        }

        private static void ReconcileOwnedMissiles(Aircraft aircraft, bool force = false)
        {
            if (!force && Time.unscaledTime < _nextReconcileTimeUnscaled)
                return;

            _nextReconcileTimeUnscaled = Time.unscaledTime + _nextReconcileBackoff;

            if (!force && OwnedActive.Count > 0)
                return;

            Missile[] missiles = Object.FindObjectsOfType<Missile>();
            for (int i = 0; i < missiles.Length; i++)
            {
                Missile missile = missiles[i];
                if (!IsOwnedByAircraft(missile, aircraft) || !IsTrackableMissile(missile))
                    continue;

                if (!OwnedActive.Contains(missile))
                {
                    OwnedActive.Add(missile);
                    MfdLog.Info($"missileCam reconcile add id={missile.persistentID} age={missile.timeSinceSpawn:F2}");
                }
            }
        }

        private static bool IsOwnedByAircraft(Missile missile, Aircraft aircraft)
        {
            if (missile.owner == aircraft)
                return true;

            return missile.ownerID == aircraft.persistentID;
        }

        private static bool IsTrackableMissile(Missile missile)
        {
            if (missile == null || missile.disabled)
                return false;

            return HasRigidbody(missile);
        }

        private static bool HasRigidbody(Missile missile)
        {
            if (missile.rb != null)
                return true;

            return missile.GetComponent<Rigidbody>() != null;
        }

        private static void TryBindLocalAircraft()
        {
            bool getLocal = GameManager.GetLocalAircraft(out Aircraft aircraft);

            if (!getLocal)
            {
                if (_subscribedAircraft != null)
                {
                    TryUnbindAircraft();
                    OwnedActive.Clear();
                    _followedMissile = null;
                    MfdLayoutController.HardResetForAircraftChange();
                }

                return;
            }

            if (_subscribedAircraft == aircraft)
                return;

            Aircraft? previous = _subscribedAircraft;
            TryUnbindAircraft();

            // First bind after mission start is not a swap — do not wipe layout/missiles.
            if (previous != null)
            {
                OwnedActive.Clear();
                _followedMissile = null;
                MfdLayoutController.HardResetForAircraftChange();
            }

            _subscribedAircraft = aircraft;
            _subscribedAircraft.onRegisterMissile += OnRegisterMissile;
            _subscribedAircraft.onDeregisterMissile += OnDeregisterMissile;
            _nextReconcileTimeUnscaled = 0f;
            ReconcileOwnedMissiles(aircraft, force: true);

            if (previous != null && HasTrackableOwnedMissile())
                MfdLayoutController.EnsureLayoutForMissileFeed();
        }

        private static void TryUnbindAircraft()
        {
            if (_subscribedAircraft == null)
                return;

            _subscribedAircraft.onRegisterMissile -= OnRegisterMissile;
            _subscribedAircraft.onDeregisterMissile -= OnDeregisterMissile;
            _subscribedAircraft = null;
        }

        private static void OnRegisterMissile(Missile missile)
        {
            if (missile == null)
                return;

            if (!OwnedActive.Contains(missile))
                OwnedActive.Add(missile);

            _nextReconcileBackoff = 2f;
            MissileCameraSalvoTracker.OnRegister(missile);

            if (MissileCameraHost.IsSessionActive)
                MfdLayoutController.EnsureLayoutForMissileFeed();
        }

        private static void OnDeregisterMissile(Missile missile)
        {
            if (missile == null)
                return;

            OwnedActive.Remove(missile);
            MissileCameraSalvoTracker.OnDeregister(missile);
            if (_followedMissile == missile)
                _followedMissile = null;

            if (_overlayActive && !HasTrackableOwnedMissile() && !_postLossSequenceActive)
                BeginPostLossSequence();
        }

        private static bool IsDisplayPipelineActive() =>
            _overlayActive || MissileCameraFullscreenController.IsActive;

        private static RawImage? ResolveFeedImage() =>
            MissileCameraFullscreenController.IsActive
                ? MissileCameraFullscreenFeedHost.FeedImage
                : _feedImage;

        private static MissileCameraHudOverlay ResolveHudOverlay() =>
            MissileCameraFullscreenController.IsActive
                ? MissileCameraFullscreenFeedHost.Hud
                : HudOverlay;

        private static RectTransform? ResolveLayoutRoot() =>
            MissileCameraFullscreenController.IsActive
                ? MissileCameraFullscreenFeedHost.PanelRt ?? _layoutRoot
                : _layoutRoot;

        private static RectTransform? ResolveViewRt()
        {
            if (MissileCameraFullscreenController.IsActive)
                return MissileCameraFullscreenFeedHost.ViewRt ?? MissileCameraFullscreenFeedHost.PanelRt;

            return _layoutRoot != null
                ? MissileCameraFeedLayout.ResolveProjectionRect(_layoutRoot)
                : null;
        }
    }
}
