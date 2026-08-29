---
title: Restore Authored Bonus Choice VFX Baselines on Re-enable
date: 2026-08-15
last_updated: 2026-08-29
category: ui-bugs
module: Shooter Survival bonus choice boxes
problem_type: ui_bug
component: tooling
symptoms:
  - "Re-enabling a bonus-choice altar compounded the glow pulse into its authored scale."
  - "The stat icon resumed bobbing around its last animated position instead of its authored anchored position."
  - "Applying Unique-grade presentation during editor authoring could persist multiplied Transform and particle values as new baselines."
  - "Attack portals mixed amber beams and runes with cyan water, foam, and droplets."
  - "Returning a Unique altar to Normal or Rare could leave purple renderer overrides active when its authored MaterialPropertyBlock was empty."
root_cause: logic_error
resolution_type: code_fix
severity: medium
related_components:
  - "testing_framework"
  - "development_workflow"
tags:
  - "unity"
  - "bonus-wall"
  - "vfx"
  - "monobehaviour-lifecycle"
  - "transform-baseline"
  - "material-property-block"
  - "color-restore"
  - "regression-test"
---

# Restore Authored Bonus Choice VFX Baselines on Re-enable

## Problem

`BonusChoiceAltarVfx` animated a glow transform and a UI icon relative to cached authored values. Its original `OnEnable` implementation cached those values again, even after `LateUpdate` had already applied pulse and bob offsets. A pooled or otherwise reactivated bonus box could therefore treat animation output as its next baseline and accumulate visible drift.

The later Unique-grade presentation expanded the same invariant. Unique now
enlarges the glow ring, energy plume, ground aura, and icon auras, speeds their
motion, and increases particle size, rate, speed, and capacity. Applying those
multipliers while editing would turn transient grade output into serialized
prefab-instance input, so Play Mode could multiply an already-enlarged value.

The Unique color treatment added a second kind of transient state. Its purple
world effects use per-renderer `MaterialPropertyBlock`s so shared materials stay
unchanged, while UI icon auras use `Graphic.color`. Both paths must restore the
exact state that existed before the tint, including an authored absence of any
renderer property block.

Normal and Elite stat-family themes extend the same requirement. Every visible
effect layer must use one RGB family for the rolled stat. The authored attack
prefab could not be treated as a special baseline because its compass and beam
were amber while its water, foam, and droplets were hardcoded cyan.

## Symptoms

- Repeated deactivate/reactivate cycles progressively changed the glow size.
- The stat icon's bob center shifted after each reuse instead of returning to its authored anchored position.
- Repeated Unique refreshes could enlarge the same Transform or particle curve
  more than once.
- Selecting Unique in the map tool could dirty the scene with runtime-only VFX
  values if presentation were applied during editor authoring.
- Returning from Unique could leave a purple property override on one or more
  renderers even though the transforms and particles returned to normal.

## What Didn't Work

Recaching transform values on every enable did not reset the animation:

```csharp
private void OnEnable()
{
    CacheBaselines();
}
```

At reactivation time, `glowRoot.localScale` and `iconRect.anchoredPosition` can already contain the previous frame's animation. Recaching converts transient output into persistent input.

Applying rarity presentation unconditionally has the same flaw across the
editor/runtime boundary:

```csharp
public void SetRarity(Rarity grade)
{
    rarity = grade;
    RefreshPresentation(); // unsafe during editor authoring
}
```

That implementation would update real prefab-instance Transforms and particle
modules when a designer changes the Inspector grade. Saving the scene would
make the multiplied presentation indistinguishable from its authored baseline.

Changing `sharedMaterial` to purple was not viable because every altar using
that asset would change. Assigning a cached empty block was also not equivalent
to removing a renderer override:

```csharp
effectRenderer.SetPropertyBlock(emptyBlock); // does not reliably clear override state
```

Skipping the attack runtime tint preserved the old property-block restoration
contract but broke the visual contract. The exception assumed every authored
attack effect already used amber. The later layered-water pass added cyan
`WaterVortex`, `WaterFoam`, and `WaterDroplets` materials, so only the attack
family displayed two unrelated colors at once.

The first restoration test also compared `Color` structs exactly. Unity's
property-block round trip can introduce insignificant floating-point channel
differences, so the test reported visually identical RGBA values as unequal.

