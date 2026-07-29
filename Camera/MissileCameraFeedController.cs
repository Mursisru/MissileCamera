using System.Collections.Generic;
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
            NotifyOverlayGone();
            TryUnbindAircraft();
            OwnedActive.Clear();
            MissileCameraSalvoTracker.Reset();
            MissileCameraInfraredEffect.Shutdown();
            MissileCameraLossInterference.Shutdown();
            MissileCameraPostFxStack.Release();
            MissileCameraAircraftCamController.Shutdown();
            MissileCameraCockpitPipController.Shutdown();
            MissileCameraFullscreenController.ExitIfActive();
            _postLossSequenceActive = false;
            _manualFollowActive = false;
            _zoomOffset = 0f;
            _fullscreenMagnification = 1f;
            MissileCameraVisionModeController.Reset();
            _rig?.Destroy();
            _rig = null;
        }

        internal static void SelectNextMissile()
        {
            if (!_overlayActive || !HasTrackableOwnedMissile())
                return;

            Missile? current = _followedMissile ?? PickLatestMissile();
            Missile? next = MissileCameraFeedSelection.CycleCurrent(OwnedActive, current, 1);
            ApplyManualSelection(next);
            NotifyCameraSwitchInterference(current, next);
        }

        internal static void SelectPreviousMissile()
        {
            if (!_overlayActive || !HasTrackableOwnedMissile())
                return;

            Missile? current = _followedMissile ?? PickLatestMissile();
            Missile? previous = MissileCameraFeedSelection.CycleCurrent(OwnedActive, current, -1);
            ApplyManualSelection(previous);
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
            HudOverlay.NotifyZoomChanged(_fullscreenMagnification);
        }

        internal static void ResetFullscreenMagnification()
        {
            if (!Mathf.Approximately(_fullscreenMagnification, 1f))
            {
                _fullscreenMagnification = 1f;
                if (_overlayActive && MissileCameraFullscreenController.IsActive)
                {
                    ApplyFullscreenOptics();
                    HudOverlay.NotifyZoomChanged(_fullscreenMagnification);
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
            RefreshConfigsIfDue();
            MissileCameraFullscreenController.TickYieldToVanillaUi();

            // Fullscreen/MFD NO SIGNAL burst (switch / destroy / exit-no-missile).
            MissileCameraLossInterference.Tick(_feedImage);
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

            if (HasTrackableOwnedMissile() && !_overlayActive)
                MfdLayoutController.EnsureLayoutForMissileFeed();

            if (!_overlayActive)
            {
                UseIdleDriverWait = !HasTrackableOwnedMissile();
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

            if (fullscreen && MissileCameraLossInterference.IsActive)
            {
                MissileCameraInfraredEffect.Clear(_feedImage, rig);
            }
            else if (fullscreen)
            {
                MissileCameraInfraredEffect.ApplyFullscreenVision(
                    _feedImage,
                    rig,
                    MissileCameraVisionModeController.Mode,
                    exposure);
            }
            else
            {
                MissileCameraInfraredEffect.Apply(_feedImage, rig, autoInfrared, exposure);
            }

            MissileCameraAircraftCamController.Tick();
            if (_panelRt != null && HudOverlay.Root != null)
                MissileCameraCockpitPipController.Tick(HudOverlay.Root, GetPanelMetrics(_panelRt));

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

        internal static RectTransform? TryGetPanelRt() => _panelRt;

        internal static Texture? TryGetFeedTexture() =>
            _rig != null && _rig.IsRootAlive ? _rig.Texture : _feedImage != null ? _feedImage.texture : null;

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
            if (_feedImage != null && tex != null)
            {
                _feedImage.texture = tex;
                _feedImage.enabled = true;
            }

            return tex;
        }

        /// <summary>One-shot FLIR fill for boot text capture (skips when boot not using overlay).</summary>
        internal static void RefreshFlirHudOnce()
        {
            if (_layoutRoot == null || _panelRt == null)
                return;

            Missile? missile = _followedMissile;
            if (missile == null || missile.disabled)
                missile = PickLatestMissile();

            SyncFeedLayout();
            MissileCameraHudSnapshot snapshot = ResolveHudSnapshot(missile);
            MissileCameraPanelMetrics panel = GetPanelMetrics(_panelRt);
            HudOverlay.ForceFlirUpdate(snapshot, panel);
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

            if (_rig != null)
            {
                // Force RT recreate at fullscreen / MFD resolution on next render.
                MissileCameraFeedConfig.ResolveActiveFeedSize(out int w, out int h);
                MfdLog.Info($"fullscreen layout feedRT={w}x{h}");
            }
        }

        internal static void NotifyOverlayReady(RectTransform panelRt)
        {
            _overlayActive = true;
            _loggedBind = false;
            BindPanel(panelRt);
        }

        internal static void NotifyOverlayGone()
        {
            _overlayActive = false;
            MissileCameraFullscreenController.ExitIfActive();
            CancelPostLossSequence();
            MissileCameraInfraredEffect.Clear(_feedImage, _rig);
            MissileCameraPostFxStack.Release();
            _feedImage = null;
            _telemetryText = null;
            _colorLabel = null;
            _layoutRoot = null;
            _panelRt = null;
            _cachedLayoutRoot = null;
            _cachedLayoutRotationZ = float.NaN;
            _cachedPanelW = -1f;
            _cachedPanelH = -1f;
            HudOverlay.Destroy();
            _manualFollowActive = false;
            _zoomOffset = 0f;
            DetachRig();
            MissileCameraTelemetry.ResetThrottle();
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

            if (_feedImage != null)
            {
                if (MissileCameraLossInterference.IsActive
                    && MissileCameraLossInterference.ActiveKind == MissileCameraLossInterference.BurstKind.ExitShutdown)
                {
                    // ExitShutdown uses dedicated fullscreen NO SIGNAL canvas; hide feed.
                    _feedImage.enabled = false;
                }
                else if (MissileCameraLossInterference.IsActive || _postLossSequenceActive)
                {
                    // Destroy/switch NO SIGNAL covers feed (MFD host or fullscreen overlay).
                }
                else
                {
                    if (_feedImage.texture != texture)
                        _feedImage.texture = texture;
                    bool show = texture != null;
                    if (_feedImage.enabled != show)
                        _feedImage.enabled = show;
                    // IR/vision already applied in Tick(); skip redundant Apply here on the hot path.
                }
            }

            if (missile == null || texture == null)
                MissileCameraInfraredEffect.Apply(_feedImage, _rig, infrared: false, exposure: 0f);

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

            if (_layoutRoot != null && _panelRt != null)
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

                    SyncFeedLayout();
                    RectTransform viewRt = MissileCameraFeedLayout.ResolveProjectionRect(_layoutRoot);

                    // Fullscreen FLIR still ticks every frame; snapshot strings throttle via ResolveHudSnapshot.
                    MissileCameraHudSnapshot snapshot = ResolveHudSnapshot(missile);
                    Camera? feedCamera = _rig?.FeedCamera;

                    MissileCameraPanelMetrics panel = GetPanelMetrics(_panelRt);
                    HudOverlay.Update(
                        snapshot,
                        _layoutRoot,
                        viewRt,
                        feedCamera,
                        panel,
                        _panelRt,
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
            // Stop pose overlay removed — CSM never touched (CAMERA_SAFETY.md).
            if (!_overlayActive)
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
            MissileCameraInfraredEffect.Clear(_feedImage, null);

            if (_feedImage != null)
            {
                _feedImage.enabled = true;
                _feedImage.color = Color.white;
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
            if (_feedImage != null)
            {
                _feedImage.texture = null;
                _feedImage.enabled = false;
            }

            // After impact NO SIGNAL: leave fullscreen (cockpit camera was never hijacked).
            MissileCameraFullscreenController.ExitIfActive();

            UpdateDisplay(null);
            TryReleaseLayout();
        }

        private static void UpdatePostLossHud()
        {
            MissileCameraTelemetry.Update(_telemetryText, null);

            if (_layoutRoot == null || _panelRt == null)
                return;

            SyncFeedLayout();
            RectTransform viewRt = MissileCameraFeedLayout.ResolveProjectionRect(_layoutRoot);
            MissileCameraPanelMetrics panel = GetPanelMetrics(_panelRt);
            HudOverlay.Update(
                MissileCameraHudSnapshot.Empty,
                _layoutRoot,
                viewRt,
                feedCamera: null,
                panel,
                _panelRt,
                updateCorners: true,
                updateDynamic: true);
        }

        private static void TryReleaseLayout()
        {
            if (!ShouldRetainLayoutForMissileFeed())
                MfdLayoutController.ReleaseLayoutIfNoMissileFeed();
        }

        /// <summary>Keep MFD overlay while missiles fly or post-loss NO SIGNAL/hold plays.</summary>
        internal static bool ShouldRetainLayoutForMissileFeed() =>
            HasTrackableOwnedMissile()
            || _postLossSequenceActive
            || MissileCameraLossInterference.IsActive
            || MissileCameraFullscreenController.IsActive
            || MissileCameraFullscreenController.IsDeferredExit;

        private static void DetachRig()
        {
            _followedMissile = null;
            if (_rig == null)
                return;

            _rig.SetPipelineDriven(false);
            _rig.Detach();
            if (!_rig.IsRootAlive)
                _rig = null;
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
                }

                return;
            }

            if (_subscribedAircraft == aircraft)
                return;

            TryUnbindAircraft();
            _subscribedAircraft = aircraft;
            _subscribedAircraft.onRegisterMissile += OnRegisterMissile;
            _subscribedAircraft.onDeregisterMissile += OnDeregisterMissile;
            _nextReconcileTimeUnscaled = 0f;
            ReconcileOwnedMissiles(aircraft, force: true);
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
    }
}
