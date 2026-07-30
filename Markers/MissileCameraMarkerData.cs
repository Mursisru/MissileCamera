using UnityEngine;

namespace MissileCamera
{
    internal enum MissileCameraMarkerType : byte
    {
        Target = 0,
        Aim = 1,
        Threat = 2,
        Ally = 3,
        Waypoint = 4,
        Jam = 5,
        InboundMissile = 6
    }

    internal readonly struct MissileCameraMarkerData
    {
        internal readonly MissileCameraMarkerType Type;
        internal readonly GlobalPosition WorldPosition;
        internal readonly Color Color;
        internal readonly bool ShowLabel;
        /// <summary>World-space velocity for motion vector (zero = none).</summary>
        internal readonly Vector3 VelocityWorld;
        internal readonly bool Valid;

        internal MissileCameraMarkerData(
            MissileCameraMarkerType type,
            GlobalPosition worldPosition,
            Color color,
            bool showLabel,
            Vector3 velocityWorld = default)
        {
            Type = type;
            WorldPosition = worldPosition;
            Color = color;
            ShowLabel = showLabel;
            VelocityWorld = velocityWorld;
            Valid = true;
        }

        internal static MissileCameraMarkerData Invalid => default;
    }
}
