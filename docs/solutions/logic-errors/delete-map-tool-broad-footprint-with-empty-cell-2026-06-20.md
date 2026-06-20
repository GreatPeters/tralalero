---
title: Delete map tool broad footprints with the empty cell tool
date: 2026-06-20
category: docs/solutions/logic-errors
module: Unity Noryangjin map tooling
problem_type: logic_error
component: tooling
symptoms:
  - "Selecting the empty cell palette item and clicking inside a large placed object's footprint did not delete the object."
  - "Deletion only worked when the cursor was on the object's encoded anchor coordinate."
root_cause: logic_error
resolution_type: code_fix
severity: medium
tags: [unity, noryangjin, map-tool, deletion, footprint]
---

# Delete map tool broad footprints with the empty cell tool

## Problem
The Noryangjin map tool's empty cell palette item is meant to remove the placed object under the cursor. Large or manually overridden footprints could show as occupied and block placement, but clicking inside that occupied footprint did not delete the object unless the cursor was on the object's anchor cell.

## Symptoms
- The empty cell tool appeared selected, but clicking a visibly occupied grid cell did nothing.
- Large props with anchors outside the clicked cell were collected as overlap candidates, then discarded before deletion.

## What Didn't Work
- Restricting deletion to objects whose name encoded the exact cursor coordinate avoided accidental broad deletes, but it broke the core empty cell workflow for any object whose displayed footprint spans multiple cells.

## Solution
Keep the existing priority rule for exact anchor hits, but fall back to the already-computed overlap candidates when no candidate starts at the cursor:

```csharp
if (anchoredCandidates.Count == 0)
{
    candidates.Sort((left, right) =>
    {
        return GetSelectionPriority(GetPlacedObjectLayer(left)).CompareTo(GetSelectionPriority(GetPlacedObjectLayer(right)));
    });

    return candidates[0];
}
```

The regression test should assert that `SelectSingleCursorDeleteTarget` still prefers an anchor-cell target when present, and otherwise returns a broad-overlap candidate instead of `null`.

## Why This Works
`DeletePlacedObjectsOverlappingCursor` already filters candidates by the real displayed footprint using `GetPlacedObjectDisplayedFootprintCells(...)`. The bug was in the final target chooser: it threw away every candidate that did not start at the cursor coordinate. Falling back to the overlap list preserves the precise footprint calculation while keeping anchor-cell hits as the highest-confidence delete target.

## Prevention
- For map-tool delete/select actions, distinguish candidate collection from final priority selection. Candidate collection should answer "does the cursor overlap this object's displayed footprint?", while priority selection should only choose among those valid candidates.
- Keep tests for both cases: exact anchor hit wins, and broad footprint overlap still deletes when there is no exact anchor hit.

## Related Issues
- [Resolve selected prefab children to Noryangjin map tool placement roots](resolve-selected-prefab-child-to-map-tool-placement-root-2026-06-08.md)
- [Prefer prefab placement previews over SceneView line grids](../developer-experience/prefer-prefab-placement-previews-over-sceneview-line-grids-2026-06-06.md)
