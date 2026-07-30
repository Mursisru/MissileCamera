using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// MFD: auto IR when dark. Fullscreen: manual vision modes (J cycle).
    /// </summary>
    internal static class MissileCameraInfraredEffect
    {
        private const float ExposureEpsilon = 0.0005f;

        private static bool _loggedPath;
        private static bool _lastInfrared;
        private static float _lastExposure = float.NaN;
        private static MissileCameraVisionMode _lastFsMode = (MissileCameraVisionMode)255;
        private static bool _lastFsApplied;

        internal static void Apply(RawImage? feedImage, MissileCameraRig? rig, bool infrared, float exposure)
        {
            try
            {
                if (!infrared)
                {
                    if (!_lastInfrared && Mathf.Approximately(_lastExposure, 0f))
                    {
                        ClearFeedMaterialIfNeeded(feedImage);
                        return;
                    }

                    ClearFeedMaterialIfNeeded(feedImage);
                    rig?.SetVisionMode(MissileCameraVisionMode.Color, 0f);
                    _lastInfrared = false;
                    _lastExposure = 0f;
                    _lastFsApplied = false;
                    return;
                }

                if (_lastInfrared
                    && Mathf.Abs(_lastExposure - exposure) <= ExposureEpsilon
                    && rig != null)
                {
                    ClearFeedMaterialIfNeeded(feedImage);
                    return;
                }

                ClearFeedMaterialIfNeeded(feedImage);
                MissileCameraInfraredAudit.LogStartupOnce();

                if (rig == null)
                {
                    if (!_loggedPath)
                    {
                        _loggedPath = true;
                        MfdLog.Error("IR ON but rig missing — feed stays COLOR.");
                    }

                    _lastInfrared = false;
                    return;
                }

                rig.SetVisionMode(MissileCameraVisionMode.WhiteHot, exposure);

                if (!_loggedPath || !_lastInfrared)
                {
                    _loggedPath = true;
                    MfdLog.Info($"IR on path=hdr+blit finalExp={rig.InfraredBlitExposure:F2}");
                }

                _lastInfrared = true;
                _lastExposure = exposure;
                _lastFsApplied = false;
                MissileCameraInfraredAudit.LogPipeline(feedImage, rig, infrared: true, exposure);
            }
            catch (System.Exception ex)
            {
                MfdLog.Error("IR apply failed: " + ex.Message);
                ClearFeedMaterialIfNeeded(feedImage);
                rig?.SetVisionMode(MissileCameraVisionMode.Color, 0f);
                _lastInfrared = false;
                _lastFsApplied = false;
            }
        }

        internal static void ApplyFullscreenVision(
            RawImage? feedImage,
            MissileCameraRig? rig,
            MissileCameraVisionMode mode,
            float infraredExposure)
        {
            try
            {
                ClearFeedMaterialIfNeeded(feedImage);
                if (rig == null)
                    return;

                if (_lastFsApplied
                    && _lastFsMode == mode
                    && Mathf.Abs(_lastExposure - infraredExposure) <= ExposureEpsilon)
                {
                    return;
                }

                if (MissileCameraVisionModeController.UsesInfraredBlit(mode))
                    MissileCameraInfraredAudit.LogStartupOnce();

                rig.SetVisionMode(mode, infraredExposure);

                if (_lastFsMode != mode)
                    MfdLog.Info("FS vision apply mode=" + mode);

                _lastFsMode = mode;
                _lastFsApplied = true;
                _lastInfrared = MissileCameraVisionModeController.UsesInfraredBlit(mode);
                _lastExposure = infraredExposure;
            }
            catch (System.Exception ex)
            {
                MfdLog.Error("FS vision apply failed: " + ex.Message);
                ClearFeedMaterialIfNeeded(feedImage);
                rig?.SetVisionMode(MissileCameraVisionMode.Color, 0f);
                _lastFsApplied = false;
            }
        }

        internal static void Clear(RawImage? feedImage, MissileCameraRig? rig)
        {
            ClearFeedMaterialIfNeeded(feedImage);
            rig?.SetVisionMode(MissileCameraVisionMode.Color, 0f);
            _lastInfrared = false;
            _lastExposure = 0f;
            _lastFsMode = (MissileCameraVisionMode)255;
            _lastFsApplied = false;
            MissileCameraInfraredPolicy.Reset();
            MissileCameraInfraredAudit.Reset();
        }

        internal static void Shutdown()
        {
            _loggedPath = false;
            _lastInfrared = false;
            _lastExposure = float.NaN;
            _lastFsMode = (MissileCameraVisionMode)255;
            _lastFsApplied = false;
            MissileCameraShaderBundle.Unload();
            MissileCameraInfraredBlit.Shutdown();
            MissileCameraInfraredPolicy.Reset();
            MissileCameraInfraredAudit.Reset();
            MissileCameraVisionModeController.Reset();
        }

        private static void ClearFeedMaterialIfNeeded(RawImage? feedImage)
        {
            if (feedImage != null && feedImage.material != null)
                feedImage.material = null;
        }
    }
}
