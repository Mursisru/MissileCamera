using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace MissileCamera
{
    /// <summary>
    /// Loads optional embedded AssetBundle with MissileCamera/Infrared UI shader.
    /// Missing bundle is non-fatal — Volume ColorAdjustments fallback is used.
    /// </summary>
    internal static class MissileCameraShaderBundle
    {
        private const string ResourceName = "MissileCamera.Shaders.missilecamera_shaders.bundle";
        private const string ShaderAssetName = "MissileCameraInfrared";
        private const string BlitShaderAssetName = "MissileCameraInfraredBlit";
        private const string ShaderFindName = "MissileCamera/Infrared";
        private const string BlitShaderFindName = "Hidden/MissileCamera/InfraredBlit";

        private static bool _attempted;
        private static AssetBundle? _bundle;
        private static Shader? _infraredShader;
        private static Shader? _infraredBlitShader;
        private static readonly System.Collections.Generic.Dictionary<string, Shader?> FxShaders =
            new System.Collections.Generic.Dictionary<string, Shader?>(System.StringComparer.Ordinal);

        internal static Shader? InfraredShader
        {
            get
            {
                EnsureLoaded();
                return _infraredShader;
            }
        }

        internal static Shader? InfraredBlitShader
        {
            get
            {
                EnsureLoaded();
                return _infraredBlitShader;
            }
        }

        internal static bool TryGetFxShader(string findName, out Shader? shader)
        {
            EnsureLoaded();
            if (FxShaders.TryGetValue(findName, out shader) && shader != null)
                return true;

            shader = Shader.Find(findName);
            if (shader != null)
            {
                FxShaders[findName] = shader;
                return true;
            }

            if (_bundle != null)
            {
                string shortName = findName;
                int slash = findName.LastIndexOf('/');
                if (slash >= 0 && slash + 1 < findName.Length)
                    shortName = findName.Substring(slash + 1);

                shader = _bundle.LoadAsset<Shader>(shortName);
                if (shader == null)
                {
                    string[] names = _bundle.GetAllAssetNames();
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (names[i].IndexOf(shortName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            shader = _bundle.LoadAsset<Shader>(names[i]);
                            if (shader != null)
                                break;
                        }
                    }
                }

                if (shader != null)
                {
                    FxShaders[findName] = shader;
                    return true;
                }
            }

            FxShaders[findName] = null;
            return false;
        }

        internal static bool HasInfraredShader
        {
            get
            {
                EnsureLoaded();
                return _infraredShader != null;
            }
        }

        internal static void EnsureLoaded()
        {
            if (_attempted)
                return;

            _attempted = true;
            try
            {
                _infraredShader = Shader.Find(ShaderFindName);
                _infraredBlitShader = Shader.Find(BlitShaderFindName);

                byte[]? bytes = ReadEmbeddedBundle();
                if (bytes == null || bytes.Length == 0)
                    return;

                _bundle = AssetBundle.LoadFromMemory(bytes);
                if (_bundle == null)
                {
                    MfdLog.Error("IR shader AssetBundle LoadFromMemory returned null.");
                    return;
                }

                if (_infraredShader == null)
                {
                    _infraredShader = _bundle.LoadAsset<Shader>(ShaderAssetName);
                    if (_infraredShader == null)
                        _infraredShader = _bundle.LoadAsset<Shader>("Assets/Shaders/MissileCameraInfrared.shader");
                    if (_infraredShader == null)
                    {
                        string[] names = _bundle.GetAllAssetNames();
                        for (int i = 0; i < names.Length && _infraredShader == null; i++)
                        {
                            if (names[i].IndexOf("MissileCameraInfrared", System.StringComparison.OrdinalIgnoreCase) >= 0
                                && names[i].IndexOf("Blit", System.StringComparison.OrdinalIgnoreCase) < 0)
                            {
                                _infraredShader = _bundle.LoadAsset<Shader>(names[i]);
                            }
                        }
                    }

                    if (_infraredShader == null)
                        _infraredShader = Shader.Find(ShaderFindName);
                }

                if (_infraredShader == null)
                    MfdLog.Error("IR UI shader asset missing from embedded AssetBundle.");

                if (_infraredBlitShader == null)
                {
                    _infraredBlitShader = _bundle.LoadAsset<Shader>(BlitShaderAssetName);
                    if (_infraredBlitShader == null)
                        _infraredBlitShader = _bundle.LoadAsset<Shader>("Assets/Shaders/MissileCameraInfraredBlit.shader");
                    if (_infraredBlitShader == null)
                    {
                        string[] blitNames = _bundle.GetAllAssetNames();
                        for (int i = 0; i < blitNames.Length && _infraredBlitShader == null; i++)
                        {
                            if (blitNames[i].IndexOf("MissileCameraInfraredBlit", System.StringComparison.OrdinalIgnoreCase) >= 0)
                                _infraredBlitShader = _bundle.LoadAsset<Shader>(blitNames[i]);
                        }
                    }

                    if (_infraredBlitShader == null)
                        _infraredBlitShader = Shader.Find(BlitShaderFindName);
                }

                if (_infraredBlitShader == null)
                    MfdLog.Error("IR blit shader asset missing from embedded AssetBundle.");
            }
            catch (Exception ex)
            {
                _infraredShader = null;
                _infraredBlitShader = null;
                MfdLog.Error("IR shader bundle load failed: " + ex.Message);
            }
        }

        internal static void Unload()
        {
            _infraredShader = null;
            _infraredBlitShader = null;
            FxShaders.Clear();
            if (_bundle != null)
            {
                _bundle.Unload(false);
                _bundle = null;
            }

            _attempted = false;
        }

        private static byte[]? ReadEmbeddedBundle()
        {
            Assembly assembly = typeof(MissileCameraShaderBundle).Assembly;
            using Stream? stream = assembly.GetManifestResourceStream(ResourceName);
            if (stream == null)
                return null;

            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }
    }
}
