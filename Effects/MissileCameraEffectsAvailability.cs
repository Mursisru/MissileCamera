using UnityEngine;

namespace MissileCamera
{
    internal static class MissileCameraEffectsAvailability
    {
        private static bool _probed;
        private static bool _infraredOk = true;
        private static bool _scanlinesOk;
        private static bool _motionBlurOk;
        private static bool _chromaticOk;
        private static bool _bloomOk;

        internal static void Probe(IMissileCameraPostFx[] stages)
        {
            if (_probed)
                return;

            _probed = true;
            MissileCameraShaderBundle.EnsureLoaded();

            _infraredOk = MissileCameraShaderBundle.InfraredBlitShader != null;
            _scanlinesOk = MissileCameraShaderBundle.TryGetFxShader("Hidden/MissileCamera/Scanlines", out _);
            _motionBlurOk = MissileCameraShaderBundle.TryGetFxShader("Hidden/MissileCamera/MotionBlur", out _);
            _chromaticOk = MissileCameraShaderBundle.TryGetFxShader("Hidden/MissileCamera/ChromaticAberration", out _);
            _bloomOk = MissileCameraShaderBundle.TryGetFxShader("Hidden/MissileCamera/Bloom", out _);

            if (!_infraredOk)
                MfdLog.Warning("FX Infrared blit shader missing — IR stage inactive until bundle rebuild.");
            if (!_scanlinesOk)
                MfdLog.Warning("FX Scanlines shader missing — stage inactive until bundle rebuild.");
            if (!_motionBlurOk)
                MfdLog.Warning("FX MotionBlur shader missing — stage inactive until bundle rebuild.");
            if (!_chromaticOk)
                MfdLog.Warning("FX ChromaticAberration shader missing — stage inactive until bundle rebuild.");
            if (!_bloomOk)
                MfdLog.Warning("FX Bloom shader missing — stage inactive until bundle rebuild.");

            MfdLog.Info(
                $"FX availability IR={_infraredOk} Scanlines={_scanlinesOk} MotionBlur={_motionBlurOk} " +
                $"Chromatic={_chromaticOk} Bloom={_bloomOk}");
        }

        internal static void Reset()
        {
            _probed = false;
        }

        internal static bool IsStageAvailable(string stageId) =>
            stageId switch
            {
                "Infrared" => _infraredOk,
                "Scanlines" => _scanlinesOk,
                "MotionBlur" => _motionBlurOk,
                "ChromaticAberration" => _chromaticOk,
                "Bloom" => _bloomOk,
                _ => false
            };
    }
}
