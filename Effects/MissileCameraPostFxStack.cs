using UnityEngine;

namespace MissileCamera
{
    /// <summary>Ordered blit chain on the missile display RT. Pass-through when no stage active.</summary>
    internal static class MissileCameraPostFxStack
    {
        private static RenderTexture? _tempA;
        private static RenderTexture? _tempB;
        private static int _tempW;
        private static int _tempH;
        private static readonly IMissileCameraPostFx[] Stages =
        {
            new MissileCameraInfraredStage(),
            new MissileCameraScanlinesStage(),
            new MissileCameraMotionBlurStage(),
            new MissileCameraChromaticAberrationStage(),
            new MissileCameraBloomStage()
        };

        internal static void ProbeAvailabilityAtStartup()
        {
            MissileCameraEffectsAvailability.Probe(Stages);
        }

        internal static RenderTexture? Apply(RenderTexture? source, bool infraredActive, float infraredExposure)
        {
            if (source == null)
                return null;

            MissileCameraEffectsConfig.Refresh();
            MissileCameraInfraredStage.Configure(infraredActive, infraredExposure);

            bool any = false;
            for (int i = 0; i < Stages.Length; i++)
            {
                IMissileCameraPostFx stage = Stages[i];
                if (stage.IsAvailable && stage.IsEnabled && stage.Intensity > 0.001f)
                {
                    any = true;
                    break;
                }
            }

            if (!any)
                return source;

            EnsureTemps(source.width, source.height);
            if (_tempA == null || _tempB == null)
                return source;

            RenderTexture read = source;
            RenderTexture write = _tempA;
            bool wrote = false;

            for (int i = 0; i < Stages.Length; i++)
            {
                IMissileCameraPostFx stage = Stages[i];
                if (!stage.IsAvailable || !stage.IsEnabled || stage.Intensity <= 0.001f)
                    continue;

                if (!stage.Apply(read, write))
                    continue;

                wrote = true;
                RenderTexture nextRead = write;
                write = read == source ? _tempB : (read == _tempA ? _tempB : _tempA);
                if (write == source)
                    write = _tempA == nextRead ? _tempB : _tempA;
                read = nextRead;
            }

            return wrote ? read : source;
        }

        internal static void Release()
        {
            ReleaseTemp(ref _tempA);
            ReleaseTemp(ref _tempB);
            _tempW = 0;
            _tempH = 0;
        }

        private static void EnsureTemps(int width, int height)
        {
            if (_tempA != null && _tempB != null && _tempW == width && _tempH == height)
                return;

            Release();
            _tempW = width;
            _tempH = height;
            _tempA = CreateTemp(width, height, "MissileCamera.FxTempA");
            _tempB = CreateTemp(width, height, "MissileCamera.FxTempB");
        }

        private static RenderTexture CreateTemp(int width, int height, string name)
        {
            var rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            rt.Create();
            return rt;
        }

        private static void ReleaseTemp(ref RenderTexture? rt)
        {
            if (rt == null)
                return;

            rt.Release();
            Object.Destroy(rt);
            rt = null;
        }
    }
}
