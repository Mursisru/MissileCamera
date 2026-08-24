# Changelog

All notable changes to **MissileCamera** (BepInEx) are documented here. Public version is clean semver in `AppVersion.ReleaseBase` / `DisplayVersion` / `[BepInPlugin]` (identical strings, no letter suffixes).

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

## [2.1.2] — 2026-08-24

> [!IMPORTANT]
> Pair with MissileCamera: Remote Control **2.1.2**.

### Fixed

- Fullscreen HUD missile names now use the instance `unitName` (RC clones share the vanilla flying prefab `definition`).

## [2.1.0] — 2026-08-22

> [!IMPORTANT]
> **NOXMFD bridge release** — headless seeker feed for browser MFD extensions via `McBridge.RequestCapture`. Requires MissileCamera: Remote Control **2.1.0** for full RC in NOXMFD.

### Added

- **`MissileCameraBridge` config section** — headless NOXMFD feed tuning (16:9 render size, MJPEG/stream intervals, marker label modes, cockpit MFD suppression).
- **`Bridge/McBridge.cs` extension surface** — `RequestCapture`, `FeedTexture`, telemetry/markers JSON, bridge tuning getters for the NOXMFD extension.

### Changed

- Default bridge feed resolution **960×540** (was 512×512) for widescreen browser MFD pages.

### Fixed

- Headless bridge no longer loses the seeker rig when cockpit layout soft-parks (`SoftParkRig` bypass while `RequestCapture` is active).
- Cockpit MFD layout gated while NOXMFD owns capture; no auto-restore onto unrelated NOXMFD tabs when bridge ends.

## [2.0.0] — 2026-08-10

> [!IMPORTANT]
> **Major release** — fullscreen gunship HUD is the default FS experience; seeker pose and RemoteControl-facing contracts remain compatible (`IsActive`, `TryGetFeedCamera`, `FlirGaugeBars.Update`).

### Added

- **Fullscreen gunship HUD (EN):** COD AC-130 style chrome (`Hud/Gunship/*`) — telemetry, reticle + N/E/S/W, range/zoom tape, NAV footer, weapon column, FUEL%/THR%, MODE.
- **FS look-around (RMB):** pan seeker within a cone; main gunship reticle stays on bore; small center mark follows look.
- **Soft horizon seeker:** counter-roll around bore so calm flight (and banked aircraft launch) levels the picture; TurnLook bank remains on horizontal turns.
- **GitHub update check:** compares `AppVersion` to latest **full** release (`releases/latest`); one EN prompt per session when outdated; offline = silent; Don't show again in cfg.

### Changed

- **Default FS vision:** WhiteHot.
- **FS zoom:** optical FOV only (fixed RT size — no magnify lag).
- **FUEL/THR:** live motor values (no display smoothing); RC gauge field surface kept.
- **SoftPark:** restore weapons + destroy stub between missiles so MFD does not stay blank.

### Fixed

- **CombatHUD "Target" TMP** / white marker bleach in FS.
- **Range tape stuck at 500 m** under RC aim stubs — optical FOV footprint drives zoom feedback again.
- **Pitch / turn false camera snaps** from world-up rebuild and TurnLook sign jumps.
- **Rotated MFD HUD cull** (`RectMask2D` under ~90° RotatedView).
- **Invisible FS cost:** throttled SuppressIls / marker force / stub hide.

## [1.9.9] — 2026-08-09

> [!NOTE]
> **Pre-release** — gunship fullscreen HUD and related MFD polish. Test before relying on it in a long campaign.

### Added

- **Fullscreen gunship HUD (EN):** COD AC-130 style chrome (`Hud/Gunship/*`) — telemetry with jitter, reticle + N/E/S/W, range tape, NAV footer, weapon column with owned-missile count, FUEL%/THR%, MODE.
- **FS TV look:** safe UI CRT overlay (`GunshipTvOverlay` scan/grain/vignette). PostFx Scanlines stage stays off (avoids zoom lag / pink IR).

### Changed

