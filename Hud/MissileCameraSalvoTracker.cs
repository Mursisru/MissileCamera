using System.Collections.Generic;
using UnityEngine;

namespace MissileCamera
{
    internal static class MissileCameraSalvoTracker
    {
        private static readonly List<Missile> CurrentBurst = new List<Missile>();
        private static float _lastRegisterTimeUnscaled = -999f;

        internal static void Reset()
        {
            CurrentBurst.Clear();
            _lastRegisterTimeUnscaled = -999f;
        }

        internal static void OnRegister(Missile missile)
        {
            if (missile == null)
                return;

            MissileCameraHudConfig.Refresh();
            float now = Time.unscaledTime;
            if (now - _lastRegisterTimeUnscaled > MissileCameraHudConfig.SalvoWindowSeconds)
            {
                PruneBurst();
                // Start a new burst only after the previous wave is fully gone.
            }

            _lastRegisterTimeUnscaled = now;
            if (!CurrentBurst.Contains(missile))
                CurrentBurst.Add(missile);
        }

        internal static void OnDeregister(Missile missile)
        {
            if (missile == null)
                return;

            CurrentBurst.Remove(missile);
        }

        internal static void GetSalvoInfo(
            Missile missile,
            IReadOnlyList<Missile>? ownedActive,
            out int index,
            out int total)
        {
            index = 1;
            total = 1;
            if (missile == null)
                return;

            if (ownedActive != null && TryGetSalvoFromList(missile, ownedActive, out index, out total))
                return;

            PruneBurst();
            if (CurrentBurst.Count == 0)
                return;

            TryGetSalvoFromList(missile, CurrentBurst, out index, out total);
        }

        private static bool TryGetSalvoFromList(
            Missile missile,
            IReadOnlyList<Missile> missiles,
            out int index,
            out int total)
        {
            index = 1;
            total = 0;
            float myAge = missile.timeSinceSpawn;

            for (int i = 0; i < missiles.Count; i++)
            {
                Missile candidate = missiles[i];
                if (!IsTrackable(candidate))
                    continue;

                total++;
                if (candidate != missile && candidate.timeSinceSpawn > myAge)
                    index++;
            }

            if (total <= 0)
                return false;

            total = Mathf.Max(1, total);
            return true;
        }

        private static void PruneBurst()
        {
            for (int i = CurrentBurst.Count - 1; i >= 0; i--)
            {
                Missile missile = CurrentBurst[i];
                if (missile == null || missile.disabled || missile.rb == null)
                    CurrentBurst.RemoveAt(i);
            }
        }

        private static bool IsTrackable(Missile missile) =>
            missile != null && !missile.disabled && missile.rb != null;
    }
}
