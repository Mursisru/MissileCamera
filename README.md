# MissileCamera (Nuclear Option Mod)

[![Nuclear Option](https://img.shields.io/badge/Game-Nuclear%20Option-blue)](https://store.steampowered.com/app/2168680/Nuclear_Option/)
[![BepInEx 5](https://img.shields.io/badge/Loader-BepInEx%205-orange)](https://docs.bepinex.dev/)
[![Version](https://img.shields.io/badge/Version-0.27.1-green)]()

BepInEx 5 plugin for the flight sim **Nuclear Option** that adds a live seeker-eye view (Missile Nose Cam) and a tactical HUD overlay directly onto your cockpit MFD Target display.

**Plugin GUID:** `com.at747.missilecamera.bepinex`

**NOLoader port:** [NOLoader.MissileCamera](https://github.com/Mursisru/NOLoader.MissileCamera/tree/NOLoaderVersion) — same gameplay; uses `mod_config.ini` and Cecil patches. Use **one** loader — do not install both builds.

---

## Table of contents

* [Features](#features)
* [Choose your loader](#choose-your-loader)
* [Requirements](#requirements)
* [Player installation](#player-installation)
* [Controls & keybinds](#controls--keybinds)
* [Configuration (BepInEx Configuration Manager)](#configuration-bepinex-configuration-manager)
* [Runtime lifecycle](#runtime-lifecycle)
* [Developer guide](#developer-guide)
* [Project layout](#project-layout)
* [Compatibility & limitations](#compatibility--limitations)
* [Troubleshooting](#troubleshooting)
* [Changelog](#changelog)
* [Licence](#licence)

---

## Features

* **MFD split-screen UI:** Splits the wide tactical MFD (Target view) into zones and embeds the missile feed in the weapons panel area.
* **Seeker cam (missile nose cam):** Renders a live `RawImage` feed from your latest **player-owned** in-flight missile while it guides toward the target.
* **Tactical HUD overlay:** Telemetry (`SPD`, `ALT`, `RNG`), horizon reticle, salvo info, and target markers drawn on the live feed.
* **Manual feed controls:** Cycle in-flight owned missiles and adjust camera zoom while the MFD overlay is active (see **Controls** below).
* **Per-aircraft layout (`DisplayMode=auto`):**
  * **Dedicated split** (e.g. KR-67): wide target cam on the left, missile panel on the right.
  * **Small tac overlay** (e.g. Cricket): mod **skipped** — vanilla tactical MFD unchanged.
* **Mission-only bootstrap:** Harmony patches and the feed driver attach on the **first mission scene**, not in the main menu.

---

## Choose your loader

| | **This repo (BepInEx)** | [NOLoader.MissileCamera](https://github.com/Mursisru/NOLoader.MissileCamera/tree/NOLoaderVersion) |
|---|---|---|
| Loader | BepInEx 5 + Harmony | [NOLoader](https://github.com/Mursisru/NOLoader) |
| Config | Configuration Manager (`.cfg`) | `mod_config.ini` |
| Patches | Harmony runtime | Cecil IL + PatchTool |
| Install path | `BepInEx\plugins\MissileCamera\` | `NOLoader\mods\MissileCamera\` |

**Do not install both loaders** in the same game directory.

---

## Requirements

* **Nuclear Option** ([Steam](https://store.steampowered.com/app/2168680/Nuclear_Option/)).
* Matching game `Assembly-CSharp.dll` (for Harmony patches).
* **BepInEx 5** (x64) in the game root (`BepInEx\core\` must exist for **build** reference paths).
* **[Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager)** (recommended) — in-game UI for plugin settings.

---

## Player installation

1. Install [BepInEx 5](https://docs.bepinex.dev/articles/user_guide/installation/index.html) for Nuclear Option.
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

Active only while the missile feed overlay is on and you have **player-owned** in-flight missiles. **US English keyboard layout** (Right Alt may act as AltGr on some EU keyboards). Keybinds are **fixed in code** (not in Configuration Manager).

| Keybind | Unity `KeyCode` | Action |
| :--- | :--- | :--- |
| **Right Alt** + `/` | `RightAlt` + `Slash` | Next missile (newer; wraps 6/6 → 1/6) |
| **Right Alt** + `,` | `RightAlt` + `Comma` | Previous missile (older; wraps 1/6 → 6/6) |
| **Right Alt** + `;` | `RightAlt` + `Semicolon` | Zoom in (narrower FOV) |
| **Right Alt** + `.` | `RightAlt` + `Period` | Zoom out (wider FOV) |
| **Right Shift** + `.` | `RightShift` + `Period` | Reset zoom offset to `0.0` |

**Sticky selection:** after Next/Prev the camera stays on your chosen missile when new ones launch. If it is destroyed, the feed falls back to the newest remaining missile.

**Zoom HUD:** each zoom change shows the current **offset** for **0.5 s** above feed center. Zoom step/limits: **MissileCameraControls** in Configuration Manager.

---

## Configuration (BepInEx Configuration Manager)

All settings are exposed through **BepInEx.Configuration** (`Config.Bind` in `Config/MissileCameraBepInConfig.cs`). Use [Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager) in-game, or edit:

```text
BepInEx\config\com.at747.missilecamera.bepinex.cfg
```

NOLoader builds use `mod_config.ini` with the **same keys and defaults** — do not mix config files between loaders.

Hot-reload: change a value in Configuration Manager during a mission — the mod polls config every ~0.5 s.

### Layout

| Key | Default | Description |
| :--- | :---: | :--- |
| `Enabled` | `true` | Master switch for MFD layout changes |
| `DisplayMode` | `split` | `auto` \| `skip` \| `split` |
| `OverlayMaxWidth` | `0.45` | Max normalized width for tac overlay detection |
| `LeftWidth` | `0.58` | Target cam column width (0–1) |
| `MissilePanelBottom` | `0.38` | Bottom edge of missile panel |
| `WeaponsStripHeight` | `0.12` | Compressed weapons strip height |
| `ShowDivider` | `true` | Zone divider lines |
| `DebugStub` | `false` | Bright magenta test panel |
| `StubLabel` | `MISSILE CAMERA` | Label on debug stub |

### MissileCameraFeed

| Key | Default | Description |
| :--- | :---: | :--- |
| `Enabled` | `true` | Live missile camera feed |
| `NoseSkinInset` | `0.08` | Keep camera outside nose mesh (meters) |
| `CameraBackOffset` | `0.35` | Pull camera back from nose point (meters) |
| `Fov` | `60` | Base field of view (degrees) |
| `FeedWidth` | `512` | RenderTexture width |
| `FeedHeight` | `512` | RenderTexture height |
| `HorizonLock` | `true` | World-up roll lock |
| `TurnLookBankScale` | `1` | G-load turn look scale |
| `MaxTurnLookDegrees` | `90` | Max turn-look offset (degrees) |
| `DefaultMissileGLimit` | `20` | Fallback G limit |
| `TurnLookGDeadband` | `0.15` | G deadband |
| `TurnLookGFilterHz` | `7` | G filter cutoff (Hz) |
| `TurnLookSlewDegPerSec` | `120` | Turn-look slew rate (deg/s) |
| `TurnLookSmoothTime` | `0.18` | Turn-look smoothing |
| `PostExplosionHoldSeconds` | `0` | Hold last frame after missile loss (0 = off) |
| `RenderFps` | `30` | Feed refresh rate |

### MissileCameraHud

| Key | Default | Description |
| :--- | :---: | :--- |
| `Enabled` | `true` | HUD overlay on feed |
| `SalvoWindowSeconds` | `0.5` | Salvo grouping window (seconds) |
| `ShowCenterCluster` | `true` | Center reticle / intercept ring |
| `ShowTargetMarker` | `true` | Target diamond marker |
| `InterceptColor` | `0,1,0,1` | Intercept ring RGBA (0–1) |
| `ReticleColor` | `0,0.4,1,1` | Reticle RGBA |
| `HorizonColor` | `0.05,0.35,0.08,1` | Horizon fill |
| `HorizonOutlineColor` | `0.2,1,0.25,1` | Horizon outline |
| `MissileNameColor` | `1,0,1,1` | Missile name label |
| `TargetNameColor` | `0.4,0.9,1,1` | Target name label |
| `LabelBackgroundColor` | `0.18,0.18,0.18,0.62` | Label backdrop |
| `LabelBackgroundAlpha` | `0.62` | Backdrop alpha |

### MissileCameraControls

| Key | Default | Description |
| :--- | :---: | :--- |
| `Enabled` | `true` | Keyboard missile cycling and zoom (**keybinds fixed in code**) |
| `ZoomStep` | `0.5` | Offset change per zoom key press |
| `ZoomMin` | `-4` | Minimum zoom offset |
| `ZoomMax` | `4` | Maximum zoom offset |
| `ZoomFovDegreesPerUnit` | `5` | FOV delta (degrees) per offset unit |
| `IndicatorSeconds` | `0.5` | Zoom HUD readout duration (seconds) |

---

## Runtime lifecycle

1. **Bootstrap:** Plugin loads at game start; **Harmony** and the feed driver stay dormant until a mission scene loads (`MissileCameraHost`).
2. **Activation:** With Target MFD active, launch a trackable owned missile — layout applies and the feed binds.
3. **Rendering:** The auxiliary camera draws only while the overlay is active and a missile is in flight.
4. **Isolation:** Vanilla `TargetCam` geometry is not modified; layout uses UI zones and a separate render rig.

---

## Developer guide

Close the game before deploy (managed DLLs must be unlocked).

**AI / mod authors:** see `.cursorrules` in this repo; NOLoader port workflow in [NOLoader.MissileCamera](https://github.com/Mursisru/NOLoader.MissileCamera) and `NOLoader_Engine/.cursorrules`.

### Quick deploy

```powershell
.\scripts\deploy.ps1 -ClearHarmonyCache
```

### Build

Set the game path in `Directory.Build.props` (`NuclearOptionRoot`) if needed. Copy `Directory.Build.props.example` to `Directory.Build.props` and adjust the path.

```powershell
dotnet build MissileCamera.csproj -c Release
```

Output: `bin\Release\net48\MissileCamera.dll`

Open `MissileCamera.sln` in Visual Studio or JetBrains Rider for IDE builds.

### Harmony patches

Seven postfix patches in `Harmony/MfdHarmonyPatches.cs` (same targets as NOLoader `mod.json`):

* `TargetScreenUI::SetupCamera`
* `TargetCam::SetLandingCam` / `CancelTarget` / `OnDestroy`
* `TacScreen::Initialize` / `TacScreen_OnCamToggle`
* `WeaponManager::TargetListChanged`

Shared hook bodies: `Harmony/MfdHarmonyHooks.cs`.

### Porting to NOLoader

1. Copy `Camera/`, `Hud/`, `Layout/`, `Access/`, `Ui/` to NOLoader repo `src/`.
2. Map Harmony postfixes → `Patches.cs` static methods + `mod.json` entries with `expectedSignatureHash`.
3. Mirror Configuration Manager defaults in `mod_config.ini`.
4. See [NOLoader.MissileCamera README](https://github.com/Mursisru/NOLoader.MissileCamera/blob/NOLoaderVersion/README.md#porting-from-bepinex).

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
└── scripts/
    └── deploy.ps1
```

**GitHub:** [Mursisru/MissileCamera](https://github.com/Mursisru/MissileCamera/tree/BepInExVersion) · **NOLoader port:** [NOLoader.MissileCamera](https://github.com/Mursisru/NOLoader.MissileCamera/tree/NOLoaderVersion)

---

## Compatibility & limitations

Developed and tested against **vanilla Nuclear Option** aircraft and the stock Target MFD. The mod may work **incorrectly or not at all** when:

* **Third-party / custom aircraft** — non-vanilla cockpit MFD hierarchy, custom `TargetScreenUI` layouts, or unusual weapon integration can break layout detection (`DisplayMode=auto`), feed binding, nose-cam placement, or salvo tracking.
* **Other mods that change the MFD** — tactical UI overlays, layout replacers, or Harmony patches to `TargetScreenUI`, `TacScreen`, `TargetCam`, or target/weapon lists may **conflict** with this plugin's hooks and UI zone split.

**Mitigation:** set **Layout → DisplayMode** to `skip` in Configuration Manager to keep vanilla MFD layout (feed may still bind if hooks remain compatible), or disable conflicting MFD mods. For modded setups, include aircraft/mod names and repro steps in issue reports.

---

## Troubleshooting

| Symptom | Likely cause | Fix |
| :--- | :--- | :--- |
| Plugin not in Configuration Manager | Wrong folder / BepInEx broken | Verify `BepInEx\plugins\MissileCamera\MissileCamera.dll` |
| Hooks never fire after update | Stale Harmony cache | Delete `BepInEx\cache\harmony_interop_cache.dat`; restart |
| No feed on MFD | No owned in-flight missile | Launch missile with Target MFD active |
| Layout wrong on modded aircraft | Custom MFD | `DisplayMode=skip` |
| Keybinds ignored | Wrong keyboard / overlay off | US layout; feed must be active |
| NOLoader + BepInEx conflict | Both loaders | Remove one installation |

**Logs:** `BepInEx\LogOutput.log` — search for `com.at747.missilecamera.bepinex`.

---

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

---

## Licence

MIT License — see [LICENSE](LICENSE).
