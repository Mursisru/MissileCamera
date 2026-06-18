using System.Reflection;
using UnityEngine;

namespace MissileCamera
{
    internal readonly struct MissileCameraNoseResolveResult
    {
        internal MissileCameraNoseResolveResult(
            float cameraLocalZ,
            float noseLocalZ,
            float meshMaxLocalZ,
            float colliderMaxLocalZ,
            float minLocalZ,
            string pivotMode,
            string source)
        {
            CameraLocalZ = cameraLocalZ;
            NoseLocalZ = noseLocalZ;
            MeshMaxLocalZ = meshMaxLocalZ;
            ColliderMaxLocalZ = colliderMaxLocalZ;
            MinLocalZ = minLocalZ;
            PivotMode = pivotMode;
            Source = source;
        }

        internal float CameraLocalZ { get; }
        internal float NoseLocalZ { get; }
        internal float MeshMaxLocalZ { get; }
        internal float ColliderMaxLocalZ { get; }
        internal float MinLocalZ { get; }
        internal string PivotMode { get; }
        internal string Source { get; }
    }

    internal static class MissileCameraNoseResolver
    {
        private const float DefaultHalfLength = 1.5f;
        private const float TailPivotMinZ = -0.05f;
        private const float MinCameraLocalZ = 0.2f;

        private static FieldInfo? _effectsTransformField;

        internal static MissileCameraNoseResolveResult Resolve(Missile missile)
        {
            Transform missileTransform = missile.transform;
            Transform? effectsRoot = GetEffectsTransform(missile);
            Transform? boosterRoot = GetBoosterTransform(missile);

            float minLocalZ = float.MaxValue;
            float maxLocalZ = float.MinValue;
            bool foundMesh = ScanMeshBounds(missileTransform, effectsRoot, boosterRoot, ref minLocalZ, ref maxLocalZ);

            float meshMaxLocalZ = foundMesh ? maxLocalZ : 0f;
            float colliderMaxLocalZ = 0f;
            string source = "mesh";
            string pivotMode = "unknown";
            float noseLocalZ;

            if (foundMesh && maxLocalZ > 0f)
            {
                noseLocalZ = maxLocalZ;
                pivotMode = DetectPivotMode(minLocalZ, maxLocalZ);
            }
            else
            {
                colliderMaxLocalZ = ScanColliderBounds(missile, missileTransform, ref minLocalZ, ref maxLocalZ);
                if (colliderMaxLocalZ > 0f)
                {
                    noseLocalZ = colliderMaxLocalZ;
                    source = "collider";
                    pivotMode = DetectPivotMode(minLocalZ, maxLocalZ);
                }
                else
                {
                    noseLocalZ = ResolveDefinitionNose(missile, minLocalZ, maxLocalZ, out pivotMode);
                    source = "definition";
                    if (!foundMesh && colliderMaxLocalZ <= 0f && minLocalZ == float.MaxValue)
                        minLocalZ = 0f;
                }
            }

            float skinInset = MissileCameraFeedConfig.NoseSkinInset;
            float backOffset = MissileCameraFeedConfig.CameraBackOffset;
            float cameraLocalZ = Mathf.Max(noseLocalZ - skinInset - backOffset, MinCameraLocalZ);
            float refineMaxZ = noseLocalZ + Mathf.Max(skinInset, 0.15f);
            cameraLocalZ = RefineCameraLocalZ(missile, missileTransform, cameraLocalZ, refineMaxZ);

            if (!foundMesh && minLocalZ == float.MaxValue)
                minLocalZ = 0f;

            return new MissileCameraNoseResolveResult(
                cameraLocalZ,
                noseLocalZ,
                meshMaxLocalZ,
                colliderMaxLocalZ,
                minLocalZ,
                pivotMode,
                source);
        }

        private static bool ScanMeshBounds(
            Transform missileTransform,
            Transform? effectsRoot,
            Transform? boosterRoot,
            ref float minLocalZ,
            ref float maxLocalZ)
        {
            bool found = false;

            MeshRenderer[] meshRenderers = missileTransform.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                if (TryEncapsulateRendererBounds(meshRenderers[i], missileTransform, effectsRoot, boosterRoot, ref minLocalZ, ref maxLocalZ))
                    found = true;
            }

            SkinnedMeshRenderer[] skinnedRenderers = missileTransform.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skinnedRenderers.Length; i++)
            {
                if (TryEncapsulateRendererBounds(skinnedRenderers[i], missileTransform, effectsRoot, boosterRoot, ref minLocalZ, ref maxLocalZ))
                    found = true;
            }

