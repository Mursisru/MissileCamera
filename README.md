# MissileCamera (Nuclear Option Mod)

[![Nuclear Option](https://img.shields.io/badge/Game-Nuclear%20Option-blue)](https://store.steampowered.com/app/2168680/Nuclear_Option/)
[![BepInEx 5](https://img.shields.io/badge/Loader-BepInEx%205-orange)](https://docs.bepinex.dev/)
[![Version](https://img.shields.io/badge/Version-2.2.0-green)](https://github.com/Mursisru/MissileCamera/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow)](https://github.com/Mursisru/MissileCamera/blob/BepInExVersion/LICENSE)

---

## Critical warnings
> [!IMPORTANT]
> **BepInEx 5 (x64) required** - install [BepInEx](https://docs.bepinex.dev/articles/user_guide/installation/index.html) before this mod.

> [!NOTE]
> **Fullscreen never touches the cockpit camera** - seeker video is a dedicated RenderTexture on a UI `RawImage`. See `Fullscreen/CAMERA_SAFETY.md` in the source tree.

> [!WARNING]
> - **Third-party aircraft and MFD mods may break layout** - custom `TargetScreenUI` / tactical overlays can conflict; set `DisplayMode=skip` in Configuration Manager or disable conflicting MFD mods.

> [!TIP]
> **Configuration Manager recommended** - in-game UI for `com.at747.missilecamera.bepinex.cfg`. After game updates, delete `BepInEx\cache\harmony_interop_cache.dat` if patches behave oddly.

BepInEx 5 plugin for the flight sim **Nuclear Option** that adds a live seeker-eye view (Missile Nose Cam) and a tactical HUD overlay directly onto your cockpit MFD Target display.

**Plugin GUID:** `com.at747.missilecamera.bepinex`

---

## Table of contents

- [Critical warnings](#critical-warnings)
* [Features](#features)
* [Requirements](#requirements)
* [Player installation](#player-installation)
* [Controls & keybinds](#controls--keybinds)
* [Configuration (BepInEx Configuration Manager)](#configuration-bepinex-configuration-manager)
* [Project layout](#project-layout)
* [Changelog](#changelog)
* [Licence](#licence)

## Features

* **MFD split-screen UI:** Splits the wide tactical MFD (Target view) into zones and embeds the missile feed in the weapons panel area.
* **Seeker cam (missile nose cam):** Renders a live `RawImage` feed from your latest **player-owned** in-flight missile. **No selected target required** — dumb-fire / no-lock launches still open the MFD feed. On destruction, a brief TV-static burst plays before the panel closes.
* **Fullscreen feed:** `RightAlt+F` — dedicated seeker RenderTexture on a fullscreen `RawImage` (same as MFD). **Never hijacks** vanilla `CameraStateManager` / cockpit camera. First enter per mission plays a ~3.5s boot (tile assemble → symbol cal → hex/value drums + diagnostics). FLIR chrome overlay; CombatHUD unit markers only.
* **Fullscreen zoom / filters:** mouse wheel optical zoom up to **50×** (MMB reset); **J** cycles vision modes (Color / NVG / WhiteHot / BlackHot / Contour±). MFD keeps keyboard zoom + auto IR when dark.
* **Fullscreen FLIR HUD:** green sensor chrome with live `— MSL —` / `— TGT —` telemetry, scrolling compass, dials; **vanilla CombatHUD target markers** — **fullscreen only**. MFD keeps the classic S/A/R corner HUD.
* **Classic MFD HUD:** S/A/R corners + salvo (1.30.1 style).
* **Post-FX:** optional scanlines / motion blur / chromatic / bloom (`MissileCameraEffects`) — inactive with a startup warning if shaders are missing from the embedded bundle.
* **Aircraft mini-cam:** optional second feed (`MissileCameraAircraftCam`, default off). **No-op when `DisplayMode=skip`.**
* **Auto B/W IR (WhiteHot):** When dark at the missile — low `GetDaylightFactor` (night / under thick clouds) or very low `GetAmbientLight` — the feed switches to black-and-white IR. Not a fixed clock window. Disable with `InfraredAutoEnabled=false`.
* **Manual feed controls:** Cycle in-flight owned missiles and adjust camera zoom while the MFD overlay is active (see **Controls** below).
* **Per-aircraft layout (`DisplayMode=auto`):**
  * **Dedicated split** (e.g. KR-67): wide target cam on the left, missile panel on the right.
  * **VT-7 Vagrant:** MissileCamera replaces the right-column **NOZZLE + ENGINE** block (weapons silhouette kept).
  * **Small tac overlay** (e.g. Cricket): mod **skipped** — vanilla tactical MFD unchanged.
* **Mission-only bootstrap:** Harmony patches and the feed driver attach on the **first mission scene**, not in the main menu.

---

## Requirements

* **Nuclear Option** ([Steam](https://store.steampowered.com/app/2168680/Nuclear_Option/)).
* **BepInEx 5** (x64) in the game root (`BepInEx\core\` must exist for **build** reference paths).
* **[Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager)** (recommended) — in-game UI for plugin settings.

---

## Player installation

1. Install [BepInEx 5](https://github.com/bepinex/bepinex) for Nuclear Option.
2. Install [Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager) (recommended).
3. Copy into:

   ```text
   Nuclear Option\BepInEx\plugins\MissileCamera\
   ```

   * `MissileCamera.dll`

4. Settings are stored in `BepInEx\config\com.at747.missilecamera.bepinex.cfg` (auto-created on first run). Edit in-game via **Configuration Manager**, or edit the `.cfg` file while the game is closed.

5. Do **not** rename the plugin folder or install duplicate copies under different names.

6. See `release/v0.27.1/INSTALL.txt` or [GitHub Releases](https://github.com/Mursisru/MissileCamera/releases) for a checklist.

---

## Controls & keybinds

Active only while the missile feed overlay is on and you have **player-owned** in-flight missiles. **US English keyboard layout** (Right Alt may act as AltGr on some EU keyboards). Keybinds are configurable in **MissileCameraControls** / **MissileCameraFullscreen** via Configuration Manager (`KeyboardShortcut`).

| Keybind | Default | Action |
| :--- | :--- | :--- |
| **NextMissile** | Right Alt + `/` | Next missile (newer; wraps) |
| **PreviousMissile** | Right Alt + `,` | Previous missile (older; wraps) |
| **ZoomIn** | Right Alt + `;` | MFD zoom in (narrower FOV) |
| **ZoomOut** | Right Alt + `.` | MFD zoom out (wider FOV) |
| **ResetZoom** | Right Shift + `.` | MFD reset zoom offset to `0.0` |
| **Mouse wheel** | — | **Fullscreen only:** optical zoom **1×…50×** |
| **ZoomResetKey** | Middle mouse | **Fullscreen only:** reset magnification to **1×** |
| **ToggleKey** + **RequireRightAlt** | Right Alt + `F` | Fullscreen toggle (**MissileCameraFullscreen**) |
| **VisionCycleKey** | `J` | Fullscreen vision cycle (**MissileCameraFullscreen**) |

**Vision cycle (fullscreen):** Color → NightVision → WhiteHot → BlackHot → WhiteContour → BlackContour. NightVision uses a local feed Volume only — never toggles stock cockpit NVG.

**Sticky selection:** after Next/Prev the camera stays on your chosen missile when new ones launch. If it is destroyed, the feed falls back to the newest remaining missile.

**Zoom HUD (MFD):** each zoom change shows the current **offset** for **0.5 s** above feed center. Fullscreen FLIR shows `MAG xN` from optical magnification.

---

## Configuration (BepInEx Configuration Manager)

All settings are exposed through **BepInEx.Configuration** (`Config.Bind` in `Config/MissileCameraBepInConfig.cs`). Use [Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager) in-game, or edit:

```text
BepInEx\config\com.at747.missilecamera.bepinex.cfg
```

### Layout

| Key | Default | Description |
| :--- | :---: | :--- |
| `Enabled` | `true` | Turns MFD layout splitting on/off |
| `DisplayMode` | `split` | Changes which aircraft get the missile panel: `auto` \| `skip` \| `split` |
| `OverlayMaxWidth` | `0.45` | Changes small-tac detection max normalized width |
| `LeftWidth` | `0.58` | Changes MFD split: target cam column width (0–1) |
| `MissilePanelBottom` | `0.38` | Changes MFD split: bottom edge of missile feed zone |
| `WeaponsStripHeight` | `0.12` | Changes MFD split: compressed weapons strip height |
| `ShowDivider` | `true` | Changes MFD split: zone divider lines |
| `DebugStub` | `false` | Dev: bright magenta test panel |
| `StubLabel` | `MISSILE CAMERA` | Dev: label on debug stub |

### MissileCameraFeed

| Key | Default | Description |
| :--- | :---: | :--- |
| `Enabled` | `true` | Turns live missile nose camera on MFD on/off |
| `NoseSkinInset` | `0.08` | Changes seeker camera distance outside nose mesh (m) |
| `CameraBackOffset` | `0.35` | Changes seeker camera pull-back from nose point (m) |
| `Fov` | `60` | Changes seeker base FOV before MFD zoom (degrees) |
| `FeedWidth` | `512` | Changes MFD feed render texture width |
| `FeedHeight` | `512` | Changes MFD feed render texture height |
| `HorizonLock` | `true` | Changes seeker roll lock to world up |
| `TurnLookBankScale` | `1` | [Advanced] Changes G-load camera sway strength |
| `MaxTurnLookDegrees` | `90` | [Advanced] Changes max G-load camera offset (degrees) |
| `DefaultMissileGLimit` | `20` | [Advanced] Fallback G limit |
| `TurnLookGDeadband` | `0.15` | [Advanced] G deadband |
| `TurnLookGFilterHz` | `7` | [Advanced] G filter cutoff (Hz) |
| `TurnLookSlewDegPerSec` | `120` | [Advanced] Turn-look slew rate (deg/s) |
| `TurnLookSmoothTime` | `0.18` | [Advanced] Turn-look smoothing (seconds) |
| `PostExplosionHoldSeconds` | `0` | Changes post-loss freeze of last frame (0 = off) |
| `PostLossInterferenceSeconds` | `0.5` | Changes NO SIGNAL flash length on switch/destroy/FS exit (0 = off) |
| `RenderFps` | `30` | Changes MFD feed render rate (Hz) |
| `InfraredAutoEnabled` | `true` | Changes MFD auto B/W IR from lighting at missile |
| `InfraredDaylightThreshold` | `0.12` | Changes IR ON threshold for daylight factor |
| `InfraredAmbientThreshold` | `0.06` | Changes IR ON threshold for ambient light |
| `InfraredLightHysteresis` | `0.03` | Changes anti-flicker margin before IR turns off |
| `InfraredContrast` | `1` | Changes MFD IR contrast |
| `InfraredBlackPoint` | `0.05` | Changes MFD IR black clip |
| `InfraredWhitePoint` | `0.95` | Changes MFD IR white clip |
| `InfraredRedWeight` | `0.55` | Changes MFD IR red luminance weight |
| `InfraredExposureBiasEv` | `0` | Changes MFD IR exposure vs TargetCam (0 = match) |

### MissileCameraHud

| Key | Default | Description |
| :--- | :---: | :--- |
| `Enabled` | `true` | Turns MFD HUD overlay (classic S/A/R) on/off |
| `SalvoWindowSeconds` | `0.5` | Changes salvo label grouping window (seconds) |
| `ShowCenterCluster` | `true` | Changes center reticle and intercept ring |
| `ShowTargetMarker` | `true` | Changes target diamond marker |
| `CockpitPipEnabled` | `true` | Changes bottom-left cockpit PiP |
| `CockpitPipFps` | `10` | Changes cockpit PiP render rate (Hz) |
| `InterceptColor` | `0,1,0,1` | Changes intercept ring RGBA (0–1) |
| `ReticleColor` | `1,1,1,1` | Changes reticle RGBA |
| `HorizonColor` | `0.05,0.35,0.08,1` | Changes horizon fill RGBA |
| `HorizonOutlineColor` | `0.2,1,0.25,1` | Changes horizon outline RGBA |
| `MissileNameColor` | `1,0,1,1` | Changes missile name label RGBA |
| `OwnshipNameColor` | `1,0.15,0.15,1` | Changes ownship name RGBA |
| `TargetNameColor` | `0.4,0.9,1,1` | Changes target name RGBA |
| `LabelBackgroundColor` | `0.18,0.18,0.18,0.62` | Changes label backdrop RGBA |
| `LabelBackgroundAlpha` | `0.62` | Changes backdrop opacity |

### MissileCameraControls

| Key | Default | Description |
| :--- | :---: | :--- |
| `Enabled` | `true` | Turns keyboard missile cycling and MFD zoom on/off |
| `ZoomStep` | `0.5` | Changes MFD zoom offset step per key press |
| `ZoomMin` | `-4` | Changes MFD zoom minimum offset |
| `ZoomMax` | `4` | Changes MFD zoom maximum offset |
| `ZoomFovDegreesPerUnit` | `5` | Changes FOV delta (degrees) per offset unit |
| `IndicatorSeconds` | `0.5` | Changes zoom HUD readout duration (seconds) |
| `NextMissile` | RightAlt + `/` | Changes keybind: next missile (`KeyboardShortcut`) |
| `PreviousMissile` | RightAlt + `,` | Changes keybind: previous missile (`KeyboardShortcut`) |
| `ZoomIn` | RightAlt + `;` | Changes keybind: MFD zoom in (`KeyboardShortcut`) |
| `ZoomOut` | RightAlt + `.` | Changes keybind: MFD zoom out (`KeyboardShortcut`) |
| `ResetZoom` | RightShift + `.` | Changes keybind: MFD zoom reset (`KeyboardShortcut`) |

### MissileCameraFullscreen

| Key | Default | Description |
| :--- | :---: | :--- |
| `Enabled` | `true` | Turns fullscreen missile feed on/off |
| `ToggleKey` | `F` | Changes fullscreen toggle KeyCode (with RequireRightAlt) |
| `RequireRightAlt` | `true` | Changes requirement for RightAlt with ToggleKey |
| `FeedWidth` | `1920` | Changes fullscreen render texture width |
| `FeedHeight` | `1080` | Changes fullscreen render texture height |
| `ZoomMax` | `50` | Changes max optical magnification (mouse wheel) |
| `ZoomWheelFactor` | `1.12` | Changes zoom multiply per wheel notch |
| `VisionCycleKey` | `J` | Changes key to cycle Color / NVG / IR / Contour modes |
| `ZoomResetOnExit` | `true` | Changes reset to 1× magnification when leaving fullscreen |
| `PitchLadderEnabled` | `true` | Changes stock pitch ladder on fullscreen FLIR |
| `PitchLadderTint` | `0.55,1,0.9,1` | Changes pitch ladder RGBA tint |
| `PitchLadderIntensity` | `3.2` | Changes pitch ladder brightness |
| `ZoomResetKey` | Middle mouse | Changes fullscreen zoom reset (`KeyboardShortcut`) |

### MissileCameraTelemetry

| Key | Default | Description |
| :--- | :---: | :--- |
| `SmoothHz` | `10` | Changes telemetry smoothing rate (Hz) |

### MissileCameraEffects

| Key | Default | Description |
| :--- | :---: | :--- |
| `ScanlinesEnabled` | `false` | Changes scanlines post-FX (requires shader bundle) |
| `ScanlinesIntensity` | `0.35` | Changes scanlines strength (0–1) |
| `MotionBlurEnabled` | `false` | Changes motion blur post-FX |
| `MotionBlurIntensity` | `0.25` | Changes motion blur strength (0–1) |
| `ChromaticEnabled` | `false` | Changes chromatic aberration post-FX |
| `ChromaticIntensity` | `0.2` | Changes chromatic strength (0–1) |
| `BloomEnabled` | `false` | Changes bloom post-FX |
| `BloomIntensity` | `0.3` | Changes bloom strength (0–1) |

---

## Project layout

```text
MissileCamera/
├── MissileCamera.csproj
├── MissileCamera.sln
├── MissileCameraPlugin.cs      # BepInPlugin entry, Config.Bind
├── MissileCameraHost.cs        # DDOL host, mission-scene bootstrap
├── AppVersion.cs
├── CHANGELOG.md
├── Harmony/                    # Harmony patches + hooks
├── Camera/                     # Feed rig, config, controller
├── Hud/                        # Overlay widgets
├── Layout/                     # MFD zone split
├── Config/                     # BepInEx config bindings, paths
├── Access/                     # Game API wrappers
├── Ui/                         # HUD graphics helpers
├── Logging/
├── release/
│   └── v0.27.1/
│       └── INSTALL.txt
└── 
```

---

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

---

## Licence

MIT License — see [LICENSE](LICENSE).
