---
title: Verify Unity presentation and progression beyond numeric checks
date: 2026-09-10
last_updated: 2026-09-23
category: workflow-issues
module: SR18 and Highway presentation and progression
problem_type: workflow_issue
component: development_workflow
severity: high
applies_when:
  - "Changing equipment, humanoid presentation and balance together"
  - "Validating pooled projectile damage or generated character exports"
  - "Measuring progression through repeated real purchases and scene reloads"
  - "Cloning a chapter while replacing its route geometry or reviving unused hazards"
  - "Comparing generated video models and interpreting GPU memory or timing claims"
tags: [unity, validation, animation, pooling, equipment, progression, trellis, spreadsheet, frame-cadence]
---

# Verify Unity presentation and progression beyond numeric checks

## Context

A combined SR18 revision exposed several cases where valid parameters or successful imports did not match the visible game. A lamp could flash without stopping a projectile, a helper's weapon stat could change without changing damage received, a humanoid rig could pass structural tests while sinking or tearing during a throw, and a decimated FBX could pass numeric validation while its silhouette was destroyed.

## Guidance

September13 correction to earlier progression evidence: the old Editor harness could call movement multiple times per game frame. Its historical18/22/14-attempt cohorts retain diagnostic value, but are not sufficient evidence of ordinary keyboard/touch difficulty. Final acceptance requires the frame-bound harness and its input/frame counters below.

### Match automated input to the game's clock

`EditorApplication.update` is not the gameplay frame callback. A five-second live observation measured828Editor updates across248game frames, up to7calls in one frame. `PlayerMove` multiplies its smoothing by `Time.deltaTime`, so repeating it in an unchanged frame multiplied lateral movement and biased balance results. Merely leaving the9999/fast-lateral toggles off did not establish normal movement.

The harness now gates the whole tick before calling movement:

```csharp
if (Time.frameCount == lastProcessedFrame) return;
lastProcessedFrame = Time.frameCount;
```

Every final run records `movement_calls` and `game_frames`; reports reject counts above one call per frame. Seed before the real encounter reroll, preserve actual earned carryover at chapter boundaries, and distinguish the greedy-affordable purchase policy from saving for the next highest-value upgrade. A cheap-purchase loop can starve health upgrades without proving that the game's health prices alone are wrong. Snapshot entry preferences before comparing policies; if reconstructing a measured exit, validate every inferred level against the actual price table first.

The corrected final cohorts begin with `chapter-polish-final-` under the chapter-polish QA evidence. Older cohorts must not be silently relabeled. The active execution log tracks recalibration after this correction.

### Keep evidence callbacks bounded and scene-aware

ScreenCapture queues a later frame. A victory capture immediately followed by Replay produced a lobby PNG even though the reward/visibility assertions had passed. Keep the screen unchanged until the capture has been written. A dedicated capture must also require the final chapter: an earlier chapter automatically travels and never presents its victory panel.

Never repeatedly throw from an EditorApplication.update callback. A temporary capture that expected a victory panel in Noryangjin kept throwing and prevented later Editor handlers/Pipeline requests from being serviced. Unsubscribe on Play exit, destroyed targets, timeout and completion; write the diagnostic once instead of leaving a throwing delegate registered. In the isolated QA incident, all authored state was saved and only the verified task-owned Editor was restarted to discard the dynamic delegate. Original Editor/user preferences were preserved. A generated delegate that does not survive domain reload is not a durable job record.

### Check income and remaining-health contact separately

Enemy contact exchanges remaining enemy health with player health in this runner; the separate damage field drives projectiles. Increasing enemy HP therefore also increases contact cost. A road test that avoided enemies from22m stopped aiming before shots arrived. Calibrate aiming/escape windows against actual movement and use the inner enemy beside an open lane instead of demanding a full-road crossing.

On wider roads, a0.6m coin trigger also missed legitimate kills during safe-lane movement. Optional planar collection was added without enlarging the physics trigger, with a vertical-separation and one-shot guard. Final live checks collect37at3m exactly once while leaving6m/other-floor pickups untouched. Income needs to support the actual next upgrade prices; changing damage alone cannot repair a stalled purchase loop.

### Coordinate dependency pins and platform floors before building

