# MissileCamera (Nuclear Option Mod)

[![Nuclear Option](https://img.shields.io/badge/Game-Nuclear%20Option-blue)](https://store.steampowered.com/app/2168680/Nuclear_Option/)
[![BepInEx 5](https://img.shields.io/badge/Loader-BepInEx%205-orange)](https://docs.bepinex.dev/)
[![Version](https://img.shields.io/badge/Version-0.26.0-green)]()

BepInEx 5 plugin for the flight sim **Nuclear Option** that adds a live seeker-eye view (Missile Nose Cam) and a tactical HUD overlay directly onto your cockpit MFD Target display.

**Plugin GUID:** `com.at747.missilecamera.bepinex`

---

## Features

* **MFD split-screen UI:** Splits the wide tactical MFD (Target view) into zones and embeds the missile feed in the weapons panel area.
* **Seeker cam (missile nose cam):** Renders a live `RawImage` feed from your latest **player-owned** in-flight missile while it guides toward the target.
* **Tactical HUD overlay:** Telemetry (`SPD`, `ALT`, `RNG`), horizon reticle, salvo info, and target markers drawn on the live feed.
* **Per-aircraft layout (`DisplayMode=auto`):**
  * **Dedicated split** (e.g. KR-67): wide target cam on the left, missile panel on the right.
  * **Small tac overlay** (e.g. Cricket): mod **skipped** — vanilla tactical MFD unchanged.
* **Mission-only bootstrap:** Harmony patches and the feed driver attach on the **first mission scene**, not in the main menu.

---

## Requirements

* **Nuclear Option** ([Steam](https://store.steampowered.com/app/2168680/Nuclear_Option/)).
* Matching game `Assembly-CSharp.dll` (for Harmony patches).
* **BepInEx 5** (x64) in the game root (`BepInEx\core\` must exist for **build** reference paths).

---

## Player installation

1. Install [BepInEx 5](https://docs.bepinex.dev/articles/user_guide/installation/index.html) for Nuclear Option.
2. Copy into:

   ```text
   Nuclear Option\BepInEx\plugins\MissileCamera\
   ```

   * `MissileCamera.dll`
   * `mod_config.ini`

3. Do **not** rename the plugin folder or install duplicate copies under different names.

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

## Configuration (`mod_config.ini`)

Edit `mod_config.ini` next to the DLL.

### `[Layout]`

| Key | Default | Description |
| :--- | :---: | :--- |
| `Enabled` | `1` | Master switch for MFD layout changes |
| `DisplayMode` | `split` | `auto` (per-aircraft) \| `skip` (bypass) \| `split` (forced split) |
| `OverlayMaxWidth` | `0.45` | Max normalized width for tac overlay detection |
| `LeftWidth` | `0.58` | Target cam column width (0–1) |
| `MissilePanelBottom` | `0.38` | Bottom edge of missile panel (engine strip below) |
| `WeaponsStripHeight` | `0.12` | Compressed weapons wireframe strip height |
| `ShowDivider` | `1` | Zone divider lines |
| `DebugStub` | `0` | Bright magenta test panel |
| `StubLabel` | `MISSILE CAMERA` | Label on debug stub |

### `[MissileCameraFeed]`

| Key | Default | Description |
| :--- | :---: | :--- |
| `Enabled` | `1` | Live missile camera feed |
| `NoseSkinInset` | `0.08` | Keep camera outside nose mesh (meters) |
| `CameraBackOffset` | `0.35` | Pull camera back from nose point (meters) |
| `Fov` | `60` | Field of view (degrees) |
| `FeedWidth` | `512` | RenderTexture width |
| `FeedHeight` | `512` | RenderTexture height |
| `HorizonLock` | `1` | World-up roll lock; body-follow pitch/yaw |
| `TurnLookBankScale` | `1` | G-load turn look scale |
| `MaxTurnLookDegrees` | `90` | Max turn-look offset (degrees) |
| `DefaultMissileGLimit` | `20` | Fallback G limit |
| `TurnLookGDeadband` | `0.15` | G deadband |
| `TurnLookGFilterHz` | `7` | G filter cutoff (Hz) |
| `TurnLookSlewDegPerSec` | `120` | Turn-look slew rate (deg/s) |
| `TurnLookSmoothTime` | `0.18` | Turn-look smoothing |
| `PostExplosionHoldSeconds` | `0` | Hold last frame after missile loss (0 = off) |
| `RenderFps` | `30` | Feed refresh rate |

### `[MissileCameraHud]`

| Key | Default | Description |
| :--- | :---: | :--- |
| `Enabled` | `1` | HUD overlay on feed |
| `SalvoWindowSeconds` | `0.5` | Salvo grouping window (seconds) |
| `ShowCenterCluster` | `1` | Center reticle / intercept ring |
| `ShowTargetMarker` | `1` | Target diamond marker |
| `InterceptColor` | `0,1,0,1` | Intercept ring RGBA (0–1) |
| `ReticleColor` | `0,0.4,1,1` | Reticle RGBA |
| `HorizonColor` | `0.05,0.35,0.08,1` | Horizon fill |
| `HorizonOutlineColor` | `0.2,1,0.25,1` | Horizon outline |
| `MissileNameColor` | `1,0,1,1` | Missile name label |
| `TargetNameColor` | `0.4,0.9,1,1` | Target name label |
| `LabelBackgroundColor` | `0.18,0.18,0.18,0.62` | Label backdrop |
| `LabelBackgroundAlpha` | `0.62` | Backdrop alpha |

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
├── mod_config.ini
├── MissileCameraPlugin.cs      # BepInPlugin entry
├── MissileCameraHost.cs        # DDOL host, mission-scene bootstrap
├── AppVersion.cs
├── Harmony/                    # Harmony patches
├── Camera/                     # Feed rig, config, controller
├── Hud/                        # Overlay widgets
├── Layout/                     # MFD zone split
├── Config/                     # INI reader, paths
├── Access/                     # Game API wrappers
├── Ui/                         # HUD graphics helpers
├── Logging/
└── scripts/
    └── deploy.ps1
```

---

## Licence

MIT License — see [LICENSE](LICENSE).
