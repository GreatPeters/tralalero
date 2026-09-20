# Docs Index

- `../map-concepts/mobile-release-2026-09-20/README.md`: current work merged/pushed to `master`, ARM64 build and in-place Samsung phone installation verified by matching APK hash; game rendering verification awaits phone unlock.

- `../map-concepts/seagull-size-followup-2026-09-20/README.md`: latest size-only follow-up, +20% warning diameter to 3.744m and +30% bird scale to 4.095; timing and impact reactions preserved.

- `../map-concepts/seagull-impact-tumble-2026-09-20/README.md`: latest +50% bird/+30% shadow sizing; two-turn shark spin plus three-turn bird knockback, native normal/slow-motion video and pause/retry verification.

- `../map-concepts/seagull-early-landing-2026-09-20/README.md`: latest timing correction—28m warning, earlier completed landing and wait until the runner passes; native landing lead measured at approximately 15m and two-second before/after comparison.

- `../map-concepts/seagull-warning-readability-2026-09-20/README.md`: latest seagull tuning—50% larger bird/2.4m warning, darker alpha and earlier 21m warning with matching delay; native dodge/contact verification.

- `../map-concepts/seagull-dodge-size-2026-09-20/README.md`: smaller 1.6m landing warning and matching bird/contact footprint; actual delayed left/right steering avoids damage, while contact retains the 20% penalty and two-turn spin.

- `../map-concepts/synced-crate-throw-2026-09-20/README.md`: supersedes the jittering two-hand throw with one pose owner, post-pose launch and scoped outline repair; 52 tests and complete native normal/half-speed videos.
- `exec-plans/completed/synced-crate-throw-2026-09-20.md`: implementation and verification of the timing/motion correction.

- `../map-concepts/seagull-contact-2026-09-20/README.md`: animated body/wing contact, two-turn spin and 20% maximum-HP penalty; 15 tests, moving/miss/landed physical probes and native HUD/video.
- `solutions/integration-issues/keep-seagull-contact-active-through-landing-2026-09-20.md`: align collider lifetime with the visible hazard, validate root-player contact and stop probe-only coroutine producers.

- `../map-concepts/two-hand-crate-2026-09-20/README.md`: replaces the rejected one-hand attachment with independent crate placement, two-palm contact, body-mesh validation and a native front/side motion video.

- `../map-concepts/throw-ship-refinement-2026-09-20/README.md`: follow-up crate/face clearance with a hand-anchored rim, fixed road-facing ship fire, native captures and 13 regression tests.

- `../map-concepts/combat-route-fixes-2026-09-20/README.md`: nine combat/route fixes, 70 tests, native projectile/HP/slope evidence and three-map placement audit.
- `solutions/integration-issues/verify-combat-with-authored-prefab-layers-and-pivots-2026-09-20.md`: production Default-layer collisions, bone-relative grip anchoring, imported projectile scale and native verification corrections.

- `../map-concepts/talisman-polish-2026-09-20/README.md`: reference-based Blender rebuild, raised emblems, visible shared torso VFX, actual physical-pickup videos in three scenes and 39 tests; supersedes the rejected first presentation.
- `solutions/workflow-issues/validate-pickup-art-in-moving-gameplay-2026-09-20.md`: distinguish functional test success from visible fidelity; motion, depth and caption-material checks.

- `../map-concepts/common-talisman-applied-2026-09-20/README.md`: implemented common talisman/body absorption for all Bonus sources; 37 tests, actual pickups/drops and native three-scene evidence.
- `solutions/workflow-issues/common-bonus-feedback-survives-root-deactivation-2026-09-20.md`: detached pickup effects, nonuniform prefab transforms, explicit Single sprite import and simulation-clock verification.

- `../map-concepts/talisman-common-body-2026-09-20/README.md`: latest user correction—all upgrade talismans share one torso absorption effect; replaces per-stat destinations.

- `../map-concepts/talisman-stat-targets-2026-09-20/README.md`: corrected talisman feedback targets—attack to visible mouth muzzle, health to torso; supersedes attack-to-shoe concepts.

- `../map-concepts/talisman-pickup-flows-2026-09-20/README.md`: four pickup storyboards for the preferred folded talisman, immediate-reward timing, separate effect lifetime and verified external gallery.

- `../map-concepts/universal-bonus-2026-09-20/README.md`: ten distinct buildable bonus concepts, each identical across harbor/highway/rest-stop, with construction breakdowns and a verified gallery.

- `../map-concepts/slim-ring-story-2026-09-19/README.md`: selected slim ring linked to offerings and cursed shoes in a four-panel image concept; full prompt and external gallery.

