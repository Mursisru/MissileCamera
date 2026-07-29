using System;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// Fullscreen FLIR chrome — all labels live (grid, LRF, HDIR, FOC/EXP, IP-RA, TRK, SLAVE).
    /// Caller updates every frame when fullscreen.
    /// </summary>
    internal sealed class MissileCameraFlirHud
    {
        private static readonly Color FlirGreen = new Color(0.55f, 1f, 0.9f, 1f);
        internal static Color MarkerColor => FlirGreen;

        private const int CompassTickCount = 32;
        private const int CompassMarkLabelCount = 9;
        private const int DialTickCount = 12;
        private const float CompassSpanDeg = 90f;
        private const float CompassMinorStepDeg = 5f;
        private const float CompassMajorStepDeg = 30f;
        private const float CompassSmoothTime = 0.14f;
        private const float CompassSnapDeltaDeg = 90f;
        private const float ZoomAutoEpsilon = 0.05f;

        private readonly RectTransform _root;
        private readonly Text _sys;
        private readonly MissileCameraFlirPanel _mslPanel;
        private readonly MissileCameraFlirPanel _launchPanel;
        private readonly MissileCameraFlirPanel _tgtTrackPanel;
        private readonly MissileCameraFlirPanel _tgtEngagePanel;
        private readonly MissileCameraFlirPanel _sensorPanel;
        private readonly MissileCameraFlirPanel _guidancePanel;
        private readonly Text _headingBig;
        private readonly Text[] _compassMarks;
        private readonly Text _lrf;
        private readonly Text _dialPitchLabel;
        private readonly Text _dialHdgLabel;
        private readonly MissileCameraFlirPanel _mslKinPanel;
        private readonly Text _northArrow;
        private readonly Text _sliderLabel;
        private readonly HudLineGraphic _crossL;
        private readonly HudLineGraphic _crossR;
        private readonly HudLineGraphic _crossU;
        private readonly HudLineGraphic _crossD;
        private readonly HudLineGraphic[] _compassTicks;
        private readonly HudLineGraphic _compassCenter;
        private readonly HudLineGraphic _sliderRail;
        private readonly HudLineGraphic _sliderPointer;
        private readonly HudRingGraphic _dialLeftRing;
        private readonly HudRingGraphic _dialRightRing;
        private readonly HudLineGraphic[] _dialLeftTicks;
        private readonly HudLineGraphic[] _dialRightTicks;
        private readonly HudLineGraphic _dialLeftNeedle;
        private readonly HudLineGraphic _dialRightNeedle;
        private readonly HudLineGraphic _dialFrameT;
        private readonly HudLineGraphic _dialFrameB;
        private readonly HudLineGraphic _dialFrameL;
        private readonly HudLineGraphic _dialFrameR;
        private readonly MissileCameraFlirGaugeBars _gaugeBars;
        private readonly MissileCameraFlirOwnshipPip _ownshipPip;
        private float _layoutW = -1f;
        private float _layoutH = -1f;
        private float _smoothHeading;
        private float _headingVel;
        private bool _headingReady;
        private readonly StringBuilder _sb = new StringBuilder(192);
        private float _lastNorthHdg = float.NaN;
        private float _lastDialPitch = float.NaN;
        private float _lastDialHdg = float.NaN;
        private Vector2 _lastDialLeftCenter;
        private Vector2 _lastDialRightCenter;
        private float _lastDialRadius = -1f;
        private bool _northLabelReady;
        private int _activeCompassTicks;
        private int _activeCompassMarks;
        private int _lastHdgInt = int.MinValue;
        private string _cachedHdgBig = string.Empty;

        private MissileCameraFlirHud(
            RectTransform root,
            Text sys,
            MissileCameraFlirPanel mslPanel,
            MissileCameraFlirPanel launchPanel,
            MissileCameraFlirPanel tgtTrackPanel,
            MissileCameraFlirPanel tgtEngagePanel,
            MissileCameraFlirPanel sensorPanel,
            MissileCameraFlirPanel guidancePanel,
            Text headingBig,
            Text[] compassMarks,
            Text lrf,
            Text dialPitchLabel,
            Text dialHdgLabel,
            MissileCameraFlirPanel mslKinPanel,
            Text northArrow,
            Text sliderLabel,
            HudLineGraphic crossL,
            HudLineGraphic crossR,
            HudLineGraphic crossU,
            HudLineGraphic crossD,
            HudLineGraphic[] compassTicks,
            HudLineGraphic compassCenter,
            HudLineGraphic sliderRail,
            HudLineGraphic sliderPointer,
            HudRingGraphic dialLeftRing,
            HudRingGraphic dialRightRing,
            HudLineGraphic[] dialLeftTicks,
            HudLineGraphic[] dialRightTicks,
            HudLineGraphic dialLeftNeedle,
            HudLineGraphic dialRightNeedle,
            HudLineGraphic dialFrameT,
            HudLineGraphic dialFrameB,
            HudLineGraphic dialFrameL,
            HudLineGraphic dialFrameR,
            MissileCameraFlirGaugeBars gaugeBars,
            MissileCameraFlirOwnshipPip ownshipPip)
        {
            _root = root;
            _sys = sys;
            _mslPanel = mslPanel;
            _launchPanel = launchPanel;
            _tgtTrackPanel = tgtTrackPanel;
            _tgtEngagePanel = tgtEngagePanel;
            _sensorPanel = sensorPanel;
            _guidancePanel = guidancePanel;
            _headingBig = headingBig;
            _compassMarks = compassMarks;
            _lrf = lrf;
            _dialPitchLabel = dialPitchLabel;
            _dialHdgLabel = dialHdgLabel;
            _mslKinPanel = mslKinPanel;
            _northArrow = northArrow;
            _sliderLabel = sliderLabel;
            _crossL = crossL;
            _crossR = crossR;
            _crossU = crossU;
            _crossD = crossD;
            _compassTicks = compassTicks;
            _compassCenter = compassCenter;
            _sliderRail = sliderRail;
            _sliderPointer = sliderPointer;
            _dialLeftRing = dialLeftRing;
            _dialRightRing = dialRightRing;
            _dialLeftTicks = dialLeftTicks;
            _dialRightTicks = dialRightTicks;
            _dialLeftNeedle = dialLeftNeedle;
            _dialRightNeedle = dialRightNeedle;
            _dialFrameT = dialFrameT;
            _dialFrameB = dialFrameB;
            _dialFrameL = dialFrameL;
            _dialFrameR = dialFrameR;
            _gaugeBars = gaugeBars;
            _ownshipPip = ownshipPip;
        }

        internal static MissileCameraFlirHud Create(RectTransform parent)
        {
            var rootGo = new GameObject("MissileCameraFlirHud", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            RectTransform root = rootGo.GetComponent<RectTransform>();
            Stretch(root);

            return new MissileCameraFlirHud(
                root,
                CreateLabel(root, "FlirSys", TextAnchor.UpperLeft),
                MissileCameraFlirPanel.Create(root, "FlirMslPanel", TextAnchor.UpperLeft),
                MissileCameraFlirPanel.Create(root, "FlirLaunchPanel", TextAnchor.UpperLeft),
                MissileCameraFlirPanel.Create(root, "FlirTgtTrackPanel", TextAnchor.UpperRight),
                MissileCameraFlirPanel.Create(root, "FlirTgtEngagePanel", TextAnchor.UpperRight),
                MissileCameraFlirPanel.Create(root, "FlirSensorPanel", TextAnchor.UpperLeft),
                MissileCameraFlirPanel.Create(root, "FlirGuidancePanel", TextAnchor.UpperRight),
                CreateLabel(root, "FlirHdgBig", TextAnchor.UpperCenter),
                CreateCenterLabels(root, "FlirCompassMark", CompassMarkLabelCount),
                CreateLabel(root, "FlirLrf", TextAnchor.MiddleLeft),
                CreateLabel(root, "FlirDialPitch", TextAnchor.LowerCenter),
                CreateLabel(root, "FlirDialHdg", TextAnchor.LowerCenter),
                MissileCameraFlirPanel.Create(root, "FlirMslKinPanel", TextAnchor.MiddleCenter),
                CreateLabel(root, "FlirNorth", TextAnchor.MiddleLeft),
                CreateLabel(root, "FlirSliderLbl", TextAnchor.LowerLeft),
                CreateLine(root, "FlirCrossL"),
                CreateLine(root, "FlirCrossR"),
                CreateLine(root, "FlirCrossU"),
                CreateLine(root, "FlirCrossD"),
                CreateLines(root, "FlirCompassTick", CompassTickCount),
                CreateLine(root, "FlirCompassCenter"),
                CreateLine(root, "FlirSliderRail"),
                CreateLine(root, "FlirSliderPtr"),
                CreateRing(root, "FlirDialLRing"),
                CreateRing(root, "FlirDialRRing"),
                CreateLines(root, "FlirDialLTick", DialTickCount),
                CreateLines(root, "FlirDialRTick", DialTickCount),
                CreateLine(root, "FlirDialLNeedle"),
                CreateLine(root, "FlirDialRNeedle"),
                CreateLine(root, "FlirDialFrameT"),
                CreateLine(root, "FlirDialFrameB"),
                CreateLine(root, "FlirDialFrameL"),
                CreateLine(root, "FlirDialFrameR"),
                MissileCameraFlirGaugeBars.Create(root),
                MissileCameraFlirOwnshipPip.Create(root));
        }

        internal void InvalidateLayout()
        {
            _layoutW = -1f;
            _layoutH = -1f;
            _lastDialRadius = -1f;
        }

        internal RectTransform Root => _root;

        internal void SetVisible(bool visible)
        {
            if (!visible)
            {
                _headingReady = false;
                _ownshipPip.Hide();
            }

            _root.gameObject.SetActive(visible);
        }

        internal void Shutdown() => _ownshipPip.Shutdown();

        internal void Update(MissileCameraHudSnapshot snapshot, MissileCameraPanelMetrics panel)
        {
            bool panelChanged = !Mathf.Approximately(panel.Width, _layoutW)
                || !Mathf.Approximately(panel.Height, _layoutH);

            if (panelChanged)
            {
                _layoutW = panel.Width;
                _layoutH = panel.Height;
                ApplyLayout(panel);
                ApplyFonts();
                ApplyColors();
                ApplyStaticMarks(panel);
            }

            ApplyContent(snapshot, panel);
            _gaugeBars.Update(snapshot, panel);
            _ownshipPip.Update();
        }

        internal void UpdateGaugeBarsOnly(MissileCameraHudSnapshot snapshot, MissileCameraPanelMetrics panel)
        {
            // Used during fullscreen boot: keep only FUEL/THR bars live without updating the whole FLIR chrome.
            _gaugeBars.Update(snapshot, panel);
        }

        private float SmoothHeading(float targetDeg)
        {
            targetDeg = Mathf.Repeat(targetDeg, 360f);
            if (!_headingReady
                || Mathf.Abs(Mathf.DeltaAngle(_smoothHeading, targetDeg)) > CompassSnapDeltaDeg)
            {
                _smoothHeading = targetDeg;
                _headingVel = 0f;
                _headingReady = true;
                return _smoothHeading;
            }

            _smoothHeading = Mathf.SmoothDampAngle(
                _smoothHeading,
                targetDeg,
                ref _headingVel,
                CompassSmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime);
            return Mathf.Repeat(_smoothHeading, 360f);
        }

        private void ApplyContent(MissileCameraHudSnapshot snapshot, MissileCameraPanelMetrics panel)
        {
            DateTime utc = DateTime.UtcNow;
            float ownHdg = SmoothHeading(snapshot.MissileHeadingDeg);
            float pitch = snapshot.PitchDeg;
            float fov = snapshot.FeedFovDeg > 0.1f ? snapshot.FeedFovDeg : 60f;
            float mag = snapshot.BaseFovDeg > 0.1f ? snapshot.BaseFovDeg / fov : 1f;
            int hdgInt = Mathf.RoundToInt(ownHdg);
            int pitchInt = Mathf.RoundToInt(pitch);
            int fovInt = Mathf.RoundToInt(fov);

            if (hdgInt != _lastHdgInt)
            {
                _lastHdgInt = hdgInt;
                _cachedHdgBig = hdgInt.ToString(CultureInfo.InvariantCulture) + "°T";
            }

            SetTextIfChanged(_headingBig, _cachedHdgBig);
            UpdateCompassTape(panel, ownHdg);
            UpdateAzimuthSlider(panel, ownHdg, snapshot);
            UpdateGimbalDials(panel, pitch, ownHdg);

            if (!_northLabelReady)
            {
                _northArrow.text = "-N->";
                _northLabelReady = true;
            }

            if (float.IsNaN(_lastNorthHdg) || Mathf.Abs(Mathf.DeltaAngle(_lastNorthHdg, ownHdg)) > 0.05f)
            {
                _lastNorthHdg = ownHdg;
                _northArrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, -ownHdg);
            }

            int channel = Mathf.Max(1, snapshot.SalvoIndex + 183);
            int salvoTotal = Mathf.Max(1, snapshot.SalvoTotal);
            _sb.Length = 0;
            _sb.Append("FLIR SYSTEMS ").Append(channel.ToString(CultureInfo.InvariantCulture))
                .Append("  CH").Append((snapshot.SalvoIndex + 1).ToString(CultureInfo.InvariantCulture))
                .Append('/').Append(salvoTotal.ToString(CultureInfo.InvariantCulture));
            SetTextIfChanged(_sys, _sb);

            string grid = string.IsNullOrEmpty(snapshot.GridText) ? "GRID ---" : snapshot.GridText;
            string mslName = string.IsNullOrEmpty(snapshot.MissileName) ? "MSL ---" : snapshot.MissileName;
            string spd = StripPrefix(snapshot.TgpSpdText, "SPD ");
            if (string.IsNullOrEmpty(spd))
                spd = "---";

            string guide = snapshot.Guidance switch
            {
                MissileGuidanceStatus.Guided => "GUIDED",
                MissileGuidanceStatus.LostLock => "LOST LOCK",
                _ => "BALLISTIC"
            };

            _mslPanel.SetTitle("MSL");
            _sb.Length = 0;
            // Most important first: guidance/mode + range/altitude, then kinematics.
            _sb.Append("MODE ").Append(guide)
                .Append("\nRNG  ").Append(
                    snapshot.HasTarget
                        ? FormatRangeMeters(snapshot.TargetRangeMeters)
                        : "---")
                .Append("\nALT  ").Append(FormatAltitudeFlir(snapshot))
                .Append("\nSPD  ").Append(spd)
                .Append("\nHDG  ").Append(hdgInt.ToString(CultureInfo.InvariantCulture)).Append("°T")
                .Append("\nMACH ").Append(snapshot.Mach.ToString("0.00", CultureInfo.InvariantCulture))
                .Append("\nPIT  ").Append(pitchInt.ToString(CultureInfo.InvariantCulture)).Append('°')
                .Append("\nROL  ").Append(Mathf.RoundToInt(snapshot.RollDeg).ToString(CultureInfo.InvariantCulture)).Append('°')
                .Append("\nG    ").Append(snapshot.InstantG.ToString("0.0", CultureInfo.InvariantCulture))
                .Append('\n').Append(mslName)
                .Append('\n').Append(grid);
            _mslPanel.SetBody(_sb);

            string plat = string.IsNullOrEmpty(snapshot.OwnshipName) || snapshot.OwnshipName == "---"
                ? "---"
                : snapshot.OwnshipName;
            _launchPanel.SetTitle("LAUNCH");
            _sb.Length = 0;
            _sb.Append("PLAT ").Append(plat)
                .Append("\nSALVO ").Append((snapshot.SalvoIndex + 1).ToString(CultureInfo.InvariantCulture))
                .Append('/').Append(salvoTotal.ToString(CultureInfo.InvariantCulture))
                .Append("\nDATE ").Append(utc.ToString("MM/dd/yy", CultureInfo.InvariantCulture))
                .Append("\nTIME ").Append(utc.ToString("HH:mm:ss", CultureInfo.InvariantCulture)).Append(" Z");
            _launchPanel.SetBody(_sb);

            if (snapshot.HasTarget)
            {
                string tgtAlt = UnitConverter.AltitudeReading(snapshot.TargetPosition.y);
                int brgInt = Mathf.RoundToInt(snapshot.TargetBearingDeg);
                string tgtHdg = StripPrefix(snapshot.TgpHdgText.Replace("°", "°T"), "HDG ");
                _tgtTrackPanel.SetTitle("TGT TRACK");
                _sb.Length = 0;
                // Most important first: bearing + relative geometry, then target kinematics.
                _sb.Append("BRG  ").Append(brgInt.ToString(CultureInfo.InvariantCulture)).Append("°T")
                    .Append('\n').Append(snapshot.TgpRelText)
                    .Append("\nALT  ").Append(tgtAlt)
                    .Append("\nSPD  ").Append(StripPrefix(snapshot.TgpTargetSpdText, "SPD "))
                    .Append("\nHDG  ").Append(tgtHdg)
                    .Append('\n').Append(snapshot.TargetName)
                    .Append('\n').Append(snapshot.TargetGridText);
                _tgtTrackPanel.SetBody(_sb);

                _tgtEngagePanel.SetTitle("TGT ENGAGE");
                _sb.Length = 0;
                if (!string.IsNullOrEmpty(snapshot.TgpTtiText))
                    _sb.Append("TTI  ").Append(snapshot.TgpTtiText);
                else
                    _sb.Append("TTI  ---");

                // G-LIM instead of CLOS — stable limit for useful "max G" cue.
                _sb.Append("\nG-LIM ").Append(snapshot.GLimit.ToString("0.0", CultureInfo.InvariantCulture))
                    .Append("\nLRF  ").Append(FormatRangeMeters(snapshot.TargetRangeMeters))
                    .Append("\nΔBRG ").Append(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0:+0;-0}°",
                            Mathf.DeltaAngle(ownHdg, snapshot.TargetBearingDeg)))
                    .Append("\nANG  ").Append(snapshot.TargetAngleDeg.ToString("0.0", CultureInfo.InvariantCulture)).Append('°')
                    .Append('\n').Append(snapshot.TgpRidText)
                    .Append("\nSLT  ").Append(StripPrefix(snapshot.TgpRngText, "RNG "));
                _tgtEngagePanel.SetBody(_sb);
            }
            else
            {
                _tgtTrackPanel.SetTitle("TGT TRACK");
                _tgtTrackPanel.SetBody("NO TRACK\nBRG  ---\nREL  ---\nALT  ---\nSPD  ---\nHDG  ---\nGRID ---");
                _tgtEngagePanel.SetTitle("TGT ENGAGE");
                _tgtEngagePanel.SetBody("TTI  ---\nG-LIM ---\nLRF  ---\nΔBRG ---\nANG  ---\nRID  ---\nSLT  ---");
            }

            if (_lrf.gameObject.activeSelf)
                _lrf.gameObject.SetActive(false);

            MissileCameraVisionMode vision = MissileCameraVisionModeController.Mode;
            string polarity = MissileCameraVisionModeController.FlirPolarityLabel(vision);
            _sensorPanel.SetTitle("SENSOR");
            _sb.Length = 0;
            _sb.Append("HDIR ").Append(hdgInt.ToString(CultureInfo.InvariantCulture)).Append("°T")
                .Append('\n').Append(polarity);
            if (mag <= 1f + ZoomAutoEpsilon)
                _sb.Append("\nFOC  AUTO");
            else
                _sb.Append("\nFOC  MAN x").Append(mag.ToString("0.0", CultureInfo.InvariantCulture));
            _sb.Append("\nMAG  x").Append(mag.ToString("0.0", CultureInfo.InvariantCulture))
                .Append("\nFOV  ").Append(fovInt.ToString(CultureInfo.InvariantCulture)).Append('°');
            if (MissileCameraVisionModeController.UsesInfraredBlit(vision))
                _sb.Append("\nEXP  ").Append(snapshot.InfraredExposure.ToString("0.00", CultureInfo.InvariantCulture));
            else if (MissileCameraVisionModeController.UsesNightVisionVolume(vision))
                _sb.Append("\nEXP  NVG");
            else
                _sb.Append("\nEXP  DAY");
            _sensorPanel.SetBody(_sb);

            _guidancePanel.SetTitle("GUIDANCE");
            _sb.Length = 0;
            // Most important first: guidance mode + tracking state.
            _sb.Append("MODE ").Append(guide);

            if (snapshot.Guidance == MissileGuidanceStatus.LostLock)
                _sb.Append("\nTRK  LOST");
            else if (snapshot.Guidance == MissileGuidanceStatus.Guided)
                _sb.Append("\nTRK  ON");
            else
                _sb.Append("\nTRK  OFF");

            if (snapshot.Guidance == MissileGuidanceStatus.LostLock)
                _sb.Append("\nSLAVE LOST");
            else if (snapshot.HasTarget && snapshot.Guidance == MissileGuidanceStatus.Guided)
                _sb.Append("\nSLAVE READY");
            else
                _sb.Append("\nSLAVE IDLE");

            if (snapshot.HasAimPoint)
            {
                _sb.Append("\nIP-RA ")
                    .Append(snapshot.TargetAngleDeg.ToString("0.0", CultureInfo.InvariantCulture)).Append('°')
                    .Append("\nREL  ").Append(snapshot.RelativeAltitudeMeters.ToString("0", CultureInfo.InvariantCulture)).Append('m');
            }
            else
                _sb.Append("\nIP-RA OFF");

            _sb.Append("\nINS  ").Append(snapshot.TargetAngleDeg.ToString("0.00", CultureInfo.InvariantCulture)).Append('°');
            _guidancePanel.SetBody(_sb);

            _sb.Length = 0;
            _sb.Append("PIT\n").Append(pitchInt.ToString(CultureInfo.InvariantCulture)).Append('°');
            SetTextIfChanged(_dialPitchLabel, _sb);
            _sb.Length = 0;
            _sb.Append("HDG\n").Append(hdgInt.ToString(CultureInfo.InvariantCulture)).Append('°');
            SetTextIfChanged(_dialHdgLabel, _sb);

            _mslKinPanel.SetTitle("MSL KIN");
            _sb.Length = 0;
            _sb.Append("SPD  ").Append(spd)
                .Append("\nMACH ").Append(snapshot.Mach.ToString("0.00", CultureInfo.InvariantCulture))
                .Append("\nG    ").Append(snapshot.InstantG.ToString("0.0", CultureInfo.InvariantCulture))
                .Append("\nALT  ").Append(FormatAltitudeFlir(snapshot));
            _mslKinPanel.SetBody(_sb);
        }

        private static string StripPrefix(string? value, string prefix)
        {
            if (value == null || value.Length == 0)
                return "---";
            if (value.StartsWith(prefix, StringComparison.Ordinal) && value.Length > prefix.Length)
                return value.Substring(prefix.Length);
            return value;
        }

        private static void SetTextIfChanged(Text text, string value)
        {
            if (text.text != value)
                text.text = value;
        }

        private void SetTextIfChanged(Text text, StringBuilder sb)
        {
            string value = sb.ToString();
            if (text.text != value)
                text.text = value;
        }

        private static string FormatRangeMeters(float rangeM)
        {
            if (rangeM < 0.5f)
                return "---";
            if (rangeM >= 1000f)
                return string.Format(CultureInfo.InvariantCulture, "{0:F1}km", rangeM * 0.001f);
            return string.Format(CultureInfo.InvariantCulture, "{0:F0}m", rangeM);
        }

        private static string FormatAltitudeFlir(MissileCameraHudSnapshot snapshot)
        {
            string raw = snapshot.TgpAltText;
            if (raw.StartsWith("ALT ", StringComparison.Ordinal) && raw.Length > 4)
                return raw.Substring(4).ToUpperInvariant();
            return "---";
        }

        private static string FormatCompassMark(float deg)
        {
            float d = Mathf.Repeat(deg, 360f);
            if (d > 359.5f)
                d = 0f;

            if (Mathf.Abs(d) < 0.5f || Mathf.Abs(d - 360f) < 0.5f)
                return "N";
            if (Mathf.Abs(d - 90f) < 0.5f)
                return "E";
            if (Mathf.Abs(d - 180f) < 0.5f)
                return "S";
            if (Mathf.Abs(d - 270f) < 0.5f)
                return "W";
            return ((int)Mathf.Round(d)).ToString(CultureInfo.InvariantCulture);
        }

        private static bool IsMajorMark(float deg)
        {
            float r = Mathf.Repeat(deg, CompassMajorStepDeg);
            return r < 0.01f || r > CompassMajorStepDeg - 0.01f;
        }

        private void ApplyStaticMarks(MissileCameraPanelMetrics panel)
        {
            float arm = Mathf.Clamp(panel.MinSide * 0.1f, 36f, 80f);
            float gap = 10f;
            float thick = 1.5f;
            _crossL.SetLine(new Vector2(-(arm + gap), 0f), new Vector2(-gap, 0f), thick, FlirGreen);
            _crossR.SetLine(new Vector2(gap, 0f), new Vector2(arm + gap, 0f), thick, FlirGreen);
            _crossU.SetLine(new Vector2(0f, gap), new Vector2(0f, arm + gap), thick, FlirGreen);
            _crossD.SetLine(new Vector2(0f, -(arm + gap)), new Vector2(0f, -gap), thick, FlirGreen);

            float caretY = panel.Height * 0.5f - panel.VerticalInset - 48f;
            _compassCenter.SetLine(new Vector2(0f, caretY - 8f), new Vector2(0f, caretY + 2f), 2.2f, FlirGreen);
        }

        /// <summary>
        /// Scrolling heading tape: ticks + N/E/S/W|deg marks share one px/deg mapping.
        /// </summary>
        private void UpdateCompassTape(MissileCameraPanelMetrics panel, float headingDeg)
        {
            float tapeW = Mathf.Clamp(panel.Width * 0.42f, 280f, 520f);
            float half = tapeW * 0.5f;
            float pxPerDeg = tapeW / CompassSpanDeg;
            float halfSpan = CompassSpanDeg * 0.5f;
            float yTick = panel.Height * 0.5f - panel.VerticalInset - 42f;
            float yMark = yTick - 16f;

            float first = Mathf.Floor((headingDeg - halfSpan) / CompassMinorStepDeg) * CompassMinorStepDeg;
            int tickIdx = 0;
            int markIdx = 0;

            for (float mark = first; mark <= headingDeg + halfSpan + CompassMinorStepDeg; mark += CompassMinorStepDeg)
            {
                float markWrapped = Mathf.Repeat(mark, 360f);
                float offset = Mathf.DeltaAngle(headingDeg, markWrapped);
                if (Mathf.Abs(offset) > halfSpan + 0.25f)
                    continue;

                float x = offset * pxPerDeg;
                if (x < -half - 1f || x > half + 1f)
                    continue;

                bool major = IsMajorMark(markWrapped);
                if (tickIdx < _compassTicks.Length)
                {
                    float tickH = major ? 11f : 5f;
                    _compassTicks[tickIdx].SetLine(
                        new Vector2(x, yTick),
                        new Vector2(x, yTick - tickH),
                        major ? 1.7f : 1.1f,
                        FlirGreen);
                    if (!_compassTicks[tickIdx].gameObject.activeSelf)
                        _compassTicks[tickIdx].gameObject.SetActive(true);
                    tickIdx++;
                }

                if (major && markIdx < _compassMarks.Length)
                {
                    Text label = _compassMarks[markIdx];
                    SetTextIfChanged(label, FormatCompassMark(markWrapped));
                    RectTransform rt = label.rectTransform;
                    rt.anchoredPosition = new Vector2(x, yMark);
                    if (!label.gameObject.activeSelf)
                    {
                        rt.anchorMin = new Vector2(0.5f, 0.5f);
                        rt.anchorMax = new Vector2(0.5f, 0.5f);
                        rt.pivot = new Vector2(0.5f, 1f);
                        rt.sizeDelta = new Vector2(48f, 18f);
                        label.alignment = TextAnchor.UpperCenter;
                        label.gameObject.SetActive(true);
                    }

                    markIdx++;
                }
            }

            for (int i = tickIdx; i < _activeCompassTicks; i++)
            {
                if (_compassTicks[i].gameObject.activeSelf)
                    _compassTicks[i].gameObject.SetActive(false);
            }

            for (int i = markIdx; i < _activeCompassMarks; i++)
            {
                if (_compassMarks[i].gameObject.activeSelf)
                    _compassMarks[i].gameObject.SetActive(false);
            }

            _activeCompassTicks = tickIdx;
            _activeCompassMarks = markIdx;
        }

        private void UpdateAzimuthSlider(MissileCameraPanelMetrics panel, float missileHdg, MissileCameraHudSnapshot snapshot)
        {
            float pad = panel.HorizontalInset;
            float y = -panel.Height * 0.5f + panel.VerticalInset + 36f;
            float left = -panel.Width * 0.5f + pad + 24f;
            float right = left + Mathf.Clamp(panel.Width * 0.22f, 140f, 240f);

            // Ownship PiP sits bottom-left; if the rail overlaps, shift it to the right just enough
            // so it remains in a "normal" HUD position and doesn't protrude beside the PiP.
            float pipSize = _ownshipPip.Size;
            if (pipSize > 0f && panel.Width > 0f)
            {
                float pipXPad = Mathf.Max(panel.HorizontalInset, 8f);
                float pipRightX = -panel.Width * 0.5f + pipXPad + pipSize;
                float minLeft = pipRightX + 10f;

                float railW = right - left;
                if (left < minLeft)
                {
                    float delta = minLeft - left;
                    // Keep inside the right-side padding.
                    float maxRight = panel.Width * 0.5f - pad - 10f;
                    float newRight = right + delta;
                    if (newRight <= maxRight)
                    {
                        left += delta;
                        right = newRight;
                    }
                    else
                    {
                        float maxDelta = maxRight - right;
                        if (maxDelta > 0f)
                        {
                            left += maxDelta;
                            right += maxDelta;
                        }
                    }
                }
            }

            _sliderRail.SetLine(new Vector2(left, y), new Vector2(right, y), 1.4f, FlirGreen);

            if (snapshot.HasTarget)
            {
                float brg = snapshot.TargetBearingDeg;
                float delta = Mathf.DeltaAngle(missileHdg, brg);
                float t = Mathf.Clamp01((delta + 90f) / 180f);
                float x = Mathf.Lerp(left, right, t);
                _sliderPointer.SetLine(new Vector2(x - 4f, y + 8f), new Vector2(x, y), 2f, FlirGreen);
                _sliderPointer.gameObject.SetActive(true);
                _sliderLabel.text = "W—N  TGT "
                    + string.Format(CultureInfo.InvariantCulture, "{0:F0}°", brg)
                    + " Δ" + string.Format(CultureInfo.InvariantCulture, "{0:+0;-0}°", delta);
            }
            else
            {
                float x = Mathf.Lerp(left, right, 0.5f);
                _sliderPointer.SetLine(new Vector2(x, y + 6f), new Vector2(x, y - 1f), 1.6f, FlirGreen);
                _sliderLabel.text = "W          N";
            }

            // Keep label X in sync with the shifted slider rail.
            float labelX = left + panel.Width * 0.5f - 4f;
            Place(_sliderLabel, 0f, 0f, labelX, panel.VerticalInset + 18f, 220f, 16f, TextAnchor.MiddleLeft);
        }

        private void UpdateGimbalDials(MissileCameraPanelMetrics panel, float pitchDeg, float headingDeg)
        {
            // Keep dials near the bottom of the screen (center X). Do NOT lift them for ownship PiP.
            float gap = 70f;
            float r = 28f;
            const float captionH = 30f;
            const float padX = 10f;
            const float padY = 8f;
            const float kinW = 250f;
            const float kinH = 78f;
            const float kinGap = 10f;

            float dialStackH = r * 2f + captionH + padY * 2f;
            float bottomSafe = Mathf.Max(panel.VerticalInset, 8f) + dialStackH * 0.5f + 8f;
            float cy = -panel.Height * 0.5f + bottomSafe;
            var leftCenter = new Vector2(-gap, cy);
            var rightCenter = new Vector2(gap, cy);

            bool layoutDirty = _lastDialRadius < 0f
                || !Mathf.Approximately(_lastDialRadius, r)
                || (leftCenter - _lastDialLeftCenter).sqrMagnitude > 0.01f
                || (rightCenter - _lastDialRightCenter).sqrMagnitude > 0.01f;

            bool pitchDirty = layoutDirty
                || float.IsNaN(_lastDialPitch)
                || Mathf.Abs(_lastDialPitch - pitchDeg) > 0.05f;
            bool hdgDirty = layoutDirty
                || float.IsNaN(_lastDialHdg)
                || Mathf.Abs(Mathf.DeltaAngle(_lastDialHdg, headingDeg)) > 0.05f;

            if (pitchDirty)
            {
                DrawDial(_dialLeftRing, _dialLeftTicks, _dialLeftNeedle, leftCenter, r, pitchDeg * 2f, layoutDirty);
                _lastDialPitch = pitchDeg;
            }

            if (hdgDirty)
            {
                DrawDial(_dialRightRing, _dialRightTicks, _dialRightNeedle, rightCenter, r, headingDeg, layoutDirty);
                _lastDialHdg = headingDeg;
            }

            if (layoutDirty)
            {
                _lastDialLeftCenter = leftCenter;
                _lastDialRightCenter = rightCenter;
                _lastDialRadius = r;
                PlaceDialCaption(_dialPitchLabel, -gap, cy - r - 2f);
                PlaceDialCaption(_dialHdgLabel, gap, cy - r - 2f);

                float left = -gap - r - padX;
                float right = gap + r + padX;
                float top = cy + r + padY;
                float bottom = cy - r - captionH - padY;
                const float thick = 1.5f;
                _dialFrameT.SetLine(new Vector2(left, top), new Vector2(right, top), thick, FlirGreen);
                _dialFrameB.SetLine(new Vector2(left, bottom), new Vector2(right, bottom), thick, FlirGreen);
                _dialFrameL.SetLine(new Vector2(left, bottom), new Vector2(left, top), thick, FlirGreen);
                _dialFrameR.SetLine(new Vector2(right, bottom), new Vector2(right, top), thick, FlirGreen);

                // Separate framed MSL KIN block directly above the dial frame.
                float kinCenterY = top + kinGap + kinH * 0.5f;
                _mslKinPanel.Place(
                    0.5f,
                    0.5f,
                    0f,
                    kinCenterY,
                    kinW,
                    kinH,
                    TextAnchor.MiddleCenter,
                    TextAnchor.MiddleCenter);
            }
        }

        private static void PlaceDialCaption(Text text, float x, float y)
        {
            RectTransform rt = text.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(80f, 36f);
            text.alignment = TextAnchor.UpperCenter;
        }

        private static void DrawDial(
            HudRingGraphic ring,
            HudLineGraphic[] ticks,
            HudLineGraphic needle,
            Vector2 center,
            float radius,
            float needleDeg,
            bool redrawTicks)
        {
            if (ring.transform is RectTransform ringRt)
                ringRt.anchoredPosition = center;
            ring.SetRing(radius, 1.6f, FlirGreen, filled: false);

            if (redrawTicks)
            {
                for (int i = 0; i < ticks.Length; i++)
                {
                    float ang = i * (360f / ticks.Length) * Mathf.Deg2Rad;
                    Vector2 dir = new Vector2(Mathf.Sin(ang), Mathf.Cos(ang));
                    ticks[i].SetLine(center + dir * (radius - 6f), center + dir * radius, 1.2f, FlirGreen);
                }
            }

            float n = needleDeg * Mathf.Deg2Rad;
            Vector2 nDir = new Vector2(Mathf.Sin(n), Mathf.Cos(n));
            needle.SetLine(center, center + nDir * (radius - 4f), 1.8f, FlirGreen);
        }

        private void ApplyLayout(MissileCameraPanelMetrics panel)
        {
            // Match all left/right chrome inset to the ownship PiP inset.
            float pad = Mathf.Max(panel.HorizontalInset, 8f);
            float vPad = Mathf.Max(panel.VerticalInset, 8f);
            float row = 15f;
            float colW = Mathf.Clamp(panel.Width * 0.24f, 180f, 280f);
            float stackGap = 4f;
            float mslH = row * 11.6f;
            float launchH = row * 5.0f;
            float tgtTrackH = row * 8.6f;
            float tgtEngageH = row * 8.6f;
            float sensorH = row * 7.6f;
            float guidanceH = row * 8.8f;
            float bottomY = vPad + 150f;

            Place(_sys, 0f, 1f, pad, -vPad, colW, row, TextAnchor.MiddleLeft);
            float yLeft = vPad + row + 2f;
            _mslPanel.Place(0f, 1f, pad, -yLeft, colW, mslH, TextAnchor.MiddleLeft, TextAnchor.UpperLeft);
            yLeft += mslH + stackGap;
            _launchPanel.Place(0f, 1f, pad, -yLeft, colW, launchH, TextAnchor.MiddleLeft, TextAnchor.UpperLeft);

            Place(_headingBig, 0.5f, 1f, 0f, -vPad, 140f, row + 4f, TextAnchor.MiddleCenter, center: true);

            float yRight = vPad;
            _tgtTrackPanel.Place(1f, 1f, -pad, -yRight, colW, tgtTrackH, TextAnchor.MiddleRight, TextAnchor.UpperRight);
            yRight += tgtTrackH + stackGap;
            _tgtEngagePanel.Place(1f, 1f, -pad, -yRight, colW, tgtEngageH, TextAnchor.MiddleRight, TextAnchor.UpperRight);

            Place(_lrf, 0f, 0.55f, pad, 40f, 36f, row, TextAnchor.MiddleLeft);
            Place(_northArrow, 0f, 0.58f, pad + 22f, 8f, 64f, row, TextAnchor.MiddleLeft);

            float pipPad = Mathf.Clamp(panel.HorizontalInset, 6f, 18f);
            _ownshipPip.Place(panel, pipPad);
            // Keep bottom-left blocks readable under potentially large PiP.
            float pipTopY = pipPad + _ownshipPip.Size;
            float sensorBottomY = Mathf.Max(bottomY, pipTopY + stackGap);
            _sensorPanel.Place(0f, 0f, pad, sensorBottomY, colW, sensorH, TextAnchor.MiddleLeft, TextAnchor.UpperLeft);
            _guidancePanel.Place(1f, 0f, -pad, bottomY, colW, guidanceH, TextAnchor.MiddleRight, TextAnchor.UpperRight);
        }

        private void ApplyFonts()
        {
            const int body = 12;
            const int title = 11;
            const int big = 18;
            const int kinBody = 15;
            Font font = HudFontHelper.GetFont();

            _sys.font = font;
            _sys.fontSize = body;
            _sys.horizontalOverflow = HorizontalWrapMode.Overflow;
            _sys.verticalOverflow = VerticalWrapMode.Truncate;
            _sys.raycastTarget = false;

            _mslPanel.ApplyFont(font, title, body);
            _launchPanel.ApplyFont(font, title, body);
            _tgtTrackPanel.ApplyFont(font, title, body);
            _tgtEngagePanel.ApplyFont(font, title, body);
            _sensorPanel.ApplyFont(font, title, body);
            _guidancePanel.ApplyFont(font, title, body);
            _mslKinPanel.ApplyFont(font, title, kinBody);

            foreach (Text t in new[] { _lrf, _dialPitchLabel, _dialHdgLabel, _northArrow, _sliderLabel })
            {
                t.font = font;
                t.fontSize = body;
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.verticalOverflow = VerticalWrapMode.Overflow;
                t.raycastTarget = false;
            }

            for (int i = 0; i < _compassMarks.Length; i++)
            {
                Text t = _compassMarks[i];
                t.font = font;
                t.fontSize = body;
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.verticalOverflow = VerticalWrapMode.Overflow;
                t.raycastTarget = false;
            }

            _headingBig.font = font;
            _headingBig.fontSize = big;
            _dialPitchLabel.alignment = TextAnchor.UpperCenter;
            _dialHdgLabel.alignment = TextAnchor.UpperCenter;
        }

        private void ApplyColors()
        {
            _sys.color = FlirGreen;
            _headingBig.color = FlirGreen;
            foreach (Text t in new[] { _lrf, _dialPitchLabel, _dialHdgLabel, _northArrow, _sliderLabel })
                t.color = FlirGreen;

            for (int i = 0; i < _compassMarks.Length; i++)
                _compassMarks[i].color = FlirGreen;
        }

        private static void Place(
            Text text,
            float anchorX,
            float anchorY,
            float x,
            float y,
            float w,
            float h,
            TextAnchor align,
            bool center = false)
        {
            RectTransform rt = text.rectTransform;
            rt.anchorMin = new Vector2(anchorX, anchorY);
            rt.anchorMax = new Vector2(anchorX, anchorY);
            rt.pivot = center ? new Vector2(0.5f, 1f) : new Vector2(anchorX, anchorY);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            text.alignment = align;
        }

        private static Text CreateLabel(RectTransform parent, string name, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.alignment = alignment;
            text.color = FlirGreen;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(0.7f, 0.7f);
            text.raycastTarget = false;
            return text;
        }

        private static Text[] CreateCenterLabels(RectTransform parent, string prefix, int count)
        {
            var labels = new Text[count];
            for (int i = 0; i < count; i++)
            {
                Text t = CreateLabel(parent, prefix + i, TextAnchor.UpperCenter);
                RectTransform rt = t.rectTransform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 1f);
                t.gameObject.SetActive(false);
                labels[i] = t;
            }

            return labels;
        }

        private static HudLineGraphic CreateLine(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HudLineGraphic));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            return go.GetComponent<HudLineGraphic>();
        }

        private static HudLineGraphic[] CreateLines(RectTransform parent, string prefix, int count)
        {
            var lines = new HudLineGraphic[count];
            for (int i = 0; i < count; i++)
                lines[i] = CreateLine(parent, prefix + i);
            return lines;
        }

        private static HudRingGraphic CreateRing(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HudRingGraphic));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            return go.GetComponent<HudRingGraphic>();
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
