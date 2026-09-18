---
title: Preserve play intent and actual upgrade limits in UI concepts
date: 2026-09-14
last_updated: 2026-09-15
category: design-patterns
module: Harbor Workshop UI concept generation
problem_type: design_pattern
component: development_workflow
severity: medium
applies_when:
  - "Restyling a game start screen from an existing gameplay reference."
  - "Generating upgrade mockups whose layout and level indicators imply game behavior."
tags: [ui-concepts, image-generation, gameplay-camera, upgrades, reference-validation]
---

# Preserve play intent and actual upgrade limits in UI concepts

## Context

The user rejected the C Harbor Workshop start composition because a large side-view shark and detached arrows resembled a skin selector. The original start reference uses the gameplay camera and a horizontal movement instruction. Separate attack/health chips above a two-column grid implied that each column upgraded one stat; five decorative level dots also invented an incorrect cap.

## Guidance

Inspect the user's actual original image before changing composition. Resolve a mistyped path against repository filenames: the supplied nested path was absent, while `Assets/JH/Image/Lobby/Main/img_main_all.jpg` contained the exact start UI. Preserve the rear gameplay camera, visible route and direct horizontal-movement start instruction when applying new art styling.

Place current attack and health in one common summary and put each upgrade's name, illustration, effect, numeric level and price in its own full-width row. Stacking the two summary values was one proposed correction, not a universal requirement: the user subsequently selected v2, whose two values share one horizontal summary panel above a single-column list. Preserve that newer selection.

Read actual per-item limits before drawing progress. The workbook currently has caps 60, 30, 20 and 10 for different categories. `UpgradeTables.MaxLevel(id)` is the runtime source; the concept should use the same per-category denominators. Example balances and current levels must remain explicitly illustrative, not a new balance specification.

Inspect generated output against each requested constraint. The first edit retained horizontal summary values and invented a coin cap of 20 in the partially clipped next row. A second targeted image edit stacked the summary and corrected that denominator to 30. Preserve both candidates and save the selected PNG in the repository preview folder.

## Why This Matters

When extending an approved style across a UI suite, also check the state behind each modal. In the September 15 suite, the first common-panel image put lobby navigation behind in-run resume/results controls and changed the cosmetic shop's tabs and two-column grid. A targeted edit restored the appropriate backgrounds. The edited shop then showed 800 gems behind an insufficient-funds dialog with 80 available and 200 required; a second edit made both balances 80. The defeat HUD and player-local bar were also emptied. A footer saying values are examples does not excuse contradictory values within one screen.

Use an explicit state table in prompts and review: screen origin, wallet balance, ownership, affordability, current health and allowed actions. Keep currency icon identity constant when displaying an error; express shortage through text color, not a different gem color. Review the whole image after each targeted edit because referenced backgrounds can reintroduce incompatible values. Preserve intermediate candidates and link the selected revision explicitly.

Visual composition communicates interaction and scope. A high-quality illustration can still teach the wrong starting gesture or create a false association between upgrades. Decorative progress indicators can silently introduce gameplay rules absent from the data.

## When to Apply

- Review image mockups for behavioral implications as well as palette, typography and illustration quality.
- Check partially visible rows and retained panels after an edit, not only the intended change area.
- Record user corrections separately from the assistant's proposed layout and from runtime implementation status.

## Examples

- Video progress is also a data/interaction contract: the later A selection uses discrete indicators for actual story scenes, not elapsed time. The installed opening has one movie with four timed scenes, so four indicators describe scenes rather than four physical movie files. Keep that distinction and propagate the chosen state through layout JSON, PSD, native prefab and versioned exports.
- Before: side-profile shark, detached navigation arrows, “밀어서 출발”. After: shark from behind on the route with “좌우로 움직여 / 게임시작”.
- Before: two stat chips above two columns with five dots. After: a stacked common summary and full-width rows showing `Lv. 3 / 60`, `Lv. 7 / 30`, `Lv. 3 / 10`.
- Selected evidence: `tmp/image-previews/ui-original-variation-2026-09-14/ui-concept-c-v3-gameplay-start-upgrades.png`. This is an inspected image concept, not runtime UI acceptance.

## Related

- [Full UI suite and retained revision evidence](../../../map-concepts/harbor-ui-suite-revision-2026-09-15/README.md)
- [Revision and prompts](../../../map-concepts/combat-feedback-2026-09-14/ui-original-variation.md)
- [User requirement provenance](../workflow-issues/keep-user-quotes-separate-from-design-decisions-2026-09-13.md)
- [Actual runtime scroll and modal verification](../ui-bugs/restore-inactive-unity-modal-order-and-scroll-height-2026-09-14.md)
