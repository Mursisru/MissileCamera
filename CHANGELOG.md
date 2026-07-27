# Changelog

## [2.2.0] — 2026-07-26

### Added

- **Fullscreen optical zoom:** mouse wheel magnification **1×…50×** (`fov = baseFov / mag`), middle-click reset to 1×. RT supersample buckets (up to 4K) + Bilinear when zoomed. Config: `Fullscreen.ZoomMax`, `ZoomWheelFactor`, `ZoomResetOnExit`.
- **Fullscreen vision cycle (J):** Color → NightVision → WhiteHot → BlackHot → WhiteContour → BlackContour. Stock-style NVG via local feed Volume only (never calls `NightVision.Toggle`). Contour modes use Sobel edge blit. MFD keeps auto WhiteHot-when-dark and keyboard zoom (`RightAlt+;` / `.`).
- **Fullscreen boot (~3.5s, first enter per mission):** feed tile reassembly → FLIR flicker + per-character calibration → hex line drum → telemetry value drum; center diagnostic stack ending with SUCCESSFUL badge.

### Changed

- **Versioning:** clean numeric semver only (`MAJOR.MINOR.PATCH`). Removed letter suffixes (`QV`, channels, `Build` tails). `AppVersion.DisplayVersion` matches `[BepInPlugin]` (`2.2.0`).
- **Hot-path performance (no quality cut):** FLIR text dirty-check + StringBuilder; TargetCam/RenderPrep FieldInfo caches; IR/vision Apply early-out; marker feed-camera per-frame cache; IR audit ReadPixels off by default; URP mirror dirty-skip.
- **Soft tick rates:** HUD snapshot / corner / dynamic / FS labels **10 Hz**; Cockpit PiP + AircraftCam + Telemetry `SmoothHz` defaults **10**; IR policy + config refresh **1 s**. Feed `RenderFps` stays **30**; fullscreen video still every frame.
- **Loss / switch / exit flash:** black screen with bordered **NO SIGNAL** (replaces TV static).
- **Documentation:** README aligned to Mursisru standards (`Developer: Mursisru`, badges, GitHub Alerts, Keywords).

### Fixed

- **Cockpit camera fly-out on fullscreen exit:** fullscreen never writes `CameraStateManager` / `cameraPivot` / `mainCamera` (`Fullscreen/CAMERA_SAFETY.md`). Feed is RT → `RawImage` only.
- **Fullscreen markers stuck at center:** CombatHUD contacts reprojected via seeker feed `WorldToViewportPoint` → Overlay screen.
- **Bootstrap hid the feed:** first-enter staging no longer forces `MissileCameraFeed` alpha to 0.

## [2.32.26] — 2026-07-26

### Changed

- **Loss / switch / exit flash:** TV static replaced by black screen with bordered **NO SIGNAL** label (fullscreen Overlay + MFD cover). Same triggers and duration (`PostLossInterferenceSeconds`).

## [2.32.25] — 2026-07-26

### Fixed

- **Fullscreen markers stuck at screen center:** after vanilla `HUDUnitMarker.UpdatePosition` (cockpit `mainCamera`), reproject contacts through the seeker feed via `WorldToViewportPoint` → Overlay screen. Does not move `CameraStateManager`. Selected-target edge pin uses the same feed mapping.

## [2.32.24] — 2026-07-26

### Fixed

- **Cockpit camera fly-out on fullscreen exit (root cause):** fullscreen no longer touches `CameraStateManager` / `cameraPivot` / `mainCamera` at all. Video is dedicated feed RT → `RawImage` only (`Fullscreen/CAMERA_SAFETY.md`).
- **Bootstrap hid the feed:** first-enter staging no longer forces `MissileCameraFeed` alpha to 0 (legacy path when vanilla mainCamera was the view).
- **Opaque fullscreen backdrop** under the RawImage so the cockpit is fully covered without hijacking the camera.

### Changed

- `MissileCameraFullscreenViewDriver` remains a permanent no-op guard; enter/exit no longer call Snap/pose helpers.

## [2.32.23] — 2026-07-26

### Changed

- **Fullscreen TV static (0.5s):** dedicated Overlay noise layer (sorting 200). Plays on missile camera switch, on destroy, and on fullscreen exit when no missiles remain (then auto-closes fullscreen).

