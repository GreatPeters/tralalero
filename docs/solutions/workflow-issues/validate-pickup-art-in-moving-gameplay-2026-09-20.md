---
title: Validate pickup artwork in moving gameplay before accepting fidelity
date: 2026-09-20
category: workflow-issues
module: Bonus talisman visual authoring
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - A reference-based Unity effect passes tests but looks crude or invisible
  - Frozen animation samples do not establish in-game readability
tags: [unity, blender, reference-fidelity, vfx, video, visual-review]
---

# Validate pickup artwork in moving gameplay before accepting fidelity

## Context

The first talisman shipped with passing functional tests but squat paper, tiny stock icons, a box clip and barely visible absorption. The user rejected it against the concept. Technical validity and visual acceptance had been conflated.

## Guidance and correction

1. Freeze original assets, code and native captures before replacing them. Write a visual fail/pass ledger grounded in the actual and reference images.
2. Repair primary shapes with an appropriate representation. This iteration used separated Blender paper meshes, deliberate bevel normals, a bent enamel clip, metal rivet and raised emblem meshes. Do not add glow to disguise a flat placeholder.
3. Keep source, GLB reimport and Unity rendering as separate checks. Here source/reimport triangle count and bounds matched; neutral and identical-camera studio renders exposed the actual geometry.
4. Record a real physical pickup while the character moves. Fixed animation-phase captures concealed two problems: the unfolded object overlapped the character, and the curved transfer was occluded by opaque skin. A short camera-relative reveal and depth-independent transfer ribbon restored the visible causal chain. The body target stayed common to all bonuses.
5. Separate material roles. Reusing glossy navy enamel on a caption caused complete specular whiteout in other chapter lighting. The text background now uses an explicitly non-specular material, covered by a resource test.
6. Treat serialized legacy UI references as optional presentation data. Some old Bonus prefabs applied the correct reward but had no statValueTmp reference. The label now falls back to the formatted rolled data value; a regression test covers +14% without UI text.
7. Record fixed-capture-rate videos honestly: 30fps replay demonstrates simulation/visual timing, not device performance. Preserve both final videos and their collision/effect-frame metadata.

## Evidence

[Rework, source artifacts, iteration ledger and 39 passing tests](../../../map-concepts/talisman-polish-2026-09-20/README.md). Three native clips each show a physical claim and 30 effect frames. Functional reward/choice/drop/reset rules remain intact.

Related: [detached effect lifetime](common-bonus-feedback-survives-root-deactivation-2026-09-20.md), [asset-grounded VFX](ground-vfx-concepts-in-reusable-parts-2026-09-19.md).

Captured with ce-compound mode:headless. Review ran sequentially per AGENTS.md; no independent artistic score or exact-reference claim is implied.
