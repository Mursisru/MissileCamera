using System;
using System.Collections;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using MissileCamera.Config;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MissileCamera
{
    /// <summary>Persists across scene loads. Harmony/feed start on mission scene (NOLoader loadStage Mission parity).</summary>
    internal sealed class MissileCameraHost : MonoBehaviour
    {
        private static MissileCameraHost? _instance;
        private Harmony? _harmony;
        private string _pluginDir = string.Empty;
        private ManualLogSource? _logger;
        private bool _missionReady;
        private bool _startupScheduled;

        internal static bool IsMissionReady => _instance != null && _instance._missionReady;

        internal static void Ensure(string pluginDir, ManualLogSource logger)
        {
            if (_instance != null)
                return;

            var go = new GameObject("MissileCamera.Host");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            _instance = go.AddComponent<MissileCameraHost>();
            _instance._pluginDir = pluginDir;
            _instance._logger = logger;
            SceneManager.sceneLoaded += _instance.OnSceneLoaded;
            _instance.TryBootstrapCurrentScene();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_missionReady)
                return;

            if (IsMenuOrSystemScene(scene.path))
                return;

            ScheduleMissionStartup(scene.path);
        }

        private void TryBootstrapCurrentScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!IsMenuOrSystemScene(scene.path))
                ScheduleMissionStartup(scene.path);
        }

        private void ScheduleMissionStartup(string scenePath)
        {
            if (_missionReady || _startupScheduled)
                return;

            _startupScheduled = true;
            StartCoroutine(DeferredMissionStartup(scenePath));
        }

        private IEnumerator DeferredMissionStartup(string scenePath)
        {
            yield return null;
            if (_missionReady)
                yield break;

            _missionReady = true;
            StartupMission(_pluginDir, _logger!, scenePath);
        }

        private void StartupMission(string pluginDir, ManualLogSource logger, string scenePath)
        {
            ModPaths.Init(pluginDir);
            ModIniConfig.EnsureDefault(pluginDir, MissileCameraPlugin.DefaultIni);
            MfdLayoutConfig.Init(pluginDir);
            MissileCameraFeedConfig.Refresh(force: true);
            MissileCameraHudConfig.Refresh(force: true);

            ApplyHarmonyPatches(logger);
            MissileCameraFeedDriverHost.Ensure();

            logger.LogInfo("MissileCamera host ready (mission).");
            MfdLog.Info("host ready v" + AppVersion.DisplayVersion + " scene=" + scenePath);
        }

        private static bool IsMenuOrSystemScene(string path)
        {
            if (string.IsNullOrEmpty(path))
                return true;

            return path.IndexOf("MainMenu", StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("MultiplayerMenu", StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("MissionsMenu", StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("Encyclopedia", StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("MissionEditor", StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("empty", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void ApplyHarmonyPatches(ManualLogSource logger)
        {
            if (_harmony != null)
                return;

            _harmony = new Harmony(MissileCameraPlugin.PluginGuid);
            _harmony.PatchAll(typeof(MissileCameraPlugin).Assembly);

            int patched = 0;
            foreach (MethodBase method in _harmony.GetPatchedMethods())
            {
                patched++;
                logger.LogInfo("Harmony target: " + method.DeclaringType?.FullName + "." + method.Name);
            }

            if (patched == 0)
                logger.LogError("Harmony applied zero game patches.");
            else
                logger.LogInfo("Harmony patched methods: " + patched);
        }

        private void OnApplicationQuit()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            MissileCameraFeedDriverHost.Shutdown();
            _harmony?.UnpatchSelf();
            _harmony = null;
            _instance = null;
        }
    }
}
