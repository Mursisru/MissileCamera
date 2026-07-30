using System.Collections.Generic;
using UnityEngine;

namespace MissileCamera
{
    /// <summary>
    /// Markers from HudSnapshot + datalink map set (trackingDatabase / factionUnits) + inbound missiles.
    /// Datalink states: Tracked (live+vector), Lost (last-known, no vector), Friendly (live).
    /// </summary>
    internal sealed class MissileCameraMarkerSystem
    {
        private const float MotionVectorSeconds = 1.5f;
        private const float InboundBlinkHz = 3f;
        private const float LostAlphaScale = 0.55f;

        private readonly MissileCameraMarkerPool _pool = new MissileCameraMarkerPool();
        private readonly List<MissileCameraMarkerData> _scratch = new List<MissileCameraMarkerData>(64);
        private readonly HashSet<uint> _seenIds = new HashSet<uint>();
        private RectTransform? _root;

        internal void EnsureBuilt(RectTransform hudRoot)
        {
            Transform? existing = hudRoot.Find("MissileCameraHudMarkers");
            RectTransform root;
            if (existing != null && existing.TryGetComponent(out RectTransform existingRt))
            {
                root = existingRt;
            }
            else
            {
                var go = new GameObject("MissileCameraHudMarkers", typeof(RectTransform));
                go.transform.SetParent(hudRoot, false);
                root = go.GetComponent<RectTransform>();
                root.anchorMin = Vector2.zero;
                root.anchorMax = Vector2.one;
                root.offsetMin = Vector2.zero;
                root.offsetMax = Vector2.zero;
            }

            _root = root;
            MissileCameraMarkersConfig.Refresh();
            _pool.Ensure(root, MissileCameraMarkersConfig.MaxMarkers);
        }

        internal void Update(
            MissileCameraHudSnapshot snapshot,
            RectTransform viewRt,
            Camera? feedCamera,
            float panelMinSide,
            Missile? seekerMissile)
        {
            if (_root == null || feedCamera == null)
                return;

            MissileCameraMarkersConfig.Refresh();
            _pool.Ensure(_root, MissileCameraMarkersConfig.MaxMarkers);
            _pool.ReleaseAll();

            BuildFromSnapshot(snapshot, seekerMissile, _scratch, _seenIds);
            int count = Mathf.Min(_scratch.Count, MissileCameraMarkersConfig.MaxMarkers);
            for (int i = 0; i < count; i++)
            {
                MissileCameraMarkerData data = _scratch[i];
                if (!data.Valid)
                    continue;

                MissileCameraMarkerView? view = _pool.Rent();
                if (view == null)
                    break;

                FeedProjection projection = FeedScreenProjector.Project(feedCamera, viewRt, data.WorldPosition);
                string? label = data.ShowLabel ? ResolveLabel(data.Type, snapshot) : null;
                bool diamond = ResolveDiamondShape(data.Type);
                Color color = ResolveBlinkColor(data);

                Vector2 motionTip = Vector2.zero;
                bool hasMotion = false;
                if (data.VelocityWorld.sqrMagnitude > 1f)
                {
                    Vector3 tipWorld = data.WorldPosition.ToLocalPosition()
                        + data.VelocityWorld * MotionVectorSeconds;
                    FeedProjection tipProj = FeedScreenProjector.Project(feedCamera, viewRt, tipWorld);
                    if (tipProj.Valid && tipProj.InFront)
                    {
                        motionTip = tipProj.AnchoredPosition;
                        hasMotion = true;
                    }
                }

                view.Show(projection, panelMinSide, color, diamond, label, hasMotion, motionTip);
            }
        }

        private static Color ResolveBlinkColor(MissileCameraMarkerData data)
        {
            if (data.Type != MissileCameraMarkerType.InboundMissile)
                return data.Color;

            float phase = Mathf.Repeat(Time.unscaledTime * InboundBlinkHz, 1f);
            return phase < 0.5f
                ? new Color(1f, 0.12f, 0.08f, 1f)
                : new Color(1f, 0.92f, 0.15f, 1f);
        }

        private static string? ResolveLabel(MissileCameraMarkerType type, MissileCameraHudSnapshot snapshot)
        {
            switch (type)
            {
                case MissileCameraMarkerType.Target:
                    return snapshot.HasTarget && !string.IsNullOrEmpty(snapshot.TargetName)
                        ? snapshot.TargetName
                        : "TGT";
                case MissileCameraMarkerType.Aim:
                    return "AIM";
                default:
                    return null;
            }
        }

        private static bool ResolveDiamondShape(MissileCameraMarkerType type)
        {
            return type == MissileCameraMarkerType.Target
                || type == MissileCameraMarkerType.Threat
                || type == MissileCameraMarkerType.Waypoint
                || type == MissileCameraMarkerType.InboundMissile;
        }

        internal void Destroy()
        {
            _pool.Clear();
            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
                _root = null;
            }
        }

