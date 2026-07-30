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
        private bool _teardownInProgress;
        private int _teardownEpoch;

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
            SceneManager.sceneUnloaded += _instance.OnSceneUnloaded;
            _instance.TryBootstrapCurrentScene();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SafeMissionTeardown("host_destroy", shutdownDriver: true, unpatchHarmony: true);
            _instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (IsMenuOrSystemScene(scene.path))
            {
                if (_missionReady)
                    SafeMissionTeardown("menu_scene_loaded");
                return;
            }

            if (_missionReady)
                return;

            ScheduleMissionStartup(scene.path);
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (scene.path.IndexOf("GameWorld", StringComparison.OrdinalIgnoreCase) < 0
                && !string.Equals(scene.name, "GameWorld", StringComparison.OrdinalIgnoreCase))
                return;

            SafeMissionTeardown("scene_unloaded_gameworld");
        }

        private void SafeMissionTeardown(string reason, bool shutdownDriver = false, bool unpatchHarmony = false)
        {
            if (_teardownInProgress)
                return;

            _teardownInProgress = true;
            _teardownEpoch++;
            try
            {
                MfdLog.Info($"mission teardown epoch={_teardownEpoch} reason={reason}");

                try { MissileCameraFullscreenController.ResetForMissionUnload(); }
                catch (Exception ex) { MfdLog.Info("teardown FS failed: " + ex.Message); }

                try { MfdLayoutRetryHost.Cancel(); }
                catch (Exception ex) { MfdLog.Info("teardown retry failed: " + ex.Message); }

                try { MfdLayoutController.ResetForMissionUnload(); }
                catch (Exception ex) { MfdLog.Info("teardown layout failed: " + ex.Message); }

                try { MfdWeaponsZoneAccess.ResetForMissionUnload(); }
                catch (Exception ex) { MfdLog.Info("teardown weapons failed: " + ex.Message); }

                try { MissileCameraFeedController.ResetForMissionUnload(); }
                catch (Exception ex) { MfdLog.Info("teardown feed failed: " + ex.Message); }

                try { MissileCameraRenderPrep.ResetAll(); }
                catch (Exception ex) { MfdLog.Info("teardown render failed: " + ex.Message); }

                try
                {
                    MissileCameraHudSnapshot.ResetSmoothing();
                    MissileCameraStockPitchLadder.ResetSourceCache();
                    MissileCameraInfraredPolicy.Reset();
                    MissileCameraInfraredAudit.Reset();
                    MissileCameraInfraredExposure.Reset();
                    MissileCameraEffectsAvailability.Reset();
                    MissileCameraSalvoTracker.Reset();
                }
                catch (Exception ex)
                {
                    MfdLog.Info("teardown statics failed: " + ex.Message);
                }

                if (shutdownDriver)
                {
                    try { MissileCameraFeedDriverHost.Shutdown(); }
                    catch (Exception ex) { MfdLog.Info("teardown driver failed: " + ex.Message); }
                }

                if (unpatchHarmony)
                {
                    _harmony?.UnpatchSelf();
                    _harmony = null;
                }

                _missionReady = false;
                _startupScheduled = false;
            }
            finally
            {
                _teardownInProgress = false;
            }
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
            MissileCameraKeybindConfig.Refresh(force: true);
            MfdLayoutConfig.Refresh(force: true);
            MissileCameraFeedConfig.Refresh(force: true);
            MissileCameraHudConfig.Refresh(force: true);
            MissileCameraControlsConfig.Refresh(force: true);
            MissileCameraFullscreenConfig.Refresh(force: true);
            MissileCameraTelemetryConfig.Refresh(force: true);
            MissileCameraEffectsConfig.Refresh(force: true);
            MissileCameraAircraftCamConfig.Refresh(force: true);

            MissileAccess.WarmFieldCache();
            MissileCameraPostFxStack.ProbeAvailabilityAtStartup();

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
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SafeMissionTeardown("application_quit", shutdownDriver: true, unpatchHarmony: true);
            _instance = null;
        }
    }
}
