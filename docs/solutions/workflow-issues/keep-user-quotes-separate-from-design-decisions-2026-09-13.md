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

## Preview review follow-up (2026-09-28)

A plan can be newer than its reference pictures while still carrying older assumptions. The Noryangjin preview request exposed three independent layers: the direct requirement register specified the current auction rewards, smaller human-height boss and Meshy-to-TRELLIS fallback; the handoff still listed budget questions; earlier images still depicted a large boss, overlapping support aircraft and an unconfirmed coin multiplier. Reusing those pictures without adjacent corrections would silently restore superseded choices.

For an application-preview homepage, label each asset as an actual capture, historical concept or new target concept. Put material differences beside the relevant image, not only in a remote source document. Keep proposals such as coin-loss penalties and boss merging visibly undecided. An attractive generated view does not establish achievable mobile rendering, final balance, route order or native implementation. The new review board explicitly separated indoor tile/ceiling and turret readability goals from those verification claims.

The request remained preview-only: no Unity or model-generation commands ran. SHA-256 checks preserved both Noryangjin scenes, Build Settings, installer, handoff and user requirement register. The local homepage verified its two comparison states, route choices, ten event views, image dialog and narrow layout. The browser's requested viewport and actual viewport differed; report observed dimensions rather than treating a requested device size as verified. Evidence: [preview README](../../../outputs/noryangjin-review-homepage-2026-09-28/README.md), [interpretation and sources](../../../outputs/noryangjin-review-homepage-2026-09-28/site/review-notes.md).

Captured with `ce-compound mode:headless`, sequentially per AGENTS.md. Existing guidance had high overlap and was updated. No session history or GitHub issue search was needed; the available tools did not expose a GitHub issue search connector or `gh` executable. Instruction-file discoverability already passes.

## Related

- [User source register](../../design/USER_STATED_REQUIREMENTS.md).
- [Primary game design](../../../GAME_DESIGN_OVERVIEW.md).
- [Verify anatomy against real references](verify-character-anatomy-and-real-support-in-generated-assets-2026-09-13.md).
