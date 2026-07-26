using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// First-enter FS boot ~3.5s: tile puzzle → flicker/char cal → hex drum → value drum + diagnostics.
    /// </summary>
    internal static class MissileCameraBootSequence
    {
        // Original 2.4s timeline scaled by 3.5/2.4.
        private const float Phase0End = 0.3f * 3.5f / 2.4f;
        private const float Phase1End = 1.0f * 3.5f / 2.4f;
        private const float Phase2End = 1.6f * 3.5f / 2.4f;
        private const float Phase3End = 1.9f * 3.5f / 2.4f;
        private const float Phase4End = 3.5f;

        private static bool _destroyed;
        private static MissileCameraBootTilePuzzle? _puzzle;
        private static MissileCameraBootDiagnostics? _diag;
        private static MissileCameraBootTextFx? _textFx;

        internal static bool IsPlaying => _diag != null || _puzzle != null;

        internal static IEnumerator Play(RectTransform fullscreenRoot, RectTransform panelRt)
        {
            _destroyed = false;
            DestroyVisuals();
            if (fullscreenRoot == null || panelRt == null)
                yield break;

            _diag = MissileCameraBootDiagnostics.Create(fullscreenRoot);
            _textFx = new MissileCameraBootTextFx();

            SetFeedAlpha(panelRt, 0f);
            SetFlirVisible(panelRt, false);

            // Wait for seeker RT (and force one render) so tiles are not skipped.
            Texture? feedTex = null;
            for (int i = 0; i < 45 && !_destroyed; i++)
            {
                feedTex = MissileCameraFeedController.EnsureFeedReadyForBoot();
                if (feedTex == null)
                    feedTex = MissileCameraFeedController.TryGetFeedTexture();
                if (feedTex != null)
                    break;
                yield return null;
            }

            if (_destroyed)
                yield break;

            Canvas.ForceUpdateCanvases();
            float w = fullscreenRoot.rect.width;
            float h = fullscreenRoot.rect.height;
            if (w < 64f || h < 64f)
            {
                w = 1920f;
                h = 1080f;
            }

            // --- Phase 0: puzzle ---
            _diag.BeginStage("VIDEO STREAM REASSEMBLY");
            _puzzle = MissileCameraBootTilePuzzle.Create(fullscreenRoot, feedTex, w, h);
            if (_puzzle == null)
                MfdLog.Info("boot tiles failed to create");

            float elapsed = 0f;
            while (elapsed < Phase0End)
            {
                if (_destroyed)
                    yield break;
                float dt = Time.unscaledDeltaTime;
                if (dt <= 0f)
                    dt = 0.016f;
                elapsed += dt;
                // Keep texture fresh on tiles if RT was late.
                if (feedTex == null)
                {
                    feedTex = MissileCameraFeedController.EnsureFeedReadyForBoot();
                    if (feedTex != null && _puzzle == null)
                        _puzzle = MissileCameraBootTilePuzzle.Create(fullscreenRoot, feedTex, w, h);
                }

                _puzzle?.Tick(Mathf.Clamp01(elapsed / Phase0End));
                yield return null;
            }

            if (_destroyed)
                yield break;

            _puzzle?.Tick(1f);
            _puzzle?.Destroy();
            _puzzle = null;
            _diag.CompleteCurrentStage();
            SetFeedAlpha(panelRt, 1f);

            // --- Phase 1: flicker + char cal ---
            _diag.BeginStage("INTERFACE SYMBOL CALIBRATION");
            SetFlirVisible(panelRt, true);
            MissileCameraFeedController.RefreshFlirHudOnce();
            RectTransform? flirRoot = MissileCameraHudOverlay.TryGetFlirRoot();
            _textFx.Bind(flirRoot);
            _textFx.RecaptureTargets();

            while (elapsed < Phase1End)
            {
                if (_destroyed)
                    yield break;
                float dt = Time.unscaledDeltaTime;
                if (dt <= 0f)
                    dt = 0.016f;
                elapsed += dt;
                float p = Mathf.Clamp01((elapsed - Phase0End) / (Phase1End - Phase0End));
                float amp = Mathf.Lerp(0.55f, 0f, p);
                float flicker = 1f - amp * (0.5f + 0.5f * Mathf.Sin(elapsed * 42f));
                _textFx.SetFlickerAlpha(flicker);
                _textFx.TickCharCalibration(dt);
                yield return null;
            }

            if (_destroyed)
                yield break;

            _textFx.SetFlickerAlpha(1f);
            _textFx.RestoreTargets();
            _diag.CompleteCurrentStage();

            // --- Phase 2: hex line drum ---
            _diag.BeginStage("DATA BUS DECODE");
            while (elapsed < Phase2End)
            {
                if (_destroyed)
                    yield break;
                float dt = Time.unscaledDeltaTime;
                if (dt <= 0f)
                    dt = 0.016f;
                elapsed += dt;
                _textFx.TickHexLineDrum();
                yield return null;
            }

            if (_destroyed)
                yield break;

            _textFx.RestoreTargets();
            _diag.CompleteCurrentStage();

            // --- Phase 3: value drum ---
            _diag.BeginStage("TELEMETRY VALUE LOCK");
            while (elapsed < Phase3End)
            {
                if (_destroyed)
                    yield break;
                float dt = Time.unscaledDeltaTime;
                if (dt <= 0f)
                    dt = 0.016f;
                elapsed += dt;
                _textFx.TickValueDrum(Time.unscaledTime);
                yield return null;
            }

            if (_destroyed)
                yield break;

            _textFx.RestoreTargets();
            _diag.CompleteCurrentStage();

            // --- Phase 4: badge hold ---
            _diag.DimStackAndShowBadge();
            while (elapsed < Phase4End)
            {
                if (_destroyed)
                    yield break;
                float dt = Time.unscaledDeltaTime;
                if (dt <= 0f)
                    dt = 0.016f;
                elapsed += dt;
                yield return null;
            }

            if (_destroyed)
                yield break;

            DestroyVisuals();
            SetFeedAlpha(panelRt, 1f);
            SetFlirVisible(panelRt, true);
            MfdLog.Info("fullscreen boot sequence complete");
        }

        internal static void Abort()
        {
            _destroyed = true;
            DestroyVisuals();
        }

        private static void DestroyVisuals()
        {
            _textFx?.RestoreTargets();
            _textFx = null;
            _puzzle?.Destroy();
            _puzzle = null;
            _diag?.Destroy();
            _diag = null;
        }

        private static void SetFeedAlpha(RectTransform panelRt, float alpha)
        {
            Transform? feed = FindDeep(panelRt, "MissileCameraFeed");
            if (feed == null)
                return;
            CanvasGroup? cg = feed.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = feed.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = Mathf.Clamp01(alpha);
        }

        private static void SetFlirVisible(RectTransform panelRt, bool visible)
        {
            Transform? hud = FindDeep(panelRt, "MissileCameraHudOverlay");
            if (hud == null)
                return;
            Transform? flir = hud.Find("MissileCameraFlirHud");
            if (flir != null)
                flir.gameObject.SetActive(visible);
        }

        private static Transform? FindDeep(Transform root, string name)
        {
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform? found = FindDeep(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
