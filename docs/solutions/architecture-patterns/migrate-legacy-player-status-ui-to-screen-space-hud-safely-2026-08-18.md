---
title: Migrate Legacy Player Status UI to a Screen-Space HUD Safely
date: 2026-08-18
last_updated: 2026-08-23
category: docs/solutions/architecture-patterns
module: Unity Player Status HUD
problem_type: architecture_pattern
component: development_workflow
severity: medium
applies_when:
  - Replacing player-child status widgets with a centralized screen-space HUD
  - Supporting scenes where replacement HUD references may be absent or partially configured
  - Wiring player and Canvas references through Unity editor automation
  - Verifying scene scoping, runtime fallback, and Undo behavior
related_components:
  - Unity Canvas
  - Unity editor scene tooling
  - Legacy player-child UI
tags: [unity, player-hud, screen-space-ui, legacy-ui-migration, canvas-binding, scene-scoping, undo, play-mode-testing]
---

# Migrate Legacy Player Status UI to a Screen-Space HUD Safely

## Context

The Noryangjin gameplay scene replaced the player-child health Canvas and the
top-level `ATT` text with a compact screen-space `PlayerStatusHUD`. Its current
dark-glass cards use high-contrast white type, a coral health fill, and a shorter
attack card while preserving legacy displays as a fallback for scenes that do
not have the replacement.

The implementation and review exposed several failure modes that apply to any
incremental Unity UI migration:

- A replacement component can exist while one or more serialized child
  references are missing.
- New HUD updates can accidentally depend on the legacy widgets they replace.
- Global object lookup can bind a player to another additively loaded scene.
- An editor builder can create and destroy objects through Undo while forgetting
  to record serialized reference changes on existing objects.
- A gameplay recovery path can blindly reactivate the legacy Canvas after the
  new HUD has become authoritative.
- A preview-scene builder test and a Game View screenshot can both pass while
  the saved authored scene still contains stale bindings or styling.

## Guidance

### Validate the complete replacement contract

Treat component presence and component readiness as separate facts. The legacy
UI should be hidden only when every required HUD reference is valid:

```csharp
public bool IsConfigured =>
    healthValueText != null &&
    healthFill != null &&
    attackValueText != null;
```

`CanvasScript.HasPlayerStatusHud` uses this contract. A partial
`PlayerStatusHud` therefore leaves the legacy attack text and player-child
health Canvas available rather than suppressing every status display.

### Report runtime state independently of legacy widgets

Legacy widgets are optional consumers, not the event source for their
replacement. `PlayerScript.RefreshHealthUI` tracks the last reported current
and maximum health separately from `healthText` and `healthBar`. It notifies
the screen-space HUD whenever either value changes even when both legacy
references are null.

```csharp
bool statusChanged =
    force ||
    !Mathf.Approximately(lastReportedCurrentHealth, currentHealth) ||
    !Mathf.Approximately(lastReportedMaxHealth, maxHealth);
```

### Bind ownership explicitly

The editor builder already knows the intended `PlayerScript` and
`CanvasScript` pair. It binds them explicitly and serializes that relationship
instead of making runtime scene-wide search the primary ownership mechanism.
`CanvasScript` then owns HUD visibility, legacy `ATT` fallback, and the
player-child Canvas replacement policy.

Temporary gameplay effects must restore the UI through the same policy. For
example, obstacle recovery calls:

```csharp
playerScript.EnsurePlayerChildCanvasVisible();
```

It does not call `SetPlayerChildCanvasVisible(true)`, which would resurrect the
legacy display under a valid screen HUD.

### Scope editor lookup and make rebuilding idempotent

`PlayerStatusHudBuilder` searches only the validated target scene's root
objects. This prevents an additive source or preview scene from receiving the
HUD accidentally.

The generated root has the stable name `PlayerStatusHUD`. Rebuilding removes
only that root, creates one replacement, and rebinds the Canvas. The stable
automation command is:

```text
Tools/Shooter Survival/UI/Apply Player Status HUD
```

The menu path is pinned by a test because docs and agent automation depend on it.

### Include serialized references in the Undo transaction

Undo coverage must include existing objects whose references change, not only
new or destroyed hierarchy objects:

```csharp
Undo.RecordObject(canvas, "Configure Player Status HUD");
Undo.RecordObject(player, "Configure Player Status HUD Canvas");
canvas.ConfigurePlayerStatusHud(hud, player);
```

The rollback test starts with a player bound to one Canvas, builds the HUD on
another Canvas, reverts the Undo group, and verifies that the original binding
returns.

