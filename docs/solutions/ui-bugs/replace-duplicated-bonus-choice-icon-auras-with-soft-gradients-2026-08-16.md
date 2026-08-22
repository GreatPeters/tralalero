---
title: Replace Duplicated Bonus Choice Icon Auras with Soft Gradients
date: 2026-08-16
last_updated: 2026-08-17
category: ui-bugs
module: Shooter Survival bonus choice boxes
problem_type: ui_bug
component: tooling
severity: low
symptoms:
  - "Attack and health icons appeared doubled, ghosted, or surrounded by displaced colored contours."
  - "Small mint hearts and orange flame sprites looked like static stickers around the choices."
  - "Particle-only tuning reached the icon mathematically but remained visually imperceptible at 369 by 657 gameplay resolution."
  - "Bright altar effects and detailed dock scenery washed through the title, stat row, and hero icon."
  - "Enlarging the plume without bottom anchoring pushed the effect into the title and stat band."
root_cause: logic_error
resolution_type: code_fix
related_components:
  - "testing_framework"
  - "development_workflow"
tags:
  - "unity"
  - "bonus-choice"
  - "world-space-ui"
  - "particle-system"
  - "vertical-glow"
  - "billboard"
  - "render-order"
  - "visual-regression"
---

# Replace Duplicated Bonus Choice Icon Auras with Soft Gradients

## Problem

The bonus-choice altars accumulated three related duplication problems around
their hero icons:

1. Two enlarged aura layers reused the detailed attack or health sprite, so
   the sword, heart, arrow, and baked contours appeared as ghost copies.
2. The main `Stat_Icon` added UGUI `Outline` and `Shadow` mesh effects to a PNG
   that already contained its intended border.
3. Static `SemanticMote_*` `RawImage`s repeated literal hearts, flames, and
   embers around an icon that already communicated the choice.

The third problem was especially visible on the health altar. Five saturated,
symmetrically placed mint hearts read as cheap stickers rather than energy.
The particle system also reused literal heart or ember textures, repeating the
same semantics in the hero icon, pedestal sigil, Canvas decoration, and VFX.

None of these artifacts came from the altar's FlatKit shader. The affected UI
graphics used Unity's default Canvas material.

After the duplicate shapes were removed and the reference-strength plume was
restored, a second hierarchy problem became visible: the Korean title and stat
row sat directly over high-frequency dock detail, while the icon relied on the
root Canvas ordering rather than its own foreground contract. The text needed
local contrast without weakening the effect, and the icon needed a render root
that could never fall behind the transparent altar layers.

## Symptoms

- Detailed icon edges appeared two or three times at different scales.
- A colored fringe and offset black copy followed the main icon.
- Five static hearts surrounded the health icon; attack had three static
  flames and four static embers.
- Lowering opacity made the clutter fainter but did not make it feel spatial or
  animated.
- The first abstract particle replacement passed asset checks but fell below
  the perceptual threshold in the actual 369 by 657 gameplay framing.
- White bold text and a glyph outline still lost contrast over wood grain,
  props, and the bright vertical plume.
- Increasing the VFX made the icon look merged with the energy even when its
  hierarchy sibling order appeared correct.

## What Didn't Work

### Blaming the shader or changing the altar material

The main icon and semantic motes had no custom UI material. FlatKit rendered
only the 3D pedestal. Changing those shaders could not remove duplicated Canvas
geometry or static decorative images.

### Reusing semantic art as a glow

Low opacity preserves every internal detail in a sword, flame, heart, or arrow.
The result remains recognizable as a second copy. A glow layer must be an
abstract alpha mask rather than the source icon.

### Lowering sticker opacity

At readable opacity the hearts still looked pasted on. Below that threshold
they disappeared while retaining unnecessary hierarchy and overdraw. The
correct fix was to remove them.

### Fixing only one duplicate path

Replacing the aura sprites left `Outline` and `Shadow` on the main icon.
Removing those mesh modifiers still left the static `SemanticMote_*` objects.
All three render paths had to be handled independently.

### Testing only existence and upper bounds

A particle texture and system can exist while producing no readable effect.
Upper bounds alone prevent excess but do not prevent an emission rate, alpha,
or particle size from becoming effectively zero. Structural tests therefore
need visibility lower bounds, and the result still needs a real-scene capture.

### Extending particle travel without a deterministic coverage layer

