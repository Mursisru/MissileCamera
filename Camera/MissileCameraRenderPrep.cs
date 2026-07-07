using System.Reflection;
using NuclearOption.Effects;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MissileCamera
{
    /// <summary>
    /// Nuclear Option terrain/detail shaders read per-camera globals from <see cref="ShaderGlobalManager"/>.
    /// Manual <see cref="Camera.Render"/> must set them for the feed camera, then restore the main view.
    /// </summary>
    internal static class MissileCameraRenderPrep
    {
        private static readonly int WindowDataId = Shader.PropertyToID("_WindowData");
        private static readonly int HeightMapId = Shader.PropertyToID("_HeightMap");
        private static readonly int BlockerMapId = Shader.PropertyToID("_BlockerMap");

        private static Vector2Int _lastBakedWindow = new(int.MinValue, int.MinValue);

        internal static void BeforeRender(Camera feedCamera)
        {
            ApplyShaderGlobalsForCamera(feedCamera);
            MirrorUrpFromMain(feedCamera);
            BakeTerrainWindowForCamera(feedCamera);
        }

        internal static void AfterRender()
        {
            Camera? main = Camera.main;
            if (main == null)
                return;

            ApplyShaderGlobalsForCamera(main);
            BakeTerrainWindowForCamera(main);
        }

        private static void ApplyShaderGlobalsForCamera(Camera camera)
        {
            int maxTargetOffset = GetMaxTargetOffset();
            ShaderGlobalManager.SetCameraPlanes(camera, maxTargetOffset, out _);

            Vector2Int windowIndex = GetWindowIndex(camera.transform.position);
            Shader.SetGlobalVector(
                WindowDataId,
                new Vector4(windowIndex.x, windowIndex.y, GetWindowSnapping(), GetWindowSize()));
        }

        private static void BakeTerrainWindowForCamera(Camera camera)
        {
            TerrainHeightMap? terrainHeightMap = SceneSingleton<TerrainHeightMap>.i;
            if (terrainHeightMap == null || terrainHeightMap.heightMap == null)
                return;

            Vector2Int windowIndex = GetWindowIndex(camera.transform.position);
            if (windowIndex != _lastBakedWindow)
            {
                _lastBakedWindow = windowIndex;
                CommandBuffer cmd = new() { name = "MissileCamera.TerrainWindow" };
                try
                {
                    terrainHeightMap.BakeWindow(cmd, windowIndex);
                    Graphics.ExecuteCommandBuffer(cmd);
                }
                finally
                {
                    cmd.Release();
                }
            }

            Shader.SetGlobalTexture(HeightMapId, terrainHeightMap.heightMap);
            Shader.SetGlobalTexture(BlockerMapId, terrainHeightMap.blockerMap);
        }

        private static void MirrorUrpFromMain(Camera feedCamera)
        {
            Camera? main = Camera.main;
            if (main == null)
                return;

            feedCamera.cullingMask = main.cullingMask;
            feedCamera.allowHDR = main.allowHDR;

            UniversalAdditionalCameraData feedUrp = feedCamera.GetUniversalAdditionalCameraData();
            UniversalAdditionalCameraData mainUrp = main.GetUniversalAdditionalCameraData();
            feedUrp.SetRenderer(GetRendererIndex(mainUrp));
            feedUrp.renderShadows = mainUrp.renderShadows;
            feedUrp.requiresDepthOption = CameraOverrideOption.UsePipelineSettings;
            feedUrp.requiresColorOption = CameraOverrideOption.UsePipelineSettings;
        }

        private static int GetRendererIndex(UniversalAdditionalCameraData cameraData)
        {
            FieldInfo? field = typeof(UniversalAdditionalCameraData).GetField(
                "m_RendererIndex",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field?.GetValue(cameraData) is int index)
                return index;

            return 0;
        }

        private static int GetMaxTargetOffset()
        {
            int windowSize = GetWindowSize();
            int windowSnapping = GetWindowSnapping();
            return Mathf.Max(0, windowSize / 2 - windowSnapping * 2);
        }

        private static int GetWindowSize()
        {
            DetailRenderer? detail = SceneSingleton<DetailRenderer>.i;
            return detail != null ? detail.windowSize : 1024;
        }

        private static int GetWindowSnapping()
        {
            DetailRenderer? detail = SceneSingleton<DetailRenderer>.i;
            return detail != null ? detail.windowSnapping : 64;
        }

        private static Vector2Int GetWindowIndex(Vector3 localPosition)
        {
            GlobalPosition global = localPosition.ToGlobalPosition();
            int snapping = GetWindowSnapping();
            return new Vector2Int(
                Mathf.FloorToInt(global.x / snapping),
                Mathf.FloorToInt(global.z / snapping));
        }
    }
}
