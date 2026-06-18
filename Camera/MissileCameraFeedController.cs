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
        private static float _restoreAfterLossAtUnscaled = -1f;
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
        private const float HudSnapshotInterval = 1f / 15f;
        private const float CornerHudInterval = 1f / 15f;
        private const float ConfigRefreshInterval = 0.5f;

        internal static bool UseIdleDriverWait { get; private set; }

        internal static void Shutdown()
        {
            NotifyOverlayGone();
            TryUnbindAircraft();
            OwnedActive.Clear();
            MissileCameraSalvoTracker.Reset();
            _rig?.Destroy();
            _rig = null;
        }

        internal static void Tick()
        {
            RefreshConfigsIfDue();

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

            Missile? missile = PickLatestMissile();
            if (missile == null)
            {
                HandleMissileLost();
                return;
            }

            _restoreAfterLossAtUnscaled = -1f;
            if (_followedMissile != missile)
            {
                _followedMissile = missile;
                _nextHudSnapshotTime = 0f;
                _nextCornerHudTime = 0f;
            }

            MissileCameraRig rig = EnsureRig();
            rig.Attach(missile);
            rig.AdvanceRoll(Time.deltaTime);

            if (Time.unscaledTime >= _nextRenderTimeUnscaled)
            {
                float interval = 1f / Mathf.Max(MissileCameraFeedConfig.RenderFps, 1);
                _nextRenderTimeUnscaled = Time.unscaledTime + interval;
                rig.SyncPose();
                rig.RenderFrame();
            }

            UpdateDisplay(missile);
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
            if (_feedImage != null)
            {
                _feedImage.texture = texture;
                _feedImage.enabled = texture != null;
            }

            MissileCameraTelemetry.Update(_telemetryText, missile);

            if (_layoutRoot != null && _panelRt != null)
            {
                bool updateCorners = missile == null || Time.unscaledTime >= _nextCornerHudTime;
                bool updateDynamic = missile != null && Time.unscaledTime >= _nextHudVisualTime;
                if (missile == null || updateCorners || updateDynamic)
                {
                    if (missile != null)
                    {
                        if (updateCorners)
                            _nextCornerHudTime = Time.unscaledTime + CornerHudInterval;
                        if (updateDynamic)
                            _nextHudVisualTime = Time.unscaledTime + HudSnapshotInterval;
                    }

                    SyncFeedLayout();
                    RectTransform viewRt = MissileCameraFeedLayout.ResolveProjectionRect(_layoutRoot);

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
                _nextHudSnapshotTime = now + HudSnapshotInterval;
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
            float linger = MissileCameraFeedConfig.PostExplosionHoldSeconds;
            if (linger <= 0f || _followedMissile == null)
            {
                _restoreAfterLossAtUnscaled = -1f;
                DetachRig();
                UpdateDisplay(null);
                TryReleaseLayout();
                return;
            }

            if (_restoreAfterLossAtUnscaled < 0f)
                _restoreAfterLossAtUnscaled = Time.unscaledTime + linger;

            if (Time.unscaledTime >= _restoreAfterLossAtUnscaled)
            {
                _restoreAfterLossAtUnscaled = -1f;
                DetachRig();
                UpdateDisplay(null);
                TryReleaseLayout();
            }
        }

        private static void TryReleaseLayout()
        {
            if (!HasTrackableOwnedMissile())
                MfdLayoutController.ReleaseLayoutIfNoMissileFeed();
        }

        private static void DetachRig()
        {
            _followedMissile = null;
            if (_rig == null)
                return;

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

            TryReleaseLayout();
        }
    }
}
