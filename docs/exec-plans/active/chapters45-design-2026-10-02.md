# Chapters 4–5 — reviewed design v2

Status: critical reviewer and independent final checker both approved v2 for implementation. This is a design contract, not evidence of implementation or final quality. The implementation lead owns project integration and the timestamped project progress log.

## Authority, inspected evidence, and continuity

The current task explicitly requests playable, polished Chapters 4 and 5, independent concept/review/development/QA responsibilities, iterative functional and fun evaluation, and a visible project-local decision log. Chapter 4 leaves the rest stop, has a very short highway transition, and traverses Jamsil streets toward the shoe-crowned tower. Chapter 5 goes inside that building and ascends to the shoe building at its summit. TRELLIS 2.0 is preferred for environment/prop production; Meshy is authorized for characters and poor remaining models after local refinement. No purchases, credit top-ups, newly configured credentials, GitHub push, or cloud backup are implied.

Inspected repository sources: root `AGENTS.md`, `ARCHITECTURE.md`, `docs/README.md`, relevant entries in `docs/design/USER_STATED_REQUIREMENTS.md`, `GAME_DESIGN_OVERVIEW.md`, `MAP_DESIGN_OVERVIEW.md`, `docs/exec-plans/active/codex-harness-foundation.md`, current Highway/RestStop completion records, and `ChapterProgression`, `HighwayRoute`, `HighwayChapter2Rules`, `RestStopHoldout`, `ChapterEnemyProgression`, `RuntimePerformanceBudget`, and relevant `PlayerScript` hooks. Historical map descriptions are not assumed to supersede current code or the current task.

Actual video inspection: the complete fourth segment of the current installed `Assets/JH/UI/Opening/Animated/Curse_Opening_Animated.mp4`, 30–39 seconds, was inspected in temporal order through the coordinator's extracted 2 fps contact sheet. The canonical close reveal is frame 13, approximately 36 seconds. Evidence files:

- `C:\Users\ljh\Documents\Codex\2026-10-02\task-2\intro-inspection\fourth-intro-contact.png`
- `C:\Users\ljh\Documents\Codex\2026-10-02\task-2\intro-inspection\frame-13.png`

Observed visual facts: clear blue daytime sky; a tall tapering blue-gray glass tower; a slim upper neck and splayed metallic supports; an enormous white/off-white sneaker lying horizontally across its summit; the toe points right in the reveal. The warm low market buildings foreground the distant tower. The hero has cyan shoes. The old static `Story_04.png` shows a different skyline and must not determine the new tower.

Design inference: crown width approximately 25–30% of the visible tower height preserves the revealed silhouette. This ratio is an art target from the shot, not a user-specified dimension or a surveyed building measurement. The architecture is a stylized Jamsil destination rather than a literal geographic simulation.

Story continuity from `GAME_DESIGN_OVERVIEW.md`: the shark seeks a better shoe as an offering acceptable to 가브릴렐로, to repay the stolen offering. The objective is not merely comfortable shoes, and attack upgrades are not shoe-strength upgrades. Preserve two front feet and the tail-foot; do not invent a separate third leg. The final beat in this scope is obtaining the offering. Do not assert that the curse is lifted or change the hero's anatomy without implementing a corresponding approved ending.

Existing chapter identities: Chapter 1 combines market incidents, physical paths and close combat; Chapter 2 emphasizes breaking through vehicle traffic; Chapter 3 includes rest-stop interiors and its established rotating police holdout. Chapter 4 should emphasize urban crossing rhythm and route commitment. Chapter 5 should emphasize visible ascent and opening secure passages, with a destination payoff.

All exact times, rewards, counts, palettes, implementation names, and mesh budgets below are designer proposals approved for implementation evaluation. They are not new statements attributed to the user. Approximately 300 seconds per completed chapter preserves the established campaign convention; actual combat duration and growth balance must be measured. Respect the existing bonus/enemy/gimmick placement requirements while treating the tables below as major beats, not a replacement spreadsheet of every placement.

