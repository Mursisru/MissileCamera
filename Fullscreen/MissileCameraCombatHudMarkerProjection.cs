using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// CombatHUD markers use mainCamera.WorldToScreenPoint — center-stuck on seeker FS.
    /// Reproject via feed camera viewport → Screen. Missile units: hide image only (never Deselect).
    /// Never rewrite marker.image.color (vanilla faction theme). CAMERA_SAFETY: no CSM writes.
    /// </summary>
    internal static class MissileCameraCombatHudMarkerProjection
    {
        private static readonly FieldInfo? HiddenField =
            AccessTools.Field(typeof(HUDUnitMarker), "hidden");
        private static readonly FieldInfo? MarkersField =
            AccessTools.Field(typeof(CombatHUD), "markers");

        private static int _feedCameraFrame = -1;
        private static Camera? _feedCameraCached;

        internal static void ResetCache()
        {
            _feedCameraFrame = -1;
            _feedCameraCached = null;
        }

        /// <summary>No-op — opaque contrast bleached faction colors white. Keep vanilla theme.</summary>
        internal static void ApplyOpaqueContrast(HUDUnitMarker marker)
        {
            // Intentionally empty.
        }

        internal static void RestoreMarkerImages()
        {
            try
            {
                CombatHUD? hud = SceneSingleton<CombatHUD>.i;
                if (hud == null || MarkersField == null)
                    return;

                if (MarkersField.GetValue(hud) is not System.Collections.Generic.List<HUDUnitMarker> markers)
                    return;

                for (int i = 0; i < markers.Count; i++)
                {
                    HUDUnitMarker? marker = markers[i];
                    if (marker?.image == null)
                        continue;

                    if (HiddenField != null && HiddenField.GetValue(marker) is true)
                        continue;

                    if (!marker.image.enabled)
                        marker.image.enabled = true;
                }
            }
            catch
            {
                // ignore
            }
        }

        internal static void ReprojectIfFullscreen(HUDUnitMarker marker)
        {
            if (!MissileCameraFullscreenController.IsActive)
                return;

            if (marker == null || marker.image == null || marker.unit == null)
                return;

            try
            {
                // Cockpit ghosts of friendly missiles on elevated CombatHUD — hide image only.
                if (marker.unit is Missile)
                {
                    HideMarkerImage(marker);
                    return;
                }

                if (HiddenField != null && HiddenField.GetValue(marker) is true)
                    return;

                Camera? feed = ResolveFeedCamera();
                if (feed == null)
                {
                    HideMarkerImage(marker);
                    return;
                }

                if (!TryResolveWorld(marker, out Vector3 world))
                {
                    HideMarkerImage(marker);
                    return;
                }

                if (marker.selected)
                {
                    ReprojectSelected(marker, feed, world);
                    return;
                }

                Vector3 screen = FeedWorldToOverlayScreen(feed, world);
                if (screen.z <= 0f)
                {
                    HideMarkerImage(marker);
                    return;
                }

                if (!marker.image.enabled)
                    marker.image.enabled = true;

                marker.image.transform.position = new Vector3(screen.x, screen.y, 0f);
            }
            catch
            {
                // Marker failures must never block FS / UpdatePosition.
            }
        }

        private static void HideMarkerImage(HUDUnitMarker marker)
        {
            if (marker?.image != null && marker.image.enabled)
                marker.image.enabled = false;
        }

        private static Camera? ResolveFeedCamera()
        {
            int frame = Time.frameCount;
            if (_feedCameraFrame == frame)
                return _feedCameraCached;

            _feedCameraFrame = frame;
            _feedCameraCached = MissileCameraFeedController.TryGetFeedCamera();
            return _feedCameraCached;
        }

        private static bool TryResolveWorld(HUDUnitMarker marker, out Vector3 world)
        {
            world = default;
            GlobalPosition global = marker.unit.GlobalPosition();
            if (marker.outdated)
            {
                FactionHQ? hq = null;
                try
                {
                    hq = SceneSingleton<DynamicMap>.i?.HQ;
                }
                catch
                {
                    // ignore
                }

                if (hq == null || !hq.TryGetKnownPosition(marker.unit, out global))
                    return false;
            }

            world = global.ToLocalPosition();
            return true;
        }

        private static void ReprojectSelected(HUDUnitMarker marker, Camera feed, Vector3 world)
        {
            CombatHUD? hud = SceneSingleton<CombatHUD>.i;
            if (hud == null)
                return;

            // Never SetTargetArrow(true) — enables CombatHUD.targetText ("Target") TMP.
            // Off-screen selected: hide marker image only (vanilla arrow/label stay suppressed).
            if (PinToScreenEdgeFeed(feed, world, out Vector3 position, out _))
            {
                marker.image.enabled = false;
            }
            else
            {
                marker.image.enabled = true;
                marker.image.transform.position = position;
            }

            try { hud.SetTargetArrow(false, Vector3.zero, Vector3.zero); }
            catch { /* ignore */ }
        }

        private static bool PinToScreenEdgeFeed(
            Camera feed,
            Vector3 world,
            out Vector3 rayToScreen,
            out float arrowAngle)
        {
            bool offScreen = false;
            Vector3 to = world - feed.transform.position;
            rayToScreen = FeedWorldToOverlayScreen(feed, world);
            Vector3 center = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            Vector3 half = center;
            rayToScreen -= center;
            rayToScreen.z = 0f;
            arrowAngle = Mathf.Atan2(rayToScreen.y, rayToScreen.x);
            float tan = Mathf.Tan(arrowAngle);

            if (Vector3.Angle(feed.transform.forward, to) > 90f
                || Mathf.Abs(rayToScreen.x) > Screen.width * 0.5f
                || Mathf.Abs(rayToScreen.y) > Screen.height * 0.5f)
            {
                offScreen = true;
                if (rayToScreen.x > 0f)
                    rayToScreen = new Vector3(half.x, half.x * tan, 0f);
                else
                    rayToScreen = new Vector3(-half.x, -half.x * tan, 0f);

                if (rayToScreen.y > half.y)
                    rayToScreen = new Vector3(half.y / tan, half.y, 0f);
                else if (rayToScreen.y < -half.y)
                    rayToScreen = new Vector3(-half.y / tan, -half.y, 0f);
            }

            rayToScreen += center;
            return offScreen;
        }

        private static Vector3 FeedWorldToOverlayScreen(Camera feed, Vector3 world)
        {
            Vector3 vp = feed.WorldToViewportPoint(world);
            return new Vector3(vp.x * Screen.width, vp.y * Screen.height, vp.z);
        }
    }
}