## [2.32.22] — 2026-07-26

### Fixed

- **Impact / exit HUD wipe:** stop `ForceOff` without restore on ILS elements; restore FlightHud visuals on exit.
- **Post-hit interference:** do not tear down fullscreen on missile loss — keep overlay, enable RawImage for TV static, then exit after sequence.
- **Camera after impact/exit:** always `SnapToCockpit` when pose overlay stops.

## [2.32.21] — 2026-07-26

### Fixed

- **Fullscreen camera exit (audit):** removed `cameraPivot.SetParent(missile)` and Harmony block of `UpdateState` (root cause of sticky missile view). Fullscreen only overwrites world pose after vanilla cockpit LateUpdate; Exit stops the overlay. Toggle runs in that LateUpdate before pose write. Missile loss auto-exits fullscreen.

## [2.32.20] — 2026-07-26

### Fixed

- **Fullscreen exit camera freeze:** toggle is handled before missile pose Tick (same-frame re-parent race); Exit calls `SwitchState(cockpitState)` + hard `SnapToCockpit` and keeps snapping for 8 LateUpdate frames.

## [2.32.19] — 2026-07-26

### Fixed

- **ILS leftovers on fullscreen:** every LateUpdate suppresses `FlightHud.velocityVector` / HUDCenter (Update re-enables them), disables `ObjectiveOverlayManager` + mission pointers under `iconLayer`, and hides `targetDesignator` Image (GO kept for TargetSelect).

## [2.32.18] — 2026-07-26

### Fixed

- **Fullscreen exit camera freeze:** unblock hijack first, then rebind `cameraPivot` to `cockpitViewPoint` like vanilla `CameraCockpitState.EnterState`.
- **Markers only:** hide HMD/ILS, HUDAppManager, weapon-state chrome and trim all non-marker branches under the elevated CombatHUD canvas; keep only `iconLayer` + `targetDesignator` (+ target arrow).

## [2.32.17] — 2026-07-26

### Fixed

- **Markers survive fullscreen toggle:** no longer reparent/destroy `CombatHUD.iconLayer` (that permanently broke markers after exit). Canvas is elevated to Overlay sorting 120 instead.
- **Target select:** keep vanilla `targetDesignator` active — dump `TargetSelect` ranges markers against it.
- **FLIR overlay:** `blocksRaycasts=false` so Select input is not eaten by the fullscreen chrome.

## [2.32.16] — 2026-07-26

### Fixed

- **Stub labels:** deep-find kill for `MissileCameraTitle` / `COLOR` / telemetry; fullscreen always forces stubs off.
- **Markers (dump audit):** `CombatHUD.UpdateMarkers` forced after camera LateTick; `iconLayer` reparent uses screen-overlay reset (`localScale=1`); soft-hide FlightHud when CombatHUD shares its canvas so LateUpdate keeps running.

## [2.32.15] — 2026-07-26

### Fixed

- **Fullscreen stub labels:** bootstrap no longer fades in `MissileCameraTitle` / `COLOR` / `A:---/R:---/S:---` — stubs stay disabled every tick.
- **Markers under FLIR overlay:** `CombatHUD.iconLayer` reparented to Overlay sorting **120** (above FLIR chrome). Projection remains vanilla `mainCamera` (missile nose).

## [2.32.14] — 2026-07-26

### Changed

- **Fullscreen = vanilla main camera:** drives `CameraStateManager` pivot to the missile nose (same idea as cockpit cam). No RT/RawImage as the game view. CombatHUD markers use native `WorldToScreenPoint` again (fixes center-stuck markers).
- **HUD:** FlightHud / minimap / CombatHUD chrome hidden; only unit markers remain. FLIR labels stay as a transparent overlay.

## [2.32.13] — 2026-07-26

### Fixed

- **Fullscreen HUD clutter:** only **CombatHUD unit markers** (`iconLayer` + target arrow) are shown. FlightHud (ILS/compass), DynamicMap minimap, and the rest of CombatHUD stay off / under the feed. Markers are reparented onto a dedicated Overlay host.

## [2.32.12] — 2026-07-26

### Fixed

