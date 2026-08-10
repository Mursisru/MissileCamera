using NuclearOption.MissionEditorScripts;
using UnityEngine;

namespace MissileCamera
{
    /// <summary>
    /// Fullscreen vision filter cycle (J). MFD ignores this and keeps lighting auto-IR.
    /// </summary>
    internal static class MissileCameraVisionModeController
    {
        private static readonly MissileCameraVisionMode[] CycleOrder =
        {
            MissileCameraVisionMode.Color,
            MissileCameraVisionMode.NightVision,
            MissileCameraVisionMode.WhiteHot,
            MissileCameraVisionMode.BlackHot,
            MissileCameraVisionMode.WhiteContour,
            MissileCameraVisionMode.BlackContour
        };

        private static MissileCameraVisionMode _mode = MissileCameraVisionMode.WhiteHot;

        internal static MissileCameraVisionMode Mode => _mode;

        internal static void Reset() => _mode = MissileCameraVisionMode.WhiteHot;

        internal static void Cycle()
        {
            int index = 0;
            for (int i = 0; i < CycleOrder.Length; i++)
            {
                if (CycleOrder[i] == _mode)
                {
                    index = i;
                    break;
                }
            }

            _mode = CycleOrder[(index + 1) % CycleOrder.Length];
            MfdLog.Info("vision mode → " + _mode);
        }

        internal static bool UsesInfraredBlit(MissileCameraVisionMode mode) =>
            mode == MissileCameraVisionMode.WhiteHot
            || mode == MissileCameraVisionMode.BlackHot
            || mode == MissileCameraVisionMode.WhiteContour
            || mode == MissileCameraVisionMode.BlackContour;

        internal static bool UsesNightVisionVolume(MissileCameraVisionMode mode) =>
            mode == MissileCameraVisionMode.NightVision;

        internal static string FlirPolarityLabel(MissileCameraVisionMode mode) =>
            mode switch
            {
                MissileCameraVisionMode.NightVision => "C NVG",
                MissileCameraVisionMode.WhiteHot => "C WH DDE",
                MissileCameraVisionMode.BlackHot => "C BH DDE",
                MissileCameraVisionMode.WhiteContour => "C EDGE+",
                MissileCameraVisionMode.BlackContour => "C EDGE-",
                _ => "C COLOR"
            };

        internal static string ModeLabel(MissileCameraVisionMode mode) =>
            mode switch
            {
                MissileCameraVisionMode.NightVision => "MODE: NVG",
                MissileCameraVisionMode.WhiteHot => "MODE: IR",
                MissileCameraVisionMode.BlackHot => "MODE: IR",
                MissileCameraVisionMode.WhiteContour => "MODE: EDGE",
                MissileCameraVisionMode.BlackContour => "MODE: EDGE",
                _ => "MODE: COLOR"
            };

        internal static string PaletteLabel(MissileCameraVisionMode mode) =>
            mode switch
            {
                MissileCameraVisionMode.NightVision => "PALETTE: NVG",
                MissileCameraVisionMode.WhiteHot => "PALETTE: WhiteHot",
                MissileCameraVisionMode.BlackHot => "PALETTE: BlackHot",
                MissileCameraVisionMode.WhiteContour => "PALETTE: Contour+",
                MissileCameraVisionMode.BlackContour => "PALETTE: Contour-",
                _ => "PALETTE: ---"
            };

        /// <summary>Compact English labels for gunship HUD weapon block.</summary>
        internal static string GunshipModeLabel(MissileCameraVisionMode mode) =>
            mode switch
            {
                MissileCameraVisionMode.NightVision => "MODE  NVG",
                MissileCameraVisionMode.WhiteHot => "MODE  WHITE HOT",
                MissileCameraVisionMode.BlackHot => "MODE  BLACK HOT",
                MissileCameraVisionMode.WhiteContour => "MODE  EDGE+",
                MissileCameraVisionMode.BlackContour => "MODE  EDGE-",
                _ => "MODE  COLOR"
            };

        internal static bool IsBlockedByUi()
        {
            try
            {
                if (InputFieldChecker.InsideInputField)
                    return true;
            }
            catch
            {
                // ignore
            }

            try
            {
                return GameplayUI.GameIsPaused || DynamicMap.mapMaximized;
            }
            catch
            {
                return false;
            }
        }
    }
}
