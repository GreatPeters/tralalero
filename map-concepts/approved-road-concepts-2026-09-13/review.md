# Scoped implementation review

Mode: ce-code-review autofix workflow, conducted sequentially in the main thread per the repository tool mapping. No independent reviewer agents were dispatched. Scope is this task's explicit plan and edits since its fresh before-snapshot, not all of the heavily modified `map2` branch. No commits, pushes or PR changes were requested/performed.

Reviewed correctness, maintainability, project standards, test coverage, performance, Python generation scripts, native authoring/state safety and agent-callable entry points. Model/scene visual review is additional evidence and not replaced by source review.

| Finding | Resolution | Evidence |
|---|---|---|
| P1: local camera height ignored inherited1.5scale, placing initial roof below the actual camera | Raised structural roof/clerestory while preserving the camera; bound complete sign/shutter assemblies for occlusion | Rejected pass1 screenshots retained; final camera/roof native test and later gameplay |
| P1: holdout camera changed position, rotation and FOV | Removed camera interpolation/look-at; retained visual-only body aim | Failing regression reproduced before fix; four-direction test passes afterward |
| P2: legacy integration used pre-curve builder coordinates and old turn pads | Sample the authored HighwayRoute; restore an8m supported spawn approach | Integration initially failed at420m, then passed against actual curves |
| P2: stretched outer-curve wall gaps and dim lit line paint | Fit wall/rail lengths at their actual lateral offsets; use unlit road paint | Highway presentation revision, route-support tests and live screenshots |
| P2: repeated helper invocation could start overlapping play loops; invalid Highway warp validated after run start | Reject an already-active run and reject Highway section warp before Start | Local deterministic guard changes; later live invocation remains to be covered by final run |
| P2: rig source required newer Blender than system4.4 | Used installed5.2.1; retained original source and failed log | Nine GLB/FBX fresh validations and native pose sampling |
| P2: large counter evidence appeared dark because studio light distance scaled but wattage did not | Normalize evidence lighting by squared extent without changing model/camera | Both original dim and corrected lit multiview sets retained |
| P3: unused imports/local bounds calculation | Removed dead work | Scoped source inspection |

No unresolved blocking source findings identified. Scene authoring remains guarded against repeated initial installation and requires saved Edit Mode. The TRELLIS helper used a task-owned backend, verified identity/empty queue, sequential generation and task-specific outputs; owned processes were stopped after completion. Final full-route directed traversal and exact63-entry user-state restoration completed successfully. Later ordinary-health live invocation also exercised the added playtest guard.

Limits: this is not Android device certification or a20-run growth cohort. Expanded hall approach distances can affect difficulty even though combat/workbook values are unchanged. Existing shader/text warnings remain separate from newly introduced errors. The visual result implements the reviewed structure and reuses project assets; it does not establish pixel-identical reproduction of generated concept art.