Adding Google Mobile Ads11.5.0updated the shared dependency manager to1.2.187, while an older Firebase build guard still demanded1.2.186and its archive hash. Keep exact pin/integrity checking: verify the new archive against its distribution registry, update the version/hash and documentation, then run the missing/corrupt-package tests. Do not bypass the guard or alter the Firebase destination to get a green build.

The next Android attempt compiled shaders/IL2CPP but failed manifest merge because the project minimumAPI23was below the ad wrapper'sAPI24. Raise the supported floor explicitly toAndroid7/API24and add a pre-build guard before expensive compilation; do not force overrideLibrary to ignore unavailable APIs. The final ARM64APK built with0errors and passed AAPT minimum/architecture and apksigner v2verification. Device ad delivery and production publisher IDs remain separate evidence boundaries.

1. **Trace a value through its consumer.** `EnemyScript_space` previously read the player's current damage for every bullet, ignoring BoomBar's weapon damage. `WeaponScript` now passes a launch snapshot to `BulletScript`, and the receiver consumes it. Retain that snapshot during pool deactivation because the other participant's collision callback may run afterward; overwrite it at the next launch. The physics probe intentionally used13 damage when the player had a different value and observed enemy45→32.

2. **Check contact, feedback and consumption separately.** Four visible opening lamps had disabled collision/damage. Authored lamps were also untagged, so the projectile return code did not consume bullets after a visible impact. All12 active SR18 lamps now have stem colliders, contact damage and impact feedback. Light-component detection also consumes bullets independently of legacy tags. A real10m approach observed100→50 HP; the enemy behind the lamp stayed130→130, verifying that shots stopped.

3. **Inspect the whole animation, including follow-through.** Root-facing alignment did not prove that the head looked at the player. Foot bones provided a useful ground reference without confusing broad skinned-renderer bounds with the current pose. `EnemyGroundedPose` adjusts only the visual hierarchy; humanoid look-at keeps the head attentive. The original FatMan goalkeeper clip tore the torso during its late follow-through, so a derived1.1-second clip preserves its safe wind-up/release and returns to idle. The original FBX remains intact. Shared animator rebuilds must preserve the added layers, IK passes and derived clip.

4. **Make focused probes explicit and effective.** `PlayerScript.movement=false` blocks lateral movement, not forward marching. The first close-up probe therefore walked into and killed its subject. Corrected pose probes disable the player script and freeze its Rigidbody, disable firing, label the9999 test condition, and record both CSV samples and actual frames. These probes certify presentation, not ordinary traversal or difficulty.

5. **Partition equipment semantically.** A height-only shoe partition tinted the low tail. Filtering all leg bones still tinted the rear hip. Terminal foot bones plus an authored shoe-height boundary removed both. Add anatomical landmark checks and inspect the real colored preview. Snapshot matching mounts with `ToArray()` before destroying them; deferred enumeration over descendants can access transforms destroyed with their parent.

6. **Treat export metrics and images as separate gates.** The optional4000-triangle TRELLIS LODs had finite coordinates, UVs and zero degenerate faces, yet visible holes and collapsed silhouettes. They were rejected and removed from Assets; reviewed higher-detail FBXs were retained within the user's allowed budget. Inspect every delivery variant before import or publication. Fresh FBX/GLB import success alone does not certify shape quality or rigging.

7. **Preserve spreadsheet coordinates and unrelated parts.** `getUsedRange()` need not begin atA1. A first tuning attempt shifted formulas by treating its returned matrix as anA1 grid. Restore the verified before copy, let control edits recalculate their dependents, and compare formula addresses/content where preservation is required. Artifact Tool authors the candidate; the targeted graft keeps unrelated workbook parts unchanged. A zero-error calculation scan cannot detect a valid formula written into the wrong column.

8. **Measure progression with real earnings and purchases.** The campaign harness uses ordinary `PlayerMove`, collisions, bonuses and `UpgradeUI.TryBuy`, carrying only earned coins across reloads. It explicitly skips the opening and presses the defeat return action. Keep failed runs and changed tuning versions; a boosted completion or a route-end coordinate does not certify the intended number of attempts. Record game-time scaling and random seeds, and distinguish automated steering from human difficulty evaluation.

9. **Exercise public UI actions across lifecycle boundaries.** The start handler rejects opening/defeat/clear states. Video startup is idempotent, and disabling, skipping or destroying the opening releases its RenderTexture. Stable equipment modifier keys prevent retry stacking. Owned levels, rather than stale stored stat amounts, are reapplied from the current workbook on lobby load.

