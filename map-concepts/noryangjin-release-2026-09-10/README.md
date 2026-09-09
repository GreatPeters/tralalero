# Noryangjin release work — 2026-09-10

## Progress

- Applied the workbook balance controls and 12 cosmetic entries. Artifact Tool authored the workbook; a targeted XML graft preserved unrelated original parts. `verify-noryangjin-release-data.mjs` reimported the saved source with zero formula errors; the protected runtime archive matches it.
- Upgrade cards now use the supplied reference's merchant and nine illustrations with native prices/text. Skin button opens a three-tab, four-items-per-tab part shop. Live purchases charged 50/45 for ATT/HP level 1, and 150/120/180 for coral skin/ruby shoes/cap. Re-equipping charged zero; a scene reload retained all three equipped parts and one hat. Test currency and purchase prefs were restored.
- `E10`/`E19` cross the bridge laterally at speed 1.4 over distance 2. HP rewards grow current and maximum health together. Permanent helper percentages no longer create dozens of helpers. Surviving an 80-damage broken plank preserves the player's route orientation.
- Two post-balance normal-stat runs exposed a late descent issue at E23: the enemy stood only 3.42 units after the level trigger, so high-altitude shots passed overhead until contact. The enemy moved from x278.33 to x260, leaving 21.75 flat units; its activation gate is x278 on the flat. This changes neither enemy stats nor road geometry. A focused placement test passes.

## Verification conditions

**최종 주행:** 내리막 수정 후 `054059` 주행은 HP145/ATK68(ATT/HP 3레벨), 정상 좌우 이동으로 STAGE CLEAR에 도달했다. 보너스 25쌍·적 26명, 남은 체력 271.06/726.06. 공·체 9999는 꺼진 상태다. 기본 무업그레이드 완주나 인간 난이도 인증과는 구분한다. [완주 화면](normal-upgrades-after-ramp-win.png).

다시하기로 씬을 새로 불러온 뒤 Start가 완료된 상태에서 시작 버튼을 눌러, 선택된 9999 옵션과 좌우 4배가 다시 HP9999/Max9999/ATK9999/분모31.25로 적용되는 것도 확인했다. 종료 후 옵션을 모두 끄고 시간 배율1, 코인2967/보석0과 44개 구매 관련 키를 복구했다. 자동 TMP 글리프 변경은 사본을 보존한 뒤 원래 폰트 자산으로 되돌렸다.

[업그레이드 화면](upgrade-ui-final.png) · [꾸미기 구매/착용 화면](cosmetic-equipped.png) · [9999 완주](boost-after-balance-win.png) · [최종 밸런스 시트](balance-final.png) · [커스터마이징 시트](cosmetics-data-final.png). [다음 단계 고속도로 예시 5장](../highway-examples-2026-09-10/README.md).

Inputs use the real PlayerMove and start/purchase/replay handlers. Automated steering selects a bonus side, aims at one member of a two-enemy wall and avoids authored hazards. It is not a human difficulty rating. TimeScale 3 accelerates game time; movement geometry and normal stats remain unchanged unless the listed Editor options or real upgrades are selected.

| Run | Start stats | Result before late-ramp fix | Evidence folder |
|---|---|---|---|
| Initial boosted | 9999 HP/ATK, 4x lateral | Clear; exposed current/max HP mismatch subsequently fixed | `034755` |
| Base progression | 100 HP / 50 ATK, normal lateral | E23 contact death, 23 choices, 22 enemies killed | `052402` |
| Post-balance boosted | 9999 HP/ATK, 4x lateral, three cosmetics worn | STAGE CLEAR, 25 choices, 25 enemies killed; 20098/20263 HP at finish | `052924` |
| Small permanent upgrades | 145 HP / 68 ATK, normal lateral, ATT/HP level 3 (435 coins) | E23 contact death despite full 320 HP before contact; 23 choices, 24 enemies killed | `053356` |

Folders are under `tmp/image-previews/sr18-live-playtest-2026-09-10/`. Test screenshots are under `tmp/image-previews/noryangjin-release-2026-09-10/`. These are retained locally, not dependency trees for publication.

Focused validation: cosmetic 8/8, BonusWall 16/16, run balance 6/6, test overrides 2/2, workbook 15/15, upgrade UI 2/2, late ramp 1/1. SR18 26/28 passed; the two failures guard an older Map1 hash. The existing Map1 is unchanged relative to HEAD in this task. The isolated broad suite was 606/629, with one subsequently fixed title test; it must not be described as green. [Detailed test report](test-results.json).

Build: `dotnet build Assembly-CSharp-Editor.csproj -nologo -v:q` passes; harness validator passes. Broad rebuilds retain two unrelated warnings (TMP word wrapping and a third-party unassigned spline field). No new runtime errors were captured during the completed post-balance boosted run.

## Scope and limits

The UI is installed in both Noryangjin map-tool scenes. The protected build-source Map1 was preserved, and SR18 is not automatically promoted into the build list. Highway automatic transition is still not implemented. Cosmetic items are visual only and use the original shark rig, not arbitrary whole-skin mesh swapping. Human playtest and device performance remain separate checks.

## Token-efficient follow-up

Use this record and `ARCHITECTURE.md` as entry points; read narrow code ranges, summarize tool results, and avoid dumping Git status for caches. Graphify may reduce repeated structural lookup but has not been installed or benchmarked here. It cannot replace Unity scene/runtime visual checks. Official guidance recommends relevant source limits and smaller models for routine tasks: https://learn.chatgpt.com/docs/pricing . Graphify's documented code-graph workflow: https://github.com/Graphify-Labs/graphify .

- Editor Convenience controls implemented: attack/health9999 and4x lateral input. Preferences use SessionState, apply on run start/retry, and restore normal values on disable/Play exit. Two focused tests pass.
- Upgrade art generated from the supplied reference. Source retained at `Assets/JH/UI/Upgrade/Workshop_Reference_20260910.png`; preview at `tmp/image-previews/noryangjin-release-2026-09-10/workshop-art.png`. Generated with the built-in ImageGen tool, preserving the merchant and nine part illustrations while removing UI values/labels for live Unity text.
- Existing project roads found under `Assets/ithappy/Megacity/Traffic/Prefabs/Roads/`. External Highway results contain FBX/GLB/Blend plus previews and validation metadata. These are reserved for the final Highway concept step.

## Recovery

Git publication excludes newly staged `tmp/` dependency trees and `Assets/_Recovery/` autosaves; those files remain on disk. Previously tracked historical tmp evidence remains tracked and unchanged. The final publication contains the existing SR18 authored work plus this task's code, data, UI/assets and documentation. Unity-generated empty metadata values carry native trailing spaces; source-code and Markdown whitespace checks pass.

- Pre-work project patch: `tmp/backups/sr18-release-20260910/before-project.patch`.
- Pre-controls scene: `tmp/backups/sr18-release-20260910/before-test-controls.unity`.
- The pre-existing Git index contains many temporary/dependency files. Keep those local; publish only reviewed project sources, assets, data, reproducible tools and durable records.

## Art prompt

Edit the supplied upgrade reference without rearranging its merchant, workshop or nine illustrated parchment cards. Remove dynamic card labels, levels, values, descriptions, prices and currency symbols; keep blank price-bar frames. Remove top currency HUD and footer controls. Produce a portrait production art sheet suitable for slicing into a merchant hero and nine card sprites; Unity renders all live text and prices.
