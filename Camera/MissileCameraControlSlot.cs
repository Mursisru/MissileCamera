namespace MissileCamera
{
    /// <summary>
    /// Reserved hook for future missile steering / guidance control.
    /// Core feed/HUD/FX must never depend on a real implementation.
    /// </summary>
    internal interface IMissileControlHook
    {
        bool IsActive { get; }
    }

    /// <summary>Empty slot — always inactive until a future control mod wires in.</summary>
    internal static class MissileCameraControlSlot
    {
        internal static IMissileControlHook? Active { get; set; }

        internal static bool HasActiveControl => Active != null && Active.IsActive;
    }
}
