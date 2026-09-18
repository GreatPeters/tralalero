---
title: Keep direct user requirements separate from implementation decisions
date: 2026-09-13
category: workflow-issues
module: Game design requirements provenance
problem_type: workflow_issue
component: documentation
severity: medium
applies_when:
  - "A user asks to preserve only requirements they personally stated."
  - "Long-running design documents mix user corrections, proposals and implementation reports."
tags: [requirements, provenance, user-quotes, design-docs, corrections]
---

# Keep direct user requirements separate from implementation decisions

## Context

The user requested that their own explicit principles be saved without the agent's guesses. Existing design sections still described three independent legs, an unbranched straight route, and chapters after Highway as undecided despite later user corrections. A balance paragraph also presented an approximate twenty-attempt target with no forced count gate as though that interpretation came from the user.

## Guidance

Maintain a source register with stable requirement IDs and direct user excerpts. Preserve conditions, approximations, corrections and task-specific tool choices. A translation request containing a third-party generation plan is not an endorsement of that plan. An unspecified “apply that” is not evidence for a candidate ID.

Separate direct requirements from design decisions and observations. “Stay still for about thirty seconds while police approach from all sides” does not itself specify automatic aiming, four doors, three phases or enemy health. Record those choices in implementation documents. Preserve the user's twenty-plays-per-chapter wording without silently changing it to an average or inventing a locking mechanism.

Link the register from the primary design document and project reading instructions. Correct contradictory current prose as well as adding a new section; otherwise an old “final principle” can silently override the source. Keep unverified older lore as existing design text rather than claiming the user authored it.

This guidance is an agent documentation practice, not an additional set of user-authored game requirements.

## Why This Matters

A successful implementation does not prove user authorship of every detail. Context summaries and historical test results are useful records, but promoting them into absolute requirements causes design drift and can erase explicit corrections.

## Examples

The source register contains41items with quoted user evidence. The primary game, map and balance documents link it; contradictory anatomy, chapter order and universal unbranched-route wording were corrected. The approximate-count interpretation remains labeled as an implementation choice, not a user instruction. Documentation checks verified unique sequential IDs, a quotation for each item and local link targets. No gameplay or workbook values were changed by this documentation task.

## Related

- [User source register](../../design/USER_STATED_REQUIREMENTS.md).
- [Primary game design](../../../GAME_DESIGN_OVERVIEW.md).
- [Verify anatomy against real references](verify-character-anatomy-and-real-support-in-generated-assets-2026-09-13.md).
