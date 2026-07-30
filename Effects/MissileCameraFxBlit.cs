using UnityEngine;

namespace MissileCamera
{
    internal static class MissileCameraFxBlit
    {
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        internal static bool TryBlit(
            string shaderFindName,
            string materialName,
            ref Material? material,
            ref bool initFailed,
            RenderTexture source,
            RenderTexture destination,
            float intensity)
        {
            if (source == null || destination == null)
                return false;

            Material? mat = EnsureMaterial(shaderFindName, materialName, ref material, ref initFailed);
            if (mat == null)
                return false;

            mat.SetFloat(IntensityId, Mathf.Clamp01(intensity));
            if (mat.HasProperty(MainTexId))
                mat.SetTexture(MainTexId, source);

            Graphics.Blit(source, destination, mat);
            return true;
        }

        private static Material? EnsureMaterial(
            string shaderFindName,
            string materialName,
            ref Material? material,
            ref bool initFailed)
        {
            if (material != null)
                return material;

            if (initFailed)
                return null;

            if (!MissileCameraShaderBundle.TryGetFxShader(shaderFindName, out Shader? shader) || shader == null)
            {
                initFailed = true;
                return null;
            }

            try
            {
                material = new Material(shader)
                {
                    name = materialName,
                    hideFlags = HideFlags.HideAndDontSave
                };
                return material;
            }
            catch (System.Exception ex)
            {
                initFailed = true;
                MfdLog.Error(materialName + " create failed: " + ex.Message);
                return null;
            }
        }
    }
}
