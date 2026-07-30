using UnityEngine;

namespace MissileCamera
{
    internal sealed class MissileCameraScanlinesStage : IMissileCameraPostFx
    {
        private static Material? _material;
        private static bool _initFailed;

        public string StageId => "Scanlines";
        public bool IsAvailable => MissileCameraEffectsAvailability.IsStageAvailable(StageId);
        public bool IsEnabled => MissileCameraEffectsConfig.ScanlinesEnabled && IsAvailable;
        public float Intensity => MissileCameraEffectsConfig.ScanlinesIntensity;

        public bool Apply(RenderTexture source, RenderTexture destination) =>
            MissileCameraFxBlit.TryBlit(
                "Hidden/MissileCamera/Scanlines",
                "MissileCamera.Scanlines",
                ref _material,
                ref _initFailed,
                source,
                destination,
                Intensity);
    }

    internal sealed class MissileCameraMotionBlurStage : IMissileCameraPostFx
    {
        private static Material? _material;
        private static bool _initFailed;

        public string StageId => "MotionBlur";
        public bool IsAvailable => MissileCameraEffectsAvailability.IsStageAvailable(StageId);
        public bool IsEnabled => MissileCameraEffectsConfig.MotionBlurEnabled && IsAvailable;
        public float Intensity => MissileCameraEffectsConfig.MotionBlurIntensity;

        public bool Apply(RenderTexture source, RenderTexture destination) =>
            MissileCameraFxBlit.TryBlit(
                "Hidden/MissileCamera/MotionBlur",
                "MissileCamera.MotionBlur",
                ref _material,
                ref _initFailed,
                source,
                destination,
                Intensity);
    }

    internal sealed class MissileCameraChromaticAberrationStage : IMissileCameraPostFx
    {
        private static Material? _material;
        private static bool _initFailed;

        public string StageId => "ChromaticAberration";
        public bool IsAvailable => MissileCameraEffectsAvailability.IsStageAvailable(StageId);
        public bool IsEnabled => MissileCameraEffectsConfig.ChromaticEnabled && IsAvailable;
        public float Intensity => MissileCameraEffectsConfig.ChromaticIntensity;

        public bool Apply(RenderTexture source, RenderTexture destination) =>
            MissileCameraFxBlit.TryBlit(
                "Hidden/MissileCamera/ChromaticAberration",
                "MissileCamera.Chromatic",
                ref _material,
                ref _initFailed,
                source,
                destination,
                Intensity);
    }

    internal sealed class MissileCameraBloomStage : IMissileCameraPostFx
    {
        private static Material? _material;
        private static bool _initFailed;

        public string StageId => "Bloom";
        public bool IsAvailable => MissileCameraEffectsAvailability.IsStageAvailable(StageId);
        public bool IsEnabled => MissileCameraEffectsConfig.BloomEnabled && IsAvailable;
        public float Intensity => MissileCameraEffectsConfig.BloomIntensity;

        public bool Apply(RenderTexture source, RenderTexture destination) =>
            MissileCameraFxBlit.TryBlit(
                "Hidden/MissileCamera/Bloom",
                "MissileCamera.Bloom",
                ref _material,
                ref _initFailed,
                source,
                destination,
                Intensity);
    }
}
