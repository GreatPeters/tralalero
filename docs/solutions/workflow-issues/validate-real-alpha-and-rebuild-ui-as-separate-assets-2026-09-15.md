---
title: Validate actual alpha and reconstruct flat UI art as reusable assets
date: 2026-09-15
category: workflow-issues
module: Harbor Workshop UI production
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - "Turning an approved flat game UI image into reusable Unity artwork."
  - "A generated transparency preview contains painted checkerboard pixels."
tags: [imagegen, alpha, sprite-slicing, unity, nine-slice, psd, source-assets]
---

# Validate actual alpha and reconstruct flat UI art as reusable assets

## Context

The user requested revised shop/start UI and asked for an actionable alternative if a flat PNG could not be cut perfectly. Original separated PNGs and PSDs existed under Assets/JH. The first newly generated surfaces and parts looked transparent in the viewer but were RGB images with painted checkerboards.

## Guidance

Inspect image channels before treating a checkerboard as transparency. Preserve original art and reconstruct text-free surfaces/isolated illustrations where the flat image has baked-in labels or occluded pixels. For this request, the user explicitly authorized image cutting and alternative production methods.

Generate a uniform magenta background for isolated objects, then use a reproducible alpha extraction and despill script. Request magenta inside all object gaps, especially spring coils and laces. The extractor retains neutral steel and warm parchment, cuts reviewed rectangles, pads edges and reports transparent/partial-alpha pixels. Inspect every asset over both light and dark backgrounds. Opaque shop/merchant backdrops remain intentionally opaque.

Configure PNGs through native TextureImporter APIs. Eight scalable surfaces use actual sprite borders, not stretched screenshots. Keep ability names, levels, prices, ownership state and chapter locks in UI/data. Live scenes and the shop's changing 3D model are not decorative background sprites.

The image-tool revision approximates the real scene, so a separate deliverable composites the extracted overlay onto an unchanged camera capture. Its PNG overlay and layered PSD can be inspected independently. In this psd-tools path, non-ASCII legacy layer names failed while saving; ASCII layer names with separately rasterized Korean content and retained source strings saved and reopened successfully.

## Why This Matters

A visual mockup cannot recover the original editable layers or hidden pixels exactly. A rebuilt asset kit is useful only when its alpha, stretch borders, text separation and live-content boundaries work outside the generated picture.

## When to Apply

- Do this before promising that generated game screens are ready for import.
- Distinguish direct crops, reused source assets, regenerated alternatives and runtime views in the manifest.
- Use actual Play Mode frames to verify the separate runtime changes. The native green bar measured 1.0 → 0.65 → 1.0 across full health, damage and retry; saved ownership/wallet state was restored after shop label tests.

## Examples

- The video A/B separation extended this workflow with native editable TMP labels and per-button label parents. PSD text remains rasterized; distinguish that from Unity's actual editable text rather than calling every PSD layer editable type.
- Fit a real movie preview with explicit aspect-preserving resize when enlargement is needed; Pillow `thumbnail` only reduces size and otherwise adds unnecessary margins compared with Unity FitInParent.
- Punch a video aperture only through the backing, then recompose foreground frames/gradients. Punching the flattened overlay erases rounded frame corners or subtitles.
- A long Filled Image ignores nine-slice cap protection; use a straight center-strip sprite for the fill and a separate sliced track.
- Native PreviewScene renders need the camera's matching `scene` and `overrideSceneCullingMask`. Structural assertions passed even while the first output was blank; add an image-content gate and visually inspect the result. See [video asset evidence](../../../map-concepts/harbor-video-ui-2026-09-15/production/README.md).

- `tools/extract-harbor-ui-sprites.py`: retained-source crop/key contract and light/dark alpha evidence.
- `tools/import-harbor-ui-art.cs`: 35 sprite imports and 8 native nine-slice borders.
- `tools/package-harbor-ui.py`: 35 sprite review layers and a 27-layer actual-scene start composition.
- Official Pipeline `eval_file` executes statements in a generated method body. This install script was corrected from a full class to qualified statements; the rejected eval did not change scenes.
- In this Pipeline version, `capture_game_view` normalized even an absolute destination into Assets. Use an explicitly absolute `ScreenCapture.CaptureScreenshot` call for workspace evidence and inspect the actual returned path.

## Related

- [Artifact and verification index](../../../map-concepts/harbor-ui-production-2026-09-15/README.md)
- [Preserve gameplay intent and true level limits](../design-patterns/preserve-play-intent-and-data-limits-in-ui-concepts-2026-09-14.md)
