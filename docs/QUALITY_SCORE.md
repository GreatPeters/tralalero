# Quality Score

Late-ramp follow-up: E23 moved to give 21.75 flat units before contact; its new placement test passes. The subsequent ATT/HP level-3 run (68 attack/145 starting HP, ordinary lateral movement, TimeScale 3) completed SR18 with 25 choices and 26 kills. This is normal-stat automated completion with permanent upgrades, not an unupgraded or human-play certification. Two boosted runs also completed, and the 9999 controls reapply after a fully initialized scene reload.

2026-09-10 release validation: cosmetic 8/8, shared bonus 16/16, run balance 6/6, Editor overrides 2/2, workbook 15/15 and upgrade UI 2/2 pass. SR18 focused checks pass 26/28; two assert historical Map1 hashes that no longer match the existing source. A broad isolated run reported 606/629 pass; its title mismatch was fixed and retested, but the full suite is not green. Other failures include old scene hashes/counts, palette/Undo contracts, canShoot field-vs-property assumptions, old missile speed and synthetic integration fixture setup. Details are retained in `map-concepts/noryangjin-release-2026-09-10/test-results.json`; they are not silently rebaselined. Device performance and human difficulty assessment remain open.

## Snapshot
- Agent readability: C
- Runtime verification: C+
- Pure logic testability: D+
- Documentation coverage: D
- Operational recovery: C-

## Why
- Core gameplay entry points are discoverable, but repo-level operating context was mostly absent.
- Runtime combat harness exists, but only a small amount of pure logic is testable outside scenes.
- The official Unity CLI/Pipeline path is the supported Codex editor-control route and survives Play Mode transitions; removing the CoderGamester bridge eliminates that bridge's reload and port-reuse failures.
- A distinct pre-existing localhost HTTP package, `com.youngwoocho02.unity-cli-connector`, remains installed outside Codex registration and should be evaluated separately rather than conflated with the removed MCP bridge.

## Next Improvements
- SR18's current contact/formation update has49 passing related tests: durable authored lamps, centered FatMan entries, two unavoidable-while-alive Normal pairs and a shared2-second bonus cooldown. Both formations were traversed in a normal-health run after clearing one side. The run ended around98 seconds; full normal-difficulty completion and later balance remain unverified. See `map-concepts/sr18-contact-pairs-2026-09-10/README.md`.
- SR18's shot-lamp damage and health-fill rendering findings were fixed on2026-09-10. The opening now uses alternating Bucket lanes, wider input-response intervals and choices before edge holes. Related23 regressions pass. Human playtest feedback, later encounter pacing and normal-difficulty completion remain open. See `map-concepts/sr18-opening-rhythm-2026-09-10/README.md`.
- After the final normal-health run reset, two stackless Editor exceptions reported "This cannot be used during play mode" around prefab PreviewImporter activity. Their caller remains unidentified; do not conflate them with the verified lamp/HUD fixes or report the entire Editor session as error-free.
- SR18's four live-loop defects were fixed on2026-09-10: guarded projectile returns, camera occluder handling, decorative legacy lamps and an actual clear trigger. The continuous endurance run reached the exit with654 successful shot initializations and no corrupt roots. Normal-health campaign balance, Highway transition and device performance remain separate gaps. See `map-concepts/sr18-runtime-fixes-2026-09-10/README.md`.
- Extract score, damage, and progression calculations into pure helpers.
- Add more edit-mode tests around wave progression and state transitions.
- Reduce duplicated run-state ownership across `GameManager`, `CanvasScript`, and `TimeManager`.
- Keep top-level docs current when the Unity CLI or Pipeline workflow changes.
