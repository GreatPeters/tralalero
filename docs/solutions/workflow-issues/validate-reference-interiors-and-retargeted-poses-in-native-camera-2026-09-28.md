---
title: Validate reference interiors and retargeted poses in the native camera
date: 2026-09-28
category: workflow-issues
module: Noryangjin interior and Meshy merchant import
problem_type: workflow_issue
component: development_workflow
severity: high
applies_when:
  - A generated scene compiles but fails the supplied visual reference
  - Reused assets have unsuitable silhouettes or embedded tools
  - Generic animation clips change imported character scale or penetrate the floor
tags: [unity, meshy, reference, animation, grounding, camera, blender]
---

# Validate reference interiors and retargeted poses in the native camera

## Context

The user rejected the first market interior as crude and unlike the reference. Reusing small dark tanks, brown divisions and an old armed humanoid as a driver preserved the wrong visual identity. Successful mechanic tests did not establish visual acceptance. The correction generated six dedicated Meshy assets, rebuilt the hall presentation, and checked the actual portrait game camera.

## Guidance

1. **Read the reference as construction and density.** The reference calls for continuous glass aquaria, stainless fish counters, foam boxes, small blue signs, a white ceiling with pendant lights, and a warm wet aisle. Compare native camera captures to these visible properties, not to model counts or an attractive concept alone.
2. **Inspect complete combined meshes.** Brown posts remaining between the new tanks belonged to old road meshes with 27m-tall bounds. Filtering props by root position missed them. Hide intersecting road renderers while inside, preserve their colliders, and restore the original enabled state on exit/disable. `NoryangjinInteriorV2Tests` verifies restoration and collider preservation.
3. **Keep near-camera display panels out of the foreground.** The general scenery fade retained 40% opacity. A TV just ahead of the camera then covered the game with a huge translucent, inverted face. Side-mounted displays and an explicit ahead-of-player visibility rule fixed the native capture. Hiding only the picture leaves floating text; operate on the whole authored display group.
4. **Check lighting on the imported model, not just in a neutral renderer.** FlatKit stepped additional lights blew out faces and aprons under the new local lamps. Scene-specific URP Lit materials preserve the generated base map and avoid changing unrelated outlined assets. A procedural pendant with opposing triangles sharing vertices produced cancelling normals and a crumpled-metal appearance; one outward-wound surface restored a smooth dome.
5. **Validate Unity's evaluated poses after conversion.** Blender FBX phase renders looked coherent, but the actual Unity generic clips changed the nominal 2.3m body to about 3m and put the lying body up to 0.46m below the origin. Normalize the evaluated idle pose, then sample each new merchant clip at 30Hz. Add only the lift needed to keep the evaluated minimum above the ground through the hips translation curves. Keep positive airborne motion. Scope this finishing step to N13/N14, regenerate from the original imported clips before finishing, and measure again after saving/reloading the prefab. An iterative measured fit avoids assuming parent-scale changes produce a single linear change in an imported skin's evaluated vertices.
6. **Retain the complete death lifecycle.** `EnemyScript_space` uses the merchant's actual 2.5s die clip duration. A Play fixture keeps both merchants active during the fall and at about 2.3s after death, then confirms both are inactive after completion. Keep this lifecycle proof distinct from manually sampled animation poses.
7. **Record anchored UI coordinates.** A prefab world-space Canvas retained its old anchored position even when a builder assigned Transform.localPosition. Set RectTransform anchors, anchoredPosition3D, scale, and prefab overrides explicitly before saving.

## Evidence and limits

- Six fresh-import heroes and contact sheets, ten FBX action phase sheets: `outputs/noryangjin-interior-v2-2026-09-28/asset-review/`.
- Unity 70-pose before/after audits: `native-motion-before-grounding.json`, `native-motion.json`. Final sampled minimum Y is above zero; evaluated idle heights are 2.3m.
- Real Game-view death sequence and lifecycle: `tmp/image-previews/noryangjin-interior-v2-2026-09-28/native-death-045529/`.
- Before accepting an installer run, inspect `data.result.success`, saved-scene state, console changes, and a new Play capture. Rebuilding this scene can emit Editor-only URP `ZBinningJob`/null exceptions with empty stacks; cached reflection imports did not eliminate them. No subsequent Play errors were observed in the recorded complete runs or death fixture. The rebuild exception remains an operational limitation, not a proven asset defect or a claimed engine fix.
- Blender 4.4's operator proxy made `hasattr(bpy.ops.wm, 'fbx_import')` misleading. Use the available `bpy.ops.import_scene.fbx`, `--python-exit-code 1`, and absolute render output paths. Relative render paths previously wrote PNGs under `C:/outputs` although the metadata was in the D: workspace.

## When to apply

Use this sequence whenever generated characters/props are retargeted into a reused scene: reference review, model/animation inspection, native integration capture, focused lifecycle checks, then route regression. Keep failed visual passes as evidence and make the gallery's default view the latest actual scene.

## Related

- [Validate branches beyond trigger/root positions](validate-noryangjin-branches-beyond-trigger-and-root-position-2026-09-28.md): moderate overlap on combined road bounds and native proof; this record adds reference fidelity, lighting and imported pose corrections.
- [Meshy import with a budget ledger](import-meshy-characters-vehicles-with-budget-ledger-2026-09-25.md).
- [Imported timing and mesh ownership in Blender](verify-imported-timing-and-mesh-ownership-before-blender-previs-render-2026-09-23.md).

Captured with `ce-compound mode:headless`, sequentially per AGENTS.md. No session-history or issue search; AGENTS.md already points to docs/solutions.