10. **Validate the mesh's actual opening axis before adjusting attachment offsets.** The Bucket mesh opens along local +Z, not +Y. Its previous `(12.37, 180, 0)` rotation left it almost horizontal; moving it forward made it look suspended from the shark's snout. `ObstacleStats.AttachBucketRoutine` now uses `(78, 180, 0)`, pointing the opening down with a 12-degree tilt while preserving the authored position and world size. Moving the mount back to local Z0.85 covered the body and left the eyes exposed, so SR18 retains Z1.25. Inspect both the gameplay camera and a side view during Walk. `BucketAttachment_OpeningFacesDownOnScaledTurningPlayer` uses the real prefab mesh's rim/base, a scaled player, two route headings and a slope; both cases failed with downward alignment0.214 before the correction and pass above0.95 afterward. The physical-contact probe in `tools/verify-sr18-bucket-fit.cs` also observed attachment, walking, detachment after3 seconds and shooting restoration. It freezes translation and does not certify normal traversal or balance. [Corrected side view](../../../map-concepts/sr18-presentation-progression-2026-09-10/bucket-down-side-2026-09-11.png).

11. **Rebind consumers when replacing cloned scene geometry.** HighWay's copied height follower and camera occlusion component still referenced the destroyed SR18 Roads transform. The scene imported and flat sections worked, but the player remained at Y0.12 and missed an elevated turn. Configure both components with the new root, assert their serialized references, project a sequence of positions across the entire height profile, and then traverse it with ordinary player movement. A valid world coordinate or a flat opening is insufficient proof.

12. **Separate carried props from deforming bodies.** Surface-distance weighting could not make a merged tire/tool behave like rigid equipment. Generate the body separately and attach independent carry meshes to the hand. Retain failed walk renders, inspect all six final poses and rerun fresh-import metrics after micro-cleanup. The final six Highway models have 18-bone rigs, six actions and no reported hard-gate issues; valid metrics alone were never the acceptance criterion.

13. **Verify physics callback ownership and delivery order.** A Seagull child Rigidbody intercepted the callback intended for its parent's ObstacleStats; removing the redundant child body restored the compound-collider event. A roadblock lost damage when the bullet deactivated before its callback. BulletScript now delivers HighwayHazard.ReactToProjectile before pool return, under its existing return guard. The physical probe measured three real weapon shots breaking an 85-health barrier. A ship's target was at the player's feet, below the capsule; aiming at the capsule's center restored its measured 10 damage.

14. **Capture a stable rendered frame before advancing state.** ScreenCapture schedules a later frame. Calling Next or selecting another item immediately afterward captured the new fade with the old movie frame and mislabeled the purchase image. Separate capture and mutation by at least one rendered frame; wait for entrance fades. A Pipeline camera render excludes ScreenSpaceOverlay UI. Tests must also reject paused Play Mode: an Editor error-pause following a Pipeline timeout produced a misleading all-zero contact report, retained as invalid evidence.

15. **Use the video clock without tying playback to gameplay time.** Real movie playback uses UnscaledGameTime, an explicit seeking state and seekCompleted before captions follow the clock. Invalidate the owned RenderTexture before Stop and clear both VideoPlayer and RawImage references. The live probe exercised purchase/re-equip/reload, five rapid replay/skip cycles, seeks and start gating; all persisted state and release checks passed.

16. **Separate a model comparison from a parameter-count claim.** The September12 walk comparison preserved the5B clip and matched the source image, prompts, seed,121 frames, resolution and20-step budget. The14B model still needed its own VAE and sampler/CFG/shift recipe. It took3244.136seconds in the completed run, versus the earlier5B shot's567.35seconds; these are individual workflow timings, not a controlled general benchmark. Retain workflow JSON, model checksums and both clips. Sampled frames still showed ghosting and anatomy changes, so the larger model was not declared uniformly superior.

17. **Read memory-counter definitions before diagnosing paging.** ComfyUI's `get_free_memory` includes unused torch cache in available memory; Windows dedicated residency includes cached allocations. Their different values alone are not evidence of a broken allocator. Disabling dynamic VRAM reduced observed shared allocation in this run, but later steps remained slow. Keep the hypothesis and its limited outcome distinct. Record the sampling window and interval, and label observed peaks accordingly.

