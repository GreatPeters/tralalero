---
title: Bake Generated Prefab UI Previews and Isolate EditMode Instantiation
date: 2026-08-13
last_updated: 2026-08-28
category: docs/solutions/workflow-issues
module: Unity Feast bonus wall prefab generation
problem_type: workflow_issue
component: tooling
severity: medium
applies_when:
  - "Generating Unity prefabs whose editor-visible UI, materials, textures, or VFX references must be correct before runtime"
  - "A prefab's production parent rotation or scale differs from its isolated preview"
  - "A builder generates or replaces procedural textures, materials, or VFX hierarchies"
  - "EditMode tests instantiate prefabs while an authored scene may already be dirty"
  - "A generated object is disabled and reactivated during gameplay"
symptoms:
  - "Generated walls retained stale template labels and values until runtime initialization repaired them"
  - "Local horizontal offsets looked correct at yaw zero but reversed under the production 180-degree placement yaw"
  - "A procedural magic circle rendered as an opaque plate instead of sparse transparent rings"
  - "Outlined TMP text looked correct in memory while its shared material reference could serialize as null"
  - "Component-presence tests passed without proving serialized VFX wiring or reusable trigger state"
related_components: [testing_framework, development_workflow]
tags: [unity, prefab-builder, serialization, editmode-tests, production-transform, textmeshpro, procedural-texture, unity-undo]
---

# Bake Generated Prefab UI Previews and Isolate EditMode Instantiation

## Context

The Feast of Fortune builder initially assigned the correct `BuffType` and icon,
but the saved prefabs still contained the template's old label and value. Play
Mode hid the defect by repairing the UI after `WallScript` found the player.
The first EditMode verification also instantiated objects into the user's dirty
Noryangjin scene, allowing Unity's save/discard dialog to block automation.

The later visual-fidelity pass exposed a broader version of the same mistake:
an attractive isolated preview is not the shipped artifact. The production
roots use yaw 180 degrees, generated PNG alpha must be correct on disk, TMP must
reference a persistent material asset after serialization, VFX fields must
point to the intended saved transforms, and a reused choice must restore its
interaction state. Each boundary can look correct in memory while the saved or
placed result is wrong.

A later EditMode fixture exposed another isolation boundary. It instantiated
`random_wall_normal`, then called a production helper that registered the
child `GFX` object with Unity's global Undo system. The fixture removed the
parent through ordinary teardown. After Test Runner restored the authored
Noryangjin scene, the Undo-tracked child appeared there as a new top-level
`GFX` root even though that root did not exist in the saved scene YAML.

## Guidance

Treat the generated prefab as a complete serialized artifact, then verify it in
the transform and lifecycle context where the player will encounter it.

### Bake the full authored contract before saving

Derive gameplay type, localization entry, icon, preview value, visual hierarchy,
and serialized VFX references from the same build definition before calling
`SaveAsPrefabAsset`:

```csharp
wall.wallType = WallType.BuffWall;
wall.buffType = definition.BuffType;
wall.isRandom = definition.IsRandom;
wall.rarity = definition.Rarity;

ConfigureStatLocalization(wall, definition.BuffType);
RectTransform[] iconAuras = ConfigureStatIcon(
    root,
    wall,
    definition.BuffType,
    definition.VisualOffsetX);
ConfigureStatValuePreview(wall, definition.BonusValue);

altarVfx.Configure(
    glowRoot,
    wall.statIconImage.rectTransform,
    iconAuras);
```

Do not depend on runtime initialization to make an asset readable in the
Project window, Inspector, map tool, or Scene view. When checking a localized
table entry, resolve its numeric key through the shared table data instead of
assuming `TableEntryReference.Key` is populated.

### Retire superseded generated assets after the replacement prefab is saved

Replacing generated VFX is not complete when the new hierarchy renders. The
previous procedural textures, materials, and generation branches remain valid
Unity assets until the builder deletes them. Save the replacement prefab first,
then remove only the exact generated paths that the new prefab no longer uses:

```csharp
string altarPrefab = BuildWallPrefab(materials, beveledBoxMesh);
RemoveLegacyRuneAssets();
AssetDatabase.SaveAssets();

private static void RemoveLegacyRuneAssets()
{
    string[] obsoleteAssetPaths =
    {
        MaterialFolder + "/BonusBox_AttackGlow.mat",
        MaterialFolder + "/BonusBox_AttackRuneCircle.mat",
        MaterialFolder + "/BonusBox_AttackParticles.mat",
        GeneratedTextureFolder + "/BonusBox_MagicCircle.png",
        GeneratedTextureFolder + "/BonusBox_EnergyMote.png"
    };

    foreach (string assetPath in obsoleteAssetPaths)
    {
        if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null)
            AssetDatabase.DeleteAsset(assetPath);
    }
}
```

