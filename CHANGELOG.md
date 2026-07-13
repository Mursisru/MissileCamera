# Changelog

## [0.27.9] — 2026-07-13

### Fixed

- **IR too dark again:** URP Volume on manual `Camera.Render` does not apply ColorAdjustments reliably. Restored **HDR render + guaranteed blit** with exact TargetCam `postExposure` (live sync when `MODE: IR`). Removed bright-light EV penalty and default −1.25 bias (both crushed the whole feed). Soft highlight compress (`k=0.35`) prevents plume blowout without darkening midtones. Dropped double-gamma on blit output.

## [0.27.8] — 2026-07-13

### Fixed

- **IR overexposure vs TargetCam:** dropped HDR+blit `exp2` path (blew out missile plume). IR now uses **URP ColorAdjustments + tonemap on LDR** like TargetCam. `MirrorUrpFromMain` no longer forces `allowHDR` during IR. Added bright-light penalty (vanilla `ExposureController` logic) and `InfraredMissileExposureBias` (default −1.25 EV).

### Added

- Full IR audit: exposure breakdown (`policy`, `vanilla`, `brightPenalty`, `missileBias`, `final`), RT luma min/avg/max readback, pre/post render logs.

## [0.27.7] — 2026-07-13

### Fixed

- **IR too bright vs TargetCam:** removed double exposure (URP Volume + blit). IR is now **one pass** — raw HDR scene render, then blit with Rec.709 grayscale + single `postExposure` / `contrast` matching live TargetCam `ColorAdjustments` when `MODE: IR`.

## [0.27.6] — 2026-07-13

### Fixed

- **Weak / missing IR (ЧБИК):** feed camera now renders to **HDR** (`ARGBHalf`) in IR mode, then **guaranteed post-blit** (`Hidden/MissileCamera/InfraredBlit`) to LDR display RT with linear grayscale + `exp2(exposure)` — same strength as TargetCam MFD IR. URP Volume kept as extra pass during HDR render.

### Added

- `MissileCameraInfraredBlit` + embedded blit shader in AssetBundle.
- Audit logs: `blit=true`, `blitExp`, `allowHDR`.

## [0.27.5] — 2026-07-13

### Fixed

- **IR (ЧБИК) not visible on missile feed:** removed broken RawImage material path that disabled URP Volume during render. IR now matches vanilla TargetCam — `ColorAdjustments` (saturation −100, postExposure, contrast 1) applied on the feed camera at `RenderFrame` time.
- Fog during IR render follows TargetCam (`fog` off in IR, restored after frame).
- Live sync from local `TargetCam` ColorAdjustments when vanilla MFD is already in IR mode.

### Added

- `MissileCameraInfraredAudit` rate-limited pipeline logs (`IR audit`, `IR render`) for in-game verification.

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


All notable changes to **MissileCamera** (BepInEx) are documented here. Release semver in `AppVersion.ReleaseBase` / `[BepInPlugin]`; engine build string in `DisplayVersion`.

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

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
