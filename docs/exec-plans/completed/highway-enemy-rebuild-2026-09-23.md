# Highway enemy reconstruction and mandatory encounters

Status: completed in the project and verified in the live Editor. The preceding readability/bonus fixes and original user save are preserved.

The user rejected the resized highway characters, reported no visible animation and the ability to bypass living enemies, and requested an automated reconstruction from better references with animation. Original source lineage is TRELLIS 2; the immediately preceding work only resized existing assets. Do not describe that as a fresh generation.

1. Inspect the live attack/animation bindings and bypass/finish rules. Clarify all-row versus final-boss-only enforcement; independently begin the asset work.
2. Generate six isolated references with distinct human silhouettes and the previously requested three-head proportions. Use the installed local TRELLIS 2 engine, preserve raw outputs and validate shape before rigging.
3. Author fitted deformation rigs, rigid hand equipment and readable idle, locomotion, attack, hit and death actions. Verify evaluated motion and exports.
4. Replace the six highway source visuals and saved placements using native Unity authoring. Retain combat IDs/stats, labels, pooling and earlier unrelated work. Bind every controller to the new rig and verify native motion, not only clip existence.
5. Prevent bypass of the intended enemy rows using visible, route-aligned engagement semantics. Preserve hazard avoidance and authored recovery bypasses as separately intended mechanics. Validate death/clear/reset behavior.
6. Test real left/right attempts, combat/motion at 1x, relevant regressions and state restoration. Deliver a native video/gallery and record the rejected approaches and reusable lessons.

Production artifacts: `outputs/highway-enemy-rebuild-2026-09-23/`; evidence: `tmp/image-previews/highway-enemy-rebuild-2026-09-23/`; authored references/contract: `map-concepts/highway-enemy-rebuild-2026-09-23/`. No new merge/push/phone deployment is inferred.

## Completion evidence

- Six newly generated TRELLIS bodies, v3 deformation rigs and authored project tools installed in six source prefabs and 50 HighWay placements. One Animator per scene actor, all 34 ranged bindings valid after save/reopen. The 36 shared source users in RestStop were read-only audited: one rig and valid projectile references, without saving or changing that scene's gameplay.
- Seven imported actions per body; actual native playback, source/fresh-export geometry and full multi-view images checked. Wide-vest deformation was repaired through torso anchoring and weighted-surface smoothing. Bodies compensate the existing 0.1m placement clearance to align their soles with the road.
- Visible combat passages, lane-preserving patrols and one-contact-per-pair behavior applied. Final left/right/center input probes each reached enemy-contact gameover. An ordinary traffic-enabled clip also reaches enemy-contact gameover. The green recovery branch remains intentional.
- All four controlled firing probes release at the final hand/muzzle position with 0m position error and apply attack100 as HP500→400. Held pose is retained for non-gun throws; Arrow2 keeps its flight-axis orientation.
- 146 focused EditMode cases pass; runtime/Editor builds, harness and source/doc whitespace checks pass. Native console is clear after the repaired scene-binding checks and final gameplay captures.
- Original66preference records were compared after restoration with zero mismatches. Wallet731/0, levels15/8/1 and absent TutorialDone retained. HighWay is clean in Edit Mode. Protected workbook archive is current; workbook SHA-256 remains `4ccf8641bd5fb165846206ce82e84f8c4bdf2b4dd1b4713cb7abe0328800eee0`.
- A verified browser gallery contains the actual gameplay clip, six native animation videos and the new generation references. The local TRELLIS backend was stopped after its queue emptied; app settings were not changed.
- `ce-compound mode:headless` recorded the duplicate-rig, missing-projectile, moving-gap and skin-weight lessons in `docs/solutions/integration-issues/rebuild-enemy-prefabs-without-stale-scene-rigs-2026-09-23.md`.

Full report: `map-concepts/highway-enemy-rebuild-2026-09-23/README.md`. The current task does not claim a new20-run campaign matrix, full highway balancing or device FPS measurements.
