using MissileCamera.Config;

namespace MissileCamera
{
    internal static class MissileCameraEffectsConfig
    {
        internal const bool InfraredEnabled = true;
        internal static bool ScanlinesEnabled;
        internal static float ScanlinesIntensity = 0.35f;
        internal static bool MotionBlurEnabled;
        internal static float MotionBlurIntensity = 0.25f;
        internal static bool ChromaticEnabled;
        internal static float ChromaticIntensity = 0.2f;
        internal static bool BloomEnabled;
        internal static float BloomIntensity = 0.3f;
        internal static int Revision;

        internal static void Refresh(bool force = false)
        {
            if (!MissileCameraBepInConfig.IsBound)
                return;

            bool scanlinesEnabled = MissileCameraBepInConfig.FxScanlinesEnabled.Value;
            float scanlinesIntensity = MissileCameraBepInConfig.FxScanlinesIntensity.Value;
            bool motionBlurEnabled = MissileCameraBepInConfig.FxMotionBlurEnabled.Value;
            float motionBlurIntensity = MissileCameraBepInConfig.FxMotionBlurIntensity.Value;
            bool chromaticEnabled = MissileCameraBepInConfig.FxChromaticEnabled.Value;
            float chromaticIntensity = MissileCameraBepInConfig.FxChromaticIntensity.Value;
            bool bloomEnabled = MissileCameraBepInConfig.FxBloomEnabled.Value;
            float bloomIntensity = MissileCameraBepInConfig.FxBloomIntensity.Value;

            if (!force
                && scanlinesEnabled == ScanlinesEnabled
                && scanlinesIntensity == ScanlinesIntensity
                && motionBlurEnabled == MotionBlurEnabled
                && motionBlurIntensity == MotionBlurIntensity
                && chromaticEnabled == ChromaticEnabled
                && chromaticIntensity == ChromaticIntensity
                && bloomEnabled == BloomEnabled
                && bloomIntensity == BloomIntensity)
                return;

            ScanlinesEnabled = scanlinesEnabled;
            ScanlinesIntensity = scanlinesIntensity;
            MotionBlurEnabled = motionBlurEnabled;
            MotionBlurIntensity = motionBlurIntensity;
            ChromaticEnabled = chromaticEnabled;
            ChromaticIntensity = chromaticIntensity;
            BloomEnabled = bloomEnabled;
            BloomIntensity = bloomIntensity;
            Revision++;
        }
    }
}
