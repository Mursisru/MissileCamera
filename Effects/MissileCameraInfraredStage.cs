using UnityEngine;

namespace MissileCamera
{
    /// <summary>
    /// IR remains in <see cref="MissileCameraRig"/> for parity.
    /// This stage is reserved / availability-probed only — never double-applies.
    /// </summary>
    internal sealed class MissileCameraInfraredStage : IMissileCameraPostFx
    {
        internal static void Configure(bool infraredActive, float exposure)
        {
            // Kept for API compatibility with PostFxStack; IR is applied inside the rig.
            _ = infraredActive;
            _ = exposure;
        }

        public string StageId => "Infrared";

        public bool IsAvailable => MissileCameraInfraredBlit.IsAvailable
            || MissileCameraShaderBundle.InfraredBlitShader != null;

        public bool IsEnabled => false;

        public float Intensity => 0f;

        public bool Apply(RenderTexture source, RenderTexture destination) => false;
    }
}
