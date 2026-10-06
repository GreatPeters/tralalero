# Preserve dirty assets around generator tests

Date: 2026-10-02. Project: Tralalero Shooter. Final Chapter 4/5 verification.

## Symptom and cause

An otherwise read-only-looking full EditMode run changed metadata for 21 shared product assets. `MonsterGrowthAndMapToolEnemyTests.BonusChoiceWaterVortex_ReplacesRuneGeometryWithLayeredWaterEffects` invokes `FeastOfFortuneWallSetup.BuildWallPrefabs()`, which regenerates `Box_left.prefab` from `random_wall_normal.prefab`, rewrites generated materials, and saves assets globally. Palette tests also save assets and can flush TMP dynamic-font caches.

Generated prefab IDs are not stable. Matching object names, file sizes, or a successful test result cannot establish preservation. The generator also reads the current camera and live bonus-wall spacing through presentation refresh, so output can depend on editor scene state.

## Recovery used here

The build guard stopped before building. All 21 current assets were backed up. A font that was proven clean before the suite was restored exactly from its clean baseline; native title/HUD/goal captures verified rendering afterward. Previously dirty materials were compared with actual local dirty-worktree checkpoint blobs rather than overwritten from HEAD.

For the dirty prefab, a real 17:35 checkpoint and the current file had identical serialized properties across 421 documents after canonicalizing local references by full hierarchy path, sibling position and component position. The source template was byte-identical; generator sources differed only in line endings. This was a semantic comparison, not proof of exact bytes immediately before the suite: the 19:59 snapshot was metadata only.

Internal equality was insufficient by itself. A complete scan of 12,828 YAML assets found 31,385 inbound references. No current regenerated ID was referenced, and no reference rebound to another object. The 78 apparently lost targets were all existing null added-component records (`addedObject: {fileID: 0}`) with identical prior scene records, not lost live components. The independent reviewer accepted preserving the current prefab without rewriting IDs. The other 6,994 references already unresolved at the checkpoint were explicitly excluded from the scoped fix.

## Future practice

- Before a broad suite in a dirty worktree, identify tests that generate or save assets. Save exact bytes and hashes of their inputs and outputs, including font caches; metadata alone is insufficient.
- Prefer an isolated checkout or temporary outputs when the test supports them. Do not run a second mutating suite merely to verify the first one touched files.
- Never reset a preexisting dirty asset to HEAD to obtain a clean diff. Use a verified pre-run byte backup; otherwise record the evidence gap and obtain independent semantic/reference review.
- When generated local IDs change, inspect inbound GUID/fileID references and PrefabInstance override context. Distinguish live added components from null deletion records before repairing anything.
- Hash every product file around the final build. This task compared 51,746 files, about 14.32 GB, before and after the corrected build.

Evidence: `outputs/chapters45-2026-10-02/final-verification-20261001T212400Z/shared-side-effect-resolution.json` and `shared-side-effect-evidence/`. The complete native suite retains 53 failures; this preservation work does not turn it into an all-green result.