- **Fullscreen vanilla markers invisible:** feed Overlay covered ScreenSpace-Camera CombatHUD; `WorldToScreenPoint` on the RT feed camera returned RT pixels, not Screen. Markers now project via `WorldToViewportPoint` → Screen, and the CombatHUD canvas is forced to **ScreenSpaceOverlay** above the feed. FlightHud instruments are hidden without killing the HUD canvas.

## [2.32.11] — 2026-07-26

### Changed

- **Fullscreen target markers:** removed the custom marker pool. Fullscreen now uses **vanilla CombatHUD / HUDUnitMarker** contacts, projected through the missile feed camera (map/datalink set unchanged).
- **FlightHud instruments** (compass/pitch) are hidden in fullscreen — wrong POV; CombatHUD unit icons stay on top of the feed.

### Removed

- `Markers/*`, `UnitRegistryAccess`, `MissileCameraTargetMarker`, and the `MissileCameraMarkers` config section.

## [2.32.10] — 2026-07-14

### Fixed

- **Fullscreen would not open:** yield on `menuCanvas.enabled` was a false positive — canvas stayed disabled while the panel was already reparented off the MFD. Yield now only on **pause** / **maximized map**, and defers via **Exit()** (panel restored) instead of hiding the host canvas.
- **Fullscreen toggle** no longer depends on `Controls.Enabled`.
- **Bootstrap:** `ApplyFullVisibility` restores feed/title alphas after abort/complete.

## [2.32.9] — 2026-07-14

### Fixed

- **Markers audit:** ambient units follow **DynamicMap parity** — HQ from `DynamicMap.HQ`; allies = same faction; hostiles = `trackingDatabase` only (map contacts). Dropped over-strict `IsTargetBeingTracked` (≤4s) that hid all non-target units.
- **Fullscreen vs pause/map:** while `GameplayUI.GameIsPaused`, `menuCanvas.enabled`, or `DynamicMap.mapMaximized`, the fullscreen feed canvas is disabled so pause menu / map render and receive input on top.

## [2.32.8] — 2026-07-14

### Fixed

- **Anti-cheat (strict):** ambient hostiles require `FactionHQ.IsTargetBeingTracked` (active spot ≤ 4s). Stale `trackingDatabase` entries no longer wallhack.
- **Fullscreen vs vanilla UI:** feed canvas sortingOrder lowered (40) and GraphicRaycaster removed so stock CombatHUD / menus can draw and receive input on top.

### Added

- **Target motion vector** line on markers (1.5s lead from unit velocity).
- **Inbound missile markers:** missiles whose `targetID` is the seeker blink red ↔ yellow.

## [2.32.7] — 2026-07-14

### Fixed

- **Anti-cheat ambient markers:** only contacts known to own `FactionHQ` (`TryGetKnownPosition` / trackingDatabase). Undetected hostiles are hidden. Marker pose uses **last-known** position, not live GPS.

## [2.32.6] — 2026-07-14

### Fixed

- **PIT/HDG captions no longer clipped** — dials and labels sit in a raised safe band (center-anchored under the rings).

### Changed

- **FLIR telemetry split:** left `— MSL —` (name/grid/SPD/HDG/ALT/G/MACH/FUEL/PLAT/salvo), right `— TGT —` (name/grid/SPD/HDG/ALT/REL/SLT/CLOS/TTI/LRF/RID/ANG).
- **Ambient unit markers:** translucent unlabeled Ally/Threat diamonds for every `UnitRegistry` unit (skip seeker + locked target); config `ShowSceneUnits` / `SceneUnitAlpha` (default on, alpha 0.4); pool default 48.

## [2.32.5] — 2026-07-14

### Fixed

- **FLIR gimbal dials labeled:** bottom rings show `PIT` / `HDG` titles with live degrees (two-line captions).

### Changed

- **Markers colored by type** (no more forced FLIR-green override): Target cyan diamond, Aim amber box + `AIM` label; Threat/Ally/Waypoint/Jam keep distinct RGBA + silhouettes. Default Aim color no longer blends with HUD green.

## [2.32.4] — 2026-07-14

### Fixed

- **FLIR heading tape:** ticks, N/E/S/W and degree marks scroll together on one px/deg scale (no more fixed ticks with jumping labels).
- **Smooth compass motion:** `SmoothDampAngle` on continuous missile yaw (no teleports from rounded `F0` heading text).

