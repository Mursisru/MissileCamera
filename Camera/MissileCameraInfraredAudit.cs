using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// Full IR pipeline audit: exposure breakdown, URP state, RT luminance samples (rate-limited).
    /// </summary>
    internal static class MissileCameraInfraredAudit
    {
        private const float LogIntervalSeconds = 2f;
        private const int SampleSize = 8;

        private static float _nextLogUnscaled;
        private static bool _loggedStartup;
        private static Texture2D? _readbackTexture;

        internal static void LogStartupOnce()
        {
            if (_loggedStartup)
                return;

            _loggedStartup = true;
            MfdLog.Info(
                "IR audit ready path=hdr+blit (TargetCam EV + soft highlight compress, no URP Volume)");
        }

        internal static void LogPipeline(
            RawImage? feedImage,
            MissileCameraRig? rig,
            bool infrared,
            float policyExposure)
        {
            if (!infrared || rig == null)
                return;

            float now = Time.unscaledTime;
            if (now < _nextLogUnscaled)
                return;

            _nextLogUnscaled = now + LogIntervalSeconds;
            LogFullReport(feedImage, rig, policyExposure, afterRender: false);
        }

        internal static void LogAfterRender(MissileCameraRig rig, RenderTexture? displayRt)
        {
            float now = Time.unscaledTime;
            if (now < _nextLogUnscaled)
                return;

            _nextLogUnscaled = now + LogIntervalSeconds;
            LogFullReport(null, rig, rig.LastPolicyExposure, afterRender: true, displayRt);
        }

        internal static void Reset()
        {
            _nextLogUnscaled = 0f;
            _loggedStartup = false;
            if (_readbackTexture != null)
            {
                Object.Destroy(_readbackTexture);
                _readbackTexture = null;
            }

            MissileCameraInfraredExposure.Reset();
        }

        private static void LogFullReport(
            RawImage? feedImage,
            MissileCameraRig rig,
            float policyExposure,
            bool afterRender,
            RenderTexture? sampleRt = null)
        {
            InfraredExposureBreakdown breakdown = rig.LastExposureBreakdown;
            TargetCamAccess.TryGetVanillaIrSnapshot(
                out bool vanillaIr,
                out float vanillaExposure,
                out float vanillaContrast);

            string feedMat = feedImage != null && feedImage.material != null
                ? feedImage.material.shader.name
                : "null";

            RenderTexture? rt = sampleRt ?? rig.Texture;
            string rtInfo = "null";
            string lumaInfo = "n/a";
            if (rt != null)
            {
                rtInfo = $"{rt.width}x{rt.height} fmt={rt.format} hdr={rt.sRGB}";
                if (afterRender && TrySampleLuminance(rt, out float min, out float max, out float avg))
                    lumaInfo = $"min={min:F3} avg={avg:F3} max={max:F3}";
            }

            string phase = afterRender ? "post-render" : "pre-render";

            MfdLog.Info(
                $"IR audit [{phase}] path=hdr+blit policy={policyExposure:F2} final={breakdown.FinalExposure:F2} " +
                $"vanilla={breakdown.VanillaExposure:F2} syncVanilla={breakdown.SyncedVanilla} " +
                $"missileBias={breakdown.MissileBiasEv:F2} blit={rig.LastFrameUsedBlit} " +
                $"contrast={rig.InfraredBlitContrast:F2} allowHDR={rig.FeedCamera.allowHDR} " +
                $"fog={RenderSettings.fog} feedMat={feedMat} rt={rtInfo} luma={lumaInfo} " +
                $"targetCamIR={vanillaIr} targetCamExp={vanillaExposure:F2} targetCamContrast={vanillaContrast:F2}");
        }

        private static bool TrySampleLuminance(RenderTexture rt, out float min, out float max, out float avg)
        {
            min = 0f;
            max = 0f;
            avg = 0f;

            try
            {
                EnsureReadbackTexture();
                if (_readbackTexture == null)
                    return false;

                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = rt;
                try
                {
                    int x = Mathf.Max(0, (rt.width - SampleSize) / 2);
                    int y = Mathf.Max(0, (rt.height - SampleSize) / 2);
                    _readbackTexture.ReadPixels(new Rect(x, y, SampleSize, SampleSize), 0, 0, false);
                    _readbackTexture.Apply(false, false);
                }
                finally
                {
                    RenderTexture.active = prev;
                }

                Color[] pixels = _readbackTexture.GetPixels();
                if (pixels.Length == 0)
                    return false;

                float sum = 0f;
                min = 1f;
                max = 0f;
                for (int i = 0; i < pixels.Length; i++)
                {
                    float lum = pixels[i].grayscale;
                    sum += lum;
                    if (lum < min)
                        min = lum;
                    if (lum > max)
                        max = lum;
                }

                avg = sum / pixels.Length;
                return true;
            }
            catch (System.Exception ex)
            {
                MfdLog.Error("IR audit readback failed: " + ex.Message);
                return false;
            }
        }

        private static void EnsureReadbackTexture()
        {
            if (_readbackTexture != null && _readbackTexture.width == SampleSize && _readbackTexture.height == SampleSize)
                return;

            if (_readbackTexture != null)
                Object.Destroy(_readbackTexture);

            _readbackTexture = new Texture2D(SampleSize, SampleSize, TextureFormat.RGB24, false, true)
            {
                name = "MissileCamera.IRAuditReadback",
                hideFlags = HideFlags.HideAndDontSave
            };
        }
    }
}