## Chapter 4 — 잠실, 하늘의 신발

Core feeling: the distant object promised by the opening becomes a reachable place. Streets gradually open from the familiar exit road into broad urban avenues, then the tower podium and entrance.

| Target interval | Place and visual purpose | Player action and pacing |
| --- | --- | --- |
| 0–12 s | Rest-stop exit continuity, familiar asphalt and guardrail, one short exit curve | Very short highway transition only. No repeat tollgate sequence or extended vehicle chapter. |
| 12–42 s | Broad median avenue and urban bus bay; tower immediately identifiable ahead | Easy patrol teaches local combat and lets the player orient toward the crown. |
| 42–84 s | Two readable signalized cross-streets | Learn delayed lateral traffic sweeps: visible footprint, approximately 1.5 s warning, and a genuinely open lateral corridor. Teach before combining. |
| 84–126 s | Physical park-edge/shopfront fork, then merge with a larger tower view | Park edge offers lighter combat and 20% maximum-health healing. Shopfront offers stronger security rows and a coin cache. Signs expose risk and resolved reward before commitment. |
| 126–172 s | Commercial delivery edge and service crossing | Remix the crossing sweep with a patrol between sweeps. Do not overlap a sweep with a mandatory full-width combat blocker. |
| 172–215 s | Open park/lake promenade | Visual and combat breather, followed by an existing supported shield/attack bonus choice. The tower remains the same destination and grows in scale. |
| 215–255 s | Wide tower plaza, podium ramp and glazed entrance | Two security partitions: first single-switch teaching row, then a left/right remix. Clear hit-to-opening feedback. |
| 255–288 s | Final approach immediately outside the tower | A simple final security patrol/barrier using the established combat contract. This is not another multi-phase captain fight; reserve that payoff for Chapter 5. |
| 288–300 s | Entrance opens, lobby visible | Walk across the threshold and use normal automatic chapter transition. The destination is physically reached. |

Jamsil identity must be spatial, not just Korean text on generic boxes. Required native views include the median boulevard and bus bay, mixed-height commercial fronts and mall podium, park/lake edge, and expansive paved plaza with a glass tower entrance. Street stone is warm and pale, glass restrained cyan/blue-gray, safety cues yellow/amber, and the crown white with restrained warm metallic accents. Keep the daylight visual promise of the actual intro.

Ordinary civilians flee or react from the side; combatants are consistent security/police personnel. Do not fill these locations with unrelated giant chefs or arbitrary enemy costumes. Use a few visible local reactions instead of an expensive city population simulation.

### Crossing and partition contracts

- Crossing warnings describe the actual swept footprint and motion direction. A safe lane must be clear according to the hero's evaluated collider width plus clearance, not merely an unoccupied nominal lane center.
- Keep the damage collider synchronized with the visible moving body. Warning time must be measured in normal native play. Slowing or pausing the game must affect the event consistently.
- Partition power targets sit inside the ordinary left/right firing corridor. No aiming mode or offside target is introduced. A visible connection links the switch to its panel.
- The target exposes HP and shared hit feedback. Destroying it visibly retracts the corresponding partition and removes its blocking collider at the appropriate visible opening point.
- If neither side is destroyed on arrival, auto-forward stops before the visible closed panel at a clearly readable safe line. Lateral movement and ordinary shooting remain available. There is no invisible damage timer, hidden collision penalty, or offscreen required target.
- Limit bespoke partition encounters to the two plaza rows. Their purpose is teaching Chapter 5's secure-passage grammar, not adding another repeated attrition layer.

## Chapter 5 — 슈 타워, 최상층

Core feeling: the player climbs through a real building, sees the city recede below, reaches the architectural underside of the promised shoe, then enters that shoe and claims the offering. Avoid an indistinguishable flat corridor wearing floor-number labels.