- `../map-concepts/bonus-ring-variants-2026-09-19/README.md`: ten variations of the user-preferred latest 07 open orbit ring, original prompts, corrected 05 sheet and verified external gallery.

- `../map-concepts/bonus-diverse-pickups-2026-09-19/README.md`: ten new silhouettes extending the positively received enamel/metal bonus concepts, with construction notes and a verified external gallery; no gameplay installation.

- `../map-concepts/bonus-polished-pickups-2026-09-19/README.md`: three reference-informed, feasible 3D bonus pickup concepts with gameplay/detail/pickup views and a verified external gallery; concept only.

- `solutions/workflow-issues/protect-codex-image-clicks-without-changing-thread-state-2026-09-19.md`: reversible Codex VS Code thumbnail safeguard, installation/rollback, fixture checks and pending native activation verification.

- `../map-concepts/bonus-concepts-2026-09-19/README.md`: ten bonus-VFX concept images with idle/pickup views, existing asset candidates and feasible Unity/Blender implementation paths; no candidate selected or installed.

- `../map-concepts/feedback-2026-09-19/README.md`: reference-sized settings controls, merchant sleeve deformation repair, hand-relative shovel fit and gold reward effects; native tests and actual collection evidence.

- `../map-concepts/new-content-stylized-2026-09-17/README.md`: 174 new surfaces unified with FlatKit, preserved maps/transparency, three shipping outline renderers, merchant/three-scene native renders and regression tests.

- `../map-concepts/shop-fidelity-2026-09-17/README.md`: corrected cosmetic and upgrade shop proportions, native nine-slice artwork, runtime states, real-model framing and three-scene verification.
- `solutions/ui-bugs/keep-shop-reference-styling-through-runtime-refresh-2026-09-17.md`: distinguish font outlines from sprite borders, inspect active runtime states and protect final styling from generic authoring passes.

- `../map-concepts/ui-text-alignment-2026-09-17/README.md`: measured glyph alignment, symmetric story labels, centered previous-arrow group and runtime proofs.
- `solutions/ui-bugs/center-visible-tmp-glyphs-and-compound-button-content-2026-09-17.md`: distinguish font-metric centering from visible glyph centering and center compound controls together.

- `../map-concepts/chapter-ui-themes-2026-09-17/README.md`: Highway/RestStop lobby motifs, explicit chapter-art selection, native captures and Noryangjin preservation.
- `solutions/design-patterns/separate-chapter-identity-from-shared-ui-2026-09-17.md`: share UI structure while selecting location-appropriate symbols through the normal authoring path.

- `../map-concepts/harbor-faithful-art-2026-09-17/README.md`: integrated reference-derived lobby/story art, Jua SDF, responsive video chrome, actual navigation/transition checks and same-width reference comparisons.

- `../map-concepts/harbor-reference-fidelity-2026-09-17/README.md`: supplied concept vs native lobby correction, padded display SDF, independent hand animation and enlarged supported merchant framing.

- `../map-concepts/harbor-opening-refinement-2026-09-16/README.md`: 11-point native refinement, Gmarket SDF, all-chapter UI/HP, extended pier, TRELLIS workshop/merchant, bonus effects and verification.
- `solutions/ui-bugs/verify-mobile-sdf-and-world-presentation-in-native-frames-2026-09-16.md`: mobile outline shader variants, world text units, animated rig weights, portrait camera visibility and early decoder seeks.

- `../map-concepts/coastal-enamel-ui-2026-09-16/README.md`: selected style1 production assets and all-screen game installation, PSD/sprite sources, native validation and live evidence.
- `solutions/ui-bugs/normalize-sliced-ui-and-rebind-replaced-screen-fields-2026-09-16.md`: native sprite modes, complete fixed borders, compact row surfaces and external victory-label rebinding.

- `solutions/workflow-issues/preserve-concept-scope-when-applying-ui-style-2026-09-15.md`: concept-only clarification, reversed unintended Unity edits, verified restoration and actual-character image proposals.

- `../map-concepts/harbor-ui-suite-revision-2026-09-15/README.md`: five coordinated concept images covering thirteen upgrade/shop/tutorial/story/common-panel states, exact generation prompts and reviewed state consistency; static proposals.

- `../map-concepts/haunted-festival-wearables-2026-09-15/README.md`: selected Haunted Festival BGM installed; canonical forehead seating, lined pirate crown, live animated headwear captures and preservation checks.

- `../map-concepts/sr18-placement-repair-2026-09-15/README.md`: U89 one-to-one trigger repair, protected map-tool assignments, fitted signal gantry and actual view occlusion checks.