18. **Keep latents until decoder and visual review are complete.** The two video samples shared short temporal tiled decoding and showed middle-frame ghosting. Its cause was not isolated. Saving the final latent before releasing the executor cache would permit full/tiled VAE comparisons without repeating expensive diffusion. A `/free` request with `free_memory` may reset the executor cache, so use model unloading separately until decoder QA is finished. Do not attribute a common decoding artifact to model size without testing it.

## Evidence and reuse

Android/Flow follow-up (September12): separate a successful APK build, SDK event emission, HTTP upload receipt, cloud DebugView visibility and the later daily export. The new x86_64 test produced one matched38133ms start/death pair and HTTP204, independently checked in DebugView, while emulator MSAA/FlatKit errors still prevented claiming visual quality. Read current Firebase debug-event guidance before asserting export inclusion/exclusion; the September10 documentation says inclusion is default unless a developer-traffic filter excludes it. Preserve the run ID to reconcile the later sheet row.

For external video generation, inspect each artifact and its billing status. Flow accepted the four-clip plan but generated only scenes01/03;02/04 had explicit provider refusals and no charge. Actual credit balance dropped by200 rather than the proposed400. Downloaded192-frame clips establish completion of those two only. Keep refusal cards, request a substantive design decision for rejected content, and do not treat a chat message saying generation started as proof of four successful outputs.

Flow storyboard correction (September12): the first shoe scene's prompt only asked the shark to snap at the offering, leaving the intended successful theft unspecified. A corrected first frame already grips the shoe; acceptance requires carrying it away through the final frame. For a transformation, an already-transformed first frame cannot establish the requested ordinary-shark beginning. Prepare separate before/after frames and inspect the actual transition and final anatomy.

Filename-only multi-scene requests introduced another failure: Flow's written plan named the right images, but the downloaded clips used different first frames. The escape clip began with the purple deity, and the transformation clip began beside the sunny shrine. Its endpoint also distorted the body and detached shoes. Keep those rejected files and their source evidence. Select images explicitly in the visible asset picker, verify preview names, attach them, and have Flow resolve their actual generation media references. Editor-URL artifact IDs are not generation media IDs: passing one directly caused a media-not-found error. Generate separate scenes and verify the downloaded first frame, not just the agent's claimed source. Explicit attachment corrected the next escape output; it retained the golden shoe while leaving the sunny shrine. Transformation acceptance additionally requires three separate attached feet after the metamorphosis. Prompt wording alone does not guarantee that invariant.

- [Rejected source-mismatch evidence](../../../map-concepts/flow-opening-2026-09-12/revision-2/README.md)
- [Explicit reference attachment prompts](../../../map-concepts/flow-opening-2026-09-12/revision-3/prompts.md)

The corrected interpolation still briefly lost its third foot near the raw ending. Retaining the first6seconds and retiming that continuously animated portion to8seconds removed this late defect without substituting still images; the raw source remains available. This edit does not prove the generator learned stable anatomy. Also compare credit activity with failure banners: a media-not-found attempt displayed no-charge text but still had an unmatched100-credit debit in the Google One ledger at closeout. Report the discrepancy rather than promising a refund.

Installation follow-up: replacing two5-second shots with8-second clips invalidated equal-quarter caption and seek timing. Share explicit boundaries between automatic page selection, Next and prepare-time seeks, and test the frame immediately before each boundary. A concat can have the right frame count yet report a fractional average frame rate; the corrected626-frame movie uses an explicit1/24 output timebase and frame-index timestamps. The preserved trailing242frames were compared against the scaled original (41.476dB average PSNR after re-encoding).

Inspect the movie in the actual tall Game view after installation. EnvelopeParent filled a1080×2340 display by cutting off the carried shoe at the left edge, despite a correct720×1280 source. FitInParent preserves the complete frame and mandatory watermark; hiding the static illustration during movie playback prevents a duplicated background in the letterbox area. Restore that illustration on stop/error for comic fallback. Real Play verification must include natural caption transitions, asynchronous Next seeks and end-of-video cleanup, not only static time-mapping tests. [Installation evidence](../../../map-concepts/flow-opening-2026-09-12/installed/README.md).

