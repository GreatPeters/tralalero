---
title: "Ground game VFX concepts in reusable parts and inspect rejected effects"
date: "2026-09-19"
category: workflow-issues
module: "Bonus Wall concept artwork"
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - "Generating game concept art constrained by existing prefabs and solo-developer implementation."
tags: [imagegen, bonus-wall, prefab-reuse, vfx, concept-art]
---

# Ground game VFX concepts in reusable parts and inspect rejected effects

## Context

The user asked for sacred Bonus Wall concepts that fit the game design and can be built with existing assets, simple new geometry, or optional TRELLIS output. Earlier ornate illustrations did not show the concrete assembly clearly. A revised wood-table concept also reintroduced small star particles previously rejected by the user.

## Guidance

- Separate mechanical references from visual quality targets. The user found Arrow a Row's appearance too crude despite its structural similarity. Preserve choice readability while raising craft through silhouette, restrained material contrast and beveled edges. See [polished pickup concepts](../../../map-concepts/bonus-polished-pickups-2026-09-19/README.md). When transit arrows regress, an after-collection inset can communicate the state clearly without implying reversed absorption.
- Read the relevant design section before adding narrative motifs. Here, GAME_DESIGN_OVERVIEW.md §12.5 defines temporary power as gosa offerings absorbed by cursed shoes, distinct from the stage-start resurrection altar.
- Search actual prefab paths and distinguish file existence from visual inspection. A Pig_Head or Lantern_A filename supports a reuse candidate; it does not prove the generated drawing matches that mesh.
- Show assembly, gameplay idle and pickup together. Specify broad low-poly shapes, flat surface details and a bounded effect budget. This makes simplification visible.
- Keep asset generation separate from effect implementation. Static altar parts may use existing models or optional generated meshes; moving absorption trails and transparent halos still need Unity implementation.
- Inspect each output. Negative prompt instructions did not prevent stars in candidate 03. A targeted edit removed the stars from its pickup panel while preserving the assembly and idle views.
- Do not interpret a still image as verified gameplay state. The correction retained a faint offering silhouette; some images put glow above the intended ankle position. Cleanup, single-choice semantics and effect attachment still need native implementation checks.
- Retain exact tool-returned output paths, save versioned project/preview copies, and record correction prompts.

## 2026-09-20 follow-up: separate concept diversity from chapter variation

The user corrected a proposal that changed offering props by chapter: bonuses must look the same in harbor, highway and rest stop. For a ten-concept request, vary identity and construction BETWEEN candidates, and lock geometry/materials/icons WITHIN each candidate across all chapters. Put the same candidate in three environment panels so this requirement can be inspected directly. A chapter-themed source prop should not silently become mandatory for a universal pickup.

Ground feasibility in the current code: BonusRewardCue exposes acquisition but emits at player origin plus 1.65 m; a shoe-targeted effect still requires new anchoring and motion. Static rigid meshes, simple extrusions and fixed cord curves support the ten candidates without cloth/refraction simulations. Distinguish a proposed simple implementation from already-built geometry, exact rendered fidelity or verified mobile performance. During review, candidate 01's studio hoop had an unintended gap that lower gameplay hoops lacked; a versioned ImageGen correction aligned them and restored the header contrast.

Evidence: [ten universal concepts and implementation mapping](../../../map-concepts/universal-bonus-2026-09-20/README.md). Existing learning updated through ce-compound mode:headless after local overlap review; sequential research per AGENTS.md, no additional history or issue search. All 10 PNGs passed structural validation; Chrome gallery and click enlargement were verified.

## Pickup flow diagrams must preserve event count and effect lifetime

The folded-talisman follow-up showed a second semantic trap: repeated motion afterimages looked like multiple pickups. A targeted edit reduced candidate 03 to one small folded object at the shoe; the explicitly three-part candidate remains three visual pieces of ONE reward. Inspect event count as well as travel direction. Final frames should distinguish fading cosmetic marks from the persistent stat change.

