---
title: Verify Dynamic Route Plans at Campaign and Chapter Scales
date: 2026-07-19
last_updated: 2026-07-19
category: docs/solutions/design-patterns
module: Campaign route planning artifacts
problem_type: design_pattern
component: tooling
severity: medium
applies_when:
  - "Building Excel and PNG stage-route plans from target times and module counts"
  - "Both a campaign overview and per-chapter sheets must communicate traversal rhythm"
  - "Planning alternate map geometry without modifying the active Unity scene"
  - "A denser-turn alternative must be compared without replacing an approved baseline"
tags: [excel, route-design, stage-design, visual-regression, noryangjin, openpyxl, orthogonal-routes, layout-variants]
---

# Verify Dynamic Route Plans at Campaign and Chapter Scales

## Context

A five-chapter route plan correctly matched its target times, module counts, and total distance, but the first overview still read as a mostly straight out-and-back path. The next revision overcorrected with continuous curves, loops, and frequent direction changes. A user-drawn reference clarified the actual target: the current Noryangjin scene should remain a small footprint inside a much longer red route, and the blue highway should continue from the same transition point using long straight runs and only a few large bends.

PDF rendering also exposed a presentation-scale mismatch: the 4K overview could look acceptable while several Excel chapter sheets still clustered their turns at one end or used a different route rhythm.

The numeric contract was correct in both cases. The route silhouette and pacing communication were not.

## Guidance

Keep route scale and route shape as two separate contracts.

First, freeze the measurable contract: target duration, module count, distance, and display-cell count. Changing the silhouette must not silently shorten the stage. Validate these totals after every workbook rebuild.

Second, derive the route grammar from the reference image instead of treating "dynamic" as "more curves." In this case the grammar is long orthogonal segments separated by major 90-degree bends. The final corner budgets were Noryangjin 7, highway 3, rest stop 4, city 5, and department store 5. That places major direction changes roughly 24-60 seconds apart instead of every few seconds.

Use the existing scene as a visible scale anchor. The current Noryangjin route is 21 modules and about 43.5 seconds, while the 4:30 target requires 142 modules. The planned red route therefore needs to read as roughly 6.8 times the existing route, not merely occupy a larger bounding box.

Adjacent chapters share the same transition point: Noryangjin END is highway START, and the same invariant applies at every later chapter boundary.

When feedback asks for a more bent route after a readable baseline already exists, preserve the baseline as its own artifact and create a named variant. Keep the same time, module, distance, cell-count, start, end, and transition contracts; change only the route silhouette. The denser B variant used corner budgets of 10/6/5/8/9 (38 total) while the A baseline remained 7/3/4/5/5 (24 total). The extra turns are medium doglegs and block bypasses, not alternating one-cell zigzags, so the average major turn still represents roughly 20-34 seconds of play.

For spreadsheet cell maps, prefer explicit orthogonal waypoints when the silhouette is part of the deliverable. A constrained random walk can satisfy start, end, and cell-count requirements while still concentrating most turns at one end of the sheet.

```python
chapter_waypoints = [
    (29, 22), (25, 22), (25, 16),  # current route: left, then up
    (31, 16), (31, 10), (8, 10),
    (8, 19), (20, 19), (20, 15),
]

path = waypoint_grid_path(chapter_waypoints, target_count=71)
```

The waypoint expander should assert that every segment is orthogonal, no cell is repeated, and the final path contains exactly the required number of cells. This makes the intended turns reviewable in code and keeps the scale deterministic.

Finally, verify at both presentation levels:

- Embed the supplied sketch in a dedicated interpretation sheet so the route decisions remain auditable.
- Render the 4K campaign overview and confirm that long segments dominate, corner counts match the budget, the current-scene footprint is visibly smaller, and transitions are continuous.
- Recalculate the workbook in Excel, export every sheet to a one-page PDF, and inspect a contact sheet plus the individual chapter maps.
- Assert workbook totals, phase durations, corner counts, transition markers, route colors, formula results, embedded-image count, PDF page count, and PNG dimensions.

On Windows, keep PowerShell QA scripts ASCII-safe where possible. Discover a Korean-named workbook from its ASCII parent directory with `Get-ChildItem -Filter '*.xlsx'` instead of embedding the full Korean filename in a Windows PowerShell script that may be decoded with the wrong code page.

Make verification inspect the workbook it actually produced. Read and compare `workbook.sheetnames` before asserting exact order; otherwise a validator can reject a correct workbook because it assumed a different sheet sequence. For inline HTML previews, render the fragment to a standalone page, invoke headless Chrome with an isolated `--user-data-dir`, and allow the screenshot file to flush before checking it. Chrome can print that it wrote the image just after the shell's first existence check, and `Start-Process` can return exit code 13 when its argument quoting or profile state differs from direct invocation.

## Why This Matters

Duration and distance only show how much route exists; they do not show how the route feels. A numerically correct path can still look like a one-minute corridor when its turns are clustered or hidden at the edge of a sheet. The opposite failure is also possible: excessive loops and micro-turns make a route look busy while contradicting a reference built from long straights. Multi-scale visual review catches both mismatches.

Explicit route grammars also make chapters easier to distinguish. Controlled straightaways and sparse major bends create recognizable beats without changing the underlying module budget or touching the Unity scene under reference.

## When to Apply

- A route-planning workbook uses display cells as a proxy for many Unity road modules.
- A single continuous campaign is split into independently restarted chapters.
- A hand-drawn route reference communicates proportions and turn density more clearly than prose.
- The overview looks plausible but individual chapter diagrams may use a different straight-to-corner rhythm.
- Numeric QA passes while stakeholder feedback says the route still feels too short or too linear.

## Examples

Before: generate a path that meets the exact cell count and endpoints, add curves whenever the user asks for a more dynamic route, and assume the displayed stage will read as long and varied.

After: preserve the same 626 modules, 315 display cells, and 20-minute target; embed the user's sketch; show the existing 21-module Noryangjin footprint inside a 142-module plan; budget only 7/3/4/5/5 major corners; render all 12 sheets through Excel; inspect both the 4K overview and chapter maps; and reject both micro-zigzags and falsely short-looking layouts.

Alternative B: copy the approved A workbook and image to a variant-specific output directory, replace only the overview and five chapter route diagrams with 10/6/5/8/9-corner paths, then rerun the same formula, transition-marker, embedded-image, 4K-dimension, and 12-page PDF checks. This produces a visibly less linear comparison without erasing the baseline or changing Unity content.

## Related

- [Use Continuous Procedural Bases For Unity Stage Layouts](use-continuous-procedural-bases-for-unity-stage-layouts-2026-05-25.md)
- [Keep Generated Map Tool Layouts Inside Work Grid Bounds](../developer-experience/keep-generated-map-tool-layouts-inside-work-grid-bounds-2026-06-21.md)
- [Verify MeshyAI workbook migrations with stable selectors](../workflow-issues/verify-meshyai-workbook-migrations-with-stable-selectors-2026-06-01.md)