Longer lifetime, faster rise, and taller particles moved the calculated screen
bounds into the icon area. They still read as isolated noise at 369 by 657,
not as light connecting the rune seal to the icon. Adding more particles would
have increased clutter and overdraw without guaranteeing the reference's broad,
controlled overlap.

### Using two narrow veils with the VFX in front of the Canvas

The first deterministic bridge was only two `0.22 x 0.86` glow strips. It was
far smaller and dimmer than the reference, so increasing particle travel still
could not produce a broad icon aura. Its transparent order also placed the
strips over the world-space Canvas, which washed across the icon instead of
framing it from behind.

Simply making those strips taller created a second failure: the plume entered
the title and stat band. The accepted layout keeps the bottom attached to the
rune while shortening only the upper reach.

### Relying on bold text, outline, or sibling order alone

A glyph outline does not remove the high-frequency scenery visible inside and
around the letters. Weakening the full plume protected the text but discarded
the strong reference look. `SetAsLastSibling()` also orders only graphics in
one Canvas; it is not an explicit contract against separate transparent mesh
and particle renderers. The final fix separates contrast, UI sibling order, and
renderer sorting into three independently testable concerns.

## Solution

### 1. Render the hero icon once

The main `Stat_Icon` keeps its authored PNG and baked border, explicitly uses
the default UI material, and removes every `Shadow` derivative. `Outline`
inherits from `Shadow`, so the cleanup removes both mesh effects:

```csharp
image.sprite = iconSprite;
image.material = null;
image.preserveAspect = true;
image.raycastTarget = false;

foreach (Shadow effect in iconObject.GetComponents<Shadow>())
    UnityEngine.Object.DestroyImmediate(effect);
```

### 2. Use shape-free icon auras

`Stat_Icon_AuraInner` and `Stat_Icon_AuraOuter` remain animated
`RectTransform`s, but they render `BonusBox_SoftAura.png` through `RawImage`.
Legacy `Image`, `Outline`, and `Shadow` components are removed during every
prefab regeneration.

The refined bounds and alpha make the reference-scale halo visible outside the
hero silhouette while keeping it shape-free:

- inner: 130 percent of the icon width, alpha 0.25;
- outer: 155 percent of the icon width, alpha 0.12.

This supplies colored light without redrawing any semantic contour.

### 3. Remove static semantic decoration

The generator no longer calls `CreateSemanticMotes`, and no descendant named
`SemanticMote_*` is created. The only floating heart, sword, or flame artwork is
the central choice icon. Ambient energy is deliberately abstract.

The only `RawImage`s permitted below the stat Canvas are:

```text
Stat_Icon_AuraInner
Stat_Icon_AuraOuter
```

### 4. Keep one sparse particle system and differentiate texture and motion

Health uses the generated 64 by 64 `BonusBox_EnergyMote.png`, a soft four-point
glint with transparent corners and a small readable core. Attack uses the
existing `FX_TX_VerticalImpact_01.png` to produce narrow flame/impact streaks.
Neither path repeats the hero icon or a literal heart sticker.

Each altar owns exactly one local-space particle system:

- Attack: at most 16 particles, rate 11 per second, bright amber vertical
  streaks, faster rise, narrow 14-degree cone.
- Health: at most 12 particles, rate 7 per second, slower cyan/mint glints,
  wider 17-degree cone.

Those colors and counts are the authored Normal/Elite baseline. At runtime a
Unique altar overrides the existing glow, plume, ground aura, front sigil,
particle renderer, and UI icon auras with one vivid purple hue, then increases
particle size, rate, speed, and capacity. It uses per-renderer
`MaterialPropertyBlock`s rather than editing shared materials, preserves each
renderer and UI aura's authored alpha, and restores the exact previous blocks
when the grade returns to Normal or Elite.

Trails, collision, lights, sub-emitters, cast shadows, and receive shadows stay
disabled. This reduces overdraw while keeping the two choices visibly distinct.

### 5. Rebalance the complete glow stack

The ring remains the strongest environmental effect. Ground aura and beam now
connect the icon to the pedestal without competing with it:

```csharp
Color glyphColor = WithAlpha(glowColor, 0.36f);
Color ringColor = WithAlpha(glowColor, 0.44f);
Color arcColor = WithAlpha(glowColor, 0.12f);
Color auraColor = WithAlpha(glowColor, 0.30f);
Color beamColor = WithAlpha(glowColor, isAttack ? 0.48f : 0.42f);
Color sigilColor = WithAlpha(glowColor, 0.55f);
```

