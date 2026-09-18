# SR18 activation binding and signal gantry repair

User request U89: recover the activation exception after moving two enemies and make the moved harbor gantry stop obscuring them. The user-authored enemy positions remain `(-11.84,0.12,-86.75)` and `(-9.64,0.12,-86.75)`.

## Cause and correction

`SR18_L_E01_Activation` pointed at the right enemy. `SR18_L_E01_Activation_Right` pointed at both enemies. This gave the left actor a two-target spot and the right actor two owning spots, violating the workbook's one-to-one contract. The two canonical spots now point at their corresponding single actors. All50enemy/50spot bindings were checked.

`WorkbookEnemyAssignment` enforces this contract at the map-tool edit boundary for workbook-managed actors. Re-clicking a managed link keeps it; selecting an actor owned by another occupied spot swaps their targets with Undo support. Empty destinations transfer ownership. Ambiguous existing groups are rejected before mutation, and managed spots must remain under the correct map's Props subtree. Generic multi-target assignment remains available for non-workbook content. The UI explains the difference.

Runtime validation remains enabled. Duplicate ownership now produces the same useful InvalidDataException with spot/target counts instead of an unrelated LINQ SingleOrDefault exception.

## Gantry and nearby scenery

- Kept the gantry's X/Z position at approximately`(-10.73,-110.33)`.
- Reduced scale480→240: width16.75→8.375m, height12→6m, fitting the pier width. Raised the buried base fromY-2.20toY0.02.
- Disabled its decorative colliders so it cannot block movement.
- Restored the camera occlusion component's missing player/road references and registered the gantry as an explicit scenery group.
- Explicit props can now hide even when a combined mesh includes ground-level posts. Road candidates retain the overhead-only restriction, preserving the walkable deck. The entire gantry restores when it no longer intersects the view.
- Disabled the exact duplicate decorative perch`Prop_008_STAGE01_NRY_PROPS_008_Seagull_perch_post_X-64_Z-451 (1)` after comparing its transform, mesh and materials against the original.

## Validation

- Native tests:5WorkbookEnemyAssignmentTests,4NoryangjinCameraOcclusionTests and6existing EnemyAssignment tests passed.
- Editor and runtime build passed; the pre-existing SplineSpeed warning can appear on a cold compile.
- `tools/verify-sr18-placement-repair.cs` verifies all100workbook placement rows, repeated run preparation, all50one-to-one bindings, independent first-pair activation, consumed-trigger reset, grounded-gantry hide/restore and non-blocking decoration.
- Actual renderer comparison and reports: `tmp/image-previews/sr18-placement-repair-2026-09-15/`; final captures are in its `final/` folder. The visible-reference frame deliberately disables occlusion for comparison.
- The real map-tool dispatch was also invoked on the currently assigned actor; it preserved its single link.

## Recovery

The initial scene was dirty. Both its on-disk version and unsaved content were preserved before saving and repair under `tmp/backups/sr18-placement-repair-2026-09-15/20260915-124939/`. A fresh66-key preference snapshot records141coins/0gems. `tools/repair-sr18-placement.cs` reproduces the scoped scene authoring; it does not move the enemies or rewrite the workbook.

Final native run reported zero console errors. All66 snapshotted preferences were restored and compared with zero mismatches, retaining141coins/0gems. The repaired scene was reopened in Edit Mode after verification.
