---
title: Verify imported timing and mesh ownership before Blender previs rendering
date: 2026-09-23
last_updated: 2026-09-23
category: workflow-issues
module: Rest-stop Blender previs
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - Building a long Blender timeline with imported FBX actors
  - Joining procedural meshes that share cached geometry data blocks
  - Reviewing animated environments with roofs and transparent surfaces
tags: [blender, previs, fbx, fps, shared-mesh, camera-occlusion, animation]
---

# Verify imported timing and mesh ownership before Blender previs rendering

## Context

The rest-stop request required one connected environment, a300-second animation, ten exact intervals, and a60-second top-view defense with a90-degree-per-second turn cap. Successful script completion did not prove the native timeline or camera views met these conditions.

## Guidance

1. **Reapply timing after imports.** The existing shark FBX importer changed the scene from24fps to30fps. The generator still wrote7,200frames and reported the constant`FPS=24`, which would have hidden a240-second native timeline. Reassert`scene.render.fps`, `fps_base`, `frame_start`, and `frame_end` after import; then reopen the saved`.blend` in a fresh process and calculate duration from its actual scene properties.
2. **Make the active join destination single-user.** Primitive helpers cached mesh data across objects and zones. Joining into a shared active data block expanded other instances unintentionally, producing11,413,746triangles. Copy the active object's data before`bpy.ops.object.join()`. After this correction the otherwise same scene was832,086triangles. The later32-enemy and camera refinement scene was880,186triangles; do not compare those as a pure optimization delta.
3. **Validate the saved action, not only the simulation.** The seconds-based defense test passed90degrees/s and9degrees/0.1s. The native action was independently sampled for1,441frames to verify fixedXY, top-view camera and yaw speed. Blender's float storage produced90.003752degrees/s at its maximum difference, within the declared0.01degree/s numerical tolerance. Preserve that tolerance and the measured value instead of claiming mathematically exact serialized floats.
4. **Inspect the camera through actual architecture.** Initial frames showed the petrol canopy covering almost the entire frame and the entrance sign covering the shark. Keep the geometry, but key its render visibility for the intended cutaway intervals. The first defense camera was too close to show all approaches; widening it exposed the relevant space. Record that this is a previs camera treatment, not a Unity occlusion implementation.
5. **Review transparency in the chosen renderer.** Low-sample EEVEE dithered glass made refrigerators and food cabinets look like noisy screens. `surface_render_method='BLENDED'` removed this visible noise in Blender5.2.1. Do not assume the shader default is acceptable because the mesh is valid.
6. **Use semantic export checks.** A check for the batched`10_`prefix falsely reported a missing exit: the exit had too few objects per material to be batched and retained`Exit_`names. Verify the actual semantic objects in the imported GLB rather than imposing a batching implementation detail as an asset requirement.
7. **Offset integer frame indices after rounding.** A first impact arrived on a half-frame boundary. Independently rounding`t`and`t - 1/fps` collapsed the hidden pre-key and visible start-key onto one frame, making an impact sphere slowly grow long before the hit. Round the start once, then use`start_frame - 1`for the hidden key. Video inspection exposed the early sphere; numeric transform/turn checks did not cover it.
8. **Unwrap Euler path headings.** `atan2` changes representation at±π. Direct interpolation between those values can create a long spin even when adjacent headings are close. Use a wrapped angular delta and a per-second rate limit, then compare actual native frame orientations. Preserve the exact affected-frame list when patch-rendering; the rest of the master must remain identical.
9. **Review the whole approach, not only the hero time.** The restroom looked clear at236s, but encoded222s/231s frames exposed long front-wall occlusion. A steeper camera was authored over215–253s, with three-second transitions. Six camera-to-body ray probes passed, and the911affected native frames are separately re-rendered. Add the early approach and exit to the visual evidence list; one successful interior frame is insufficient.
10. **Exclude actor hierarchies from static batching even when a limb is idle.** Idle staff had animated torsos but static legs. Checking only animation-data presence merged their legs into environment batches. An isolated cast view exposed the missing legs. Preserve the semantic actor root (`role` here), not just animated descendants. Six actors'3,696leg triangles were restored under their own joints; world vertex error stayed below0.000018m. The staticGLB exporter now excludes every actor descendant as well.
11. **Benchmark native animation rendering before a long batch.** Repeated `render(write_still=True)` calls paid avoidable setup costs in this workflow. A12-frame native `render(animation=True)` canary retained the correct five-digit filenames and averaged0.283seconds between written frames. The final840-frame contiguous interval took170.6seconds. These are task measurements, not a universal speedup claim; preserve exact frame ranges and validate the encoded output after changing render orchestration.
12. **Read game scale before expanding the set.** The next revision was rejected for small facilities and a camera unlike the game. Read native player renderer bounds, capsule/root scale, camera offset/pitch/vertical FOV, and workbook movement values through a read-only scene preview. Keep original scene and dirty state. Unity vertical FOV needs Blender's vertical sensor fit; copying a focal length alone changes composition with aspect ratio. Record game values separately from authored choices, including the user's 60-second defense overriding the game's existing 30 seconds.
13. **Measure evaluated body geometry after animation setup.** The FBX import's initial bounds were not the final posed body bounds. Including a child aim arrow also inflated the player length. The v2 check first reported `[3.0983, 6.1951, 3.3239]` against the Unity reference `[2.7495, 5.1901, 3.2624]`. Restrict the measurement to the body hierarchy, evaluate the dependency graph, transform evaluated vertices into the player root's local space, and apply calibration after pose keys exist. Reopen and measure the same reference frame. Do not claim every walking pose has identical bounds.
14. **Place hazards from the undodged centerline.** Computing a reversing car's location from the already displaced player route moves the hazard along with the dodge and can erase the intended clearance. Anchor the hazard to the centerline, then add player lateral motion independently. For long curved vehicle routes, sample the curve and lane offset each key; a single tangent line is only locally valid. The v2 exit convoy initially drove on grass and was repaired to follow the road curve before the final complete render.
15. **Check transitions across the contract boundary.** A valid defense-only yaw sequence can still snap when it starts. Include at least the preceding native frame in the speed check, and pre-aim over a bounded interval before the holdout. At defense exit, use travel direction for the follow-camera transition instead of inheriting the last combat aim. Keep caption-safe composition in the native camera, then inspect encoded frames with the actual overlays.
16. **Separate fresh render frames from an interrupted batch.** Restarting a renderer while its watch encoder sees old JPEGs can encode a stale interval before the replacement frames arrive. v2 retains the interrupted `frames/` folder and writes final source images to `frames-final/`; the encoder watches only that new directory and forces all ten receipts. Compare concatenated decoded frame hashes against the ordered per-clip hashes after completion.
17. **Give an overview its own far clip.** The gameplay camera's 1,400-unit far plane cut the distant approach road when that camera was moved far back for the full-site view. Set the overview clip range from its distance to the complete bounds. Check the actual overview for missing distant geometry even when the geometry exists in the source and export.
18. **Include shared structure in isolated comparisons.** v1's store floor belonged to the main-building zone, so selecting only store-tagged objects omitted its floor. Comparison-only copies now include the shared structure and clip actual geometry to each room's footprint. Freeze original visibility actions in the review copy before hiding them; otherwise rendering re-evaluates keyed visibility and exposes unrelated walls. Identify roofs by geometry as well as names, since v2's roof was named `Restroom` without a `roof` suffix. Keep the source artifacts untouched and use identical camera scale, orientation, resolution and frame for both candidates.

