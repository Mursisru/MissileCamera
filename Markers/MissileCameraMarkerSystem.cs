using System.Collections.Generic;
using UnityEngine;

namespace MissileCamera
{
    /// <summary>
    /// Typed markers from HudSnapshot SSOT + detected UnitRegistry markers + inbound missiles.
    /// </summary>
    internal sealed class MissileCameraMarkerSystem
    {
        private const float MotionVectorSeconds = 1.5f;
        private const float InboundBlinkHz = 3f;

        private readonly MissileCameraMarkerPool _pool = new MissileCameraMarkerPool();
        private readonly List<MissileCameraMarkerData> _scratch = new List<MissileCameraMarkerData>(64);
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

            BuildFromSnapshot(snapshot, seekerMissile, _scratch);
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

            // Red ↔ yellow blink for missiles targeting our seeker.
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
            List<MissileCameraMarkerData> into)
        {
            into.Clear();
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
            }

            if (MissileCameraMarkersConfig.ShowAim && snapshot.HasAimPoint)
            {
                into.Add(new MissileCameraMarkerData(
                    MissileCameraMarkerType.Aim,
                    snapshot.AimPoint,
                    MissileCameraMarkersConfig.AimColor,
                    showLabel: true));
            }

            // Inbound missiles at our seeker (always — threat awareness).
            if (seekerMissile != null)
                AppendInboundMissiles(seekerMissile, into);

            if (!MissileCameraMarkersConfig.ShowSceneUnits)
                return;

            FactionHQ? ownHq = UnitRegistryAccess.ResolveOwnHq(seekerMissile);
            if (ownHq == null)
                return;

            float alpha = MissileCameraMarkersConfig.SceneUnitAlpha;
            Color ally = MissileCameraMarkersConfig.AllyColor;
            ally.a = alpha;
            Color threat = MissileCameraMarkersConfig.ThreatColor;
            threat.a = alpha;

            List<Unit> units = UnitRegistry.allUnits;
            for (int i = 0; i < units.Count; i++)
            {
                if (into.Count >= MissileCameraMarkersConfig.MaxMarkers)
                    break;

                Unit unit = units[i];
                if (!UnitRegistryAccess.IsUsableMarkerUnit(unit, seekerMissile, locked))
                    continue;

                // Skip inbound missiles already drawn with blink style.
                if (unit is Missile inboundCheck
                    && seekerMissile != null
                    && UnitRegistryAccess.IsInboundAtSeeker(inboundCheck, seekerMissile))
                    continue;

                if (!UnitRegistryAccess.TryGetMapVisibleMarkerPose(
                        unit, ownHq, out GlobalPosition knownPos, out bool isAlly))
                    continue;

                Vector3 vel = UnitRegistryAccess.TryGetUnitVelocity(unit);
                into.Add(new MissileCameraMarkerData(
                    isAlly ? MissileCameraMarkerType.Ally : MissileCameraMarkerType.Threat,
                    knownPos,
                    isAlly ? ally : threat,
                    showLabel: false,
                    velocityWorld: vel));
            }
        }

        private static void AppendInboundMissiles(Missile seeker, List<MissileCameraMarkerData> into)
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
