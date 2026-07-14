using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    internal readonly struct MissileCameraHudFit
    {
        internal readonly float TopBandHeight;
        internal readonly float BottomBandHeight;
        internal readonly float NameRowHeight;
        internal readonly float SalvoRowHeight;
        internal readonly float TelemetryRowHeight;
        internal readonly float NameTextWidth;
        internal readonly float TelemetryTextWidth;
        internal readonly float SalvoBlockWidth;
        internal readonly float RowGap;
        internal readonly int FontSizeHeader;
        internal readonly int FontSizeBody;
        internal readonly int FontSizeTelemetry;
        internal readonly MissileCameraTelemetryLayout TelemetryLayout;
        internal readonly float RightColumnBlockWidth;
        internal readonly float RightColumnTelemetryMaxWidth;

        internal MissileCameraHudFit(
            float topBandHeight,
            float bottomBandHeight,
            float nameRowHeight,
            float salvoRowHeight,
            float telemetryRowHeight,
            float nameTextWidth,
            float telemetryTextWidth,
            float salvoBlockWidth,
            float rowGap,
            int fontSizeHeader,
            int fontSizeBody,
            int fontSizeTelemetry,
            MissileCameraTelemetryLayout telemetryLayout,
            float rightColumnBlockWidth,
            float rightColumnTelemetryMaxWidth)
        {
            TopBandHeight = topBandHeight;
            BottomBandHeight = bottomBandHeight;
            NameRowHeight = nameRowHeight;
            SalvoRowHeight = salvoRowHeight;
            TelemetryRowHeight = telemetryRowHeight;
            NameTextWidth = nameTextWidth;
            TelemetryTextWidth = telemetryTextWidth;
            SalvoBlockWidth = salvoBlockWidth;
            RowGap = rowGap;
            FontSizeHeader = fontSizeHeader;
            FontSizeBody = fontSizeBody;
            FontSizeTelemetry = fontSizeTelemetry;
            TelemetryLayout = telemetryLayout;
            RightColumnBlockWidth = rightColumnBlockWidth;
            RightColumnTelemetryMaxWidth = rightColumnTelemetryMaxWidth;
        }
    }

    internal static class MissileCameraHudLayout
    {
        internal static MissileCameraHudFit Fit(
            MissileCameraPanelMetrics panel,
            MissileCameraHudSnapshot snapshot,
            TargetScreenUI? screenUi,
            MissileCameraCornerHud.Rows rows)
        {
            if (MissileCameraPanelMetrics.IsGameFullscreen)
                return FitFullscreen(panel, snapshot, screenUi, rows);

            bool rightColumn = panel.UsesRightColumnTelemetry;
            float nameTextWidth = panel.NameTextWidth;
            float telemetryTextWidth = panel.TelemetryTextWidth;
            float rowGap = panel.RowGap;
            float topBandHeight = panel.TopBandHeight;
            float bottomBandHeight = panel.BottomBandHeight;
            float telemetryRowCap = rightColumn ? panel.RightColumnRowHeight : bottomBandHeight;

            int minFont = panel.GetMinFontSize();
            int startFont = Mathf.Max(panel.GetFontSize(StubTextRole.Body), minFont);
            for (int font = startFont; font >= minFont; font -= font > 24 ? 2 : 1)
            {
                int header = TargetScreenUiStyle.SnapHudFont(Mathf.RoundToInt(font * 1.04f));
                int body = font;
                int telemetry = TargetScreenUiStyle.SnapHudFont(Mathf.Max(10, font - 1));

                float nameRowH = RowHeight(header);
                float salvoRowH = RowHeight(body);
                float telemetryRowH = RowHeight(telemetry);
                float computedTopH = nameRowH + rowGap + salvoRowH;

                ApplyFonts(rows, screenUi, panel, header, body, telemetry);
                ApplyContent(rows, snapshot, nameTextWidth, forceCanvasUpdate: false);

                float salvoBlockW = ComputeSalvoBlockWidth(panel, rows.Salvo);
                float salvoTextW = Mathf.Max(salvoBlockW - MissileCameraPanelMetrics.RowEdgePad * 2f, 8f);

                if (Fits(
                        rows,
                        nameTextWidth,
                        salvoTextW,
                        telemetryTextWidth,
                        computedTopH,
                        topBandHeight,
                        telemetryRowH,
                        telemetryRowCap))
                {
                    return new MissileCameraHudFit(
                        topBandHeight,
                        bottomBandHeight,
                        nameRowH,
                        salvoRowH,
                        telemetryRowH,
                        nameTextWidth,
                        telemetryTextWidth,
                        salvoBlockW,
                        rowGap,
                        header,
                        body,
                        telemetry,
                        rightColumn ? MissileCameraTelemetryLayout.RightColumn : MissileCameraTelemetryLayout.BottomRow,
                        panel.RightColumnBlockWidth,
                        panel.RightColumnTelemetryMaxWidth);
                }
            }

            int fallback = minFont;
            float fallbackNameH = RowHeight(fallback);
            float fallbackSalvoH = RowHeight(fallback);
            float fallbackTelemetryH = RowHeight(fallback);
            ApplyFonts(rows, screenUi, panel, fallback, fallback, fallback);
            ApplyContent(rows, snapshot, nameTextWidth, forceCanvasUpdate: true);
            float fallbackSalvoBlockW = ComputeSalvoBlockWidth(panel, rows.Salvo);
            return new MissileCameraHudFit(
                topBandHeight,
                bottomBandHeight,
                fallbackNameH,
                fallbackSalvoH,
                fallbackTelemetryH,
                nameTextWidth,
                telemetryTextWidth,
                fallbackSalvoBlockW,
                rowGap,
                fallback,
                fallback,
                fallback,
                rightColumn ? MissileCameraTelemetryLayout.RightColumn : MissileCameraTelemetryLayout.BottomRow,
                panel.RightColumnBlockWidth,
                panel.RightColumnTelemetryMaxWidth);
        }

        private static MissileCameraHudFit FitFullscreen(
            MissileCameraPanelMetrics panel,
            MissileCameraHudSnapshot snapshot,
            TargetScreenUI? screenUi,
            MissileCameraCornerHud.Rows rows)
        {
            int header = panel.GetFontSize(StubTextRole.Header);
            int body = panel.GetFontSize(StubTextRole.Body);
            int telemetry = panel.GetFontSize(StubTextRole.Telemetry);
            float nameRowH = panel.FullscreenNameRowHeight;
            float salvoRowH = Mathf.Max(RowHeight(body), 20f);
            float telemetryRowH = panel.FullscreenTelemetryChipHeight;
            float topBand = panel.TopBandHeight;
            float bottomBand = panel.BottomBandHeight;
            float nameTextWidth = panel.NameTextWidth;
            float telemetryTextWidth = panel.TelemetryTextWidth;

            ApplyFonts(rows, screenUi, panel, header, body, telemetry);
            ApplyContent(rows, snapshot, nameTextWidth, forceCanvasUpdate: false);
            float salvoBlockW = Mathf.Clamp(ComputeSalvoBlockWidth(panel, rows.Salvo), 48f, 96f);

            return new MissileCameraHudFit(
                topBand,
                bottomBand,
                nameRowH,
                salvoRowH,
                telemetryRowH,
                nameTextWidth,
                telemetryTextWidth,
                salvoBlockW,
                panel.RowGap,
                header,
                body,
                telemetry,
                MissileCameraTelemetryLayout.BottomRow,
                panel.RightColumnBlockWidth,
                panel.RightColumnTelemetryMaxWidth);
        }

        internal static void UpdateContent(
            MissileCameraCornerHud.Rows rows,
            MissileCameraHudSnapshot snapshot,
            float nameTextWidth)
        {
            ApplyContent(rows, snapshot, nameTextWidth, forceCanvasUpdate: false);
        }

        private static float RowHeight(int fontSize) => fontSize + 6f;

        private static void ApplyFonts(
            MissileCameraCornerHud.Rows rows,
            TargetScreenUI? screenUi,
            MissileCameraPanelMetrics panel,
            int header,
            int body,
            int telemetry)
        {
            ApplyFont(rows.MissileName, screenUi, panel, header);
            ApplyFont(rows.Salvo, screenUi, panel, body);
            ApplyFont(rows.Target, screenUi, panel, body);
            ApplyFont(rows.Speed, screenUi, panel, telemetry);
            ApplyFont(rows.Altitude, screenUi, panel, telemetry);
            ApplyFont(rows.Range, screenUi, panel, telemetry);
        }

        private static void ApplyFont(Text target, TargetScreenUI? screenUi, MissileCameraPanelMetrics panel, int fontSize)
        {
            if (screenUi != null)
            {
                TargetScreenUiStyle.ApplyScaledStubText(target, screenUi, panel.FontRefSize, panel.FontRefSize, StubTextRole.Telemetry);
                target.fontSize = fontSize;
            }
            else
            {
                target.font = HudFontHelper.GetFont();
                target.fontSize = fontSize;
            }
        }

        private static void ApplyContent(
            MissileCameraCornerHud.Rows rows,
            MissileCameraHudSnapshot snapshot,
            float nameTextWidth,
            bool forceCanvasUpdate)
        {
            rows.MissileName.text = snapshot.MissileName;
            rows.Salvo.text = $"{snapshot.SalvoIndex}/{snapshot.SalvoTotal}";
            rows.Target.text = snapshot.TargetName;
            rows.Speed.text = MissileCameraTelemetry.FormatLabeledRow("S", snapshot.SpeedText);
            rows.Altitude.text = MissileCameraTelemetry.FormatLabeledRow("A", snapshot.AltitudeText);
            rows.Range.text = MissileCameraTelemetry.FormatLabeledRow("R", snapshot.RangeText);

            if (forceCanvasUpdate)
                Canvas.ForceUpdateCanvases();

            TruncateIfNeeded(rows.MissileName, nameTextWidth);
            TruncateIfNeeded(rows.Target, nameTextWidth);
        }

        private static bool Fits(
            MissileCameraCornerHud.Rows rows,
            float nameTextWidth,
            float salvoTextWidth,
            float telemetryTextWidth,
            float computedTopHeight,
            float maxTopBandHeight,
            float telemetryRowHeight,
            float maxTelemetryRowHeight)
        {
            Canvas.ForceUpdateCanvases();

            if (MeasureWidth(rows.MissileName) > nameTextWidth + 0.5f)
                return false;

            if (MeasureWidth(rows.Target) > nameTextWidth + 0.5f)
                return false;

            if (MeasureWidth(rows.Salvo) > salvoTextWidth + 0.5f)
                return false;

            if (computedTopHeight > maxTopBandHeight + 0.5f)
                return false;

            if (telemetryRowHeight > maxTelemetryRowHeight + 0.5f)
                return false;

            return MeasureWidth(rows.Speed) <= telemetryTextWidth + 0.5f
                && MeasureWidth(rows.Altitude) <= telemetryTextWidth + 0.5f
                && MeasureWidth(rows.Range) <= telemetryTextWidth + 0.5f;
        }

        private static float MeasureWidth(Text text) => HudBackdropHelper.MeasureTextWidth(text);

        private static void TruncateIfNeeded(Text text, float maxWidth)
        {
            if (HudBackdropHelper.MeasureTextWidth(text) <= maxWidth)
                return;

            string value = text.text;
            const string ellipsis = "…";
            for (int len = value.Length - 1; len > 0; len--)
            {
                string trial = value.Substring(0, len) + ellipsis;
                text.text = trial;
                if (HudBackdropHelper.MeasureTextWidth(text) <= maxWidth)
                    return;
            }

            text.text = ellipsis;
        }

        internal static float ComputeSalvoBlockWidth(MissileCameraPanelMetrics panel, Text salvo)
        {
            float pad = MissileCameraPanelMetrics.RowEdgePad * 2f;
            float measured = MeasureWidth(salvo) + pad;
            return Mathf.Clamp(Mathf.Max(measured, panel.SalvoBlockWidth), 24f, panel.SalvoBlockMaxWidth);
        }
    }
}