Once the restroom camera exposed the approach clearly, it also revealed an unnecessary screen crossing the common aisle. Only that32-vertex component was removed, with its0.1×5×2.85m bounds checked before editing. A505-sample centerline ray check over190–253s found no remaining static blockers at the tested height. This is not a swept character-hull collision or gameplay-physics claim.

```python
# FBX import can change the scene's timing.
bpy.ops.import_scene.fbx(filepath=str(source))
scene.render.fps = 24
scene.render.fps_base = 1
scene.frame_start = 1
scene.frame_end = 7200

# Join into owned geometry, never a cache shared with unrelated objects.
objects[0].data = objects[0].data.copy()
bpy.context.view_layer.objects.active = objects[0]
bpy.ops.object.join()
```

## Why This Matters

The v3 movement clarification also required an explicit control contract: an automatic route parent with one local lateral property, zero local forward displacement, and a camera independent of lateral input. A world-position animation can look freely navigable even when its centerline is predetermined. Test extreme lateral and forward inputs against the saved Blender driver/constraints, not just the authored example trajectory. The v3 native probe tested ±999 lateral and 50 forward inputs, retained ±2.2 lateral displacement, rejected forward displacement, and left the camera fixed.

Vehicle variety exposed another axis trap: the imported truck's height exceeded its width, so the previous shortest-axis heuristic laid it on its side. Preserve the source's verified Z-up orientation. Generated bus/truck models also contained support sheets that made their raw width nearly equal to their length; scaling that aggregate box made the body too narrow. Inspect a vehicle lineup, remove the bottom support artifact in Blender copies, and fit the actual body envelope before distribution. Evidence: `tools/reststop-previs-v3/vehicles.py`, `vehicle_evidence.py`, and `outputs/reststop-blender-v3-2026-09-23/vehicle-lineup.png`.