            return found;
        }

        private static bool TryEncapsulateRendererBounds(
            Renderer renderer,
            Transform missileTransform,
            Transform? effectsRoot,
            Transform? boosterRoot,
            ref float minLocalZ,
            ref float maxLocalZ)
        {
            if (renderer == null || !renderer.enabled)
                return false;

            Transform rendererTransform = renderer.transform;
            if (IsUnderExcludedRoot(rendererTransform, effectsRoot) || IsUnderExcludedRoot(rendererTransform, boosterRoot))
                return false;

            Bounds localBounds = renderer.localBounds;
            Vector3 center = localBounds.center;
            Vector3 extents = localBounds.extents;

            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    for (int sz = -1; sz <= 1; sz += 2)
                    {
                        Vector3 localCorner = center + new Vector3(
                            extents.x * sx,
                            extents.y * sy,
                            extents.z * sz);
                        Vector3 worldCorner = rendererTransform.TransformPoint(localCorner);
                        float localZ = missileTransform.InverseTransformPoint(worldCorner).z;
                        if (localZ < minLocalZ)
                            minLocalZ = localZ;
                        if (localZ > maxLocalZ)
                            maxLocalZ = localZ;
                    }
                }
            }

            return true;
        }

        private static float ScanColliderBounds(Missile missile, Transform missileTransform, ref float minLocalZ, ref float maxLocalZ)
        {
            if (!missile.TryGetComponent(out Collider collider) || collider == null)
                return 0f;

            Bounds bounds = collider.bounds;
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;
            float colliderMaxLocalZ = float.MinValue;

            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    for (int sz = -1; sz <= 1; sz += 2)
                    {
                        Vector3 corner = center + new Vector3(
                            extents.x * sx,
                            extents.y * sy,
                            extents.z * sz);
                        float localZ = missileTransform.InverseTransformPoint(corner).z;
                        if (localZ < minLocalZ)
                            minLocalZ = localZ;
                        if (localZ > maxLocalZ)
                            maxLocalZ = localZ;
                        if (localZ > colliderMaxLocalZ)
                            colliderMaxLocalZ = localZ;
                    }
                }
            }

            return colliderMaxLocalZ > 0f ? colliderMaxLocalZ : 0f;
        }

        private static float ResolveDefinitionNose(Missile missile, float minLocalZ, float maxLocalZ, out string pivotMode)
        {
            if (missile.definition != null)
            {
                float length = missile.definition.length;
                pivotMode = DetectPivotMode(minLocalZ, maxLocalZ);

                return pivotMode switch
                {
                    "tail" => length,
                    "center" => length * 0.5f,
                    _ => length * 0.45f
                };
            }

            pivotMode = "default";
            return DefaultHalfLength;
        }

        private static string DetectPivotMode(float minLocalZ, float maxLocalZ)
        {
            if (minLocalZ == float.MaxValue || maxLocalZ == float.MinValue)
                return "unknown";

            float spanZ = maxLocalZ - minLocalZ;
            if (spanZ <= 1e-4f)
                return "unknown";

            if (minLocalZ >= TailPivotMinZ)
                return "tail";

            if (Mathf.Abs(maxLocalZ + minLocalZ) < spanZ * 0.2f)
                return "center";

            return "unknown";
        }

        private static Transform? GetEffectsTransform(Missile missile)
        {
            _effectsTransformField ??= typeof(Missile).GetField(
                "effectsTransform",
                BindingFlags.Instance | BindingFlags.NonPublic);

            return _effectsTransformField?.GetValue(missile) as Transform;
        }

        private static Transform? GetBoosterTransform(Missile missile)
        {
            VLSBooster? booster = missile.GetComponentInChildren<VLSBooster>(true);
            return booster != null ? booster.transform : null;
        }

        private static bool IsUnderExcludedRoot(Transform node, Transform? excludedRoot)
        {
            if (excludedRoot == null)
                return false;

            return node == excludedRoot || node.IsChildOf(excludedRoot);
        }

        private static float RefineCameraLocalZ(Missile missile, Transform missileTransform, float startZ, float maxZ)
        {
            float z = startZ;
            const float step = 0.025f;
            const int maxSteps = 80;

            for (int i = 0; i < maxSteps && z <= maxZ; i++)
            {
                if (!IsCameraInsideMissileBody(missile, missileTransform, z))
                    return z;

                z += step;
            }

            return Mathf.Min(z, maxZ);
        }

        private static bool IsCameraInsideMissileBody(Missile missile, Transform missileTransform, float localZ)
        {
            Vector3 worldPoint = missileTransform.TransformPoint(new Vector3(0f, 0f, localZ));
            Vector3 forward = missileTransform.forward;
            Transform? effectsRoot = GetEffectsTransform(missile);
            Transform? boosterRoot = GetBoosterTransform(missile);

            Collider[] colliders = missile.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider col = colliders[i];
                if (col == null || !col.enabled)
                    continue;

                if (IsUnderExcludedRoot(col.transform, effectsRoot) || IsUnderExcludedRoot(col.transform, boosterRoot))
                    continue;

                Vector3 closest = col.ClosestPoint(worldPoint);
                if ((closest - worldPoint).sqrMagnitude < 1e-6f)
                    return true;
            }

            if (ForwardRayHitsOwnMissile(missile, worldPoint, forward, 0.35f))
                return true;

            return false;
        }

        private static bool ForwardRayHitsOwnMissile(Missile missile, Vector3 origin, Vector3 direction, float distance)
        {
            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                direction,
                distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.transform.IsChildOf(missile.transform) || hit.rigidbody == missile.rb)
                    return true;
            }

            return false;
        }
    }
}
