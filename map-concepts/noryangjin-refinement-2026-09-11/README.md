# Noryangjin refinement

The existing SR18 road layout, opening slalom and contact formations remain the foundation. This pass changes selected later repetitions and adds small environmental details around existing stalls.

## Changes

- G10 and G19: replace three-bucket stations with oil spills. Contact temporarily reduces steering to55% for2seconds; overlapping spills refresh rather than multiply the effect. Forward travel, route orientation, camera and shooting remain intact.
- G16: replace a bucket station with a telegraphed seagull dive. A shadow appears before the moving bird can hit; the inactive root collider cannot damage the player. The bird leaves after landing.
- Two coastal ships use the existing cannonball setup. Their player lookup and projectile cleanup support scenes without `GameManager`.
-154 small market details use existing boxes, tanks and net assets, with road-overlap rejection;4 moored boats bob subtly and4 animated gulls add harbor motion.
- The original306 stalls and306 quay pieces remain. Roads and turns were not rebuilt.

Exact replacements/additions and current centers: [changes.json](changes.json). The workbook now contains78 SR18 placement rows. `밸런스 조정!B47:D50` controls the added oil duration, gull damage and ship damage.

## Verification record

Before the environmental/mechanic pass, three runs at the user's existing ATT12/HP60 profile reached48.45/48.46/48.44seconds and earned30 coins each. TimeScale3, ordinary movement, no purchases or power cheats. The original57 preference keys were snapshotted for restoration after all testing.

Projected route support, copied road geometry, opening response space, paired enemies, lamp collision and updated placement contracts are covered by49 passing SR18 tests. At ATT33/HP130 with attack-speed level4, three ordinary-input runs produced330.71seconds clear,311.02seconds death and330.67seconds clear. Final contact fixes received another follow-up cohort, retained separately in the [acceptance record](../two-chapter-2026-09-11/README.md).

The directed physics probe observed oil steering at55% with restoration, Seagull damage20 and departure, and Ship damage10 from an active30-unit/s projectile. It exposed and fixed a redundant Seagull child Rigidbody and the cannon's foot-level aim. These are isolated contact proofs, not normal-clear runs.