The explicit catalog keeps cleanup narrow and reviewable. Do not delete an
entire generated directory or infer deletion from filename globs. A prefab
contract test should assert both sides of the replacement: the new water-vortex
layers and textures exist, while the old rune material and magic-circle texture
do not.

### Assign persistent TMP materials after component setters

TMP outline setters can create or select a component-local material instance.
If a builder assigns a persistent material first and then changes outline
properties, the text may render correctly in memory while the prefab stores a
null or transient reference. Configure the component first and assign the
AssetDatabase-backed shared material last:

```csharp
title.font = template.font;
title.outlineWidth = 0.25f;
title.outlineColor = Color.black;

Material persistentMaterial =
    CreateOrUpdateChoiceTextMaterial(template.font);
title.fontSharedMaterial = persistentMaterial;
```

Reload the saved prefab and assert the persistent path, not only non-nullness:

```csharp
Assert.That(
    AssetDatabase.GetAssetPath(choiceTitle.fontSharedMaterial),
    Is.EqualTo(
        "Assets/ShooterSurvival/Materials/Generated/" +
        "BonusChoiceBoxes/BonusBox_ChoiceText.mat"));
```

### Test generated pixels with the intended math

Same-named shader and C# APIs may not share semantics. Unity's
`Mathf.SmoothStep(from, to, t)` interpolates outputs; it is not GLSL's
threshold-shaped `smoothstep(edge0, edge1, x)`. Normalize first and apply the
cubic easing explicitly:

```csharp
private static float SmoothThreshold(
    float edge0,
    float edge1,
    float value)
{
    float t = Mathf.InverseLerp(edge0, edge1, value);
    return t * t * (3f - 2f * t);
}
```

Validate the generated PNG itself. For a magic circle, assert transparent
corners, an open center, a visible target ring, and a bounded opaque-pixel
ratio. Material-path tests alone cannot detect an opaque backing quad.

### Encode production transforms in tests

The two bonus roots are placed at yaw 180 degrees, so local X is reversed in
world space. The intended outward offsets are therefore attack `+0.10` and
health `-0.10` in prefab-local coordinates:

```csharp
float worldOffsetX =
    (Quaternion.Euler(0f, 180f, 0f) * visual.localPosition).x;

Assert.That(
    isAttack ? worldOffsetX : -worldOffsetX,
    Is.LessThan(0f));
```

Record that rotation assumption in the generator report. Use an isolated
preview to inspect construction, but use an actual-scene capture for final
acceptance so the production camera, gate, player, neighboring choice, root
rotation, and screen resolution all participate in the result.

### Verify serialized references and hostile reactivation state

Checking that `BonusChoiceAltarVfx` exists does not prove its private serialized
references survived saving. Load the prefab through `AssetDatabase` and assert
that `glowRoot`, `iconRect`, and both aura entries reference the exact expected
children. Mutate their animated transforms, invoke the re-enable path, and
assert that every authored position, rotation, and scale is restored.

Test reusable interactions from the consumed state as well. The player-choice
path changes the collider from a trigger before disabling the composite root,
so reactivation must restore both flags before activation callbacks can run:

```csharp
public void ReactivateLifetimeObject()
{
    Collider trigger = GetComponent<Collider>();
    if (trigger != null)
    {
        trigger.enabled = true;
        trigger.isTrigger = true;
    }

    GetLifetimeObject().SetActive(true);
}
```

### Keep EditMode fixtures out of the active authoring scene

Every prefab-instantiation test should own a preview scene. Enter `try` before
instantiating so an early exception cannot leak test objects:

```csharp
Scene previewScene = EditorSceneManager.NewPreviewScene();
try
{
    GameObject instance =
        PrefabUtility.InstantiatePrefab(prefab, previewScene) as GameObject;
    Assert.That(instance, Is.Not.Null);
}
finally
{
    EditorSceneManager.ClosePreviewScene(previewScene);
}
```

Preview-scene ownership is necessary but does not neutralize Unity's global
Undo history. If a production editor helper records Undo, give disposable
fixtures an explicit side-effect-free path instead of mixing Undo registration
with `DestroyImmediate` or preview-scene teardown:

