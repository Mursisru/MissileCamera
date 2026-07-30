using UnityEngine;

namespace MissileCamera
{
    internal readonly struct InfraredExposureBreakdown
    {
        internal readonly float PolicyExposure;
        internal readonly float VanillaExposure;
        internal readonly bool SyncedVanilla;
        internal readonly float BrightLightPenalty;
        internal readonly float MissileBiasEv;
        internal readonly float FinalExposure;

        internal InfraredExposureBreakdown(
            float policyExposure,
            float vanillaExposure,
            bool syncedVanilla,
            float brightLightPenalty,
            float missileBiasEv,
            float finalExposure)
        {
            PolicyExposure = policyExposure;
            VanillaExposure = vanillaExposure;
            SyncedVanilla = syncedVanilla;
            BrightLightPenalty = brightLightPenalty;
            MissileBiasEv = missileBiasEv;
            FinalExposure = finalExposure;
        }
    }

    /// <summary>
    /// TargetCam IR exposure only. No bright-light EV darkening (that crushed the whole feed).
    /// Optional <see cref="MissileCameraFeedConfig.InfraredExposureBiasEv"/> is additive and defaults to 0.
    /// </summary>
    internal static class MissileCameraInfraredExposure
    {
        internal static float Resolve(
            Camera feedCamera,
            float policyExposure,
            out InfraredExposureBreakdown breakdown)
        {
            _ = feedCamera;

            float vanillaExposure = policyExposure;
            bool syncedVanilla = TargetCamAccess.TryGetVanillaIrSnapshot(
                out bool vanillaIr,
                out float liveVanillaExposure,
                out _)
                && vanillaIr;

            if (syncedVanilla)
                vanillaExposure = liveVanillaExposure;

            float baseExposure = syncedVanilla ? vanillaExposure : policyExposure;
            float missileBias = MissileCameraFeedConfig.InfraredExposureBiasEv;
            float finalExposure = Mathf.Clamp(baseExposure + missileBias, -4f, 4f);

            breakdown = new InfraredExposureBreakdown(
                policyExposure,
                vanillaExposure,
                syncedVanilla,
                brightLightPenalty: 0f,
                missileBias,
                finalExposure);

            return finalExposure;
        }

        internal static void Reset()
        {
        }
    }
}
