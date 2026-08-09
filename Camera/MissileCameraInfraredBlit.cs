using UnityEngine;

namespace MissileCamera
{
    /// <summary>
    /// HDR scene → vision blit (WhiteHot / BlackHot / Contour). Manual Camera.Render path.
    /// </summary>
    internal static class MissileCameraInfraredBlit
    {
        private static readonly int ExposureId = Shader.PropertyToID("_Exposure");
        private static readonly int ContrastId = Shader.PropertyToID("_Contrast");
        private static readonly int HighlightCompressId = Shader.PropertyToID("_HighlightCompress");
        private static readonly int ModeId = Shader.PropertyToID("_Mode");
        private static readonly int EdgeStrengthId = Shader.PropertyToID("_EdgeStrength");

        private const float HighlightCompress = 0.35f;
        private const float EdgeStrength = 2.5f;
        private const float BlackHotExposureBias = -0.75f;

        private static Material? _material;
        private static bool _materialInitFailed;
        private static bool _loggedReady;
        private static int _lastConfigRevision = -1;
        private static float _lastExposure = float.NaN;
        private static float _lastContrast = float.NaN;
        private static int _lastMode = int.MinValue;

        internal static bool IsAvailable
        {
            get
            {
                EnsureMaterial();
                return _material != null;
            }
        }

        internal static void Apply(
            RenderTexture source,
            RenderTexture destination,
            float exposure,
            float contrast)
        {
            Apply(source, destination, exposure, contrast, MissileCameraVisionMode.WhiteHot);
        }

        internal static void Apply(
            RenderTexture source,
            RenderTexture destination,
            float exposure,
            float contrast,
            MissileCameraVisionMode mode)
        {
            if (source == null || destination == null)
                return;

            Material? material = EnsureMaterial();
            if (material == null)
            {
                if (!_loggedReady)
                {
                    _loggedReady = true;
                    MfdLog.Error("Vision blit shader unavailable — feed stays COLOR.");
                }

                Graphics.Blit(source, destination);
                return;
            }

            float applyExposure = exposure;
            if (mode == MissileCameraVisionMode.BlackHot)
                applyExposure = exposure + BlackHotExposureBias;

            SyncMaterialParams(material, applyExposure, contrast, mode);
            Graphics.Blit(source, destination, material);

            if (!_loggedReady)
            {
                _loggedReady = true;
                MfdLog.Info("Vision blit ready shader=" + material.shader.name);
            }
        }

        internal static void Shutdown()
        {
            if (_material != null)
            {
                Object.Destroy(_material);
                _material = null;
            }

            _materialInitFailed = false;
            _loggedReady = false;
            _lastConfigRevision = -1;
            _lastExposure = float.NaN;
            _lastContrast = float.NaN;
            _lastMode = int.MinValue;
        }

        private static Material? EnsureMaterial()
        {
            if (_material != null)
            {
                Shader? live = _material.shader;
                if (live != null
                    && live.isSupported
                    && live.name.IndexOf("Error", System.StringComparison.OrdinalIgnoreCase) < 0)
                    return _material;

                Object.Destroy(_material);
                _material = null;
                _materialInitFailed = false;
            }

            if (_materialInitFailed)
                return null;

            Shader? shader = MissileCameraShaderBundle.InfraredBlitShader;
            if (shader == null
                || !shader.isSupported
                || shader.name.IndexOf("Error", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _materialInitFailed = true;
                MfdLog.Error(
                    shader == null
                        ? "Vision blit shader missing from AssetBundle/Shader.Find."
                        : "Vision blit rejected broken shader=" + shader.name);
                return null;
            }

            try
            {
                _material = new Material(shader)
                {
                    name = "MissileCamera.VisionBlit",
                    hideFlags = HideFlags.HideAndDontSave
                };
                return _material;
            }
            catch (System.Exception ex)
            {
                _materialInitFailed = true;
                MfdLog.Error("Vision blit material create failed: " + ex.Message);
                return null;
            }
        }

        private static void SyncMaterialParams(
            Material material,
            float exposure,
            float contrast,
            MissileCameraVisionMode mode)
        {
            int modeInt = (int)mode;
            int revision = MissileCameraFeedConfig.Revision;
            if (revision == _lastConfigRevision
                && Mathf.Approximately(exposure, _lastExposure)
                && Mathf.Approximately(contrast, _lastContrast)
                && modeInt == _lastMode)
            {
                return;
            }

            _lastConfigRevision = revision;
            _lastExposure = exposure;
            _lastContrast = contrast;
            _lastMode = modeInt;
            material.SetFloat(ExposureId, exposure);
            material.SetFloat(ContrastId, Mathf.Max(0.01f, contrast));
            material.SetFloat(HighlightCompressId, HighlightCompress);
            material.SetFloat(ModeId, modeInt);
            material.SetFloat(EdgeStrengthId, EdgeStrength);
        }
    }
}
