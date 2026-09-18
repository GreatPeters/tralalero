# Combat feedback revision — 2026-09-14

## Implemented

| User item | Result |
|---|---|
| 1 | Scene-owned red edge flash and pooled red negative health number above the shark; zero per-hit overlay construction. |
| 2 | 74 ranged enemy rows across three chapters: projectile damage ×0.45, speed ×0.85. Right paired firing gates move 7 units later, with a 6-unit minimum warning distance. |
| 3 | Player Awake clears static run projectile duration across scene reloads. ResetState clears weapon attack/rate/count, health bonuses, duration and movement state; existing scene/GameManager helper cleanup and permanent upgrade/equipment ownership remain. |
| 4 | Woman and sword YellowMan use neutral carry/idle and new 1.6-second ControlledSlash clips. Carry mask contains arms rather than overriding torso/head locomotion. Anticipation, cut, follow-through and recovery use bounded humanoid muscle curves; full original clips remain available. |
| 5 | Shared 3.5-unit planar coin collection, vertical separation below 2.5 units, one wallet claim. Highway/RestStop workbook radii match the shared fallback. |
| 6 | Animated white pointing hand and two dark arrows in the three saved chapter lobbies; the actual nested Fill/Hint text is hidden. |
| 7 | 21 paid cosmetics: prices ×10 (200–1000 jewels), effects ×4 (16–48% for percentage items; diver regeneration 0.6 health/second). Three defaults remain free. IDs and ownership keys retained. |
| 8–11 | Standing pole and hole contacts kill. Roadblocks, closed toll barriers, crossing/oncoming traffic contacts kill in road chapters. Oil spins the model while route/camera retain heading. Shot poles become contact hazards after the 0.5-second fall: 30% maximum health, a player-wide 1-second repeat guard. Rigidbody state is restored on retry. Results appear 0.9 seconds after death so the tumble remains visible. Bucket, ship and seagull effects retain their distinct existing roles. |
| 12 | Two original UI concept boards, for visual comparison; the broad UI replacement is not installed. |
| 13 | New ImpactSplash prefab: 12 short red/coral droplets, 0.18–0.42-second lifetimes, fade/shrink and bounded 32-slot reuse. Shared enemy data in all three chapters references it. |
| 14 | Five new original local BGM candidates and 62-second MP3/OGG/WAV loop previews; existing installed track retained. |

The canonical workbook and signed runtime archive are updated. `workbook-verification.json` checks all 229 edited cells/formulas, original cell styles/metadata, and the other 17 package parts byte-for-byte. No scene road layout or enemy placement count was changed by this revision.

## UI reference and concepts

- [Brawl Stars interface screenshots](https://interfaceingame.com/games/brawl-stars/): used to examine large pictorial controls, prominent character presentation and sparse battle information.
- [Habby's Survivor](https://www.habby.com/game/detail/survivor): related one-hand survivor-game context.
- [Concept A](../../tmp/image-previews/combat-feedback-2026-09-14/ui-concept-a.png): bright ocean arcade, lobby/HUD/equipment. Its illustrative enemies and extra decorative buttons are concept art, not requested changes to the actual enemy roster.
- [Concept B](../../tmp/image-previews/combat-feedback-2026-09-14/ui-concept-b.png): mysterious night sea, lobby/HUD/upgrades. This is the cleaner information hierarchy; the already selected ocean/cream palette can be retained when adopting its pictorial controls.

Both boards were generated with the built-in image tool. They are UI proposals, not in-engine screenshots. Generated source PNGs remain in the Codex image directory, with non-overwritten project preview copies above.

## Verification

- Runtime and editor `dotnet build` succeeded. The runtime build reports the existing third-party `SplineSpeed.m_LastIndex` unassigned-field warning.
- `tools/validate-agent-harness.ps1` passed.
- 82 focused native tests passed: CombatFeedbackRevision (5), ObstacleLampSafety (5), OilSteeringEffect (1), CombatPresentationPool (4), PlayerCharacterDefaults (9), EnemyEventController (24), EnemyThrowDirection (9), RoadChapterPattern (20), HighwayChapterIntegration (1), CosmeticShopIntegration (4).
- Two additional legacy suites: 2 pass / 2 fail on old scene counts, before reaching the updated pole assertions. `Sr18ContactPairsTests` expects 3 FatMan entries but the existing paired layout has 6; `Sr18RuntimeFixSceneTests` expects 717 legacy props but has 740. The fresh before-scene snapshot already contains the six FatMan targets. These are pre-existing snapshot mismatches, not claimed passing tests.
- `tmp/combat-feedback-2026-09-14/native-tests.json` contains the focused results. `actual-retry.txt` confirms the real defeat Continue → scene reload returns to HP60, attack12 (including the existing purchased upgrade), rate1.6, count1, duration1.25.
- `sr18-pass3`, `highway-pass3`, and `reststop-pass4` each record full temporary stat reset, exactly7 nearby coins, a retained vertically separated coin, HP42 after18 damage, oil and hole death. The later RestStop images verify separated hand/text, visible oil rotation and an unobscured tumble with fixed camera heading.
- `melee-verified` records eight live samples through move → attack → return with the actual player forward speed explicitly held at0 for inspection. The final sample shows forward-facing controlled attacks. Earlier melee captures are retained diagnostics: inactive prefab clones did not run, and disabling lateral input alone did not stop forward travel, so the moving player caused legitimate facing reversals.
- Actual settled pole contact retained its collider and eventually killed the60-health diagnostic player under sustained contact. Exact initial30% and repeated1-second boundaries are asserted by native tests.

These are directed Unity Editor checks. They are not an ordinary full-chapter progression cohort, an Android build/install, device frame-rate acceptance or subjective BGM listening approval.

## Preservation and reproduction

Fresh disk/live SR18 scene, source C#, workbook/archive and player preferences are under `tmp/combat-feedback-2026-09-14/before/`. Scene edits used official Unity Pipeline APIs; the shell's WindowsApps Unity alias was inaccessible, while the configured official Unity MCP remained reachable.

Final restoration compared all66captured preferences with zero mismatches:3047coins,0jewels and the original purchase/equipment state. Unity is stopped with the clean saved SR18 scene open; protected archive status is Current. The completed execution record is `docs/exec-plans/completed/combat-feedback-2026-09-14.md`.

`CombatFeedbackInstaller.Apply` authors chapter hints, pole physics/settings and the new shared hit effect; `RepairMelee` authors the final clips. Data authoring uses the Artifact Tool, with a surgical OOXML graft preserving unrelated package content. Run `verify-combat-workbook.py` for scope checks. Audio scripts and the original prompt/seed manifest are in the audio index.

Automatic approval review rejected deletion of two experimental GroundedSlash clips because the proposed dependency check covered only three scenes. They remain in Assets; the final controllers use ControlledSlash. This optional cleanup was abandoned, with no effect on the requested gameplay changes.
