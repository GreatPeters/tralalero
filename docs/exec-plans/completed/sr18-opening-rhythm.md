---
status: completed
date: 2026-09-10
---

# SR18 opening rhythm improvement

Implement the user's requested follow-up to `map-concepts/sr18-pattern-playtest-2026-09-10/README.md` on the existing `map2` branch, preserving the existing uncommitted work.

## Outcome

- Shooting a lamp immediately makes its falling geometry harmless. Restart restores its authored collider states and pose.
- The health HUD visibly represents the numeric current/max ratio, including the SR18 Sliced-image override.
- The opening changes from repeated single-lane obstruction to a readable sequence of slalom, a shootable path, and a choice near a visible hazard. Later bucket pressure coincides with the existing ambush cue.
- Retain roads, scenery, encounter IDs, workbook-owned combat stats, paired reward ownership, corner safety, camera restoration and exit/replay behavior.

## Work and verification

1. Write failing lamp/HUD regressions, reproduce in an isolated test scene, apply minimal fixes, run focused tests.
2. Back up SR18 and use live Editor authoring to change selected early obstacle members and selected reward positions. Preserve original placement records and write a new application record.
3. Check road support, playable lanes, corner margins, workbook reapplication and reset. Verify actual GameView in normal-health runs, with any focused diagnostic conditions labeled separately.
4. Run runtime/editor builds sequentially, relevant regression fixtures and the agent harness validation. Review this session's changes against its backup and update the local documents/learning.

No automatic Highway transition, new enemy type, shared prefab replacement, permanent-upgrade edits or normal-difficulty completion claim is included. Test-earned currency and session speed are restored.
