namespace MissileCamera
{
    /// <summary>
    /// HARD RULE — never touch vanilla cockpit camera.
    /// Do NOT modify CameraStateManager.mainCamera, cameraPivot, FOV, nearClip, parenting, or cameraMode.
    /// Fullscreen video = MissileCameraRig RenderTexture → RawImage only.
    /// See Fullscreen/CAMERA_SAFETY.md.
    /// </summary>
    internal static class MissileCameraFullscreenViewDriver
    {
        internal static void Enter(Missile? missile)
        {
            // Intentionally empty — never hijack CSM.
        }

        internal static void TickZoom(float zoomOffset)
        {
            // Zoom is applied on MissileCameraRig via FeedController.
        }

        internal static void LateTick()
        {
            // Intentionally empty — never write camera transforms in LateUpdate.
        }

        internal static void Exit()
        {
            // Intentionally empty — never SnapToCockpit / restore FOV on CSM.
        }

        internal static void SnapToCockpit()
        {
            // Intentionally empty. NEVER reintroduce CSM parenting here.
        }
    }
}
