using UnityEngine;

namespace MissileCamera
{
    internal interface IMissileCameraPostFx
    {
        string StageId { get; }
        bool IsAvailable { get; }
        bool IsEnabled { get; }
        float Intensity { get; }
        bool Apply(RenderTexture source, RenderTexture destination);
    }
}
