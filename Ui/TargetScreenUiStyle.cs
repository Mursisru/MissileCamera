using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    internal static class TargetScreenUiStyle
    {
        private const float ReferencePanelHeight = 150f;

        internal static void ApplyLabel(Text target, TargetScreenUI? screenUi, bool header = false)
        {
            Text? reference = screenUi != null
                ? TargetScreenUiAccess.GetModeText(screenUi) ?? TargetScreenUiAccess.GetMagText(screenUi)
                : null;

            if (reference != null)
            {
                target.font = reference.font;
                target.fontSize = header ? reference.fontSize : Mathf.Max(10, reference.fontSize - 2);
                target.color = reference.color;
                return;
            }

            target.font = HudFontHelper.GetFont();
            target.fontSize = header ? 14 : 12;
            target.color = new Color(0.85f, 0.95f, 0.85f, 1f);
        }

        internal static Color GetDividerColor(TargetScreenUI? screenUi)
        {
            Text? reference = screenUi != null ? TargetScreenUiAccess.GetModeText(screenUi) : null;
            if (reference != null)
            {
                Color c = reference.color;
                c.a = 0.55f;
                return c;
            }

            return new Color(0.45f, 0.75f, 0.45f, 0.55f);
        }

        internal static Color GetPanelBorderColor(TargetScreenUI? screenUi)
        {
            Color c = GetDividerColor(screenUi);
            c.a = 1f;
            return c;
        }

        internal static Color GetStubPanelColor(TargetScreenUI? screenUi)
        {
            if (MfdLayoutConfig.DebugStub)
                return new Color(1f, 0f, 1f, 0.85f);

            return new Color(0.05f, 0.08f, 0.14f, 1f);
        }

        internal static Color GetStubLabelColor(TargetScreenUI? screenUi)
        {
            Text? reference = screenUi != null ? TargetScreenUiAccess.GetModeText(screenUi) : null;
            if (reference != null)
                return reference.color;

            return new Color(0.55f, 0.95f, 0.55f, 1f);
        }

        internal static void ApplyScaledStubText(
            Text target,
            TargetScreenUI? screenUi,
            float panelWidth,
            float panelHeight,
            StubTextRole role)
        {
            Text? reference = screenUi != null
                ? TargetScreenUiAccess.GetModeText(screenUi) ?? TargetScreenUiAccess.GetMagText(screenUi)
                : null;

            target.font = reference?.font ?? HudFontHelper.GetFont();
            target.color = GetStubLabelColor(screenUi);
            target.horizontalOverflow = HorizontalWrapMode.Overflow;
            target.verticalOverflow = VerticalWrapMode.Overflow;
            target.raycastTarget = false;
            target.resizeTextForBestFit = false;

            int refSize = reference?.fontSize ?? 14;
            float scale = panelHeight / ReferencePanelHeight;
            float roleMul = role switch
            {
                StubTextRole.Header => 1.0f,
                StubTextRole.Body => 0.92f,
                _ => 0.82f,
            };
            target.fontSize = SnapEvenFont(Mathf.RoundToInt(refSize * scale * roleMul));
        }

        internal static float ScaledRowHeight(float panelHeight, float ratio, float min, float max) =>
            Snap(Mathf.Clamp(panelHeight * ratio, min, max));

        internal static float Snap(float value) => Mathf.Round(value);

        internal static int SnapEvenFont(int size)
        {
            size = Mathf.Clamp(size, 8, 24);
            return (size + 1) / 2 * 2;
        }

        internal static int SnapHudFont(int size)
        {
            size = Mathf.Clamp(size, 10, 72);
            return (size + 1) / 2 * 2;
        }
    }

    internal enum StubTextRole
    {
        Header,
        Body,
        Telemetry
    }
}
