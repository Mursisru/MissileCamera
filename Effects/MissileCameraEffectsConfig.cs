using MissileCamera.Config;

namespace MissileCamera
{
    internal static class MissileCameraEffectsConfig
    {
        internal const bool InfraredEnabled = true;

        // Hardcoded intensities (toggles stay in cfg).
        internal const float ScanlinesIntensity = 0.35f;
        internal const float MotionBlurIntensity = 0.25f;
        internal const float ChromaticIntensity = 0.2f;
        internal const float BloomIntensity = 0.3f;

        internal static bool ScanlinesEnabled;
        internal static bool MotionBlurEnabled;
        internal static bool ChromaticEnabled;
        internal static bool BloomEnabled;
        internal static int Revision;

        internal static void Refresh(bool force = false)
        {
            if (!MissileCameraBepInConfig.IsBound)
                return;

            bool scanlinesEnabled = MissileCameraBepInConfig.FxScanlinesEnabled.Value;
            bool motionBlurEnabled = MissileCameraBepInConfig.FxMotionBlurEnabled.Value;
            bool chromaticEnabled = MissileCameraBepInConfig.FxChromaticEnabled.Value;
            bool bloomEnabled = MissileCameraBepInConfig.FxBloomEnabled.Value;

            if (!force
                && scanlinesEnabled == ScanlinesEnabled
                && motionBlurEnabled == MotionBlurEnabled
                && chromaticEnabled == ChromaticEnabled
                && bloomEnabled == BloomEnabled)
                return;

            ScanlinesEnabled = scanlinesEnabled;
            MotionBlurEnabled = motionBlurEnabled;
            ChromaticEnabled = chromaticEnabled;
            BloomEnabled = bloomEnabled;
            Revision++;
        }
    }
}
