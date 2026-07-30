using MissileCamera.Config;
using UnityEngine;

namespace MissileCamera
{
    internal static class MissileCameraHudConfig
    {
        // Hardcoded HUD look (not player-facing).
        internal const float SalvoWindowSeconds = 0.5f;
        internal const int CockpitPipFps = 10;
        internal const float LabelBackgroundAlpha = 0.62f;

        // MFD classic center cluster only (FS FLIR uses its own FlirGreen chrome + hollow intercept ring).
        internal static readonly Color InterceptColor = new Color(0.15f, 0.95f, 0.25f, 1f);
        /// <summary>FS hollow intercept ring at aimPoint (classic green).</summary>
        internal static readonly Color FsInterceptRingColor = new Color(0f, 1f, 0f, 1f);
        internal static readonly Color ReticleColor = new Color(0.08f, 0.18f, 0.55f, 1f);
        internal static readonly Color HorizonColor = new Color(0.05f, 0.35f, 0.08f, 1f);
        internal static readonly Color HorizonOutlineColor = new Color(0.2f, 1f, 0.25f, 1f);
        internal static readonly Color MissileNameColor = new Color(1f, 0f, 1f, 1f);
        internal static readonly Color OwnshipNameColor = new Color(1f, 0.15f, 0.15f, 1f);
        internal static readonly Color TargetNameColor = new Color(0.4f, 0.9f, 1f, 1f);
        /// <summary>MFD locked-target diamond on seeker feed.</summary>
        internal static readonly Color TargetMarkerColor = new Color(1f, 0.45f, 0.05f, 1f);
        internal static readonly Color LabelBackgroundColor = new Color(0.18f, 0.18f, 0.18f, 0.62f);

        internal static Color HorizonFillColor = DeriveHorizonFillColor(HorizonOutlineColor);

        internal static bool Enabled = true;
        internal static bool ShowCenterCluster = true;
        internal static bool ShowTargetMarker = true;
        internal static bool CockpitPipEnabled = true;
        internal static int Revision;

        internal static bool UseTgpStyle => false;

        /// <summary>FLIR HUD only while game-fullscreen feed is active. MFD always uses classic corners.</summary>
        internal static bool UseFullscreenFlirHud =>
            Enabled && MissileCameraFullscreenController.IsActive;

        internal static void Refresh(bool force = false)
        {
            if (!MissileCameraBepInConfig.IsBound)
                return;

            bool enabled = MissileCameraBepInConfig.HudEnabled.Value;
            bool showCenterCluster = MissileCameraBepInConfig.ShowCenterCluster.Value;
            bool showTargetMarker = MissileCameraBepInConfig.ShowTargetMarker.Value;
            bool cockpitPipEnabled = MissileCameraBepInConfig.HudCockpitPipEnabled.Value;

            if (!force
                && enabled == Enabled
                && showCenterCluster == ShowCenterCluster
                && showTargetMarker == ShowTargetMarker
                && cockpitPipEnabled == CockpitPipEnabled)
                return;

            Enabled = enabled;
            ShowCenterCluster = showCenterCluster;
            ShowTargetMarker = showTargetMarker;
            CockpitPipEnabled = cockpitPipEnabled;
            HorizonFillColor = DeriveHorizonFillColor(HorizonOutlineColor);
            Revision++;
        }

        internal static Color DeriveHorizonFillColor(Color outline)
        {
            Color.RGBToHSV(outline, out float h, out float s, out float v);
            v = Mathf.Clamp01(v - 0.18f);
            s = Mathf.Clamp01(s * 0.95f);
            Color fill = Color.HSVToRGB(h, s, v);
            fill.a = outline.a;
            return fill;
        }
    }
}