- `../map-concepts/harbor-polish-2026-09-15/README.md`: U88 gameplay/settings/retry correction, readable HUD/TMP presets, five-rank chapter upgrades, coin/hitbox/camera improvements, fitted hats, coastal ship visibility and ten lively BGM candidates.

- `../map-concepts/emotional-horror-bgm-2026-09-15/README.md`: ten emotional/creepy BGM candidates from U87, local generation prompts, MP3/WAV/OGG listening copies, playlist and signal-level validation.

- `../map-concepts/harbor-ui-live-2026-09-15/README.md`: installed Harbor UI and chapter purchases across three scenes, actual A video bindings/markers, equipment totals,32 native tests, screen evidence and verified user-state restoration.
- `solutions/integration-issues/validate-connected-ui-through-activation-and-scene-reload-2026-09-15.md`: canvas-root scale, inactive contrast, dirty-component persistence, test cleanup boundaries and real playback evidence.

- `../map-concepts/harbor-video-ui-2026-09-15/production/README.md`: both video concepts separated into 25 UI PNGs plus a movie preview, 2 PSDs and 2 native presentation prefabs; inspected alpha, native aspect and source/reconstruction boundaries.

- `../map-concepts/harbor-video-ui-2026-09-15/README.md`: two Harbor-style video-screen concepts, cinema overlay versus separate parchment caption; static proposals with retained playback-flow references and prompts.

- `../map-concepts/harbor-ui-production-2026-09-15/README.md`: selected-v2 revision, chapter/purchase state concepts, 35 separated sprites, 2 layered PSDs, exact-scene overlay and verified live health/action-label changes; full UI replacement/chapter logic not installed.
- `solutions/workflow-issues/validate-real-alpha-and-rebuild-ui-as-separate-assets-2026-09-15.md`: opaque checkerboard recovery, real alpha/9-slice inspection, editable-layer boundaries and actual-scene composition.

- `../map-concepts/combat-feedback-2026-09-14/ui-original-variation.md`: C UI concept revision with gameplay-camera start, stacked current stats and real numeric upgrade caps; generated proposal and preserved prompts.
- `solutions/design-patterns/preserve-play-intent-and-data-limits-in-ui-concepts-2026-09-14.md`: user-reviewed start interaction and upgrade-layout semantics; inspect partial rows and derive caps from data.

- `../map-concepts/combat-feedback-2026-09-14/README.md`: three-chapter damage feedback, retry reset, lethal hazards/fallen-pole cooldown, nearby coins, melee corrections, stronger cosmetics, two UI concepts and five original BGM previews;82focused native tests and legacy snapshot limits.
- `solutions/integration-issues/verify-run-reset-and-rendered-feedback-at-runtime-boundaries-2026-09-14.md`: static bonus lifetime, hierarchy-aware UI edits and actual-frame visual verification.

- `exec-plans/active/mobile-presentation-2026-09-14.md`: ongoing Ocean Pop UI, exact lateral upgrade, KERIS font, knife carry, visible coin, original audio and Android content reduction; records native/test/device limits.
- `solutions/ui-bugs/restore-inactive-unity-modal-order-and-scroll-height-2026-09-14.md`: real menu lifecycle, modal order, actual grid bounds and dirty-scene test blocking.
- `../map-concepts/mobile-presentation-2026-09-14/audio/README.md`: two original BGM candidates,26SFX, event coverage and generation provenance.

- `exec-plans/completed/mobile-device-playtest-2026-09-13.md`: optimized APK verified and installed in place on Samsung SM-S901N/Android16; actual gameplay capture, signing restoration and USB recovery.

- `exec-plans/completed/mobile-combat-performance-2026-09-13.md`: bounded damage/coin and hit-effect pools, mobile shadows, measured Editor call costs, native regressions and device-validation limits.
- `solutions/performance-issues/prewarm-bounded-combat-presentation-pools-2026-09-13.md`: prewarming, saturation, lifecycle checks and trustworthy frame-bounded performance evidence.

- `../map-concepts/approved-road-concepts-2026-09-13/README.md`: installed3-head human proportions,2+2 Highway, new TRELLIS indoor counters, open covered hall, unchanged combat camera and native/live verification. Includes model/source index and restoration evidence.

- `../map-concepts/road-reference-visuals-2026-09-13/README.md`: direct comparison with the two actual source images, additive road/service scenery, preserved gameplay snapshots, directed live evidence and fresh preference restoration. Exact reference reproduction and Android acceptance remain separate.