        internal static void BuildFromSnapshot(
            MissileCameraHudSnapshot snapshot,
            Missile? seekerMissile,
            List<MissileCameraMarkerData> into,
            HashSet<uint> seenIds)
        {
            into.Clear();
            seenIds.Clear();
            if (!snapshot.HasFeed)
                return;

            Unit? locked = null;
            if (seekerMissile != null)
                MissileAccess.TryGetTarget(seekerMissile, out locked);

            if (MissileCameraMarkersConfig.ShowTarget
                && MissileCameraHudConfig.ShowTargetMarker
                && snapshot.HasTarget)
            {
                Vector3 tgtVel = locked != null
                    ? UnitRegistryAccess.TryGetUnitVelocity(locked)
                    : Vector3.zero;
                into.Add(new MissileCameraMarkerData(
                    MissileCameraMarkerType.Target,
                    snapshot.TargetPosition,
                    MissileCameraMarkersConfig.TargetColor,
                    showLabel: true,
                    velocityWorld: tgtVel));
                if (locked != null)
                    seenIds.Add(locked.persistentID.Id);
            }

            if (MissileCameraMarkersConfig.ShowAim && snapshot.HasAimPoint)
            {
                into.Add(new MissileCameraMarkerData(
                    MissileCameraMarkerType.Aim,
                    snapshot.AimPoint,
                    MissileCameraMarkersConfig.AimColor,
                    showLabel: true));
            }

            if (seekerMissile != null)
                AppendInboundMissiles(seekerMissile, into, seenIds);

            if (!MissileCameraMarkersConfig.ShowSceneUnits)
                return;

            FactionHQ? ownHq = UnitRegistryAccess.ResolveOwnHq(seekerMissile);
            if (ownHq == null)
                return;

            AppendTrackingDatabase(ownHq, seekerMissile, locked, into, seenIds);
            AppendFactionUnits(ownHq, seekerMissile, locked, into, seenIds);
        }

        /// <summary>
        /// Same set as DynamicMap: HQ.trackingDatabase.
        /// Tracked = Observed (live+vector); Lost = frozen lastKnown (no vector).
        /// </summary>
        private static void AppendTrackingDatabase(
            FactionHQ ownHq,
            Missile? seekerMissile,
            Unit? locked,
            List<MissileCameraMarkerData> into,
            HashSet<uint> seenIds)
        {
            float alpha = MissileCameraMarkersConfig.SceneUnitAlpha;
            Color threatLive = MissileCameraMarkersConfig.ThreatColor;
            threatLive.a = alpha;
            Color threatLost = MissileCameraMarkersConfig.ThreatColor;
            threatLost.a = alpha * LostAlphaScale;
            Color ally = MissileCameraMarkersConfig.AllyColor;
            ally.a = alpha;

            foreach (KeyValuePair<PersistentID, TrackingInfo> pair in ownHq.trackingDatabase)
            {
                if (into.Count >= MissileCameraMarkersConfig.MaxMarkers)
                    break;

                PersistentID id = pair.Key;
                if (!id.IsValid || seenIds.Contains(id.Id))
                    continue;

                TrackingInfo info = pair.Value;
                if (!UnitRegistryAccess.TryResolveDatalinkContact(
                        info,
                        seekerMissile,
                        locked,
                        ownHq,
                        out Unit? unit,
                        out GlobalPosition pose,
                        out UnitDatalinkState state,
                        out Vector3 velocity))
                    continue;

                if (unit is Missile inbound
                    && seekerMissile != null
                    && UnitRegistryAccess.IsInboundAtSeeker(inbound, seekerMissile))
                    continue;

                MissileCameraMarkerType type;
                Color color;
                switch (state)
                {
                    case UnitDatalinkState.Friendly:
                        type = MissileCameraMarkerType.Ally;
                        color = ally;
                        break;
                    case UnitDatalinkState.Lost:
                        type = MissileCameraMarkerType.Threat;
                        color = threatLost;
                        velocity = Vector3.zero;
                        break;
                    default:
                        type = MissileCameraMarkerType.Threat;
                        color = threatLive;
                        break;
                }

                into.Add(new MissileCameraMarkerData(type, pose, color, showLabel: false, velocityWorld: velocity));
                seenIds.Add(id.Id);
            }
        }

        /// <summary>Same set as DynamicMap: HQ.factionUnits (friendly always live).</summary>
        private static void AppendFactionUnits(
            FactionHQ ownHq,
            Missile? seekerMissile,
            Unit? locked,
            List<MissileCameraMarkerData> into,
            HashSet<uint> seenIds)
        {
            float alpha = MissileCameraMarkersConfig.SceneUnitAlpha;
            Color ally = MissileCameraMarkersConfig.AllyColor;
            ally.a = alpha;

            for (int i = 0; i < ownHq.factionUnits.Count; i++)
            {
                if (into.Count >= MissileCameraMarkersConfig.MaxMarkers)
                    break;

                PersistentID id = ownHq.factionUnits[i];
                if (!id.IsValid || seenIds.Contains(id.Id))
                    continue;

                if (!UnitRegistry.TryGetUnit(new PersistentID?(id), out Unit unit) || unit == null)
                    continue;

                if (!UnitRegistryAccess.TryResolveFriendlyUnit(
                        unit, seekerMissile, locked, out GlobalPosition pose, out Vector3 velocity))
                    continue;

                into.Add(new MissileCameraMarkerData(
                    MissileCameraMarkerType.Ally,
                    pose,
                    ally,
                    showLabel: false,
                    velocityWorld: velocity));
                seenIds.Add(id.Id);
            }
        }

        private static void AppendInboundMissiles(
            Missile seeker,
            List<MissileCameraMarkerData> into,
            HashSet<uint> seenIds)
        {
            List<Unit> units = UnitRegistry.allUnits;
            for (int i = 0; i < units.Count; i++)
            {
                if (into.Count >= MissileCameraMarkersConfig.MaxMarkers)
                    break;

                if (units[i] is not Missile inbound || inbound.disabled)
                    continue;

                if (!UnitRegistryAccess.IsInboundAtSeeker(inbound, seeker))
                    continue;

                PersistentID id = inbound.persistentID;
                if (id.IsValid)
                    seenIds.Add(id.Id);

                into.Add(new MissileCameraMarkerData(
                    MissileCameraMarkerType.InboundMissile,
                    inbound.GlobalPosition(),
                    new Color(1f, 0.12f, 0.08f, 1f),
                    showLabel: false,
                    velocityWorld: UnitRegistryAccess.TryGetUnitVelocity(inbound)));
            }
        }
    }
}
