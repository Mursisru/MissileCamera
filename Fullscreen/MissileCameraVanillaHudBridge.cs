using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// Fullscreen: elevate CombatHUD markers + missile target highlight.
    /// NEVER HideGo/SetActive/EnableCanvas on FlightHud or CombatHUD chrome — that stuck across sorties.
    /// </summary>
    internal static class MissileCameraVanillaHudBridge
    {
        private const int MarkersSortingOrder = 120;

        private static readonly MethodInfo? UpdateMarkersMethod =
            HarmonyLib.AccessTools.Method(typeof(CombatHUD), "UpdateMarkers");

        private static Canvas? _combatCanvas;
        private static bool _canvasElevated;
        private static RenderMode _savedRenderMode;
        private static int _savedSortingOrder;
        private static bool _savedOverrideSorting;
        private static Camera? _savedWorldCamera;
        private static bool _savedPixelPerfect;

        internal static void OnFullscreenEntered()
        {
            try
            {
                HideStubsOnMissilePanel();
                ElevateCombatHudCanvas();
                ForceCombatHudMarkerPass();
                MissileCameraFullscreenTargetLock.OnFullscreenEntered();
            }
            catch (Exception ex)
            {
                MfdLog.Info("fullscreen enter hud failed: " + ex.Message);
            }

            MfdLog.Info("fullscreen markers-only"
                + (_canvasElevated ? " canvas↑" : " canvas miss")
                + " (no vanilla HideGo)");
        }

        internal static void OnFullscreenExited()
        {
            MissileCameraFullscreenTargetLock.OnFullscreenExited();
            RestoreCombatHudCanvas();
            ForceCombatHudMarkerPass();
        }

        /// <summary>
        /// If CombatHUD still alive — restore canvas/target lock. Else drop flags only.
        /// </summary>
        internal static void ResetForMissionUnload()
        {
            CombatHUD? hud = null;
            try
            {
                hud = SceneSingleton<CombatHUD>.i;
            }
            catch
            {
                // ignore
            }

            if (hud != null)
            {
                try
                {
                    OnFullscreenExited();
                    return;
                }
                catch (Exception ex)
                {
                    MfdLog.Info("fullscreen unload restore failed: " + ex.Message);
                }
            }

            MissileCameraFullscreenTargetLock.AbandonSession();
            _combatCanvas = null;
            _canvasElevated = false;
        }

        internal static void TickHideStubs()
        {
            if (!MissileCameraFullscreenController.IsActive)
                return;

            HideStubsOnMissilePanel();
        }

        internal static void LateTickMarkers()
        {
            if (!MissileCameraFullscreenController.IsActive)
                return;

            ForceCombatHudMarkerPass();
            MissileCameraFullscreenTargetLock.Maintain();
        }

        private static void HideStubsOnMissilePanel()
        {
            RectTransform? panelRt = MissileCameraFeedController.TryGetPanelRt();
            if (panelRt == null)
                return;

            MissileCameraHudOverlay.ApplyLegacyStubVisibility(panelRt, hide: true);
            ForceStubGone(panelRt, "MissileCameraTitle");
            ForceStubGone(panelRt, "MissileCameraColor");
            ForceStubGone(panelRt, "MissileTelemetry");

            if (panelRt.TryGetComponent(out Image panelImage))
            {
                Color c = panelImage.color;
                c.a = 0f;
                panelImage.color = c;
                panelImage.raycastTarget = false;
            }
        }

        private static void ForceStubGone(RectTransform panelRt, string childName)
        {
            Transform? node = FindDeep(panelRt, childName);
            if (node == null)
                return;

            node.gameObject.SetActive(false);
            CanvasGroup? cg = node.GetComponent<CanvasGroup>();
            if (cg != null)
                cg.alpha = 0f;

            if (node.TryGetComponent(out Text text))
            {
                text.text = string.Empty;
                text.enabled = false;
            }
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

        private static void ElevateCombatHudCanvas()
        {
            CombatHUD? hud = SceneSingleton<CombatHUD>.i;
            if (hud == null)
                return;

            Canvas? canvas = ResolveCombatCanvas(hud);
            if (canvas == null)
                return;

            _combatCanvas = canvas;
            if (_canvasElevated)
                return;

            _savedRenderMode = canvas.renderMode;
            _savedSortingOrder = canvas.sortingOrder;
            _savedOverrideSorting = canvas.overrideSorting;
            _savedWorldCamera = canvas.worldCamera;
            _savedPixelPerfect = canvas.pixelPerfect;

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = MarkersSortingOrder;
            canvas.pixelPerfect = false;
            _canvasElevated = true;
        }

        private static void RestoreCombatHudCanvas()
        {
            if (!_canvasElevated)
            {
                _combatCanvas = null;
                return;
            }

            Canvas? canvas = _combatCanvas;
            _canvasElevated = false;
            _combatCanvas = null;
            if (canvas == null)
                return;

            canvas.renderMode = _savedRenderMode;
            canvas.sortingOrder = _savedSortingOrder;
            canvas.overrideSorting = _savedOverrideSorting;
            canvas.worldCamera = _savedWorldCamera;
            canvas.pixelPerfect = _savedPixelPerfect;
        }

        private static void ForceCombatHudMarkerPass()
        {
            CombatHUD? hud = SceneSingleton<CombatHUD>.i;
            if (hud == null)
                return;

            try
            {
                if (hud.iconLayer != null && !hud.iconLayer.gameObject.activeSelf)
                    hud.iconLayer.gameObject.SetActive(true);

                UpdateMarkersMethod?.Invoke(hud, null);
            }
            catch
            {
                // ignore
            }
        }

        private static Canvas? ResolveCombatCanvas(CombatHUD hud)
        {
            if (hud.iconLayer != null)
            {
                Canvas? c = hud.iconLayer.GetComponentInParent<Canvas>();
                if (c != null)
                    return c;
            }

            return hud.GetComponentInParent<Canvas>();
        }
    }
}