- `design/USER_STATED_REQUIREMENTS.md`: 사용자 직접 발언에 근거한 U01~U74 원칙·요구 원장. 모바일 UI·음향·용량·이동 강화 및 전투 피드백·리트라이·기믹·새 UI/BGM 요청을 포함한다. 원문·구현 상태·에이전트 설계를 구분한다.

- `../map-concepts/road-patterns-2026-09-13/README.md`: continuous Highway curves, two playable recovery bypasses, oncoming traffic and RestStop's stationary30-second food-hall encounter;123tests and scoped live evidence.
- `exec-plans/completed/road-patterns-2026-09-13.md`: planning, native authoring, repeated play/review, data integration and original-state preservation.
- `solutions/design-patterns/separate-authored-enemy-resets-from-pooled-spawns-2026-09-13.md`: actual spawn-position verification, curve-normal continuity and exact restoration of temporary combat state.

- `../map-concepts/chapters-polish-2026-09-12/README.md`: completed three-chapter content/UI, corrected tail-shoe movies,CC0font,17/22/22progression,83final native tests, signed Android test APK and user-state preservation.
- `exec-plans/completed/chapters-style-balance-ads-2026-09-12-log.md`: original Editor recovery, isolated QA, native imports, rejected diagnostics, final gameplay cohorts,83tests, Android build and cleanup.
- `solutions/workflow-issues/verify-character-anatomy-and-real-support-in-generated-assets-2026-09-13.md`: attachment-aware anatomy, rig axes/frame rates, importer-helper bounds and floor-fragment repair evidence.

- `../map-concepts/startup-performance-2026-09-12/README.md`: installed workbook-style compaction, same-Editor first-frame18.078→2.156seconds, protected archive verification and pending Unity test-runner block.

- `../map-concepts/analytics-smoke-2026-09-12/README.md`: actual latest-code Android start/death event test, matching Firebase DebugView values/HTTP204, daily-export boundary and emulator rendering limitation.
- `../map-concepts/flow-opening-2026-09-12/README.md`: two completed Veo3.1 Quality clips, two provider refusals/no charge, actual200-credit usage and pending redesign decision.

- `../map-concepts/player-logs-live-2026-09-12/README.md`: completed Tra → Firebase BigQuery connection, one verified real round,60-day report window and daily10:00–11:00 KST refresh.

- `../map-concepts/skins-reststop-2026-09-12/README.md`: accepted22 TRELLIS assets, body UVs, shop,1:2:1 chapters,18-run progression, rest stop,50-test review and57-key restoration; native Google Sheets connection subsequently completed.

- `exec-plans/completed/skins-progression-analytics-reststop-2026-09-12.md`: current skin/model, two-column shop,1:2:1 chapters,18-run progression, rest-stop and player-log work.
- `exec-plans/completed/skins-progression-analytics-reststop-2026-09-12-log.md`: implementation, failed candidates, verified checks and pending/restoration state.
- `../tools/analytics/google-sheets/README.md`: live Tra destination and maintained bounded BigQuery refresh connector.
- `solutions/integration-issues/trellis-uv-backend-bootstrap-order-2026-09-12.md`: independent UV export implementation and required ComfyUI initialization order.

- `../map-concepts/opening-walk-14b-2026-09-12/README.md`: completed5-second14B FP8 walk comparison, subsequently installed as the game's final opening shot on user request; earlier363 frames preserved,20.17-second total,54-minute generation timing and retained5B backup.

- `../map-concepts/two-chapter-2026-09-11/README.md`: completed16-request acceptance table, usage, screenshots, actual video and65-test verification;57 original preference keys restored.
- `exec-plans/completed/two-chapter-presentation-highway-2026-09-11.md`: completed presentation, editor workflow, Highway production and chapter verification contract.
- `exec-plans/completed/two-chapter-presentation-highway-2026-09-11-log.md`: final implementation/validation record, retained failed candidates and verified user-state restoration.
- `../map-concepts/highway-chapter-2026-09-11/placements.json`: authored2340m HighWay route and23 enemy/12 bonus/29 gimmick identifiers used by the workbook.
- `../map-concepts/opening-animation-2026-09-11/movie-report.json`: actual local Wan2.2 animation provenance and exported20.1667-second MP4 contract.

