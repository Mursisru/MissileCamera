using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// Replaces vanilla weapons menu (top-right) with MissileCamera stub. TargetCam and center gauges stay vanilla.
    /// </summary>
    internal static class MfdLayoutController
    {
        private const string TacOverlayRootName = "MissileCamera.TacStub";

        private static TargetCam? _activeTargetCam;
        private static TargetScreenUI? _activeScreenUi;
        private static TacScreen? _activeTacScreen;
        private static GameObject? _tacOverlayRoot;
        private static Text? _stubLabel;
        private static bool _layoutActive;
        private static int _appliedConfigRevision = -1;
        private static float _lastNoOpLogTime = -999f;
        private static MissileCameraZone? _cachedOverlayZone;
        private static int _cachedOverlayRevision = -1;
        private static bool _cachedSuppressBottomDivider;
        private static bool _cachedShowPanelBorder;
        private static bool _cachedSuppressBottomBorder;
        private static float _cachedOverlayRotationZ;
        private static float _cachedStubContentRotationZ;
        private static float _cachedStubFontRef;
        private static Vector2 _cachedStubContentBand = Vector2.up;
        private static bool _cachedStubForcePortraitLayout;
        private static float _cachedHudLeftInsetExtra = 0.02f;
        private static MissileCameraTelemetryLayout _cachedTelemetryLayout = MissileCameraTelemetryLayout.BottomRow;
        private static float _lastDarkreachPortraitLogTime = -999f;
        private static bool _cleanupBusy;
        private static int _cleanupFrame = -1;
        private static int _layoutGeneration;

        internal static bool IsLayoutActive => _layoutActive;

        internal static float ActiveStubContentRotationZ => _cachedStubContentRotationZ;

        internal static float ActiveStubFontRef => _cachedStubFontRef;

        internal static Vector2 ActiveStubContentBand => _cachedStubContentBand;

        internal static float HudLeftInsetExtra => _cachedHudLeftInsetExtra;

        internal static bool ActiveStubForcePortraitLayout => _cachedStubForcePortraitLayout;

        internal static MissileCameraTelemetryLayout ActiveTelemetryLayout => _cachedTelemetryLayout;

        internal static TargetScreenUI? GetActiveScreenUi() => _activeScreenUi;

        /// <summary>Same drawable root as <see cref="ApplyScaledStubLayout"/> (feed + HUD must match).</summary>
        internal static RectTransform ResolveFeedLayoutRoot(RectTransform panelRt) =>
            ResolveStubLayoutRoot(panelRt, _cachedStubContentRotationZ, _cachedStubContentBand);

        internal static void OnSetupCamera(TargetScreenUI screenUi, TargetCam targetCam)
        {
            if (!MissileCameraHost.IsSessionActive)
                return;

            Bootstrap();

            if (_layoutActive && _activeTargetCam != null && _activeTargetCam != targetCam)
                HardResetForAircraftChange();

            _activeScreenUi = screenUi;
            _activeTargetCam = targetCam;
            if (!MissileCameraFeedController.HasTrackableOwnedMissile())
                return;

            TryApplyLayout(targetCam, screenUi);
            ScheduleRetryIfNeeded(targetCam);
        }

        internal static void OnTacCamToggle(TargetCam targetCam)
        {
            if (!MissileCameraHost.IsSessionActive)
                return;

            Bootstrap();
            if (!MissileCameraFeedController.HasTrackableOwnedMissile())
                return;

            TryApplyLayout(targetCam, _activeScreenUi);
            ScheduleRetryIfNeeded(targetCam);
        }

        internal static void OnTacScreenReady(TacScreen tacScreen, TargetCam targetCam)
        {
            if (!MissileCameraHost.IsSessionActive)
                return;

            Bootstrap();
            if (!MissileCameraFeedController.HasTrackableOwnedMissile())
                return;

            TargetScreenUI? screenUi = TargetCamAccess.GetTargetScreenUi(targetCam) ?? _activeScreenUi;
            TryApplyLayout(targetCam, screenUi);
            ScheduleRetryIfNeeded(targetCam);
        }

        internal static void OnTargetCamDisabled(TargetCam? targetCam)
        {
            if (targetCam != null && _activeTargetCam != null && _activeTargetCam != targetCam)
                return;

            // Tac cam toggles / FS yield disable TargetCam while missiles still fly — keep MC overlay.
            if (ShouldRetainLayoutForMissileFeed())
                return;

            SoftParkMissileFeed("target_cam_disabled");
        }

        internal static void OnTargetListCleared(TargetCam? targetCam)
        {
            if (targetCam != null && _activeTargetCam != null && _activeTargetCam != targetCam)
                return;

            if (ShouldRetainLayoutForMissileFeed())
                return;

            SoftParkMissileFeed("target_list_empty");
        }

        internal static void ReleaseLayoutIfNoMissileFeed()
        {
            if (!_layoutActive)
                return;

            if (ShouldRetainLayoutForMissileFeed())
                return;

            // Keep weapons hidden + stub; only soft-park feed. Next launch reuses without PrepareReplacement.
            SoftParkMissileFeed("missile_feed_ended");
        }

        /// <summary>Mid-sortie cfg off — restore vanilla weapons and drop tac stub.</summary>
        internal static void ReleaseFully(string reason)
        {
            if (!_layoutActive && _tacOverlayRoot == null)
                return;

            try { MfdLayoutRetryHost.Cancel(); }
            catch { /* ignore */ }

            if (_layoutActive)
                ClearLayout(reason);
            else
            {
                try { MissileCameraFeedController.NotifyOverlayGone(destroyHud: true); }
                catch { /* ignore */ }

                try { MfdWeaponsZoneAccess.Restore(); }
                catch { /* ignore */ }
            }

            try { DestroyTacOverlay(); }
            catch { /* ignore */ }
        }

        /// <summary>
        /// Drop live feed/HUD bind but keep tac stub + ApplyHidden strip for the sortie.
        /// Avoids Restore+discovery+ApplyHidden hitch on every subsequent launch.
        /// </summary>
        private static void SoftParkMissileFeed(string reason)
        {
            try { MfdLayoutRetryHost.Cancel(); }
            catch { /* ignore */ }

            try { MissileCameraFeedController.NotifyOverlayGone(destroyHud: false); }
            catch { /* ignore */ }

            if (_tacOverlayRoot != null)
            {
                try { _tacOverlayRoot.SetActive(false); }
                catch { /* ignore */ }
            }

            // Stay _layoutActive so EnsureLayoutForMissileFeed reactivates stub only.
            MissileCameraMissionLifecycleDiag.Info("SoftParkMissileFeed reason=" + reason);
        }

        internal static void ResetForMissionUnload() => HardResetForMissionUnload();

        internal static void HardResetForMissionUnload()
        {
            try { MfdLayoutRetryHost.Cancel(); }
            catch { /* ignore */ }

            _cleanupBusy = false;

            try { DestroyTacOverlay(); }
            catch { /* ignore */ }

            try { ClearLayout("mission_hard_reset"); }
            catch
            {
                try { MfdWeaponsZoneAccess.Restore(); }
                catch { /* ignore */ }
            }

            // Force dead session flags even if ClearLayout threw.
            _layoutActive = false;
            _activeTargetCam = null;
            _activeScreenUi = null;
            _activeTacScreen = null;
            _appliedConfigRevision = -1;
            _cachedOverlayZone = null;
            _cachedOverlayRevision = -1;
            _cleanupBusy = false;
            _layoutGeneration++;
        }

        /// <summary>Same-mission aircraft swap: restore hide + drop TacStub without killing Host session.</summary>
        internal static void HardResetForAircraftChange()
        {
            try { MfdLayoutRetryHost.Cancel(); }
            catch { /* ignore */ }

            _cleanupBusy = false;

            try { DestroyTacOverlay(); }
            catch { /* ignore */ }

            try { ClearLayout("aircraft_changed"); }
            catch
            {
                try { MfdWeaponsZoneAccess.Restore(); }
                catch { /* ignore */ }
            }

            _layoutActive = false;
            _activeTargetCam = null;
            _activeScreenUi = null;
            _activeTacScreen = null;
            _appliedConfigRevision = -1;
            _cachedOverlayZone = null;
            _cachedOverlayRevision = -1;
            _cleanupBusy = false;
            _layoutGeneration++;
            MfdLog.Info("layout hard reset reason=aircraft_changed");
        }

        internal static void EnsureLayoutForMissileFeed()
        {
            if (!MissileCameraHost.IsSessionActive)
            {
                MissileCameraMissionLifecycleDiag.WarnThrottled(
                    "ensure_gated",
                    "EnsureLayout gated off (session inactive)");
                return;
            }

            if (!MfdLayoutConfig.Enabled)
                return;

            if (_layoutActive)
            {
                if (_tacOverlayRoot == null)
                {
                    ClearLayout("overlay_destroyed");
                }
                else
                {
                    if (!_tacOverlayRoot.activeSelf)
                        _tacOverlayRoot.SetActive(true);

                    // Soft park kept feed/HUD refs — rewake without BindPanel/EnsureBuilt rebuild.
                    if (MissileCameraFeedController.TrySoftRewakeOverlay())
                        return;

                    // Re-bind only when soft refs were cleared or RawImage died.
                    Transform? panel = _tacOverlayRoot.transform.Find("MissileCameraPanel");
                    if (panel != null && panel.TryGetComponent(out RectTransform panelRt))
                        MissileCameraFeedController.NotifyOverlayReady(panelRt);
                    return;
                }
            }

            if (!GameManager.GetLocalAircraft(out Aircraft aircraft))
                return;

            TargetCam? targetCam = TargetCamAccess.GetTargetCam(aircraft);
            if (targetCam == null)
                return;

            TargetScreenUI? screenUi = TargetCamAccess.GetTargetScreenUi(targetCam);
            TryApplyLayout(targetCam, screenUi);
            ScheduleRetryIfNeeded(targetCam);
        }

        private static bool ShouldRetainLayoutForMissileFeed() =>
            MissileCameraFeedController.ShouldRetainLayoutForMissileFeed();

        internal static void TryApplyLayoutFromRetry(TargetCam targetCam, int generation)
        {
            if (generation != _layoutGeneration)
                return;

            if (_layoutActive)
            {
                MfdLayoutRetryHost.Cancel();
                return;
            }

            if (!MissileCameraFeedController.HasTrackableOwnedMissile())
                return;

            TargetScreenUI? screenUi = TargetCamAccess.GetTargetScreenUi(targetCam) ?? _activeScreenUi;
            TryApplyLayout(targetCam, screenUi);
            if (_layoutActive)
                MfdLayoutRetryHost.Cancel();
        }

        internal static void OnSetLandingCam(TargetCam targetCam)
        {
            if (_activeTargetCam != targetCam)
                return;

            if (ShouldRetainLayoutForMissileFeed())
                return;

            ClearLayout("landing_cam");
        }

        internal static void OnCancelTarget(TargetCam targetCam)
        {
            if (_activeTargetCam != targetCam && _activeTargetCam != null)
                return;

            if (ShouldRetainLayoutForMissileFeed())
                return;

            ClearLayout("cancel_target");
        }

        internal static void OnTargetCamDestroy(TargetCam targetCam)
        {
            if (_activeTargetCam != targetCam && _activeTargetCam != null)
                return;

            // TargetCam can recycle while missiles still need the feed — keep overlay if alive.
            if (ShouldRetainLayoutForMissileFeed() && _tacOverlayRoot != null)
            {
                _activeTargetCam = null;
                _activeScreenUi = null;
                return;
            }

            DestroyTacOverlay();
            ClearLayout("target_cam_destroy");
        }

        private static void Bootstrap()
        {
            MfdLayoutConfig.EnsureInitialized();
        }

        private static void TryApplyLayout(TargetCam targetCam, TargetScreenUI? screenUi)
        {
            if (!MissileCameraHost.IsSessionActive)
                return;

            if (!MissileCameraFeedController.HasTrackableOwnedMissile())
                return;

            if (!MfdLayoutConfig.Enabled)
            {
                if (_activeTargetCam == targetCam)
                    ClearLayout("disabled_or_landing");
                return;
            }

            if (TargetCamAccess.IsLandingMode(targetCam))
            {
                if (ShouldRetainLayoutForMissileFeed())
                    return;

                if (_activeTargetCam == targetCam)
                    ClearLayout("disabled_or_landing");
                return;
            }

            screenUi ??= TargetCamAccess.GetTargetScreenUi(targetCam) ?? _activeScreenUi;

            MfdLayoutConfig.Refresh();
            int revision = MfdLayoutConfig.Revision;

            if (_layoutActive
                && _activeTargetCam == targetCam
                && _activeScreenUi == screenUi
                && _activeTacScreen != null
                && _appliedConfigRevision == revision)
            {
                return;
            }

            string? jsonKey = MfdDisplayMode.GetAircraftJsonKey(targetCam);

            TacScreen? tacScreen = MfdDisplayMode.ResolveTacScreen(targetCam);
            if (tacScreen == null)
                return;

            if (TacScreenAccess.IsCricketAircraft(jsonKey))
            {
                TryApplyCricketEngineLayout(targetCam, screenUi, tacScreen, revision);
                ScheduleRetryIfNeeded(targetCam);
                return;
            }

            MfdLayoutProfile profile = MfdDisplayMode.Resolve(targetCam);
            if (profile == MfdLayoutProfile.Skip)
            {
                if (_layoutActive && _activeTargetCam == targetCam)
                    return;

                if (_activeTargetCam == targetCam)
                    ClearLayout("skip_profile");
                MfdLog.Info(
                    $"skip profile={profile} mode={MfdLayoutConfig.DisplayMode} jsonKey={MfdDisplayMode.GetAircraftJsonKey(targetCam)}");
                return;
            }

            // Single resolve pass — PrepareReplacement already TryResolveWeaponsPanel (no CanDiscover duplicate).
            WeaponsReplacementResult replacement = MfdWeaponsZoneAccess.PrepareReplacement(tacScreen, jsonKey);
            if (!replacement.Success)
            {
                LogNoOpThrottled("weapons panel not found тАФ no-op");
                return;
            }

            MissileCameraZone zone = replacement.Zone;
            Canvas overlayCanvas = replacement.OverlayCanvas!;
            bool stubCreated;
            try
            {
                MissileCameraMissionLifecycleDiag.Info("TryApplyLayout stage=hide_ok jsonKey=" + (jsonKey ?? "?"));
                stubCreated = EnsureTacOverlay(
                    overlayCanvas,
                    replacement.OverlayParent,
                    screenUi,
                    zone,
                    replacement.SuppressBottomDivider,
                    replacement.ShowPanelBorder,
                    replacement.SuppressBottomBorder,
                    replacement.OverlayRotationZ,
                    replacement.StubContentRotationZ,
                    replacement.StubFontRef,
                    replacement.StubContentBand,
                    replacement.StubForcePortraitLayout,
                    replacement.HudLeftInsetExtra,
                    replacement.TelemetryLayout);

                // Feed must be live — HUD chrome is optional (EnsureBuilt may soft-fail).
                if (!MissileCameraFeedController.IsOverlayActiveForDiag)
                {
                    MissileCameraMissionLifecycleDiag.Warn(
                        "TryApplyLayout stage=bind_missing — Restore weapons + destroy stub");
                    try { DestroyTacOverlay(); }
                    catch { /* ignore */ }
                    try { MfdWeaponsZoneAccess.Restore(); }
                    catch { /* ignore */ }
                    return;
                }
            }
            catch (System.Exception ex)
            {
                MissileCameraMissionLifecycleDiag.Warn(
                    "TryApplyLayout failed: " + ex.GetType().Name + ": " + ex.Message);
                try { DestroyTacOverlay(); }
                catch { /* ignore */ }
                try { MissileCameraFeedController.NotifyOverlayGone(); }
                catch { /* ignore */ }
                try { MfdWeaponsZoneAccess.Restore(); }
                catch { /* ignore */ }
                _layoutActive = false;
                return;
            }

            _activeTargetCam = targetCam;
            _activeScreenUi = screenUi;
            _activeTacScreen = tacScreen;
            _layoutActive = true;
            _appliedConfigRevision = revision;
            MfdLayoutRetryHost.Cancel();

            MfdLog.Info(
                $"weaponsтЖТmissilecam profile={profile} jsonKey={MfdDisplayMode.GetAircraftJsonKey(targetCam)} " +
                $"stub={stubCreated} zone={zone.MinX:F2}-{zone.MaxX:F2} y={zone.MinY:F2}-{zone.MaxY:F2}");
            MissileCameraMissionLifecycleDiag.Info(
                "TryApplyLayout stage=commit_ok stub=" + stubCreated
                + " zone=" + zone.MinX.ToString("F2") + "-" + zone.MaxX.ToString("F2"));
        }

        private static void TryApplyCricketEngineLayout(
            TargetCam targetCam,
            TargetScreenUI? screenUi,
            TacScreen tacScreen,
            int revision)
        {
            if (!MissileCameraFeedController.HasTrackableOwnedMissile())
                return;

            string? jsonKey = MfdDisplayMode.GetAircraftJsonKey(targetCam);

            if (!MfdWeaponsZoneAccess.CanDiscoverCricketEnginePanel(tacScreen, jsonKey))
            {
                MfdWeaponsZoneAccess.LogCricketDiscoveryFailure(tacScreen);
                LogNoOpThrottled("cricket engine MFD not found тАФ no-op");
                return;
            }

            WeaponsReplacementResult replacement = MfdWeaponsZoneAccess.PrepareCricketEngineReplacement(tacScreen, jsonKey);
            if (!replacement.Success)
            {
                LogNoOpThrottled("cricket engine MFD not found тАФ no-op");
                return;
            }

            MissileCameraZone zone = replacement.Zone;
            Canvas overlayCanvas = replacement.OverlayCanvas!;
            bool stubCreated;
            try
            {
                stubCreated = EnsureTacOverlay(
                    overlayCanvas,
                    replacement.OverlayParent,
                    screenUi,
                    zone,
                    replacement.SuppressBottomDivider,
                    replacement.ShowPanelBorder,
                    replacement.SuppressBottomBorder,
                    replacement.OverlayRotationZ,
                    replacement.StubContentRotationZ,
                    replacement.StubFontRef,
                    replacement.StubContentBand,
                    replacement.StubForcePortraitLayout,
                    replacement.HudLeftInsetExtra,
                    replacement.TelemetryLayout);

                if (!MissileCameraFeedController.IsOverlayActiveForDiag)
                {
                    MissileCameraMissionLifecycleDiag.Warn("Cricket apply bind_missing — Restore");
                    try { DestroyTacOverlay(); }
                    catch { /* ignore */ }
                    try { MfdWeaponsZoneAccess.Restore(); }
                    catch { /* ignore */ }
                    return;
                }
            }
            catch (System.Exception ex)
            {
                MissileCameraMissionLifecycleDiag.Warn(
                    "Cricket TryApply failed: " + ex.GetType().Name + ": " + ex.Message);
                try { DestroyTacOverlay(); }
                catch { /* ignore */ }
                try { MissileCameraFeedController.NotifyOverlayGone(); }
                catch { /* ignore */ }
                try { MfdWeaponsZoneAccess.Restore(); }
                catch { /* ignore */ }
                _layoutActive = false;
                return;
            }

            _activeTargetCam = targetCam;
            _activeScreenUi = screenUi;
            _activeTacScreen = tacScreen;
            _layoutActive = true;
            _appliedConfigRevision = revision;
            MfdLayoutRetryHost.Cancel();

            MfdLog.Info(
                $"cricketтЖТmissilecam jsonKey={jsonKey} stub={stubCreated} " +
                $"zone={zone.MinX:F2}-{zone.MaxX:F2} y={zone.MinY:F2}-{zone.MaxY:F2}");
        }

        private static bool EnsureTacOverlay(
            Canvas parentCanvas,
            RectTransform? overlayParent,
            TargetScreenUI? screenUi,
            MissileCameraZone zone,
            bool suppressBottomDivider,
            bool showPanelBorder,
            bool suppressBottomBorder,
            float overlayRotationZ = 0f,
            float stubContentRotationZ = 0f,
            float stubFontRef = 0f,
            Vector2 stubContentBand = default,
            bool stubForcePortraitLayout = false,
            float hudLeftInsetExtra = 0.02f,
            MissileCameraTelemetryLayout telemetryLayout = MissileCameraTelemetryLayout.BottomRow)
        {
            stubContentBand = stubContentBand == default ? Vector2.up : stubContentBand;
            Transform overlayHost = overlayParent != null ? overlayParent.transform : parentCanvas.transform;

            if (_tacOverlayRoot != null && _tacOverlayRoot.transform.parent == overlayHost)
            {
                if (!IsOverlayLayoutCurrent(
                        zone, suppressBottomDivider, showPanelBorder, suppressBottomBorder,
                        overlayRotationZ, stubContentRotationZ, stubFontRef, stubContentBand, stubForcePortraitLayout, hudLeftInsetExtra, telemetryLayout))
                {
                    UpdateTacOverlayLayout(
                        screenUi, zone, suppressBottomDivider, showPanelBorder, suppressBottomBorder,
                        overlayRotationZ, stubContentRotationZ, stubFontRef, stubContentBand, stubForcePortraitLayout, hudLeftInsetExtra, telemetryLayout);
                }

                if (!_tacOverlayRoot.activeSelf)
                    _tacOverlayRoot.SetActive(true);

                TryNotifyFeedOverlay();
                return true;
            }

            DestroyTacOverlay();

            _tacOverlayRoot = new GameObject(TacOverlayRootName, typeof(RectTransform));
            _tacOverlayRoot.transform.SetParent(overlayHost, false);
            _tacOverlayRoot.transform.SetAsLastSibling();

            RectTransform rootRt = _tacOverlayRoot.GetComponent<RectTransform>();
            Stretch(rootRt);
            ApplyOverlayRotation(rootRt, overlayRotationZ);

            if (MfdLayoutConfig.ShowDivider && !suppressBottomDivider)
            {
                CreateDivider(rootRt, screenUi, zone.MinY, zone.MinX, zone.MaxX);
            }

            var panelGo = new GameObject("MissileCameraPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(rootRt, false);
            RectTransform panelRt = panelGo.GetComponent<RectTransform>();
            ApplyZoneRect(panelRt, zone);

            Image panelImage = panelGo.GetComponent<Image>();
            UiImageHelper.ApplySolid(panelImage, TargetScreenUiStyle.GetStubPanelColor(screenUi));
            MissileCameraHudOverlay.ApplyPanelBackground(panelImage, screenUi);

            RectTransform contentRt = EnsureStubContentRoot(panelRt, stubContentRotationZ, stubContentBand);

            var titleGo = new GameObject("MissileCameraTitle", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(contentRt, false);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -4f);
            titleRt.sizeDelta = new Vector2(-8f, 20f);

            _stubLabel = titleGo.GetComponent<Text>();
            ApplyStubText(_stubLabel, screenUi, header: true);
            _stubLabel.alignment = TextAnchor.UpperCenter;
            _stubLabel.text = MfdLayoutConfig.StubLabel;

            var colorGo = new GameObject("MissileCameraColor", typeof(RectTransform), typeof(Text));
            colorGo.transform.SetParent(contentRt, false);
            RectTransform colorRt = colorGo.GetComponent<RectTransform>();
            colorRt.anchorMin = new Vector2(0.62f, 0.72f);
            colorRt.anchorMax = new Vector2(0.96f, 0.84f);
            colorRt.offsetMin = Vector2.zero;
            colorRt.offsetMax = Vector2.zero;

            Text colorLabel = colorGo.GetComponent<Text>();
            ApplyStubText(colorLabel, screenUi);
            colorLabel.alignment = TextAnchor.MiddleCenter;
            colorLabel.fontStyle = FontStyle.Bold;
            colorLabel.text = "COLOR";

            var feedGo = new GameObject("MissileCameraFeed", typeof(RectTransform), typeof(RawImage));
            feedGo.transform.SetParent(contentRt, false);
            feedGo.transform.SetAsFirstSibling();
            RectTransform feedRt = feedGo.GetComponent<RectTransform>();
            RawImage feedImage = feedGo.GetComponent<RawImage>();
            feedImage.raycastTarget = false;
            feedImage.enabled = false;

            var telemetryGo = new GameObject("MissileTelemetry", typeof(RectTransform), typeof(Text));
            telemetryGo.transform.SetParent(contentRt, false);
            RectTransform telemetryRt = telemetryGo.GetComponent<RectTransform>();
            telemetryRt.anchorMin = new Vector2(0f, 0f);
            telemetryRt.anchorMax = new Vector2(1f, 0f);
            telemetryRt.pivot = new Vector2(0.5f, 0f);
            telemetryRt.anchoredPosition = new Vector2(0f, 6f);
            telemetryRt.sizeDelta = new Vector2(-8f, 18f);

            Text telemetry = telemetryGo.GetComponent<Text>();
            ApplyStubText(telemetry, screenUi);
            telemetry.alignment = TextAnchor.LowerCenter;
            telemetry.text = MissileCameraTelemetry.FormatIdleTelemetryLine();

            ApplyScaledStubLayout(panelRt, screenUi, forceCanvasUpdate: true, stubContentRotationZ, stubFontRef, stubContentBand, stubForcePortraitLayout);

            if (MfdLayoutConfig.ShowDivider && showPanelBorder)
                EnsurePanelBorder(rootRt, screenUi, zone, suppressBottomBorder);

            CacheOverlayLayout(
                zone, suppressBottomDivider, showPanelBorder, suppressBottomBorder,
                overlayRotationZ, stubContentRotationZ, stubFontRef, stubContentBand, stubForcePortraitLayout, hudLeftInsetExtra, telemetryLayout);
            TryNotifyFeedOverlay();
            return true;
        }

        private static void TryNotifyFeedOverlay()
        {
            if (_tacOverlayRoot == null)
                return;

            Transform? panel = _tacOverlayRoot.transform.Find("MissileCameraPanel");
            if (panel != null && panel.TryGetComponent(out RectTransform panelRt))
                MissileCameraFeedController.NotifyOverlayReady(panelRt);
        }

        private const string StubContentName = "MissileCameraContent";

        private static RectTransform EnsureStubContentRoot(
            RectTransform panelRt,
            float contentRotationZ,
            Vector2 contentBand)
        {
            Transform? existing = panelRt.Find(StubContentName);
            if (existing != null && existing.TryGetComponent(out RectTransform existingRt))
            {
                ApplyStubContentBand(existingRt, contentBand);
                ApplyStubContentRotation(existingRt, contentRotationZ);
                return existingRt;
            }

            var contentGo = new GameObject(StubContentName, typeof(RectTransform));
            contentGo.transform.SetParent(panelRt, false);
            RectTransform contentRt = contentGo.GetComponent<RectTransform>();
            ApplyStubContentBand(contentRt, contentBand);
            contentRt.pivot = new Vector2(0.5f, 0.5f);
            contentRt.anchoredPosition = Vector2.zero;
            ApplyStubContentRotation(contentRt, contentRotationZ);
            return contentRt;
        }

        private static void ApplyStubContentBand(RectTransform contentRt, Vector2 contentBand)
        {
            contentRt.anchorMin = new Vector2(0f, contentBand.x);
            contentRt.anchorMax = new Vector2(1f, contentBand.y);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;
        }

        private static void ApplyStubContentRotation(RectTransform contentRt, float contentRotationZ)
        {
            // Important: rotating the whole container changes effective pivot and shifts rects
            // on portrait MFD. Vanilla rotates the Text transforms themselves, not a shared root.
            contentRt.localEulerAngles = Vector3.zero;
        }

        private static RectTransform ResolveStubLayoutRoot(
            RectTransform panelRt,
            float contentRotationZ,
            Vector2 contentBand)
        {
            if (Mathf.Abs(contentRotationZ) < 0.5f)
                return panelRt;

            return EnsureStubContentRoot(panelRt, contentRotationZ, contentBand);
        }

        private static void ApplyLandscapeStubAnchors(RectTransform panelRt)
        {
            Transform? title = panelRt.Find("MissileCameraTitle");
            if (title != null && title.TryGetComponent(out RectTransform titleRt))
            {
                titleRt.localEulerAngles = Vector3.zero;
                titleRt.anchorMin = new Vector2(0f, 1f);
                titleRt.anchorMax = new Vector2(1f, 1f);
                titleRt.pivot = new Vector2(0.5f, 1f);
                if (title.TryGetComponent(out Text titleText))
                    titleText.alignment = TextAnchor.UpperCenter;
            }

            Transform? color = panelRt.Find("MissileCameraColor");
            if (color != null && color.TryGetComponent(out RectTransform colorRt))
            {
                colorRt.localEulerAngles = Vector3.zero;
                if (color.TryGetComponent(out Text colorText))
                    colorText.alignment = TextAnchor.MiddleCenter;
            }

            Transform? telemetry = panelRt.Find("MissileTelemetry");
            if (telemetry != null && telemetry.TryGetComponent(out RectTransform telemetryRt))
            {
                telemetryRt.localEulerAngles = Vector3.zero;
                telemetryRt.anchorMin = new Vector2(0f, 0f);
                telemetryRt.anchorMax = new Vector2(1f, 0f);
                telemetryRt.pivot = new Vector2(0.5f, 0f);
                if (telemetry.TryGetComponent(out Text telemetryText))
                    telemetryText.alignment = TextAnchor.LowerCenter;
            }
        }

        private static void ApplyDarkreachPortraitAnchors(RectTransform panelRt)
        {
            ApplyDarkreachPortraitSlot(panelRt, "MissileCameraTitle", 0.17f);
            ApplyDarkreachPortraitSlot(panelRt, "MissileTelemetry", 0.83f);
        }

        private static void ApplyDarkreachPortraitSlot(RectTransform panelRt, string childName, float xCenter)
        {
            Transform? child = panelRt.Find(childName);
            if (child == null || !child.TryGetComponent(out RectTransform childRt))
                return;

            childRt.localEulerAngles = Vector3.zero;
            childRt.anchorMin = new Vector2(xCenter, 0.5f);
            childRt.anchorMax = new Vector2(xCenter, 0.5f);
            childRt.pivot = new Vector2(0.5f, 0.5f);
            childRt.anchoredPosition = Vector2.zero;
            if (child.TryGetComponent(out Text childText))
                childText.alignment = TextAnchor.MiddleCenter;
        }

        private static bool ShouldUsePortraitStubLayout(
            RectTransform layoutRt,
            float stubContentRotationZ,
            bool stubForcePortraitLayout)
        {
            float delta = Mathf.Abs(Mathf.DeltaAngle(0f, stubContentRotationZ));
            bool perpendicularRot = Mathf.Abs(delta - 90f) < 5f;
            if (!perpendicularRot)
                return false;

            if (stubForcePortraitLayout)
                return true;

            float w = Mathf.Max(layoutRt.rect.width, 1f);
            float h = Mathf.Max(layoutRt.rect.height, 1f);
            return h >= w * 1.3f;
        }

        private static void ApplyOverlayRotation(RectTransform rootRt, float overlayRotationZ)
        {
            if (Mathf.Abs(overlayRotationZ) < 0.5f)
                return;

            rootRt.localEulerAngles = new Vector3(0f, 0f, overlayRotationZ);
        }

        private static void ApplyScaledStubLayout(
            RectTransform panelRt,
            TargetScreenUI? screenUi,
            bool forceCanvasUpdate,
            float stubContentRotationZ = 0f,
            float stubFontRef = 0f,
            Vector2 stubContentBand = default,
            bool stubForcePortraitLayout = false)
        {
            stubContentBand = stubContentBand == default ? Vector2.up : stubContentBand;
            if (forceCanvasUpdate)
                Canvas.ForceUpdateCanvases();

            RectTransform layoutRt = ResolveStubLayoutRoot(panelRt, stubContentRotationZ, stubContentBand);
            bool usePortraitLayout = ShouldUsePortraitStubLayout(layoutRt, stubContentRotationZ, stubForcePortraitLayout);
            if (usePortraitLayout)
                ApplyDarkreachPortraitAnchors(layoutRt);
            else
                ApplyLandscapeStubAnchors(layoutRt);

            float layoutW = Mathf.Max(layoutRt.rect.width, 1f);
            float layoutH = Mathf.Max(layoutRt.rect.height, 1f);
            float w = layoutW;
            float h = layoutH;
            float delta = Mathf.Abs(Mathf.DeltaAngle(0f, stubContentRotationZ)); // 0..180
            bool perpendicularRot = Mathf.Abs(delta - 90f) < 5f;
            if (perpendicularRot && !usePortraitLayout)
                (w, h) = (h, w);

            float fontRef = stubFontRef > 1f ? stubFontRef : h;
            float runLength = 0f;
            float slotThickness = 0f;
            if (usePortraitLayout)
            {
                runLength = TargetScreenUiStyle.Snap(Mathf.Clamp(layoutH * 0.72f, layoutW * 1.0f, layoutH * 0.88f));
                slotThickness = TargetScreenUiStyle.Snap(Mathf.Clamp(layoutW * 0.24f, 14f, 42f));
            }

            Transform? title = layoutRt.Find("MissileCameraTitle");
            if (title != null && title.TryGetComponent(out Text titleText))
            {
                TargetScreenUiStyle.ApplyScaledStubText(titleText, screenUi, fontRef, fontRef, StubTextRole.Header);
                titleText.text = MfdLayoutConfig.StubLabel;
                if (title.TryGetComponent(out RectTransform titleRt))
                {
                    if (usePortraitLayout)
                        titleRt.sizeDelta = new Vector2(runLength, slotThickness);
                    else
                    {
                        float rowH = TargetScreenUiStyle.ScaledRowHeight(w, 0.12f, 12f, 28f);
                        float pad = TargetScreenUiStyle.Snap(Mathf.Clamp(Mathf.Min(w, h) * 0.04f, 4f, 10f));
                        titleRt.sizeDelta = new Vector2(-pad * 2f, rowH);
                        titleRt.anchoredPosition = new Vector2(0f, -TargetScreenUiStyle.Snap(Mathf.Clamp(h * 0.02f, 2f, 6f)));
                    }
                }
            }

            Transform? color = layoutRt.Find("MissileCameraColor");
            if (color != null && color.TryGetComponent(out Text colorText))
            {
                TargetScreenUiStyle.ApplyScaledStubText(colorText, screenUi, fontRef, fontRef, StubTextRole.Body);
                colorText.fontStyle = FontStyle.Bold;
                colorText.text = "COLOR";
            }

            Transform? telemetry = layoutRt.Find("MissileTelemetry");
            if (telemetry != null && telemetry.TryGetComponent(out Text telemetryText))
            {
                TargetScreenUiStyle.ApplyScaledStubText(telemetryText, screenUi, fontRef, fontRef, StubTextRole.Telemetry);
                if (telemetry.TryGetComponent(out RectTransform telemetryRt))
                {
                    if (usePortraitLayout)
                        telemetryRt.sizeDelta = new Vector2(runLength, slotThickness);
                    else
                    {
                        float rowH = TargetScreenUiStyle.ScaledRowHeight(w, 0.10f, 10f, 22f);
                        float pad = TargetScreenUiStyle.Snap(Mathf.Clamp(Mathf.Min(w, h) * 0.04f, 4f, 10f));
                        telemetryRt.sizeDelta = new Vector2(-pad * 2f, rowH);
                        telemetryRt.anchoredPosition = new Vector2(0f, TargetScreenUiStyle.Snap(Mathf.Clamp(h * 0.02f, 2f, 6f)));
                    }
                }
            }

            // Rotate each Text transform, mirroring vanilla behavior.
            if (Mathf.Abs(stubContentRotationZ) > 0.5f)
            {
                foreach (string childName in new[] { "MissileCameraTitle", "MissileCameraColor", "MissileTelemetry" })
                {
                    Transform? child = layoutRt.Find(childName);
                    if (child == null || !child.TryGetComponent(out RectTransform childRt))
                        continue;

                    Vector3 euler = childRt.localEulerAngles;
                    childRt.localEulerAngles = new Vector3(euler.x, euler.y, stubContentRotationZ);
                }
            }

            if (usePortraitLayout && Time.unscaledTime - _lastDarkreachPortraitLogTime >= 2f)
            {
                _lastDarkreachPortraitLogTime = Time.unscaledTime;
                MfdLog.Info(
                    $"portrait stub columns w={layoutW:F0} h={layoutH:F0} run={runLength:F0} thick={slotThickness:F0} " +
                    $"rot={stubContentRotationZ:F0} force={stubForcePortraitLayout} band={stubContentBand.x:F2}-{stubContentBand.y:F2}");
            }

            MissileCameraFeedLayout.Apply(layoutRt, usePortraitLayout, stubContentRotationZ);
        }

        private static void ApplyStubText(Text target, TargetScreenUI? screenUi, bool header = false)
        {
            TargetScreenUiStyle.ApplyLabel(target, screenUi, header);
            target.color = TargetScreenUiStyle.GetStubLabelColor(screenUi);
            target.horizontalOverflow = HorizontalWrapMode.Overflow;
            target.verticalOverflow = VerticalWrapMode.Overflow;
            target.raycastTarget = false;
        }

        private static void UpdateTacOverlayLayout(
            TargetScreenUI? screenUi,
            MissileCameraZone zone,
            bool suppressBottomDivider,
            bool showPanelBorder,
            bool suppressBottomBorder,
            float overlayRotationZ = 0f,
            float stubContentRotationZ = 0f,
            float stubFontRef = 0f,
            Vector2 stubContentBand = default,
            bool stubForcePortraitLayout = false,
            float hudLeftInsetExtra = 0.02f,
            MissileCameraTelemetryLayout telemetryLayout = MissileCameraTelemetryLayout.BottomRow)
        {
            stubContentBand = stubContentBand == default ? Vector2.up : stubContentBand;
            if (_tacOverlayRoot == null)
                return;

            CacheOverlayLayout(
                zone, suppressBottomDivider, showPanelBorder, suppressBottomBorder,
                overlayRotationZ, stubContentRotationZ, stubFontRef, stubContentBand, stubForcePortraitLayout, hudLeftInsetExtra, telemetryLayout);

            if (_tacOverlayRoot.TryGetComponent(out RectTransform rootRt))
                ApplyOverlayRotation(rootRt, overlayRotationZ);

            Transform? divider = _tacOverlayRoot.transform.Find("WeaponsBottomDivider");
            if (suppressBottomDivider || !MfdLayoutConfig.ShowDivider)
            {
                if (divider != null)
                    divider.gameObject.SetActive(false);
            }
            else if (divider != null)
            {
                divider.gameObject.SetActive(true);
                if (divider.TryGetComponent(out RectTransform dividerRt))
                    ApplyDividerRect(dividerRt, zone.MinY, zone.MinX, zone.MaxX);
                if (divider.TryGetComponent(out Image dividerImage))
                    UiImageHelper.ApplySolid(dividerImage, TargetScreenUiStyle.GetDividerColor(screenUi));
            }
            else if (MfdLayoutConfig.ShowDivider)
            {
                CreateDivider(_tacOverlayRoot.GetComponent<RectTransform>(), screenUi, zone.MinY, zone.MinX, zone.MaxX);
            }

            Transform? panel = _tacOverlayRoot.transform.Find("MissileCameraPanel");
            if (panel != null)
            {
                RectTransform panelRt = panel.GetComponent<RectTransform>();
                ApplyZoneRect(panelRt, zone);
                if (panel.TryGetComponent(out Image panelImage))
                    UiImageHelper.ApplySolid(panelImage, TargetScreenUiStyle.GetStubPanelColor(screenUi));
                ApplyScaledStubLayout(
                    panelRt, screenUi, forceCanvasUpdate: false, stubContentRotationZ, stubFontRef, stubContentBand, stubForcePortraitLayout);
                TryNotifyFeedOverlay();
            }

            if (MfdLayoutConfig.ShowDivider && showPanelBorder)
                EnsurePanelBorder(_tacOverlayRoot.GetComponent<RectTransform>(), screenUi, zone, suppressBottomBorder);
            else
                HidePanelBorder(_tacOverlayRoot);
        }

        private static bool IsOverlayLayoutCurrent(
            MissileCameraZone zone,
            bool suppressBottomDivider,
            bool showPanelBorder,
            bool suppressBottomBorder,
            float overlayRotationZ = 0f,
            float stubContentRotationZ = 0f,
            float stubFontRef = 0f,
            Vector2 stubContentBand = default,
            bool stubForcePortraitLayout = false,
            float hudLeftInsetExtra = 0.02f,
            MissileCameraTelemetryLayout telemetryLayout = MissileCameraTelemetryLayout.BottomRow)
        {
            stubContentBand = stubContentBand == default ? Vector2.up : stubContentBand;
            if (_cachedOverlayZone == null || _cachedOverlayRevision != MfdLayoutConfig.Revision)
                return false;

            MissileCameraZone cached = _cachedOverlayZone.Value;
            return Mathf.Approximately(cached.MinX, zone.MinX)
                && Mathf.Approximately(cached.MaxX, zone.MaxX)
                && Mathf.Approximately(cached.MinY, zone.MinY)
                && Mathf.Approximately(cached.MaxY, zone.MaxY)
                && cached.OffsetMin == zone.OffsetMin
                && cached.OffsetMax == zone.OffsetMax
                && _cachedSuppressBottomDivider == suppressBottomDivider
                && _cachedShowPanelBorder == showPanelBorder
                && _cachedSuppressBottomBorder == suppressBottomBorder
                && Mathf.Approximately(_cachedOverlayRotationZ, overlayRotationZ)
                && Mathf.Approximately(_cachedStubContentRotationZ, stubContentRotationZ)
                && Mathf.Approximately(_cachedStubFontRef, stubFontRef)
                && Mathf.Approximately(_cachedStubContentBand.x, stubContentBand.x)
                && Mathf.Approximately(_cachedStubContentBand.y, stubContentBand.y)
                && _cachedStubForcePortraitLayout == stubForcePortraitLayout
                && Mathf.Approximately(_cachedHudLeftInsetExtra, hudLeftInsetExtra)
                && _cachedTelemetryLayout == telemetryLayout;
        }

        private static void CacheOverlayLayout(
            MissileCameraZone zone,
            bool suppressBottomDivider,
            bool showPanelBorder,
            bool suppressBottomBorder,
            float overlayRotationZ = 0f,
            float stubContentRotationZ = 0f,
            float stubFontRef = 0f,
            Vector2 stubContentBand = default,
            bool stubForcePortraitLayout = false,
            float hudLeftInsetExtra = 0.02f,
            MissileCameraTelemetryLayout telemetryLayout = MissileCameraTelemetryLayout.BottomRow)
        {
            stubContentBand = stubContentBand == default ? Vector2.up : stubContentBand;
            _cachedOverlayZone = zone;
            _cachedOverlayRevision = MfdLayoutConfig.Revision;
            _cachedSuppressBottomDivider = suppressBottomDivider;
            _cachedShowPanelBorder = showPanelBorder;
            _cachedSuppressBottomBorder = suppressBottomBorder;
            _cachedOverlayRotationZ = overlayRotationZ;
            _cachedStubContentRotationZ = stubContentRotationZ;
            _cachedStubFontRef = stubFontRef;
            _cachedStubContentBand = stubContentBand;
            _cachedStubForcePortraitLayout = stubForcePortraitLayout;
            _cachedHudLeftInsetExtra = hudLeftInsetExtra;
            _cachedTelemetryLayout = telemetryLayout;
        }

        private static void ScheduleRetryIfNeeded(TargetCam targetCam)
        {
            if (_layoutActive || !MfdLayoutConfig.Enabled || TargetCamAccess.IsLandingMode(targetCam))
                return;

            if (!MissileCameraFeedController.HasTrackableOwnedMissile())
                return;

            MfdLayoutRetryHost.Schedule(targetCam, ++_layoutGeneration);
        }

        private static void ApplyZoneRect(RectTransform panelRt, MissileCameraZone zone)
        {
            panelRt.anchorMin = new Vector2(zone.MinX, zone.MinY);
            panelRt.anchorMax = new Vector2(zone.MaxX, zone.MaxY);
            panelRt.offsetMin = zone.OffsetMin;
            panelRt.offsetMax = zone.OffsetMax;
        }

        private static void CreateDivider(RectTransform parent, TargetScreenUI? screenUi, float y, float xMin, float xMax)
        {
            var go = new GameObject("WeaponsBottomDivider", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            ApplyDividerRect(rt, y, xMin, xMax);
            UiImageHelper.ApplySolid(go.GetComponent<Image>(), TargetScreenUiStyle.GetDividerColor(screenUi));
        }

        private static void ApplyDividerRect(RectTransform rt, float y, float xMin, float xMax)
        {
            rt.anchorMin = new Vector2(xMin, y);
            rt.anchorMax = new Vector2(xMax, y);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(0f, 2f);
        }

        private const float PanelBorderThickness = 3f;

        private static void EnsurePanelBorder(
            RectTransform overlayRoot,
            TargetScreenUI? screenUi,
            MissileCameraZone zone,
            bool suppressBottomBorder)
        {
            Transform? borderRoot = overlayRoot.Find("WeaponsPanelBorder");
            if (borderRoot == null)
            {
                var go = new GameObject("WeaponsPanelBorder", typeof(RectTransform));
                go.transform.SetParent(overlayRoot, false);
                Stretch(go.GetComponent<RectTransform>());
                CreateBorderEdge(go.transform, "BorderTop");
                CreateBorderEdge(go.transform, "BorderBottom");
                CreateBorderEdge(go.transform, "BorderLeft");
                CreateBorderEdge(go.transform, "BorderRight");
                borderRoot = go.transform;
            }

            borderRoot.gameObject.SetActive(true);
            borderRoot.SetAsLastSibling();
            Color borderColor = TargetScreenUiStyle.GetPanelBorderColor(screenUi);
            ApplyPanelBorderRects(borderRoot, zone, borderColor, suppressBottomBorder);
        }

        private static void HidePanelBorder(GameObject overlayRoot)
        {
            Transform? borderRoot = overlayRoot.transform.Find("WeaponsPanelBorder");
            if (borderRoot != null)
                borderRoot.gameObject.SetActive(false);
        }

        private static void CreateBorderEdge(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.transform.SetAsLastSibling();
        }

        private static void ApplyPanelBorderRects(
            Transform borderRoot,
            MissileCameraZone zone,
            Color color,
            bool suppressBottomBorder)
        {
            ApplyBorderEdge(borderRoot.Find("BorderTop"), zone, PanelBorderEdge.Top, color);
            Transform? bottom = borderRoot.Find("BorderBottom");
            if (bottom != null)
                bottom.gameObject.SetActive(!suppressBottomBorder);
            if (!suppressBottomBorder)
                ApplyBorderEdge(bottom, zone, PanelBorderEdge.Bottom, color);
            ApplyBorderEdge(borderRoot.Find("BorderLeft"), zone, PanelBorderEdge.Left, color);
            ApplyBorderEdge(borderRoot.Find("BorderRight"), zone, PanelBorderEdge.Right, color);
        }

        private static void ApplyBorderEdge(
            Transform? edge,
            MissileCameraZone zone,
            PanelBorderEdge side,
            Color color)
        {
            if (edge == null || !edge.TryGetComponent(out RectTransform rt))
                return;

            float t = PanelBorderThickness;
            switch (side)
            {
                case PanelBorderEdge.Top:
                    rt.anchorMin = new Vector2(zone.MinX, zone.MaxY);
                    rt.anchorMax = new Vector2(zone.MaxX, zone.MaxY);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.sizeDelta = new Vector2(0f, t);
                    break;
                case PanelBorderEdge.Bottom:
                    rt.anchorMin = new Vector2(zone.MinX, zone.MinY);
                    rt.anchorMax = new Vector2(zone.MaxX, zone.MinY);
                    rt.pivot = new Vector2(0.5f, 0f);
                    rt.sizeDelta = new Vector2(0f, t);
                    break;
                case PanelBorderEdge.Left:
                    rt.anchorMin = new Vector2(zone.MinX, zone.MinY);
                    rt.anchorMax = new Vector2(zone.MinX, zone.MaxY);
                    rt.pivot = new Vector2(0f, 0.5f);
                    rt.sizeDelta = new Vector2(t, 0f);
                    break;
                default:
                    rt.anchorMin = new Vector2(zone.MaxX, zone.MinY);
                    rt.anchorMax = new Vector2(zone.MaxX, zone.MaxY);
                    rt.pivot = new Vector2(1f, 0.5f);
                    rt.sizeDelta = new Vector2(t, 0f);
                    break;
            }

            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            if (edge.TryGetComponent(out Image image))
                UiImageHelper.ApplySolid(image, color);
        }

        private enum PanelBorderEdge
        {
            Top,
            Bottom,
            Left,
            Right
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void LogNoOpThrottled(string message)
        {
            float now = Time.unscaledTime;
            if (now - _lastNoOpLogTime < 5f)
                return;

            _lastNoOpLogTime = now;
            MfdLog.Info(message);
        }

        private static void ClearLayout(string reason)
        {
            // Hard reset must never no-op Restore because same-frame cleanup lock was set.
            if (string.Equals(reason, "mission_hard_reset", System.StringComparison.Ordinal))
                _cleanupBusy = false;

            if (!TryBeginCleanup())
            {
                // Same-frame cleanup lock: still drop sticky flags so next sortie can EnsureLayout.
                try { MfdWeaponsZoneAccess.Restore(); }
                catch { /* ignore */ }

                try { MissileCameraFeedController.NotifyOverlayGone(destroyHud: true); }
                catch { /* ignore */ }

                _layoutActive = false;
                MissileCameraMissionLifecycleDiag.Warn(
                    "ClearLayout busy-skip reason=" + reason + " forced layout/overlay down");
                return;
            }

            try
            {
                bool wasActive = _layoutActive;
                MfdLayoutRetryHost.Cancel();
                // Full wipe — not SoftPark. SoftPark keeps feed/Rig; ClearLayout restores weapons.
                MissileCameraFeedController.NotifyOverlayGone(destroyHud: true);
                MfdWeaponsZoneAccess.Restore();

                if (_tacOverlayRoot != null)
                    _tacOverlayRoot.SetActive(false);

                _layoutActive = false;
                _activeTargetCam = null;
                _activeScreenUi = null;
                _activeTacScreen = null;
                _appliedConfigRevision = -1;
                _cachedOverlayZone = null;
                _cachedOverlayRevision = -1;
                _cachedSuppressBottomBorder = false;
                _cachedOverlayRotationZ = 0f;
                _cachedStubContentRotationZ = 0f;
                _cachedStubFontRef = 0f;
                _cachedStubContentBand = Vector2.up;
                _cachedHudLeftInsetExtra = 0.02f;
                _cachedTelemetryLayout = MissileCameraTelemetryLayout.BottomRow;
                _layoutGeneration++;

                if (wasActive)
                {
                    MfdLog.Info("layout cleared reason=" + reason);
                    MissileCameraMissionLifecycleDiag.Info("ClearLayout ok reason=" + reason);
                }
            }
            finally
            {
                EndCleanup();
            }
        }

        private static void DestroyTacOverlay()
        {
            GameObject? go = null;
            try
            {
                if (_tacOverlayRoot != null)
                    go = _tacOverlayRoot.gameObject;
            }
            catch
            {
                // destroyed RectTransform
            }

            // Drop refs first so a throw in NotifyOverlayGone cannot leave sticky overlay pointers.
            _tacOverlayRoot = null;
            _stubLabel = null;

            try { MissileCameraFeedController.NotifyOverlayGone(destroyHud: true); }
            catch { /* ignore */ }

            if (go != null)
            {
                try { Object.Destroy(go); }
                catch { /* ignore */ }
            }
        }

        private static bool TryBeginCleanup()
        {
            int frame = Time.frameCount;
            if (_cleanupBusy && _cleanupFrame == frame)
                return false;

            _cleanupBusy = true;
            _cleanupFrame = frame;
            return true;
        }

        private static void EndCleanup()
        {
            _cleanupBusy = false;
        }
    }
}
