using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    internal readonly struct MissileCameraZone
    {
        internal static readonly MissileCameraZone Invalid = new MissileCameraZone(false);

        internal MissileCameraZone(PanelRectState rect)
        {
            IsValid = true;
            MinX = rect.AnchorMin.x;
            MaxX = rect.AnchorMax.x;
            MinY = rect.AnchorMin.y;
            MaxY = rect.AnchorMax.y;
            OffsetMin = rect.OffsetMin;
            OffsetMax = rect.OffsetMax;
        }

        private MissileCameraZone(bool valid)
        {
            IsValid = valid;
            MinX = MaxX = MinY = MaxY = 0f;
            OffsetMin = OffsetMax = Vector2.zero;
        }

        internal bool IsValid { get; }
        internal float MinX { get; }
        internal float MaxX { get; }
        internal float MinY { get; }
        internal float MaxY { get; }
        internal Vector2 OffsetMin { get; }
        internal Vector2 OffsetMax { get; }
    }

    internal readonly struct WeaponsReplacementResult
    {
        internal WeaponsReplacementResult(
            MissileCameraZone zone,
            Canvas? overlayCanvas,
            bool suppressBottomDivider = false,
            bool showPanelBorder = false,
            RectTransform? overlayParent = null,
            bool suppressBottomBorder = false,
            float overlayRotationZ = 0f,
            float stubContentRotationZ = 0f,
            float stubFontRef = 0f,
            Vector2 stubContentBand = default,
            bool stubForcePortraitLayout = false,
            float hudLeftInsetExtra = 0.02f,
            MissileCameraTelemetryLayout telemetryLayout = MissileCameraTelemetryLayout.BottomRow)
        {
            Zone = zone;
            OverlayCanvas = overlayCanvas;
            OverlayParent = overlayParent;
            SuppressBottomDivider = suppressBottomDivider;
            ShowPanelBorder = showPanelBorder;
            SuppressBottomBorder = suppressBottomBorder;
            OverlayRotationZ = overlayRotationZ;
            StubContentRotationZ = stubContentRotationZ;
            StubFontRef = stubFontRef;
            StubContentBand = stubContentBand == default ? Vector2.up : stubContentBand;
            StubForcePortraitLayout = stubForcePortraitLayout;
            HudLeftInsetExtra = hudLeftInsetExtra;
            TelemetryLayout = telemetryLayout;
        }

        internal MissileCameraZone Zone { get; }
        internal Canvas? OverlayCanvas { get; }
        internal RectTransform? OverlayParent { get; }
        internal bool SuppressBottomDivider { get; }
        internal bool ShowPanelBorder { get; }
        internal bool SuppressBottomBorder { get; }
        internal float OverlayRotationZ { get; }
        internal float StubContentRotationZ { get; }
        internal float StubFontRef { get; }
        /// <summary>Normalized Y band (min,max) inside overlay panel for stub content.</summary>
        internal Vector2 StubContentBand { get; }
        /// <summary>Rotated bezel MFD: use portrait column anchors (X slots) even when canvas rect is nearly square.</summary>
        internal bool StubForcePortraitLayout { get; }
        internal float HudLeftInsetExtra { get; }
        internal MissileCameraTelemetryLayout TelemetryLayout { get; }
        internal bool Success => Zone.IsValid && OverlayCanvas != null;
    }

    /// <summary>
    /// Find WEAPON ARMED panel, hide it, place MissileCamera at the same canvas rect (Revoker + Ifrit).
    /// </summary>
    internal static class MfdWeaponsZoneAccess
    {
        private static RectTransform? _hiddenWeaponsPanel;
        private static readonly List<(RectTransform Node, bool WasActive)> HiddenStripNodes = new List<(RectTransform, bool)>();
        private static bool _weaponsWasActive;
        private static bool _overlayOnlyReplacement;
        private static bool _debugDumpDone;
        private static bool _failureDiagDone;
        private static bool _ifritStatusDiagDone;
        private static bool _alkyonDiagDone;
        private static bool _darkreachDiagDone;
        private static bool _darkreachParentDiagDone;
        private static bool _compassDiagDone;
        private static bool _tarantulaDiagDone;
        private static bool _chicaneDiagDone;
        private static bool _cricketDiagDone;
        private static int _hiddenRootInstanceId;
        private static string _hiddenRootPath = string.Empty;

        internal static bool HasBomberBayMarkers(GameObject mfdRoot)
        {
            Text[] texts = mfdRoot.GetComponentsInChildren<Text>(true);
            foreach (Text label in texts)
            {
                if (label != null && IsBomberBayMarker(label.text))
                    return true;
            }

            return false;
        }

        internal static bool HasBomberBayMarkersForTacScreen(TacScreen tacScreen, string? aircraftJsonKey) =>
            TacScreenAccess.HasBomberBayMarkersForAircraft(tacScreen, aircraftJsonKey);

        internal static bool HasDarkreachLeftBayMarkers(TacScreen tacScreen)
        {
            GameObject mfdRoot = tacScreen.gameObject;
            Canvas? canvas = TacScreenAccess.GetCanvasForRoot(mfdRoot) ?? TacScreenAccess.GetCanvas(tacScreen);
            if (canvas == null)
                return false;

            return HasBomberBayMarkers(mfdRoot) && CanResolveDarkreachLeftColumn(mfdRoot, canvas);
        }

        internal static bool IsBomberBayMarkerText(string? raw) => IsBomberBayMarker(raw ?? string.Empty);

        internal static bool HasMedusaWeaponsMarkers(GameObject mfdRoot)
        {
            Text[] texts = mfdRoot.GetComponentsInChildren<Text>(true);
            foreach (Text label in texts)
            {
                if (label != null && IsMedusaWeaponsMarker(label.text))
                    return true;
            }

            return false;
        }

        internal static void LogCricketDiscoveryFailure(TacScreen tacScreen) =>
            MaybeLogCricketDiscoveryFailure(tacScreen);

        internal static bool CanDiscoverCricketEnginePanel(TacScreen tacScreen, string? aircraftJsonKey = null)
        {
            if (!TacScreenAccess.IsCricketAircraft(aircraftJsonKey))
                return false;

            return TryResolveCricketEnginePanel(tacScreen, out _);
        }

        internal static WeaponsReplacementResult PrepareCricketEngineReplacement(
            TacScreen tacScreen,
            string? aircraftJsonKey = null)
        {
            Restore();

            if (!TryResolveCricketEnginePanel(tacScreen, out ResolvedPanel resolved))
            {
                MfdLog.Info("cricket engine MFD not found");
                return new WeaponsReplacementResult(MissileCameraZone.Invalid, null);
            }

            MfdLog.Info(
                $"cricketEngineRoot={resolved.Panel.name} hideChildren={resolved.HideTargets.Count} " +
                $"canvas={resolved.OverlayCanvas?.name ?? "null"} zone={FormatAnchors(resolved.Zone)}");

            return ApplyHidden(resolved, "cricket-engine");
        }

        internal static WeaponsReplacementResult PrepareReplacement(TacScreen tacScreen, string? aircraftJsonKey = null)
        {
            Restore();

            GameObject mfdRoot = TacScreenAccess.ResolveDiscoveryRoot(tacScreen, aircraftJsonKey);
            bool dedicatedWeaponMfd = TacScreenAccess.IsDedicatedWeaponMfdRoot(mfdRoot, tacScreen);
            Canvas? canvas = TacScreenAccess.GetCanvasForRoot(mfdRoot) ?? TacScreenAccess.GetCanvas(tacScreen);
            if (canvas == null)
                return new WeaponsReplacementResult(MissileCameraZone.Invalid, null);

            MaybeDumpTextInventory(mfdRoot);
            MfdLog.Info($"discoveryRoot={mfdRoot.name} dedicated={dedicatedWeaponMfd} canvas={canvas.name}");

            if (!TryResolveWeaponsPanel(
                    mfdRoot,
                    tacScreen,
                    canvas,
                    aircraftJsonKey,
                    dedicatedWeaponMfd,
                    out ResolvedPanel resolved))
            {
                MfdLog.Info(
                    dedicatedWeaponMfd
                        ? "weapons panel not found on cockpit weapon MFD"
                        : "weapons panel not found on MFD root");
                return new WeaponsReplacementResult(MissileCameraZone.Invalid, null);
            }

            string layout = resolved.IsAlkyonFullPanel
                ? "alkyon"
                : resolved.IsDarkreachSection
                    ? "darkreach"
                    : resolved.IsMedusaSection
                    ? "medusa"
                    : resolved.IsCricketEngineSection
                        ? "cricket-engine"
                    : resolved.IsChicaneEngineSection
                        ? "chicane-engine"
                    : resolved.IsIbisSection
                        ? "ibis"
                    : resolved.IsTarantulaSection
                        ? "tarantula"
                    : resolved.IsVagrantNozzleEngineSection
                        ? "vagrant-nozzle-engine"
                    : resolved.IsCompassEngineSection
                        ? "engine"
                    : resolved.IsIfritStrip
                        ? "ifrit"
                        : (PanelRectNormalizer.IsTopRightZone(resolved.Zone) ? "revoker" : "ifrit");
            return ApplyHidden(resolved, layout);
        }

        internal static bool CanDiscoverWeaponsPanel(TacScreen tacScreen, string? aircraftJsonKey = null)
        {
            GameObject mfdRoot = TacScreenAccess.ResolveDiscoveryRoot(tacScreen, aircraftJsonKey);
            bool dedicatedWeaponMfd = TacScreenAccess.IsDedicatedWeaponMfdRoot(mfdRoot, tacScreen);
            Canvas? canvas = TacScreenAccess.GetCanvasForRoot(mfdRoot) ?? TacScreenAccess.GetCanvas(tacScreen);
            if (canvas == null)
                return false;

            return TryResolveWeaponsPanel(
                mfdRoot,
                tacScreen,
                canvas,
                aircraftJsonKey,
                dedicatedWeaponMfd,
                out _);
        }

        internal static void Restore()
        {
            if (!IsRestoreContextValid())
            {
                HiddenStripNodes.Clear();
                _hiddenWeaponsPanel = null;
                _overlayOnlyReplacement = false;
                _hiddenRootInstanceId = 0;
                _hiddenRootPath = string.Empty;
                return;
            }

            foreach ((RectTransform node, bool wasActive) in HiddenStripNodes)
            {
                if (node)
                    node.gameObject.SetActive(wasActive);
            }

            HiddenStripNodes.Clear();

            if (_hiddenWeaponsPanel is { } livePanel && livePanel)
                livePanel.gameObject.SetActive(_weaponsWasActive);

            _hiddenWeaponsPanel = null;
            _overlayOnlyReplacement = false;
            _hiddenRootInstanceId = 0;
            _hiddenRootPath = string.Empty;
        }

        internal static bool IsReplacementActive() =>
            _hiddenWeaponsPanel != null || HiddenStripNodes.Count > 0 || _overlayOnlyReplacement;

        internal static void ResetForMissionUnload()
        {
            Restore();
            _debugDumpDone = false;
            _failureDiagDone = false;
            _ifritStatusDiagDone = false;
            _alkyonDiagDone = false;
            _darkreachDiagDone = false;
            _darkreachParentDiagDone = false;
            _compassDiagDone = false;
            _tarantulaDiagDone = false;
            _chicaneDiagDone = false;
            _cricketDiagDone = false;
        }

        private static bool IsRestoreContextValid()
        {
            if (_hiddenRootInstanceId == 0)
                return true;

            RectTransform? root = _hiddenWeaponsPanel;
            if (root == null)
                return true;

            if (root.GetInstanceID() == _hiddenRootInstanceId)
                return true;

            string currentPath = BuildTransformPath(root);
            return string.Equals(currentPath, _hiddenRootPath, System.StringComparison.Ordinal);
        }

        private static string BuildTransformPath(Transform? node)
        {
            if (node == null)
                return string.Empty;

            string path = node.name;
            Transform? current = node.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        private static bool IsMedusaLayout(GameObject mfdRoot, string? aircraftJsonKey)
        {
            if (string.Equals(aircraftJsonKey, "EW1", System.StringComparison.OrdinalIgnoreCase))
                return true;

            return HasMedusaWeaponsMarkers(mfdRoot);
        }

        private static bool TryResolveWeaponsPanel(
            GameObject mfdRoot,
            TacScreen tacScreen,
            Canvas canvas,
            string? aircraftJsonKey,
            bool dedicatedWeaponMfd,
            out ResolvedPanel resolved)
        {
            resolved = default;

            List<RectTransform> allHits = CollectWeaponHits(mfdRoot);
            List<RectTransform> primaryHits = CollectPrimaryWeaponHits(mfdRoot);
            List<RectTransform> hardpointHits = CollectHardpointArmedHits(mfdRoot);
            List<RectTransform> gunHits = CollectGunArmedHits(mfdRoot);

            if (IsDarkreachAircraft(aircraftJsonKey))
            {
                if (TryResolveDarkreachWeaponArmedPanel(mfdRoot, canvas, out resolved))
                    return true;

                MaybeLogDarkreachDiscoveryFailure(mfdRoot, null, canvas, primaryHits);
                return false;
            }

            if (IsIbisAircraft(aircraftJsonKey))
            {
                if (TryResolveIbisWeaponArmedSection(mfdRoot, canvas, out resolved))
                    return true;

                MaybeLogIbisDiscoveryFailure(mfdRoot, canvas);
                return false;
            }

            if (HasBomberBayMarkers(mfdRoot))
            {
                if (TryResolveAlkyonFullRightPanel(mfdRoot, canvas, primaryHits, out resolved))
                    return true;

                MaybeLogAlkyonDiscoveryFailure(
                    mfdRoot,
                    FindAlkyonWeaponsAnchor(mfdRoot, primaryHits),
                    canvas,
                    default,
                    0);
                return false;
            }

            if (IsMedusaLayout(mfdRoot, aircraftJsonKey))
            {
                if (TryResolveMedusaWeaponsSection(mfdRoot, canvas, out resolved))
                    return true;

                MaybeLogMedusaDiscoveryFailure(mfdRoot, canvas);
                return false;
            }

            if (IsTarantulaAircraft(aircraftJsonKey))
            {
                if (TryResolveTarantulaWeaponArmedSection(mfdRoot, canvas, out resolved))
                    return true;

                MaybeLogTarantulaDiscoveryFailure(mfdRoot, canvas);
                return false;
            }

            if (IsChicaneAircraft(aircraftJsonKey))
            {
                if (TryResolveChicaneEngineSection(mfdRoot, tacScreen, canvas, out resolved))
                    return true;

                MaybeLogChicaneDiscoveryFailure(mfdRoot, tacScreen, canvas);
                return false;
            }

            if (TacScreenAccess.IsVagrantAircraft(aircraftJsonKey))
            {
                if (TryResolveVagrantNozzleEngineSection(mfdRoot, tacScreen, canvas, aircraftJsonKey, out resolved))
                    return true;

                MaybeLogVagrantDiscoveryFailure(mfdRoot, tacScreen, canvas, aircraftJsonKey);
                return false;
            }

            if (TacScreenAccess.IsEngineSectionMfdAircraft(aircraftJsonKey))
            {
                if (TryResolveEngineSectionPanel(mfdRoot, tacScreen, canvas, aircraftJsonKey, out resolved))
                    return true;

                MaybeLogEngineSectionDiscoveryFailure(mfdRoot, tacScreen, canvas, aircraftJsonKey);
                return false;
            }

            RectTransform? revokerPanel = ResolveRevokerPanel(mfdRoot, canvas, primaryHits);
            if (revokerPanel != null)
                return TryFinalizeSinglePanel(revokerPanel, canvas, mfdRoot, allHits, primaryHits, hardpointHits, gunHits, out resolved);

            if (TryResolveIfritFlatStrip(mfdRoot, canvas, out resolved))
                return true;

            // Legacy Ifrit path тАФ only when TryResolveIfritFlatStrip fails.
            RectTransform? legacyIfrit = ResolveIfritWeaponsStrip(hardpointHits, gunHits, primaryHits, mfdRoot, canvas);
            if (legacyIfrit != null)
                return TryFinalizeSinglePanel(legacyIfrit, canvas, mfdRoot, allHits, primaryHits, hardpointHits, gunHits, out resolved);

            RectTransform? rpmPanel = DiscoverViaRpmGauge(mfdRoot);
            if (rpmPanel != null)
                return TryFinalizeSinglePanel(rpmPanel, canvas, mfdRoot, allHits, primaryHits, hardpointHits, gunHits, out resolved);

            MaybeLogDiscoveryFailure(
                mfdRoot, canvas, allHits, primaryHits, hardpointHits, gunHits, null, default, null);
            return false;
        }

        private static bool TryFinalizeSinglePanel(
            RectTransform weaponsPanel,
            Canvas canvas,
            GameObject mfdRoot,
            List<RectTransform> allHits,
            List<RectTransform> primaryHits,
            List<RectTransform> hardpointHits,
            List<RectTransform> gunHits,
            out ResolvedPanel resolved)
        {
            resolved = default;
            Canvas? overlayCanvas = TacScreenAccess.GetOverlayCanvas(weaponsPanel) ?? canvas;
            PanelRectState saved = PanelRectNormalizer.CaptureOnCanvas(weaponsPanel, overlayCanvas);
            if (!PanelRectNormalizer.IsWeaponsReplacementZone(saved))
            {
                string? reject = DescribeStripRejectReason(weaponsPanel, canvas, hardpointHits, gunHits);
                MaybeLogDiscoveryFailure(
                    mfdRoot, canvas, allHits, primaryHits, hardpointHits, gunHits, weaponsPanel, saved, reject);
                return false;
            }

            resolved = new ResolvedPanel(weaponsPanel, overlayCanvas, saved);
            return true;
        }

        private static bool TryResolveAlkyonFullRightPanel(
            GameObject mfdRoot,
            Canvas canvas,
            List<RectTransform> primaryHits,
            out ResolvedPanel resolved)
        {
            resolved = default;
            if (!HasBomberBayMarkers(mfdRoot))
                return false;

            RectTransform? anchor = FindAlkyonWeaponsAnchor(mfdRoot, primaryHits);
            if (anchor == null)
                return false;

            if (PanelContainsEngineGauges(anchor))
                return false;

            if (HasIfritStripLayout(anchor))
                return false;

            Canvas overlayCanvas = TacScreenAccess.GetOverlayCanvas(anchor) ?? canvas;
            RectTransform? columnRoot = ResolveAlkyonColumnRoot(anchor, mfdRoot, overlayCanvas);
            PanelRectState zone;
            List<RectTransform> hideTargets;

            if (columnRoot != null)
            {
                zone = PanelRectNormalizer.CaptureOnCanvas(columnRoot, overlayCanvas);
                hideTargets = new List<RectTransform> { columnRoot };
            }
            else
            {
                List<RectTransform> zoneNodes = CollectAlkyonZoneNodes(anchor, overlayCanvas, mfdRoot);
                ExpandAlkyonZoneNodesIfNeeded(anchor, overlayCanvas, zoneNodes);
                zone = PanelRectNormalizer.UnionOnCanvas(overlayCanvas, zoneNodes);
                hideTargets = CollectAlkyonHideTargets(anchor, overlayCanvas, zoneNodes);
            }

            zone = ExpandAlkyonZoneVertical(anchor, mfdRoot, overlayCanvas, zone, hideTargets);

            RectTransform? fullRoot = SelectAlkyonColumnRoot(anchor, overlayCanvas);
            if (fullRoot != null)
            {
                PanelRectState fullZone = PanelRectNormalizer.CaptureOnCanvas(fullRoot, overlayCanvas);
                if (IsAlkyonFullColumnZone(fullZone))
                {
                    zone = fullZone;
                    hideTargets = new List<RectTransform> { fullRoot };
                    columnRoot = fullRoot;
                }
            }

            if (!IsAlkyonFullColumnZone(zone))
            {
                MaybeLogAlkyonDiscoveryFailure(mfdRoot, anchor, canvas, zone, hideTargets.Count);
                return false;
            }

            string columnName = hideTargets.Count > 0 ? hideTargets[0].name : "none";
            MfdLog.Info(
                $"alkyon anchor={anchor.name} columnRoot={columnName} " +
                $"zone={zone.AnchorMin.x:F2}-{zone.AnchorMax.x:F2} y={zone.AnchorMin.y:F2}-{zone.AnchorMax.y:F2} hide={hideTargets.Count}");

            resolved = new ResolvedPanel(
                anchor,
                overlayCanvas,
                zone,
                hideTargets,
                isAlkyonFullPanel: true);
            return true;
        }

        private static bool IsDarkreachAircraft(string? jsonKey) =>
            string.Equals(jsonKey, "Darkreach", System.StringComparison.OrdinalIgnoreCase);

        private static bool IsCricketAircraft(string? jsonKey) =>
            TacScreenAccess.IsCricketAircraft(jsonKey);

        private static bool TryResolveCricketEnginePanel(TacScreen tacScreen, out ResolvedPanel resolved)
        {
            resolved = default;
            Component? aircraft = TacScreenAccess.GetAircraft(tacScreen);
            if (aircraft == null)
                return false;

            GameObject? engineMfdRoot = TacScreenAccess.FindCricketEngineMfdRoot(tacScreen, aircraft.transform);
            if (engineMfdRoot == null)
                return false;

            Canvas? canvas = TacScreenAccess.GetCanvasForRoot(engineMfdRoot);
            if (canvas == null)
                return false;

            if (TryGetKnownCricketEngPanel(engineMfdRoot, canvas, out resolved))
                return true;

            GameObject? targetCamDisplay = TacScreenAccess.GetTargetCamDisplay(tacScreen);
            List<RectTransform> hideTargets = CollectCricketEngineHideTargets(
                canvas,
                engineMfdRoot,
                targetCamDisplay);
            if (hideTargets.Count == 0)
                return false;

            PanelRectState zone = BuildCricketEngineOverlayZone(PanelRectNormalizer.UnionOnCanvas(canvas, hideTargets));
            if (!PanelRectNormalizer.IsWeaponsReplacementZone(zone))
            {
                MfdLog.Info($"cricket zone rejected union={FormatAnchors(zone)} hideChildren={hideTargets.Count}");
                return false;
            }

            RectTransform anchor = hideTargets[0];
            resolved = new ResolvedPanel(
                anchor,
                canvas,
                zone,
                hideTargets,
                isCricketEngineSection: true);
            return true;
        }

        /// <summary>Cricket shared canvas: fixed EngPanel node (TURBINE / PROP gauges).</summary>
        private static bool TryGetKnownCricketEngPanel(
            GameObject mfdRoot,
            Canvas canvas,
            out ResolvedPanel resolved)
        {
            resolved = default;
            RectTransform? engPanel = FindPanelByName(mfdRoot, "EngPanel");
            if (engPanel == null)
                return false;

            var hideTargets = new List<RectTransform> { engPanel };
            PanelRectState canvasZone = PanelRectNormalizer.CaptureOnCanvas(engPanel, canvas);
            if (!PanelRectNormalizer.IsWeaponsReplacementZone(canvasZone))
            {
                MfdLog.Info($"cricket EngPanel zone rejected norm={FormatAnchors(canvasZone)}");
                return false;
            }

            // Bezel maps canvas X тЖТ screen vertical: column anchors on X (Darkreach), not rows on Y.
            PanelRectState zone = BuildCricketEngineOverlayZone(canvasZone);
            float contentRotZ = SampleCricketEngineLabelRotation(engPanel);
            float fontRef = Mathf.Max(
                Mathf.Max(engPanel.rect.width, engPanel.rect.height),
                1f);
            Vector2 contentBand = Vector2.up;

            Canvas overlayCanvas = TacScreenAccess.GetOverlayCanvas(engPanel) ?? canvas;
            MfdLog.Info(
                $"cricket engine source=EngPanel panelZone={FormatAnchors(zone)} " +
                $"contentRotZ={contentRotZ:F0} fontRef={fontRef:F0} portraitColumns=True " +
                $"panelRect={engPanel.rect.width:F0}x{engPanel.rect.height:F0}");

            resolved = new ResolvedPanel(
                engPanel,
                overlayCanvas,
                zone,
                hideTargets,
                isCricketEngineSection: true,
                stubContentRotationZ: contentRotZ,
                stubFontRef: fontRef,
                stubContentBand: contentBand,
                stubForcePortraitLayout: true);
            return true;
        }

        private static float SampleCricketEngineLabelRotation(RectTransform engPanel)
        {
            foreach (Text label in engPanel.GetComponentsInChildren<Text>(true))
            {
                if (label == null || string.IsNullOrEmpty(label.text))
                    continue;

                float rotZ = label.rectTransform.localEulerAngles.z;
                if (Mathf.Abs(Mathf.DeltaAngle(0f, rotZ)) > 0.5f)
                    return rotZ;
            }

            foreach (Component gauge in engPanel.GetComponentsInChildren<Component>(true))
            {
                if (gauge is not (PropGauge or RPMGauge or EngineTelemetry))
                    continue;

                if (!gauge.TryGetComponent(out RectTransform gaugeRt))
                    continue;

                float rotZ = gaugeRt.localEulerAngles.z;
                if (Mathf.Abs(Mathf.DeltaAngle(0f, rotZ)) > 0.5f)
                    return rotZ;
            }

            return engPanel.rect.height > engPanel.rect.width * 1.15f ? 90f : 0f;
        }

        // COIN/Cricket manual bezel tuning тАФ canvas X = screen vertical on engine MFD.
        // MinX = physical top, MaxX = physical bottom. Positive expand pulls edge outward.
        private const float CricketManualExpandMinX = 0.01f;
        private const float CricketManualExpandMaxX = 0.085f;
        private const float CricketManualExpandMinY = 0f;
        private const float CricketManualExpandMaxY = 0f;
        private const float CricketInsetTopRatio = 0.05f;
        private const float CricketInsetBottomRatio = 0.006f;
        private const float CricketInsetLeftRatio = 0.06f;
        private const float CricketInsetRightRatio = 0.06f;

        /// <summary>Cricket shared canvas: inset zone so overlay stays inside one physical MFD bezel.</summary>
        private static PanelRectState BuildCricketEngineOverlayZone(PanelRectState panelZone)
        {
            float minX = Mathf.Clamp01(panelZone.AnchorMin.x - CricketManualExpandMinX);
            float maxX = Mathf.Clamp01(panelZone.AnchorMax.x + CricketManualExpandMaxX);
            float minY = Mathf.Clamp01(panelZone.AnchorMin.y - CricketManualExpandMinY);
            float maxY = Mathf.Clamp01(panelZone.AnchorMax.y + CricketManualExpandMaxY);
            var expanded = new PanelRectState(
                new Vector2(minX, minY),
                new Vector2(maxX, maxY),
                Vector2.zero,
                Vector2.zero);

            float spanX = Mathf.Max(expanded.AnchorMax.x - expanded.AnchorMin.x, 0.01f);
            float spanY = Mathf.Max(expanded.AnchorMax.y - expanded.AnchorMin.y, 0.01f);
            float insetTop = Mathf.Max(spanX * CricketInsetTopRatio, 0.008f);
            float insetBottom = Mathf.Max(spanX * CricketInsetBottomRatio, 0.003f);
            float insetLeft = Mathf.Max(spanY * CricketInsetLeftRatio, 0.008f);
            float insetRight = Mathf.Max(spanY * CricketInsetRightRatio, 0.008f);

            return new PanelRectState(
                new Vector2(
                    Mathf.Clamp01(expanded.AnchorMin.x + insetTop),
                    Mathf.Clamp01(expanded.AnchorMin.y + insetLeft)),
                new Vector2(
                    Mathf.Clamp01(expanded.AnchorMax.x - insetBottom),
                    Mathf.Clamp01(expanded.AnchorMax.y - insetRight)),
                Vector2.zero,
                Vector2.zero);
        }

        private static float ResolveHudLeftInsetExtra(ResolvedPanel resolved)
        {
            if (resolved.IsAlkyonFullPanel)
                return 0.05f;

            if (resolved.IsCricketEngineSection)
                return 0.04f;

            if (resolved.IsVagrantNozzleEngineSection)
                return 0.04f;

            return 0.02f;
        }

        private static List<RectTransform> CollectCricketEngineHideTargets(
            Canvas canvas,
            GameObject searchRoot,
            GameObject? targetCamDisplay)
        {
            var targets = new List<RectTransform>();
            var seen = new HashSet<RectTransform>();
            RectTransform? canvasRt = canvas.GetComponent<RectTransform>();

            foreach (PropGauge prop in searchRoot.GetComponentsInChildren<PropGauge>(true))
                AddCricketEngineSection(prop, canvas, canvasRt, targetCamDisplay, seen, targets);

            foreach (RPMGauge rpm in searchRoot.GetComponentsInChildren<RPMGauge>(true))
                AddCricketEngineSection(rpm, canvas, canvasRt, targetCamDisplay, seen, targets);

            foreach (EngineTelemetry telemetry in searchRoot.GetComponentsInChildren<EngineTelemetry>(true))
                AddCricketEngineSection(telemetry, canvas, canvasRt, targetCamDisplay, seen, targets);

            return targets;
        }

        private static void AddCricketEngineSection(
            Component gauge,
            Canvas canvas,
            RectTransform? canvasRt,
            GameObject? targetCamDisplay,
            HashSet<RectTransform> seen,
            List<RectTransform> targets)
        {
            if (!gauge.TryGetComponent(out RectTransform gaugeRt))
                return;

            if (targetCamDisplay != null && gaugeRt.IsChildOf(targetCamDisplay.transform))
                return;

            if (!IsCricketEngineGauge(gaugeRt, canvas))
                return;

            RectTransform? section = ResolveCricketEngineSection(gaugeRt, canvasRt);
            if (section == null || !seen.Add(section))
                return;

            targets.Add(section);
        }

        private static bool IsCricketEngineGauge(RectTransform gaugeRt, Canvas canvas)
        {
            PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(gaugeRt, canvas);
            return zone.AnchorMin.x >= 0.65f;
        }

        private static RectTransform? ResolveCricketEngineSection(RectTransform gaugeRt, RectTransform? canvasRt)
        {
            RectTransform? best = gaugeRt.parent != null ? gaugeRt.parent.GetComponent<RectTransform>() : gaugeRt;
            RectTransform? current = best;

            while (current != null && current != canvasRt)
            {
                if (PanelContainsEngineGauges(current) && !SpansFullMfd(current))
                    best = current;

                current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
            }

            return best;
        }

        private static void MaybeLogCricketDiscoveryFailure(TacScreen tacScreen)
        {
            if (_cricketDiagDone)
                return;

            _cricketDiagDone = true;
            Component? aircraft = TacScreenAccess.GetAircraft(tacScreen);
            if (aircraft == null)
            {
                MfdLog.Info("cricket discovery failed aircraft=null");
                return;
            }

            GameObject? engineMfdRoot = TacScreenAccess.FindCricketEngineMfdRoot(tacScreen, aircraft.transform);
            MFDAppManager? manager = MFDAppManager.i;
            int propCount = aircraft.GetComponentsInChildren<PropGauge>(true).Length;
            int rpmCount = aircraft.GetComponentsInChildren<RPMGauge>(true).Length;
            RectTransform? engPanel = engineMfdRoot != null
                ? FindPanelByName(engineMfdRoot, "EngPanel")
                : null;
            MfdLog.Info(
                $"cricket discovery failed engineMfdRoot={(engineMfdRoot != null ? engineMfdRoot.name : "null")} " +
                $"EngPanel={(engPanel != null ? FormatAnchors(engPanel) : "null")} " +
                $"MFDAppManager.i={(manager != null ? manager.name : "null")} prop={propCount} rpm={rpmCount}");

            if (engineMfdRoot == null)
                return;

            Canvas? canvas = TacScreenAccess.GetCanvasForRoot(engineMfdRoot);
            if (canvas == null)
                return;

            RectTransform? host = canvas.GetComponent<RectTransform>();
            if (host == null)
                return;

            int childDump = Mathf.Min(host.childCount, 10);
            for (int i = 0; i < childDump; i++)
            {
                RectTransform? child = host.GetChild(i).GetComponent<RectTransform>();
                if (child == null)
                    continue;

                PanelRectState childNorm = PanelRectNormalizer.CaptureOnCanvas(child, canvas);
                MfdLog.Info(
                    $"  cricket canvasChild[{i}] {child.name} norm={FormatAnchors(childNorm)} " +
                    $"gauges={PanelContainsEngineGauges(child)}");
            }
        }

        private static bool IsTarantulaAircraft(string? jsonKey) =>
            TacScreenAccess.IsTarantulaAircraft(jsonKey);

        private static bool _ibisDiagDone;

        private static bool IsIbisAircraft(string? jsonKey) =>
            TacScreenAccess.IsIbisAircraft(jsonKey);

        private static bool TryResolveIbisWeaponArmedSection(
            GameObject mfdRoot,
            Canvas canvas,
            out ResolvedPanel resolved)
        {
            resolved = default;
            RectTransform? weaponPanel = FindWeaponPanel(mfdRoot);
            if (weaponPanel == null)
                return false;

            List<RectTransform> stripNodes = CollectIbisStripChildren(weaponPanel);
            if (!TryResolveStripWeaponsSection(
                    mfdRoot,
                    canvas,
                    weaponPanel,
                    stripNodes,
                    expandFullColumnWidth: false,
                    isMedusaSection: false,
                    isTarantulaSection: false,
                    out resolved))
            {
                return false;
            }

            if (!PanelRectNormalizer.IsIbisWeaponArmedZone(resolved.Zone))
            {
                MfdLog.Info(
                    $"ibis weaponArmed zone rejected zone={FormatAnchors(resolved.Zone)} " +
                    $"hideChildren={resolved.HideTargets.Count}");
                return false;
            }

            PanelRectState stripZone = resolved.Zone;
            PanelRectState zone = BuildIbisWeaponOverlayZone(weaponPanel, stripZone, canvas, mfdRoot);
            float statusFloorX = TryFindIbisStatusFloorMinX(weaponPanel, mfdRoot, canvas);
            if (!PanelRectNormalizer.IsIbisWeaponArmedZone(zone))
            {
                MfdLog.Info(
                    $"ibis weaponArmed fitted zone rejected zone={FormatAnchors(zone)} " +
                    $"strip={FormatAnchors(stripZone)}");
                zone = stripZone;
            }

            // Profile labels use 270┬░ in hierarchy; left MFD bezel reads stub correctly at 90┬░ (see Cricket).
            float contentRotZ = 90f;
            float fontRef = Mathf.Max(
                Mathf.Max(weaponPanel.rect.width, weaponPanel.rect.height),
                1f);

            MfdLog.Info(
                $"ibis weaponArmed source=strip hideRoot={weaponPanel.name} hideChildren={resolved.HideTargets.Count} " +
                $"strip={FormatAnchors(stripZone)} zone={FormatAnchors(zone)} statusFloorX={statusFloorX:F2} stripBottom={resolved.StripBottomY:F2} " +
                $"statusTop={resolved.StatusTopY:F2} statusFrame={resolved.StatusFrameName} " +
                $"contentRotZ={contentRotZ:F0} fontRef={fontRef:F0} portraitColumns=True");

            resolved = new ResolvedPanel(
                resolved.Panel,
                resolved.OverlayCanvas,
                zone,
                resolved.HideTargets,
                resolved.StatusTopY,
                resolved.StatusFrameName,
                resolved.StripBottomY,
                isIbisSection: true,
                stubContentRotationZ: contentRotZ,
                stubFontRef: fontRef,
                stubContentBand: Vector2.up,
                stubForcePortraitLayout: true);
            return true;
        }

        /// <summary>
        /// Left MFD bezel is portrait: canvas Y тЖТ screen horizontal, canvas X тЖТ screen vertical.
        /// Widen Y; cap X above FUEL/StatusGauges.
        /// </summary>
        private static PanelRectState BuildIbisWeaponOverlayZone(
            RectTransform weaponPanel,
            PanelRectState stripUnion,
            Canvas canvas,
            GameObject mfdRoot)
        {
            PanelRectState panelZone = PanelRectNormalizer.CaptureOnCanvas(weaponPanel, canvas);

            const float horizontalInset = 0.007f;
            const float verticalFloorInset = 0.005f;

            float minY = Mathf.Max(0.01f, panelZone.AnchorMin.y + horizontalInset);
            float maxY = panelZone.AnchorMax.y - horizontalInset;

            float minX = Mathf.Min(stripUnion.AnchorMin.x, panelZone.AnchorMin.x);
            float statusFloorX = TryFindIbisStatusFloorMinX(weaponPanel, mfdRoot, canvas);
            float panelSpanX = panelZone.AnchorMax.x - panelZone.AnchorMin.x;
            float fallbackMaxX = panelZone.AnchorMin.x + panelSpanX * 0.69f;
            float maxX = statusFloorX > minX + 0.05f
                ? statusFloorX - verticalFloorInset
                : fallbackMaxX - verticalFloorInset;
            maxX = Mathf.Clamp(maxX, minX + 0.08f, panelZone.AnchorMax.x);

            return new PanelRectState(
                new Vector2(minX, minY),
                new Vector2(maxX, maxY),
                Vector2.zero,
                Vector2.zero);
        }

        private static float TryFindIbisStatusFloorMinX(
            RectTransform weaponPanel,
            GameObject mfdRoot,
            Canvas canvas)
        {
            PanelRectState panelZone = PanelRectNormalizer.CaptureOnCanvas(weaponPanel, canvas);
            float best = 0f;

            foreach (StatusGauges gauges in mfdRoot.GetComponentsInChildren<StatusGauges>(true))
            {
                if (gauges == null || !gauges.TryGetComponent(out RectTransform gaugeRt))
                    continue;

                PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(gaugeRt, canvas);
                if (zone.AnchorMax.y < panelZone.AnchorMin.y + 0.04f
                    || zone.AnchorMin.y > panelZone.AnchorMax.y + 0.06f)
                {
                    continue;
                }

                if (zone.AnchorMin.x <= panelZone.AnchorMin.x + 0.03f)
                    continue;

                if (best <= 0f || zone.AnchorMin.x < best)
                    best = zone.AnchorMin.x;
            }

            return best;
        }

        /// <summary>UH-90: TopView + Box_* / profile strip on left WeaponPanel (FUEL/HEAT below).</summary>
        private static List<RectTransform> CollectIbisStripChildren(RectTransform weaponPanel)
        {
            var nodes = CollectIfritStripChildren(weaponPanel);
            TryAddNamedStripChild(weaponPanel, nodes, "TopView");
            TryAddNamedStripChild(weaponPanel, nodes, "topView");

            if (nodes.Count >= 2)
                return nodes;

            for (int i = 0; i < weaponPanel.childCount; i++)
            {
                RectTransform? child = weaponPanel.GetChild(i).GetComponent<RectTransform>();
                if (child == null || nodes.Contains(child) || PanelContainsEngineGauges(child))
                    continue;

                if (!ContainsIbisWeaponsMarkerText(child))
                    continue;

                nodes.Add(child);
            }

            return nodes;
        }

        private static void TryAddNamedStripChild(
            RectTransform weaponPanel,
            List<RectTransform> nodes,
            string childName)
        {
            Transform? child = weaponPanel.Find(childName);
            if (child != null
                && child.TryGetComponent(out RectTransform childRt)
                && !nodes.Contains(childRt))
            {
                nodes.Add(childRt);
            }
        }

        private static bool ContainsIbisWeaponsMarkerText(RectTransform root)
        {
            foreach (Text text in root.GetComponentsInChildren<Text>(true))
            {
                if (text != null && IsIbisWeaponsMarker(text.text))
                    return true;
            }

            return false;
        }

        private static bool IsIbisWeaponsMarker(string raw)
        {
            string norm = Normalize(raw);
            if (string.IsNullOrEmpty(norm))
                return false;

            if (norm.Contains("WEAPON ARMED") || norm.Contains("GUN ARMED"))
                return true;

            if (norm.Contains("TIP") && norm.Contains("ARMED"))
                return true;
            if (norm.Contains("PYLON") && norm.Contains("ARMED"))
                return true;
            if (norm.Contains("BAY") && norm.Contains("ARMED"))
                return true;

            return norm.Contains("GUN") && norm.Contains("ARMED");
        }

        private static void MaybeLogIbisDiscoveryFailure(GameObject mfdRoot, Canvas canvas)
        {
            if (_ibisDiagDone)
                return;

            _ibisDiagDone = true;
            RectTransform? weaponPanel = FindWeaponPanel(mfdRoot);
            string weaponPanelNorm = weaponPanel != null
                ? FormatAnchors(PanelRectNormalizer.CaptureOnCanvas(weaponPanel, canvas))
                : "null";

            MfdLog.Info($"ibis weaponArmed discovery failed WeaponPanel={weaponPanelNorm}");
            if (weaponPanel == null)
                return;

            for (int i = 0; i < weaponPanel.childCount; i++)
            {
                RectTransform? child = weaponPanel.GetChild(i).GetComponent<RectTransform>();
                if (child == null)
                    continue;

                PanelRectState childNorm = PanelRectNormalizer.CaptureOnCanvas(child, canvas);
                MfdLog.Info(
                    $"  ibis WeaponPanel child[{i}] {child.name} norm={FormatAnchors(childNorm)} " +
                    $"gauges={PanelContainsEngineGauges(child)} strip={IsIfritStripChild(child)}");
            }
        }

        private static bool IsChicaneAircraft(string? jsonKey) =>
            TacScreenAccess.IsChicaneAircraft(jsonKey);

        private static bool TryResolveChicaneEngineSection(
            GameObject mfdRoot,
            TacScreen tacScreen,
            Canvas canvas,
            out ResolvedPanel resolved)
        {
            resolved = default;
            Component? aircraft = TacScreenAccess.GetAircraft(tacScreen);
            Transform? aircraftRoot = aircraft != null ? aircraft.transform : null;

            List<RectTransform> hideTargets = CollectChicaneEngineHideTargets(
                mfdRoot,
                aircraftRoot,
                canvas,
                out Canvas overlayCanvas);

            if (hideTargets.Count < 2)
                return false;

            PanelRectState zone = PanelRectNormalizer.UnionOnCanvas(overlayCanvas, hideTargets);
            if (!PanelRectNormalizer.IsChicaneEngineZone(zone))
            {
                MfdLog.Info(
                    $"chicane engine zone rejected union={FormatAnchors(zone)} hideChildren={hideTargets.Count}");
                return false;
            }

            MfdLog.Info(
                $"chicane engine source=turbineL+R hideChildren={hideTargets.Count} " +
                $"zone={FormatAnchors(zone)} panels={FormatPanelNames(hideTargets)}");

            resolved = new ResolvedPanel(
                hideTargets[0],
                overlayCanvas,
                zone,
                hideTargets,
                isChicaneEngineSection: true,
                overlayOnly: true);
            return true;
        }

        /// <summary>SAH-46: L/R TURBINE blocks only тАФ TAIL DUCT stays vanilla.</summary>
        private static List<RectTransform> CollectChicaneEngineHideTargets(
            GameObject mfdRoot,
            Transform? aircraftRoot,
            Canvas canvas,
            out Canvas overlayCanvas)
        {
            overlayCanvas = canvas;
            var targets = new List<RectTransform>();
            var seen = new HashSet<RectTransform>();
            Canvas resolvedOverlay = canvas;

            void ScanRoot(Transform root)
            {
                foreach (Text label in root.GetComponentsInChildren<Text>(true))
                {
                    if (label == null || !IsChicaneTurbineBlockMarker(label.text))
                        continue;

                    if (!label.TryGetComponent(out RectTransform labelRt))
                        continue;

                    resolvedOverlay = TacScreenAccess.GetOverlayCanvas(labelRt) ?? canvas;
                    RectTransform? section = ResolveChicaneEngineSectionFromLabel(label, canvas);
                    if (section == null || !seen.Add(section))
                        continue;

                    targets.Add(section);
                }
            }

            ScanRoot(mfdRoot.transform);
            if (aircraftRoot != null && aircraftRoot != mfdRoot.transform)
                ScanRoot(aircraftRoot);

            if (targets.Count > 2)
            {
                targets.Sort((a, b) =>
                {
                    float ay = PanelRectNormalizer.CaptureOnCanvas(a, canvas).AnchorMax.y;
                    float by = PanelRectNormalizer.CaptureOnCanvas(b, canvas).AnchorMax.y;
                    return by.CompareTo(ay);
                });
                targets.RemoveRange(2, targets.Count - 2);
            }

            overlayCanvas = resolvedOverlay;
            return targets;
        }

        private static bool IsChicaneTurbineBlockMarker(string raw)
        {
            string norm = Normalize(raw);
            if (string.IsNullOrEmpty(norm))
                return false;

            if (norm.Contains("TAIL") || norm.Contains("DUCT"))
                return false;

            return norm.Contains("TURBINE");
        }

        private static bool IsChicaneTailDuctMarker(string raw)
        {
            string norm = Normalize(raw);
            return norm.Contains("TAIL") && norm.Contains("DUCT");
        }

        private static bool ContainsChicaneTailDuctMarker(RectTransform section)
        {
            foreach (Text label in section.GetComponentsInChildren<Text>(true))
            {
                if (label != null && IsChicaneTailDuctMarker(label.text))
                    return true;
            }

            return false;
        }

        private static RectTransform? ResolveChicaneEngineSectionFromLabel(Text label, Canvas canvas)
        {
            if (!label.TryGetComponent(out RectTransform start))
                return null;

            RectTransform? best = null;
            float bestArea = float.MaxValue;
            RectTransform? current = start;

            for (int depth = 0; depth < 12 && current != null; depth++)
            {
                if (current.GetComponent<Canvas>() != null)
                    break;

                if (ContainsChicaneTailDuctMarker(current))
                {
                    current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
                    continue;
                }

                bool hasGauges = current.GetComponentInChildren<StatusGauges>(true) != null
                    || current.GetComponentInChildren<EngineTelemetry>(true) != null
                    || current.GetComponentInChildren<RPMGauge>(true) != null;

                if (!hasGauges)
                {
                    current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
                    continue;
                }

                PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(current, canvas);
                if (zone.AnchorMin.x < 0.40f)
                {
                    current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
                    continue;
                }

                float area = current.rect.width * current.rect.height;
                if (area < bestArea)
                {
                    bestArea = area;
                    best = current;
                }

                current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
            }

            return best;
        }

        private static string FormatPanelNames(IReadOnlyList<RectTransform> panels)
        {
            if (panels.Count == 0)
                return "none";

            var names = new string[panels.Count];
            for (int i = 0; i < panels.Count; i++)
                names[i] = panels[i].name;

            return string.Join("+", names);
        }

        private static void MaybeLogChicaneDiscoveryFailure(
            GameObject mfdRoot,
            TacScreen tacScreen,
            Canvas canvas)
        {
            if (_chicaneDiagDone)
                return;

            _chicaneDiagDone = true;
            Component? aircraft = TacScreenAccess.GetAircraft(tacScreen);
            int turbineLabels = 0;
            int tailDuctLabels = 0;

            void CountLabels(Transform root)
            {
                foreach (Text label in root.GetComponentsInChildren<Text>(true))
                {
                    if (label == null)
                        continue;

                    if (IsChicaneTurbineBlockMarker(label.text))
                        turbineLabels++;
                    else if (IsChicaneTailDuctMarker(label.text))
                        tailDuctLabels++;
                }
            }

            CountLabels(mfdRoot.transform);
            if (aircraft != null)
                CountLabels(aircraft.transform);

            MfdLog.Info(
                $"chicane engine discovery failed root={mfdRoot.name} turbineLabels={turbineLabels} " +
                $"tailDuctLabels={tailDuctLabels}");

            RectTransform? host = canvas.GetComponent<RectTransform>();
            if (host == null)
                return;

            int childDump = Mathf.Min(host.childCount, 12);
            for (int i = 0; i < childDump; i++)
            {
                RectTransform? child = host.GetChild(i).GetComponent<RectTransform>();
                if (child == null)
                    continue;

                PanelRectState childNorm = PanelRectNormalizer.CaptureOnCanvas(child, canvas);
                MfdLog.Info(
                    $"  chicane canvasChild[{i}] {child.name} norm={FormatAnchors(childNorm)} " +
                    $"gauges={PanelContainsEngineGauges(child)}");
            }
        }

        private static bool TryResolveTarantulaWeaponArmedSection(
            GameObject mfdRoot,
            Canvas canvas,
            out ResolvedPanel resolved)
        {
            resolved = default;
            RectTransform? weaponPanel = FindWeaponPanel(mfdRoot);
            if (weaponPanel == null)
                return false;

            Canvas overlayCanvas = TacScreenAccess.GetOverlayCanvas(weaponPanel) ?? canvas;
            List<RectTransform> hideTargets = CollectTarantulaKnownChildren(weaponPanel);
            if (hideTargets.Count < 2)
                hideTargets = CollectTarantulaStripChildren(weaponPanel);

            if (hideTargets.Count < 2)
                return false;

            PanelRectState zone = PanelRectNormalizer.UnionOnCanvas(overlayCanvas, hideTargets);
            if (!PanelRectNormalizer.IsWeaponsReplacementZone(zone))
            {
                MfdLog.Info(
                    $"tarantula zone rejected union={FormatAnchors(zone)} hideChildren={hideTargets.Count}");
                return false;
            }

            MfdLog.Info(
                $"tarantula weaponArmed source=known hideRoot={weaponPanel.name} hideChildren={hideTargets.Count} " +
                $"zone={FormatAnchors(zone)}");

            resolved = new ResolvedPanel(
                weaponPanel,
                overlayCanvas,
                zone,
                hideTargets,
                isTarantulaSection: true);
            return true;
        }

        /// <summary>VL-49: WeaponPanel children from tacScreen_QuadVTOL (see Player.log).</summary>
        private static List<RectTransform> CollectTarantulaKnownChildren(RectTransform weaponPanel)
        {
            var targets = new List<RectTransform>();

            for (int i = 0; i < weaponPanel.childCount; i++)
            {
                RectTransform? child = weaponPanel.GetChild(i).GetComponent<RectTransform>();
                if (child == null || !IsTarantulaWeaponPanelChild(child.name))
                    continue;

                targets.Add(child);
            }

            return targets;
        }

        private static bool IsTarantulaWeaponPanelChild(string name) =>
            string.Equals(name, "TopView", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "CargoRear", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "CargoForward", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "PylonLeft", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "PylonRight", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "ForwardTurret", System.StringComparison.OrdinalIgnoreCase);

        private static List<RectTransform> CollectTarantulaStripChildren(RectTransform weaponPanel)
        {
            var nodes = CollectTarantulaKnownChildren(weaponPanel);
            if (nodes.Count >= 2)
                return nodes;

            for (int i = 0; i < weaponPanel.childCount; i++)
            {
                RectTransform? child = weaponPanel.GetChild(i).GetComponent<RectTransform>();
                if (child == null || nodes.Contains(child) || PanelContainsEngineGauges(child))
                    continue;

                if (child.GetComponentInChildren<StatusGauges>(true) != null)
                    continue;

                if (IsIfritStripChild(child) || ContainsTarantulaWeaponsMarkerText(child))
                    nodes.Add(child);
            }

            return nodes;
        }

        private static bool ContainsTarantulaWeaponsMarkerText(RectTransform root)
        {
            foreach (Text text in root.GetComponentsInChildren<Text>(true))
            {
                if (text != null && IsTarantulaWeaponsMarker(text.text))
                    return true;
            }

            return false;
        }

        private static bool IsTarantulaWeaponsMarker(string raw)
        {
            string norm = Normalize(raw);
            if (string.IsNullOrEmpty(norm))
                return false;

            if (norm.Contains("GUN") && norm.Contains("ARMED"))
                return true;

            if (norm.Contains("PYLON") && norm.Contains("ARMED"))
                return true;

            return norm.Contains("CARGO") && norm.Contains("READY");
        }

        private static void MaybeLogTarantulaDiscoveryFailure(GameObject mfdRoot, Canvas canvas)
        {
            if (_tarantulaDiagDone)
                return;

            _tarantulaDiagDone = true;
            RectTransform? weaponPanel = FindWeaponPanel(mfdRoot);
            string weaponPanelNorm = weaponPanel != null
                ? FormatAnchors(PanelRectNormalizer.CaptureOnCanvas(weaponPanel, canvas))
                : "n/a";

            MfdLog.Info($"tarantula discovery failed WeaponPanel={weaponPanelNorm}");
            if (weaponPanel == null)
                return;

            for (int i = 0; i < weaponPanel.childCount; i++)
            {
                RectTransform? child = weaponPanel.GetChild(i).GetComponent<RectTransform>();
                if (child == null)
                    continue;

                PanelRectState childNorm = PanelRectNormalizer.CaptureOnCanvas(child, canvas);
                MfdLog.Info(
                    $"  WeaponPanel child[{i}] {child.name} norm={FormatAnchors(childNorm)} " +
                    $"tarantulaMarker={ContainsTarantulaWeaponsMarkerText(child)}");
            }
        }

        private static bool TryResolveEngineSectionPanel(
            GameObject mfdRoot,
            TacScreen tacScreen,
            Canvas canvas,
            string? aircraftJsonKey,
            out ResolvedPanel resolved)
        {
            resolved = default;
            List<RectTransform> hideTargets;
            Canvas overlayCanvas;

            if (TryGetKnownEnginePanels(mfdRoot, canvas, out hideTargets, out overlayCanvas))
            {
                MfdLog.Info($"engineSection source=known engPanel1+engPanel2 jsonKey={aircraftJsonKey}");
            }
            else
            {
                Component? aircraft = TacScreenAccess.GetAircraft(tacScreen);
                Transform? aircraftRoot = aircraft != null ? aircraft.transform : null;

                hideTargets = CollectCompassEngineTelemetryTargets(
                    mfdRoot,
                    aircraftRoot,
                    canvas,
                    out overlayCanvas);

                if (hideTargets.Count == 0)
                {
                    RectTransform? weaponPanel = FindWeaponPanel(mfdRoot);
                    if (weaponPanel != null)
                    {
                        overlayCanvas = TacScreenAccess.GetOverlayCanvas(weaponPanel) ?? canvas;
                        hideTargets = CollectCompassEngineHideTargets(weaponPanel, mfdRoot, overlayCanvas);
                    }
                    else
                    {
                        hideTargets = CollectCompassEngineHideTargetsFromMfdRoot(mfdRoot, canvas, out overlayCanvas);
                    }
                }
            }

            if (hideTargets.Count == 0)
                return false;

            PanelRectState zone = PanelRectNormalizer.UnionOnCanvas(overlayCanvas, hideTargets);
            zone = ClampCompassEngineZone(zone);

            if (!PanelRectNormalizer.IsCompassEngineZone(zone))
            {
                MfdLog.Info(
                    $"engineSection zone rejected jsonKey={aircraftJsonKey} union={FormatAnchors(zone)} " +
                    $"hideChildren={hideTargets.Count}");
                return false;
            }

            MfdLog.Info(
                $"engineSection hideRoot={hideTargets[0].name} hideChildren={hideTargets.Count} " +
                $"engineCanvas={FormatAnchors(zone)} jsonKey={aircraftJsonKey}");

            resolved = new ResolvedPanel(
                hideTargets[0],
                overlayCanvas,
                zone,
                hideTargets,
                isCompassEngineSection: true);
            return true;
        }

        /// <summary>
        /// VT-7 Vagrant: replace stacked NOZZLE + ENGINE gauges on the right MFD column.
        /// Weapons silhouette above stays visible.
        /// </summary>
        private static bool TryResolveVagrantNozzleEngineSection(
            GameObject mfdRoot,
            TacScreen tacScreen,
            Canvas canvas,
            string? aircraftJsonKey,
            out ResolvedPanel resolved)
        {
            resolved = default;

            Component? aircraft = TacScreenAccess.GetAircraft(tacScreen);
            Transform? aircraftRoot = aircraft != null ? aircraft.transform : null;

            List<RectTransform> hideTargets = CollectVagrantNozzleEngineTargets(
                mfdRoot,
                aircraftRoot,
                canvas,
                out Canvas overlayCanvas);

            if (hideTargets.Count == 0)
                hideTargets = CollectVagrantRawGaugeRects(mfdRoot, aircraftRoot, overlayCanvas);

            if (hideTargets.Count == 0)
                return false;

            // Only collapse to a common parent when it still fits the NOZZLE+ENGINE band
            // (otherwise climbing pulls in the weapons silhouette тЖТ reject at yтЙИ0.95).
            RectTransform? commonParent = TryFindVagrantCommonParent(hideTargets, overlayCanvas);
            List<RectTransform> finalTargets = hideTargets;
            if (commonParent != null)
                finalTargets = new List<RectTransform> { commonParent };

            PanelRectState discovered = PanelRectNormalizer.UnionOnCanvas(overlayCanvas, finalTargets);
            PanelRectState zone = FitVagrantOverlayZone(discovered);

            // Prefer chrome parent size when it wraps NOZZLE+ENGINE without weapons silhouette.
            RectTransform? chrome = TryFindVagrantChromeParent(finalTargets, overlayCanvas);
            if (chrome != null)
            {
                PanelRectState chromeZone = FitVagrantOverlayZone(
                    PanelRectNormalizer.CaptureOnCanvas(chrome, overlayCanvas));
                float chromeArea = (chromeZone.AnchorMax.x - chromeZone.AnchorMin.x)
                    * (chromeZone.AnchorMax.y - chromeZone.AnchorMin.y);
                float zoneArea = (zone.AnchorMax.x - zone.AnchorMin.x)
                    * (zone.AnchorMax.y - zone.AnchorMin.y);
                if (PanelRectNormalizer.IsVagrantNozzleEngineZone(chromeZone) && chromeArea >= zoneArea * 0.95f)
                {
                    zone = chromeZone;
                    if (!finalTargets.Contains(chrome))
                        finalTargets = new List<RectTransform>(finalTargets) { chrome };
                }
            }

            if (!PanelRectNormalizer.IsVagrantNozzleEngineZone(zone))
            {
                MfdLog.Info(
                    $"vagrant nozzle/engine fit rejected jsonKey={aircraftJsonKey} " +
                    $"discovered={FormatAnchors(discovered)} fit={FormatAnchors(zone)} " +
                    $"hideChildren={finalTargets.Count}");
                return false;
            }

            MfdLog.Info(
                $"vagrant nozzle/engine hideRoot={finalTargets[0].name} hideChildren={finalTargets.Count} " +
                $"discovered={FormatAnchors(discovered)} zone={FormatAnchors(zone)} jsonKey={aircraftJsonKey}");

            resolved = new ResolvedPanel(
                finalTargets[0],
                overlayCanvas,
                zone,
                finalTargets,
                isVagrantNozzleEngineSection: true);
            return true;
        }

        private static List<RectTransform> CollectVagrantNozzleEngineTargets(
            GameObject mfdRoot,
            Transform? aircraftRoot,
            Canvas canvas,
            out Canvas overlayCanvas)
        {
            overlayCanvas = canvas;
            var targets = new List<RectTransform>();
            var seen = new HashSet<RectTransform>();
            Canvas resolvedOverlay = canvas;

            void TryAdd(RectTransform? frame)
            {
                if (frame == null || !seen.Add(frame))
                    return;

                if (!IsVagrantEngineBandFrame(frame, canvas))
                    return;

                if (IsVagrantWeaponsSilhouetteNode(frame))
                    return;

                resolvedOverlay = TacScreenAccess.GetOverlayCanvas(frame) ?? canvas;
                targets.Add(frame);
            }

            void ScanRoot(Transform root)
            {
                foreach (NozzleGauge nozzle in root.GetComponentsInChildren<NozzleGauge>(true))
                {
                    if (nozzle == null || !nozzle.TryGetComponent(out RectTransform nozzleRt))
                        continue;

                    TryAdd(ResolveVagrantGaugeFrame(nozzleRt, canvas));
                }

                foreach (EngineTelemetry telemetry in root.GetComponentsInChildren<EngineTelemetry>(true))
                {
                    if (telemetry == null || !telemetry.TryGetComponent(out RectTransform telemetryRt))
                        continue;

                    TryAdd(ResolveVagrantGaugeFrame(telemetryRt, canvas));
                }

                // RPM lives under ENGINE on Vagrant тАФ only take if already under an accepted engine frame.
                foreach (RPMGauge rpm in root.GetComponentsInChildren<RPMGauge>(true))
                {
                    if (rpm == null || !rpm.TryGetComponent(out RectTransform rpmRt))
                        continue;

                    if (rpmRt.GetComponentInParent<EngineTelemetry>() == null
                        && !IsVagrantEngineRpmNode(rpmRt))
                        continue;

                    TryAdd(ResolveVagrantGaugeFrame(rpmRt, canvas));
                }
            }

            ScanRoot(mfdRoot.transform);
            if (aircraftRoot != null && aircraftRoot != mfdRoot.transform)
                ScanRoot(aircraftRoot);

            RectTransform? knownNozzle = FindPanelByName(mfdRoot, "NozzlePanel")
                ?? FindPanelByName(mfdRoot, "nozzlePanel");
            RectTransform? knownEngine = FindPanelByName(mfdRoot, "EnginePanel")
                ?? FindPanelByName(mfdRoot, "enginePanel")
                ?? FindPanelByName(mfdRoot, "engPanel")
                ?? FindPanelByName(mfdRoot, "EngPanel");
            if (knownNozzle != null)
                TryAdd(knownNozzle);
            if (knownEngine != null)
                TryAdd(knownEngine);

            overlayCanvas = resolvedOverlay;
            return targets;
        }

        private static bool IsVagrantEngineBandFrame(RectTransform frame, Canvas canvas)
        {
            PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(frame, canvas);
            float w = zone.AnchorMax.x - zone.AnchorMin.x;
            float h = zone.AnchorMax.y - zone.AnchorMin.y;
            float midY = (zone.AnchorMin.y + zone.AnchorMax.y) * 0.5f;

            // Right column, below weapons silhouette.
            if (zone.AnchorMax.x < 0.50f)
                return false;
            if (zone.AnchorMin.x > 0.92f)
                return false;
            if (midY > 0.82f || zone.AnchorMax.y > 0.90f)
                return false;
            if (w > 0.55f || h > 0.60f)
                return false;
            if (h < 0.04f || w < 0.06f)
                return false;

            return true;
        }

        private static List<RectTransform> FilterVagrantTargetsToEngineBand(
            IReadOnlyList<RectTransform> hideTargets,
            Canvas canvas)
        {
            var filtered = new List<RectTransform>(hideTargets.Count);
            for (int i = 0; i < hideTargets.Count; i++)
            {
                RectTransform target = hideTargets[i];
                if (target == null)
                    continue;
                if (!IsVagrantEngineBandFrame(target, canvas))
                    continue;
                if (IsVagrantWeaponsSilhouetteNode(target))
                    continue;
                filtered.Add(target);
            }

            return filtered;
        }

        private static List<RectTransform> CollectVagrantRawGaugeRects(
            GameObject mfdRoot,
            Transform? aircraftRoot,
            Canvas canvas)
        {
            var targets = new List<RectTransform>();
            var seen = new HashSet<RectTransform>();

            void Scan(Transform root)
            {
                foreach (NozzleGauge nozzle in root.GetComponentsInChildren<NozzleGauge>(true))
                {
                    if (nozzle == null || !nozzle.TryGetComponent(out RectTransform rt) || !seen.Add(rt))
                        continue;
                    if (PanelRectNormalizer.CaptureOnCanvas(rt, canvas).AnchorMax.x < 0.40f)
                        continue;
                    targets.Add(rt);
                }

                foreach (EngineTelemetry telemetry in root.GetComponentsInChildren<EngineTelemetry>(true))
                {
                    if (telemetry == null || !telemetry.TryGetComponent(out RectTransform rt) || !seen.Add(rt))
                        continue;
                    if (PanelRectNormalizer.CaptureOnCanvas(rt, canvas).AnchorMax.x < 0.40f)
                        continue;
                    targets.Add(rt);
                }
            }

            Scan(mfdRoot.transform);
            if (aircraftRoot != null && aircraftRoot != mfdRoot.transform)
                Scan(aircraftRoot);

            return targets;
        }

        /// <summary>
        /// Soft-clamp discovery union into the right column. Size comes from NOZZLE+ENGINE gauges;
        /// only block bleed into FUEL/HEAT (minX) and weapons silhouette (maxY).
        /// </summary>
        private static PanelRectState FitVagrantOverlayZone(PanelRectState discovered)
        {
            float minXFloor = 0.62f;
            float maxYCeil = 0.79f;

            float minX = Mathf.Max(discovered.AnchorMin.x, minXFloor);
            float maxX = Mathf.Min(discovered.AnchorMax.x, 0.995f);
            float minY = Mathf.Max(discovered.AnchorMin.y, 0.24f);
            float maxY = Mathf.Min(discovered.AnchorMax.y, maxYCeil);

            if (maxX - minX < 0.18f || maxY - minY < 0.18f)
            {
                minX = minXFloor;
                maxX = 0.99f;
                minY = 0.28f;
                maxY = maxYCeil;
            }

            return new PanelRectState(
                new Vector2(minX, minY),
                new Vector2(maxX, maxY),
                Vector2.zero,
                Vector2.zero);
        }

        private static RectTransform? TryFindVagrantChromeParent(
            IReadOnlyList<RectTransform> hideTargets,
            Canvas canvas)
        {
            if (hideTargets.Count == 0)
                return null;

            RectTransform? current = hideTargets[0].parent as RectTransform;
            for (int depth = 0; depth < 5 && current != null; depth++)
            {
                if (IsVagrantWeaponsSilhouetteNode(current))
                    return null;

                bool coversAll = true;
                for (int i = 0; i < hideTargets.Count; i++)
                {
                    if (!IsDescendantOrSelf(current, hideTargets[i]))
                    {
                        coversAll = false;
                        break;
                    }
                }

                if (coversAll)
                {
                    PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(current, canvas);
                    float h = zone.AnchorMax.y - zone.AnchorMin.y;
                    if (zone.AnchorMin.x >= 0.55f
                        && zone.AnchorMax.y <= 0.82f
                        && h <= 0.58f
                        && h >= 0.20f)
                        return current;
                }

                current = current.parent as RectTransform;
            }

            return null;
        }

        private static PanelRectState ClampVagrantOverlayZone(PanelRectState zone) =>
            FitVagrantOverlayZone(zone);

        private static bool IsVagrantWeaponsSilhouetteNode(RectTransform node)
        {
            string name = node.name;
            if (name.IndexOf("Weapon", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Profile", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("TopView", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Hardpoint", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Armed", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            Transform? current = node;
            for (int depth = 0; depth < 6 && current != null; depth++)
            {
                string n = current.name;
                if (string.Equals(n, "WeaponPanel", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(n, "weaponPanel", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(n, "weaponStations", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(n, "frontProfile", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(n, "TopView", System.StringComparison.OrdinalIgnoreCase))
                    return true;

                current = current.parent;
            }

            return false;
        }

        private static bool IsVagrantEngineRpmNode(RectTransform rpmRt)
        {
            Transform? current = rpmRt;
            for (int depth = 0; depth < 6 && current != null; depth++)
            {
                string n = current.name;
                if (n.IndexOf("Engine", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("THRUST", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("RPM", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("Nozzle", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                current = current.parent;
            }

            return rpmRt.GetComponentInParent<EngineTelemetry>() != null;
        }

        private static RectTransform ResolveVagrantGaugeFrame(RectTransform gaugeRt, Canvas canvas)
        {
            // Prefer the gauge itself or a tight local frame тАФ never climb into the full right column
            // (that pulls weapons silhouette and rejects at maxYтЙИ0.95).
            RectTransform? best = null;
            float bestScore = float.MaxValue;
            RectTransform? current = gaugeRt;

            for (int depth = 0; depth < 4 && current != null; depth++)
            {
                if (IsVagrantWeaponsSilhouetteNode(current))
                    break;

                PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(current, canvas);
                float w = zone.AnchorMax.x - zone.AnchorMin.x;
                float h = zone.AnchorMax.y - zone.AnchorMin.y;
                if (zone.AnchorMax.x < 0.42f
                    || zone.AnchorMax.y > 0.86f
                    || w > 0.58f
                    || h > 0.48f
                    || zone.AnchorMin.y > 0.72f)
                {
                    break;
                }

                if (!IsVagrantEngineBandFrame(current, canvas))
                {
                    current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
                    continue;
                }

                // Prefer mid-sized frames that still cover the gauge chrome.
                float score = Mathf.Abs(h - 0.22f) + Mathf.Abs(w - 0.35f);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = current;
                }

                current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
            }

            if (best != null)
                return best;

            return IsVagrantEngineBandFrame(gaugeRt, canvas) ? gaugeRt : gaugeRt;
        }

        private static RectTransform? TryFindVagrantCommonParent(
            IReadOnlyList<RectTransform> hideTargets,
            Canvas canvas)
        {
            if (hideTargets.Count < 2)
                return null;

            RectTransform? current = hideTargets[0];
            for (int depth = 0; depth < 5 && current != null; depth++)
            {
                if (IsVagrantWeaponsSilhouetteNode(current))
                    return null;

                bool coversAll = true;
                for (int i = 0; i < hideTargets.Count; i++)
                {
                    if (!IsDescendantOrSelf(current, hideTargets[i]))
                    {
                        coversAll = false;
                        break;
                    }
                }

                if (coversAll)
                {
                    PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(current, canvas);
                    if (PanelRectNormalizer.IsVagrantNozzleEngineZone(zone)
                        && IsVagrantEngineBandFrame(current, canvas)
                        && !IsVagrantWeaponsSilhouetteNode(current))
                        return current;
                }

                current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
            }

            return null;
        }

        private static bool IsDescendantOrSelf(RectTransform ancestor, RectTransform node)
        {
            Transform? current = node;
            while (current != null)
            {
                if (current == ancestor)
                    return true;
                current = current.parent;
            }

            return false;
        }

        private static void MaybeLogVagrantDiscoveryFailure(
            GameObject mfdRoot,
            TacScreen tacScreen,
            Canvas canvas,
            string? aircraftJsonKey)
        {
            int nozzles = mfdRoot.GetComponentsInChildren<NozzleGauge>(true).Length;
            int telemetry = mfdRoot.GetComponentsInChildren<EngineTelemetry>(true).Length;
            int rpm = mfdRoot.GetComponentsInChildren<RPMGauge>(true).Length;
            Component? aircraft = TacScreenAccess.GetAircraft(tacScreen);
            int nozzlesOnAircraft = 0;
            int telemetryOnAircraft = 0;
            if (aircraft != null)
            {
                nozzlesOnAircraft = aircraft.GetComponentsInChildren<NozzleGauge>(true).Length;
                telemetryOnAircraft = aircraft.GetComponentsInChildren<EngineTelemetry>(true).Length;
            }

            foreach (NozzleGauge nozzle in mfdRoot.GetComponentsInChildren<NozzleGauge>(true))
            {
                if (nozzle != null && nozzle.TryGetComponent(out RectTransform rt))
                    MfdLog.Info($"vagrant nozzleRt={rt.name} {FormatAnchors(PanelRectNormalizer.CaptureOnCanvas(rt, canvas))}");
            }

            foreach (EngineTelemetry telemetryNode in mfdRoot.GetComponentsInChildren<EngineTelemetry>(true))
            {
                if (telemetryNode != null && telemetryNode.TryGetComponent(out RectTransform rt))
                    MfdLog.Info($"vagrant engineRt={rt.name} {FormatAnchors(PanelRectNormalizer.CaptureOnCanvas(rt, canvas))}");
            }

            MfdLog.Info(
                $"vagrant nozzle/engine discovery failed jsonKey={aircraftJsonKey} root={mfdRoot.name} " +
                $"canvas={canvas.name} nozzle={nozzles}/{nozzlesOnAircraft} " +
                $"telemetry={telemetry}/{telemetryOnAircraft} rpm={rpm}");
        }

        /// <summary>Compass / Brawler tac canvas: fixed engPanel1 (ENGINE L) + engPanel2 (ENGINE R) nodes.</summary>
        private static bool TryGetKnownEnginePanels(
            GameObject mfdRoot,
            Canvas canvas,
            out List<RectTransform> hideTargets,
            out Canvas overlayCanvas)
        {
            hideTargets = new List<RectTransform>(2);
            overlayCanvas = canvas;

            RectTransform? engPanel1 = FindPanelByName(mfdRoot, "engPanel1");
            RectTransform? engPanel2 = FindPanelByName(mfdRoot, "engPanel2");

            if (engPanel1 != null)
                hideTargets.Add(engPanel1);
            if (engPanel2 != null)
                hideTargets.Add(engPanel2);

            if (hideTargets.Count < 2)
                return false;

            overlayCanvas = TacScreenAccess.GetOverlayCanvas(engPanel1!) ?? canvas;
            return true;
        }

        private static List<RectTransform> CollectCompassEngineTelemetryTargets(
            GameObject mfdRoot,
            Transform? aircraftRoot,
            Canvas canvas,
            out Canvas overlayCanvas)
        {
            overlayCanvas = canvas;
            var targets = new List<RectTransform>();
            var seen = new HashSet<RectTransform>();
            Canvas resolvedOverlay = canvas;

            void ScanRoot(Transform root)
            {
                foreach (EngineTelemetry telemetry in root.GetComponentsInChildren<EngineTelemetry>(true))
                {
                    if (telemetry == null || !telemetry.TryGetComponent(out RectTransform telemetryRt))
                        continue;

                    PanelRectState telemetryZone = PanelRectNormalizer.CaptureOnCanvas(telemetryRt, canvas);
                    if (telemetryZone.AnchorMax.x < 0.45f)
                        continue;

                    resolvedOverlay = TacScreenAccess.GetOverlayCanvas(telemetryRt) ?? canvas;
                    RectTransform? frame = ResolveEngineTelemetryFrame(telemetry, canvas);
                    if (frame == null || !seen.Add(frame))
                        continue;

                    targets.Add(frame);
                }
            }

            ScanRoot(mfdRoot.transform);
            if (aircraftRoot != null && aircraftRoot != mfdRoot.transform)
                ScanRoot(aircraftRoot);

            overlayCanvas = resolvedOverlay;
            return targets;
        }

        private static RectTransform? ResolveEngineTelemetryFrame(EngineTelemetry telemetry, Canvas canvas)
        {
            if (!telemetry.TryGetComponent(out RectTransform gaugeRt))
                return null;

            RectTransform? best = null;
            float bestArea = float.MaxValue;
            RectTransform? current = gaugeRt;

            for (int depth = 0; depth < 10 && current != null; depth++)
            {
                if (!PanelContainsCompassEngineUi(current) || FindFrontProfileUnder(current) != null)
                {
                    current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
                    continue;
                }

                PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(current, canvas);
                float h = zone.AnchorMax.y - zone.AnchorMin.y;
                if (zone.AnchorMin.x < 0.45f || zone.AnchorMax.y > 0.68f || h < 0.04f)
                {
                    current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
                    continue;
                }

                float area = current.rect.width * current.rect.height;
                if (area < bestArea)
                {
                    bestArea = area;
                    best = current;
                }

                current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
            }

            return best ?? gaugeRt.parent?.GetComponent<RectTransform>() ?? gaugeRt;
        }

        private static bool PanelContainsCompassEngineUi(RectTransform panel) =>
            panel.GetComponentInChildren<EngineTelemetry>(true) != null
            || panel.GetComponentInChildren<StatusGauges>(true) != null
            || panel.GetComponentInChildren<RPMGauge>(true) != null;

        private static RectTransform? FindWeaponPanel(GameObject root) =>
            FindPanelByName(root, "WeaponPanel")
            ?? FindPanelByName(root, "weaponPanel")
            ?? FindPanelByName(root, "weaponStations");

        private static List<RectTransform> CollectCompassEngineHideTargetsFromMfdRoot(
            GameObject mfdRoot,
            Canvas canvas,
            out Canvas overlayCanvas)
        {
            overlayCanvas = canvas;
            var targets = new List<RectTransform>();
            var seen = new HashSet<RectTransform>();

            foreach (EngineTelemetry telemetry in mfdRoot.GetComponentsInChildren<EngineTelemetry>(true))
            {
                if (telemetry == null || !telemetry.TryGetComponent(out RectTransform telemetryRt))
                    continue;

                PanelRectState telemetryZone = PanelRectNormalizer.CaptureOnCanvas(telemetryRt, canvas);
                if (telemetryZone.AnchorMax.x < 0.45f)
                    continue;

                overlayCanvas = TacScreenAccess.GetOverlayCanvas(telemetryRt) ?? canvas;
                RectTransform? frame = ResolveEngineTelemetryFrame(telemetry, canvas);
                if (frame == null || !seen.Add(frame))
                    continue;

                targets.Add(frame);
            }

            if (targets.Count > 0)
                return targets;

            foreach (RPMGauge rpm in mfdRoot.GetComponentsInChildren<RPMGauge>(true))
            {
                if (rpm == null || !rpm.TryGetComponent(out RectTransform gaugeRt))
                    continue;

                PanelRectState gaugeZone = PanelRectNormalizer.CaptureOnCanvas(gaugeRt, canvas);
                if (gaugeZone.AnchorMax.x < 0.45f)
                    continue;

                overlayCanvas = TacScreenAccess.GetOverlayCanvas(gaugeRt) ?? canvas;
                RectTransform? frame = DiscoverCompassEngineFrameViaRpmOnGauge(gaugeRt, canvas);
                if (frame == null || !seen.Add(frame))
                    continue;

                targets.Add(frame);
            }

            if (targets.Count > 0)
                return targets;

            RectTransform? fromMarkers = DiscoverCompassEngineFrameViaMarkers(mfdRoot, null, canvas);
            if (fromMarkers != null)
            {
                overlayCanvas = TacScreenAccess.GetOverlayCanvas(fromMarkers) ?? canvas;
                return new List<RectTransform> { fromMarkers };
            }

            return targets;
        }

        private static RectTransform? DiscoverCompassEngineFrameViaRpmOnGauge(RectTransform gaugeRt, Canvas canvas)
        {
            RectTransform? best = null;
            float bestArea = float.MaxValue;
            RectTransform? current = gaugeRt;

            for (int depth = 0; depth < 10 && current != null; depth++)
            {
                if (!PanelContainsEngineGauges(current) || FindFrontProfileUnder(current) != null)
                {
                    current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
                    continue;
                }

                PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(current, canvas);
                float h = zone.AnchorMax.y - zone.AnchorMin.y;
                if (zone.AnchorMin.x < 0.45f || zone.AnchorMax.y > 0.65f || h < 0.06f)
                {
                    current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
                    continue;
                }

                float area = current.rect.width * current.rect.height;
                if (area < bestArea)
                {
                    bestArea = area;
                    best = current;
                }

                current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
            }

            return best ?? gaugeRt.parent?.GetComponent<RectTransform>() ?? gaugeRt;
        }

        private static PanelRectState ClampCompassEngineZone(PanelRectState zone)
        {
            const float minX = 0.55f;
            if (zone.AnchorMin.x >= minX)
                return zone;

            return new PanelRectState(
                new Vector2(minX, zone.AnchorMin.y),
                zone.AnchorMax,
                zone.OffsetMin,
                zone.OffsetMax);
        }

        private static List<RectTransform> CollectCompassEngineHideTargets(
            RectTransform weaponPanel,
            GameObject mfdRoot,
            Canvas canvas)
        {
            List<RectTransform> fromChildren = CollectCompassEngineDirectChildren(weaponPanel);
            if (fromChildren.Count > 0)
                return fromChildren;

            RectTransform? fromRpm = DiscoverCompassEngineFrameViaRpm(weaponPanel, canvas);
            if (fromRpm != null)
                return new List<RectTransform> { fromRpm };

            RectTransform? fromMarkers = DiscoverCompassEngineFrameViaMarkers(mfdRoot, weaponPanel, canvas);
            if (fromMarkers != null)
                return new List<RectTransform> { fromMarkers };

            return CollectCompassEngineGaugeRows(weaponPanel);
        }

        /// <summary>WeaponPanel direct children with RPM/Status gauges тАФ mirror Ifrit status-child scan.</summary>
        private static List<RectTransform> CollectCompassEngineDirectChildren(RectTransform weaponPanel)
        {
            var targets = new List<RectTransform>();

            for (int i = 0; i < weaponPanel.childCount; i++)
            {
                RectTransform? child = weaponPanel.GetChild(i).GetComponent<RectTransform>();
                if (child == null || IsIfritStripChild(child) || IsTimeObject(child))
                    continue;

                if (!PanelContainsEngineGauges(child) || FindFrontProfileUnder(child) != null)
                    continue;

                targets.Add(child);
            }

            return targets;
        }

        /// <summary>Inverse of Revoker DiscoverViaRpmGauge: smallest frame around engine gauges in lower right column.</summary>
        private static RectTransform? DiscoverCompassEngineFrameViaRpm(RectTransform weaponPanel, Canvas canvas)
        {
            RPMGauge? rpm = weaponPanel.GetComponentInChildren<RPMGauge>(true);
            if (rpm == null || !rpm.TryGetComponent(out RectTransform gaugeRt))
                return null;

            RectTransform? best = null;
            float bestArea = float.MaxValue;
            RectTransform? current = gaugeRt;

            for (int depth = 0; depth < 10 && current != null; depth++)
            {
                if (current == weaponPanel)
                    break;

                if (!PanelContainsEngineGauges(current) || FindFrontProfileUnder(current) != null)
                {
                    current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
                    continue;
                }

                PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(current, canvas);
                float h = zone.AnchorMax.y - zone.AnchorMin.y;
                if (zone.AnchorMin.x < 0.45f || zone.AnchorMax.y > 0.65f || h < 0.06f)
                {
                    current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
                    continue;
                }

                float area = current.rect.width * current.rect.height;
                if (area < bestArea)
                {
                    bestArea = area;
                    best = current;
                }

                current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
            }

            return best;
        }

        private static RectTransform? DiscoverCompassEngineFrameViaMarkers(
            GameObject mfdRoot,
            RectTransform? weaponPanel,
            Canvas canvas)
        {
            RectTransform? best = null;
            float bestTop = float.MaxValue;

            foreach (Text label in mfdRoot.GetComponentsInChildren<Text>(true))
            {
                if (label == null || string.IsNullOrEmpty(label.text))
                    continue;

                string trimmed = label.text.Trim();
                if (!IsCompassEngineMarkerText(trimmed))
                    continue;

                RectTransform? frame = FindCompassEngineOuterFrame(label.rectTransform, weaponPanel, canvas);
                if (frame == null)
                    continue;

                PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(frame, canvas);
                if (zone.AnchorMax.y >= bestTop)
                    continue;

                bestTop = zone.AnchorMax.y;
                best = frame;
            }

            return best;
        }

        private static bool IsCompassEngineMarkerText(string trimmed) =>
            trimmed.StartsWith("ENGINE", System.StringComparison.OrdinalIgnoreCase);

        private static RectTransform? FindCompassEngineOuterFrame(
            RectTransform? start,
            RectTransform? weaponPanel,
            Canvas canvas)
        {
            if (start == null)
                return null;

            RectTransform? best = null;
            float bestArea = float.MaxValue;
            RectTransform? rt = start;
            RectTransform? stopAt = weaponPanel?.parent?.GetComponent<RectTransform>();

            while (rt != null && rt != stopAt)
            {
                bool containsGauges = PanelContainsCompassEngineUi(rt);
                bool directChild = weaponPanel != null && rt.parent == weaponPanel;
                if (containsGauges || directChild)
                {
                    PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(rt, canvas);
                    float h = zone.AnchorMax.y - zone.AnchorMin.y;
                    if (h >= 0.06f
                        && zone.AnchorMin.x >= 0.45f
                        && zone.AnchorMax.y <= 0.65f
                        && FindFrontProfileUnder(rt) == null)
                    {
                        float area = rt.rect.width * rt.rect.height;
                        if (area < bestArea)
                        {
                            bestArea = area;
                            best = rt;
                        }
                    }
                }

                rt = rt.parent != null ? rt.parent.GetComponent<RectTransform>() : null;
            }

            return best;
        }

        private static List<RectTransform> CollectCompassEngineGaugeRows(RectTransform weaponPanel)
        {
            var rows = new List<RectTransform>();
            var seen = new HashSet<RectTransform>();

            foreach (RPMGauge rpm in weaponPanel.GetComponentsInChildren<RPMGauge>(true))
            {
                if (rpm == null || !rpm.TryGetComponent(out RectTransform rt))
                    continue;

                RectTransform? row = rt.parent != null ? rt.parent.GetComponent<RectTransform>() : rt;
                if (row == null || IsIfritStripChild(row) || !seen.Add(row))
                    continue;

                rows.Add(row);
            }

            foreach (StatusGauges gauges in weaponPanel.GetComponentsInChildren<StatusGauges>(true))
            {
                if (gauges == null || !gauges.TryGetComponent(out RectTransform rt))
                    continue;

                RectTransform? row = rt.parent != null ? rt.parent.GetComponent<RectTransform>() : rt;
                if (row == null || IsIfritStripChild(row) || !seen.Add(row))
                    continue;

                rows.Add(row);
            }

            if (rows.Count == 0)
                return rows;

            RectTransform? shared = FindLowestCommonAncestor(rows);
            if (shared != null
                && shared != weaponPanel
                && FindFrontProfileUnder(shared) == null
                && PanelContainsEngineGauges(shared))
            {
                return new List<RectTransform> { shared };
            }

            return rows;
        }

        private static void MaybeLogEngineSectionDiscoveryFailure(
            GameObject mfdRoot,
            TacScreen tacScreen,
            Canvas canvas,
            string? aircraftJsonKey)
        {
            if (_compassDiagDone)
                return;

            _compassDiagDone = true;
            Component? aircraft = TacScreenAccess.GetAircraft(tacScreen);
            Transform? aircraftRoot = aircraft != null ? aircraft.transform : null;

            RectTransform? weaponPanel = FindWeaponPanel(mfdRoot);
            string panelNorm = weaponPanel != null
                ? FormatAnchors(PanelRectNormalizer.CaptureOnCanvas(weaponPanel, canvas))
                : "null";

            int telemetryOnMfd = mfdRoot.GetComponentsInChildren<EngineTelemetry>(true).Length;
            int telemetryOnAircraft = aircraftRoot != null
                ? aircraftRoot.GetComponentsInChildren<EngineTelemetry>(true).Length
                : 0;
            int rpmCount = mfdRoot.GetComponentsInChildren<RPMGauge>(true).Length;
            RectTransform? frontProfile = FindFrontProfileInMfd(mfdRoot);
            int engineChildren = CollectCompassEngineTelemetryTargets(
                mfdRoot,
                aircraftRoot,
                canvas,
                out _).Count;

            if (engineChildren == 0)
            {
                engineChildren = weaponPanel != null
                    ? CollectCompassEngineHideTargets(weaponPanel, mfdRoot, canvas).Count
                    : CollectCompassEngineHideTargetsFromMfdRoot(mfdRoot, canvas, out _).Count;
            }

            MfdLog.Info(
                $"engineSection discovery failed jsonKey={aircraftJsonKey} root={mfdRoot.name} WeaponPanel={panelNorm} " +
                $"engineTelemetry={telemetryOnMfd}/{telemetryOnAircraft} rpm={rpmCount} " +
                $"frontProfile={(frontProfile != null ? frontProfile.name : "null")} engineChildren={engineChildren}");

            Canvas? hudCanvas = weaponPanel != null
                ? TacScreenAccess.GetOverlayCanvas(weaponPanel)
                : null;
            hudCanvas ??= TacScreenAccess.GetCanvasForRoot(mfdRoot);
            if (hudCanvas == null)
                return;

            RectTransform? hudRt = hudCanvas.GetComponent<RectTransform>();
            if (hudRt == null)
                return;

            int childDump = Mathf.Min(hudRt.childCount, 12);
            for (int i = 0; i < childDump; i++)
            {
                RectTransform? child = hudRt.GetChild(i).GetComponent<RectTransform>();
                if (child == null)
                    continue;

                PanelRectState childNorm = PanelRectNormalizer.CaptureOnCanvas(child, canvas);
                MfdLog.Info(
                    $"  compass hudChild[{i}] {child.name} norm={FormatAnchors(childNorm)} " +
                    $"telemetry={child.GetComponentInChildren<EngineTelemetry>(true) != null}");
            }
        }

        private static bool TryResolveDarkreachWeaponArmedPanel(
            GameObject mfdRoot,
            Canvas canvas,
            out ResolvedPanel resolved)
        {
            resolved = default;
            if (!HasBomberBayMarkers(mfdRoot))
                return false;

            RectTransform? anchor = FindDarkreachLeftBayAnchor(mfdRoot);
            if (anchor == null)
                return false;

            if (PanelContainsEngineGauges(anchor))
                return false;

            Canvas overlayCanvas = TacScreenAccess.GetOverlayCanvas(anchor) ?? canvas;
            RectTransform? hideRoot = ResolveDarkreachBaySectionRoot(anchor, mfdRoot, overlayCanvas);
            if (hideRoot == null)
                return false;

            if (PanelContainsEngineGauges(hideRoot))
                return false;

            if (!ContainsDarkreachBayUi(hideRoot))
                return false;

            PanelRectState canvasZone = PanelRectNormalizer.CaptureOnCanvas(hideRoot, overlayCanvas);

            if (IsDarkreachWeaponPanelZone(canvasZone))
            {
                MfdLog.Info(
                    $"darkreach rejected center weaponPanel zone={FormatAnchors(canvasZone)}");
                return false;
            }

            if (!PanelRectNormalizer.IsDarkreachBaySectionZone(canvasZone))
            {
                MaybeLogDarkreachDiscoveryFailure(
                    mfdRoot,
                    hideRoot,
                    overlayCanvas,
                    canvasZone,
                    zoneNodeCount: CollectDarkreachBayHits(mfdRoot).Count,
                    dedicatedWeaponMfd: false);
                return false;
            }

            List<RectTransform> hideTargets = CollectDarkreachVisualHideTargets(hideRoot);
            if (hideTargets.Count == 0)
                hideTargets = new List<RectTransform> { hideRoot };

            // Left MFD bezel is portrait: vanilla bay labels use rotated Text (typically Z=270).
            // Landscape stub rows inside a rotated content root тАФ same idea as other aircraft, one group rotation.
            PanelRectState zone = BuildDarkreachCanvasColumnZone(canvasZone);
            float contentRotZ = SampleDarkreachBayLabelRotation(hideRoot);
            float fontRef = Mathf.Max(Mathf.Min(hideRoot.rect.width, hideRoot.rect.height), 1f);
            Vector2 contentBand = BuildDarkreachContentBand(canvasZone, zone);

            MfdLog.Info(
                $"darkreach baySection hideRoot={hideRoot.name} anchor={anchor.name} hideChildren={hideTargets.Count} " +
                $"weaponPanelCanvas={FormatAnchors(canvasZone)} " +
                $"columnZone={FormatAnchors(zone)} panelRect={hideRoot.rect.width:F0}x{hideRoot.rect.height:F0} " +
                $"contentRotZ={contentRotZ:F0} fontRef={fontRef:F0} contentBand={contentBand.x:F2}-{contentBand.y:F2}");

            if (MfdLayoutConfig.DebugStub)
                DumpDarkreachBayTexts(mfdRoot, overlayCanvas);

            resolved = new ResolvedPanel(
                hideRoot,
                overlayCanvas,
                zone,
                hideTargets,
                isDarkreachSection: true,
                overlayParent: null,
                stubContentRotationZ: contentRotZ,
                stubFontRef: fontRef,
                stubContentBand: contentBand);
            return true;
        }

        private static Vector2 BuildDarkreachContentBand(PanelRectState weaponPanelOnCanvas, PanelRectState columnZone)
        {
            float zoneHeight = Mathf.Max(columnZone.AnchorMax.y - columnZone.AnchorMin.y, 0.01f);
            float minY = (weaponPanelOnCanvas.AnchorMin.y - columnZone.AnchorMin.y) / zoneHeight;
            float maxY = (weaponPanelOnCanvas.AnchorMax.y - columnZone.AnchorMin.y) / zoneHeight;
            return new Vector2(Mathf.Clamp01(minY), Mathf.Clamp01(maxY));
        }

        private static float SampleDarkreachBayLabelRotation(RectTransform hideRoot)
        {
            foreach (Text label in hideRoot.GetComponentsInChildren<Text>(true))
            {
                if (label == null || !IsBomberBayMarker(label.text))
                    continue;

                float rotZ = label.rectTransform.localEulerAngles.z;
                if (Mathf.Abs(Mathf.DeltaAngle(0f, rotZ)) > 0.5f)
                    return rotZ;
            }

            RectTransform? profile = FindProfileByName(hideRoot.gameObject, "rearProfile")
                ?? FindProfileByName(hideRoot.gameObject, "frontProfile");
            if (profile != null)
            {
                float rotZ = profile.localEulerAngles.z;
                if (Mathf.Abs(Mathf.DeltaAngle(0f, rotZ)) > 0.5f)
                    return rotZ;
            }

            return hideRoot.rect.height > hideRoot.rect.width * 1.15f ? 90f : 0f;
        }

        /// <summary>Full-height canvas column at weaponPanel X тАФ matches left MFD bezel viewport.</summary>
        private static PanelRectState BuildDarkreachCanvasColumnZone(PanelRectState weaponPanelOnCanvas)
        {
            float minX = Mathf.Clamp(weaponPanelOnCanvas.AnchorMin.x - 0.01f, 0.02f, 0.95f);
            float maxX = Mathf.Clamp(weaponPanelOnCanvas.AnchorMax.x + 0.01f, minX + 0.04f, 0.98f);
            return new PanelRectState(
                new Vector2(minX, 0.02f),
                new Vector2(maxX, 0.98f),
                Vector2.zero,
                Vector2.zero);
        }

        /// <summary>Tallest ancestor that still contains hideRoot on canvas (full MFD viewport).</summary>
        private static RectTransform SelectDarkreachOverlayHost(
            RectTransform hideRoot,
            RectTransform anchor,
            GameObject mfdRoot,
            Canvas canvas)
        {
            PanelRectState panelOnCanvas = PanelRectNormalizer.CaptureOnCanvas(hideRoot, canvas);
            RectTransform best = hideRoot;
            float bestHeight = panelOnCanvas.AnchorMax.y - panelOnCanvas.AnchorMin.y;

            RectTransform? halfRoot = ResolveDarkreachHalfRoot(anchor, mfdRoot, canvas);
            if (halfRoot != null)
            {
                PanelRectState halfZone = PanelRectNormalizer.CaptureOnCanvas(halfRoot, canvas);
                float halfH = halfZone.AnchorMax.y - halfZone.AnchorMin.y;
                if (halfH > bestHeight + 0.02f)
                {
                    best = halfRoot;
                    bestHeight = halfH;
                }
            }

            RectTransform? current = hideRoot;
            for (int depth = 0; depth < 12 && current != null; depth++)
            {
                Transform? parent = current.parent;
                if (parent == null)
                    break;

                if (parent.GetComponent<Canvas>() != null)
                    break;

                if (!parent.TryGetComponent(out RectTransform parentRt))
                    break;

                PanelRectState parentOnCanvas = PanelRectNormalizer.CaptureOnCanvas(parentRt, canvas);
                float height = parentOnCanvas.AnchorMax.y - parentOnCanvas.AnchorMin.y;

                bool containsPanel =
                    parentOnCanvas.AnchorMin.x <= panelOnCanvas.AnchorMin.x + 0.015f
                    && parentOnCanvas.AnchorMax.x >= panelOnCanvas.AnchorMax.x - 0.015f
                    && parentOnCanvas.AnchorMin.y <= panelOnCanvas.AnchorMin.y + 0.015f
                    && parentOnCanvas.AnchorMax.y >= panelOnCanvas.AnchorMax.y - 0.015f;

                if (containsPanel && height > bestHeight + 0.02f)
                {
                    best = parentRt;
                    bestHeight = height;
                }

                current = parentRt;
            }

            return best;
        }

        /// <summary>Map full-height canvas column (panel X, host Y) into overlayHost-local anchors.</summary>
        private static PanelRectState BuildDarkreachOverlayZone(
            RectTransform hideRoot,
            RectTransform overlayHost,
            Canvas canvas)
        {
            if (overlayHost == hideRoot)
                return ExpandDarkreachZoneToFullMfd(
                    new PanelRectState(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero));

            PanelRectState panelOnCanvas = PanelRectNormalizer.CaptureOnCanvas(hideRoot, canvas);
            PanelRectState hostOnCanvas = PanelRectNormalizer.CaptureOnCanvas(overlayHost, canvas);

            float minX = Mathf.Max(hostOnCanvas.AnchorMin.x, panelOnCanvas.AnchorMin.x - 0.005f);
            float maxX = Mathf.Min(hostOnCanvas.AnchorMax.x, panelOnCanvas.AnchorMax.x + 0.005f);
            float minY = hostOnCanvas.AnchorMin.y;
            float maxY = hostOnCanvas.AnchorMax.y;

            PanelRectState targetOnCanvas = new PanelRectState(
                new Vector2(minX, minY),
                new Vector2(maxX, maxY),
                Vector2.zero,
                Vector2.zero);

            PanelRectState local = PanelRectNormalizer.CanvasZoneToParentZone(targetOnCanvas, overlayHost, canvas);

            return new PanelRectState(
                new Vector2(Mathf.Clamp(local.AnchorMin.x, 0.02f, 0.95f), Mathf.Clamp(local.AnchorMin.y, 0.02f, 0.95f)),
                new Vector2(Mathf.Clamp(local.AnchorMax.x, 0.05f, 0.98f), Mathf.Clamp(local.AnchorMax.y, 0.05f, 0.98f)),
                Vector2.zero,
                Vector2.zero);
        }

        private static void MaybeLogDarkreachParentChain(
            RectTransform hideRoot,
            Canvas canvas,
            RectTransform overlayHost)
        {
            if (_darkreachParentDiagDone)
                return;

            _darkreachParentDiagDone = true;
            MfdLog.Info("Darkreach UI parent chain (canvas-normalized):");
            RectTransform? current = hideRoot;
            for (int depth = 0; depth < 12 && current != null; depth++)
            {
                PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(current, canvas);
                string mark = current == overlayHost ? " <- overlayHost" : string.Empty;
                MfdLog.Info(
                    $"  [{depth}] {current.name} norm={FormatAnchors(zone)} " +
                    $"rect={current.rect.width:F0}x{current.rect.height:F0}{mark}");
                if (current.parent == null)
                    break;

                if (current.parent.GetComponent<Canvas>() != null)
                    break;

                current = current.parent.GetComponent<RectTransform>();
            }
        }

        private static bool CanResolveDarkreachLeftColumn(GameObject mfdRoot, Canvas canvas)
        {
            if (!HasBomberBayMarkers(mfdRoot))
                return false;

            RectTransform? anchor = FindDarkreachLeftBayAnchor(mfdRoot);
            if (anchor == null)
                return false;

            Canvas overlayCanvas = TacScreenAccess.GetOverlayCanvas(anchor) ?? canvas;
            RectTransform? hideRoot = ResolveDarkreachBaySectionRoot(anchor, mfdRoot, overlayCanvas);
            if (hideRoot == null || !ContainsDarkreachBayUi(hideRoot))
                return false;

            PanelRectState canvasZone = PanelRectNormalizer.CaptureOnCanvas(hideRoot, overlayCanvas);
            if (IsDarkreachWeaponPanelZone(canvasZone))
                return false;

            return PanelRectNormalizer.IsDarkreachBaySectionZone(canvasZone);
        }

        private static bool ContainsDarkreachBayUi(RectTransform section)
        {
            bool hasProfile = false;
            bool hasBay = false;

            foreach (RectTransform rt in section.GetComponentsInChildren<RectTransform>(true))
            {
                string name = rt.name;
                if (string.Equals(name, "rearProfile", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(name, "frontProfile", System.StringComparison.OrdinalIgnoreCase))
                {
                    hasProfile = true;
                }
            }

            foreach (Text label in section.GetComponentsInChildren<Text>(true))
            {
                if (label != null && IsBomberBayMarker(label.text))
                    hasBay = true;
            }

            return hasProfile && hasBay;
        }

        private static RectTransform? ResolveDarkreachBaySectionRoot(
            RectTransform anchor,
            GameObject mfdRoot,
            Canvas canvas)
        {
            RectTransform? profile = FindBomberProfileInMfd(mfdRoot);
            if (profile?.parent != null)
            {
                RectTransform? fromProfile = SelectDarkreachBaySectionRoot(
                    profile.parent.GetComponent<RectTransform>(),
                    canvas);
                if (fromProfile != null)
                    return fromProfile;
            }

            foreach (Text label in mfdRoot.GetComponentsInChildren<Text>(true))
            {
                if (label == null || !IsBomberBayMarker(label.text))
                    continue;

                RectTransform? section = FindDarkreachBaySectionFromHit(label.rectTransform, canvas);
                if (section != null)
                    return section;
            }

            return SelectDarkreachBaySectionRoot(anchor, canvas);
        }

        private static RectTransform? FindDarkreachBaySectionFromHit(RectTransform hit, Canvas canvas)
        {
            RectTransform? current = hit;
            for (int depth = 0; depth < 12 && current != null; depth++)
            {
                if (ContainsDarkreachBayUi(current))
                {
                    PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(current, canvas);
                    if (PanelRectNormalizer.IsDarkreachBaySectionZone(zone)
                        && !IsDarkreachWeaponPanelZone(zone))
                    {
                        return current;
                    }
                }

                current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
            }

            return null;
        }

        private static RectTransform? SelectDarkreachBaySectionRoot(RectTransform? start, Canvas canvas)
        {
            RectTransform? best = null;
            float bestArea = float.MaxValue;
            RectTransform? current = start;

            for (int depth = 0; depth < 12 && current != null; depth++)
            {
                if (ContainsDarkreachBayUi(current))
                {
                    PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(current, canvas);
                    if (PanelRectNormalizer.IsDarkreachBaySectionZone(zone)
                        && !IsDarkreachWeaponPanelZone(zone))
                    {
                        float area = (zone.AnchorMax.x - zone.AnchorMin.x) * (zone.AnchorMax.y - zone.AnchorMin.y);
                        if (area < bestArea)
                        {
                            bestArea = area;
                            best = current;
                        }
                    }
                }

                current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
            }

            return best;
        }

        /// <summary>Hide bay visuals but keep section root active so MissileCamera overlay remains visible.</summary>
        private static List<RectTransform> CollectDarkreachVisualHideTargets(RectTransform sectionRoot)
        {
            var targets = new List<RectTransform>();
            for (int i = 0; i < sectionRoot.childCount; i++)
            {
                if (sectionRoot.GetChild(i) is RectTransform child)
                    targets.Add(child);
            }

            return targets;
        }

        private static RectTransform? FindDarkreachLeftBayAnchor(GameObject mfdRoot)
        {
            RectTransform? profile = FindProfileByName(mfdRoot, "rearProfile")
                ?? FindProfileByName(mfdRoot, "frontProfile");
            if (profile?.parent != null)
                return profile.parent.GetComponent<RectTransform>();

            List<RectTransform> bayHits = CollectDarkreachBayHits(mfdRoot);
            if (bayHits.Count > 0)
                return FindLowestCommonAncestor(bayHits);

            RectTransform? anchor = FindPanelByName(mfdRoot, "weaponStations");
            if (anchor != null)
                return anchor;

            anchor = FindPanelByName(mfdRoot, "WeaponPanel");
            if (anchor != null)
                return anchor;

            return FindPanelByName(mfdRoot, "weaponPanel");
        }

        private static RectTransform? SelectDarkreachLeftBayHideRoot(RectTransform? start, Canvas canvas)
        {
            RectTransform? best = null;
            float bestArea = float.MaxValue;
            RectTransform? current = start;

            for (int depth = 0; depth < 12 && current != null; depth++)
            {
                PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(current, canvas);
                if (IsDarkreachLeftBayZone(zone))
                {
                    float area = (zone.AnchorMax.x - zone.AnchorMin.x) * (zone.AnchorMax.y - zone.AnchorMin.y);
                    if (area < bestArea)
                    {
                        bestArea = area;
                        best = current;
                    }
                }

                current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
            }

            return best;
        }

        private static bool IsDarkreachLeftBayZone(PanelRectState rect) =>
            PanelRectNormalizer.IsDarkreachLeftBayZone(rect);

        private static bool DarkreachZoneOverlapsForbidden(PanelRectState zone) =>
            zone.AnchorMax.x > 0.25f || zone.AnchorMin.x >= 0.55f;

        private static List<RectTransform> CollectDarkreachBayHits(GameObject mfdRoot)
        {
            var hits = new List<RectTransform>();
            foreach (Text label in mfdRoot.GetComponentsInChildren<Text>(true))
            {
                if (label == null || !IsBomberBayMarker(label.text))
                    continue;

                RectTransform rt = label.rectTransform;
                if (!hits.Contains(rt))
                    hits.Add(rt);
            }

            return hits;
        }

        private static bool IsDarkreachWeaponPanelZone(PanelRectState rect) =>
            PanelRectNormalizer.IsDarkreachWeaponPanelZone(rect);

        private static bool IsDarkreachFullHalfZone(PanelRectState rect)
        {
            if (PanelRectNormalizer.IsDarkreachWeaponPanelZone(rect))
                return true;

            return PanelRectNormalizer.IsBomberLeftHalfZone(rect)
                && rect.AnchorMax.y - rect.AnchorMin.y >= 0.55f;
        }

        private static bool IsDarkreachFullMfdZone(PanelRectState rect)
        {
            if (PanelRectNormalizer.IsDarkreachWeaponPanelZone(rect))
                return true;

            return PanelRectNormalizer.IsDarkreachFullMfdZone(rect)
                && rect.AnchorMax.y - rect.AnchorMin.y >= 0.55f;
        }

        private static bool IsDarkreachResolvedZone(PanelRectState rect, bool dedicatedWeaponMfd) =>
            dedicatedWeaponMfd ? IsDarkreachFullMfdZone(rect) : IsDarkreachFullHalfZone(rect);

        private static PanelRectState ExpandDarkreachZoneToFullMfd(PanelRectState zone)
        {
            return new PanelRectState(
                new Vector2(0.02f, 0.02f),
                new Vector2(0.98f, 0.98f),
                Vector2.zero,
                Vector2.zero);
        }

        private static RectTransform? ResolveDarkreachDedicatedHideRoot(
            RectTransform anchor,
            Canvas canvas,
            GameObject mfdRoot)
        {
            RectTransform? selected = SelectDarkreachDedicatedRoot(anchor, canvas);
            if (selected != null)
                return selected;

            RectTransform? canvasRt = canvas.GetComponent<RectTransform>();
            if (canvasRt != null)
                return canvasRt;

            Transform? current = anchor.parent;
            for (int depth = 0; depth < 8 && current != null; depth++)
            {
                if (current == mfdRoot.transform)
                    break;

                RectTransform? rt = current.GetComponent<RectTransform>();
                if (rt != null && rt != anchor)
                    return rt;

                current = current.parent;
            }

            return anchor;
        }

        private static RectTransform? ResolveDarkreachDedicatedRoot(
            RectTransform anchor,
            GameObject mfdRoot,
            Canvas canvas)
        {
            RectTransform? best = SelectDarkreachDedicatedRoot(anchor, canvas);
            if (best != null)
                return best;

            Canvas? overlayCanvas = canvas;
            RectTransform? canvasRt = overlayCanvas.GetComponent<RectTransform>();
            if (canvasRt != null && IsDarkreachFullMfdZone(PanelRectNormalizer.CaptureOnCanvas(canvasRt, overlayCanvas)))
                return canvasRt;

            return ResolveDarkreachHalfRoot(anchor, mfdRoot, canvas);
        }

        private static RectTransform? SelectDarkreachDedicatedRoot(RectTransform? start, Canvas canvas)
        {
            RectTransform? best = null;
            float bestArea = float.MaxValue;
            RectTransform? current = start;

            for (int depth = 0; depth < 12 && current != null; depth++)
            {
                PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(current, canvas);
                if (IsDarkreachFullMfdZone(zone))
                {
                    float area = (zone.AnchorMax.x - zone.AnchorMin.x) * (zone.AnchorMax.y - zone.AnchorMin.y);
                    if (area < bestArea)
                    {
                        bestArea = area;
                        best = current;
                    }
                }

                current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
            }

            return best;
        }

        private static RectTransform? ResolveDarkreachHalfRoot(
            RectTransform anchor,
            GameObject mfdRoot,
            Canvas canvas)
        {
            var anchors = new List<RectTransform> { anchor };

            RectTransform? profile = FindBomberProfileInMfd(mfdRoot);
            if (profile?.parent != null)
            {
                RectTransform section = profile.parent.GetComponent<RectTransform>();
                if (!anchors.Contains(section))
                    anchors.Add(section);
            }

            foreach (Text label in mfdRoot.GetComponentsInChildren<Text>(true))
            {
                if (label == null || !IsBomberBayMarker(label.text))
                    continue;

                RectTransform? section = FindDarkreachLeftHalfSection(label.rectTransform, canvas);
                if (section != null && !anchors.Contains(section))
                    anchors.Add(section);
            }

            RectTransform? lca = FindLowestCommonAncestor(anchors);
            RectTransform? best = SelectDarkreachHalfRoot(lca, canvas);
            if (best != null)
                return best;

            return SelectDarkreachHalfRoot(anchor, canvas);
        }

        private static RectTransform? SelectDarkreachHalfRoot(RectTransform? start, Canvas canvas)
        {
            RectTransform? best = null;
            float bestArea = float.MaxValue;
            RectTransform? current = start;

            for (int depth = 0; depth < 12 && current != null; depth++)
            {
                PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(current, canvas);
                if (IsDarkreachFullHalfZone(zone))
                {
                    float area = (zone.AnchorMax.x - zone.AnchorMin.x) * (zone.AnchorMax.y - zone.AnchorMin.y);
                    if (area < bestArea)
                    {
                        bestArea = area;
                        best = current;
                    }
                }

                current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
            }

            return best;
        }

        private static RectTransform? FindBomberProfileInMfd(GameObject mfdRoot)
        {
            RectTransform? rear = FindProfileByName(mfdRoot, "rearProfile");
            if (rear != null)
                return rear;

            return FindProfileByName(mfdRoot, "frontProfile");
        }

        private static RectTransform? FindProfileByName(GameObject mfdRoot, string profileName)
        {
            foreach (RectTransform rt in mfdRoot.GetComponentsInChildren<RectTransform>(true))
            {
                if (string.Equals(rt.name, profileName, System.StringComparison.OrdinalIgnoreCase))
                    return rt;
            }

            return null;
        }

        private static RectTransform? FindDarkreachLeftHalfSection(RectTransform hit, Canvas canvas)
        {
            RectTransform? current = hit;
            for (int depth = 0; depth < 10 && current != null; depth++)
            {
                PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(current, canvas);
                if (zone.AnchorMax.x <= 0.54f && zone.AnchorMax.x - zone.AnchorMin.x >= 0.10f)
                    return current;

                current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
            }

            return null;
        }

        private static PanelRectState ExpandDarkreachZoneToLeftHalf(
            PanelRectState zone,
            Canvas canvas,
            List<RectTransform> zoneNodes)
        {
            float minX = zone.AnchorMin.x;
            float maxX = zone.AnchorMax.x;
            float minY = zone.AnchorMin.y;
            float maxY = zone.AnchorMax.y;

            foreach (RectTransform node in zoneNodes)
            {
                PanelRectState nz = PanelRectNormalizer.CaptureOnCanvas(node, canvas);
                minX = Mathf.Min(minX, nz.AnchorMin.x);
                maxX = Mathf.Max(maxX, nz.AnchorMax.x);
                minY = Mathf.Min(minY, nz.AnchorMin.y);
                maxY = Mathf.Max(maxY, nz.AnchorMax.y);
            }

            maxX = Mathf.Min(maxX, 0.50f - 0.005f);
            minX = Mathf.Max(minX, 0.02f);

            return new PanelRectState(
                new Vector2(minX, minY),
                new Vector2(maxX, maxY),
                Vector2.zero,
                Vector2.zero);
        }

        private static PanelRectState ExpandDarkreachZoneVertical(
            RectTransform anchor,
            GameObject mfdRoot,
            Canvas canvas,
            PanelRectState zone,
            List<RectTransform> hideTargets,
            bool dedicatedWeaponMfd)
        {
            if (IsDarkreachResolvedZone(zone, dedicatedWeaponMfd))
                return zone;

            PanelRectState anchorZone = PanelRectNormalizer.CaptureOnCanvas(anchor, canvas);
            float minX = Mathf.Min(zone.AnchorMin.x, anchorZone.AnchorMin.x);
            float maxX = dedicatedWeaponMfd
                ? Mathf.Max(zone.AnchorMax.x, anchorZone.AnchorMax.x)
                : Mathf.Min(Mathf.Max(zone.AnchorMax.x, anchorZone.AnchorMax.x), 0.50f - 0.005f);
            float minY = zone.AnchorMin.y;
            float maxY = zone.AnchorMax.y;

            RectTransform? profile = FindBomberProfileInMfd(mfdRoot);
            if (profile != null)
            {
                PanelRectState profileZone = PanelRectNormalizer.CaptureOnCanvas(profile, canvas);
                minX = Mathf.Min(minX, profileZone.AnchorMin.x);
                maxX = dedicatedWeaponMfd
                    ? Mathf.Max(maxX, profileZone.AnchorMax.x)
                    : Mathf.Min(Mathf.Max(maxX, profileZone.AnchorMax.x), 0.50f - 0.005f);
                minY = Mathf.Min(minY, profileZone.AnchorMin.y);
                maxY = Mathf.Max(maxY, profileZone.AnchorMax.y);

                RectTransform? section = profile.parent != null
                    ? profile.parent.GetComponent<RectTransform>()
                    : null;
                if (section != null)
                {
                    PanelRectState sectionZone = PanelRectNormalizer.CaptureOnCanvas(section, canvas);
                    minX = Mathf.Min(minX, sectionZone.AnchorMin.x);
                    maxX = dedicatedWeaponMfd
                        ? Mathf.Max(maxX, sectionZone.AnchorMax.x)
                        : Mathf.Min(Mathf.Max(maxX, sectionZone.AnchorMax.x), 0.50f - 0.005f);
                    minY = Mathf.Min(minY, sectionZone.AnchorMin.y);
                    maxY = Mathf.Max(maxY, sectionZone.AnchorMax.y);
                    AddDarkreachHideTarget(hideTargets, section);
                }
            }

            foreach (RectTransform rt in mfdRoot.GetComponentsInChildren<RectTransform>(true))
            {
                if (!IsDarkreachWireframeCandidate(rt))
                    continue;

                PanelRectState candidateZone = PanelRectNormalizer.CaptureOnCanvas(rt, canvas);
                if (candidateZone.AnchorMin.x > maxX + 0.03f || candidateZone.AnchorMax.x < minX - 0.03f)
                    continue;

                if (candidateZone.AnchorMax.y <= maxY + 0.01f)
                    continue;

                minY = Mathf.Min(minY, candidateZone.AnchorMin.y);
                maxY = Mathf.Max(maxY, candidateZone.AnchorMax.y);
                AddDarkreachHideTarget(hideTargets, rt);
            }

            return new PanelRectState(
                new Vector2(minX, minY),
                new Vector2(maxX, maxY),
                Vector2.zero,
                Vector2.zero);
        }

        private static bool IsDarkreachWireframeCandidate(RectTransform rt)
        {
            string name = rt.name;
            if (string.Equals(name, "rearProfile", System.StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(name, "frontProfile", System.StringComparison.OrdinalIgnoreCase))
                return true;

            if (name.StartsWith("Box_", System.StringComparison.OrdinalIgnoreCase))
                return true;

            return name.IndexOf("Profile", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void AddDarkreachHideTarget(List<RectTransform> hideTargets, RectTransform node)
        {
            if (!hideTargets.Contains(node))
                hideTargets.Add(node);
        }

        private static List<RectTransform> CollectDarkreachZoneNodes(
            RectTransform anchor,
            Canvas canvas,
            GameObject mfdRoot,
            bool dedicatedWeaponMfd)
        {
            var nodes = new List<RectTransform> { anchor };

            for (int i = 0; i < anchor.childCount; i++)
            {
                RectTransform? child = anchor.GetChild(i).GetComponent<RectTransform>();
                if (child != null && IsDarkreachZoneVisual(child, canvas, dedicatedWeaponMfd) && !nodes.Contains(child))
                    nodes.Add(child);
            }

            RectTransform? parent = anchor.parent != null
                ? anchor.parent.GetComponent<RectTransform>()
                : null;
            if (parent != null)
            {
                for (int i = 0; i < parent.childCount; i++)
                {
                    RectTransform? sibling = parent.GetChild(i).GetComponent<RectTransform>();
                    if (sibling == null || sibling == anchor || nodes.Contains(sibling))
                        continue;

                    if (!IsDarkreachZoneVisual(sibling, canvas, dedicatedWeaponMfd))
                        continue;

                    nodes.Add(sibling);
                }
            }

            RectTransform? profile = FindBomberProfileInMfd(mfdRoot);
            if (profile?.parent != null)
            {
                RectTransform section = profile.parent.GetComponent<RectTransform>();
                if (!nodes.Contains(section))
                    nodes.Add(section);
            }

            foreach (Text label in mfdRoot.GetComponentsInChildren<Text>(true))
            {
                if (label == null || !IsBomberBayMarker(label.text))
                    continue;

                RectTransform? section = dedicatedWeaponMfd
                    ? FindDarkreachDedicatedSection(label.rectTransform, canvas)
                    : FindDarkreachLeftHalfSection(label.rectTransform, canvas);
                if (section != null && !nodes.Contains(section))
                    nodes.Add(section);
            }

            return nodes;
        }

        private static void ExpandDarkreachZoneNodesIfNeeded(
            RectTransform anchor,
            Canvas canvas,
            List<RectTransform> nodes,
            bool dedicatedWeaponMfd)
        {
            PanelRectState union = PanelRectNormalizer.UnionOnCanvas(canvas, nodes);
            if (union.AnchorMax.y - union.AnchorMin.y >= 0.55f)
                return;

            RectTransform? parent = anchor.parent != null
                ? anchor.parent.GetComponent<RectTransform>()
                : null;
            RectTransform? grandparent = parent?.parent != null
                ? parent.parent.GetComponent<RectTransform>()
                : null;
            if (grandparent == null)
                return;

            for (int i = 0; i < grandparent.childCount; i++)
            {
                RectTransform? cousin = grandparent.GetChild(i).GetComponent<RectTransform>();
                if (cousin == null || nodes.Contains(cousin))
                    continue;

                if (!IsDarkreachZoneVisual(cousin, canvas, dedicatedWeaponMfd))
                    continue;

                nodes.Add(cousin);
            }
        }

        private static bool IsDarkreachZoneVisual(RectTransform node, Canvas canvas, bool dedicatedWeaponMfd)
        {
            if (IsTimeObject(node))
                return false;

            if (PanelContainsEngineGauges(node))
                return false;

            if (node.GetComponentInChildren<StatusGauges>(true) != null)
                return false;

            PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(node, canvas);
            float w = zone.AnchorMax.x - zone.AnchorMin.x;
            float h = zone.AnchorMax.y - zone.AnchorMin.y;
            if (w < 0.06f && h < 0.06f)
                return false;

            if (dedicatedWeaponMfd)
                return w >= 0.08f || h >= 0.08f;

            if (zone.AnchorMin.x >= 0.52f)
                return false;

            return zone.AnchorMax.x <= 0.54f;
        }

        private static RectTransform? FindDarkreachDedicatedSection(RectTransform hit, Canvas canvas)
        {
            RectTransform? current = hit;
            for (int depth = 0; depth < 10 && current != null; depth++)
            {
                PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(current, canvas);
                if (zone.AnchorMax.x - zone.AnchorMin.x >= 0.50f && zone.AnchorMax.y - zone.AnchorMin.y >= 0.40f)
                    return current;

                current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
            }

            return null;
        }

        private static List<RectTransform> CollectDarkreachHideTargets(
            RectTransform anchor,
            Canvas canvas,
            List<RectTransform> zoneNodes,
            bool dedicatedWeaponMfd)
        {
            RectTransform? parent = anchor.parent != null
                ? anchor.parent.GetComponent<RectTransform>()
                : null;
            if (parent != null
                && AllZoneNodesAreChildrenOf(parent, zoneNodes)
                && IsDarkreachZoneVisual(parent, canvas, dedicatedWeaponMfd))
            {
                PanelRectState parentZone = PanelRectNormalizer.CaptureOnCanvas(parent, canvas);
                bool validParent = dedicatedWeaponMfd
                    ? PanelRectNormalizer.IsDarkreachFullMfdZone(parentZone)
                        || PanelRectNormalizer.IsDarkreachWeaponPanelZone(parentZone)
                    : PanelRectNormalizer.IsBomberLeftHalfZone(parentZone)
                        || PanelRectNormalizer.IsDarkreachWeaponPanelZone(parentZone);
                if (validParent)
                    return new List<RectTransform> { parent };
            }

            return new List<RectTransform>(zoneNodes);
        }

        private static void MaybeLogDarkreachDiscoveryFailure(
            GameObject mfdRoot,
            RectTransform? anchor,
            Canvas canvas,
            List<RectTransform> primaryHits)
        {
            if (_darkreachDiagDone)
                return;

            _darkreachDiagDone = true;
            int markerCount = CountBomberBayMarkers(mfdRoot);
            string anchorName = anchor != null ? anchor.name : "null";
            MfdLog.Info(
                $"darkreach discovery failed root={mfdRoot.name} canvas={canvas.name} " +
                $"anchor={anchorName} bomberMarkers={markerCount} primaryHits={primaryHits.Count}");
            DumpDarkreachBayTexts(mfdRoot, canvas);
        }

        private static void MaybeLogDarkreachDiscoveryFailure(
            GameObject mfdRoot,
            RectTransform? anchor,
            Canvas canvas,
            PanelRectState zone,
            int zoneNodeCount,
            bool dedicatedWeaponMfd)
        {
            if (_darkreachDiagDone)
                return;

            _darkreachDiagDone = true;
            int markerCount = CountBomberBayMarkers(mfdRoot);
            string anchorName = anchor != null ? anchor.name : "null";
            string strip = anchor != null && HasIfritStripLayout(anchor) ? "true" : "false";
            string validators =
                $"leftBay={IsDarkreachLeftBayZone(zone)} weaponPanel={IsDarkreachWeaponPanelZone(zone)}";
            bool tacOverlap = zone.AnchorMax.x > 0.25f;
            bool engineOverlap = zone.AnchorMin.x >= 0.55f;
            MfdLog.Info(
                $"darkreach discovery failed root={mfdRoot.name} dedicated={dedicatedWeaponMfd} canvas={canvas.name} " +
                $"anchor={anchorName} bomberMarkers={markerCount} zoneNodes={zoneNodeCount} " +
                $"union norm={FormatAnchors(zone)} strip={strip} tacOverlap={tacOverlap} engineOverlap={engineOverlap} {validators}");
            DumpDarkreachBayTexts(mfdRoot, canvas);
        }

        private static void DumpDarkreachBayTexts(GameObject mfdRoot, Canvas canvas)
        {
            Canvas? overlayCanvas = canvas;
            RectTransform? anchor = FindDarkreachLeftBayAnchor(mfdRoot);
            if (anchor != null)
                overlayCanvas = TacScreenAccess.GetOverlayCanvas(anchor) ?? canvas;

            RectTransform? profile = FindBomberProfileInMfd(mfdRoot);
            if (profile != null)
            {
                PanelRectState profileZone = PanelRectNormalizer.CaptureOnCanvas(profile, overlayCanvas);
                string parentName = profile.parent != null ? profile.parent.name : "null";
                MfdLog.Info(
                    $"  rearProfile parent={parentName} norm={FormatAnchors(profileZone)} localRotZ={profile.localEulerAngles.z:F0}");
            }

            MfdLog.Info("Darkreach bay text inventory:");
            int count = 0;
            foreach (Text label in mfdRoot.GetComponentsInChildren<Text>(true))
            {
                if (label == null || !IsBomberBayMarker(label.text))
                    continue;

                PanelRectState leafZone = PanelRectNormalizer.CaptureOnCanvas(label.rectTransform, overlayCanvas);
                RectTransform? baySection = FindDarkreachBaySectionFromHit(label.rectTransform, overlayCanvas);
                string sectionName = baySection != null ? baySection.name : "null";
                MfdLog.Info(
                    $"  [{count}] {label.name} text=\"{Normalize(label.text)}\" " +
                    $"norm={FormatAnchors(leafZone)} baySection={sectionName} localRotZ={label.rectTransform.localEulerAngles.z:F0}");
                count++;
            }
        }

        private static RectTransform? FindAlkyonWeaponsAnchor(
            GameObject mfdRoot,
            List<RectTransform> primaryHits)
        {
            RectTransform? anchor = FindPanelByName(mfdRoot, "WeaponPanel");
            if (anchor != null)
                return anchor;

            anchor = FindPanelByName(mfdRoot, "weaponStations");
            if (anchor != null)
                return anchor;

            if (primaryHits.Count == 0)
                return null;

            return FindLowestCommonAncestor(primaryHits);
        }

        private static bool IsAlkyonFullColumnZone(PanelRectState rect)
        {
            return PanelRectNormalizer.IsBomberRightColumnZone(rect)
                && rect.AnchorMax.y - rect.AnchorMin.y >= 0.55f;
        }

        private static RectTransform? ResolveAlkyonColumnRoot(
            RectTransform anchor,
            GameObject mfdRoot,
            Canvas canvas)
        {
            var anchors = new List<RectTransform> { anchor };

            RectTransform? front = FindFrontProfileInMfd(mfdRoot);
            if (front?.parent != null)
            {
                RectTransform section = front.parent.GetComponent<RectTransform>();
                if (!anchors.Contains(section))
                    anchors.Add(section);
            }

            foreach (Text label in mfdRoot.GetComponentsInChildren<Text>(true))
            {
                if (label == null || !IsBomberBayMarker(label.text))
                    continue;

                RectTransform? section = FindAlkyonRightColumnSection(label.rectTransform, canvas);
                if (section != null && !anchors.Contains(section))
                    anchors.Add(section);
            }

            RectTransform? lca = FindLowestCommonAncestor(anchors);
            RectTransform? best = SelectAlkyonColumnRoot(lca, canvas);
            if (best != null)
                return best;

            return SelectAlkyonColumnRoot(anchor, canvas);
        }

        private static RectTransform? SelectAlkyonColumnRoot(RectTransform? start, Canvas canvas)
        {
            RectTransform? best = null;
            float bestArea = float.MaxValue;
            RectTransform? current = start;

            for (int depth = 0; depth < 12 && current != null; depth++)
            {
                PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(current, canvas);
                if (IsAlkyonFullColumnZone(zone))
                {
                    float area = (zone.AnchorMax.x - zone.AnchorMin.x) * (zone.AnchorMax.y - zone.AnchorMin.y);
                    if (area < bestArea)
                    {
                        bestArea = area;
                        best = current;
                    }
                }

                current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
            }

            return best;
        }

        private static RectTransform? FindFrontProfileInMfd(GameObject mfdRoot)
        {
            foreach (RectTransform rt in mfdRoot.GetComponentsInChildren<RectTransform>(true))
            {
                if (string.Equals(rt.name, "frontProfile", System.StringComparison.OrdinalIgnoreCase))
                    return rt;
            }

            return null;
        }

        private static RectTransform? FindAlkyonRightColumnSection(RectTransform hit, Canvas canvas)
        {
            RectTransform? current = hit;
            for (int depth = 0; depth < 10 && current != null; depth++)
            {
                PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(current, canvas);
                if (zone.AnchorMin.x >= 0.48f && zone.AnchorMax.x - zone.AnchorMin.x >= 0.10f)
                    return current;

                current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
            }

            return null;
        }

        private static PanelRectState ExpandAlkyonZoneVertical(
            RectTransform anchor,
            GameObject mfdRoot,
            Canvas canvas,
            PanelRectState zone,
            List<RectTransform> hideTargets)
        {
            if (IsAlkyonFullColumnZone(zone))
                return zone;

            PanelRectState anchorZone = PanelRectNormalizer.CaptureOnCanvas(anchor, canvas);
            float minX = Mathf.Min(zone.AnchorMin.x, anchorZone.AnchorMin.x);
            float maxX = Mathf.Max(zone.AnchorMax.x, anchorZone.AnchorMax.x);
            float minY = zone.AnchorMin.y;
            float maxY = zone.AnchorMax.y;

            RectTransform? front = FindFrontProfileInMfd(mfdRoot);
            if (front != null)
            {
                PanelRectState frontZone = PanelRectNormalizer.CaptureOnCanvas(front, canvas);
                minX = Mathf.Min(minX, frontZone.AnchorMin.x);
                maxX = Mathf.Max(maxX, frontZone.AnchorMax.x);
                minY = Mathf.Min(minY, frontZone.AnchorMin.y);
                maxY = Mathf.Max(maxY, frontZone.AnchorMax.y);

                RectTransform? section = front.parent != null
                    ? front.parent.GetComponent<RectTransform>()
                    : null;
                if (section != null)
                {
                    PanelRectState sectionZone = PanelRectNormalizer.CaptureOnCanvas(section, canvas);
                    minX = Mathf.Min(minX, sectionZone.AnchorMin.x);
                    maxX = Mathf.Max(maxX, sectionZone.AnchorMax.x);
                    minY = Mathf.Min(minY, sectionZone.AnchorMin.y);
                    maxY = Mathf.Max(maxY, sectionZone.AnchorMax.y);
                    AddAlkyonHideTarget(hideTargets, section);
                }
            }

            foreach (RectTransform rt in mfdRoot.GetComponentsInChildren<RectTransform>(true))
            {
                if (!IsAlkyonWireframeCandidate(rt))
                    continue;

                PanelRectState candidateZone = PanelRectNormalizer.CaptureOnCanvas(rt, canvas);
                if (candidateZone.AnchorMax.x < minX - 0.03f || candidateZone.AnchorMin.x > maxX + 0.03f)
                    continue;

                if (candidateZone.AnchorMax.y <= maxY + 0.01f)
                    continue;

                minY = Mathf.Min(minY, candidateZone.AnchorMin.y);
                maxY = Mathf.Max(maxY, candidateZone.AnchorMax.y);
                AddAlkyonHideTarget(hideTargets, rt);
            }

            return new PanelRectState(
                new Vector2(minX, minY),
                new Vector2(maxX, maxY),
                Vector2.zero,
                Vector2.zero);
        }

        private static bool IsAlkyonWireframeCandidate(RectTransform rt)
        {
            string name = rt.name;
            if (string.Equals(name, "frontProfile", System.StringComparison.OrdinalIgnoreCase))
                return true;

            if (name.StartsWith("Box_", System.StringComparison.OrdinalIgnoreCase))
                return true;

            return name.IndexOf("Profile", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void AddAlkyonHideTarget(List<RectTransform> hideTargets, RectTransform node)
        {
            if (!hideTargets.Contains(node))
                hideTargets.Add(node);
        }

        private static List<RectTransform> CollectAlkyonZoneNodes(
            RectTransform anchor,
            Canvas canvas,
            GameObject mfdRoot)
        {
            var nodes = new List<RectTransform> { anchor };

            for (int i = 0; i < anchor.childCount; i++)
            {
                RectTransform? child = anchor.GetChild(i).GetComponent<RectTransform>();
                if (child != null && IsAlkyonColumnVisual(child, canvas) && !nodes.Contains(child))
                    nodes.Add(child);
            }

            RectTransform? parent = anchor.parent != null
                ? anchor.parent.GetComponent<RectTransform>()
                : null;
            if (parent != null)
            {
                for (int i = 0; i < parent.childCount; i++)
                {
                    RectTransform? sibling = parent.GetChild(i).GetComponent<RectTransform>();
                    if (sibling == null || sibling == anchor || nodes.Contains(sibling))
                        continue;

                    if (!IsAlkyonColumnVisual(sibling, canvas))
                        continue;

                    nodes.Add(sibling);
                }
            }

            RectTransform? front = FindFrontProfileInMfd(mfdRoot);
            if (front?.parent != null)
            {
                RectTransform section = front.parent.GetComponent<RectTransform>();
                if (!nodes.Contains(section))
                    nodes.Add(section);
            }

            return nodes;
        }

        private static void ExpandAlkyonZoneNodesIfNeeded(
            RectTransform anchor,
            Canvas canvas,
            List<RectTransform> nodes)
        {
            PanelRectState union = PanelRectNormalizer.UnionOnCanvas(canvas, nodes);
            if (union.AnchorMax.y - union.AnchorMin.y >= 0.55f)
                return;

            RectTransform? parent = anchor.parent != null
                ? anchor.parent.GetComponent<RectTransform>()
                : null;
            RectTransform? grandparent = parent?.parent != null
                ? parent.parent.GetComponent<RectTransform>()
                : null;
            if (grandparent == null)
                return;

            for (int i = 0; i < grandparent.childCount; i++)
            {
                RectTransform? cousin = grandparent.GetChild(i).GetComponent<RectTransform>();
                if (cousin == null || nodes.Contains(cousin))
                    continue;

                if (!IsAlkyonColumnVisual(cousin, canvas))
                    continue;

                nodes.Add(cousin);
            }
        }

        private static bool IsAlkyonColumnVisual(RectTransform node, Canvas canvas)
        {
            if (IsTimeObject(node))
                return false;

            if (PanelContainsEngineGauges(node))
                return false;

            if (node.GetComponentInChildren<StatusGauges>(true) != null)
                return false;

            PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(node, canvas);
            if (zone.AnchorMax.x < 0.48f)
                return false;

            float w = zone.AnchorMax.x - zone.AnchorMin.x;
            float h = zone.AnchorMax.y - zone.AnchorMin.y;
            if (w < 0.06f && h < 0.06f)
                return false;

            return zone.AnchorMin.x >= 0.48f;
        }

        private static List<RectTransform> CollectAlkyonHideTargets(
            RectTransform anchor,
            Canvas canvas,
            List<RectTransform> zoneNodes)
        {
            RectTransform? parent = anchor.parent != null
                ? anchor.parent.GetComponent<RectTransform>()
                : null;
            if (parent != null
                && AllZoneNodesAreChildrenOf(parent, zoneNodes)
                && IsAlkyonColumnVisual(parent, canvas)
                && PanelRectNormalizer.IsBomberRightColumnZone(
                    PanelRectNormalizer.CaptureOnCanvas(parent, canvas)))
            {
                return new List<RectTransform> { parent };
            }

            return new List<RectTransform>(zoneNodes);
        }

        private static bool AllZoneNodesAreChildrenOf(RectTransform parent, List<RectTransform> zoneNodes)
        {
            foreach (RectTransform node in zoneNodes)
            {
                if (node == null || node == parent)
                    return false;

                if (!node.IsChildOf(parent))
                    return false;
            }

            return zoneNodes.Count > 0;
        }

        private static bool HasIfritStripLayout(RectTransform weaponPanel)
        {
            int stripCount = 0;
            for (int i = 0; i < weaponPanel.childCount; i++)
            {
                RectTransform? child = weaponPanel.GetChild(i).GetComponent<RectTransform>();
                if (child != null && IsIfritStripChild(child))
                    stripCount++;
            }

            return stripCount >= 2;
        }

        private static void MaybeLogAlkyonDiscoveryFailure(
            GameObject mfdRoot,
            RectTransform? anchor,
            Canvas canvas,
            PanelRectState zone,
            int zoneNodeCount)
        {
            if (_alkyonDiagDone)
                return;

            _alkyonDiagDone = true;
            int markerCount = CountBomberBayMarkers(mfdRoot);
            string anchorName = anchor != null ? anchor.name : "null";
            string strip = anchor != null && HasIfritStripLayout(anchor) ? "true" : "false";
            MfdLog.Info(
                $"alkyon discovery failed anchor={anchorName} bomberMarkers={markerCount} zoneNodes={zoneNodeCount} " +
                $"union norm={FormatAnchors(zone)} strip={strip}");
        }

        private static int CountBomberBayMarkers(GameObject mfdRoot)
        {
            int count = 0;
            Text[] texts = mfdRoot.GetComponentsInChildren<Text>(true);
            foreach (Text label in texts)
            {
                if (label != null && IsBomberBayMarker(label.text))
                    count++;
            }

            return count;
        }

        private static bool IsBomberBayMarker(string raw)
        {
            string norm = Normalize(raw);
            if (string.IsNullOrEmpty(norm))
                return false;

            if (norm.Contains("WEAPON ARMED"))
                return false;

            if (norm.Contains("FORWARD BAY")
                || norm.Contains("REAR BAY")
                || norm.Contains("HEATER BAYS")
                || norm.Contains("WING PYLONS"))
            {
                return true;
            }

            if (norm.Contains("WEAPON BAY"))
                return true;

            return norm.Contains("BAY") && norm.Contains("ARMED");
        }

        private static bool TryResolveMedusaWeaponsSection(
            GameObject mfdRoot,
            Canvas canvas,
            out ResolvedPanel resolved)
        {
            resolved = default;
            RectTransform? weaponPanel = FindPanelByName(mfdRoot, "WeaponPanel");
            if (weaponPanel == null)
                return false;

            List<RectTransform> stripNodes = CollectMedusaStripChildren(weaponPanel);
            if (!TryResolveStripWeaponsSection(
                    mfdRoot,
                    canvas,
                    weaponPanel,
                    stripNodes,
                    expandFullColumnWidth: true,
                    isMedusaSection: true,
                    isTarantulaSection: false,
                    out resolved))
            {
                return false;
            }

            MfdLog.Info(
                $"medusa strip stripBottom={resolved.StripBottomY:F2} statusTop={resolved.StatusTopY:F2} " +
                $"statusFrame={resolved.StatusFrameName} hide={resolved.HideTargets.Count} " +
                $"zone={FormatAnchors(resolved.Zone)}");
            return true;
        }

        private static List<RectTransform> CollectMedusaStripChildren(RectTransform weaponPanel)
        {
            var nodes = CollectIfritStripChildren(weaponPanel);
            for (int i = 0; i < weaponPanel.childCount; i++)
            {
                RectTransform? child = weaponPanel.GetChild(i).GetComponent<RectTransform>();
                if (child == null || nodes.Contains(child) || PanelContainsEngineGauges(child))
                    continue;

                if (child.GetComponentInChildren<StatusGauges>(true) != null)
                    continue;

                if (!ContainsMedusaWeaponsMarkerText(child))
                    continue;

                nodes.Add(child);
            }

            return nodes;
        }

        private static bool ContainsMedusaWeaponsMarkerText(RectTransform root)
        {
            foreach (Text text in root.GetComponentsInChildren<Text>(true))
            {
                if (text != null && IsMedusaWeaponsMarker(text.text))
                    return true;
            }

            return false;
        }

        private static bool IsMedusaWeaponsMarker(string raw)
        {
            string norm = Normalize(raw);
            if (string.IsNullOrEmpty(norm))
                return false;

            if (norm.Contains("RADOME") && norm.Contains("ARMED"))
                return true;

            return norm.Contains("LASER") && norm.Contains("ARMED");
        }

        private static void MaybeLogMedusaDiscoveryFailure(GameObject mfdRoot, Canvas canvas)
        {
            if (_failureDiagDone)
                return;

            _failureDiagDone = true;
            RectTransform? weaponPanel = FindPanelByName(mfdRoot, "WeaponPanel");
            string weaponPanelNorm = weaponPanel != null
                ? FormatAnchors(PanelRectNormalizer.CaptureOnCanvas(weaponPanel, canvas))
                : "n/a";

            MfdLog.Info($"medusa discovery failed WeaponPanel={weaponPanelNorm}");
            if (weaponPanel == null)
                return;

            for (int i = 0; i < weaponPanel.childCount; i++)
            {
                RectTransform? child = weaponPanel.GetChild(i).GetComponent<RectTransform>();
                if (child == null)
                    continue;

                PanelRectState childNorm = PanelRectNormalizer.CaptureOnCanvas(child, canvas);
                MfdLog.Info(
                    $"  WeaponPanel child[{i}] {child.name} norm={FormatAnchors(childNorm)} " +
                    $"gauges={child.GetComponentInChildren<StatusGauges>(true) != null}");
            }
        }

        private static bool TryResolveIfritFlatStrip(
            GameObject mfdRoot,
            Canvas canvas,
            out ResolvedPanel resolved)
        {
            resolved = default;
            RectTransform? weaponPanel = FindPanelByName(mfdRoot, "WeaponPanel");
            if (weaponPanel == null)
                return false;

            List<RectTransform> stripNodes = CollectIfritStripChildren(weaponPanel);
            return TryResolveStripWeaponsSection(
                mfdRoot,
                canvas,
                weaponPanel,
                stripNodes,
                expandFullColumnWidth: false,
                isMedusaSection: false,
                isTarantulaSection: false,
                out resolved);
        }

        private static bool TryResolveStripWeaponsSection(
            GameObject mfdRoot,
            Canvas canvas,
            RectTransform weaponPanel,
            List<RectTransform> stripNodes,
            bool expandFullColumnWidth,
            bool isMedusaSection,
            bool isTarantulaSection,
            out ResolvedPanel resolved)
        {
            resolved = default;
            if (stripNodes.Count < 2)
                return false;

            bool useMedusaStrip = isMedusaSection || isTarantulaSection;
            Canvas overlayCanvas = TacScreenAccess.GetOverlayCanvas(weaponPanel) ?? canvas;
            float stripMinY = ComputeStripMinY(stripNodes, overlayCanvas);
            IfritStatusFloor statusFloor = ResolveIfritStatusFloor(mfdRoot, weaponPanel, overlayCanvas, stripMinY);

            List<RectTransform> zoneNodes = useMedusaStrip
                ? CollectMedusaZoneSources(
                    weaponPanel, overlayCanvas, stripNodes, stripMinY, statusFloor.TopY)
                : CollectIfritZoneSources(
                    weaponPanel, overlayCanvas, stripNodes, stripMinY, statusFloor.TopY);
            PanelRectState zone = PanelRectNormalizer.UnionOnCanvas(overlayCanvas, zoneNodes);
            zone = ExpandIfritStripZone(
                zone,
                overlayCanvas,
                zoneNodes,
                statusFloor.TopY,
                useMedusaStrip ? MedusaStubBottomInset : IfritStubBottomInset,
                forceStatusFloor: useMedusaStrip,
                expandMinX: !useMedusaStrip);

            if (expandFullColumnWidth)
            {
                zone = FitMedusaWeaponArmedFrame(
                    zone,
                    mfdRoot,
                    weaponPanel,
                    overlayCanvas,
                    statusFloor.TopY,
                    statusFloor.Frame);
            }

            if (!PanelRectNormalizer.IsWeaponsReplacementZone(zone))
                return false;

            if (statusFloor.TopY <= 0f)
                MaybeLogIfritWeaponPanelChildren(mfdRoot, weaponPanel, overlayCanvas, stripMinY);

            var hideTargets = new List<RectTransform>(stripNodes);
            if (useMedusaStrip)
            {
                foreach (RectTransform gapNode in CollectMedusaStripHiders(
                    weaponPanel, overlayCanvas, statusFloor.TopY, statusFloor.Frame, stripNodes))
                {
                    if (!hideTargets.Contains(gapNode))
                        hideTargets.Add(gapNode);
                }

                RectTransform? frameNode = FindMedusaArmedFrameNode(
                    weaponPanel, overlayCanvas, statusFloor.TopY);
                if (frameNode != null && !hideTargets.Contains(frameNode))
                    hideTargets.Add(frameNode);
            }
            else
            {
                foreach (RectTransform gapNode in CollectIfritGapHiders(
                    weaponPanel, overlayCanvas, stripMinY, statusFloor.TopY, statusFloor.Frame, stripNodes))
                {
                    if (!hideTargets.Contains(gapNode))
                        hideTargets.Add(gapNode);
                }
            }

            RectTransform panelAnchor = useMedusaStrip ? weaponPanel : stripNodes[0];
            resolved = new ResolvedPanel(
                panelAnchor,
                overlayCanvas,
                zone,
                hideTargets,
                statusFloor.TopY,
                statusFloor.FrameName,
                stripMinY,
                isMedusaSection: isMedusaSection,
                isTarantulaSection: isTarantulaSection);
            return true;
        }

        private static RectTransform? FindMedusaArmedFrameNode(
            RectTransform weaponPanel,
            Canvas canvas,
            float statusTopY)
        {
            PanelRectState panel = PanelRectNormalizer.CaptureOnCanvas(weaponPanel, canvas);
            RectTransform? frameNode = null;
            float bestFrameWidth = 0f;

            for (int i = 0; i < weaponPanel.childCount; i++)
            {
                RectTransform? child = weaponPanel.GetChild(i).GetComponent<RectTransform>();
                if (child == null || IsTimeObject(child))
                    continue;

                if (child.GetComponentInChildren<StatusGauges>(true) != null)
                    continue;

                PanelRectState childZone = PanelRectNormalizer.CaptureOnCanvas(child, canvas);
                float w = childZone.AnchorMax.x - childZone.AnchorMin.x;
                float h = childZone.AnchorMax.y - childZone.AnchorMin.y;

                if (statusTopY > 0f && childZone.AnchorMax.y <= statusTopY + 0.003f)
                    continue;

                if (childZone.AnchorMin.y > panel.AnchorMax.y + 0.01f)
                    continue;

                if (w < 0.20f || h < 0.06f)
                    continue;

                if (w <= bestFrameWidth)
                    continue;

                bestFrameWidth = w;
                frameNode = child;
            }

            return frameNode;
        }

        /// <summary>Horizontal fit: union WeaponPanel + StatusGauges column (sibling row is wider on EW-25).</summary>
        private static PanelRectState FitMedusaWeaponArmedFrame(
            PanelRectState zone,
            GameObject mfdRoot,
            RectTransform weaponPanel,
            Canvas canvas,
            float statusTopY,
            RectTransform? statusFrame)
        {
            PanelRectState panel = PanelRectNormalizer.CaptureOnCanvas(weaponPanel, canvas);
            float minX = panel.AnchorMin.x;
            float maxX = panel.AnchorMax.x;

            void Widen(PanelRectState rect)
            {
                float w = rect.AnchorMax.x - rect.AnchorMin.x;
                if (w < 0.08f || rect.AnchorMin.x < 0.35f)
                    return;

                minX = Mathf.Min(minX, rect.AnchorMin.x);
                maxX = Mathf.Max(maxX, rect.AnchorMax.x);
            }

            Widen(panel);

            RectTransform? frameNode = FindMedusaArmedFrameNode(weaponPanel, canvas, statusTopY);
            if (frameNode != null)
                Widen(PanelRectNormalizer.CaptureOnCanvas(frameNode, canvas));

            StatusGauges? gauges = mfdRoot.GetComponentInChildren<StatusGauges>(true);
            RectTransform? gaugeRt = gauges?.GetComponent<RectTransform>();
            RectTransform? walk = statusFrame ?? gaugeRt;
            for (int depth = 0; depth < 8 && walk != null; depth++)
            {
                if (!SpansFullMfd(walk))
                    Widen(PanelRectNormalizer.CaptureOnCanvas(walk, canvas));

                if (walk == weaponPanel)
                    break;

                walk = walk.parent?.GetComponent<RectTransform>();
            }

            if (gaugeRt != null)
            {
                var anchors = new List<RectTransform> { weaponPanel, gaugeRt };
                RectTransform? lca = FindLowestCommonAncestor(anchors);
                if (lca != null && !SpansFullMfd(lca))
                    Widen(PanelRectNormalizer.CaptureOnCanvas(lca, canvas));
            }

            if (weaponPanel.parent != null)
            {
                Transform parent = weaponPanel.parent;
                for (int i = 0; i < parent.childCount; i++)
                {
                    RectTransform? sibling = parent.GetChild(i).GetComponent<RectTransform>();
                    if (sibling == null)
                        continue;

                    if (sibling.GetComponentInChildren<StatusGauges>(true) == null
                        && sibling != weaponPanel)
                    {
                        continue;
                    }

                    Widen(PanelRectNormalizer.CaptureOnCanvas(sibling, canvas));
                }
            }

            float panelWidth = panel.AnchorMax.x - panel.AnchorMin.x;
            float zoneWidth = maxX - minX;
            string statusName = statusFrame != null ? statusFrame.name : gaugeRt != null ? gaugeRt.name : "none";
            MfdLog.Info(
                $"medusa columnX={minX:F2}-{maxX:F2} panelX={panel.AnchorMin.x:F2}-{panel.AnchorMax.x:F2} " +
                $"status={statusName} widen={zoneWidth - panelWidth:F3}");

            return new PanelRectState(
                new Vector2(minX, zone.AnchorMin.y),
                new Vector2(maxX, zone.AnchorMax.y),
                Vector2.zero,
                Vector2.zero);
        }

        private static List<RectTransform> CollectMedusaStripHiders(
            RectTransform weaponPanel,
            Canvas canvas,
            float statusTopY,
            RectTransform? statusFrame,
            IReadOnlyList<RectTransform> stripNodes)
        {
            var hiders = new List<RectTransform>();

            for (int i = 0; i < weaponPanel.childCount; i++)
            {
                RectTransform? child = weaponPanel.GetChild(i).GetComponent<RectTransform>();
                if (child == null || IsTimeObject(child))
                    continue;

                if (statusFrame != null && (child == statusFrame || child.IsChildOf(statusFrame)))
                    continue;

                if (child.GetComponentInChildren<StatusGauges>(true) != null)
                    continue;

                if (ContainsStripNode(child, stripNodes))
                    continue;

                PanelRectState childZone = PanelRectNormalizer.CaptureOnCanvas(child, canvas);
                if (statusTopY > 0f && childZone.AnchorMax.y <= statusTopY + 0.004f)
                    continue;

                if (!HasPanelSize(child))
                    continue;

                hiders.Add(child);
            }

            return hiders;
        }

        private static float ComputeStripMinY(IReadOnlyList<RectTransform> stripNodes, Canvas canvas)
        {
            float stripMinY = float.MaxValue;
            foreach (RectTransform node in stripNodes)
            {
                PanelRectState nodeZone = PanelRectNormalizer.CaptureOnCanvas(node, canvas);
                stripMinY = Mathf.Min(stripMinY, nodeZone.AnchorMin.y);
            }

            if (stripMinY > 0.99f)
                stripMinY = 0.78f;

            return stripMinY;
        }

        /// <summary>Medusa: include wide backgrounds/wireframe frames omitted by Ifrit x-filter.</summary>
        private static List<RectTransform> CollectMedusaZoneSources(
            RectTransform weaponPanel,
            Canvas canvas,
            List<RectTransform> stripNodes,
            float stripMinY,
            float statusTopY)
        {
            var nodes = new List<RectTransform>(stripNodes);

            for (int i = 0; i < weaponPanel.childCount; i++)
            {
                RectTransform? child = weaponPanel.GetChild(i).GetComponent<RectTransform>();
                if (child == null || nodes.Contains(child))
                    continue;

                if (!IsMedusaZoneExpansionChild(child, canvas, statusTopY, stripMinY))
                    continue;

                nodes.Add(child);
            }

            return nodes;
        }

        private static bool IsMedusaZoneExpansionChild(
            RectTransform child,
            Canvas canvas,
            float statusTopY,
            float stripMinY)
        {
            if (IsTimeObject(child))
                return false;

            if (PanelContainsEngineGauges(child))
                return false;

            if (child.GetComponentInChildren<StatusGauges>(true) != null)
                return false;

            PanelRectState childZone = PanelRectNormalizer.CaptureOnCanvas(child, canvas);
            if (childZone.AnchorMax.x < 0.38f)
                return false;

            if (statusTopY > 0f && childZone.AnchorMin.y >= statusTopY - 0.005f)
                return false;

            if (childZone.AnchorMax.y < stripMinY - 0.05f)
                return false;

            return HasPanelSize(child);
        }

        /// <summary>Union sources: strip nodes plus WeaponPanel siblings above StatusGauges (backgrounds, wireframe).</summary>
        private static List<RectTransform> CollectIfritZoneSources(
            RectTransform weaponPanel,
            Canvas canvas,
            List<RectTransform> stripNodes,
            float stripMinY,
            float statusTopY)
        {
            var nodes = new List<RectTransform>(stripNodes);

            for (int i = 0; i < weaponPanel.childCount; i++)
            {
                RectTransform? child = weaponPanel.GetChild(i).GetComponent<RectTransform>();
                if (child == null || nodes.Contains(child))
                    continue;

                if (!IsIfritZoneExpansionChild(child, canvas, statusTopY, stripMinY))
                    continue;

                nodes.Add(child);
            }

            return nodes;
        }

        private static bool IsIfritZoneExpansionChild(
            RectTransform child,
            Canvas canvas,
            float statusTopY,
            float stripMinY)
        {
            if (IsTimeObject(child))
                return false;

            if (PanelContainsEngineGauges(child))
                return false;

            if (child.GetComponentInChildren<StatusGauges>(true) != null)
                return false;

            PanelRectState childZone = PanelRectNormalizer.CaptureOnCanvas(child, canvas);
            if (childZone.AnchorMax.x < 0.45f)
                return false;

            if (statusTopY > 0f && childZone.AnchorMin.y >= statusTopY - 0.005f)
                return false;

            if (childZone.AnchorMax.y < stripMinY - 0.05f)
                return false;

            return HasPanelSize(child);
        }

        private const float IfritStubBottomInset = 0.005f;
        private const float MedusaStubBottomInset = 0.012f;

        /// <summary>Fill gap above FUEL/HEAT and flush to right-column left edge.</summary>
        private static PanelRectState ExpandIfritStripZone(
            PanelRectState zone,
            Canvas canvas,
            IReadOnlyList<RectTransform> zoneNodes,
            float statusTopY,
            float statusBottomInset = IfritStubBottomInset,
            bool forceStatusFloor = false,
            bool expandMinX = true)
        {
            float minY = zone.AnchorMin.y;
            float minX = zone.AnchorMin.x;

            if (statusTopY > 0f)
            {
                float floorY = statusTopY + statusBottomInset;
                if (forceStatusFloor)
                    minY = floorY;
                else if (statusTopY < minY - 0.001f)
                    minY = Mathf.Min(minY, floorY);
            }

            if (expandMinX)
            {
                foreach (RectTransform node in zoneNodes)
                {
                    PanelRectState nodeZone = PanelRectNormalizer.CaptureOnCanvas(node, canvas);
                    if (nodeZone.AnchorMin.x < minX)
                        minX = Mathf.Max(nodeZone.AnchorMin.x, 0.48f);
                }
            }

            if (Mathf.Approximately(minX, zone.AnchorMin.x) && Mathf.Approximately(minY, zone.AnchorMin.y))
                return zone;

            return new PanelRectState(
                new Vector2(minX, minY),
                zone.AnchorMax,
                Vector2.zero,
                Vector2.zero);
        }

        private readonly struct IfritStatusFloor
        {
            internal static readonly IfritStatusFloor None = new IfritStatusFloor(0f, null, "none");

            internal IfritStatusFloor(float topY, RectTransform? frame, string frameName)
            {
                TopY = topY;
                Frame = frame;
                FrameName = frameName;
            }

            internal float TopY { get; }
            internal RectTransform? Frame { get; }
            internal string FrameName { get; }
        }

        private static IfritStatusFloor ResolveIfritStatusFloor(
            GameObject mfdRoot,
            RectTransform weaponPanel,
            Canvas canvas,
            float stripMinY)
        {
            RectTransform? directChild = FindIfritStatusDirectChild(weaponPanel);
            if (directChild != null)
            {
                PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(directChild, canvas);
                if (zone.AnchorMax.y > 0f && zone.AnchorMax.y < stripMinY - 0.003f)
                    return new IfritStatusFloor(zone.AnchorMax.y, directChild, directChild.name);
            }

            IfritStatusFloor fromGauges = DiscoverStatusTopViaGaugesHierarchy(mfdRoot, canvas, stripMinY);
            if (fromGauges.TopY > 0f)
                return fromGauges;

            IfritStatusFloor fromChildScan = ScanWeaponPanelChildrenForStatusTop(weaponPanel, canvas, stripMinY);
            if (fromChildScan.TopY > 0f)
                return fromChildScan;

            IfritStatusFloor fromMarkers = GetIfritStatusTopFromMarkers(
                mfdRoot, weaponPanel, canvas, stripMinY);
            if (fromMarkers.TopY > 0f)
                return fromMarkers;

            return IfritStatusFloor.None;
        }

        private static RectTransform? FindIfritStatusDirectChild(RectTransform weaponPanel)
        {
            for (int i = 0; i < weaponPanel.childCount; i++)
            {
                RectTransform? child = weaponPanel.GetChild(i).GetComponent<RectTransform>();
                if (child == null || IsIfritStripChild(child) || IsTimeObject(child))
                    continue;

                if (child.GetComponentInChildren<StatusGauges>(true) != null)
                    return child;
            }

            return null;
        }

        private static IfritStatusFloor ScanWeaponPanelChildrenForStatusTop(
            RectTransform weaponPanel,
            Canvas canvas,
            float stripMinY)
        {
            float bestTop = 0f;
            RectTransform? bestFrame = null;

            for (int i = 0; i < weaponPanel.childCount; i++)
            {
                RectTransform? child = weaponPanel.GetChild(i).GetComponent<RectTransform>();
                if (child == null || IsIfritStripChild(child) || IsTimeObject(child))
                    continue;

                if (child.GetComponentInChildren<StatusGauges>(true) == null)
                    continue;

                PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(child, canvas);
                float h = zone.AnchorMax.y - zone.AnchorMin.y;
                if (zone.AnchorMax.x < 0.45f || h < 0.06f || zone.AnchorMax.y >= stripMinY - 0.003f)
                    continue;

                if (zone.AnchorMax.y <= bestTop)
                    continue;

                bestTop = zone.AnchorMax.y;
                bestFrame = child;
            }

            return bestFrame != null
                ? new IfritStatusFloor(bestTop, bestFrame, bestFrame.name)
                : IfritStatusFloor.None;
        }

        private static IfritStatusFloor GetIfritStatusTopFromMarkers(
            GameObject mfdRoot,
            RectTransform weaponPanel,
            Canvas canvas,
            float stripMinY)
        {
            float bestTop = 0f;
            RectTransform? bestFrame = null;
            Text[] texts = mfdRoot.GetComponentsInChildren<Text>(true);

            foreach (Text label in texts)
            {
                if (label == null || string.IsNullOrEmpty(label.text))
                    continue;

                string trimmed = label.text.Trim();
                if (!IsIfritStatusMarkerText(trimmed))
                    continue;

                RectTransform? frame = FindStatusOuterFrame(
                    label.rectTransform, weaponPanel, canvas, stripMinY);
                if (frame == null)
                    continue;

                PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(frame, canvas);
                if (zone.AnchorMax.y <= bestTop)
                    continue;

                bestTop = zone.AnchorMax.y;
                bestFrame = frame;
            }

            return bestFrame != null
                ? new IfritStatusFloor(bestTop, bestFrame, bestFrame.name)
                : IfritStatusFloor.None;
        }

        private static bool IsIfritStatusMarkerText(string trimmed) =>
            string.Equals(trimmed, "FUEL", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "HEAT", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "THROTTLE", System.StringComparison.OrdinalIgnoreCase);

        private static RectTransform? FindStatusOuterFrame(
            RectTransform? start,
            RectTransform weaponPanel,
            Canvas canvas,
            float stripMinY)
        {
            if (start == null)
                return null;

            RectTransform? best = null;
            float bestTop = 0f;
            RectTransform? rt = start;
            while (rt != null)
            {
                bool containsGauges = rt.GetComponentInChildren<StatusGauges>(true) != null;
                bool directChild = rt.parent == weaponPanel;
                if (containsGauges || directChild)
                {
                    PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(rt, canvas);
                    float h = zone.AnchorMax.y - zone.AnchorMin.y;
                    if (h >= 0.08f
                        && zone.AnchorMax.x >= 0.45f
                        && zone.AnchorMax.y < stripMinY - 0.003f
                        && zone.AnchorMax.y > bestTop)
                    {
                        bestTop = zone.AnchorMax.y;
                        best = rt;
                    }
                }

                rt = rt.parent?.GetComponent<RectTransform>();
            }

            return best;
        }

        private static IfritStatusFloor DiscoverStatusTopViaGaugesHierarchy(
            GameObject mfdRoot,
            Canvas canvas,
            float stripMinY)
        {
            StatusGauges? gauges = mfdRoot.GetComponentInChildren<StatusGauges>(true);
            if (gauges == null)
                return IfritStatusFloor.None;

            RectTransform? gaugeRt = gauges.GetComponent<RectTransform>();
            if (gaugeRt == null)
                return IfritStatusFloor.None;

            float bestTop = 0f;
            RectTransform? bestFrame = null;
            RectTransform? rt = gaugeRt;
            while (rt != null)
            {
                if (rt.GetComponentInChildren<StatusGauges>(true) == null)
                {
                    rt = rt.parent?.GetComponent<RectTransform>();
                    continue;
                }

                PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(rt, canvas);
                float h = zone.AnchorMax.y - zone.AnchorMin.y;
                if (h >= 0.06f
                    && zone.AnchorMax.x >= 0.45f
                    && zone.AnchorMax.y < stripMinY - 0.003f
                    && zone.AnchorMax.y > bestTop)
                {
                    bestTop = zone.AnchorMax.y;
                    bestFrame = rt;
                }

                rt = rt.parent?.GetComponent<RectTransform>();
            }

            return bestFrame != null
                ? new IfritStatusFloor(bestTop, bestFrame, bestFrame.name)
                : IfritStatusFloor.None;
        }

        private static List<RectTransform> CollectIfritGapHiders(
            RectTransform weaponPanel,
            Canvas canvas,
            float stripMinY,
            float statusTopY,
            RectTransform? statusFrame,
            IReadOnlyList<RectTransform> stripNodes)
        {
            var hiders = new List<RectTransform>();
            if (statusTopY <= 0f || stripMinY <= statusTopY + 0.001f)
                return hiders;

            for (int i = 0; i < weaponPanel.childCount; i++)
            {
                RectTransform? child = weaponPanel.GetChild(i).GetComponent<RectTransform>();
                if (child == null || IsIfritStripChild(child) || IsTimeObject(child))
                    continue;

                if (statusFrame != null && child == statusFrame)
                    continue;

                if (child.GetComponentInChildren<StatusGauges>(true) != null)
                    continue;

                if (ContainsStripNode(child, stripNodes))
                    continue;

                PanelRectState zone = PanelRectNormalizer.CaptureOnCanvas(child, canvas);
                if (zone.AnchorMax.x < 0.45f)
                    continue;

                bool overlapsGap = zone.AnchorMin.y < stripMinY + 0.005f
                    && zone.AnchorMax.y > statusTopY - 0.005f;
                if (!overlapsGap)
                    continue;

                hiders.Add(child);
            }

            return hiders;
        }

        private static bool ContainsStripNode(RectTransform candidate, IReadOnlyList<RectTransform> stripNodes)
        {
            foreach (RectTransform stripNode in stripNodes)
            {
                if (stripNode == candidate || stripNode.IsChildOf(candidate))
                    return true;
            }

            return false;
        }

        private static bool IsTimeObject(RectTransform child) =>
            string.Equals(child.name, "TimeObject", System.StringComparison.OrdinalIgnoreCase);

        private static void MaybeLogIfritWeaponPanelChildren(
            GameObject mfdRoot,
            RectTransform weaponPanel,
            Canvas canvas,
            float stripMinY)
        {
            if (_ifritStatusDiagDone)
                return;

            _ifritStatusDiagDone = true;
            MfdLog.Info($"ifrit statusTop unresolved stripMinY={stripMinY:F2} тАФ WeaponPanel children:");

            for (int i = 0; i < weaponPanel.childCount; i++)
            {
                RectTransform? child = weaponPanel.GetChild(i).GetComponent<RectTransform>();
                if (child == null)
                    continue;

                PanelRectState childNorm = PanelRectNormalizer.CaptureOnCanvas(child, canvas);
                MfdLog.Info(
                    $"  WeaponPanel child[{i}] {child.name} norm={FormatAnchors(childNorm)} " +
                    $"gauges={PanelContainsEngineGauges(child)} strip={IsIfritStripChild(child)}");
            }

            StatusGauges? rootGauges = mfdRoot.GetComponentInChildren<StatusGauges>(true);
            MfdLog.Info($"  mfdRoot StatusGauges={(rootGauges != null ? rootGauges.name : "null")}");
        }

        private static List<RectTransform> CollectIfritStripChildren(RectTransform weaponPanel)
        {
            var nodes = new List<RectTransform>();

            for (int i = 0; i < weaponPanel.childCount; i++)
            {
                RectTransform? child = weaponPanel.GetChild(i).GetComponent<RectTransform>();
                if (child == null || !IsIfritStripChild(child))
                    continue;

                nodes.Add(child);
            }

            return nodes;
        }

        private static bool IsIfritStripChild(RectTransform child)
        {
            string name = child.name;
            if (string.Equals(name, "frontProfile", System.StringComparison.OrdinalIgnoreCase))
                return true;

            return name.StartsWith("Box_", System.StringComparison.OrdinalIgnoreCase);
        }

        private static RectTransform? ResolveRevokerPanel(
            GameObject mfdRoot,
            Canvas canvas,
            List<RectTransform> primaryHits)
        {
            if (HasBomberBayMarkers(mfdRoot))
                return null;

            RectTransform? weaponPanel = FindPanelByName(mfdRoot, "WeaponPanel");
            if (weaponPanel != null
                && !PanelContainsEngineGauges(weaponPanel)
                && !HasIfritStripLayout(weaponPanel))
            {
                PanelRectState norm = PanelRectNormalizer.CaptureOnCanvas(weaponPanel, canvas);
                if (PanelRectNormalizer.IsTopRightZone(norm))
                    return weaponPanel;
            }

            if (primaryHits.Count == 0)
                return null;

            RectTransform? tight = weaponPanel != null
                ? FindWeaponsContainerWithoutEngine(weaponPanel, primaryHits)
                : null;

            if (tight != null && IsReasonableWeaponsStrip(tight, canvas, primaryHits, null))
                return tight;

            RectTransform? fromWalk = FindSmallestStripAncestor(primaryHits, weaponPanel, canvas);
            if (fromWalk != null)
                return fromWalk;

            return DiscoverViaRpmGauge(mfdRoot);
        }

        private static RectTransform? ResolveIfritWeaponsStrip(
            List<RectTransform> hardpointHits,
            List<RectTransform> gunHits,
            List<RectTransform> primaryHits,
            GameObject mfdRoot,
            Canvas canvas)
        {
            RectTransform? weaponPanel = FindPanelByName(mfdRoot, "WeaponPanel");
            if (weaponPanel == null)
                return null;

            if (hardpointHits.Count > 0)
            {
                RectTransform? bestChild = FindBestWeaponPanelChild(weaponPanel, canvas, hardpointHits, gunHits);
                if (bestChild != null)
                    return bestChild;

                RectTransform? rowContainer = FindLowestCommonAncestor(hardpointHits);
                RectTransform? expanded = ExpandToWireframeSection(
                    rowContainer, weaponPanel, canvas, hardpointHits, gunHits);
                if (expanded != null)
                    return expanded;
            }

            RectTransform? front = FindFrontProfile(weaponPanel);
            if (front?.parent != null)
            {
                RectTransform? section = front.parent.GetComponent<RectTransform>();
                if (section != null && IsReasonableWeaponsStrip(section, canvas, primaryHits, gunHits))
                    return section;
            }

            return null;
        }

        private static RectTransform? FindBestWeaponPanelChild(
            RectTransform weaponPanel,
            Canvas canvas,
            List<RectTransform> hardpointHits,
            List<RectTransform> gunHits)
        {
            RectTransform? bestChild = null;
            int bestScore = -1;

            for (int i = 0; i < weaponPanel.childCount; i++)
            {
                RectTransform? child = weaponPanel.GetChild(i).GetComponent<RectTransform>();
                if (child == null || PanelContainsEngineGauges(child))
                    continue;

                if (!HasWeaponsStripContent(child, hardpointHits, gunHits))
                    continue;

                if (!IsReasonableWeaponsStrip(child, canvas, hardpointHits, gunHits))
                    continue;

                int score = CountHitsUnder(child, hardpointHits);
                if (FindFrontProfileUnder(child) != null)
                    score += 10;

                if (score <= bestScore)
                    continue;

                bestScore = score;
                bestChild = child;
            }

            return bestChild;
        }

        private static RectTransform? ExpandToWireframeSection(
            RectTransform? rowContainer,
            RectTransform weaponPanel,
            Canvas canvas,
            List<RectTransform> hardpointHits,
            List<RectTransform> gunHits)
        {
            if (rowContainer == null)
                return null;

            RectTransform? best = null;
            RectTransform? current = rowContainer;

            while (current != null && (current == weaponPanel || current.IsChildOf(weaponPanel)))
            {
                if (!PanelContainsEngineGauges(current)
                    && HasWeaponsStripContent(current, hardpointHits, gunHits)
                    && IsReasonableWeaponsStrip(current, canvas, hardpointHits, gunHits))
                {
                    best = current;
                }

                if (current == weaponPanel)
                    break;

                current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
            }

            return best;
        }

        private static WeaponsReplacementResult ApplyHidden(ResolvedPanel resolved, string layout)
        {
            HiddenStripNodes.Clear();
            _overlayOnlyReplacement = false;

            _hiddenWeaponsPanel = null;
            _hiddenRootInstanceId = resolved.Panel != null ? resolved.Panel.GetInstanceID() : 0;
            _hiddenRootPath = BuildTransformPath(resolved.Panel);

            if (!resolved.OverlayOnly)
            {
                if (resolved.UseMultiHide)
                {
                    foreach (RectTransform node in resolved.HideTargets)
                    {
                        HiddenStripNodes.Add((node, node.gameObject.activeSelf));
                        node.gameObject.SetActive(false);
                    }
                }
                else
                {
                    _hiddenWeaponsPanel = resolved.Panel;
                    _weaponsWasActive = resolved.Panel.gameObject.activeSelf;
                    resolved.Panel.gameObject.SetActive(false);
                }
            }
            else
            {
                _overlayOnlyReplacement = true;
            }

            var zone = new MissileCameraZone(resolved.Zone);
            string label = resolved.IsAlkyonFullPanel
                ? $"alkyon├Ч{resolved.HideTargets.Count} ({resolved.Panel.name})"
                : resolved.IsDarkreachSection
                    ? $"darkreach├Ч{resolved.HideTargets.Count} ({resolved.Panel.name})"
                    : resolved.IsMedusaSection
                    ? $"medusa├Ч{resolved.HideTargets.Count} ({resolved.Panel.name})"
                    : resolved.IsCricketEngineSection
                        ? $"cricket-engine├Ч{resolved.HideTargets.Count} ({resolved.Panel.name})"
                    : resolved.IsChicaneEngineSection
                        ? $"chicane-engine├Ч{resolved.HideTargets.Count} ({resolved.Panel.name})"
                    : resolved.IsIbisSection
                        ? $"ibis├Ч{resolved.HideTargets.Count} ({resolved.Panel.name})"
                    : resolved.IsTarantulaSection
                        ? $"tarantula├Ч{resolved.HideTargets.Count} ({resolved.Panel.name})"
                    : resolved.IsVagrantNozzleEngineSection
                        ? $"vagrant-nozzle-engine├Ч{resolved.HideTargets.Count} ({resolved.Panel.name})"
                    : resolved.IsCompassEngineSection
                        ? $"engine├Ч{resolved.HideTargets.Count} ({resolved.Panel.name})"
                    : resolved.IsIfritStrip
                        ? $"ifrit-strip├Ч{resolved.HideTargets.Count} ({resolved.Panel.name})"
                        : resolved.Panel.name;
            string statusDiag = resolved.IsIfritStrip || resolved.IsMedusaSection || resolved.IsTarantulaSection
                || resolved.IsIbisSection
                ? $" stripBottom={resolved.StripBottomY:F2} statusTop={resolved.StatusTopY:F2} statusFrame={resolved.StatusFrameName}"
                : string.Empty;

            MfdLog.Info(
                (resolved.OverlayOnly ? "weapons overlay (" : "weapons hidden (") + label + $") layout={layout} " +
                $"zone={zone.MinX:F2}-{zone.MaxX:F2} y={zone.MinY:F2}-{zone.MaxY:F2}{statusDiag}");
            MissileCameraTelemetryLayout telemetryLayout = ResolveTelemetryLayout(resolved);

            return new WeaponsReplacementResult(
                zone,
                resolved.OverlayCanvas,
                resolved.SuppressBottomDivider,
                resolved.ShowPanelBorder,
                resolved.OverlayParent,
                resolved.SuppressBottomBorder,
                resolved.OverlayRotationZ,
                resolved.StubContentRotationZ,
                resolved.StubFontRef,
                resolved.StubContentBand,
                resolved.StubForcePortraitLayout,
                ResolveHudLeftInsetExtra(resolved),
                telemetryLayout);
        }

        private static MissileCameraTelemetryLayout ResolveTelemetryLayout(ResolvedPanel resolved) =>
            resolved.IsAlkyonFullPanel
                || resolved.IsCricketEngineSection
                || resolved.IsChicaneEngineSection
                || resolved.IsIbisSection
                || resolved.IsVagrantNozzleEngineSection
                || resolved.IsCompassEngineSection
                ? MissileCameraTelemetryLayout.RightColumn
                : MissileCameraTelemetryLayout.BottomRow;

        private readonly struct ResolvedPanel
        {
            internal ResolvedPanel(
                RectTransform panel,
                Canvas? overlayCanvas,
                PanelRectState zone,
                IReadOnlyList<RectTransform>? hideTargets = null,
                float statusTopY = 0f,
                string statusFrameName = "none",
                float stripBottomY = 0f,
                bool isAlkyonFullPanel = false,
                bool isDarkreachSection = false,
                bool isMedusaSection = false,
                bool isCompassEngineSection = false,
                bool isTarantulaSection = false,
                bool isCricketEngineSection = false,
                bool isChicaneEngineSection = false,
                bool isIbisSection = false,
                bool isVagrantNozzleEngineSection = false,
                bool overlayOnly = false,
                RectTransform? overlayParent = null,
                float overlayRotationZ = 0f,
                float stubContentRotationZ = 0f,
                float stubFontRef = 0f,
                Vector2 stubContentBand = default,
                bool stubForcePortraitLayout = false)
            {
                Panel = panel;
                OverlayCanvas = overlayCanvas;
                OverlayParent = overlayParent;
                Zone = zone;
                HideTargets = hideTargets ?? new[] { panel };
                StatusTopY = statusTopY;
                StatusFrameName = statusFrameName;
                StripBottomY = stripBottomY;
                IsMedusaSection = isMedusaSection;
                IsDarkreachSection = isDarkreachSection;
                IsCompassEngineSection = isCompassEngineSection;
                IsTarantulaSection = isTarantulaSection;
                IsCricketEngineSection = isCricketEngineSection;
                IsChicaneEngineSection = isChicaneEngineSection;
                IsIbisSection = isIbisSection;
                IsVagrantNozzleEngineSection = isVagrantNozzleEngineSection;
                OverlayOnly = overlayOnly;
                IsIfritStrip = !isAlkyonFullPanel && !isDarkreachSection && !isMedusaSection
                    && !isCompassEngineSection && !isTarantulaSection && !isCricketEngineSection
                    && !isChicaneEngineSection && !isIbisSection && !isVagrantNozzleEngineSection
                    && hideTargets != null && hideTargets.Count > 1;
                IsAlkyonFullPanel = isAlkyonFullPanel;
                UseMultiHide = isAlkyonFullPanel || isDarkreachSection || isMedusaSection || isCompassEngineSection
                    || isTarantulaSection || isCricketEngineSection || isChicaneEngineSection || isIbisSection
                    || isVagrantNozzleEngineSection
                    || (hideTargets != null && hideTargets.Count > 1);
                SuppressBottomDivider = UseMultiHide;
                ShowPanelBorder = isAlkyonFullPanel || isDarkreachSection || isCompassEngineSection
                    || isChicaneEngineSection || isVagrantNozzleEngineSection;
                SuppressBottomBorder = isMedusaSection || isTarantulaSection;
                OverlayRotationZ = overlayRotationZ;
                StubContentRotationZ = stubContentRotationZ;
                StubFontRef = stubFontRef;
                StubContentBand = stubContentBand == default ? Vector2.up : stubContentBand;
                StubForcePortraitLayout = stubForcePortraitLayout;
            }

            internal RectTransform Panel { get; }
            internal Canvas? OverlayCanvas { get; }
            internal RectTransform? OverlayParent { get; }
            internal PanelRectState Zone { get; }
            internal IReadOnlyList<RectTransform> HideTargets { get; }
            internal float StatusTopY { get; }
            internal string StatusFrameName { get; }
            internal float StripBottomY { get; }
            internal bool IsIfritStrip { get; }
            internal bool IsAlkyonFullPanel { get; }
            internal bool IsDarkreachSection { get; }
            internal bool IsMedusaSection { get; }
            internal bool IsCompassEngineSection { get; }
            internal bool IsTarantulaSection { get; }
            internal bool IsCricketEngineSection { get; }
            internal bool IsChicaneEngineSection { get; }
            internal bool IsIbisSection { get; }
            internal bool IsVagrantNozzleEngineSection { get; }
            internal bool OverlayOnly { get; }
            internal bool UseMultiHide { get; }
            internal bool SuppressBottomDivider { get; }
            internal bool ShowPanelBorder { get; }
            internal bool SuppressBottomBorder { get; }
            internal float OverlayRotationZ { get; }
            internal float StubContentRotationZ { get; }
            internal float StubFontRef { get; }
            internal Vector2 StubContentBand { get; }
            internal bool StubForcePortraitLayout { get; }
        }

        private static void MaybeDumpTextInventory(GameObject mfdRoot)
        {
            if (!MfdLayoutConfig.DebugStub || _debugDumpDone)
                return;

            _debugDumpDone = true;
            Text[] texts = mfdRoot.GetComponentsInChildren<Text>(true);
            int count = Mathf.Min(texts.Length, 30);
            MfdLog.Info($"MFD text inventory ({texts.Length} total, showing {count}):");
            for (int i = 0; i < count; i++)
            {
                Text text = texts[i];
                if (text == null)
                    continue;

                RectTransform rt = text.rectTransform;
                string snippet = text.text.Replace('\n', '|');
                if (snippet.Length > 40)
                    snippet = snippet.Substring(0, 40);

                MfdLog.Info(
                    $"  [{i}] {text.name} text=\"{snippet}\" anchor={FormatAnchors(rt)} rect={FormatRectSize(rt)}");
            }
        }

        private static void MaybeLogDiscoveryFailure(
            GameObject mfdRoot,
            Canvas canvas,
            List<RectTransform> allHits,
            List<RectTransform> primaryHits,
            List<RectTransform> hardpointHits,
            List<RectTransform> gunHits,
            RectTransform? candidate,
            PanelRectState candidateZone,
            string? rejectReason)
        {
            if (_failureDiagDone)
                return;

            _failureDiagDone = true;

            RectTransform? weaponPanel = FindPanelByName(mfdRoot, "WeaponPanel");
            string weaponPanelNorm = weaponPanel != null
                ? FormatAnchors(PanelRectNormalizer.CaptureOnCanvas(weaponPanel, canvas))
                : "n/a";

            string candidateInfo = candidate != null
                ? $"{candidate.name} norm={FormatAnchors(candidateZone)} gauges={PanelContainsEngineGauges(candidate)}"
                : "panel=null";

            MfdLog.Info(
                $"discovery failed primaryHits={primaryHits.Count} hardpointHits={hardpointHits.Count} " +
                $"gunHits={gunHits.Count} allHits={allHits.Count} WeaponPanel={weaponPanelNorm} " +
                $"candidate={candidateInfo} reject={(rejectReason ?? "n/a")}");

            if (weaponPanel == null)
                return;

            RectTransform? front = FindFrontProfile(weaponPanel);
            if (front?.parent != null)
            {
                RectTransform section = front.parent.GetComponent<RectTransform>();
                PanelRectState sectionNorm = PanelRectNormalizer.CaptureOnCanvas(section, canvas);
                MfdLog.Info(
                    $"  frontProfile.parent={section.name} norm={FormatAnchors(sectionNorm)} " +
                    $"stripOk={IsReasonableWeaponsStrip(section, canvas, hardpointHits, gunHits)}");
            }

            for (int i = 0; i < weaponPanel.childCount; i++)
            {
                RectTransform? child = weaponPanel.GetChild(i).GetComponent<RectTransform>();
                if (child == null)
                    continue;

                PanelRectState childNorm = PanelRectNormalizer.CaptureOnCanvas(child, canvas);
                MfdLog.Info(
                    $"  WeaponPanel child[{i}] {child.name} norm={FormatAnchors(childNorm)} " +
                    $"gauges={PanelContainsEngineGauges(child)} armed={CountHitsUnder(child, hardpointHits)} " +
                    $"stripOk={IsReasonableWeaponsStrip(child, canvas, hardpointHits, gunHits)}");
            }
        }

        private static string? DescribeStripRejectReason(
            RectTransform rt,
            Canvas canvas,
            List<RectTransform> hardpointHits,
            List<RectTransform> gunHits)
        {
            if (!HasPanelSize(rt))
                return "too_small";

            if (PanelContainsEngineGauges(rt))
                return "has_engine_gauges";

            if (!HasWeaponsStripContent(rt, hardpointHits, gunHits))
                return "no_strip_content";

            PanelRectState norm = PanelRectNormalizer.CaptureOnCanvas(rt, canvas);
            float w = norm.AnchorMax.x - norm.AnchorMin.x;
            float h = norm.AnchorMax.y - norm.AnchorMin.y;
            if (w < 0.06f || h < 0.06f || w > 0.55f || h > 0.55f)
                return $"bad_size w={w:F2} h={h:F2}";

            if (norm.AnchorMax.x < 0.45f || norm.AnchorMin.x < 0.48f)
                return $"bad_x min={norm.AnchorMin.x:F2} max={norm.AnchorMax.x:F2}";

            if (!PanelRectNormalizer.IsWeaponsReplacementZone(norm))
                return "zone_validation";

            return null;
        }

        private static List<RectTransform> CollectWeaponHits(GameObject mfdRoot)
        {
            Text[] texts = mfdRoot.GetComponentsInChildren<Text>(true);
            var hits = new List<RectTransform>();

            foreach (Text text in texts)
            {
                if (text == null || string.IsNullOrEmpty(text.text))
                    continue;

                if (!IsWeaponsMarker(text.text))
                    continue;

                RectTransform textRt = text.rectTransform;
                if (textRt != null && !hits.Contains(textRt))
                    hits.Add(textRt);
            }

            return hits;
        }

        private static List<RectTransform> CollectPrimaryWeaponHits(GameObject mfdRoot)
        {
            Text[] texts = mfdRoot.GetComponentsInChildren<Text>(true);
            var hits = new List<RectTransform>();

            foreach (Text text in texts)
            {
                if (text == null || string.IsNullOrEmpty(text.text))
                    continue;

                if (!IsPrimaryWeaponsMarker(text.text))
                    continue;

                RectTransform textRt = text.rectTransform;
                if (textRt != null && !hits.Contains(textRt))
                    hits.Add(textRt);
            }

            return hits;
        }

        private static List<RectTransform> CollectHardpointArmedHits(GameObject mfdRoot)
        {
            Text[] texts = mfdRoot.GetComponentsInChildren<Text>(true);
            var hits = new List<RectTransform>();

            foreach (Text text in texts)
            {
                if (text == null || string.IsNullOrEmpty(text.text))
                    continue;

                if (!IsHardpointArmedMarker(text.text))
                    continue;

                RectTransform textRt = text.rectTransform;
                if (textRt != null && !hits.Contains(textRt))
                    hits.Add(textRt);
            }

            RectTransform? weaponPanel = FindPanelByName(mfdRoot, "WeaponPanel");
            if (weaponPanel == null)
                return hits;

            for (int i = 0; i < weaponPanel.childCount; i++)
            {
                RectTransform? box = weaponPanel.GetChild(i).GetComponent<RectTransform>();
                if (box == null || !box.name.StartsWith("Box_", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!BoxShowsArmed(box))
                    continue;

                if (!hits.Contains(box))
                    hits.Add(box);
            }

            return hits;
        }

        private static bool BoxShowsArmed(RectTransform box)
        {
            foreach (Text text in box.GetComponentsInChildren<Text>(true))
            {
                if (text == null || string.IsNullOrEmpty(text.text))
                    continue;

                string norm = Normalize(text.text);
                if (norm.Contains("WEAPON ARMED") || norm == "ARMED" || norm == "WEAPON")
                    return true;
            }

            return false;
        }

        private static List<RectTransform> CollectGunArmedHits(GameObject mfdRoot)
        {
            Text[] texts = mfdRoot.GetComponentsInChildren<Text>(true);
            var hits = new List<RectTransform>();

            foreach (Text text in texts)
            {
                if (text == null || string.IsNullOrEmpty(text.text))
                    continue;

                if (!IsGunArmedMarker(text.text))
                    continue;

                RectTransform textRt = text.rectTransform;
                if (textRt != null && !hits.Contains(textRt))
                    hits.Add(textRt);
            }

            RectTransform? weaponPanel = FindPanelByName(mfdRoot, "WeaponPanel");
            if (weaponPanel != null)
            {
                RectTransform? gunBox = weaponPanel.Find("Box_Gun") as RectTransform;
                if (gunBox != null && !hits.Contains(gunBox))
                    hits.Add(gunBox);
            }

            return hits;
        }

        private static string Normalize(string raw) =>
            Regex.Replace(raw.ToUpperInvariant().Trim(), @"\s+", " ");

        private static bool IsHardpointLabel(string norm)
        {
            if (norm.Contains("TIP") || norm.Contains("PYLON") || norm.Contains("BAY"))
                return true;
            return norm.Contains("R.TIP") || norm.Contains("L.TIP")
                || norm.Contains("R.PYLON") || norm.Contains("L.PYLON");
        }

        private static bool IsHardpointArmedMarker(string raw)
        {
            string norm = Normalize(raw);
            return !string.IsNullOrEmpty(norm) && norm.Contains("WEAPON ARMED");
        }

        private static bool IsGunArmedMarker(string raw)
        {
            string norm = Normalize(raw);
            if (string.IsNullOrEmpty(norm))
                return false;

            if (norm.Contains("GUN ARMED"))
                return true;

            return norm == "GUN" || (norm.Contains("GUN") && norm.Contains("ARMED"));
        }

        private static bool IsPrimaryWeaponsMarker(string raw)
        {
            string norm = Normalize(raw);
            if (string.IsNullOrEmpty(norm))
                return false;

            if (norm.Contains("WEAPON ARMED") || norm.Contains("GUN ARMED"))
                return true;
            if (IsMedusaWeaponsMarker(raw))
                return true;
            if (IsTarantulaWeaponsMarker(raw))
                return true;
            if (norm == "WEAPON" || norm == "ARMED")
                return true;
            return norm.Contains("GUN") && norm.Contains("ARMED");
        }

        private static bool IsWeaponsMarker(string raw)
        {
            if (IsPrimaryWeaponsMarker(raw))
                return true;

            return IsHardpointLabel(Normalize(raw));
        }

        private static bool HasPanelSize(RectTransform rt)
        {
            Rect r = rt.rect;
            return r.width >= 40f && r.height >= 24f;
        }

        private static bool HasWeaponsStripContent(
            RectTransform rt,
            List<RectTransform> hardpointHits,
            List<RectTransform> gunHits)
        {
            if (FindFrontProfileUnder(rt) != null)
                return true;

            if (IsIfritStripChild(rt))
                return true;

            if (CountHitsUnder(rt, hardpointHits) >= 3)
                return true;

            foreach (RectTransform hit in gunHits)
            {
                if (hit != null && hit.IsChildOf(rt))
                    return true;
            }

            return false;
        }

        private static int CountHitsUnder(RectTransform rt, List<RectTransform> hits)
        {
            int count = 0;
            foreach (RectTransform hit in hits)
            {
                if (hit != null && hit.IsChildOf(rt))
                    count++;
            }

            return count;
        }

        private static RectTransform? FindSmallestStripAncestor(
            List<RectTransform> primaryHits,
            RectTransform? ceiling,
            Canvas canvas)
        {
            RectTransform? best = null;
            float bestArea = float.MaxValue;

            foreach (RectTransform hit in primaryHits)
            {
                RectTransform? current = hit;
                int depth = 0;
                while (current != null && depth < 16)
                {
                    if (ceiling != null && !current.IsChildOf(ceiling) && current != ceiling)
                        break;

                    if (ContainsAllHits(current, primaryHits)
                        && IsReasonableWeaponsStrip(current, canvas, primaryHits, null))
                    {
                        PanelRectState norm = PanelRectNormalizer.CaptureOnCanvas(current, canvas);
                        float area = (norm.AnchorMax.x - norm.AnchorMin.x) * (norm.AnchorMax.y - norm.AnchorMin.y);
                        if (area < bestArea)
                        {
                            bestArea = area;
                            best = current;
                        }
                    }

                    current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
                    depth++;
                }
            }

            return best;
        }

        private static RectTransform? FindWeaponsContainerWithoutEngine(
            RectTransform root,
            List<RectTransform> hits)
        {
            RectTransform? best = null;
            float bestArea = float.MaxValue;

            foreach (RectTransform hit in hits)
            {
                RectTransform? current = hit;
                int depth = 0;
                while (current != null && current != root && depth < 14)
                {
                    if (!PanelContainsEngineGauges(current) && ContainsAllHits(current, hits))
                    {
                        float area = current.rect.width * current.rect.height;
                        if (area < bestArea)
                        {
                            bestArea = area;
                            best = current;
                        }
                    }

                    current = current.parent != null ? current.parent.GetComponent<RectTransform>() : null;
                    depth++;
                }
            }

            return best;
        }

        private static RectTransform? FindFrontProfile(RectTransform weaponPanel)
        {
            return FindFrontProfileUnder(weaponPanel);
        }

        private static RectTransform? FindFrontProfileUnder(RectTransform root)
        {
            foreach (RectTransform rt in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (string.Equals(rt.name, "frontProfile", System.StringComparison.OrdinalIgnoreCase))
                    return rt;
            }

            return null;
        }

        private static bool ContainsAllHits(RectTransform panel, List<RectTransform> hits)
        {
            foreach (RectTransform hit in hits)
            {
                if (hit == null || (!hit.IsChildOf(panel) && hit != panel))
                    return false;
            }

            return true;
        }

        private static bool IsReasonableWeaponsStrip(
            RectTransform rt,
            Canvas canvas,
            List<RectTransform> hardpointHits,
            List<RectTransform>? gunHits)
        {
            if (!HasPanelSize(rt))
                return false;

            if (PanelContainsEngineGauges(rt))
                return false;

            if (!HasWeaponsStripContent(rt, hardpointHits, gunHits ?? new List<RectTransform>()))
                return false;

            PanelRectState norm = PanelRectNormalizer.CaptureOnCanvas(rt, canvas);
            float w = norm.AnchorMax.x - norm.AnchorMin.x;
            float h = norm.AnchorMax.y - norm.AnchorMin.y;
            if (w < 0.06f || h < 0.06f || w > 0.55f || h > 0.55f)
                return false;

            if (norm.AnchorMax.x < 0.45f || norm.AnchorMin.x < 0.48f)
                return false;

            return true;
        }

        private static RectTransform? FindPanelByName(GameObject mfdRoot, string name)
        {
            foreach (RectTransform rt in mfdRoot.GetComponentsInChildren<RectTransform>(true))
            {
                if (string.Equals(rt.name, name, System.StringComparison.OrdinalIgnoreCase))
                    return rt;
            }

            return null;
        }

        private static bool PanelContainsEngineGauges(RectTransform panel)
        {
            return panel.GetComponentInChildren<StatusGauges>(true) != null
                || panel.GetComponentInChildren<RPMGauge>(true) != null;
        }

        private static RectTransform? DiscoverViaRpmGauge(GameObject mfdRoot)
        {
            RPMGauge? rpm = mfdRoot.GetComponentInChildren<RPMGauge>(true);
            if (rpm == null)
                return null;

            RectTransform? engineRt = rpm.GetComponent<RectTransform>();
            if (engineRt == null)
                return null;

            float engineTop = engineRt.anchorMax.y;
            Transform? parent = engineRt.parent;
            if (parent == null)
                return null;

            RectTransform? best = null;
            float bestArea = 0f;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                RectTransform? crt = child.GetComponent<RectTransform>();
                if (crt == null || crt == engineRt)
                    continue;

                if (crt.anchorMax.y < engineTop - 0.02f)
                    continue;
                if (crt.anchorMin.x < 0.35f)
                    continue;
                if (!HasPanelSize(crt))
                    continue;

                float area = crt.rect.width * crt.rect.height;
                if (area <= bestArea)
                    continue;

                bestArea = area;
                best = crt;
            }

            if (best != null)
                MfdLog.Info($"weapons panel via RPMGauge sibling ({best.name})");

            return best;
        }

        private static RectTransform? FindLowestCommonAncestor(List<RectTransform> nodes)
        {
            if (nodes.Count == 0)
                return null;

            if (nodes.Count == 1)
            {
                RectTransform hit = nodes[0];
                RectTransform? parent = hit.parent != null ? hit.parent.GetComponent<RectTransform>() : null;
                return parent ?? hit;
            }

            RectTransform? candidate = nodes[0];
            while (candidate != null)
            {
                bool containsAll = true;
                foreach (RectTransform node in nodes)
                {
                    if (node == null || !node.IsChildOf(candidate))
                    {
                        containsAll = false;
                        break;
                    }
                }

                if (containsAll)
                {
                    if (SpansFullMfd(candidate))
                    {
                        Transform? parent = candidate.parent;
                        candidate = parent != null ? parent.GetComponent<RectTransform>() : null;
                        continue;
                    }

                    return candidate;
                }

                Transform? next = candidate.parent;
                candidate = next != null ? next.GetComponent<RectTransform>() : null;
            }

            return nodes[0];
        }

        private static bool SpansFullMfd(RectTransform rt)
        {
            float anchorW = rt.anchorMax.x - rt.anchorMin.x;
            float anchorH = rt.anchorMax.y - rt.anchorMin.y;
            if (anchorW > 0.65f || anchorH > 0.8f)
                return true;

            return rt.rect.width > 400f && rt.rect.height > 300f;
        }

        private static string FormatAnchors(RectTransform rt) =>
            $"{rt.anchorMin.x:F2},{rt.anchorMin.y:F2}-{rt.anchorMax.x:F2},{rt.anchorMax.y:F2}";

        private static string FormatAnchors(PanelRectState rect) =>
            $"{rect.AnchorMin.x:F2},{rect.AnchorMin.y:F2}-{rect.AnchorMax.x:F2},{rect.AnchorMax.y:F2}";

        private static string FormatRectSize(RectTransform rt) =>
            $"{rt.rect.width:F0}x{rt.rect.height:F0}";
    }
}
