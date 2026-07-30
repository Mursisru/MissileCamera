using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace MissileCamera
{
    /// <summary>
    /// Fullscreen: keep only the followed missile's target unit locked on CombatHUD markers.
    /// Snapshots locks on enter, restores on exit. Re-filters on missile switch.
    /// Never calls WeaponManager.TargetListChanged during active filtering (avoids layout teardown).
    /// Never DeselectMarker on Missile units — vanilla friendly missiles use sprite=null when minimized.
    /// </summary>
    internal static class MissileCameraFullscreenTargetLock
    {
        private static readonly FieldInfo? MarkersField =
            AccessTools.Field(typeof(CombatHUD), "markers");

        private static readonly List<Unit> SavedTargets = new List<Unit>(16);
        private static bool _sessionActive;
        private static Unit? _lastFilteredKeep;

        internal static void OnFullscreenEntered()
        {
            SafeRun(() =>
            {
                if (!_sessionActive)
                {
                    CaptureSnapshot();
                    _sessionActive = true;
                    MissileCameraMissionLifecycleDiag.Info(
                        "targetlock enter snapshotCount=" + SavedTargets.Count);
                }

                ApplyFilter();
            }, "enter");
        }

        internal static void OnFullscreenExited()
        {
            SafeRun(() =>
            {
                if (!_sessionActive)
                    return;

                RestoreSnapshot();
                _sessionActive = false;
                _lastFilteredKeep = null;
                MissileCameraMissionLifecycleDiag.Info("targetlock exit restored");
            }, "exit");
        }

        internal static void ResetForMissionUnload()
        {
            // Prefer restore while CombatHUD is still alive — Abandon alone leaves deselected markers.
            // try/finally always clears session so a throw cannot sticky-skip the next CaptureSnapshot.
            try
            {
                SafeRun(() =>
                {
                    if (!_sessionActive)
                    {
                        SavedTargets.Clear();
                        _lastFilteredKeep = null;
                        return;
                    }

                    CombatHUD? hud = null;
                    try { hud = SceneSingleton<CombatHUD>.i; }
                    catch { /* ignore */ }

                    if (hud != null)
                    {
                        // Restore prior selections only — do not Deselect units that were never selected.
                        RestoreSnapshot();
                        MissileCameraMissionLifecycleDiag.Info("targetlock unload restore ok");
                    }
                    else
                    {
                        MissileCameraMissionLifecycleDiag.Warn("targetlock unload no CombatHUD");
                    }
                }, "unload");
            }
            finally
            {
                _sessionActive = false;
                _lastFilteredKeep = null;
                SavedTargets.Clear();
            }
        }

        /// <summary>Clear lock session without restoring targets / TargetListChanged.</summary>
        internal static void AbandonSession()
        {
            _sessionActive = false;
            _lastFilteredKeep = null;
            SavedTargets.Clear();
            MissileCameraMissionLifecycleDiag.Warn("targetlock AbandonSession");
        }

        internal static void OnFollowedMissileChanged()
        {
            if (!_sessionActive || !MissileCameraFullscreenController.IsActive)
                return;

            SafeRun(ApplyFilter, "missile");
        }

        internal static void Maintain()
        {
            if (!_sessionActive || !MissileCameraFullscreenController.IsActive)
                return;

            SafeRun(() =>
            {
                CombatHUD? hud = SceneSingleton<CombatHUD>.i;
                if (hud == null || hud.aircraft == null)
                    return;

                Unit? keep = ResolveMissileTarget();
                if (IsFilterStateValid(hud, keep))
                    return;

                ApplyFilter();
            }, "maintain");
        }

        private static void SafeRun(Action action, string phase)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                MfdLog.Info($"target lock {phase} failed: {ex.Message}");
                MissileCameraMissionLifecycleDiag.Warn(
                    "targetlock " + phase + " failed: " + ex.Message);
            }
        }

        private static void CaptureSnapshot()
        {
            SavedTargets.Clear();

            CombatHUD? hud = SceneSingleton<CombatHUD>.i;
            if (hud == null)
                return;

            var seen = new HashSet<Unit>();
            List<Unit>? list = hud.GetTargetList();
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Unit? unit = list[i];
                    if (unit == null || unit.disabled || !seen.Add(unit))
                        continue;

                    SavedTargets.Add(unit);
                }
            }

            List<HUDUnitMarker>? markers = GetMarkers(hud);
            if (markers == null)
                return;

            for (int i = 0; i < markers.Count; i++)
            {
                HUDUnitMarker? marker = markers[i];
                if (marker?.unit == null || marker.unit.disabled || !marker.selected)
                    continue;

                if (seen.Add(marker.unit))
                    SavedTargets.Add(marker.unit);
            }
        }

        private static void ApplyFilter()
        {
            CombatHUD? hud = SceneSingleton<CombatHUD>.i;
            if (hud == null || hud.aircraft == null)
                return;

            Unit? keep = ResolveMissileTarget();
            if (IsFilterStateValid(hud, keep))
            {
                _lastFilteredKeep = keep;
                return;
            }

            ApplyVisualFilter(hud, keep);
            _lastFilteredKeep = keep;
        }

        private static void ApplyVisualFilter(CombatHUD hud, Unit? keep)
        {
            List<HUDUnitMarker>? markers = GetMarkers(hud);
            if (markers != null)
            {
                for (int i = 0; i < markers.Count; i++)
                {
                    HUDUnitMarker? marker = markers[i];
                    if (marker?.unit == null)
                        continue;

                    // Own/inbound missiles: never Select/Deselect — DeselectMarker → sprite=null for friendly non-Aircraft.
                    if (marker.unit is Missile)
                        continue;

                    bool shouldSelect = keep != null && marker.unit == keep;
                    if (marker.selected == shouldSelect)
                        continue;

                    if (shouldSelect)
                    {
                        if (hud.MarkerExists(marker.unit))
                            marker.SelectMarker();
                    }
                    else
                    {
                        marker.DeselectMarker();
                    }
                }
            }

            try
            {
                hud.SetTargetArrow(false, Vector3.zero, Vector3.zero);
            }
            catch
            {
                // ignore
            }
        }

        private static void RestoreSnapshot()
        {
            CombatHUD? hud = SceneSingleton<CombatHUD>.i;
            if (hud == null || hud.aircraft == null)
            {
                SavedTargets.Clear();
                _lastFilteredKeep = null;
                return;
            }

            // Only undo selections we changed for FS filter. Never blanket-Deselect all markers
            // (friendly missiles / units call UpdateMaximized → sprite=null when minimized).
            List<HUDUnitMarker>? markers = GetMarkers(hud);
            if (markers != null)
            {
                for (int i = 0; i < markers.Count; i++)
                {
                    HUDUnitMarker? marker = markers[i];
                    if (marker?.unit == null)
                        continue;

                    if (marker.unit is Missile)
                        continue;

                    bool shouldSelect = false;
                    for (int s = 0; s < SavedTargets.Count; s++)
                    {
                        if (SavedTargets[s] == marker.unit)
                        {
                            shouldSelect = true;
                            break;
                        }
                    }

                    if (marker.selected == shouldSelect)
                        continue;

                    if (shouldSelect)
                    {
                        if (hud.MarkerExists(marker.unit))
                            marker.SelectMarker();
                    }
                    else if (marker.selected)
                    {
                        marker.DeselectMarker();
                    }
                }
            }

            SavedTargets.Clear();
            _lastFilteredKeep = null;
        }

        private static Unit? ResolveMissileTarget()
        {
            Missile? missile = MissileCameraFeedController.TryGetFollowedMissile();
            if (missile == null)
                return null;

            return MissileAccess.TryGetTarget(missile, out Unit? target) ? target : null;
        }

        private static bool IsFilterStateValid(CombatHUD hud, Unit? keep)
        {
            if (!ReferenceEquals(_lastFilteredKeep, keep))
                return false;

            // Markers only — do not require GetTargetList() shape (we no longer mutate it).
            if (keep != null && hud.MarkerExists(keep))
            {
                if (!hud.TryGetMarker(keep, out HUDUnitMarker keepMarker) || keepMarker == null || !keepMarker.selected)
                    return false;
            }

            return !AnyOtherSelectedMarker(hud, keep);
        }

        private static bool AnyOtherSelectedMarker(CombatHUD hud, Unit? keep)
        {
            List<HUDUnitMarker>? markers = GetMarkers(hud);
            if (markers == null)
                return false;

            for (int i = 0; i < markers.Count; i++)
            {
                HUDUnitMarker? marker = markers[i];
                if (marker == null || !marker.selected || marker.unit == null)
                    continue;

                if (marker.unit is Missile)
                    continue;

                if (keep == null || marker.unit != keep)
                    return true;
            }

            return false;
        }

        private static List<HUDUnitMarker>? GetMarkers(CombatHUD hud)
        {
            if (MarkersField == null)
                return null;

            return MarkersField.GetValue(hud) as List<HUDUnitMarker>;
        }
    }
}
