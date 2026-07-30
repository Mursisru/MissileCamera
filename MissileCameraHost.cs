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
        private int _startupEpoch;
        private bool _pendingHardReset;
        private string _pendingHardResetReason = string.Empty;
        private bool _pendingShutdownDriver;
        private bool _pendingUnpatchHarmony;

        internal static bool IsMissionReady => _instance != null && _instance._missionReady;

        /// <summary>True only while a mission session may apply layout/hide. Gates DDOL Tick + Harmony apply.</summary>
        internal static bool IsSessionActive =>
            _instance != null
            && _instance._missionReady
            && !_instance._teardownInProgress;

        internal static bool IsTeardownInProgress =>
            _instance != null && _instance._teardownInProgress;

        internal static int TeardownEpoch => _instance != null ? _instance._teardownEpoch : 0;

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
            MissileCameraMissionLifecycleDiag.Init(pluginDir);
            SceneManager.sceneLoaded += _instance.OnSceneLoaded;
            SceneManager.sceneUnloaded += _instance.OnSceneUnloaded;
            _instance.TryBootstrapCurrentScene();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            _pendingHardReset = false;
            HardResetAll("host_destroy", shutdownDriver: true, unpatchHarmony: true);
            _instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            string label = string.IsNullOrEmpty(scene.path) ? scene.name : scene.path;
            MissileCameraMissionLifecycleDiag.Info(
                "sceneLoaded mode=" + mode + " path=" + label);

            if (IsMenuOrSystemScene(scene.path))
            {
                _startupEpoch++;
                _startupScheduled = false;
                HardResetAll("scene_loaded:" + label);
                MissileCameraMissionLifecycleDiag.Snapshot("after_menu_reset");
                return;
            }

            // Only GameWorld counts as a mission load. Ignore additive/UI scenes mid-sortie
            // (those were wiping MC overlay + marker lock while the player was still flying).
            if (!IsGameWorldScene(scene))
            {
                MissileCameraMissionLifecycleDiag.Info("sceneLoaded ignored (not GameWorld) path=" + label);
                return;
            }

            _startupEpoch++;
            _startupScheduled = false;
            HardResetAll("scene_loaded:" + label);
            ScheduleMissionStartup(scene.path);
            MissileCameraMissionLifecycleDiag.Snapshot("after_gameworld_load");
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (!IsGameWorldScene(scene))
                return;

            MissileCameraMissionLifecycleDiag.Info("sceneUnloaded GameWorld");
            HardResetAll("scene_unloaded_gameworld");
            MissileCameraMissionLifecycleDiag.Snapshot("after_gameworld_unload");
        }

        /// <summary>
        /// Full session wipe. Safe to call repeatedly. Never EnableCanvas / TargetListChanged.
        /// If already wiping, queues a follow-up pass (never silent-drop a restore request).
        /// </summary>
        private void HardResetAll(string reason, bool shutdownDriver = false, bool unpatchHarmony = false)
        {
            if (_teardownInProgress)
            {
                _pendingHardReset = true;
                _pendingHardResetReason = reason;
                _pendingShutdownDriver |= shutdownDriver;
                _pendingUnpatchHarmony |= unpatchHarmony;
                MfdLog.Info("HARD RESET queued reason=" + reason);
                MissileCameraMissionLifecycleDiag.Warn(
                    "HARD RESET queued reason=" + reason
                    + " pendingShutdown=" + _pendingShutdownDriver
                    + " pendingUnpatch=" + _pendingUnpatchHarmony);
                return;
            }

            _teardownInProgress = true;
            _teardownEpoch++;
            // Drop mission gate first so DDOL Tick cannot ApplyHidden during this method.
            _missionReady = false;
            _startupScheduled = false;

            try
            {
                MfdLog.Info($"HARD RESET epoch={_teardownEpoch} reason={reason}");
                MissileCameraMissionLifecycleDiag.Info(
                    "HARD RESET begin epoch=" + _teardownEpoch
                    + " reason=" + reason
                    + " shutdownDriver=" + shutdownDriver);

                try { MissileCameraFullscreenController.ResetForMissionUnload(); }
                catch (Exception ex) { LogHardResetFail("FS", ex); }

                try { MissileCameraVanillaHudBridge.ResetForMissionUnload(); }
                catch (Exception ex) { LogHardResetFail("vanilla hud", ex); }

                try { MissileCameraFullscreenTargetLock.ResetForMissionUnload(); }
                catch (Exception ex) { LogHardResetFail("targetlock", ex); }

                try { MfdLayoutRetryHost.HardReset(); }
                catch (Exception ex) { LogHardResetFail("retry", ex); }

                try { MfdLayoutController.HardResetForMissionUnload(); }
                catch (Exception ex) { LogHardResetFail("layout", ex); }

                try { MfdWeaponsZoneAccess.HardResetForMissionUnload(); }
                catch (Exception ex) { LogHardResetFail("weapons", ex); }

                try { MissileCameraFeedController.HardResetForMissionUnload(); }
                catch (Exception ex) { LogHardResetFail("feed", ex); }

                try { MissileCameraRenderPrep.ResetAll(); }
                catch (Exception ex) { LogHardResetFail("render", ex); }

                try { TacScreenAccess.ClearCache(); }
                catch (Exception ex) { LogHardResetFail("tac cache", ex); }

                try { MissileCameraVanillaHudBridge.ResetForMissionUnload(); }
                catch (Exception ex) { LogHardResetFail("vanilla hud pass2", ex); }

                try { MfdWeaponsZoneAccess.HardResetForMissionUnload(); }
                catch (Exception ex) { LogHardResetFail("weapons pass2", ex); }

                try
                {
                    MissileCameraControlSlot.Active = null;
                    MissileCameraCombatHudMarkerProjection.ResetCache();
                    MissileCameraHudSnapshot.ResetSmoothing();
                    MissileCameraStockPitchLadder.ResetSourceCache();
                    MissileCameraInfraredPolicy.Reset();
                    MissileCameraInfraredAudit.Reset();
                    MissileCameraInfraredExposure.Reset();
                    MissileCameraEffectsAvailability.Reset();
                    MissileCameraSalvoTracker.Reset();
                    MissileCameraLossInterference.Shutdown();
                    MissileCameraAircraftCamController.Shutdown();
                    MissileCameraCockpitPipController.Shutdown();
                    HudFontHelper.Reset();
                    HudBackdropHelper.Reset();
                }
                catch (Exception ex)
                {
                    LogHardResetFail("statics", ex);
                }

                if (shutdownDriver)
                {
                    try { MissileCameraFeedDriverHost.Shutdown(); }
                    catch (Exception ex) { LogHardResetFail("driver", ex); }
                }

                if (unpatchHarmony)
                {
                    _harmony?.UnpatchSelf();
                    _harmony = null;
                }
            }
            finally
            {
                _teardownInProgress = false;
                if (_pendingHardReset && _instance != null)
                {
                    string pendingReason = _pendingHardResetReason;
                    bool pendingShutdown = _pendingShutdownDriver;
                    bool pendingUnpatch = _pendingUnpatchHarmony;
                    _pendingHardReset = false;
                    _pendingHardResetReason = string.Empty;
                    _pendingShutdownDriver = false;
                    _pendingUnpatchHarmony = false;
                    MissileCameraMissionLifecycleDiag.Warn(
                        "HARD RESET defer follow-up reason=" + pendingReason);
                    StartCoroutine(DeferredHardReset(pendingReason, pendingShutdown, pendingUnpatch));
                }
                else
                {
                    // Pending wipe after startup can leave _missionReady=false with no ScheduleMissionStartup.
                    TryRescheduleMissionStartupAfterWipe(reason, shutdownDriver);
                }

                MissileCameraMissionLifecycleDiag.Snapshot("hardreset_end:" + reason);
            }
        }

        /// <summary>
        /// If a follow-up HardReset killed the session while GameWorld is still loaded,
        /// schedule startup again so EnsureLayout/NotifyOverlayReady are not permanently gated off.
        /// </summary>
        private void TryRescheduleMissionStartupAfterWipe(string reason, bool shutdownDriver)
        {
            if (shutdownDriver || _instance == null)
                return;

            if (_missionReady || _startupScheduled || _pendingHardReset || _teardownInProgress)
                return;

            // Caller (DeferredMissionStartup) continues and will set _missionReady itself.
            if (string.Equals(reason, "pre_mission_startup", StringComparison.Ordinal))
                return;

            // Leaving the mission — menu load will schedule properly. Do not resurrect on a dying GameWorld.
            if (string.Equals(reason, "scene_unloaded_gameworld", StringComparison.Ordinal)
                || string.Equals(reason, "application_quit", StringComparison.Ordinal)
                || string.Equals(reason, "host_destroy", StringComparison.Ordinal))
                return;

            Scene scene = SceneManager.GetActiveScene();
            string scenePath = scene.path;
            if (IsMenuOrSystemScene(scenePath) || !IsGameWorldScene(scene))
            {
                MissileCameraMissionLifecycleDiag.Info(
                    "TryReschedule skip (not live GameWorld) reason=" + reason
                    + " scene=" + (string.IsNullOrEmpty(scenePath) ? scene.name : scenePath));
                return;
            }

            MissileCameraMissionLifecycleDiag.Warn(
                "TryReschedule MissionStartup after wipe reason=" + reason
                + " startupEpoch=" + _startupEpoch);
            ScheduleMissionStartup(string.IsNullOrEmpty(scenePath) ? scene.name : scenePath);
        }

        private static void LogHardResetFail(string step, Exception ex)
        {
            string msg = string.IsNullOrEmpty(ex.Message) ? "(no message)" : ex.Message;
            MfdLog.Info("hardreset " + step + " failed: " + ex.GetType().Name + ": " + msg);
            MissileCameraMissionLifecycleDiag.Warn(
                "hardreset step failed step=" + step
                + " ex=" + ex.GetType().Name + ": " + msg);
        }

        private IEnumerator DeferredHardReset(string reason, bool shutdownDriver, bool unpatchHarmony)
        {
            yield return null;
            if (_instance == null)
                yield break;

            MissileCameraMissionLifecycleDiag.Info("DeferredHardReset run reason=" + reason);
            HardResetAll(reason, shutdownDriver, unpatchHarmony);
        }

        private void TryBootstrapCurrentScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            _startupEpoch++;
            _startupScheduled = false;
            HardResetAll("bootstrap_current:" + (string.IsNullOrEmpty(scene.path) ? scene.name : scene.path));
            if (!IsMenuOrSystemScene(scene.path))
                ScheduleMissionStartup(scene.path);
        }

        private void ScheduleMissionStartup(string scenePath)
        {
            if (_missionReady || _startupScheduled)
            {
                MissileCameraMissionLifecycleDiag.Info(
                    "ScheduleMissionStartup skip ready=" + _missionReady
                    + " scheduled=" + _startupScheduled
                    + " scene=" + scenePath);
                return;
            }

            _startupScheduled = true;
            int epoch = _startupEpoch;
            MissileCameraMissionLifecycleDiag.Info(
                "ScheduleMissionStartup epoch=" + epoch + " scene=" + scenePath);
            StartCoroutine(DeferredMissionStartup(scenePath, epoch));
        }

        private IEnumerator DeferredMissionStartup(string scenePath, int epoch)
        {
            yield return null;
            if (epoch != _startupEpoch || _missionReady)
            {
                _startupScheduled = false;
                MissileCameraMissionLifecycleDiag.Info(
                    "DeferredStartup abort early epoch=" + epoch
                    + " cur=" + _startupEpoch
                    + " ready=" + _missionReady);
                yield break;
            }

            if (IsMenuOrSystemScene(SceneManager.GetActiveScene().path))
            {
                _startupScheduled = false;
                MissileCameraMissionLifecycleDiag.Info("DeferredStartup abort menu");
                yield break;
            }

            // Always wipe again immediately before enabling the session.
            HardResetAll("pre_mission_startup");
            yield return null;

            // Absorb follow-up wipe queued during pre_mission_startup (same race that killed session).
            while (_pendingHardReset || _teardownInProgress)
                yield return null;

            yield return null;

            if (epoch != _startupEpoch)
            {
                _startupScheduled = false;
                MissileCameraMissionLifecycleDiag.Warn(
                    "DeferredStartup abort epoch drift epoch=" + epoch + " cur=" + _startupEpoch);
                yield break;
            }

            if (IsMenuOrSystemScene(SceneManager.GetActiveScene().path))
            {
                _startupScheduled = false;
                MissileCameraMissionLifecycleDiag.Info("DeferredStartup abort menu after wipe");
                yield break;
            }

            // A deferred HardReset may have cleared ready after we planned to enable — re-enable now.
            _missionReady = true;
            _startupScheduled = false;
            MissileCameraMissionLifecycleDiag.Info(
                "DeferredStartup ENABLE missionReady scene=" + scenePath
                + " epoch=" + epoch);
            StartupMission(_pluginDir, _logger!, scenePath);
            MissileCameraMissionLifecycleDiag.Snapshot("host_ready");
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

            try
            {
                MissileCameraCombatHudMarkerProjection.RestoreMarkerImages();
                MissileCameraVanillaHudBridge.ForceCombatHudMarkerPass();
                MissileCameraVanillaHudBridge.DiagLogMissileMarkers("host_ready");
            }
            catch (Exception ex)
            {
                MissileCameraMissionLifecycleDiag.Warn("host_ready marker heal failed: " + ex.Message);
            }

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

        private static bool IsGameWorldScene(Scene scene)
        {
            if (scene.path.IndexOf("GameWorld", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return string.Equals(scene.name, "GameWorld", StringComparison.OrdinalIgnoreCase);
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
            _pendingHardReset = false;
            HardResetAll("application_quit", shutdownDriver: true, unpatchHarmony: true);
            _instance = null;
        }
    }
}
