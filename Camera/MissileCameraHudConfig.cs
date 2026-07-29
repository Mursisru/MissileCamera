using System.Globalization;
using MissileCamera.Config;
using UnityEngine;

namespace MissileCamera
{
    internal static class MissileCameraHudConfig
    {
        internal static bool Enabled = true;
        internal static float SalvoWindowSeconds = 0.5f;
        internal static bool ShowCenterCluster = true;
        internal static bool ShowTargetMarker = true;
        internal static bool CockpitPipEnabled = true;
        internal static int CockpitPipFps = 10;
        internal static Color InterceptColor = new Color(0f, 1f, 0f, 1f);
        internal static Color ReticleColor = new Color(1f, 1f, 1f, 1f);
        internal static Color HorizonColor = new Color(0.05f, 0.35f, 0.08f, 1f);
        internal static Color HorizonFillColor = DeriveHorizonFillColor(new Color(0.2f, 1f, 0.25f, 1f));
        internal static Color HorizonOutlineColor = new Color(0.2f, 1f, 0.25f, 1f);
        internal static Color MissileNameColor = new Color(1f, 0f, 1f, 1f);
        internal static Color OwnshipNameColor = new Color(1f, 0.15f, 0.15f, 1f);
        internal static Color TargetNameColor = new Color(0.4f, 0.9f, 1f, 1f);
        internal static Color LabelBackgroundColor = new Color(0.18f, 0.18f, 0.18f, 0.62f);
        internal static float LabelBackgroundAlpha = 0.62f;
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
            float salvoWindowSeconds = MissileCameraBepInConfig.SalvoWindowSeconds.Value;
            bool showCenterCluster = MissileCameraBepInConfig.ShowCenterCluster.Value;
            bool showTargetMarker = MissileCameraBepInConfig.ShowTargetMarker.Value;
            bool cockpitPipEnabled = MissileCameraBepInConfig.HudCockpitPipEnabled.Value;
            int cockpitPipFps = MissileCameraBepInConfig.HudCockpitPipFps.Value;
            Color interceptColor = ParseColor(MissileCameraBepInConfig.InterceptColor.Value, InterceptColor);
            Color reticleColor = ParseColor(MissileCameraBepInConfig.ReticleColor.Value, ReticleColor);
            Color horizonColor = ParseColor(MissileCameraBepInConfig.HorizonColor.Value, HorizonColor);
            Color horizonOutlineColor = ParseColor(MissileCameraBepInConfig.HorizonOutlineColor.Value, HorizonOutlineColor);
            Color missileNameColor = ParseColor(MissileCameraBepInConfig.MissileNameColor.Value, MissileNameColor);
            Color ownshipNameColor = ParseColor(MissileCameraBepInConfig.OwnshipNameColor.Value, OwnshipNameColor);
            Color targetNameColor = ParseColor(MissileCameraBepInConfig.TargetNameColor.Value, TargetNameColor);
            Color labelBackgroundColor = ParseColor(MissileCameraBepInConfig.LabelBackgroundColor.Value, LabelBackgroundColor);
            float labelBackgroundAlpha = MissileCameraBepInConfig.LabelBackgroundAlpha.Value;

            if (!force
                && enabled == Enabled
                && salvoWindowSeconds == SalvoWindowSeconds
                && showCenterCluster == ShowCenterCluster
                && showTargetMarker == ShowTargetMarker
                && cockpitPipEnabled == CockpitPipEnabled
                && cockpitPipFps == CockpitPipFps
                && interceptColor == InterceptColor
                && reticleColor == ReticleColor
                && horizonColor == HorizonColor
                && horizonOutlineColor == HorizonOutlineColor
                && missileNameColor == MissileNameColor
                && ownshipNameColor == OwnshipNameColor
                && targetNameColor == TargetNameColor
                && labelBackgroundColor == LabelBackgroundColor
                && labelBackgroundAlpha == LabelBackgroundAlpha)
                return;

            Enabled = enabled;
            SalvoWindowSeconds = salvoWindowSeconds;
            ShowCenterCluster = showCenterCluster;
            ShowTargetMarker = showTargetMarker;
            CockpitPipEnabled = cockpitPipEnabled;
            CockpitPipFps = cockpitPipFps;
            InterceptColor = interceptColor;
            ReticleColor = reticleColor;
            HorizonColor = horizonColor;
            HorizonOutlineColor = horizonOutlineColor;
            HorizonFillColor = DeriveHorizonFillColor(horizonOutlineColor);
            MissileNameColor = missileNameColor;
            OwnshipNameColor = ownshipNameColor;
            TargetNameColor = targetNameColor;
            LabelBackgroundColor = labelBackgroundColor;
            LabelBackgroundAlpha = labelBackgroundAlpha;
            Revision++;
        }

        private static Color ParseColor(string raw, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return fallback;

            string[] parts = raw.Split(',');
            if (parts.Length < 3)
                return fallback;

            if (!TryParse(parts[0], out float r)
                || !TryParse(parts[1], out float g)
                || !TryParse(parts[2], out float b))
                return fallback;

            float a = parts.Length > 3 && TryParse(parts[3], out float parsedA) ? parsedA : 1f;
            return new Color(r, g, b, a);
        }

        private static bool TryParse(string raw, out float value) =>
            float.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

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
