using MissileCamera.Config;
using UnityEngine;

namespace MissileCamera.Bridge
{
    // Player-facing NOXMFD / headless-bridge tuning (com.at747.missilecamera.bepinex.cfg → MissileCameraBridge).
    internal static class MissileCameraBridgeConfig
    {
        internal static bool Enabled = true;
        internal static int RenderFps = 12;
        internal static bool TouchCockpitLayout;
        internal static bool SuppressCockpitMfd = true;
        internal static BridgeMarkerLabelMode MarkerLabelMode = BridgeMarkerLabelMode.SelectedOnly;
        internal static int FeedWidth = 960;
        internal static int FeedHeight = 540;

        internal static int StreamHz = 10;
        internal static int StreamMaxDim = 480;
        internal static int StreamJpegQuality = 42;

        internal static float TelemetryInterval = 0.15f;
        internal static float MarkersInterval = 0.2f;
        internal static float PoolInterval = 0.5f;

        internal static int Revision;

        internal static void Refresh(bool force = false)
        {
            if (!MissileCameraBepInConfig.IsBound)
                return;

            bool enabled = MissileCameraBepInConfig.BridgeEnabled.Value;
            int renderFps = MissileCameraBepInConfig.BridgeRenderFps.Value;
            bool touchLayout = MissileCameraBepInConfig.BridgeTouchCockpitLayout.Value;
            bool suppressMfd = MissileCameraBepInConfig.BridgeSuppressCockpitMfd.Value;
            string labelRaw = MissileCameraBepInConfig.BridgeMarkerLabels.Value;
            BridgeMarkerLabelMode labelMode = ParseMarkerLabelMode(labelRaw);
            int feedW = MissileCameraBepInConfig.BridgeFeedWidth.Value;
            int feedH = MissileCameraBepInConfig.BridgeFeedHeight.Value;
            int streamHz = MissileCameraBepInConfig.BridgeStreamHz.Value;
            int streamMax = MissileCameraBepInConfig.BridgeStreamMaxDim.Value;
            int streamQ = MissileCameraBepInConfig.BridgeStreamJpegQuality.Value;
            float tele = MissileCameraBepInConfig.BridgeTelemetryInterval.Value;
            float markers = MissileCameraBepInConfig.BridgeMarkersInterval.Value;
            float pool = MissileCameraBepInConfig.BridgePoolInterval.Value;

            if (!force
                && enabled == Enabled
                && renderFps == RenderFps
                && touchLayout == TouchCockpitLayout
                && suppressMfd == SuppressCockpitMfd
                && labelMode == MarkerLabelMode
                && feedW == FeedWidth
                && feedH == FeedHeight
                && streamHz == StreamHz
                && streamMax == StreamMaxDim
                && streamQ == StreamJpegQuality
                && tele == TelemetryInterval
                && markers == MarkersInterval
                && pool == PoolInterval)
                return;

            Enabled = enabled;
            RenderFps = Mathf.Clamp(renderFps, 4, 60);
            TouchCockpitLayout = touchLayout;
            SuppressCockpitMfd = suppressMfd;
            MarkerLabelMode = labelMode;
            FeedWidth = feedW;
            FeedHeight = feedH;
            StreamHz = Mathf.Clamp(streamHz, 4, 30);
            StreamMaxDim = Mathf.Clamp(streamMax, 240, 1080);
            StreamJpegQuality = Mathf.Clamp(streamQ, 20, 90);
            TelemetryInterval = Mathf.Clamp(tele, 0.05f, 1f);
            MarkersInterval = Mathf.Clamp(markers, 0.05f, 1f);
            PoolInterval = Mathf.Clamp(pool, 0.1f, 2f);
            Revision++;
        }

        internal static BridgeMarkerLabelMode ParseMarkerLabelMode(string? raw)
        {
            if (string.Equals(raw, "All", System.StringComparison.OrdinalIgnoreCase))
                return BridgeMarkerLabelMode.All;
            if (string.Equals(raw, "None", System.StringComparison.OrdinalIgnoreCase))
                return BridgeMarkerLabelMode.None;
            return BridgeMarkerLabelMode.SelectedOnly;
        }
    }
}
