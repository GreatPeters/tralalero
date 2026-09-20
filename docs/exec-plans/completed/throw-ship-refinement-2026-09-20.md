# Throw prop and waiting ship refinement

Status: completed. [Applied result and native evidence](../../../map-concepts/throw-ship-refinement-2026-09-20/README.md).

User requested two follow-up changes: keep FatMan's crate clear of his face throughout the throw; keep the ship facing the marked road while waiting and firing.

- Preserve prior combat/talisman edits. Baseline SR18 scene and source prefabs copied to `map-concepts/throw-ship-refinement-2026-09-20/before/`.
- Crate source and six placements: scale 50 → 35; anchor an upper-rim point to the right hand, keeping the smaller crate below the face with the existing animation.
- Ship: authored bow points toward nearest road surface; FirePos +Z follows the hull cannon +X. Fire does not rotate the ship or retarget its direction toward the moving player. Existing 7.5-second gate and world-sized CannonBall are preserved.
- Supersedes the previous predictive ship aim. Regression test now checks unchanged waiting heading, exact muzzle spawn and barrel-aligned velocity.
- Verification: both assemblies build; 13 combat feedback tests pass. Native repeated throw: 3 shots, 0m movement, 0m grip error. Ship: 0° heading change, exact FirePos origin, barrel/velocity alignment 1.0. Prepared/release/ship frames visually checked and gallery verified in Chrome. Editor restored to saved SR18 Edit Mode.
- Review correction: simple downward prop translation looked detached; runtime IK attempts were removed after native pose drift. Final solution fits the mesh rim to the authored hand, requiring no new runtime animation correction.
