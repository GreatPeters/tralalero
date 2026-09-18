# Two-chapter work log

## Delivery record

The acceptance table is [map-concepts/two-chapter-2026-09-11/README.md](../../../map-concepts/two-chapter-2026-09-11/README.md). It supersedes intermediate pending notes from production. All U1–U7 implementations are present; final playback/traversal and restoration evidence are collected in verification.json.

- U1: generic map-tool menu, chapter palette selector, separate scene navigation and Editor-only currency grants implemented. Live100/100 grant, negative/overflow rules and chapter classification verified.
- U2:29/29 latest TRELLIS props imported after exact SHA256 comparison found0 old matches. All six enemies have18-bone rigs and six combat actions. Final clean fresh-import checks pass without reported issues. Rest/walk multiviews were inspected.
- U3: shop and story rebuilt in both SR18 and HighWay. The actual20.1667-second576×1024 Wan2.2 movie replaces the earlier image-motion video. Public buy/equip/reload, replay/seek/skip and start gating verified.
- U4: HighWay contains2340m,5 turns, elevation changes,23 enemies,12 bonus pairs and29 hazards.40m of apron was added behind spawn. Ordinary-input repeated traversal and actual chapter-transition/replay UI events verified.
- U5: canonical Data.xlsx has64 HighWay plus78 SR18 rows. Artifact Tool authored candidates; the targeted graft preserved unrelated workbook ZIP parts. Protected runtime archive matches the source (Current).
- U6: selected repetitive buckets replaced by2 Oil and1 Seagull stations;2 Ship hazards added.154 market details,4 boats and4 ambient gulls enrich the existing route. Original230 roads,306 stalls and306 quay pieces remain.
- U7:65 unique related tests pass. Runtime and Editor C# builds and tools/validate-agent-harness.ps1 pass. Review corrections were retested; limits remain explicit.

## Repeated play

All cohorts use ordinary PlayerMove, TimeScale3, no9999 and no accelerated lateral control. Growth profiles are deliberate test presets, not claims of purchases made within those runs. Bonus variance affects results.

- Existing Noryangjin ATT12/HP60:48.45/48.46/48.44seconds,30 earned coins each.
- Refined Noryangjin ATT33/HP130, attack-speed level4:330.71clear /311.02death /330.67clear.
- Final Noryangjin after contact fixes, same profile:311.01death /330.67clear. Second run finished with413.72 HP after bonus choices.
- Highway ATT33/HP130, other permanent upgrades0:292.70/292.72/292.68seconds, all clear and all12 choices reached.
- Final Highway after roadblock contact fix:292.69seconds clear,106.49 remaining HP,12 choices,30 earned coins, recorded under highway-final-collision-20260911.

Original traces and screenshots remain under tmp/image-previews/sr18-presentation-progression-2026-09-10/. The harness CSV's kills column counts Dead states and may include contact/cleanup; it is not presented as a precise weapon-kill count.

## Directed functional checks

- UI:100 coin/100 jewel grant; actual60-jewel purchase; no coin spending; free re-equip; lobby restored; ownership/equipment/wallet survive scene reload.
- Movie: actual playback/frame advancement; seek to page2; gameplay start blocked;5 rapid replay/skip cycles; VideoPlayer/RawImage target references cleared on Skip.
- Transition: Noryangjin clear exposes HighWay; wallet/equipment/upgrades persist; Highway replay restores its lobby and does not invent chapter3.
- Physical contacts: oil55% steering then restore; seagull20 damage and departure; cannonball30 speed/10 damage;85-health barrier broken by3 real shots; open toll0/closed40; crossing traffic9m after1.5-second cue, avoid0/contact35.
- Evidence: tmp/image-previews/two-chapter-2026-09-11/{ui-functional,chapter-transition,hazard-physics-v3}.

## Findings and retained failed attempts

1. Replacing the cloned map's Roads left the player and camera with stale references. Flat movement worked but the elevated turn failed. Explicit rebinding plus continuous height projection tests fixed it. The invalid400-second off-route trace is excluded from valid traversal.
2. MaterialPropertyBlock allocation in a MonoBehaviour initializer failed deserialization; allocate in Awake.
3. Legacy Skin_Button listeners hid lobby UI that the new close action did not restore. Rewire the entry to public Open; Story_Button was an image and now has an actual replay action.
4. Merged carried tire/tool geometry bent under skinning despite multiple weighting attempts. Regenerate separate bodies and bind rigid carried parts to the hand. Retain v2–v5 poses; final clean exports pass.
5. Seagull had a duplicate child Rigidbody, preventing damage callbacks from reaching its parent. Remove it through Unity prefab authoring; keep the ground anchor still and apply damage once.
6. Ship aimed below the player's capsule; use collider-center height and an active non-gravity projectile. Damage now measures10.
7. Roadblock damage depended on a callback after bullet deactivation. Deliver damage inside BulletScript before pool return; the duplicate-callback regression and real3-shot break pass.
8. ScreenCapture renders later: changing pages or selection immediately after capture produced a mismatched fade image. Separate capture and mutation by a rendered frame.
9. A Pipeline timeout triggered Editor error-pause. The hazard-physics-v2 all-zero report is invalid; the probe now requires unpaused Play Mode. The corrected v3 probe passes.
10. Historical whole-file Map1 hashes were incompatible with authorized UI changes. Preserve the historical report and verify real geometry/source immutability instead; do not rewrite its recorded hashes.
11. A temporary test fixture passed null to BulletPooler.Get, whose contract requires a caller. Correcting the fixture let the duplicate-hit regression pass.
12. The frontmatter validator needs python -X utf8 on this Windows locale; plain python used CP949 and failed to read the UTF-8 prose.

## Recovery and boundaries

- Snapshot: tmp/backups/two-chapter-2026-09-11/playerprefs.tsv (57 original preference keys;3007 coins,0 jewels).
- Final restoration passed: all57 keys match the original snapshot,3007 coins/0 jewels, original equipment/upgrades restored. The Editor is back in SR18 Edit Mode. Evidence is in tmp/review-two-chapter-20260911/preferences-restored.json and the acceptance verification.json.
- Original source/scenes/workbook backups remain in the same backup directory. No broad git reset, commit or push was performed.
- The task-owned Wan server on local8190 completed with an empty queue, released GPU memory, and was stopped. Existing TRELLIS/Unity services were left running.
- ce-compound ran headlessly in the primary thread per AGENTS.md. High overlap led to updating the existing presentation/progression learning, with UTF-8 frontmatter validation passing.
-65 passing tests are targeted coverage, not a broad repository-suite result. Device performance and human difficulty are not certified. The actual movie is silent with editable Korean UI captions.
