# Changelog

## [0.27.4] — 2026-07-27

### Added

- VT-7 Vagrant (`VTOLTrainer1`) MFD support: replace NOZZLE+ENGINE section while preserving weapons silhouette.
- `MissileCameraControls` keybinds are now configurable via BepInEx config (`ModifierKey`, `NextMissileKey`, `PreviousMissileKey`, `ZoomInKey`, `ZoomOutKey`, `ResetZoomModifierKey`, `ResetZoomKey`).

### Fixed

- Keep missile-camera overlay active through early Tac initialization and disabled TargetCam states while owned missiles are still in flight.

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
