# MissileCamera (Nuclear Option Mod)

[![Nuclear Option](https://img.shields.io/badge/Game-Nuclear%20Option-blue)](https://store.steampowered.com/app/2168680/Nuclear_Option/)
[![BepInEx 5](https://img.shields.io/badge/Loader-BepInEx%205-orange)](https://docs.bepinex.dev/)
[![Version](https://img.shields.io/badge/Version-0.27.0-green)]()

BepInEx 5 plugin for the flight sim **Nuclear Option** that adds a live seeker-eye view (Missile Nose Cam) and a tactical HUD overlay directly onto your cockpit MFD Target display.

**Plugin GUID:** `com.at747.missilecamera.bepinex`

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

## Requirements

* **Nuclear Option** ([Steam](https://store.steampowered.com/app/2168680/Nuclear_Option/)).
* Matching game `Assembly-CSharp.dll` (for Harmony patches).
* **BepInEx 5** (x64) in the game root (`BepInEx\core\` must exist for **build** reference paths).
* **[Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager)** (recommended) — in-game UI for plugin settings.

---

## Player installation

1. Install [BepInEx 5](https://docs.bepinex.dev/articles/user_guide/installation/index.html) for Nuclear Option.
2. Copy into:

   ```text
   Nuclear Option\BepInEx\plugins\MissileCamera\
   ```

   * `MissileCamera.dll`

3. Settings are stored in `BepInEx\config\com.at747.missilecamera.bepinex.cfg` (auto-created on first run). Edit in-game via **Configuration Manager**, or edit the `.cfg` file while the game is closed.

4. Do **not** rename the plugin folder or install duplicate copies under different names.

> **Troubleshooting:** After a DLL update, delete `BepInEx\cache\harmony_interop_cache.dat` if Harmony patches behave oddly.

---

## Developer guide

Close the game before deploy (managed DLLs must be unlocked).

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

All settings are exposed through **BepInEx.Configuration** (`Config.Bind`). Use [Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager) in-game, or edit:

```text
BepInEx\config\com.at747.missilecamera.bepinex.cfg
```

NOLoader builds of this mod use `mod_config.ini` instead — do not mix config files between loaders.

### Sections

| Section | Purpose |
| :--- | :--- |
| **Layout** | MFD split layout, display mode (`auto` / `skip` / `split`), panel geometry |
| **MissileCameraFeed** | Seeker camera, FOV, render size, turn-look, render FPS |
| **MissileCameraHud** | Overlay telemetry, colors (RGBA comma strings), salvo window |
| **MissileCameraControls** | Zoom enable, step/limits, indicator duration (**keybinds fixed in code** — see **Controls & keybinds**) |

Default values match the previous `mod_config.ini` release. Hot-reload: change a value in Configuration Manager during a mission — the mod polls config every ~0.5 s.

---

## Runtime lifecycle

1. **Bootstrap:** Plugin loads at game start; **Harmony** and the feed driver stay dormant until a mission scene loads.
2. **Activation:** With Target MFD active, launch a trackable owned missile — layout applies and the feed binds.
3. **Rendering:** The auxiliary camera draws only while the overlay is active and a missile is in flight.
4. **Isolation:** Vanilla `TargetCam` geometry is not modified; layout uses UI zones and a separate render rig.

---

## Project layout

```text
MissileCamera/
├── MissileCamera.csproj
├── MissileCamera.sln
├── MissileCameraPlugin.cs      # BepInPlugin entry, Config.Bind
├── MissileCameraHost.cs        # DDOL host, mission-scene bootstrap
├── AppVersion.cs
├── Harmony/                    # Harmony patches
├── Camera/                     # Feed rig, config, controller
├── Hud/                        # Overlay widgets
├── Layout/                     # MFD zone split
├── Config/                     # BepInEx config bindings, paths
├── Access/                     # Game API wrappers
├── Ui/                         # HUD graphics helpers
├── Logging/
└── scripts/
    └── deploy.ps1
```

---

## Licence

MIT License — see [LICENSE](LICENSE).