| Target interval | Place and visual purpose | Player action and pacing |
| --- | --- | --- |
| 0–35 s | Polished glazed lobby with a clear crown diagram | Establish interior scale and destination. Easy readable security encounter. |
| 35–78 s | Retail mezzanine and short visible ramp/escalator ascent | Teach partition openings and patrol combinations. The ramp itself is quiet. |
| 78–83 s | Walk into the glazed lift | Collect or finish the approach before capture. |
| 83–89 s | First moving lift ride: exactly 6 s design target | Visible physical ascent; city/lower floor moves down in the view. No stationary holdout. |
| 89–91 s | Landing doors release | Restore ordinary movement and fire. |
| 91–145 s | Office/service deck physical fork | Wide quiet maintenance path: heal 20%. Narrow security archive path: two additional mandatory security pairs and an alternating partition before shield and explicit coin reward. |
| 145–188 s | Glass observation ring, city far below | Apply learned alternating openings and patrols; let a clear side view prove height gained. |
| 188–196 s | Reward collection and final lift approach | Quiet collection and anticipation, not an elevator countdown. |
| 196–202 s | Second moving lift ride: exactly 6 s design target | Rise visibly toward the sneaker's underside. |
| 202–207 s | Doors release and first crown view | Read the sole and supporting structure before the next pressure beat. |
| 207–245 s | Crown service deck and splayed supports under the sole | Alternating vent sweeps reuse the learned footprint/warning/gap grammar. Remain readable and avoid camera occlusion. |
| 245–284 s | Security captain at shoe-showroom vestibule | One visible shield/core target, normal lateral movement and shooting, three readable phases. |
| 284–300 s | Bright interior of the giant white shoe | Walk inside, approach the displayed better-shoe offering, physically claim it, show collection animation and final reward/result. Suggested story line: `공물을 찾았다`. |

The two elevator windows contain walking, collection, doors, and views; each actual moving ride is six seconds. Never interpret the 13- or 19-second enclosing interval as stationary lift waiting. The timings are an initial pacing target. Report real duration instead of padding a fast run or masking a long fight.

### Meaningful fork contract

The maintenance route stays broad and quiet and gives 20% maximum-health healing at its exit. The archive is visibly narrower, with two lanes, two extra mandatory security pairs, and an alternating partition. Its one-hit shield and coin reward arrive only after this additional risk. A full-health player still has a reason to choose the safer route: less required combat and steering under pressure.

Initial proposal: workbook value `towerArchiveCoinReward = 80`. The UI must display the resolved actual value. Tune this amount and the extra fights against ordinary inherited Chapter 3/4 growth; 80 is not an asserted economy balance. Chapter 4's risk route likewise announces its resolved coin reward. Both forks merge, lock the choice once committed, and prevent cross-path/double reward collection. Use existing physical movement and pickup language, not a popup that interrupts play.

### Captain and final offering contract

- Use one distinct shielded body/core target rather than several unreadable targets. It occupies ordinary shot lanes and visibly presents left/center/right openings.
- Initial rhythm: four-second vulnerable window, then two-second visibly amber shield/reset. Three HP thresholds gate the phases. Do not restore lost HP. If phase damage is clamped, make the threshold transition visible rather than silently ignoring normal fire.
- Ordinary lateral control and auto-fire remain unchanged. Controlled forward stopping keeps the player safely at the encounter. Do not switch to the rest-stop's rotating control mode.
- Pressure between openings always leaves a clear safe lane; avoid a shielded full-width wall that damages the player without a response.
- Defeat removes the pressure, opens the shoe showroom/pedestal path, and creates a clear quiet reward moment.
- The offering is an actual visible object reached through movement. Its physical claim invokes one chapter-owned transaction per run, records Chapter 5 completion, applies the configured final reward, animates collection, and presents the result.
- First-clear rewards retain the persistent chapter reward guard. Retry/reset rebuilds the encounter and object appropriately without allowing repeated permanent first-clear rewards. A medal-only result screen without entering the crown and claiming the offering does not satisfy the payoff.

## Movement, camera, and state ownership

Use a narrow scene-scoped `LaterChapterDirector` or equivalent, allowlisted only to the chosen Chapter 4 and 5 scene keys. It owns later-chapter stop/shield/gate/lift state and lifecycle. Keep other chapters' movement and event behavior intact.

