---
title: Resolve selected prefab children to Noryangjin map tool placement roots
date: 2026-06-08
last_updated: 2026-06-12
category: docs/solutions/logic-errors
module: Unity Noryangjin map tooling
problem_type: logic_error
component: tooling
symptoms:
  - "Scale fields can appear to do nothing when a selected prefab child is not the map-tool placement root."
  - "Transform controls may fall back to the cursor target instead of the object the author visually selected."
  - "Editing scale on a placed object may not affect future placements if the value is saved only on the scene instance."
  - "Bulk delete can miss copied or renamed placed objects when it relies only on coordinate suffixes."
  - "Scale edits can target the wrong object when a placed prefab root is under `Roads` or `Props` but no longer has a coordinate suffix name."
root_cause: logic_error
resolution_type: code_fix
severity: medium
tags: [unity, noryangjin, map-tool, selection, scale]
---

# Resolve selected prefab children to Noryangjin map tool placement roots

## Problem
The Noryangjin map tool exposes rotation, offset, height, scale, and delete controls for placed objects. Those controls originally trusted object names too directly: selecting a child mesh inside a prefab instance could leave the panel editing a different object or the cursor fallback, while bulk delete could miss copied or renamed placed objects whose names no longer encoded a grid coordinate. A later scale-specific bug came from applying scale only to the scene instance when the author expected the connected prefab asset root scale to change.

## Symptoms
- Editing X/Y/Z scale in the map tool did not visibly change the object the author expected.
- The selected Scene object could be a prefab child while the actual placed-object root was an ancestor named like `Road_Test_X+03_Z-02`.
- The issue was easy to miss because selecting the root directly worked.
- Future installations could keep using the old prefab root scale if scale edits were not written back to the prefab asset.
- `모두 삭제` could leave manually copied or renamed objects under `Roads` or `Props` because the delete pass only matched names parseable as `..._X+00_Z+00`.

## What Didn't Work
- Adding scale fields alone was insufficient. The fields read and wrote `target.transform.localScale`, but target selection still depended on the exact selected object name.
- Checking only the current cursor position was also insufficient because an author may be editing an already selected object away from the cursor.
- Marking the scene object dirty was not enough for prefab-wide scale editing. It preserved an instance override, not the prefab's authored root transform.

## Solution
Resolve editor selection by walking up the selected object's transform parents until a map-tool placement root is found. A root can be identified either by the coordinate suffix name or by being a direct child of a semantic placement container such as `Roads` or `Props`:

```csharp
internal static GameObject ResolveSelectedPlacedObject(GameObject selected)
{
    Transform current = selected != null ? selected.transform : null;
    while (current != null)
    {
        if (IsMapToolPlacedObjectName(current.gameObject.name) || IsMapToolPlacementRoot(current))
            return current.gameObject;

        current = current.parent;
    }

    return null;
}
```

Use that helper before falling back to cursor lookup, and mark the scene dirty when applying scale changes so the edit persists like the other manual transform controls.

When applying scale to a prefab instance, record both the scene instance override and the prefab asset root write. `PrefabUtility.RecordPrefabInstancePropertyModifications(target.transform)` keeps the selected instance state explicit. Prefer applying the instance's `m_LocalScale` property override to the prefab asset root, and use the heavier prefab-content save path only as a fallback. Treat a failed prefab root write as a failed scale operation instead of silently continuing.

For bulk delete, collect direct children of the placement containers (`Roads` and `Props`) as placed-object roots regardless of their names, then keep coordinate-name matching only as a compatibility fallback for older objects that may live outside those containers. Do not recurse into placement-container descendants as separate delete targets; deleting the direct placed root deletes its child meshes safely.

When the scale field is meant to edit the actual prefab from a selected instance, apply the root transform's serialized `m_LocalScale` override directly:

```csharp
internal static bool ApplyPrefabInstanceRootScaleOverride(GameObject target, string prefabPath, Vector3 scale)
{
    GameObject prefabInstanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(target);
    Transform rootTransform = prefabInstanceRoot.transform;
    rootTransform.localScale = scale;

    var serializedTransform = new SerializedObject(rootTransform);
    SerializedProperty localScaleProperty = serializedTransform.FindProperty("m_LocalScale");
    localScaleProperty.vector3Value = scale;
    serializedTransform.ApplyModifiedPropertiesWithoutUndo();
    PrefabUtility.RecordPrefabInstancePropertyModifications(rootTransform);
    PrefabUtility.ApplyPropertyOverride(localScaleProperty, prefabPath, InteractionMode.AutomatedAction);
    AssetDatabase.SaveAssets();
    return true;
}
```

Avoid calling `AssetDatabase.ImportAsset(prefabPath)` immediately after saving scale from the map-tool UI. In this project, that extra reimport surfaced FlatKit shader keyword-space errors during live numeric editing and could make the field feel like it failed to apply. Use delayed numeric fields for prefab-wide scale edits so the asset write happens once after Enter/focus change instead of on every typed character.

## Why This Works
Placed map-tool objects are identified by their root object names, while imported prefabs often expose selectable child meshes, renderers, and decorative sub-objects. Walking up the hierarchy preserves the user's visual selection while still applying map-tool edits to the one scene object that owns the placement coordinate, prefab link, and saved transform.

Prefab-wide scale is a separate persistence boundary from scene instance scale. Updating `target.transform.localScale` changes the selected instance immediately, but future placement reads `prefab.transform.localScale` and the palette scale multiplier. Saving the prefab root scale makes the user's field edit part of the prefab asset contract instead of a one-off scene override.

Applying a prefab property override from the selected instance is less disruptive than loading and re-saving prefab contents during `OnGUI`. It writes the root scale Unity already understands as an instance override, while avoiding an immediate full asset import pass that can disturb shader state and editor focus.

## Prevention
- For any SceneView map-tool transform control, resolve selection to the tool's semantic root before reading or writing transforms.
- For destructive map-tool actions such as `모두 삭제`, target semantic placement roots under `Roads` and `Props`, not only names that still match the coordinate suffix format.
- Add tests that create a placed root plus child and assert child selection resolves to the root.
- Add tests for copied or renamed placement roots under placement containers so Unity's automatic ` (1)` duplicate suffixes do not bypass cleanup.
- Add tests for scale edits where the placement root has a non-coordinate prefab name but is still a direct child of `Roads` or `Props`.
- Keep individual transform apply paths consistent: record undo, update the root transform, dirty both object and transform, and mark the scene dirty.
- For prefab-wide scale edits, add an asset-writing test that creates a temporary prefab, applies the helper, reloads the prefab asset, and asserts the root `localScale`.
- For live numeric UI, use delayed float fields or another explicit commit point before writing prefab assets; do not reimport prefab assets on every keystroke.

## Related Issues
- [Prefer prefab placement previews over SceneView line grids](../developer-experience/prefer-prefab-placement-previews-over-sceneview-line-grids-2026-06-06.md)
- [Preserve prefab root transforms in Noryangjin map tool placement](preserve-prefab-transform-in-noryangjin-map-tool-placement-2026-06-02.md)