- `../map-concepts/sr18-presentation-progression-2026-09-10/README.md`: preceding presentation/equipment revision, jewel equipment24 items, actual23-attempt progression record and verification limits; later UI/movie/Highway work is above.
- `exec-plans/completed/sr18-presentation-progression-2026-09-10.md`: completed implementation units and the1–16 acceptance contract.
- `../map-concepts/highway-enemies-2026-09-10/README.md`: reviewed TRELLIS2 enemy FBXs, Unity paths, mesh metrics and existing road FBX location.
- `solutions/workflow-issues/verify-runtime-presentation-and-progression-beyond-numeric-checks-2026-09-10.md`: semantic mesh partition, launch damage, humanoid pose checks, rejected LODs and earned-currency campaign validation.

- `../map-concepts/highway-examples-2026-09-10/README.md`: five asset-grounded Highway examples, source-model inventory and existing road prefab locations; concept stage only.

- `../map-concepts/noryangjin-release-2026-09-10/README.md`: current balance, 9999 controls, upgrade reference art, independent cosmetic purchases and repeat-play evidence; supersedes earlier unchanged-balance notes.
- `solutions/workflow-issues/verify-unity-cosmetic-previews-and-test-prefs-2026-09-10.md`: imported skin mesh scale/materials, Korean TMP labels, bounded preview layout and durable purchase-test snapshots.

- `../map-concepts/sr18-contact-pairs-2026-09-10/README.md`: current durable lamp hazards, centered FatMan entries, two Normal enemy formations and the shared2-second bonus cooldown;27 enemies/76 workbook placements.

- `../map-concepts/sr18-opening-rhythm-2026-09-10/README.md`: applied opening slalom/reward changes, shot-lamp safety and visible health fill; supersedes the unapplied proposals in the playtest report below.

- `../map-concepts/sr18-pattern-playtest-2026-09-10/README.md`: three normal-health opening runs (about41/98/69 seconds), repetitive gimmick responses, newly authored lamp damage after toppling, and the health-bar display finding. No gameplay edits; full normal-difficulty completion remains unverified.

- `../map-concepts/sr18-runtime-fixes-2026-09-10/README.md`: fixes and revalidation for the live-play projectile, camera, legacy lamp and stage-exit defects; current status supersedes the original failure report below.

- `../map-concepts/sr18-live-playtest-2026-09-09/README.md`: continuous gameplay verification, outstanding projectile-pool failure and camera/legacy-hazard findings; supersedes any assumption that the earlier focused tests certified full play.

- `../map-concepts/sr18-combat-polish-2026-09-09/README.md`: current SR18 walk-in ambush, exact facing, larger paired walls, bucket clusters, road-following helpers and placement-owned combat stats.

This directory is the repo-local record for agent work.

Use it as a map, not a dump.

Canonical project documents:

- `../GAME_DESIGN_OVERVIEW.md`: 기획서. 세계관, 캐릭터의 목적, 핵심 루프, 플레이 감정과 콘텐츠 방향을 관리한다.
- `../BALANCE_OVERVIEW.md`: 밸런스 문서. 조정 가능한 수치, 목표 체감, 조정 근거와 검증 결과를 관리한다.
- `../DEVELOPMENT_OVERVIEW.md`: 개발 문서. 구현 구조, 개발 상태, 제작 절차와 상세 기술 문서의 진입점을 관리한다.

새로운 내용은 성격에 맞는 기준 문서에 먼저 기록하고, 상세 구현이나 운영 절차가 길어지면 가까운 `docs/` 하위 문서로 분리한다.

Specialized planning document:

- `../map-concepts/sr18-choices-and-corner-space-2026-09-08/README.md`: 최신 SR18. 고정 보너스 25쌍/50개, 단일 선택 처리, 코너 뒤 대응 공간과 실제 화면·검증.

- `../map-concepts/sr18-latest-elements-applied-2026-09-07/README.md`: 현재 SR18의 적 25명·고정 BonusWall 25개·기믹 24개, 매복 등장→이동→사격과 실제 화면·검증 경계.
- `../map-concepts/sr18-four-second-moving-enemies-2026-09-06/README.md`: 현재 배치의 이미지 원본. 시작 5초 비움·4초 간격과 최근 보너스월 이동 이력.
- `../map-concepts/sr18-encounters-applied-2026-09-06/README.md`: 이전 69명·14벽 배치 이력. 2026-09-07 최신 요소 적용으로 대체됐으며 복구·비교용으로 보존한다.
- `../map-concepts/sr18-full-enemy-bonus-flow-2026-09-06/README.md`: 위 적용의 근거가 된 16구간 전체 흐름 초안과 과거 그림. 현재 씬 상태는 적용 기록을 기준으로 한다.