```csharp
internal static void ConfigureBonusWallInstance(
    GameObject instance,
    bool recordUndo = true)
{
    foreach (WallScript wall in instance.GetComponentsInChildren<WallScript>(true))
    {
        RuntimeBonusWall marker = wall.GetComponent<RuntimeBonusWall>();
        if (marker == null)
        {
            marker = recordUndo
                ? Undo.AddComponent<RuntimeBonusWall>(wall.gameObject)
                : wall.gameObject.AddComponent<RuntimeBonusWall>();
        }

        if (recordUndo)
            Undo.RecordObject(marker, "Keep Map-Authored Bonus Wall");

        marker.KeepAsMapAuthoredWall();
    }
}
```

Production placement keeps the default Undo behavior. Tests instantiate the
prefab directly into their preview scene, assert that scene ownership, and call
the helper with `recordUndo: false`:

```csharp
GameObject instance =
    PrefabUtility.InstantiatePrefab(prefab, previewScene) as GameObject;

Assert.That(instance.scene, Is.EqualTo(previewScene));
NoryangjinMapToolWindow.ConfigureBonusWallInstance(
    instance,
    recordUndo: false);
```

This keeps both the temporary hierarchy and its mutation history out of the
authored scene. When a newly added Unity assertion appears not to execute,
force a script recompile before trusting the test result; Test Runner can
otherwise run the previous compiled assembly during an import boundary.

## Why This Matters

The reliable unit of verification is not an in-memory component. It is the
saved asset after reload, placed beneath its production transform, rendered by
the representative camera, and exercised through its reuse lifecycle.

This layered contract prevents several false positives:

- assigned fields that are repaired only at runtime;
- local offsets whose world direction changes under parent rotation;
- procedural textures whose material exists but whose alpha is wrong;
- transient TMP materials that render before save but do not persist;
- VFX components with null or incorrect serialized targets;
- active objects whose pickup collider was never rearmed;
- Undo-tracked test children resurrected as authored-scene root objects;
- isolated renders that do not survive the real scene composition.
- replacement builders that create the new VFX but leave obsolete generated
  materials, textures, and dead generation code in the project.

## When to Apply

- Generating prefabs from templates with serialized UI, localization, or VFX.
- Porting shader-style procedural masks into C# texture generation.
- Authoring local layout offsets beneath rotated or scaled production roots.
- Testing pooled or repeatedly enabled visual-interaction objects.
- Running prefab tests in the same editor used for scene authoring.

## Examples

The current canonical `Box_left` altar replaces the old rune-circle stack with
procedural `WaterVortexOuter`, `WaterVortexInner`, and `WaterFoam` layers plus
lightweight circular water droplets. Its builder deletes the superseded rune
materials and `BonusBox_MagicCircle.png` only after saving the replacement
prefab. The actual Noryangjin capture confirms that two nearby water vortices
remain readable together at the mobile Game-view resolution.

The verification stack covered:

- `MonsterGrowthAndMapToolEnemyTests`: `38/38` passed, with no root-level
  `GFX` remaining after Test Runner restored the authored scene;
- `NoryangjinRuntimeCleanupContractTests`: `14/14` passed;
- both C# project builds: zero warnings and zero errors;
- `tools/validate-agent-harness.ps1`: passed;
- Unity console after the focused tests: no errors;
- the active Noryangjin scene remained clean after the actual-camera capture.

## Related

- [Keep world-space wall stat UI camera-facing on turning routes](../ui-bugs/keep-world-space-wall-stat-ui-camera-facing-on-turning-routes-2026-08-13.md)
- [Restore authored bonus choice VFX baselines on re-enable](../ui-bugs/restore-authored-bonus-choice-vfx-baselines-on-reenable-2026-08-15.md)
- [Avoid cross-lane chrome on adjacent world-space bonus choices](../ui-bugs/avoid-cross-lane-chrome-on-adjacent-world-space-bonus-choices-2026-08-28.md)
- [Protect active Unity scenes from broad EditMode test runs](protect-active-unity-scenes-from-broad-editmode-test-runs-2026-07-18.md)
- [Preserve prefab root transforms in Noryangjin map tool placement](../logic-errors/preserve-prefab-transform-in-noryangjin-map-tool-placement-2026-06-02.md)
- [Verify Unity material keywords after bulk outline conversions](verify-unity-material-keywords-after-bulk-outline-conversions-2026-07-02.md)