Read the pickup ordering before proposing an animation delay: WallScript applies the reward, calls PlayPickup, and then disables the lifetime root. Any new folding/travel effect must therefore survive outside that root, while reward application remains immediate and once-only. This is a design constraint confirmed from source, not a runtime implementation completed by the storyboard task. [Four flows, code grounding and corrected evidence](../../../map-concepts/talisman-pickup-flows-2026-09-20/README.md). Captured through ce-compound mode:headless by updating this overlapping learning.

## Ground feedback semantics without multiplying effect variants

**Later correction supersedes the per-stat recommendation below:** the user explicitly requested ALL upgrades absorb into the torso, because maintaining per-upgrade targets was unnecessary. One neutral collection animation with data-driven icon/text communicates receipt without claiming the body part literally owns that stat. Respect this simpler common effect; do not infer that rejecting attack-to-shoes mandates separate destinations for every bonus. Preserve the real gameplay effect, including helper summons, independently of shared feedback. [Latest common torso storyboard](../../../map-concepts/talisman-common-body-2026-09-20/README.md). Updated via ce-compound mode:headless after reviewing this now-overbroad guidance.

The user rejected attack pickup effects on the sneakers: the cursed-shoe story did not make footwear the target of attack upgrades. Preserve the liked talisman object, but map attack feedback to the visible firing origin and health feedback to the body. Verify that origin before drawing: WeaponManager.currentWeapon names the logical weapon; WeaponScript.ResolveProjectileSpawnPosition uses the visible shark mouth anchor. A gun added merely because of the class name would introduce a second visual error.

The corrected sheet shows attack to mouth muzzle and health to torso, with footwear unlit. Source inspection and the native HUD reference grounded the decision; this does not mean the generated side-view character or projectile art is an exact native render. [Correction and prompt](../../../map-concepts/talisman-stat-targets-2026-09-20/README.md). Captured with ce-compound mode:headless; prior overlapping flow guidance was reviewed and the superseded recommendation marked explicitly.

## Why This Matters

A visually simple concept can inform an implementation without claiming that a matching prefab or working effect already exists. Explicit construction views reduce hidden modeling work; per-image inspection catches previously rejected effects that return despite prompt constraints.

## When to Apply

Use for asset-grounded game props, pickup VFX, UI previews and requests emphasizing realistic production scope.

## Examples and Evidence

- [Verified candidate paths and construction mapping](../../../map-concepts/bonus-sacred-altars-2026-09-19/FEASIBILITY.md)
- [Ten revised sheets, prompts and targeted correction](../../../map-concepts/bonus-practical-altars-2026-09-19/README.md)
- All final PNG files were reopened successfully at 1536×1024. This verifies saved image artifacts, not Unity behavior.

## Related Guidance

After further user rejection of the proposed drop/source storyboards, compare real genre precedents before adding more fictional machinery. Arrow a Row's official store screenshots use concise paired banners, while Gun Head Run emphasizes icons, signed changes and color. These are observations from published images, not verified runtime behavior. Separate a user's relative preference for a chapter-context sheet from approval of its entire fiction or implementation. [Reference research](../../../map-concepts/runner-references-2026-09-19/README.md).

## Follow-up: validate the spawn context before redesigning the object

The user challenged why a gosa table would suddenly exist on the route. Reading EnemyScript_space showed that permitted enemy deaths instantiate the bonus prefab. Reusing a low-cost altar mesh solves production cost but not this narrative mismatch. Check authored placements and runtime drops separately before committing to a prop. The subsequent equipment/supply absorption storyboards are an explicit proposed fiction revision, not an approved replacement for the design document. See [context revision evidence](../../../map-concepts/bonus-context-revision-2026-09-19/README.md).

Directional arrows are semantic content. The first fixed-choice sheet pointed from shoes to equipment. Two edits left that ambiguity, so a new discovery/after-absorption board removed the transit-frame problem. Record failed corrections and inspect actual output rather than treating an edit prompt as verification.

- [Separate reference layout, identity and lighting](separate-reference-layout-product-artwork-and-lighting-2026-09-15.md)
- [Preserve generated outputs and preview links](persist-generated-image-before-next-prompt-2026-05-16.md)
