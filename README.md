# MissileCamera (Nuclear Option Mod)

[![Nuclear Option](https://img.shields.io/badge/Game-Nuclear%20Option-blue)](https://store.steampowered.com/app/2168680/Nuclear_Option/)
[![BepInEx 5](https://img.shields.io/badge/Loader-BepInEx%205-orange)](https://docs.bepinex.dev/)
[![Version](https://img.shields.io/badge/Version-1.9.9-green)](https://github.com/Mursisru/MissileCamera/releases)
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

Active only while the missile feed overlay is on and you have **player-owned** in-flight missiles. **US English keyboard layout** (Right Alt may act as AltGr on some EU keyboards). Every keybind is a Configuration Manager `KeyboardShortcut` under **MissileCameraControls** / **MissileCameraFullscreen** / **MissileCameraAircraftCam**.

| Keybind | Default | Action |
| :--- | :--- | :--- |
| **NextMissile** | Right Alt + `/` | Next missile (newer; wraps) |
| **PreviousMissile** | Right Alt + `,` | Previous missile (older; wraps) |
| **ZoomIn** | Right Alt + `;` | MFD zoom in (narrower FOV) |
| **ZoomOut** | Right Alt + `.` | MFD zoom out (wider FOV) |
| **ResetZoom** | Right Shift + `.` | MFD reset zoom offset to `0.0` |
| **Mouse wheel** | — | **Fullscreen only:** optical zoom **1×…50×** |
| **ZoomResetKey** | Middle mouse | **Fullscreen only:** reset magnification to **1×** |
| **Toggle** | `K` | Fullscreen missile camera toggle |
| **VisionCycle** | `J` | Fullscreen vision cycle |
| **CycleMode** | Right Alt + `V` | Aircraft mini-cam mode cycle (when enabled) |

**Vision cycle (fullscreen):** Color → NightVision → WhiteHot → BlackHot → WhiteContour → BlackContour. NightVision uses a local feed Volume only — never toggles stock cockpit NVG.

**Sticky selection:** after Next/Prev the camera stays on your chosen missile when new ones launch. If it is destroyed, the feed falls back to the newest remaining missile.

**Zoom HUD (MFD):** each zoom change shows the current **offset** for **0.5 s** above feed center. Fullscreen FLIR shows `MAG xN` from optical magnification.

---

## Configuration (BepInEx Configuration Manager)

Player-facing settings only (`Config/MissileCameraBepInConfig.cs`). Advanced tuning (layout geometry, IR picture math, HUD colors, zoom feel, FX intensities) is **hardcoded** in the `*Config` classes. Use [Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager) in-game, or edit:

```text
BepInEx\config\com.at747.missilecamera.bepinex.cfg
```

> [!TIP]
> After this config slim, delete the old `.cfg` once (game closed) if orphaned keys clutter Configuration Manager — defaults recreate cleanly. Fullscreen toggle is now **`Toggle` = `K`** (no Right Alt).

### Layout

| Key | Default | Description |
| :--- | :---: | :--- |
| `Enabled` | `true` | Turns MFD layout splitting on/off |
| `DisplayMode` | `split` | Which aircraft get the missile panel: `auto` \| `skip` \| `split` |

### MissileCameraFeed

| Key | Default | Description |
| :--- | :---: | :--- |
| `Enabled` | `true` | Turns live missile nose camera on MFD on/off |
| `Fov` | `60` | Seeker base FOV before MFD zoom (degrees) |
| `FeedWidth` | `512` | MFD feed render texture width |
| `FeedHeight` | `512` | MFD feed render texture height |
| `PostLossInterferenceSeconds` | `0.5` | NO SIGNAL flash length on switch/destroy/FS exit (0 = off) |
| `RenderFps` | `30` | MFD feed render rate (Hz) |
| `InfraredAutoEnabled` | `true` | Auto B/W IR from lighting at missile |
| `InfraredDaylightThreshold` | `0.12` | IR ON threshold for daylight factor |
| `InfraredAmbientThreshold` | `0.06` | IR ON threshold for ambient light |
| `InfraredLightHysteresis` | `0.03` | Anti-flicker margin before IR turns off |

### MissileCameraHud

| Key | Default | Description |
| :--- | :---: | :--- |
| `Enabled` | `true` | Turns MFD HUD overlay on/off |
| `ShowCenterCluster` | `true` | Center reticle and intercept ring |
| `ShowTargetMarker` | `true` | Target diamond marker |
| `CockpitPipEnabled` | `true` | Bottom-left cockpit PiP |

### MissileCameraControls

| Key | Default | Description |
| :--- | :---: | :--- |
| `Enabled` | `true` | Turns keyboard missile cycling and MFD zoom on/off |
| `NextMissile` | RightAlt + `/` | Keybind: next missile |
| `PreviousMissile` | RightAlt + `,` | Keybind: previous missile |
| `ZoomIn` | RightAlt + `;` | Keybind: MFD zoom in |
| `ZoomOut` | RightAlt + `.` | Keybind: MFD zoom out |
| `ResetZoom` | RightShift + `.` | Keybind: MFD zoom reset |

### MissileCameraFullscreen

| Key | Default | Description |
| :--- | :---: | :--- |
| `Enabled` | `true` | Turns fullscreen missile feed on/off |
| `Toggle` | `K` | Keybind: enter/exit fullscreen |
| `VisionCycle` | `J` | Keybind: cycle Color / NVG / IR / Contour |
| `ZoomResetKey` | Middle mouse | Keybind: reset optical zoom to 1× |
| `FeedWidth` | `1920` | Fullscreen render texture width |
| `FeedHeight` | `1080` | Fullscreen render texture height |
| `ZoomMax` | `50` | Max optical magnification (mouse wheel) |
| `ZoomResetOnExit` | `true` | Reset to 1× when leaving fullscreen |
| `PitchLadderEnabled` | `true` | Stock pitch ladder on fullscreen FLIR |

### MissileCameraEffects

| Key | Default | Description |
| :--- | :---: | :--- |
| `ScanlinesEnabled` | `false` | Scanlines post-FX (shader bundle) |
| `MotionBlurEnabled` | `false` | Motion blur post-FX |
| `ChromaticEnabled` | `false` | Chromatic aberration post-FX |
| `BloomEnabled` | `false` | Bloom post-FX |

### MissileCameraAircraftCam

| Key | Default | Description |
| :--- | :---: | :--- |
| `Enabled` | `false` | Aircraft mini-cam overlay |
| `Mode` | `Rear` | Mini-cam view: Rear / TopDown / Chase |
| `HideInFullscreen` | `false` | Hide mini-cam in fullscreen |
| `CycleMode` | RightAlt + `V` | Keybind: cycle mini-cam mode |

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