The first regression-test attempt used `SendMessage("OnEnable")`. Unity treated that as an invalid direct lifecycle dispatch in EditMode and emitted a `ShouldRunBehaviour()` assertion. Toggling `SetActive` also did not exercise a non-`ExecuteAlways` `MonoBehaviour` lifecycle in EditMode. The final test invokes the private method through reflection, matching the repository's EditMode test convention without asking Unity to synthesize a runtime lifecycle.

## Solution

Cache authored baselines once and restore them whenever the component becomes active:

```csharp
private Vector3 glowBaseScale;
private Vector2 iconBasePosition;
private bool baselinesCached;

public void Configure(Transform targetGlowRoot, RectTransform targetIconRect)
{
    glowRoot = targetGlowRoot;
    iconRect = targetIconRect;
    baselinesCached = false;
    CacheBaselines();
}

private void OnEnable()
{
    CacheBaselines();
    RestoreBaselines();
}

private void CacheBaselines()
{
    if (baselinesCached)
        return;

    if (glowRoot != null)
        glowBaseScale = glowRoot.localScale;
    if (iconRect != null)
        iconBasePosition = iconRect.anchoredPosition;

    baselinesCached = true;
}
```

`RestoreBaselines` writes the captured scale and anchored position back before the next animated frame. `Configure` is the only path that invalidates the cache because assigning new targets establishes a new authored contract.

The baseline set now also includes the energy billboard, ground aura, icon aura
scales, particle emission curve, start speed and size curves, and maximum
particle count. Rarity state is always synchronized, but its Transform and
particle presentation is applied only in Play Mode:

```csharp
public void SetRarity(Rarity grade)
{
    rarity = grade;
    if (Application.isPlaying)
        RefreshPresentation();
}

private void RefreshPresentation()
{
    CacheBaselines();
    RestoreBaselines();
    ApplyRarityPresentation();
}
```

`AuthoredBonusWall` still forwards the selected grade in both editor and
runtime contexts. Outside Play Mode this updates only the VFX component's
logical rarity. At runtime `RefreshPresentation` first restores every authored
value, then applies the Unique multipliers exactly once.

The rolled `BuffType` now selects the Normal/Elite family theme for every
renderer, including attack. The refresh first restores authored alphas and
unrelated properties, then writes one family RGB through property blocks:

```csharp
bool isUnique = rarity == Rarity.Unique;
Color worldColor = isUnique
    ? UniqueWorldPurple
    : ResolveWorldAccent(bonusType);
Color uiColor = isUnique
    ? UniqueUiPurple
    : ResolveUiAccent(bonusType);

ApplyEffectColors(worldColor, uiColor);
```

The prefab generator follows the same rule before runtime. Water, foam, and
droplet colors are scalar intensity variants of `glowColor`, so brightness can
differ without changing hue:

```csharp
Color waterColor = new(
    glowColor.r * 0.82f,
    glowColor.g * 0.82f,
    glowColor.b * 0.82f,
    0.58f);
Color foamColor = new(
    glowColor.r * 1.08f,
    glowColor.g * 1.08f,
    glowColor.b * 1.08f,
    0.78f);
```

`WallScript` applies the same UI accent to the displayed value and forwards the
rolled type to `BonusChoiceAltarVfx`. The VFX component owns the icon-aura
colors, so the floating icon and value change as one state while gameplay
values remain owned by `WallScript`.

The regression test starts with known authored values, configures the component, writes representative animated values, invokes `OnEnable` through reflection, and asserts that both targets return to the authored values.

The rarity regression test applies Unique twice and requires the second result
to equal the first, then switches to Rare and requires every Transform and
particle value to equal its baseline. A real prefab-instance editor test also
changes the authored grade to Unique and verifies that the logical rarity
updates without changing serializable visual values.

World-effect colors are applied with property blocks rather than material
mutation. Each renderer's original block and effective alpha are cached; UI
aura colors are cached separately. Non-empty blocks restore verbatim, while an
originally empty block is removed explicitly:

```csharp
MaterialPropertyBlock baseBlock = effectBasePropertyBlocks[index];
effectRenderer.SetPropertyBlock(
    baseBlock != null && !baseBlock.isEmpty ? baseBlock : null);
```