- **Default FS vision:** WhiteHot.
- **FS zoom:** optical FOV only — RT size no longer upscales with magnification (fixes zoom lag).
- **FS boot loading FX:** skipped — chrome shows immediately.
- **FUEL/THR:** live motor values (no display/snapshot smoothing). RemoteControl `FlirGaugeBars.Update` field surface kept.
- **SoftPark (missile gap):** restore weapons + destroy stub (`destroyHud:true`) so MFD does not stay blank; next launch cold-binds again.

### Fixed

- **CombatHUD "Target" TMP** hidden in FS; never `SetTargetArrow(true)` from reproject.
- **White unit markers:** opaque contrast disabled — vanilla faction colors kept.
- **Invisible FS LateUpdate cost:** SuppressIls / Force markers / TickHideStubs soft-rate; `SetTargetArrow(false)` only when arrow/TMP still visible.
- **MFD camera without classic UI:** feed+HUD share `MissileCameraContent`; BottomRow on portrait/Darkreach; stub labels until chrome live.
- **Rotated MFD (~90°):** disable HUD `RectMask2D` under `RotatedView` (Unity mask was culling chrome while feed still drew).
- **Multirole1 Skip:** size gate only when weapons panel cannot be discovered.

## [1.2.1] — 2026-08-01

### Fixed

- **Hotfix — MFD hitch on missile launch:** SoftPark no longer tears down the seeker Rig/RT or clears feed RawImage/panel refs between missiles. Next launch soft-rewakes the stub (`TrySoftRewakeOverlay`) without `BindPanel` / full `EnsureBuilt`. Live overlay skips deferred `EnsureLayout` on `OnRegisterMissile`. Nose camera offset is cached per missile definition. `ClearLayout` / HardReset still fully wipe with `destroyHud:true`.

> [!NOTE]
> **First cold launch** in a sortie still pays one-time weapons discovery + TacStub create. Subsequent launches in the same layout session should no longer hitch.

## [1.2.0] — 2026-07-30

### Added

- **Fullscreen optical zoom:** mouse wheel magnification **1×…50×** (`fov = baseFov / mag`), middle-click reset to 1×. RT supersample buckets (up to 4K) + Bilinear when zoomed.
- **Fullscreen vision cycle (J):** Color → NightVision → WhiteHot → BlackHot → WhiteContour → BlackContour. Local feed Volume NVG only (never `NightVision.Toggle`). Contour modes use Sobel edge blit.
- **Fullscreen boot (~3.5s, first enter per mission):** tile reassembly → FLIR flicker + character calibration → hex/value drums + diagnostics; FUEL/THR gauges stay live during boot.
- **Fullscreen FLIR HUD:** framed MSL/KIN, LAUNCH, TGT TRACK/ENGAGE, SENSOR, GUIDANCE panels; FUEL/THR edge gauge bars; ownship nose PiP (bottom-left, ~30 FPS); stock pitch ladder (`PitchLadderEnabled`); hollow green intercept ring; large missile stats above gimbal dials.
- **Independent fullscreen host:** dedicated overlay panel + RawImage + HUD — works without an active MFD layout; landscape HUD locked (rotation 0).
- **SFB-81 Darkreach:** tac-right weapon panel discovery for MFD/FS when bay text is absent.
- **SAH-46 Chicane:** MissileCamera on left Turbine MFD (TMP TURBINE + EngineTelemetry); overlay-only (vanilla Turbine stays live); TAIL DUCT unchanged.
- **Config slim:** player cfg = enable toggles + feed/IR thresholds + sizes/FPS + every keybind as `KeyboardShortcut`. Advanced geometry/colors hardcoded.
- **All keybinds configurable** via Configuration Manager (`MissileCameraControls` / `MissileCameraFullscreen` / `MissileCameraAircraftCam`). Polled through Unity `Input.GetKey*` (not BepInEx `IsDown`).

### Changed

