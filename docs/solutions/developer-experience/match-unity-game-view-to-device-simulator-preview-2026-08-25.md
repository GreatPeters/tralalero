---
title: Separate Game View Framing from URP Pixel Aliasing
date: 2026-08-25
last_updated: 2026-08-25
category: docs/solutions/developer-experience
module: Unity Game View mobile preview
problem_type: developer_experience
component: tooling
severity: low
applies_when:
  - Device Simulator framing looks correct while Game View looks cropped or too wide
  - Game View textures and outlines remain jagged after matching the device resolution
  - Captured Game View evidence must include Screen Space Overlay UI
symptoms:
  - Device Simulator and Game View show different portrait compositions from the same camera
  - Characters and environment outlines show stair-step pixels in Game View
root_cause: config_error
resolution_type: config_change
related_components:
  - Unity Device Simulator
  - Mobile camera composition
  - Screen Space Overlay HUD capture
tags: [unity, game-view, device-simulator, portrait, aspect-ratio, resolution, texture-filtering, mobile-preview]
---

# Separate Game View Framing from URP Pixel Aliasing

## Context

The Noryangjin scene looked correct in Unity Device Simulator but appeared
cropped and pixelated in Game View. The first investigation correctly found a
framing mismatch but incorrectly treated it as the complete explanation. After
Game View was changed from `9:16` to the device-like `1080x2340` resolution, the
stair-step pixels on character and environment edges remained.

Two independent configuration differences were present:

```text
Preset: 16:9 Portrait
Rendered size: 470x836
Aspect: 0.5625
Low Resolution Aspect Ratios: enabled
Texture filter: Point

Active pipeline: [FlatKit] Example URP Asset
Pipeline MSAA: 1x
Camera antialiasing: None
Camera allows MSAA: true
```

The Device Simulator screenshot used a modern tall-phone aspect near `9:19.5`.
At `9:16`, the same perspective camera exposes a materially wider horizontal
framing, so more stalls enter from both sides and the intended lane composition
looks wrong. That explains composition, not the pixel stair-stepping. The
aliasing came from the active URP asset rendering with MSAA disabled.

## Guidance

Match Game View to the device geometry before changing camera transforms, scene
objects, or textures:

```text
Preset: 1080x2340 Portrait
Resolution type: Fixed Resolution
Aspect: 0.461538...
Low Resolution Aspect Ratios: disabled
Texture filter: Bilinear
```

Use a fixed target resolution instead of the generic `16:9 Portrait` preset
when comparing Game View with a modern phone. Disable low-resolution aspect
rendering, and use bilinear display filtering for scaled non-pixel-art output.
`Point` filtering remains appropriate for intentional pixel art, but it makes
this project's outlines and texture edges look unnecessarily harsh when the
Game View is scaled down to fit its editor panel.

Then inspect the pipeline that is actually active at runtime. Do not infer it
from similarly named assets under `Assets/Settings/`. In this case Android used
the Mobile quality level, but that level referenced FlatKit's example URP asset.
The camera already allowed MSAA. The durable rendering correction was:

```text
Mobile quality pipeline:
[FlatKit] Example URP Asset -> Mobile RP Asset (4x MSAA)

Graphics Settings fallback:
[FlatKit] Example URP Asset, MSAA 1x -> 4x
```

The regression was introduced when the Mobile quality pipeline was moved from
the original 4x-MSAA mobile asset to the FlatKit example asset while adding
stylized outlines. `Mobile RP Asset` now has the required FlatKit outline
renderer feature, so restoring it no longer loses outlines. The FlatKit example
asset remains the Graphics Settings fallback and also uses 4x MSAA, preventing
the same aliasing when a quality override is absent.

Confirm the actual runtime geometry rather than trusting the preset label:

```text
Screen.width: 1080
Screen.height: 2340
Camera.aspect: 0.461538464
Game View size type: FixedResolution
```

Game View configuration is local editor state. `UserSettings/` is ignored by
source control in this repository, so another workstation may need the same
preset and display options applied again. Do not present an ignored layout file
as a shared project setting.

## Why This Matters

Device Simulator and Game View are independent preview surfaces. A valid scene
can therefore look different without any runtime defect. Editing the camera or
level to compensate for a mismatched Game View preset would damage the already
correct device composition.

The aspect correction fixes framing by making the camera render the intended
screen shape. The Game View low-resolution toggle and bilinear filter affect how
that render target is displayed inside the editor. They do not add geometric
antialiasing to the render itself.

Four-sample MSAA evaluates polygon coverage at multiple samples per pixel before
resolving the final color. That directly smooths the stair-step edges visible on
the player, props, pier posts, and FlatKit outlines. The scene remained clean;
the durable changes are isolated to Quality Settings and the fallback URP asset.

## When to Apply

- Device Simulator and Game View disagree for the same scene and play state.
- A generic portrait preset reveals substantially more horizontal content than
  the target phone.
- Jaggedness remains in a full-resolution Game View capture after its aspect
  ratio matches the target device.
- Mobile framing is being reviewed before any camera or scene-layout edit.

Safe-area behavior still needs separate device coverage. Matching `1080x2340`
does not by itself prove that every notch, cutout, or supported aspect ratio is
handled correctly.

## Examples

The verified before/after comparison was:

```text
Before: 470x836, aspect 0.5625, low resolution on, Point
Framing fix: 1080x2340, aspect 0.4615385, low resolution off, Bilinear
Rendering fix: Mobile quality restored to Mobile RP Asset with 4x MSAA
Fallback fix: FlatKit example URP asset, MSAA 1x -> 4x
```

For visual evidence that includes a Screen Space Overlay HUD, capture the
composited screen while Unity is in Play Mode:

```powershell
unity command --project-path . capture_game_view `
  --source screen `
  --width 1080 `
  --height 2340 `
  --save_path 'tmp/image-previews/game-view-aspect/after.png'
```

A camera-source capture omits Screen Space Overlay UI and can falsely suggest
that the HUD is missing. In the current Pipeline command, the relative
`save_path` above resolves under `Assets/`; copy review evidence to the
repository's `tmp/image-previews/` folder and remove the imported temporary PNG
and `.meta` files when they are not intended project assets.

## Related

- [Keep world-space wall stat UI camera-facing on turning routes](../ui-bugs/keep-world-space-wall-stat-ui-camera-facing-on-turning-routes-2026-08-13.md)
- [Bake generated prefab UI previews and isolate EditMode instantiation](../workflow-issues/bake-generated-prefab-ui-previews-and-isolate-editmode-tests-2026-08-13.md)
- [Verify Unity material keywords after bulk outline conversions](../workflow-issues/verify-unity-material-keywords-after-bulk-outline-conversions-2026-07-02.md)