Long renders amplify small authoring errors. A wrong native FPS invalidates every exported duration; a shared-mesh mutation silently duplicates geometry; a correct full environment can still be invisible behind one roof. Resolve these with short representative frames and native action checks before committing the complete frame range.

The v4 feedback rejected the vehicle scale even though imported bounds matched previously selected game-unit targets. A 1.74-unit car beside a 3.26-unit shark can still read too small. Compare vehicles beside unchanged game characters at one fixed camera before treating numeric size as an artistic pass. The chosen 1.8 enlargement also required repacking parking, resizing bays and changing reversing endpoints. Check the enlarged footprints against the route, scenery and the authored player trajectory; do not assume a scale-only edit preserves spatial clearance. Evidence: `tools/reststop-previs-v4/resize.py`, `check_vehicles.py`, `compare_scale.py`, and `outputs/reststop-blender-v4-2026-09-23/vehicle-validation.json`.

## When to Apply

Use this workflow for authored Blender cinematics, gameplay previs, imported animated actors, and large procedural environments. It is not a reason to modify the user's source FBX, Unity settings, or unrelated render processes.

## Examples and evidence

- `tools/reststop-previs/build.py`, `polish.py`, `validate.py`, `verify_export.py`.
- `outputs/reststop-blender-300s-2026-09-23/build-metrics-before-batching-fix.json`, `scene-validation.json`, `fresh-glb-validation.json`.
- `tools/reststop-previs-v2/calibrate_player.py`, `repair_exit.py`, `validate.py`, `render_pipeline.py`, `package.py`; [v2 game-scale revision](../../../map-concepts/reststop-blender-v2-2026-09-23/README.md).
- [Task evidence](../../../map-concepts/reststop-blender-300s-2026-09-23/README.md).
- [Visual evidence beyond numeric checks](validate-pickup-art-in-moving-gameplay-2026-09-20.md).
- [Preserve exact generated-image results](persist-generated-image-before-next-prompt-2026-05-16.md).

Captured with`ce-compound mode:headless`. Existing docs covered visual validation and image persistence, but not the import-timing/shared-mesh failure mechanisms, so overlap was low. No session-history or GitHub issue search was added; GitHub tooling was unavailable. AGENTS.md already describes the searchable knowledge store.