- `../MAP_DESIGN_OVERVIEW.md`: 맵 기획서. 길, 구역, 전투 비트, 기믹·보너스 배치와 맵 제작 규칙을 관리한다.
- `../map-concepts/noryangjin-expansion-2026-09-02/README.md`: 현재 51모듈에서 총 230모듈로 확장하며 기존·신규 길을 2~5번 입체 교차하는 SUPER RADICAL 30안과 도로 종류별 수량을 비교한다.

Available documents:

- `QUALITY_SCORE.md`: current quality assessment and next leverage points.
- `RELIABILITY.md`: operational failure modes and recovery notes.
- `SECURITY.md`: trust boundaries and risky assumptions.
- `solutions/`: reusable fixes, workflow notes, and scene-generation patterns discovered during agent work.
- `noryangjin-gameplay-maptool.md`: Forward gameplay installation and route-turn authoring guide for the Noryangjin map-tool scene.
- `noryangjin-enemy-movement.md`: simple per-enemy event modes, one-target movement, shared animation states, and activation-spot linking for Forward/Noryangjin encounters.
- `upgrade-shoe-workshop.md`: reference-driven shoe-workshop presentation for the existing nine permanent upgrades, including rebuild and sorting contracts.
- `Assets/ThirdParty/Quaternius/UniversalAnimationLibrary/SOURCE.md`: source URL, CC0 license, archive hash, and retained Unity FBX record for Forward enemy walk/run locomotion.
- `noryangjin-map2-authored-scene.md`: reconciled reference contract, authored-scene composition, and verification record for Noryangjin Map 2.
- `noryangjin-sr18-roads-scene.md`: SR18 Lighthouse Infinity sibling with 230 roads, 15 corner spots, 8 moving slope transitions, tested player height-following over two elevated spans, 306 roadside shops, and manual-authoring guidance.
- `exec-plans/completed/sr18-slope-follow.md`: completed slope-transition implementation, test coverage and the bounded live traversal evidence.
- `noryangjin-mobile-optimization.md`: safe Static classification, low-poly water, Android texture budgets, camera overrides, and measured Map 1/2 results.
- `noryangjin-stage1-maptool-expansion-plan.md`: legacy Stage 1 enemy, gimmick, bonus, object, and map-tool UX ideas with four concept PNGs; its earlier route-length target is superseded by the current map plan and balance document.
- `mobile-ui-atlas-optimizer.md`: one-click Sprite Atlas V2 maintenance, exclusions, settings, validation contract, and measured UI batching results.
- `player-character-defaults.md`: Player defaults, Excel precedence, and the absolute missile speed + duration model.
- `game-data-workbook.md`: the Editor-only Excel source, map-tool shortcuts, protected runtime archive, build guard, and security boundary.
- `noryangjin-encounter-workbook.md`: SR18 적·보너스·기믹76개 엑셀 설정, 좌우 군집 추가 행, 실행 반영 시점과 체력 재로딩 수정.
- `firebase-analytics-bigquery.md`: Firebase Unity SDK setup, retention/playtime and round-event contract, BigQuery export/query workflow, and telemetry trust boundary.
- `exec-plans/completed/protected-game-data-workbook.md`: completed implementation and verification record for the protected workbook workflow.
- `exec-plans/completed/noryangjin-enemy-event-controller.md`: completed migration from mixed movement/fire controls to the five-mode Enemy Event authoring and six-state animation contract.
- `exec-plans/active/firebase-analytics-bigquery.md`: analytics implementation, verification status, and the remaining external Firebase/BigQuery console steps.
- `design/stage_prop_rebuild_20260510.md`: stage-reference prop reuse and missing-image rebuild record.
- `exec-plans/active/codex-harness-foundation.md`: current repo-shaping plan for agent harness engineering.

Editor shortcuts:

- 맵툴 `편의 > 테스트 속도 (TimeScale)`의 `1배 (기본)` / `3배 (테스트)`는 에디터 테스트용이다. 선택은 현재 에디터 세션에서 기억하고 플레이 중 즉시 적용하며, 종료 시 실제 배율은 1로 복원한다. 물리 간격·게임 밸런스·플레이어 빌드에는 저장하지 않는다.

