---
title: Keep World-Space Wall Stat UI Camera-Facing on Turning Routes
date: 2026-08-13
last_updated: 2026-08-15
category: docs/solutions/ui-bugs
module: Unity Feast bonus wall UI
problem_type: ui_bug
component: tooling
severity: medium
symptoms:
  - "Attack and health icons appeared edge-on, tiny, or merged into the feast tables."
  - "A fixed Canvas rotation worked for one route direction but failed after the Noryangjin camera turned."
  - "The fixed-size three-character 공격력 label overlapped its icon or extended past the Canvas edge."
  - "Keeping the icon beside the label forced it to remain too small at the gameplay camera distance."
  - "Generated prefab preview labels could retain the previous localized stat text."
root_cause: logic_error
resolution_type: code_fix
related_components: [testing_framework, development_workflow]
tags: [unity, world-space-canvas, billboard, camera-facing, prefab-generation, localization, textmeshpro, visual-regression]
---

# Keep World-Space Wall Stat UI Camera-Facing on Turning Routes

## Problem

The Feast of Fortune walls had the correct sprites, text objects, and serialized
references, but their inherited world-space `GFX/Canvas` was almost edge-on to
the gameplay camera. The projected UI collapsed into a narrow white shape and
looked missing or merged into the offering tables.

After the projection was fixed and TMP Auto Size was disabled, the UI exposed a
second layout defect. The three-character `공격력` label extended left into its
sword icon. Moving the label right removed that collision, but the original
Canvas was too narrow and let the outlined glyph mesh overflow its right edge.

The Noryangjin route changes heading. A rotation tuned for one straight segment
therefore became invalid after a turn, and a Play-mode-only correction would
still leave the map-tool editing view wrong.

## Symptoms

- Both icons existed and were enabled, yet measured only about 7-19 pixels wide
  before the camera-facing fix.
- Scaling the Canvas made the bad projection larger without making it readable.
- A fixed rotation looked correct from one approach and edge-on from another.
- With fixed typography, `공격력` overlapped the sword while the shorter `체력`
  label could appear acceptable.
- Separating the icon and label anchors in the original Canvas moved `공격력`
  beyond the right boundary.
- Sprite-reference and RectTransform-only tests passed even while the rendered
  result was unusable.

## What Didn't Work

- Reference-only assertions proved that the icon was assigned, not that it was
  visible from the actual camera.
- Static Canvas rotation plus scale `0.11` made the overlay much too large.
- Reducing static scale to `0.06` improved size but remained route-dependent.
- Changing anchors without fixing projection only resized an edge-on layout.
- Disabling TMP Auto Size provided consistent typography but did not allocate
  enough horizontal space for the longest localized label.
- Comparing only icon and text RectTransforms missed the TMP outline padding and
  the actual positioned glyph vertices.
- Moving the text right without widening the Canvas traded the icon collision
  for right-edge overflow.
- Writing a new localization key while `LocalizeStringEvent` remained enabled
  allowed an older asynchronous refresh to restore the template preview label.

## Solution

Attach a small billboard component to the world-space Canvas and align its full
rotation with the active `MainCamera` in the same scene. It runs in Play mode
and Edit mode so the map-tool preview matches gameplay, but skips persistent
prefab assets to avoid dirtying them.

```csharp
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class WallStatCanvasBillboard : MonoBehaviour
{
    private void LateUpdate()
    {
        FaceMainCamera();
    }

    public void FaceCamera(Camera targetCamera)
    {
        if (targetCamera == null)
            return;

        Quaternion targetRotation = targetCamera.transform.rotation;
        if (Quaternion.Angle(transform.rotation, targetRotation) > 0.01f)
            transform.rotation = targetRotation;
    }
}
```

Make `FeastOfFortuneWallSetup` own the complete visual contract. The generated
Canvas uses scale `0.028`, width `1.55`, and a model-relative position above the
table. The icon is centered above the copy in normalized X `0.25..0.75` and Y
`0.96..1.74`; the centered stat name uses Y `0.49..0.94`, and the centered value
uses Y `0.06..0.48`. TMP Auto Size stays off with fixed sizes `0.35` for the name
and `0.30` for the value.

