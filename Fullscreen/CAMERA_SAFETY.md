# Camera safety (MissileCamera fullscreen)

## Forbidden

Never modify vanilla cockpit camera state:

- `CameraStateManager.mainCamera` transform / FOV / nearClip
- `CameraStateManager.cameraPivot` parent, local/world pose, scale
- `CameraStateManager.cameraMode`
- Harmony blocking of `CameraBaseState.UpdateState` / `FixedUpdateState`
- Any `SnapToCockpit` / reparent / world-pose overlay on CSM from this mod

## Required

Fullscreen missile video = `MissileCameraRig` RenderTexture → panel `RawImage` (same pipeline as MFD), stretched on the fullscreen overlay with an **opaque black backdrop**.

`MissileCameraFullscreenBootstrap` must keep `MissileCameraFeed` **CanvasGroup.alpha = 1** (never 0 — that was the old “hide RawImage because mainCamera is the view” path).

## Markers (CombatHUD)

Vanilla `HUDUnitMarker.UpdatePosition` uses `mainCamera.WorldToScreenPoint` (cockpit). With RawImage fullscreen that pins icons near screen center.

**Fix:** Harmony postfix reprojects via **feed camera** `WorldToViewportPoint` → `Screen` (do **not** use feed `WorldToScreenPoint` — that returns RT pixels). See `MissileCameraCombatHudMarkerProjection`.

## Why

Pose-overlay and `SetParent(missile)` / `SnapToCockpit` left `cameraPivot` with huge local offsets (FloatingOrigin + `cockpitViewPoint`). On exit the stock cockpit camera flew out of bounds.

## Guard file

`Fullscreen/MissileCameraFullscreenViewDriver.cs` is a **permanent no-op**. Do not put camera writes back into it. Call sites must not depend on it restoring the cockpit.