- `Tools/맵 제작 도구/문서/기획서 열기`, `밸런스 문서 열기`, `개발 문서 열기`, `맵 기획서 열기`: opens the three canonical root documents and the specialized map plan with the operating system's default Markdown app.
- The Noryangjin map tool's `편의` tab can activate or deactivate scene-root screen UI without hiding nested world-space UI. Its `작업 그리드 범위` control also changes the horizontal X and vertical Z top-view radii independently from the default 300 up to 1200 cells without moving scene objects or changing placement coordinates. The same tab exposes only `Data.xlsx 열기`; saving the workbook reloads editor data automatically, and player builds generate and validate the protected runtime archive automatically.
- `Tools/Analytics/Firebase 대상 고정`: records the reviewed Firebase project ID, app ID, and Android package under `ProjectSettings` so later config swaps fail the build.
- `Tools/Analytics/Firebase 설정 검증`: checks pinned SDK archive hashes, Android privacy defaults, the Android config, and the reviewed destination before a device build.
- `Tools/Analytics/Firebase 연결 문서 열기`: opens the Firebase/BigQuery setup and query guide.
- `Tools/맵 제작 도구/자료/자료 위치 안내`: opens the consolidated reference-location window. Individual folder shortcuts are not exposed as separate menu commands.
- `Tools/맵 제작 도구/노량진 맵 제작/맵툴 열기`: opens the Noryangjin map-tool palette.
- `Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_2.unity` is the baked Map 2 implementation of `outputs/chapter_campaign_reference_orthogonal_20min`. It is edited with the normal map-tool workflow and has no regeneration command.
- `Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity` is the build-excluded Lighthouse Infinity sibling with its approved dense market applied. Open it directly, then use the normal map-tool palette and select the `600` work-grid overlay preset to continue editing.

Internal recovery APIs are deliberately not registered as map-production `MenuItem`s. Agents may invoke a documented static method through `unity command eval "<Type.Method>();" --project-path .` only after checking that method's scene and Play Mode preconditions. Examples include `NoryangjinForwardGameplayInstaller.InstallIntoOpenNoryangjinScene`, `ForwardEnemyMovementSetup.Configure`, and the two `NoryangjinMapStaticOptimizer` methods.

