using UnityEngine;

namespace MissileCamera
{
    /// <summary>Pixel dimensions of the drawable MissileCamera surface (feed / HUD root).</summary>
    internal readonly struct MissileCameraPanelMetrics
    {
        internal const float HudFontScale = 4.2f;
        internal const float HudBaseFontPx = 20f;
        internal const float LandscapeFontRefRatio = 0.52f;
        internal const float TopBandHeightRatio = 0.34f;
        internal const float BottomBandHeightRatio = 0.20f;
        internal const float ColumnWidthRatio = 0.5f;
        internal const float SalvoBlockWidthRatio = 0.14f;
        internal const float RowEdgePad = 3f;
        internal const float EdgeInsetRatio = 0.03f;
        internal const float RightColumnTelemetryEdgeInsetRatio = 0.02f;
        internal const float RightColumnWidthRatio = 0.22f;
        internal const float RightCornerStackHeightRatio = 0.22f;
        internal const float RightColumnMaxWidthRatio = 0.52f;

        internal readonly float Width;
        internal readonly float Height;
        internal readonly float MinSide;
        internal readonly float MaxSide;

        internal MissileCameraPanelMetrics(float width, float height)
        {
            Width = Mathf.Max(width, 1f);
            Height = Mathf.Max(height, 1f);
            MinSide = Mathf.Min(Width, Height);
            MaxSide = Mathf.Max(Width, Height);
        }

        internal static MissileCameraPanelMetrics From(RectTransform panelRt, bool forceCanvasUpdate = false)
        {
            if (forceCanvasUpdate)
                Canvas.ForceUpdateCanvases();

            float w = Mathf.Abs(panelRt.rect.width);
            float h = Mathf.Abs(panelRt.rect.height);
            return new MissileCameraPanelMetrics(w, h);
        }

        internal float HorizontalInset => Width * EdgeInsetRatio;

        internal float LeftHorizontalInset => Width * (EdgeInsetRatio + MfdLayoutController.HudLeftInsetExtra);

        internal float RightHorizontalInset => HorizontalInset;

        internal float VerticalInset => Height * EdgeInsetRatio;

        internal float ContentWidth => Mathf.Max(Width - HorizontalInset * 2f, 1f);

        internal float ContentHeight => Mathf.Max(Height - VerticalInset * 2f, 1f);

        internal float ColumnWidth => ContentWidth * ColumnWidthRatio;

        internal float NameBlockWidth => ColumnWidth;

        internal float TelemetryBlockWidth => ContentWidth / 3f;

        internal float SalvoBlockWidth => Mathf.Max(MinSide * SalvoBlockWidthRatio, 24f);

        internal float SalvoBlockMaxWidth => Mathf.Max(ContentWidth * 0.35f, SalvoBlockWidth);

        internal float NameTextWidth => Mathf.Max(NameBlockWidth - RowEdgePad * 2f, 8f);

        internal float TelemetryTextWidth => Mathf.Max(ResolveTelemetryBlockWidth() - RowEdgePad * 2f, 8f);

        internal float RightColumnBlockWidth => ContentWidth * RightColumnWidthRatio;

        /// <summary>Push stack outward from safe-area root so text sits ~2% from physical MFD right edge.</summary>
        internal float RightColumnTelemetryRightOffset =>
            Mathf.Max(RightHorizontalInset - Width * RightColumnTelemetryEdgeInsetRatio, RowEdgePad);

        internal float RightColumnTelemetryMaxWidth => Mathf.Max(
            Width - Width * RightColumnTelemetryEdgeInsetRatio - HorizontalInset - RowEdgePad * 4f,
            48f);

        internal float RightCornerStackHeight => ContentHeight * RightCornerStackHeightRatio;

        internal float RightColumnRowHeight => RightCornerStackHeight / 3f;

        internal bool UsesRightColumnTelemetry =>
            MfdLayoutController.ActiveTelemetryLayout == MissileCameraTelemetryLayout.RightColumn;

        private float ResolveTelemetryBlockWidth() =>
            UsesRightColumnTelemetry ? RightColumnTelemetryMaxWidth : TelemetryBlockWidth;

        internal float SalvoTextWidth => Mathf.Max(SalvoBlockWidth - RowEdgePad * 2f, 8f);

        internal float TopBandHeight => ContentHeight * TopBandHeightRatio;

        internal float BottomBandHeight => ContentHeight * BottomBandHeightRatio;

        internal float RowGap => TargetScreenUiStyle.ScaledRowHeight(MinSide, 0.012f, 1f, 3f);

        internal float SmallPanelScale
        {
            get
            {
                if (MinSide >= 130f)
                    return 1f;

                if (MinSide <= 65f)
                    return 0.5f;

                return Mathf.Lerp(0.5f, 1f, (MinSide - 65f) / 65f);
            }
        }

        internal float FontRefSize
        {
            get
            {
                bool landscape = Width >= Height * 1.12f;
                float panelRef = landscape
                    ? MaxSide * LandscapeFontRefRatio
                    : MinSide * SmallPanelScale;

                if (!landscape)
                {
                    float layoutRef = MfdLayoutController.ActiveStubFontRef;
                    if (layoutRef > 1f)
                        panelRef = Mathf.Max(panelRef, layoutRef * SmallPanelScale * 0.85f);
                }

                return Mathf.Max(panelRef, 40f);
            }
        }

        internal int GetMinFontSize() =>
            TargetScreenUiStyle.SnapHudFont(Mathf.RoundToInt(Mathf.Clamp(MinSide * 0.10f, 16f, 28f)));

        internal int GetFontSize(StubTextRole role)
        {
            float scale = FontRefSize / 150f * HudFontScale;
            float roleMul = role switch
            {
                StubTextRole.Header => 0.94f,
                StubTextRole.Body => 0.90f,
                _ => 0.86f,
            };

            return TargetScreenUiStyle.SnapHudFont(Mathf.RoundToInt(HudBaseFontPx * scale * roleMul));
        }
    }
}
