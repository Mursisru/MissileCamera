using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>First-enter-per-mission staged UI reveal. Interruptible.</summary>
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
            if (_running == null)
                return;

            MissileCameraFeedDriverHost.StopCoroutineSafe(_running);
            _running = null;
            _aborted = true;
            ApplyFullVisibility();
        }

        internal static void StartIfNeeded(RectTransform panelRt)
        {
            if (_doneThisMission || panelRt == null)
            {
                ApplyFullVisibility();
                return;
            }

            Abort();
            _aborted = false;
            _running = MissileCameraFeedDriverHost.StartCoroutineSafe(Run(panelRt));
        }

        private static IEnumerator Run(RectTransform panelRt)
        {
            int steps = Mathf.Max(MissileCameraFullscreenConfig.BootstrapSteps, 1);
            float total = Mathf.Max(MissileCameraFullscreenConfig.BootstrapSeconds, 0.05f);
            float stepWait = total / steps;

            SetAlpha(panelRt, "MissileCameraTitle", 0f);
            SetAlpha(panelRt, "MissileCameraFeed", 0f);
            SetHudBlocksVisible(panelRt, corners: false, markers: false);

            yield return FadeChild(panelRt, "MissileCameraTitle", stepWait);
            if (_aborted) yield break;

            yield return FadeChild(panelRt, "MissileCameraFeed", stepWait);
            if (_aborted) yield break;

            SetHudBlocksVisible(panelRt, corners: true, markers: false);
            yield return new WaitForSecondsRealtime(stepWait);
            if (_aborted) yield break;

            SetHudBlocksVisible(panelRt, corners: true, markers: true);
            yield return new WaitForSecondsRealtime(stepWait);
            if (_aborted) yield break;

            ApplyFullVisibility();
            _doneThisMission = true;
            _running = null;
            MfdLog.Info("fullscreen bootstrap complete");
        }

        private static IEnumerator FadeChild(RectTransform panelRt, string childName, float duration)
        {
            Transform? child = FindDeep(panelRt, childName);
            if (child == null)
            {
                yield return new WaitForSecondsRealtime(duration);
                yield break;
            }

            CanvasGroup? group = child.GetComponent<CanvasGroup>();
            if (group == null)
                group = child.gameObject.AddComponent<CanvasGroup>();

            group.alpha = 0f;
            child.gameObject.SetActive(true);
            float t = 0f;
            while (t < duration)
            {
                if (_aborted)
                    yield break;

                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(t / duration);
                yield return null;
            }

            group.alpha = 1f;
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
            child.gameObject.SetActive(true);
        }

        private static void SetHudBlocksVisible(RectTransform panelRt, bool corners, bool markers)
        {
            Transform? hud = FindDeep(panelRt, "MissileCameraHudOverlay");
            if (hud == null)
                return;

            Transform? cornersNode = hud.Find("MissileCameraHudCorners");
            if (cornersNode != null)
                cornersNode.gameObject.SetActive(corners);

            Transform? marker = hud.Find("MissileCameraHudTargetMarker");
            if (marker != null)
                marker.gameObject.SetActive(markers);

            Transform? markerRoot = hud.Find("MissileCameraHudMarkers");
            if (markerRoot != null)
                markerRoot.gameObject.SetActive(markers);
        }

        private static void ApplyFullVisibility()
        {
            // Controllers restore HUD active state on next Update; force CanvasGroup alphas to opaque.
            RectTransform? panelRt = MissileCameraFeedController.TryGetPanelRt();
            if (panelRt == null)
                return;

            SetAlpha(panelRt, "MissileCameraTitle", 1f);
            SetAlpha(panelRt, "MissileCameraFeed", 1f);
            SetHudBlocksVisible(panelRt, corners: true, markers: true);
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
