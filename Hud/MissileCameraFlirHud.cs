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
        private static readonly Color FlirGreen = new Color(0.2f, 1f, 0.45f, 1f);
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
        private readonly Text _coord;
        private readonly Text _ownTelemetry;
        private readonly Text _alt;
        private readonly Text _date;
        private readonly Text _time;
        private readonly Text _headingBig;
        private readonly Text[] _compassMarks;
        private readonly Text _tgtCoord;
        private readonly Text _tgtTelemetry;
        private readonly Text _tgtElv;
        private readonly Text _lrf;
        private readonly Text _modes;
        private readonly Text _status;
        private readonly Text _magRid;
        private readonly Text _dialPitchLabel;
        private readonly Text _dialHdgLabel;
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
            Text coord,
            Text ownTelemetry,
            Text alt,
            Text date,
            Text time,
            Text headingBig,
            Text[] compassMarks,
            Text tgtCoord,
            Text tgtTelemetry,
            Text tgtElv,
            Text lrf,
            Text modes,
            Text status,
            Text magRid,
            Text dialPitchLabel,
            Text dialHdgLabel,
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
            HudLineGraphic dialRightNeedle)
        {
            _root = root;
            _sys = sys;
            _coord = coord;
            _ownTelemetry = ownTelemetry;
            _alt = alt;
            _date = date;
            _time = time;
            _headingBig = headingBig;
            _compassMarks = compassMarks;
            _tgtCoord = tgtCoord;
            _tgtTelemetry = tgtTelemetry;
            _tgtElv = tgtElv;
            _lrf = lrf;
            _modes = modes;
            _status = status;
            _magRid = magRid;
            _dialPitchLabel = dialPitchLabel;
            _dialHdgLabel = dialHdgLabel;
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
                CreateLabel(root, "FlirCoord", TextAnchor.UpperLeft),
                CreateLabel(root, "FlirOwnTel", TextAnchor.UpperLeft),
                CreateLabel(root, "FlirAlt", TextAnchor.UpperLeft),
                CreateLabel(root, "FlirDate", TextAnchor.UpperLeft),
                CreateLabel(root, "FlirTime", TextAnchor.UpperLeft),
                CreateLabel(root, "FlirHdgBig", TextAnchor.UpperCenter),
                CreateCenterLabels(root, "FlirCompassMark", CompassMarkLabelCount),
                CreateLabel(root, "FlirTgtCoord", TextAnchor.UpperRight),
                CreateLabel(root, "FlirTgtTel", TextAnchor.UpperRight),
                CreateLabel(root, "FlirTgtElv", TextAnchor.UpperRight),
                CreateLabel(root, "FlirLrf", TextAnchor.MiddleLeft),
                CreateLabel(root, "FlirModes", TextAnchor.LowerLeft),
                CreateLabel(root, "FlirStatus", TextAnchor.LowerRight),
                CreateLabel(root, "FlirMagRid", TextAnchor.LowerLeft),
                CreateLabel(root, "FlirDialPitch", TextAnchor.LowerCenter),
                CreateLabel(root, "FlirDialHdg", TextAnchor.LowerCenter),
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
                CreateLine(root, "FlirDialRNeedle"));
        }

        internal void InvalidateLayout()
        {
            _layoutW = -1f;
            _layoutH = -1f;
        }

        internal RectTransform Root => _root;

        internal void SetVisible(bool visible)
        {
            if (!visible)
                _headingReady = false;
            _root.gameObject.SetActive(visible);
        }

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

            SetTextIfChanged(_coord, "— MSL —");
            _sb.Length = 0;
            _sb.Append(mslName).Append('\n').Append(grid)
                .Append("\nSPD ").Append(spd)
                .Append("  HDG ").Append(hdgInt.ToString(CultureInfo.InvariantCulture)).Append("°T")
                .Append("\nALT ").Append(FormatAltitudeFlir(snapshot))
                .Append("  G ").Append(snapshot.InstantG.ToString("0.0", CultureInfo.InvariantCulture));
            SetTextIfChanged(_ownTelemetry, _sb);

            string plat = string.IsNullOrEmpty(snapshot.OwnshipName) || snapshot.OwnshipName == "---"
                ? "PLAT ---"
                : "PLAT " + snapshot.OwnshipName;
            _sb.Length = 0;
            _sb.Append("MACH ").Append(snapshot.Mach.ToString("0.00", CultureInfo.InvariantCulture))
                .Append("  FUEL ").Append(Mathf.RoundToInt(snapshot.FuelFraction * 100f).ToString(CultureInfo.InvariantCulture)).Append('%')
                .Append('\n').Append(plat)
                .Append("\nSALVO ").Append((snapshot.SalvoIndex + 1).ToString(CultureInfo.InvariantCulture))
                .Append('/').Append(salvoTotal.ToString(CultureInfo.InvariantCulture));
            SetTextIfChanged(_alt, _sb);

            SetTextIfChanged(_date, utc.ToString("MM/dd/yy", CultureInfo.InvariantCulture));
            _sb.Length = 0;
            _sb.Append(utc.ToString("HH:mm:ss", CultureInfo.InvariantCulture)).Append(" Z");
            SetTextIfChanged(_time, _sb);

            if (snapshot.HasTarget)
            {
                string tgtAlt = UnitConverter.AltitudeReading(snapshot.TargetPosition.y);
                _sb.Length = 0;
                _sb.Append("— TGT —\n").Append(snapshot.TargetName);
                SetTextIfChanged(_tgtCoord, _sb);

                _sb.Length = 0;
                _sb.Append(snapshot.TargetGridText)
                    .Append("\nSPD ").Append(StripPrefix(snapshot.TgpTargetSpdText, "SPD "))
                    .Append("  ").Append(snapshot.TgpHdgText.Replace("°", "°T"))
                    .Append("\nALT ").Append(tgtAlt)
                    .Append("  ").Append(snapshot.TgpRelText);
                SetTextIfChanged(_tgtTelemetry, _sb);

                _sb.Length = 0;
                _sb.Append("SLT ").Append(StripPrefix(snapshot.TgpRngText, "RNG "))
                    .Append("  ").Append(FormatClosSafe(snapshot.ClosingSpeedMs)).Append('\n');
                if (snapshot.HasTimeToImpact)
                    _sb.Append("TTI ").Append(snapshot.TimeToImpactSec.ToString("0.0", CultureInfo.InvariantCulture)).Append('s');
                else
                    _sb.Append("TTI ---");
                _sb.Append("  LRF ").Append(FormatRangeMeters(snapshot.TargetRangeMeters))
                    .Append('\n').Append(snapshot.TgpRidText)
                    .Append("  ANG ").Append(snapshot.TargetAngleDeg.ToString("0.0", CultureInfo.InvariantCulture)).Append('°');
                SetTextIfChanged(_tgtElv, _sb);
            }
            else
            {
                SetTextIfChanged(_tgtCoord, "— TGT —\nNO TRACK");
                SetTextIfChanged(_tgtTelemetry, "GRID ---\nSPD ---  HDG ---°T\nALT ---  REL ---");
                SetTextIfChanged(_tgtElv, "SLT ---  CLOS ---\nTTI ---  LRF ---\nRID ---  ANG ---");
            }

            if (_lrf.gameObject.activeSelf)
                _lrf.gameObject.SetActive(false);

            MissileCameraVisionMode vision = MissileCameraVisionModeController.Mode;
            string polarity = MissileCameraVisionModeController.FlirPolarityLabel(vision);
            _sb.Length = 0;
            _sb.Append("HDIR ").Append(hdgInt.ToString(CultureInfo.InvariantCulture)).Append("°T\n")
                .Append(polarity).Append('\n');
            if (mag <= 1f + ZoomAutoEpsilon)
                _sb.Append("FOC AUTO\n");
            else
                _sb.Append("FOC MAN x").Append(mag.ToString("0.0", CultureInfo.InvariantCulture)).Append('\n');
            if (MissileCameraVisionModeController.UsesInfraredBlit(vision))
                _sb.Append("EXP ").Append(snapshot.InfraredExposure.ToString("0.00", CultureInfo.InvariantCulture));
            else if (MissileCameraVisionModeController.UsesNightVisionVolume(vision))
                _sb.Append("EXP NVG");
            else
                _sb.Append("EXP DAY");
            SetTextIfChanged(_modes, _sb);

            _sb.Length = 0;
            _sb.Append("MAG x").Append(mag.ToString("0.0", CultureInfo.InvariantCulture))
                .Append("\nFOV ").Append(fovInt.ToString(CultureInfo.InvariantCulture)).Append('°');
            SetTextIfChanged(_magRid, _sb);

            string guide = snapshot.Guidance switch
            {
                MissileGuidanceStatus.Guided => "GUIDED",
                MissileGuidanceStatus.LostLock => "LOST LOCK",
                _ => "BALLISTIC"
            };

            _sb.Length = 0;
            if (snapshot.HasAimPoint)
            {
                _sb.Append("IP-RA ")
                    .Append(snapshot.TargetAngleDeg.ToString("0.0", CultureInfo.InvariantCulture)).Append("° / ")
                    .Append(snapshot.RelativeAltitudeMeters.ToString("0", CultureInfo.InvariantCulture)).Append('m');
            }
            else
                _sb.Append("IP-RA OFF");

            _sb.Append("\nINS NAV ").Append(snapshot.TargetAngleDeg.ToString("0.00", CultureInfo.InvariantCulture)).Append('°');
            if (snapshot.Guidance == MissileGuidanceStatus.LostLock)
                _sb.Append("\nTRK COR LOST");
            else if (snapshot.Guidance == MissileGuidanceStatus.Guided && snapshot.ClosingSpeedMs > 1f && snapshot.ClosingSpeedMs < 5000f)
                _sb.Append("\nTRK COR ON ").Append(FormatClosSafe(snapshot.ClosingSpeedMs));
            else
                _sb.Append("\nTRK COR OFF");

            if (snapshot.Guidance == MissileGuidanceStatus.LostLock)
                _sb.Append("\nSLAVE LOST");
            else if (snapshot.HasTarget && snapshot.Guidance == MissileGuidanceStatus.Guided)
                _sb.Append("\nSLAVE READY");
            else
                _sb.Append("\nSLAVE IDLE");

            _sb.Append('\n').Append(guide);
            SetTextIfChanged(_status, _sb);

            _sb.Length = 0;
            _sb.Append("PIT\n").Append(pitchInt.ToString(CultureInfo.InvariantCulture)).Append('°');
            SetTextIfChanged(_dialPitchLabel, _sb);
            _sb.Length = 0;
            _sb.Append("HDG\n").Append(hdgInt.ToString(CultureInfo.InvariantCulture)).Append('°');
            SetTextIfChanged(_dialHdgLabel, _sb);
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

        private static string FormatClosSafe(float closingMs)
        {
            if (closingMs < 0.5f || closingMs >= 5000f)
                return "CLOS ---";
            return "CLOS " + UnitConverter.SpeedReading(closingMs);
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

            Place(_sliderLabel, 0f, 0f, pad + 20f, panel.VerticalInset + 18f, 220f, 16f, TextAnchor.MiddleLeft);
        }

        private void UpdateGimbalDials(MissileCameraPanelMetrics panel, float pitchDeg, float headingDeg)
        {
            // Keep dials + captions above the bottom safe band (labels used to be bottom-anchored and clipped).
            float bottomSafe = Mathf.Max(panel.VerticalInset + 132f, 148f);
            float cy = -panel.Height * 0.5f + bottomSafe;
            float gap = 78f;
            float r = 30f;
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
            float pad = panel.HorizontalInset;
            float vPad = panel.VerticalInset;
            float row = 18f;
            float leftW = Mathf.Clamp(panel.Width * 0.34f, 240f, 440f);
            float rightW = leftW;

            Place(_sys, 0f, 1f, pad, -vPad, leftW, row, TextAnchor.MiddleLeft);
            Place(_coord, 0f, 1f, pad, -(vPad + row), leftW, row, TextAnchor.MiddleLeft);
            Place(_ownTelemetry, 0f, 1f, pad, -(vPad + row * 2f), leftW, row * 4.2f, TextAnchor.UpperLeft);
            Place(_alt, 0f, 1f, pad, -(vPad + row * 6.4f), leftW, row * 3.2f, TextAnchor.UpperLeft);
            Place(_date, 0f, 1f, pad, -(vPad + row * 9.8f), 120f, row, TextAnchor.MiddleLeft);
            Place(_time, 0f, 1f, pad, -(vPad + row * 10.8f), 140f, row, TextAnchor.MiddleLeft);

            Place(_headingBig, 0.5f, 1f, 0f, -vPad, 160f, row + 4f, TextAnchor.MiddleCenter, center: true);

            Place(_tgtCoord, 1f, 1f, -pad, -vPad, rightW, row * 2.2f, TextAnchor.UpperRight);
            Place(_tgtTelemetry, 1f, 1f, -pad, -(vPad + row * 2.4f), rightW, row * 3.4f, TextAnchor.UpperRight);
            Place(_tgtElv, 1f, 1f, -pad, -(vPad + row * 6f), rightW, row * 3.4f, TextAnchor.UpperRight);

            Place(_lrf, 0f, 0.55f, pad, 48f, 40f, row, TextAnchor.MiddleLeft);
            Place(_northArrow, 0f, 0.55f, pad, 18f, 80f, row, TextAnchor.MiddleLeft);
            Place(_modes, 0f, 0f, pad, vPad + 120f, 160f, row * 4.5f, TextAnchor.LowerLeft);
            Place(_magRid, 0f, 0f, pad + 170f, vPad + 120f, 160f, row * 2.5f, TextAnchor.LowerLeft);
            Place(_status, 1f, 0f, -pad, vPad + 100f, rightW * 0.7f, row * 5.5f, TextAnchor.LowerRight);
        }

        private void ApplyFonts()
        {
            const int body = 15;
            const int big = 19;
            Font font = HudFontHelper.GetFont();
            foreach (Text t in new[]
                     {
                         _sys, _coord, _ownTelemetry, _alt, _date, _time,
                         _tgtCoord, _tgtTelemetry, _tgtElv, _lrf, _modes, _status, _magRid,
                         _dialPitchLabel, _dialHdgLabel, _northArrow, _sliderLabel
                     })
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
            _modes.alignment = TextAnchor.UpperLeft;
            _magRid.alignment = TextAnchor.UpperLeft;
            _status.alignment = TextAnchor.LowerRight;
            _ownTelemetry.alignment = TextAnchor.UpperLeft;
            _alt.alignment = TextAnchor.UpperLeft;
            _tgtCoord.alignment = TextAnchor.UpperRight;
            _tgtTelemetry.alignment = TextAnchor.UpperRight;
            _tgtElv.alignment = TextAnchor.UpperRight;
            _dialPitchLabel.alignment = TextAnchor.UpperCenter;
            _dialHdgLabel.alignment = TextAnchor.UpperCenter;
            _dialPitchLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            _dialHdgLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            _dialPitchLabel.verticalOverflow = VerticalWrapMode.Overflow;
            _dialHdgLabel.verticalOverflow = VerticalWrapMode.Overflow;
            _ownTelemetry.horizontalOverflow = HorizontalWrapMode.Overflow;
            _ownTelemetry.verticalOverflow = VerticalWrapMode.Overflow;
            _alt.horizontalOverflow = HorizontalWrapMode.Overflow;
            _alt.verticalOverflow = VerticalWrapMode.Overflow;
            _tgtCoord.horizontalOverflow = HorizontalWrapMode.Overflow;
            _tgtCoord.verticalOverflow = VerticalWrapMode.Overflow;
            _tgtTelemetry.horizontalOverflow = HorizontalWrapMode.Overflow;
            _tgtTelemetry.verticalOverflow = VerticalWrapMode.Overflow;
            _tgtElv.horizontalOverflow = HorizontalWrapMode.Overflow;
            _tgtElv.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private void ApplyColors()
        {
            foreach (Text t in new[]
                     {
                         _sys, _coord, _ownTelemetry, _alt, _date, _time, _headingBig,
                         _tgtCoord, _tgtTelemetry, _tgtElv, _lrf, _modes, _status, _magRid,
                         _dialPitchLabel, _dialHdgLabel, _northArrow, _sliderLabel
                     })
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