## [2.32.3] — 2026-07-14

### Changed

- **FLIR chrome restored as live telemetry** (not removed): `FLIR SYSTEMS` channel/salvo, map **GRID** (missile + target via `DynamicMap.gridLabels`), **LRF** slant range, rotating **`-N->`**, **HDIR** heading, **FOC AUTO/MAN** from zoom offset, **EXP** from IR exposure / DAY, **IP-RA** aim LOS + REL, **INS NAV** off-boresight, **TRK COR** closing, **SLAVE** lock state, MAG/RID, W–N TGT BRG strip.

## [2.32.2] — 2026-07-14

### Fixed

- **FLIR HUD: no decorative stubs.** Removed fake GPS, LRF, FOC/EXP, HDIR, IP-RA, TRK COR, SLAVE, `-N->`. Every label is live seeker telemetry.
- **Useful readouts:** missile + platform IDs, missile SPD/HDG/ALT, POL/FOV/MAG, LOCK+guidance+RID, ANG/TTI/G/FUEL, target SPD/HDG/REL/SLT/CLOS, TGT BRG Δ strip, PIT/HDG dials, compass 10° tape.
- **MAG fixed** to `baseFov / currentFov` (was `10/FOV` → bogus `x0.2`).
- **Fullscreen FLIR updates every frame** in `Update()` (snapshot + labels + markers uncapped).

## [2.32.1] — 2026-07-14

### Fixed

- **Sticky WhiteHot / COLOR bleed:** feed never inherits TargetCam postFX; IR uses local Volume + `volumeTrigger` only when policy is ON (never global).
- **Auto-IR too aggressive:** default daylight/ambient thresholds (`0.12` / `0.06`) so daytime stays COLOR.
- **FLIR HUD incomplete chrome:** open-center crosshair, compass ticks + caret, W–N azimuth slider, gimbal dial rings, LRF/HDIR/`C WH DDE|C COLOR`/FOC/EXP, MAG/RID, DMS coords, target SPD/HDG/ELV/SLT, SLAVE status; green diamond target markers on FLIR.

## [2.32.0] — 2026-07-14

### Added

- **Fullscreen FLIR HUD** (green sensor chrome): compass tape, ownship/target blocks, mode stack, status — **fullscreen only**.
- **Fullscreen feed camera is URP pipeline-driven** (enabled camera + `targetTexture`), mirroring TargetCam/main URP settings (MSAA/AA/postFX from the game). No manual AA disable overrides.
- **Fullscreen render uncapped** (every frame). MFD keeps throttled `RenderFps`.

### Changed

- **MFD HUD restored to Classic** (S/A/R corners). TGP/FLIR overlays do not apply on MFD.
- Fullscreen IR uses URP Volume ColorAdjustments (TargetCam-style) instead of the HDR blit path.

## [2.31.0] — 2026-07-14

### Added

- **TGP-style sensor HUD** (default `MissileCameraHud.Style = Tgp`):
  - Top-left: ownship name (red) + `RNG` / `ALT` / `SPD`
  - Top-right: `HDG` / `REL` / `CLOS`
  - Center: circular reticle + crosshair, diamond target marker with name, **Time-To-Impact** bar
  - Bottom-left: cockpit PiP (`TOR: Cockpit View`) + `RID` / `MAG`
  - Bottom-right: `MODE: AUTO IR` / `PALETTE: WhiteHot`
- Config: `Style` (`Tgp` | `Classic`), `CockpitPipEnabled`, `CockpitPipFps`, `OwnshipNameColor`

### Changed

- Default reticle color is white (TGP look). Classic HUD remains available via `Style = Classic`.

## [2.30.3] — 2026-07-14

### Changed

- **Fullscreen feed resolution is configurable:** `MissileCameraFullscreen.FeedWidth` / `FeedHeight` (default **1920×1080**), independent of MFD `FeedWidth`/`FeedHeight` (512).

## [2.30.2] — 2026-07-14

### Fixed

- **Fullscreen feed sharpness:** render resolution matches the game viewport (capped), not the 512 MFD RT upscaled.
- **Fullscreen HUD:** compact corner chips (R/A bottom-left, S bottom-right) with fixed small fonts — no full-width giant telemetry bar.

