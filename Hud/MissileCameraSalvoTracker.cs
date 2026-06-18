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
                CurrentBurst.Clear();

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

        internal static void GetSalvoInfo(Missile missile, out int index, out int total)
        {
            index = 1;
            total = 1;
            if (missile == null)
                return;

            PruneBurst();
            if (CurrentBurst.Count == 0)
                return;

            var ordered = new List<Missile>(CurrentBurst.Count);
            for (int i = 0; i < CurrentBurst.Count; i++)
            {
                Missile candidate = CurrentBurst[i];
                if (IsTrackable(candidate))
                    ordered.Add(candidate);
            }

            ordered.Sort((a, b) => b.timeSinceSpawn.CompareTo(a.timeSinceSpawn));
            total = Mathf.Max(1, ordered.Count);

            for (int i = 0; i < ordered.Count; i++)
            {
                if (ordered[i] == missile)
                {
                    index = i + 1;
                    return;
                }
            }

            index = 1;
            total = Mathf.Max(1, ordered.Count);
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