The focused test snapshots the glow ring, energy plume, ground aura, front
sigil, particle renderer, UI aura, and both shared-material color properties.
It proves that edit-mode rarity selection changes no visuals, Unique applies
purple without changing shared materials or alpha, and Rare returns transforms
and particles to baseline while applying the normal stat-family tint. Color
channels are compared with a small epsilon.

`BonusChoiceAltarVfx_UsesRolledStatFamilyForNormalTheme` separately proves that
vitality, attack, and utility produce green-, red-, and blue-dominant themes.
For each state, every cached effect renderer must have identical RGB channels;
only alpha may differ. The generated-material test also compares normalized RGB
vectors for water, foam, compass, and droplets.

## Why This Works

The fix separates immutable authored state from transient frame state. `Awake` or `Configure` captures the baseline, `LateUpdate` derives visuals from that baseline, and `OnEnable` restores it without learning from the last animated frame. Reactivation is therefore idempotent instead of cumulative.

The Play Mode gate extends that separation to editor tooling. Grade selection
is configuration state, while enlarged Transforms and denser particles are
runtime presentation outputs. Restoring before applying makes repeated refresh,
grade downgrade, and object reuse converge on one deterministic result.

Using `null` for an empty renderer baseline restores the semantic state "no
override exists." Restoring a non-empty cached block preserves unrelated
per-instance properties as well as any authored color alpha. The purple tint is
therefore an isolated runtime overlay rather than a material or prefab edit.

Applying the chosen RGB to every effect renderer makes the visual contract
explicit. Authored materials still provide textures, blend modes, and alpha,
while the runtime property block owns stat-family hue. No effect layer can keep
an unrelated cyan or amber tint simply because it came from the prefab.

## Prevention

- Treat transforms written by animation code as outputs; never recache them during reuse unless the component is explicitly reconfigured.
- Treat particle module values changed by rarity presentation as outputs too;
  cache and restore their authored curves and capacity before applying a grade.
- Gate runtime-only presentation mutations when the same component participates
  in editor authoring. Synchronize the grade without writing multiplied values
  into prefab-instance Transforms or particle modules.
- Test both directions: repeated Unique application must be idempotent, and
  switching to Normal or Rare must restore every baseline.
- Use `MaterialPropertyBlock` for per-instance rarity colors; never mutate a
  shared material asset for runtime tinting.
- Cache non-empty renderer blocks verbatim. If the original block was empty,
  clear the override with `SetPropertyBlock(null)` rather than assigning an
  empty block object.
- Preserve each renderer and UI aura's baseline alpha when replacing RGB, and
  use per-channel tolerance for floating-point color assertions.
- Do not exempt an authored theme from runtime tinting unless every generated
  layer is contract-tested to use the same hue. Prefer one uniform path.
- Compare normalized RGB vectors for generated materials so intensity variants
  cannot silently introduce a second hue.
- Keep the world-effect, icon-aura, and value-text theme derived from the same
  rolled `BuffType`; otherwise one choice can present conflicting color
  semantics.
- Add a same-instance reactivation test for pooled or repeatedly enabled visual components.
- Assert both world/local transform values and UI `RectTransform` values when an effect spans 3D and canvas elements.
- In EditMode tests, invoke private lifecycle logic through reflection when ordinary activation does not run the component; do not use `SendMessage` for Unity lifecycle names.
- For generated prefabs with event-driven localization, keep the event disabled across `SaveAsPrefabAsset` and restore its serialized `m_Enabled` value on the saved asset. Re-enabling the temporary object can race localization callbacks and serialize the wrong preview label.

## Related Issues

- [Keep world-space wall stat UI camera-facing on turning routes](keep-world-space-wall-stat-ui-camera-facing-on-turning-routes-2026-08-13.md) documents the related localization callback race; its immediate re-enable guidance should be refreshed to cover the full prefab-save boundary.
- [Bake generated prefab UI previews and isolate EditMode tests](../workflow-issues/bake-generated-prefab-ui-previews-and-isolate-editmode-tests-2026-08-13.md) covers deterministic generated UI state.
- [Transactional reference-scene gameplay composition](../integration-issues/transactional-reference-scene-gameplay-composition-2026-07-23.md) covers resetting stale pose on pooled reuse.
- [Replace duplicated bonus choice icon auras with soft gradients](replace-duplicated-bonus-choice-icon-auras-with-soft-gradients-2026-08-16.md) defines the authored altar VFX stack that the runtime rarity multipliers enrich.