## [2.30.1] — 2026-07-14

### Fixed

- **Classic MFD HUD telemetry** restored to 1.30.1 style (S / A / R only). Extended G/fuel/guidance/Mach/angle are not drawn on the standard corner HUD.
- **Fullscreen = full game viewport:** Screen Space Overlay canvas over the entire game window — not Tac/MFD-only stretch.

## [2.30.0] — 2026-07-14

### Added

- **Fullscreen missile feed** (`MissileCameraFullscreen`): RightAlt+F toggle, first-enter-per-mission bootstrap (abort-safe), state retained (missile/zoom/IR).
- **Extended telemetry** (`MissileCameraTelemetry`): G, fuel, guidance (GUIDED/BALLISTIC/LOST LOCK), Mach, target range/angle — smoothed; HudSnapshot is SSOT for markers.
- **Post-FX stack** (`MissileCameraEffects`): modular scanlines / motion blur / chromatic / bloom (bundle shaders); missing shaders warn at mission start and stay inactive.
- **Pooled markers** (`MissileCameraMarkers`): typed Target/Aim (+ reserved types), cfg `MaxMarkers` (default 16), snapshot-driven primary markers.
- **Aircraft mini-cam** (`MissileCameraAircraftCam`, default off): Rear/TopDown/Chase; no-op when DisplayMode=skip; shared RenderPrep once per multi-render frame.

### Architecture

- `MissileCameraControlSlot` reserved for future steering (inactive).
- FieldInfo caches warmed at mission start; mission unload resets fullscreen bootstrap flag.

### Changed

- Version line renumbered to **2.30.0QV** (feature expansion release).

## [1.30.1] — 2026-07-14

### Added

- **Post-loss TV static:** After the last followed missile is destroyed, the MFD feed shows a short interference burst for **0.4 s** (config `PostLossInterferenceSeconds`, `0` = off), then the overlay closes.

## [1.30.0] — 2026-07-14

### Changed

- **Missile camera no longer needs a selected target.** Launching / tracking any owned in-flight missile activates the MFD feed even if TargetCam never opened (no lock). Layout stays up when vanilla TargetCam disables after cancel / empty target list / landing cam switch while a missile is still trackable.

## [1.29.3] — 2026-07-14

### Removed

- **BlackHot / `InfraredPalette`.** IR feed is WhiteHot only. Remove `InfraredPalette` from your cfg if present.

## [1.29.2] — 2026-07-14

### Fixed

- **BlackHot = exact invert of WhiteHot:** two-pass blit — identical WhiteHot LDR, then `1 - luma` only. Removed exposure/compress hacks that still washed the night frame.

## [1.29.1] — 2026-07-14

### Fixed

- **BlackHot washed white:** night IR frames are mostly dark; plain `1 - lum` flipped them to solid white. BlackHot now uses reduced exposure, soft bright-end compress after invert, and mild gain so cold terrain stays readable gray and hot areas stay dark.

## [1.29.0] — 2026-07-14

### Added

- **WhiteHot / BlackHot IR polarity** via config `InfraredPalette` (default **WhiteHot**). BlackHot inverts grayscale after TargetCam exposure math (dark = hot).

> [!NOTE]
> **Polarity** applies only while auto-IR is active. COLOR mode is unchanged.

## [1.28.0] — 2026-07-14

### Changed

- **Auto IR lighting-only:** IR no longer uses the fixed 6–18 clock window. IR turns on from **GetDaylightFactor** (night / thick clouds at the missile) **or** low **GetAmbientLight**, with hysteresis. Clear midday stays COLOR.

### Config

- Replaced `InfraredDarkAmbientThreshold` / `InfraredDarkAmbientHysteresis` with `InfraredDaylightThreshold` (default **0.35**), `InfraredAmbientThreshold` (default **0.12**), `InfraredLightHysteresis` (default **0.03**). Remove the old keys from your cfg if present.

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


All notable changes to **MissileCamera** (BepInEx) are documented here. Public version is clean semver in `AppVersion.ReleaseBase` / `DisplayVersion` / `[BepInPlugin]` (identical strings, no letter suffixes).

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
