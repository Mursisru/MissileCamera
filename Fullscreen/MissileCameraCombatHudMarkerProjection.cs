using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace MissileCamera
{
    /// <summary>
    /// CombatHUD markers use mainCamera.WorldToScreenPoint (dump HUDUnitMarker.UpdatePosition).
    /// Fullscreen video is the seeker RT → Overlay RawImage, so those positions stick near cockpit center.
    /// Reproject via feed camera viewport → Screen (RT WorldToScreen is RT pixels — wrong for Overlay).
    /// Never moves CameraStateManager (CAMERA_SAFETY.md).
    /// </summary>
    internal static class MissileCameraCombatHudMarkerProjection
    {
        private static readonly FieldInfo? HiddenField =
            AccessTools.Field(typeof(HUDUnitMarker), "hidden");

        private static int _feedCameraFrame = -1;
        private static Camera? _feedCameraCached;

        internal static void ReprojectIfFullscreen(HUDUnitMarker marker)
        {
            if (!MissileCameraFullscreenController.IsActive)
                return;

            if (marker == null || marker.image == null || marker.unit == null)
                return;

            if (HiddenField != null && HiddenField.GetValue(marker) is true)
                return;

            Camera? feed = ResolveFeedCamera();
            if (feed == null)
                return;

            if (!TryResolveWorld(marker, out Vector3 world))
                return;

            if (marker.selected)
            {
                ReprojectSelected(marker, feed, world);
                return;
            }

            Vector3 screen = FeedWorldToOverlayScreen(feed, world);
            if (screen.z <= 0f)
            {
                if (marker.image.enabled)
                    marker.image.enabled = false;
                return;
            }

            if (!marker.image.enabled)
                marker.image.enabled = true;

            marker.image.transform.position = new Vector3(screen.x, screen.y, 0f);
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

            if (PinToScreenEdgeFeed(feed, world, out Vector3 position, out float arrowAngleRad))
            {
                marker.image.enabled = false;
                hud.SetTargetArrow(
                    true,
                    position,
                    new Vector3(0f, 0f, arrowAngleRad * Mathf.Rad2Deg - 90f));
            }
            else
            {
                marker.image.enabled = true;
                marker.image.transform.position = position;
                hud.SetTargetArrow(false, Vector3.zero, Vector3.zero);
            }
        }

        /// <summary>Dump HUDFunctions.PinToScreenEdge, but feed camera + Overlay screen mapping.</summary>
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

        internal static Vector3 FeedWorldToOverlayScreen(Camera feed, Vector3 world)
        {
            Vector3 vp = feed.WorldToViewportPoint(world);
            return new Vector3(vp.x * Screen.width, vp.y * Screen.height, vp.z);
        }
    }
}