- `Tools/맵 제작 도구/노량진 맵 제작/맵툴 열기`: opens the Korean RTS-style palette for imported Noryangjin road modules, buildings, props, decorations, and backgrounds. The default screen intentionally shows only category filters (`전체/도로/건물/소품/장식/배경`) and prefab thumbnails, so layout work can be built back up one step at a time. Per-prefab defaults for `기본 크기`, `기본 회전 Y`, and `높이 오프셋` are saved in `Assets/ShooterSurvival/Editor/NoryangjinMapToolPaletteDefaults.asset`.
- The Noryangjin map-tool background palette also includes `Assets/JH/Prefab/water.prefab`, displayed as `물`. It is placed as the single low-cost `Water/Background_Water` backdrop instead of a grid-managed prop, so repeated clicks update one water plane rather than accumulating water objects in `Props`.
- Grid-managed background palette items use their own placement layer: background items block only other background items, while roads, buildings, props, and decorations can still be placed over the background.
- `Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity` is the authored map-tool and gameplay scene. Open it from the Project window before using the map-tool palette; it is not a disposable generated preview.
- The map-tool palette's rendererless `회전 스팟` item marks route corners. Select one to set its absolute target Y yaw and turn duration; see `noryangjin-gameplay-maptool.md` for placement, yaw convention, and Play Mode verification.
- The rendererless `적 발동 스팟` item stores only enemy links. Select it, click enemies in SceneView to toggle links, and press `Esc` to clear the spot selection. Each linked enemy's `Enemy Event Controller` independently chooses attack loop, shoot, move-then-attack, or start-to-target patrol; see `noryangjin-enemy-movement.md`.
- The map tool's `보너스` tab exposes one reusable `Box_left` altar, displayed as `운명의 제단`; legacy `Box_Right` and the older fixed-stat wall prefabs remain outside the palette. Choose `노멀`, `엘리트`, or `유니크` before placement, or select a placed altar and change the same grade in the map tool or `AuthoredBonusWall` Inspector. At runtime the altar reads the display label from `이름`, the effect key from `항목`, and the random value from `최소`/`최대` in `Data.xlsx`'s `보너스` sheet (`엘리트` uses the sheet's `Rare` rows). The data-first world UI places one large auto-sized value above a compact icon-and-stat badge, supporting `+999`, `+11%`, and localized labels through `ATK SPEED`. The pedestal warp combines counter-rotating water, a sparse compass layer, forward foam, and rising droplets; proximity focus enlarges only the approached choice. Only `Percent` rows render a `%` suffix; `Ratio` and `Value` rows render plain numbers. Percent-specific attack/health icons are replaced by the ordinary icons, and `attPercent`/`hpPercent` reuse the corresponding `att`/`hp` display names. Altars within the serialized `인접 판정 거리` (default 12 world units on XZ) exclude ability keys already rolled by their neighbors, so a close left/right pair cannot show the same ability. Bonus instances live under `Noryangjin_MapTool/Bonuses` on an occupancy layer separate from roads, scenery, and enemies. Rebuild the canonical prefab with `Tools/Shooter Survival/Bonus Choice Boxes/Build Box Prefabs`; refresh generated child overrides in the open scene with `Refresh Open Scene Altar Instances`; migrate legacy right-hand instances with `Migrate Open Scene To Single Altar`. Verify with `BonusAltarRulesTests` and the filtered `MonsterGrowthAndMapToolEnemyTests` suite.
- Forward enemy deaths instantiate that same `Box_left` at the enemy's ground position instead of the legacy `random_wall_normal`. The drop uses the map-tool altar's final `3 × 3 × 3` scale and `180°` Y orientation, keeps the prefab's Normal grade, and receives a removable `RuntimeBonusWall` marker, so stage preparation clears drops without deleting map-authored altars.
- In the Noryangjin map tool's selection mode, each placed object's `Y` height label is a clickable selection target. The `설치 조정` preview follows the selected placement root first and falls back to the object under the map cursor when nothing is selected. The blue footprint belongs only to the currently selected placement root (including a selected child); activation-trigger footprints follow the collider's current Y rotation. The selected-object `한 칸 이동` direction buttons move once when a press becomes active. Mouse-up blocks every following GUI frame until the next real mouse-down, so releasing cannot add another step. Holding the same button for two seconds starts repeating at four steps per second. Clearing selection also clears the blue footprint. The last-placed fallback remains limited to `이어 복붙`.
- The map-tool toolbar's `이어 복붙` action duplicates the selected placed object with its current transform and child setup. Roads advance by their manual fine-grid footprint, backgrounds retain freeform renderer placement with a one-fine-cell seam overlap, and ordinary objects meet at their renderer edges. The new copy becomes the selection, so repeated clicks continue the same placement chain.
- The selected-object card's `복사하기` action enters `붙여넣기 중` mode. Clicking any valid SceneView tile stamps another copy there while preserving the source instance's prefab connection, overrides, added children, rotation, scale, height, and within-tile offset. It can stamp multiple separated tiles without changing the clipboard source; click `붙여넣기 중` again or choose a palette item to cancel.
- The current Korean design list is `docs/design/tralalero_meshy_asset_plan_kr.xlsx`. `docs/design/tralalero_meshy_asset_plan.xlsx` is the English sibling. `old_트랄랄레오_MeshyAI_소품리스트_한글.xlsx` is an archived legacy workbook and is not part of the active rebuild flow.
- Meshy design PNGs use `{sequence}_{stage}_{kind}_{asset_number}_{english_name}.png`; run `python tools/rename_meshy_design_images.py --apply` to migrate older generated names.
- Active Meshy design sequences after the Stage01 map-tool road replacement: `001-048` Noryangjin, `049-092` Highway, `093-124` Rest Stop, `125-168` City, `169-211` Gangnam, `212-242` Common.
- `tools/redraw_modular_road_prefab_kit.py` is the previous procedural modular-road redraw flow; use it only when intentionally replacing the approved RnD road-only direction.
- Current `output/meshy_images/RnD` direction is road-only full 3D MeshyAI reference images: 9 road assets per stage, with `road_only_contact_sheet.png` and `road_only_manifest.jsonl`.
- Run `python tools/promote_rnd_road_only_assets.py --apply` to replace the active stage-specific `ROAD` modules with the approved RnD road-only images, expand each stage to 9 road variants, renumber the active queue, and update both design workbooks plus `meshy_image_prompts_kr.jsonl`.
- `tools/replace_noryangjin_roads_with_testfolder.py` replaced the previous 9 Noryangjin ROAD variants with the 3 imported map-tool modules from `Assets/ShooterSurvival/Models/MeshyAI/TestFolder`, then renumbered active PNGs and synced the workbooks/JSONL.
- `tools/generate_rnd_stage_road_examples.py` is legacy procedural RnD output and does not match the approved road-only 3D concept direction; it now requires `--allow-legacy-procedural` before it can clear and replace `RnD`.
- Use `python tools/update_redrawn_asset_notes.py` before syncing when the modular road images need their workbook visual notes and image briefs refreshed.
- Run `python tools/sync_active_meshy_design_lists.py` after moving/removing/renumbering generated PNGs so both design workbooks and `meshy_image_prompts_kr.jsonl` match the current root-level active image list.

Rules:
- Update docs when behavior, workflow, or expectations change.
- Prefer short, stable documents over one large manual.
- Link from the nearest durable index instead of repeating the same rules everywhere.
