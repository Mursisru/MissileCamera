using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>First-enter-per-mission boot (~3.5s), then normal FLIR.</summary>
    internal static class MissileCameraFullscreenBootstrap
    {
        private static Coroutine? _running;
        private static bool _doneThisMission;
        private static bool _aborted;

        internal static bool IsDoneThisMission => _doneThisMission;
        internal static bool IsRunning => _running != null;

        internal static void ResetForMissionUnload()
        {
            Abort();
            _doneThisMission = false;
            _aborted = false;
        }

        internal static void Abort()
        {
            StopRunning();
            _aborted = true;
            ApplyFullVisibility();
        }

        internal static void StartIfNeeded(RectTransform panelRt)
        {
            if (panelRt == null)
                return;

            if (_doneThisMission)
            {
                ApplyFullVisibility();
                return;
            }

            StopRunning();
            _aborted = false;
            _running = MissileCameraFeedDriverHost.StartCoroutineSafe(Run(panelRt));
        }

        private static void StopRunning()
        {
            if (_running != null)
            {
                MissileCameraFeedDriverHost.StopCoroutineSafe(_running);
                _running = null;
            }

            MissileCameraBootSequence.Abort();
        }

        private static IEnumerator Run(RectTransform panelRt)
        {
            KillStubs(panelRt);
            SetFeedVisible(panelRt, true);
            SetHudBlocksVisible(panelRt, flirVisible: false);

            if (!MissileCameraFullscreenController.TryGetFullscreenRoot(out RectTransform? root) || root == null)
            {
                ApplyFullVisibility();
                _doneThisMission = true;
                _running = null;
                yield break;
            }

            yield return MissileCameraBootSequence.Play(root, panelRt);
            if (_aborted)
            {
                _running = null;
                yield break;
            }

            ApplyFullVisibility();
            _doneThisMission = true;
            _running = null;
            MfdLog.Info("fullscreen bootstrap complete");
        }

        private static void SetFeedVisible(RectTransform panelRt, bool visible)
        {
            SetAlpha(panelRt, "MissileCameraFeed", visible ? 1f : 0f);
        }

        private static void SetAlpha(RectTransform panelRt, string childName, float alpha)
        {
            Transform? child = FindDeep(panelRt, childName);
            if (child == null)
                return;

            CanvasGroup? group = child.GetComponent<CanvasGroup>();
            if (group == null)
                group = child.gameObject.AddComponent<CanvasGroup>();

            group.alpha = alpha;
        }

        private static void SetHudBlocksVisible(RectTransform panelRt, bool flirVisible)
        {
            Transform? hud = FindDeep(panelRt, "MissileCameraHudOverlay");
            if (hud == null)
                return;

            Transform? flirNode = hud.Find("MissileCameraFlirHud");
            if (flirNode != null)
                flirNode.gameObject.SetActive(flirVisible);

            Transform? cornersNode = hud.Find("MissileCameraHudCorners");
            if (cornersNode != null)
                cornersNode.gameObject.SetActive(false);
        }

        private static void ApplyFullVisibility()
        {
            RectTransform? panelRt = MissileCameraFeedController.TryGetPanelRt();
            if (panelRt == null)
                return;

            KillStubs(panelRt);
            SetFeedVisible(panelRt, true);
            SetHudBlocksVisible(panelRt, flirVisible: true);
        }

        private static void KillStubs(RectTransform panelRt)
        {
            MissileCameraHudOverlay.ApplyLegacyStubVisibility(panelRt, hide: true);
            KillNamed(panelRt, "MissileCameraTitle");
            KillNamed(panelRt, "MissileCameraColor");
            KillNamed(panelRt, "MissileTelemetry");
        }

        private static void KillNamed(RectTransform panelRt, string name)
        {
            Transform? node = FindDeep(panelRt, name);
            if (node == null)
                return;

            node.gameObject.SetActive(false);
            CanvasGroup? cg = node.GetComponent<CanvasGroup>();
            if (cg != null)
                cg.alpha = 0f;
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