Relevant code facts: `HighwayRoute.Sample` projects the tangent onto the horizontal plane. A pure vertical span falls back to world forward and is not an elevator implementation. `NoryangjinRevampDirector` is restricted to the SR18 revamp scene. Highway's existing fork HUD and automatic healing carry highway-specific behavior. Do not reuse those controllers unchanged and assume these designs work.

Use flat combat decks and one reusable visible lift/transfer controller. A short quiet mezzanine ramp may use a tested elevation/aim path. A finite set of three combat deck groups plus the crown is sufficient; do not model or render 120 floors. Authored height, skyline parallax, a floor counter, and recognizable building shell features must together show ascent. Do not replace physical ascent with an arbitrary teleport hidden by text.

Transfer lifecycle requirements:

1. Capture current player/camera/movement/fire state at boarding. Own the visible ride pose and keep feet supported.
2. Suspend forward locomotion, lateral input, and automatic shooting only while captured on the lift. Do not pause the global run clock as a substitute for movement control.
3. Advance using the game's scaled time. Pause freezes lift and related hazard clocks; resume continues without jumps or an extra damage tick.
4. Land at an explicit valid floor point and lane, with correct target height and current deck identity. Restore the captured state after the doors open.
5. Restore or abort safely on death, replay, scene unload, and interrupted boarding/release. Clear stale singletons, event subscriptions, blockers, shields, and pooled objects.
6. Admit enemies, shots, damage, and interaction only on the correct deck, with height-aware checks. Stacked floors must not produce cross-floor targeting or damage.

All combat route samples need a valid horizontal tangent and correct floor Y. Verify enemies, pickups, bullet origin/trajectory, labels, and colliders at height. Avoid firing up/down an unverified slope.

Camera contract: preserve normal portrait control literacy. Keep the walking surface, shark and next decisive target in the central approximately 60% of the gameplay view. Major new beats get 3–5 seconds of quiet approach. Hide/fade obstructing roof or facade faces only when necessary; never hide the player's current support surface or remove a walking collider for presentation. Upper/lower decks use explicit visibility groups, and the current/adjacent floor policy must be checked at lift transitions. The lift camera must show enough stable building reference and moving city/lower floor to prove vertical travel.

Chapter 4 landmark views: first boulevard reveal, fork merge, and plaza approach. Chapter 5 landmark views: city below the observation ring, shoe underside at the final lift, and actual inside-crown showroom. These are required native visual review positions.

## Assets and performance gates

TRELLIS 2.0 is the preferred source for new environment/prop art, using the discovered safe local workflow. Proposed priority kit: canonical tower and sneaker crown, podium/entry modules, urban facade and bus-bay details, interior lift/partition/display modules. Meshy is for required security characters or models that remain visibly poor after local refinement. Existing suitable assets can support continuity; their reuse is not evidence that the new hero assets are finished.

Generated visual shells must be separated from precise authored walkable surfaces, trigger shapes and collision geometry. Refine orientation, pivot, dimensions, material tone and broken mesh details before integration. Validate actual imported meshes, materials, LOD silhouettes, collider alignment, player clearance, and readability in motion. The final crown cannot be approved as colored primitive placeholder geometry. It needs the recognizable white shoe silhouette and sole, heel, tongue/upper, structural support relationship, and an interior that reads as entering that crown.

Provisional art budgets, subject to measured evidence:

| Asset type | Initial target |
| --- | --- |
| Common props/modules | Usually 2–5k triangles or lower where silhouette allows |
| Hero sneaker crown | Up to about 15k triangles at LOD0; approximately half/quarter LODs that preserve silhouette |
| Security characters | Around 3k triangles with validated rig/animation and bounded pools |
| Textures | Usually 1K maps; 2K reserved for hero/shared atlas where a native view proves value |
| Glass/background | Prefer opaque stylized glass and inexpensive skyline/LOD forms; avoid broad layered transparency |

