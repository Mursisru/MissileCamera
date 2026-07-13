using UnityEngine;

namespace MissileCamera
{
    /// <summary>
    /// Guaranteed IR: HDR scene → TargetCam ColorAdjustments math via blit (manual Camera.Render does not reliably run URP volumes).
    /// </summary>
    internal static class MissileCameraInfraredBlit
    {
        private static readonly int ExposureId = Shader.PropertyToID("_Exposure");
        private static readonly int ContrastId = Shader.PropertyToID("_Contrast");
        private static readonly int HighlightCompressId = Shader.PropertyToID("_HighlightCompress");

        private const float HighlightCompress = 0.35f;

        private static Material? _material;
        private static bool _materialInitFailed;
        private static bool _loggedReady;
        private static int _lastConfigRevision = -1;
        private static float _lastExposure = float.NaN;
        private static float _lastContrast = float.NaN;

        internal static bool IsAvailable
        {
            get
            {
                EnsureMaterial();
                return _material != null;
            }
        }

        internal static void Apply(RenderTexture source, RenderTexture destination, float exposure, float contrast)
        {
            if (source == null || destination == null)
                return;

            Material? material = EnsureMaterial();
            if (material == null)
            {
                if (!_loggedReady)
                {
                    _loggedReady = true;
                    MfdLog.Error("IR blit shader unavailable — feed stays COLOR.");
                }

                Graphics.Blit(source, destination);
                return;
            }

            SyncMaterialParams(material, exposure, contrast);
            Graphics.Blit(source, destination, material);

            if (!_loggedReady)
            {
                _loggedReady = true;
                MfdLog.Info("IR blit ready shader=" + material.shader.name);
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
        }

        private static Material? EnsureMaterial()
        {
            if (_material != null)
                return _material;

            if (_materialInitFailed)
                return null;

            Shader? shader = MissileCameraShaderBundle.InfraredBlitShader;
            if (shader == null || !shader.isSupported)
            {
                _materialInitFailed = true;
                MfdLog.Error(
                    shader == null
                        ? "IR blit shader missing from AssetBundle."
                        : "IR blit shader not supported on this GPU.");
                return null;
            }

            try
            {
                _material = new Material(shader)
                {
                    name = "MissileCamera.InfraredBlit",
                    hideFlags = HideFlags.HideAndDontSave
                };
                return _material;
            }
            catch (System.Exception ex)
            {
                _materialInitFailed = true;
                MfdLog.Error("IR blit material create failed: " + ex.Message);
                return null;
            }
        }

        private static void SyncMaterialParams(Material material, float exposure, float contrast)
        {
            int revision = MissileCameraFeedConfig.Revision;
            if (revision == _lastConfigRevision
                && Mathf.Approximately(exposure, _lastExposure)
                && Mathf.Approximately(contrast, _lastContrast))
            {
                return;
            }

            _lastConfigRevision = revision;
            _lastExposure = exposure;
            _lastContrast = contrast;
            material.SetFloat(ExposureId, exposure);
            material.SetFloat(ContrastId, Mathf.Max(0.01f, contrast));
            material.SetFloat(HighlightCompressId, HighlightCompress);
        }
    }
}
