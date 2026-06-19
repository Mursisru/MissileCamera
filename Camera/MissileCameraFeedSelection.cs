using System.Collections.Generic;
using UnityEngine;

namespace MissileCamera
{
    internal static class MissileCameraFeedSelection
    {
        private static readonly List<Missile> Scratch = new List<Missile>();

        internal static void BuildOrderedTrackable(IReadOnlyList<Missile> owned, List<Missile> into)
        {
            into.Clear();
            for (int i = 0; i < owned.Count; i++)
            {
                Missile missile = owned[i];
                if (IsTrackable(missile))
                    into.Add(missile);
            }

            SortOldestFirst(into);
        }

        internal static Missile? CycleCurrent(IReadOnlyList<Missile> owned, Missile? current, int direction)
        {
            BuildOrderedTrackable(owned, Scratch);
            if (Scratch.Count == 0)
                return null;

            int index = FindIndex(Scratch, current);
            if (index < 0)
                index = Scratch.Count - 1;

            index += direction;
            int count = Scratch.Count;
            index = (index % count + count) % count;

            return Scratch[index];
        }

        internal static Missile? ResolveFallbackNewest(IReadOnlyList<Missile> owned)
        {
            BuildOrderedTrackable(owned, Scratch);
            if (Scratch.Count == 0)
                return null;

            return Scratch[Scratch.Count - 1];
        }

        internal static bool IsStillTrackable(Missile? missile) =>
            missile != null && IsTrackable(missile);

        private static int FindIndex(List<Missile> ordered, Missile? missile)
        {
            if (missile == null)
                return -1;

            for (int i = 0; i < ordered.Count; i++)
            {
                if (ordered[i] == missile)
                    return i;
            }

            return -1;
        }

        private static void SortOldestFirst(List<Missile> missiles)
        {
            for (int i = 1; i < missiles.Count; i++)
            {
                Missile key = missiles[i];
                float keyAge = key.timeSinceSpawn;
                int j = i - 1;
                while (j >= 0 && missiles[j].timeSinceSpawn < keyAge)
                {
                    missiles[j + 1] = missiles[j];
                    j--;
                }

                missiles[j + 1] = key;
            }
        }

        private static bool IsTrackable(Missile missile) =>
            missile != null && !missile.disabled && missile.rb != null;
    }
}