These are budgets, not a performance certification. Native submissions/frame costs, current floor visibility, material count, peak memory, and visible LOD transitions decide acceptance. Retain the established mobile rendering direction; do not add SSAO, expensive full-screen effects, or outline passes to every background object. Profile loaded actors and collision work rather than assuming low triangle counts guarantee speed. Do not report Editor timing as S22 device performance.

## Functional QA and fun evaluation

Run supported actual gameplay and retain native evidence after final edits. A high-stat bot clear proves some reachability; it does not establish ordinary difficulty, fun, or mobile performance.

Functional acceptance:

- Chapter 3 clear transitions to Chapter 4; Chapter 4 clear transitions to Chapter 5; chapter titles, entry screens, scene inclusion, start/retry selection, unlocks and persistence are correct.
- Ordinary inherited-growth attempts exercise both fork choices. Verify the selected reward and that the other path cannot also be collected. Record starting stats, elapsed time, damage, reward and outcome.
- Each new sweep is actually avoided in one trial and physically contacted in another. Its visible body, warning, timing and damage agree.
- Normal auto-fire hits partition targets, lowers their actual HP, and visibly opens the corresponding panel. Arrival at an intact panel safely stops forward motion while allowing lateral/fire.
- Both lifts are boarded, visibly ridden and exited with correct camera/feet/floor height. Test pause/resume, death/abort, retry, and scene unload. No control remains locked afterward.
- Test stacked floors for cross-floor shots, targeting, damage, pickups, label visibility and support collision.
- Captain phases visibly match vulnerability, retain lost HP, preserve a safe response, and reset correctly.
- Final offering claim is a real physical transaction; completion and rewards fire once appropriately and cannot duplicate first-clear rewards on replay/retry.
- Preserve and restore the user's pre-test preferences/editor state according to established project harness practice. Verify Chapter 1–3 regressions through focused appropriate checks.

Native visual evidence must include: Chapter 4 boulevard, fork, lake/park merge, plaza, partition impact/opening and tower threshold; Chapter 5 lobby, each combat deck, a frame during each lift, observation ring, shoe underside, final captain, and inside-crown offering collection. Inspect portrait aspect, not only a wide Scene view.

Independent fun review should record concrete observations separately from assertions: whether the destination stays understandable; whether choices are readable early enough and rewards matter; whether waits and repeated patterns drag; whether combat/lull alternation sustains momentum; whether height is felt; whether ordinary input explains every failure; and whether arriving inside the shoe rewards the opening's promise. Iterate on recoverable issues and re-run affected checks after changes. Record any manual human playtest or physical-device limits honestly.

## Critical review disposition

Critical reviewer requested six revisions and then explicitly approved v2 design:

1. Elevator windows clarified into approach/ride/release; actual moving rides are six seconds each.
2. Explicit flat-deck/lift lifecycle and scene-scoped controller; height and deck filtering; restoration on retry/death/unload.
3. Partition targets moved into normal shot lanes; intact panel uses visible safe stop with lateral/fire retained.
4. Final captain uses a visible single core and ordinary input; offering is a physical, retry-safe claim rather than a result-only reward.
5. Archive reward follows distinctly higher risk; quiet maintenance route retains value at full health.
6. Jamsil spatial identity and generated hero-crown visuals require native evidence; numeric asset budgets alone do not approve quality.

Additional clarification accepted: phase transitions never regenerate lost captain HP; amber shield/reset is visible and the safe lane remains available. Chapter 4's final encounter is deliberately a simple patrol/barrier, not the three-phase captain from an earlier draft.

The independent final checker approved this settled v2 design for implementation, with no additional design revision required. Clarifications: 300 seconds is target pacing, never timer-forced chapter completion; current/adjacent-deck culling must keep actual support surfaces visible. Separate final gates remain for refined/generated crown identity, route rewards/readability, native physical runs, ascent-state restoration, and final offering claim. Design approval does not waive functional, asset, visual, fun, or performance acceptance gates.
