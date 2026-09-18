# Combat feedback and retry revision — 2026-09-14

Status: completed. Full implementation/evidence: `map-concepts/combat-feedback-2026-09-14/README.md`.

User request: fourteen fixes and creative deliverables, applied to the three playable chapters.

## Implementation decisions

- Damage: red screen edges and a red negative number above the shark, pooled presentation.
- Ranged pressure: reduce workbook projectile damage; inspect launch timing/pose and simultaneous side shots.
- Retry: clear every run-only wall modifier (health, attack, fire rate, projectile count/duration, speed, helpers), retain purchases/equipment/wallet.
- Melee: inspect actual female-boss and sword YellowMan clips, movement and carry blending, then correct the shared path.
- Coins: shared nearby collection with vertical separation and one-shot wallet credit.
- Start prompt: visible animated finger and two directional arrows.
- Cosmetics: tenfold paid prices; substantially stronger explicit workbook effects, no loss of ownership.
- Hazards: upright pole contact lethal; shot pole topples, fallen contact deals 30% maximum health with a player-wide one-second cooldown; hole contact tumbles/falls to death; oil spins the visible character without rotating the route/camera.
- Hit VFX: short readable impact/splash with bounded reuse.
- UI: research references and deliver image concepts preserving the selected ocean/cream direction as one option; a concept is not a shipped UI replacement.
- BGM: five original locally generated mystery tracks and playable previews, retain existing installed music pending listening choice.

## Execution status

All fourteen requested areas are implemented or delivered as the requested creative candidates. Official Unity MCP authored the three scenes, clips and effect assets. Source snapshots and the initial dirty live scene were preserved before edits. Runtime/editor builds and82focused native tests pass; two additional legacy snapshot tests have unrelated object-count mismatches. Three-chapter directed runtime evidence and the actual retry button are recorded. No commit, push, APK build or device installation was requested or performed. Two unselected experimental clips remain because optional cleanup was denied by automatic approval review.

## Verification targets

Focused native tests for retry, coin claim, pole cooldown/death and effect lifecycles. Compile runtime/editor assemblies and run harness validator. Three directed play/review passes spanning SR18, HighWay and RestStop, with saved screenshots and precise limits. Preserve user preferences. Record source provenance and audio signal checks; do not claim phone/perceptual acceptance from Editor tests.