Attack and health also use opposite glow rotation and different phase offsets.
Pulse, bob, and sway therefore do not move in lockstep.

### 6. Group a reference-scale energy plume behind the icon

Particles remain the secondary motion layer. A single camera-facing
`IconEnergyBillboard` owns the complete deterministic bridge so child tilts are
preserved while the group follows turning cameras:

```text
IconEnergyBillboard
  IconEnergyHalo
  VerticalBeam
  IconEnergyCore
  IconEnergyVeilLeft
  IconEnergyVeilRight
```

The halo uses `FX_TX_GlowADD_01.png`; the beam and hot core use
`FX_TX_VerticalGlow_01.png`. Attack side plumes use the sharper
`FX_TX_VerticalImpact_01.png`, while health keeps the rounder vertical glow.
The billboard center sits `0.47` rune diameters above the ring. Its halo, beam,
core, and side-plume heights are `0.84`, `0.95`, `0.85`, and `0.93` rune
diameters respectively. That bottom-anchored shortening keeps the glow attached
to the seal while leaving a visible gap below the stat row.

Transparent ordering is explicit:

```text
runes and beam 0 -> particles/core 1 -> side plumes 2
-> root stat Canvas 3 -> nested hero-icon Canvas 4
```

The root world-space Canvas therefore draws the copy in front. `Stat_Icon`
owns a nested Canvas with `overrideSorting = true`, the same sorting layer, and
order 4, so the semantic icon has an independent foreground guarantee. No
`CanvasScaler` or `GraphicRaycaster` is added. The additive plume remains strong
around the silhouette instead of painting over it.

### 7. Protect the copy with local contrast

`Choice_TextBackplate` is a direct child of the stat Canvas. It uses Unity's
built-in sliced UI sprite and a dark navy tint at alpha 0.58, covering the title
and subordinate stat row without becoming opaque UI chrome:

```csharp
plateTransform.anchorMin = new Vector2(0.055f, 0.915f);
plateTransform.anchorMax = new Vector2(0.945f, 1.22f);

plateImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
    "UI/Skin/UISprite.psd");
plateImage.type = Image.Type.Sliced;
plateImage.color = new Color(0.015f, 0.025f, 0.04f, 0.58f);
plateImage.material = null;
plateImage.raycastTarget = false;
```

The shared persistent TMP material uses a slightly stronger outline and face,
plus a short black underlay rather than a UGUI `Shadow` mesh duplicate:

```csharp
SetFloatIfPresent(material, "_OutlineWidth", 0.28f);
SetFloatIfPresent(material, "_FaceDilate", 0.08f);
material.EnableKeyword("UNDERLAY_ON");
SetColorIfPresent(material, "_UnderlayColor", new Color(0f, 0f, 0f, 0.65f));
SetFloatIfPresent(material, "_UnderlayOffsetX", 0.06f);
SetFloatIfPresent(material, "_UnderlayOffsetY", -0.08f);
SetFloatIfPresent(material, "_UnderlayDilate", 0.04f);
SetFloatIfPresent(material, "_UnderlaySoftness", 0.1f);
```

The title is warm white, the stat name is cool off-white, and the values are
gold for attack or mint for health. The deterministic Canvas sibling order is:

```text
soft auras -> smoked backplate -> hero icon -> stat row -> choice title
```

The plate slightly overlaps the health icon's upper bounds, so placing the
icon after it is part of the visual contract rather than an incidental detail.

### 8. Verify regeneration and the gameplay capture

The editor command below captures the actual placed choices through a copied
gameplay camera at 369 by 657:

```text
Tools/Shooter Survival/Bonus Choice Boxes/Capture Gameplay Preview
```

It seeds and simulates both particle systems for 1.2 seconds, fails when either
system emits zero particles, records their screen bounds, writes
`Temp/BonusChoicePremiumVfx.png`, and restores every temporarily rotated
billboard plus particle state. The active scene remains clean.

The focused prefab test calls `BuildWallPrefabs()` twice before inspecting the
saved assets. This covers regeneration and existing-child branches, then checks
that the backplate contains both text bands, every TMP component shares the
underlay material, exactly two Canvas render roots exist, and every transparent
altar renderer remains below both the copy Canvas and the icon Canvas.

## Why This Works

The fix restores a clear visual hierarchy:

