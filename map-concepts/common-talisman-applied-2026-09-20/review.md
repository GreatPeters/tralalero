# Common talisman implementation review

Reviewed with ce-code-review mode:autofix against the task's changes from HEAD. Persona concerns were evaluated sequentially in the main thread per project AGENTS.md. Unrelated pre-existing modifications and concept-image archives were excluded; new implementation files were included with intent-to-add.

Intent: replace every positive Bonus presentation with the approved folded paper talisman and a shared torso absorption effect. Preserve existing reward amounts, type selection, rarity, pair/cooldown rules and helper summons.

| # | Area | Finding | Resolution |
|---|---|---|---|
| 1 | Correctness / adversarial | Invalid authored rolls could retain a previously valid talisman. | Invalid-roll handling now hides the new visual; covered by a focused test. |
| 2 | Reliability | Original pickup root is deactivated immediately after reward. | Effect is a detached scene-owned root; native trigger tests verify survival and cleanup. |
| 3 | Visual correctness | Legacy GFX transforms rotate and scale nonuniformly. | Authoring and camera-facing updates normalize world dimensions; a regression test covers the 25x rotated GFX hierarchy. |
| 4 | Editor safety | Reparenting inherited prefab children causes Unity errors. | Existing hierarchy remains intact; only newly created artwork is parented at creation. |
| 5 | Testing / correctness | The glow texture imported as Multiple without any sprite subasset. | Explicit Single import plus a failing-then-passing Resources.Load<Sprite> test. |
| 6 | Reliability | A same-frame run reset can leave feedback alive if the run flag returns to true. | Pickup caches the claim timestamp and cancels if player cooldown state resets; native reset check. |
| 7 | Correctness | Missing artwork resources should not suppress the old fallback effect. | Old cue is retained when no new visual exists. |
| 8 | Maintainability / product | Alias-only labels obscure which statistic changes. | Label uses the data's display name, preserving name/value semantics. |

All findings were safe local fixes within the authorized task. Residual actionable work: none at this review point. Final native captures and regression results are recorded separately in README.md; no performance result beyond Editor validation is implied.

Existing map-authoring rebuild path now calls the new presentation after constructing its data layout. Runtime SetWallSprite also ensures the common visual, so legacy/dropped/future bonuses use the same path. No external services, new package dependencies, purchases, game balance edits or equipment redesign were introduced. Negative walls keep their existing visuals.
