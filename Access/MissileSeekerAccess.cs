using System.Reflection;

namespace MissileCamera
{
    /// <summary>Seeker reflection — FieldInfo cached once.</summary>
    internal static class MissileSeekerAccess
    {
        private const BindingFlags InstanceAny = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static FieldInfo? _seekerField;
        private static bool _resolved;

        internal static void EnsureFields()
        {
            if (_resolved)
                return;

            _resolved = true;
            _seekerField = typeof(Missile).GetField("seeker", InstanceAny);
        }

        internal static bool TryGetSeeker(Missile missile, out MissileSeeker? seeker)
        {
            seeker = null;
            if (missile == null)
                return false;

            EnsureFields();
            if (_seekerField == null)
                return false;

            seeker = _seekerField.GetValue(missile) as MissileSeeker;
            return seeker != null;
        }

        internal static MissileGuidanceStatus ResolveGuidance(Missile missile)
        {
            if (missile == null)
                return MissileGuidanceStatus.Ballistic;

            EnsureFields();
            bool hasSeeker = TryGetSeeker(missile, out MissileSeeker? seeker) && seeker != null;
            if (!hasSeeker)
                return MissileGuidanceStatus.Ballistic;

            bool hasTarget = MissileAccess.TryGetTarget(missile, out _);
            bool hasAim = MissileAccess.TryGetAimPoint(missile, out _);

            if (!hasTarget && !hasAim)
                return MissileGuidanceStatus.LostLock;

            // activeSearch without target ≈ lost / searching
            try
            {
                if (missile.seekerMode == Missile.SeekerMode.activeSearch && !hasTarget)
                    return MissileGuidanceStatus.LostLock;
            }
            catch
            {
                // ignore
            }

            return MissileGuidanceStatus.Guided;
        }
    }
}
