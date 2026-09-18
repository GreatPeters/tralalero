# Apply the selected Harbor Workshop UI to the actual game

Status: completed. User explicitly authorized full integration on2026-09-15, superseding the earlier unanswered scope question.

## Scope and decisions

- Apply the real-scene lobby overlay, vertical permanent-upgrade list, permanent/chapter tabs, two-column equipment shop with actual model preview, and selected A video screen with one indicator per real story scene.
- Reuse existing purchase, wallet, cosmetic, start-gesture, video and chapter-transition entry points. Apply to SR18, HighWay and RestStop; preserve original dirty-state/preferences and source/scene backups.
- Chapter workshop entries are one permanent purchase per unlocked chapter. Initial authoring choice follows the concept: attack +100/+200/+300%, health +200/+400/+600%; prices500/2000/5000coins. Effects add across purchased chapters and multiply the total after regular/equipment upgrades. Values live in `Resources/Upgrades/ChapterWorkshop.asset`, not hardcoded UI copy. Existing workbook values and save IDs remain intact.
- Restyle related modal/HUD surfaces for readable consistent colors; do not redesign unrelated gameplay or replace videos/models.

## Verification

- Runtime/Editor builds and focused native tests for chapter unlock gating, one-shot payment, failed payment, re-entry, persistence and accumulated effects.
- Actual menu entry/back/scroll/purchase/equip across the three scenes; source values visible in summaries and card effects.
- Actual opening playback, Next/replay/skip and natural scene-indicator transitions; chapter video completion/skip still enter the next chapter.
- Retain screenshots, runtime reports, and exact restoration evidence. No phone-performance or APK claim without corresponding verification.

## Work log

- Official Pipeline is reachable. Original SR18 was clean in Edit Mode;66preferences snapshotted before mutation, plus separately captured pre-existing values for the three new chapter ownership keys. Source baselines are under `tmp/backups/harbor-ui-live-2026-09-15/`.
- Implementing the chapter purchase domain, sprite-backed authoring and A playback bindings.
- Completed connected scene authoring, chapter purchase persistence, correct equipment-inclusive lobby totals, actual video scene markers/transitions and persistent readable HUD/modal colors.
- 32 focused native tests passed. Real menu/scroll/category checks completed in all three scenes; directed purchases, startup persistence, natural/skip chapter transitions and video controls passed. The final horizontal-start path also exposes a working Pause button.
- All 66 baseline preference entries plus 3 chapter keys were restored and compared without mismatches. Final captures use the original 3047 coins/0 gems and original ownership. Native Editor acceptance only; no APK/device or full campaign-balance claim.
- Final evidence and exact initial chapter values: `map-concepts/harbor-ui-live-2026-09-15/README.md`.
- U86 follow-up: a non-intercepting 20% black Image now sits behind lobby controls in all three scenes. Saved scenes were reloaded and validated; native before/after/start screenshots confirm the dim hides on game start. Editor build passed, the fresh 66-entry preference snapshot was restored, and SR18 was left clean in Edit Mode. Evidence: `tmp/image-previews/harbor-lobby-dim-2026-09-15/`.
