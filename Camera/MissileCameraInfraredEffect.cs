using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// MFD: auto IR when dark. Fullscreen: manual vision modes (J cycle).
    /// </summary>
    internal static class MissileCameraInfraredEffect
    {
        private static bool _loggedPath;
        private static bool _lastInfrared;
        private static float _lastExposure = float.NaN;
        private static MissileCameraVisionMode _lastFsMode = (MissileCameraVisionMode)255;

        internal static void Apply(RawImage? feedImage, MissileCameraRig? rig, bool infrared, float exposure)
        {
            try
            {
                ClearFeedMaterial(feedImage);

                if (!infrared)
                {
                    rig?.SetVisionMode(MissileCameraVisionMode.Color, 0f);
                    _lastInfrared = false;
                    _lastExposure = 0f;
                    return;
                }

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
                MissileCameraInfraredAudit.LogPipeline(feedImage, rig, infrared: true, exposure);
            }
            catch (System.Exception ex)
            {
                MfdLog.Error("IR apply failed: " + ex.Message);
                ClearFeedMaterial(feedImage);
                rig?.SetVisionMode(MissileCameraVisionMode.Color, 0f);
                _lastInfrared = false;
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
                ClearFeedMaterial(feedImage);
                if (rig == null)
                    return;

                if (MissileCameraVisionModeController.UsesInfraredBlit(mode))
                    MissileCameraInfraredAudit.LogStartupOnce();

                rig.SetVisionMode(mode, infraredExposure);

                if (_lastFsMode != mode)
                {
                    _lastFsMode = mode;
                    MfdLog.Info("FS vision apply mode=" + mode);
                }

                _lastInfrared = MissileCameraVisionModeController.UsesInfraredBlit(mode);
                _lastExposure = infraredExposure;
            }
            catch (System.Exception ex)
            {
                MfdLog.Error("FS vision apply failed: " + ex.Message);
                ClearFeedMaterial(feedImage);
                rig?.SetVisionMode(MissileCameraVisionMode.Color, 0f);
            }
        }

        internal static void Clear(RawImage? feedImage, MissileCameraRig? rig)
        {
            ClearFeedMaterial(feedImage);
            rig?.SetVisionMode(MissileCameraVisionMode.Color, 0f);
            _lastInfrared = false;
            _lastExposure = 0f;
            _lastFsMode = (MissileCameraVisionMode)255;
            MissileCameraInfraredPolicy.Reset();
            MissileCameraInfraredAudit.Reset();
        }

        internal static void Shutdown()
        {
            _loggedPath = false;
            _lastInfrared = false;
            _lastExposure = float.NaN;
            _lastFsMode = (MissileCameraVisionMode)255;
            MissileCameraShaderBundle.Unload();
            MissileCameraInfraredBlit.Shutdown();
            MissileCameraInfraredPolicy.Reset();
            MissileCameraInfraredAudit.Reset();
            MissileCameraVisionModeController.Reset();
        }

        private static void ClearFeedMaterial(RawImage? feedImage)
        {
            if (feedImage != null && feedImage.material != null)
                feedImage.material = null;
        }
    }
}