```csharp
internal const float StatCanvasWidth = 1.55f;
internal const float StatNameFontSize = 0.35f;
internal const float StatValueFontSize = 0.3f;
internal const float StatIconLeftAnchor = 0.25f;
internal const float StatIconRightAnchor = 0.75f;
internal const float StatIconBottomAnchor = 0.96f;
internal const float StatIconTopAnchor = 1.74f;

canvasTransform.sizeDelta =
    new Vector2(StatCanvasWidth, canvasTransform.sizeDelta.y);
iconTransform.anchorMin =
    new Vector2(StatIconLeftAnchor, StatIconBottomAnchor);
iconTransform.anchorMax =
    new Vector2(StatIconRightAnchor, StatIconTopAnchor);
statText.anchorMin = new Vector2(0f, 0.49f);
statText.anchorMax = new Vector2(1f, 0.94f);

statName.enableAutoSizing = false;
statName.fontSize = StatNameFontSize;
statName.horizontalAlignment = TMPro.HorizontalAlignmentOptions.Center;
```

The left wall serializes sword / `공격력` / `+6`; the right serializes heart /
`체력` / `+40`. Regenerate both Feast prefabs after changing the builder so the
serialized artifacts and scene instances inherit the same layout.

When changing the generated localization reference, disable the event while
writing both the key and preview text, then restore its previous state:

```csharp
bool wasEnabled = wall.statNameLoc.enabled;
wall.statNameLoc.enabled = false;
wall.statNameLoc.StringReference.SetReference("AllTexts", key);
statText.text = previewText;
wall.statNameLoc.enabled = wasEnabled;
```

## Why This Works

The first failure was camera projection, not icon loading. Matching Canvas
right, up, and forward axes to the camera keeps the UI square and readable
through route turns and placed-wall rotations. Scene-local camera lookup avoids
using an editor preview camera from another loaded scene, while the persistent-
asset guard keeps `[ExecuteAlways]` safe for prefab assets.

The second failure was rendered layout, not merely overlapping anchor values.
Stacking the icon above the copy removes the horizontal competition between a
recognizable sprite and the longest localized label. The icon RectTransform is
now `0.775` Canvas units wide instead of roughly `0.34`, while the full Canvas
width remains available to both text rows. Measuring positioned TMP character
quads still includes SDF outline padding that RectTransform bounds alone cannot
prove safe.

Temporarily pausing localization removes the race between the builder's new
preview and an outstanding refresh from the cloned template.

## Prevention

- Treat world-space UI as a camera-facing, screen-space visibility, and rendered-
  glyph layout contract rather than a serialized-reference contract.
- In a Preview Scene, force the longest localized label (`공격력`), call
  `Canvas.ForceUpdateCanvases()` and `ForceMeshUpdate(true, true)`, then transform
  each visible `TMP_CharacterInfo.vertex_BL/TL/TR/BR` into Canvas-local space.
- Assert at least `0.02` Canvas units between the rendered label's top edge and
  the icon's bottom edge, assert an icon width of at least `0.7`, and keep the
  rendered text inside the Canvas horizontally.
- Assert the Canvas is above the model and that scene instances do not override
  prefab typography fields.
- Capture Game view at the real `375x666` encounter resolution and after route
  turns; the final layout was verified at that resolution.
- Verify both the localization key and serialized TMP preview text.
- Rebuild generated prefabs after every builder contract change.

## Related Issues

- [Bake generated prefab UI previews and isolate EditMode instantiation](../workflow-issues/bake-generated-prefab-ui-previews-and-isolate-editmode-tests-2026-08-13.md)
- [Isolate dedicated MeshyAI assets from the generic repair sweep](../workflow-issues/isolate-dedicated-meshyai-assets-from-generic-repair-2026-08-09.md)
- [Preserve MeshyAI prefab axis correction in scene builders](../workflow-issues/preserve-meshyai-prefab-axis-correction-in-scene-builders-2026-05-25.md)
- [Protect active Unity scenes from broad EditMode test runs](../workflow-issues/protect-active-unity-scenes-from-broad-editmode-test-runs-2026-07-18.md)
