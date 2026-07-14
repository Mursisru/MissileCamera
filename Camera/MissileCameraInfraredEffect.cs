using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// Auto IR via TargetCam-style URP ColorAdjustments on the feed camera during <see cref="MissileCameraRig.RenderFrame"/>.
    /// RawImage stays unmodified (HUD color layer separate). Local visual only — MP safe.
    /// </summary>
    internal static class MissileCameraInfraredEffect
    {
        private static bool _loggedPath;
        private static bool _lastInfrared;
        private static float _lastExposure = float.NaN;

        internal static void Apply(RawImage? feedImage, MissileCameraRig? rig, bool infrared, float exposure)
        {
            try
            {
                ClearFeedMaterial(feedImage);

                if (!infrared)
                {
                    rig?.SetInfraredVolume(false, 0f);
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

                rig.SetInfraredVolume(true, exposure);

                if (!_loggedPath || !_lastInfrared)
                {
                    _loggedPath = true;
                    if (rig.IsPipelineDriven)
                        MfdLog.Info($"IR on path=urp-volume finalExp={rig.InfraredBlitExposure:F2} (TargetCam parity)");
                    else
                        MfdLog.Info($"IR on path=hdr+blit finalExp={rig.InfraredBlitExposure:F2} (TargetCam EV)");
                }

                _lastInfrared = true;
                _lastExposure = exposure;
                MissileCameraInfraredAudit.LogPipeline(feedImage, rig, infrared: true, exposure);
            }
            catch (System.Exception ex)
            {
                MfdLog.Error("IR apply failed: " + ex.Message);
                ClearFeedMaterial(feedImage);
                rig?.SetInfraredVolume(false, 0f);
                _lastInfrared = false;
            }
        }

        internal static void Clear(RawImage? feedImage, MissileCameraRig? rig)
        {
            ClearFeedMaterial(feedImage);
            rig?.SetInfraredVolume(false, 0f);
            _lastInfrared = false;
            _lastExposure = 0f;
            MissileCameraInfraredPolicy.Reset();
            MissileCameraInfraredAudit.Reset();
        }

        internal static void Shutdown()
        {
            _loggedPath = false;
            _lastInfrared = false;
            _lastExposure = float.NaN;
            MissileCameraShaderBundle.Unload();
            MissileCameraInfraredBlit.Shutdown();
            MissileCameraInfraredPolicy.Reset();
            MissileCameraInfraredAudit.Reset();
        }

        private static void ClearFeedMaterial(RawImage? feedImage)
        {
            if (feedImage != null && feedImage.material != null)
                feedImage.material = null;
        }
    }
}
