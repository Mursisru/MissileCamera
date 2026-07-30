using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    internal sealed class MissileCameraCornerHud
    {
        private readonly RectTransform _root;
        private readonly RectTransform _topBand;
        private readonly RectTransform _bottomBand;
        private readonly HudBlock _nameLeft;
        private readonly HudBlock _nameRight;
        private readonly HudBlock _salvo;
        private readonly HudBlock _rng;
        private readonly HudBlock _alt;
        private readonly HudBlock _spd;
        private readonly Image _telemetryStackBackdrop;
        private TargetScreenUI? _screenUi;
        private float _nextContentTime;
        private float _layoutPanelW = -1f;
        private float _layoutPanelH = -1f;
        private MissileCameraHudFit _fit;
        private bool _hasFit;
        private MissileCameraTelemetryLayout _telemetryLayout = MissileCameraTelemetryLayout.BottomRow;
        private string _lastMissileName = string.Empty;
        private string _lastTargetName = string.Empty;
        private string _lastSalvo = string.Empty;
        private string _lastRange = string.Empty;
        private string _lastAltitude = string.Empty;
        private string _lastSpeed = string.Empty;
        private string _lastG = string.Empty;
        private string _lastFuel = string.Empty;
        private string _lastMach = string.Empty;
        private string _lastGuidance = string.Empty;
        private string _lastAngle = string.Empty;

        private MissileCameraCornerHud(
            RectTransform root,
            RectTransform topBand,
            RectTransform bottomBand,
            HudBlock nameLeft,
            HudBlock nameRight,
            HudBlock salvo,
            HudBlock rng,
            HudBlock alt,
            HudBlock spd,
            Image telemetryStackBackdrop)
        {
            _root = root;
            _topBand = topBand;
            _bottomBand = bottomBand;
            _nameLeft = nameLeft;
            _nameRight = nameRight;
            _salvo = salvo;
            _rng = rng;
            _alt = alt;
            _spd = spd;
            _telemetryStackBackdrop = telemetryStackBackdrop;
        }

        internal readonly struct Rows
        {
            internal Rows(
                Text missileName,
                Text salvo,
                Text target,
                Text speed,
                Text altitude,
                Text range)
            {
                MissileName = missileName;
                Salvo = salvo;
                Target = target;
                Speed = speed;
                Altitude = altitude;
                Range = range;
            }

            internal Text MissileName { get; }
            internal Text Salvo { get; }
            internal Text Target { get; }
            internal Text Speed { get; }
            internal Text Altitude { get; }
            internal Text Range { get; }
        }

        internal readonly struct HudBlock
        {
            internal HudBlock(RectTransform root, Image backdrop, Text label)
            {
                Root = root;
                Backdrop = backdrop;
                Label = label;
            }

            internal RectTransform Root { get; }
            internal Image Backdrop { get; }
            internal Text Label { get; }
        }

        internal static MissileCameraCornerHud Create(RectTransform parent, TargetScreenUI? screenUi)
        {
            var rootGo = new GameObject("MissileCameraHudCorners", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            RectTransform root = rootGo.GetComponent<RectTransform>();
            Stretch(root);

            RectTransform topBand = CreateBand(root, "MissileCameraHudTopBand");
            RectTransform bottomBand = CreateBand(root, "MissileCameraHudBottomBand");

            HudBlock nameLeft = CreateBlock(topBand, "MissileCameraHudNameLeft", TextAnchor.MiddleLeft);
            HudBlock nameRight = CreateBlock(topBand, "MissileCameraHudNameRight", TextAnchor.MiddleRight);
            HudBlock salvo = CreateBlock(topBand, "MissileCameraHudSalvo", TextAnchor.MiddleCenter);
            HudBlock rng = CreateBlock(bottomBand, "MissileCameraHudRng", TextAnchor.MiddleCenter);
            HudBlock alt = CreateBlock(bottomBand, "MissileCameraHudAlt", TextAnchor.MiddleCenter);
            HudBlock spd = CreateBlock(bottomBand, "MissileCameraHudSpd", TextAnchor.MiddleCenter);

            Image stackBackdrop = HudBackdropHelper.CreateBackdrop(bottomBand, "MissileCameraHudTelemetryStackBackdrop");
            HudBackdropHelper.StretchToBlock(stackBackdrop);
            stackBackdrop.enabled = false;

            var hud = new MissileCameraCornerHud(
                root, topBand, bottomBand,
                nameLeft, nameRight, salvo, rng, alt, spd, stackBackdrop);
            hud.BindScreenUi(screenUi);
            return hud;
        }

        internal void BindScreenUi(TargetScreenUI? screenUi)
        {
            _screenUi = screenUi;
            InvalidateLayout();
        }

        internal void InvalidateLayout()
        {
            _hasFit = false;
            _layoutPanelW = -1f;
            _layoutPanelH = -1f;
            _telemetryLayout = MissileCameraTelemetryLayout.BottomRow;
            _lastMissileName = string.Empty;
            _lastTargetName = string.Empty;
            _lastSalvo = string.Empty;
            _lastRange = string.Empty;
            _lastAltitude = string.Empty;
            _lastSpeed = string.Empty;
            _lastG = string.Empty;
            _lastFuel = string.Empty;
            _lastMach = string.Empty;
            _lastGuidance = string.Empty;
            _lastAngle = string.Empty;
            HudBackdropHelper.InvalidateTextWidthCache();
        }

        internal void Update(MissileCameraHudSnapshot snapshot, MissileCameraPanelMetrics panel)
        {
            float now = Time.unscaledTime;
            MissileCameraTelemetryLayout activeLayout = MfdLayoutController.ActiveTelemetryLayout;
            bool panelChanged = !Mathf.Approximately(panel.Width, _layoutPanelW)
                || !Mathf.Approximately(panel.Height, _layoutPanelH)
                || activeLayout != _telemetryLayout;
            bool needContent = now >= _nextContentTime || !snapshot.HasFeed;

            if (_hasFit && !panelChanged)
            {
                if (!needContent)
                    return;

                _nextContentTime = now + 0.1f;
                if (SnapshotContentChanged(snapshot))
                {
                    MissileCameraHudLayout.UpdateContent(GetRows(), snapshot, _fit.NameTextWidth);
                    FitSalvoBlockWidth(_fit, panel);
                    if (_telemetryLayout == MissileCameraTelemetryLayout.RightColumn
                        && !FitRightCornerTelemetryWidths(_fit, panel))
                    {
                        _hasFit = false;
                    }

                    RememberSnapshotContent(snapshot);
                }

                return;
            }

            _layoutPanelW = panel.Width;
            _layoutPanelH = panel.Height;
            _telemetryLayout = activeLayout;
            _nextContentTime = now + 0.1f;

            Rows rows = GetRows();
            _fit = MissileCameraHudLayout.Fit(panel, snapshot, _screenUi, rows);
            ApplyColors();
            ApplyRootInsets(panel);
            ApplyTopBand(_fit);
            if (_telemetryLayout != MissileCameraTelemetryLayout.RightColumn)
                ApplyBottomBand(_fit);
            ApplyBlockLayout(_fit, panel);
            ApplyFonts(_fit);
            FitSalvoBlockWidth(_fit, panel);
            if (_telemetryLayout == MissileCameraTelemetryLayout.RightColumn)
                FitRightCornerTelemetryWidths(_fit, panel);
            RememberSnapshotContent(snapshot);
            _hasFit = true;
        }

        private bool SnapshotContentChanged(MissileCameraHudSnapshot snapshot) =>
            snapshot.MissileName != _lastMissileName
            || snapshot.TargetName != _lastTargetName
            || SalvoLabel(snapshot) != _lastSalvo
            || snapshot.RangeText != _lastRange
            || snapshot.AltitudeText != _lastAltitude
            || snapshot.SpeedText != _lastSpeed
            || snapshot.GText != _lastG
            || snapshot.FuelText != _lastFuel
            || snapshot.MachText != _lastMach
            || snapshot.GuidanceText != _lastGuidance
            || snapshot.TargetAngleText != _lastAngle;

        private static string SalvoLabel(MissileCameraHudSnapshot snapshot) =>
            $"{snapshot.SalvoIndex}/{snapshot.SalvoTotal}";

        private void RememberSnapshotContent(MissileCameraHudSnapshot snapshot)
        {
            _lastMissileName = snapshot.MissileName;
            _lastTargetName = snapshot.TargetName;
            _lastSalvo = SalvoLabel(snapshot);
            _lastRange = snapshot.RangeText;
            _lastAltitude = snapshot.AltitudeText;
            _lastSpeed = snapshot.SpeedText;
            _lastG = snapshot.GText;
            _lastFuel = snapshot.FuelText;
            _lastMach = snapshot.MachText;
            _lastGuidance = snapshot.GuidanceText;
            _lastAngle = snapshot.TargetAngleText;
        }

        internal void SetVisible(bool visible) => _root.gameObject.SetActive(visible);

        private Rows GetRows() => new Rows(
            _nameLeft.Label,
            _salvo.Label,
            _nameRight.Label,
            _spd.Label,
            _alt.Label,
            _rng.Label);

        private void ApplyColors()
        {
            _nameLeft.Label.color = MissileCameraHudConfig.MissileNameColor;
            _salvo.Label.color = MissileCameraHudConfig.MissileNameColor;
            _nameRight.Label.color = MissileCameraHudConfig.TargetNameColor;
            _rng.Label.color = Color.white;
            _alt.Label.color = Color.white;
            _spd.Label.color = Color.white;

            foreach (HudBlock block in new[] { _nameLeft, _nameRight, _salvo, _rng, _alt, _spd })
            {
                block.Label.verticalOverflow = VerticalWrapMode.Overflow;
                block.Label.raycastTarget = false;
            }
        }

        private void ApplyFonts(MissileCameraHudFit fit)
        {
            _nameLeft.Label.fontSize = fit.FontSizeHeader;
            _nameRight.Label.fontSize = fit.FontSizeBody;
            _salvo.Label.fontSize = fit.FontSizeBody;
            _rng.Label.fontSize = fit.FontSizeTelemetry;
            _alt.Label.fontSize = fit.FontSizeTelemetry;
            _spd.Label.fontSize = fit.FontSizeTelemetry;
        }

        private void ApplyRootInsets(MissileCameraPanelMetrics panel)
        {
            float left = panel.LeftHorizontalInset;
            float right = panel.RightHorizontalInset;
            float v = panel.VerticalInset;
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = new Vector2(left, v);
            _root.offsetMax = new Vector2(-right, -v);
        }

        private void ApplyTopBand(MissileCameraHudFit fit)
        {
            float bandH = fit.TopBandHeight;
            _topBand.anchorMin = new Vector2(0f, 1f);
            _topBand.anchorMax = new Vector2(1f, 1f);
            _topBand.pivot = new Vector2(0.5f, 1f);
            _topBand.anchoredPosition = Vector2.zero;
            _topBand.offsetMin = new Vector2(0f, -bandH);
            _topBand.offsetMax = Vector2.zero;
        }

        private void ApplyBottomBand(MissileCameraHudFit fit)
        {
            float bandH = fit.BottomBandHeight;
            _bottomBand.anchorMin = new Vector2(0f, 0f);
            _bottomBand.anchorMax = new Vector2(1f, 0f);
            _bottomBand.pivot = new Vector2(0.5f, 0f);
            _bottomBand.anchoredPosition = Vector2.zero;
            _bottomBand.offsetMin = Vector2.zero;
            _bottomBand.offsetMax = new Vector2(0f, bandH);
        }

        private void ApplyBlockLayout(MissileCameraHudFit fit, MissileCameraPanelMetrics panel)
        {
            PlaceTopBlock(_nameLeft, 0f, 0.5f, fit.NameRowHeight, yFromTop: 0f);
            PlaceTopBlock(_nameRight, 0.5f, 1f, fit.NameRowHeight, yFromTop: 0f);
            PlaceCenterTopBlock(_salvo, fit.SalvoBlockWidth, fit.SalvoRowHeight, fit.NameRowHeight + fit.RowGap);

            if (fit.TelemetryLayout == MissileCameraTelemetryLayout.RightColumn)
            {
                ApplyRightCornerTelemetry(fit, panel);
            }
            else if (MissileCameraPanelMetrics.IsGameFullscreen)
            {
                ApplyFullscreenCornerTelemetry(fit, panel);
            }
            else
            {
                ApplyBottomRowTelemetry(fit);
            }

            foreach (HudBlock block in new[] { _nameLeft, _nameRight, _salvo, _rng, _alt, _spd })
            {
                bool telemetryRow = block.Root.parent == _bottomBand;
                if (!telemetryRow || fit.TelemetryLayout != MissileCameraTelemetryLayout.RightColumn)
                    HudBackdropHelper.StretchToBlock(block.Backdrop);

                block.Label.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
        }

        private void ApplyBottomRowTelemetry(MissileCameraHudFit fit)
        {
            _telemetryStackBackdrop.gameObject.SetActive(false);
            _telemetryStackBackdrop.enabled = false;
            _bottomBand.gameObject.SetActive(true);
            ApplyBottomBand(fit);
            ReparentTelemetryToBottomBand();
            PlaceBottomBlock(_rng, 0f, 1f / 3f, fit.TelemetryRowHeight);
            PlaceBottomBlock(_alt, 1f / 3f, 2f / 3f, fit.TelemetryRowHeight);
            PlaceBottomBlock(_spd, 2f / 3f, 1f, fit.TelemetryRowHeight);
            SetTelemetryAlignment(TextAnchor.MiddleCenter);
            SetTelemetryBackdropMode(perRow: true);
        }

        /// <summary>
        /// Game fullscreen: small corner chips — R+A bottom-left stack, S bottom-right. No full-width bar.
        /// </summary>
        private void ApplyFullscreenCornerTelemetry(MissileCameraHudFit fit, MissileCameraPanelMetrics panel)
        {
            _telemetryStackBackdrop.gameObject.SetActive(false);
            _telemetryStackBackdrop.enabled = false;
            _bottomBand.gameObject.SetActive(false);

            ReparentTelemetryToRoot();
            SetTelemetryAlignment(TextAnchor.MiddleLeft);
            SetTelemetryBackdropMode(perRow: true);

            float chipH = panel.FullscreenTelemetryChipHeight;
            float pad = panel.HorizontalInset;
            float chipW = Mathf.Max(panel.TelemetryTextWidth, 96f);

            PlaceFullscreenChip(_rng, left: true, bottomY: pad, width: chipW, height: chipH);
            PlaceFullscreenChip(_alt, left: true, bottomY: pad + chipH + 4f, width: chipW, height: chipH);
            PlaceFullscreenChip(_spd, left: false, bottomY: pad, width: chipW, height: chipH);

            _rng.Label.alignment = TextAnchor.MiddleLeft;
            _alt.Label.alignment = TextAnchor.MiddleLeft;
            _spd.Label.alignment = TextAnchor.MiddleRight;
        }

        private void ReparentTelemetryToRoot()
        {
            _rng.Root.SetParent(_root, false);
            _alt.Root.SetParent(_root, false);
            _spd.Root.SetParent(_root, false);
        }

        private static void PlaceFullscreenChip(HudBlock block, bool left, float bottomY, float width, float height)
        {
            RectTransform rt = block.Root;
            if (left)
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(0f, bottomY);
            }
            else
            {
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(0f, bottomY);
            }

            rt.sizeDelta = new Vector2(width, height);
            ApplyTextStretch(block.Label);
            if (block.Label.transform is RectTransform labelRt)
            {
                labelRt.offsetMin = new Vector2(6f, 0f);
                labelRt.offsetMax = new Vector2(-6f, 0f);
            }

            block.Backdrop.enabled = MissileCameraHudConfig.LabelBackgroundAlpha > 0.001f;
            HudBackdropHelper.StretchToBlock(block.Backdrop);
        }

        private void ApplyRightCornerTelemetry(MissileCameraHudFit fit, MissileCameraPanelMetrics panel)
        {
            _bottomBand.gameObject.SetActive(true);
            float stackH = fit.TelemetryRowHeight * 3f;
            _bottomBand.anchorMin = new Vector2(1f, 0f);
            _bottomBand.anchorMax = new Vector2(1f, 0f);
            _bottomBand.pivot = new Vector2(1f, 0f);
            _bottomBand.anchoredPosition = new Vector2(panel.RightColumnTelemetryRightOffset, 0f);
            _bottomBand.sizeDelta = new Vector2(panel.TelemetryTextWidth + MissileCameraPanelMetrics.RowEdgePad * 2f, stackH);

            ReparentTelemetryToBottomBand();
            SetTelemetryAlignment(TextAnchor.MiddleLeft);
            SetTelemetryBackdropMode(perRow: true);
            _telemetryStackBackdrop.enabled = false;
            _telemetryStackBackdrop.gameObject.SetActive(false);
        }

        private void FitSalvoBlockWidth(MissileCameraHudFit fit, MissileCameraPanelMetrics panel)
        {
            float width = MissileCameraHudLayout.ComputeSalvoBlockWidth(panel, _salvo.Label);
            PlaceCenterTopBlock(_salvo, width, fit.SalvoRowHeight, fit.NameRowHeight + fit.RowGap);
            HudBackdropHelper.StretchToBlock(_salvo.Backdrop);
        }

        private bool FitRightCornerTelemetryWidths(MissileCameraHudFit fit, MissileCameraPanelMetrics panel)
        {
            float pad = MissileCameraPanelMetrics.RowEdgePad * 2f;
            float maxAllowed = panel.TelemetryTextWidth;
            float rowH = fit.TelemetryRowHeight;
            float stackH = rowH * 3f;
            float maxW = 0f;
            bool fits = true;

            maxW = PlaceRightCornerRow(_alt, 2f / 3f, 1f, rowH, pad, maxAllowed, maxW, ref fits);
            maxW = PlaceRightCornerRow(_rng, 1f / 3f, 2f / 3f, rowH, pad, maxAllowed, maxW, ref fits);
            maxW = PlaceRightCornerRow(_spd, 0f, 1f / 3f, rowH, pad, maxAllowed, maxW, ref fits);

            _bottomBand.sizeDelta = new Vector2(maxW, stackH);
            return fits;
        }

        private float PlaceRightCornerRow(
            HudBlock block,
            float yMin,
            float yMax,
            float rowHeight,
            float pad,
            float maxAllowed,
            float maxSoFar,
            ref bool fits)
        {
            float textW = HudBackdropHelper.MeasureTextWidth(block.Label);
            if (textW > maxAllowed + 0.5f)
                fits = false;

            float width = Mathf.Min(textW + pad, maxAllowed + pad);
            width = Mathf.Max(width, 8f);

            PlaceStackRowRight(block, yMin, yMax, width, rowHeight);
            block.Backdrop.enabled = MissileCameraHudConfig.LabelBackgroundAlpha > 0.001f;
            HudBackdropHelper.StretchToBlock(block.Backdrop);
            return Mathf.Max(maxSoFar, width);
        }

        private void SetTelemetryBackdropMode(bool perRow)
        {
            _rng.Backdrop.enabled = perRow && MissileCameraHudConfig.LabelBackgroundAlpha > 0.001f;
            _alt.Backdrop.enabled = perRow && MissileCameraHudConfig.LabelBackgroundAlpha > 0.001f;
            _spd.Backdrop.enabled = perRow && MissileCameraHudConfig.LabelBackgroundAlpha > 0.001f;
            _telemetryStackBackdrop.enabled = !perRow && MissileCameraHudConfig.LabelBackgroundAlpha > 0.001f;
        }

        private void ReparentTelemetryToBottomBand()
        {
            _rng.Root.SetParent(_bottomBand, false);
            _alt.Root.SetParent(_bottomBand, false);
            _spd.Root.SetParent(_bottomBand, false);
        }

        private void SetTelemetryAlignment(TextAnchor alignment)
        {
            _rng.Label.alignment = alignment;
            _alt.Label.alignment = alignment;
            _spd.Label.alignment = alignment;
        }

        private static void PlaceTopBlock(HudBlock block, float xMin, float xMax, float height, float yFromTop)
        {
            RectTransform rt = block.Root;
            rt.anchorMin = new Vector2(xMin, 1f);
            rt.anchorMax = new Vector2(xMax, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -yFromTop);
            rt.offsetMin = new Vector2(0f, -height);
            rt.offsetMax = Vector2.zero;
            ApplyTextStretch(block.Label);
        }

        private static void PlaceStackRowRight(HudBlock block, float yMin, float yMax, float width, float height)
        {
            RectTransform rt = block.Root;
            rt.anchorMin = new Vector2(1f, yMin);
            rt.anchorMax = new Vector2(1f, yMax);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(width, 0f);
            ApplyTextStretch(block.Label);
        }

        private static void PlaceBottomBlock(HudBlock block, float xMin, float xMax, float height)
        {
            RectTransform rt = block.Root;
            rt.anchorMin = new Vector2(xMin, 0f);
            rt.anchorMax = new Vector2(xMax, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(0f, height);
            ApplyTextStretch(block.Label);
        }

        private static void PlaceCenterTopBlock(HudBlock block, float width, float height, float yFromTop)
        {
            RectTransform rt = block.Root;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -yFromTop);
            rt.sizeDelta = new Vector2(width, height);
            ApplyTextStretch(block.Label);
        }

        private static void ApplyTextStretch(Text text)
        {
            RectTransform rt = text.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            float pad = MissileCameraPanelMetrics.RowEdgePad;
            rt.offsetMin = new Vector2(pad, 0f);
            rt.offsetMax = new Vector2(-pad, 0f);
        }

        private static RectTransform CreateBand(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static HudBlock CreateBlock(RectTransform parent, string name, TextAnchor alignment)
        {
            var rootGo = new GameObject(name, typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            RectTransform root = rootGo.GetComponent<RectTransform>();

            Image backdrop = HudBackdropHelper.CreateBackdrop(root, name + "Backdrop");
            HudBackdropHelper.StretchToBlock(backdrop);

            var textGo = new GameObject(name + "Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(root, false);
            Text text = textGo.GetComponent<Text>();
            text.alignment = alignment;
            ApplyTextStretch(text);

            return new HudBlock(root, backdrop, text);
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