### Verify the generated and saved UI artifacts

The first heart edit baked a checkerboard into RGB pixels. A second background
extraction produced real 32-bit alpha and was the only output copied into the
project. Inspect alpha before import, configure the texture as a single Sprite,
disable mipmaps for the small screen-space icon, and keep the selected mockup
under an Editor-only reference path.

Builder tests in a preview scene verify what the generator creates, but not what
Unity persisted into the production scene. Add a focused EditMode contract test
that loads the canonical scene additively, inspects only that scene's roots, and
restores the previous active scene in `finally`. Close the target only when the
test opened it.

```csharp
Scene previousActive = SceneManager.GetActiveScene();
string path = NoryangjinForwardGameplayInstaller.TargetScenePath;
Scene target = SceneManager.GetSceneByPath(path);
bool openedTarget = !target.IsValid() || !target.isLoaded;

try
{
    if (openedTarget)
        target = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

    CanvasScript canvas = PlayerStatusHudBuilder.FindInScene<CanvasScript>(target);
    PlayerStatusHud[] huds = target.GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<PlayerStatusHud>(true))
        .ToArray();

    Assert.That(huds, Has.Length.EqualTo(1));
    Assert.That(canvas.HasPlayerStatusHud, Is.True);
}
finally
{
    if (previousActive.IsValid() && previousActive.isLoaded)
        EditorSceneManager.SetActiveScene(previousActive);
    if (openedTarget && target.IsValid() && target.isLoaded)
        EditorSceneManager.CloseScene(target, true);
}
```

Assert semantic presentation rather than the complete scene YAML: sliced dark
panels, light text, dark track, coral fill, panel shadow, a narrower attack card,
and absence of the removed rivets. Also verify the serialized
`CanvasScript.playerStatusHud` reference points to the single configured HUD.
This catches saved-scene drift without coupling the test to harmless file IDs or
serialization ordering.

Run Unity-generated project builds sequentially because both projects write to
the shared `Temp/bin` output:

```powershell
dotnet build Assembly-CSharp.csproj -nologo
dotnet build Assembly-CSharp-Editor.csproj -nologo
```

## Why This Matters

This pattern keeps an incremental UI migration safe under mixed project state.
Incomplete authoring cannot remove the only readable status display. Runtime
health and attack remain independent game state with optional UI consumers.
Explicit ownership prevents cross-scene binding, idempotence makes the builder
safe to repeat, and complete Undo coverage makes failed installation recoverable.

It also turns a visual scene edit into a stable agent workflow: the menu path,
asset dependencies, saved hierarchy contract, focused tests, and operator
documentation are all versioned.

## When to Apply

- Replacing a legacy Unity HUD without deleting the fallback immediately.
- Building scene UI from an editor command or installer.
- Supporting partially serialized replacement components.
- Loading multiple scenes additively during authoring.
- Temporarily hiding and restoring player-attached UI during gameplay.
- Introducing generated raster assets into a production UI.

## Examples

The focused verification contract covers:

- health formatting, clamping, and fill;
- attack rounding and legacy `ATT` fallback;
- partial-HUD fallback;
- health updates without legacy widget references;
- destroyed HUD reference recovery;
- correct child-Canvas visibility;
- target-scene-only lookup;
- idempotent rebuild;
- stable menu path; and
- Undo rollback of the player-to-Canvas binding.

Play Mode verification additionally confirmed live values and produced the
current actual-game capture at
`Assets/ShooterSurvival/UI/References/Editor/PlayerStatusHud_DarkGlass_Reference.png`.
The earlier `PlayerStatusHud_SlimCards_Reference.png` remains historical design
evidence. The focused HUD suite now includes
`SavedNoryangjinMap1_HasOneBoundModernDarkHud` and passes 12/12 tests, covering
both generated preview-scene output and the serialized Map 1 contract.

## Related

- [Transactional reference-scene gameplay composition](../integration-issues/transactional-reference-scene-gameplay-composition-2026-07-23.md)
- [Bake generated prefab UI previews and isolate EditMode instantiation](../workflow-issues/bake-generated-prefab-ui-previews-and-isolate-editmode-tests-2026-08-13.md)
- [Protect active Unity scenes from broad EditMode test runs](../workflow-issues/protect-active-unity-scenes-from-broad-editmode-test-runs-2026-07-18.md)
- [Stop Play Mode before running Unity EditMode tests](../workflow-issues/stop-play-mode-before-unity-editmode-tests-2026-08-15.md)
- [Noryangjin gameplay and map-tool guide](../../noryangjin-gameplay-maptool.md)