Best4 v5 replacement (2026-09-23): the new four silent720×1280/24fps inputs run8/12/10/9seconds, so the current boundaries are0/8/20/30seconds and936frames. Compatible streams can be concatenated with `-c copy`; compare every decoded frame hash against the ordered sources, not only total duration. Preserve the VideoClip `.meta` GUID and verify all three saved chapter bindings through Unity. The old full-frame-only assertion is no longer the current layout contract: `CoastalStoryLayout` now owns the approved chrome and permits up to15% edge crop. Test native aspect and retained area instead of copying the obsolete `FitInParent` assertion from the earlier verifier. [Current replacement evidence](../../../map-concepts/opening-best4-v5-2026-09-23/README.md).

- [Actual Android analytics test](../../../map-concepts/analytics-smoke-2026-09-12/README.md)
- [Flow outputs and provider-refusal boundary](../../../map-concepts/flow-opening-2026-09-12/README.md)

September12 equipment/progression follow-up:

- Fit replacements in the source mesh's bind space, then validate the live rig. The imported shark's default pose is already mid-stride and has four weighted feet. Positioning shoes from that posed hierarchy produced misleading mounts. Four separate ankle loops and per-shoe closed body extensions removed leftover blue cuffs while preserving the tail and keel. A global height cut incorrectly severed seven loops; reject such candidates before import.
- A static preview and a walking renderer need separate visual checks. The neutral preview used a one-time CPU bake after the GPU-skinned preview showed twisted geometry. This workaround was verified; the underlying GPU discrepancy was not isolated and should not be recorded as a proven driver defect.
- Seed before the real run-reset API rerolls authored bonuses. Setting Random.InitState after scene initialization alone did not control initial bonus rolls. The final fresh cohort used real purchases and earnings, reached300.03 seconds on attempt18, and averaged15.92 seconds of progress between first and final attempts. Intermediate plateaus and helper-driven jumps remain part of the observed behavior.
- Preserve high-detail source UVs when replacing a damaged decimation. The hall's35k mesh had roof tears; its65k re-decimation was accepted only after a fresh Unity render using textures extracted from that source. A50k restroom trial did not improve the visible wall slit and was rejected. A higher triangle count alone is not a quality gate.
- ScreenCapture's queued frame also affects shop evidence. Keep the selected item and open panel unchanged until the PNG has actually been written; functional assertions and a mislabeled image can otherwise disagree despite correct runtime behavior.
- Inspect the gameplay camera after scenery work, even when the route is unchanged. The Highway run exposed existing gantry signs crossing the player/corridor view. They lived under Props, outside the road-only occlusion cache. An explicit additional-group binding adds those signs and tunnel arches to the same hide/restore behavior, preserving geometry and colliders. The first per-renderer fix left floating arrows and beams; grouping each authored assembly fixed that visual mismatch. Tests cover child lettering, unbinding and original visibility restoration.

- [September12 skin, progression and rest-stop acceptance](../../../map-concepts/skins-reststop-2026-09-12/README.md)

- [5B/14B walk comparison, timings and limitations](../../../map-concepts/opening-walk-14b-2026-09-12/README.md)

- [Two-chapter production and verification](../../../map-concepts/highway-chapter-2026-09-11/README.md)
- [Physical contact evidence](../../../tmp/image-previews/two-chapter-2026-09-11/hazard-physics-v3/result.json)
- [Shop, video and persistence evidence](../../../tmp/image-previews/two-chapter-2026-09-11/ui-functional/result.json)

- [Implementation, per-request evidence and campaign records](../../../map-concepts/sr18-presentation-progression-2026-09-10/README.md)
- [Highway exports and rejected-LOD boundary](../../../map-concepts/highway-enemies-2026-09-10/README.md)
- [Earlier projectile return guard](../runtime-errors/guard-projectile-return-before-deactivation-2026-09-10.md)
- [Cosmetic preview and preference recovery](verify-unity-cosmetic-previews-and-test-prefs-2026-09-10.md)
- [Choice and post-turn interaction validation](../design-patterns/validate-player-choice-and-post-turn-response-space-2026-09-09.md)

Unity documents that [Animator.bodyPosition should be set during OnAnimatorIK](https://docs.unity3d.com/ScriptReference/Animator-bodyPosition.html); the visual-root ground correction avoids setting it in LateUpdate. [BakeMesh's scale contract](https://docs.unity3d.com/ScriptReference/SkinnedMeshRenderer.BakeMesh.html) also needs an explicit calibration when using imported-scale meshes for measurements.