- **Versioning:** clean numeric semver only (`MAJOR.MINOR.PATCH`). No letter suffixes.
- **Loss / switch / exit flash:** bordered **NO SIGNAL** replaces TV static.
- **Auto IR:** lighting-only policy (`GetDaylightFactor` / `GetAmbientLight` + hysteresis); HDR→InfraredBlit pipeline aligned with TargetCam.
- **Soft tick rates:** HUD / PiP / AircraftCam / SmoothHz defaults **10 Hz**; IR policy + config refresh **1 s**. Feed `RenderFps` stays **30**; fullscreen video every frame.
- **Fullscreen toggle default:** **K** (no Right Alt). Vision cycle **J**. Zoom reset **MMB**.
- **DetailRenderer** temporarily follows seeker camera so trees/grass cull match the feed. CullingMask always ORs `Effects|TransparentFX`.
- **VT-7 Vagrant:** layout clamp `maxY=0.79` (weapons silhouette kept) — base support shipped in 0.27.4.

### Fixed

- **Cockpit camera fly-out:** fullscreen never writes `CameraStateManager` / `cameraPivot` / `mainCamera` / FOV. Feed is RT → `RawImage` only.
- **Fullscreen markers:** CombatHUD contacts reprojected via feed `WorldToViewportPoint` → Screen; Missile unit images hidden in FS (no `DeselectMarker`); restored on exit.
- **Fullscreen target lock:** snapshot/filter/restore without `WeaponManager.TargetListChanged`; never `DeselectMarker` on `unit is Missile`.
- **FS input gate:** suppress Rewired Zoom/FOV only while mod zoom keys or wheel are active — never zero pitch/roll/throttle.
- **Multi-sortie lifecycle:** HardReset on menu / GameWorld unload / pre-mission; session gate; bind-first overlay (`_overlayActive` only after feed bind); half-state heal; null-safe HUD/PiP destroy; weapons Restore before new hide; retain layout while owned missiles fly (including TargetCam disable).
- **WayPoint / MissionTarget:** ObjectiveOverlayManager no longer forced off on HardReset unless fullscreen actually suppressed it.
- **Input lag:** mod keybinds polled in `FeedDriver.Update` (not EndOfFrame / idle wait). FS works even when MFD overlay is missing.

## [0.27.4] — 2026-07-13

### Added

- Automatic B/W IR missile feed at night (`timeOfDay` before 6 or after 18) or when ambient light is low (same night window as vanilla TargetCam).
- Configuration Manager keys under **MissileCameraFeed**: `InfraredAutoEnabled`, ambient threshold/hysteresis, IR contrast / Material clip weights.
- Embedded UI shader AssetBundle applied as a `RawImage` material (HUD stays color). Missing shader → COLOR fallback.

## [0.27.3] — 2026-07-07

### Fixed

- Pink/magenta static and ground objects on missile camera feed: apply NO `ShaderGlobalManager` globals, terrain height window bake, and URP settings for manual feed render; restore main camera state after each frame.

## [0.27.2] — 2026-07-06

### Fixed

- Telemetry **R** (range to target): use missile guidance `aimPoint` instead of stale HQ radar track; fallback to target actual position.

## [0.27.1] - 2026-06-30
### Changed
- Documentation refresh: Developer header, badges, GitHub Alerts, Keywords, gitignore hygiene.

## [0.27.1] — 2026-06-29

### Documentation

- Full README refresh: complete Configuration Manager tables, Harmony hook list, loader choice, troubleshooting.
- Added `CHANGELOG.md`, updated `release/v0.27.1/INSTALL.txt`.
- Expanded `.cursorrules` with BepInEx mod workflow and NOLoader parity notes.

### Notes

- **No gameplay or binary changes** from v0.27.0 — documentation-only release.

## [0.27.0] — 2026-06-19

### Added

- Manual missile cycling (Next/Prev) with **sticky** selection and **cyclic wrap**.
- Session zoom controls with HUD offset readout.
- **BepInEx Configuration Manager** bindings (`MissileCameraBepInConfig.cs`) — replaces `mod_config.ini`.
- `MissileCameraControls` section for zoom step/limits (keybinds fixed in code).

### Changed

- Removed `mod_config.ini` from BepInEx build; settings in `com.at747.missilecamera.bepinex.cfg`.

## [0.26.1] — 2026-06-19

### Fixed

- Salvo counter across mixed weapon types and launch gaps.

## [0.26.0] — 2026-06-18

### Added

- Initial release: Harmony MFD hooks, mission-scene bootstrap, MFD split layout, seeker feed, tactical HUD.