1. one semantic hero icon;
2. a soft shape-free halo;
3. a rune circle and broad, bottom-anchored icon-reaching plume;
4. sparse abstract particles for motion.

Removing repeated literal symbols solves the composition problem rather than
hiding it with lower alpha. Motion and tint communicate attack versus health
more naturally, while shape-free ambient layers keep both altars in one visual
language. Lower and upper particle constraints make the effect visible without
letting it grow back into sticker-like clutter. The grouped plume supplies a
stable bridge where stochastic particle coverage cannot, and its shared
billboard preserves both the bridge and child tilt as the camera turns.
Explicit Canvas order keeps the icon readable without weakening the background
energy. The smoked plate reduces luminance variation only behind the copy, and
the TMP outline plus underlay provide crisp and soft separation at different
scales. This keeps the reference-strength plume intact instead of solving text
contrast by dimming the entire altar.

## Prevention

- Never reuse detailed semantic art for an ambient aura unless an afterimage is
  intentional.
- Do not add UGUI `Outline` or `Shadow` to art with a baked border. Check
  `GetComponents<Shadow>()` because `Outline` derives from `Shadow`.
- Recursively collect Canvas `RawImage`s and allow only the two soft auras;
  direct-child or name-prefix checks alone can miss nested regressions.
- Assert that both aura textures are `BonusBox_SoftAura.png`, contain no legacy
  `Image` or `Shadow`, and remain non-raycastable.
- Count from each prefab root and require exactly one `ParticleSystem`; reject
  literal hero, heart, or ember textures from its material.
- Test both lower and upper bounds for particle alpha, emission, dimensions,
  lifetime, and maximum count.
- Assert the plume's halo, beam, core, side materials, alpha, side offsets,
  depth, and bottom-anchored vertical span: it must begin at the rune circle,
  surround the icon, and stay below the stat row.
- Require the stat Canvas and grouped plume root to face arbitrary
  gameplay-camera rotations. Keep child tilts authored under the shared
  billboard instead of putting a billboard component on each tilted quad.
- Assert the root stat Canvas is order 3 and the nested hero-icon Canvas is
  override-sorting order 4 on the same sorting layer. Reject `CanvasScaler` and
  `GraphicRaycaster` on the icon.
- Recursively inspect every transparent altar renderer: require the shared
  sorting layer, an order below both UI Canvases, queue 3000, and zero Z-write.
- Require a sliced, non-raycastable `Choice_TextBackplate` with an independently
  bounded readable alpha and dimensions that contain both text bands.
- Keep `UNDERLAY_ON`, outline, face dilation, underlay offsets, and opaque title,
  stat-name, and value colors asserted on all three TMP components.
- Preserve `soft auras -> backplate -> icon -> stat row -> title` sibling order.
- Run the public prefab builder twice in the focused test so assertions cannot
  pass only because stale generated assets remain on disk.
- Keep trails, collision, lights, sub-emitters, and renderer shadows disabled.
- Assert meaningful differences in attack and health motion and phase.
- For Unique rarity, assert every effect renderer and both UI auras receive the
  purple override while retaining their baseline alpha. On downgrade, compare
  the restored property blocks against their pre-override snapshots; an empty
  block must be cleared with `SetPropertyBlock(null)` rather than assigned as an
  empty object.
- After regeneration, capture the actual gameplay scene. Hierarchy and asset
  tests cannot judge whether a restrained effect is visible at final scale.

The focused prefab suite passed `38/38` after two consecutive generator runs,
and both player and editor builds completed with zero warnings or errors. The
deterministic capture emitted particles for both choices with nonzero screen
bounds and showed the amber and cyan plumes reaching behind the lower icons,
while the smoked plates kept both Korean text bands readable. Final
actual-scene review found no remaining blocking layering, overlap, sticker,
text-contrast, or visibility issue.

## Related Issues

- [Center bonus choice stat rows by combined content width](center-bonus-choice-stat-rows-by-combined-content-width-2026-08-16.md)
- [Bake generated prefab UI previews and isolate EditMode instantiation](../workflow-issues/bake-generated-prefab-ui-previews-and-isolate-editmode-tests-2026-08-13.md)
- [Keep world-space wall stat UI camera-facing on turning routes](keep-world-space-wall-stat-ui-camera-facing-on-turning-routes-2026-08-13.md)
- [Restore authored bonus choice VFX baselines on re-enable](restore-authored-bonus-choice-vfx-baselines-on-reenable-2026-08-15.md)
