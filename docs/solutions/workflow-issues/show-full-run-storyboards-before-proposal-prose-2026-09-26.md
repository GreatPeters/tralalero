---
title: Show full-run storyboards before proposal prose
date: 2026-09-26
last_updated: 2026-09-26
category: workflow-issues
module: Highway proposal website
problem_type: workflow_issue
component: documentation
severity: medium
applies_when:
  - "A user asks to see the whole game flow visually after receiving a detailed proposal"
tags: [storyboard, visual-planning, svg, game-design, documentation]
---

# Show full-run storyboards before proposal prose

## Native appearance beats an aspirational render

The user then rejected the realistic generated expressway as a predictor of their game's graphics. For "how will it look in my game", use existing Unity models, materials, lighting, HUD and the gameplay camera in disposable Play Mode staging. Preserve a same-station baseline, record which actors are hidden or newly staged, avoid saving the scene, restore preference values and key presence, and verify the original scene hash. A polished generated concept is not evidence that the real assets can achieve that rendering style.

The native comparison uses station1000after a first curve at450hid the rightmost vehicle. Do not adjust the camera silently to make a mockup look better. Capture with the existing portrait dimensions: the generic Pipeline screen capture's1280×720default stretched this Game View and normalized its relative path under Assets. Direct ScreenCapture retained1080×2340and the intended outside-Assets location. The incidental capture asset was archived and removed through AssetDatabase, with its directory contents checked before removal.

Record and images: `tmp/image-previews/highway-native-three-lanes-2026-09-26/README.md`. This is visual staging, not implemented or tested traffic gameplay.

## Latest direction: the concepts themselves were rejected

After the simplified three-frame revision, the user said all five ideas were poor and requested an ordinary Korean expressway, exactly three lanes, and all traffic approaching against the player's direction. This is a concept correction, not merely another request to improve legibility. Do not keep recommending the previous five ideas or same-direction overtaking. The new deliverable is one realistic highway concept image, not another mechanic diagram; see `tmp/image-previews/korean-highway-three-lanes-2026-09-26/README.md` for the built-in image generation prompts and final file. A targeted edit removed the first candidate's unwanted extra carriageway and distant traffic so the lane count and direction rule remain unambiguous. No Unity implementation was authorized or performed.

## Follow-up: a large storyboard was still too hard to understand

The user subsequently rejected the readability of all five ideas, even after the six-scene sheets were added. More drawings did not reduce the number of concepts, reading directions, view toggles, time labels and unfamiliar icons that had to be understood at once. The earlier one-sheet-first recommendation below is retained as history, not the current primary presentation.

The current primary page uses plain action names (avoid and shoot, protect the truck, choose a road, open a gate, chase the tow truck) and only three large frames per idea: **situation → player action → result**. Each has one short caption, an explicit subject, and a consistent spatial setup. A five-step verbal run summary preserves the whole run; one representative left/right example explains the choice. All timing tables, percentages, extra branches and implementation notes remain in a separate detailed document/page.

New renderer: `tools/build-highway-simple.py`; authored plain-language content: `outputs/highway-concepts-2026-09-26/simple.json`. The primary page has no view-mode switch or forced850px image width. Mobile reading proceeds down a single column. The original full proposal and sheets remain under `details.html` and `2026-09-26-highway-five-concepts-detailed.md`.

Validate understanding before celebrating output volume: can someone identify **who moves, where they move, and why that helps** from the three frames and captions? Count checks (5ideas,15images) verify completeness, not comprehension. Treat user corrections as evidence that the previous communication did not work, even if the diagram technically contained every requirement.

## Context

The five Highway proposals had detailed prose and one three-step gimmick diagram per idea. The user corrected the deliverable: show the **whole flow**, mainly as pictures. A local gimmick diagram and textual time tabs did not communicate a full run's sequence or the connections between choices and later outcomes.

## Guidance

- Put a complete start-to-finish storyboard first. Here each proposal has six illustrated scenes connected in chronological order, followed by explicit left/right reward and merge diagrams. Preserve the original300-second timeline and11branch choices rather than inventing a new design during illustration.
- Provide both a one-sheet view and a larger scene view. Keep detailed prose in a closed disclosure so it remains available without competing with the requested pictures.
- Label drawings as proposals, not gameplay captures or actual map coordinates. A branch drawing must not silently imply that both mutually exclusive rewards are always obtained. Later illustrated support effects are labeled as an example path.
- Retain vector files in the generated site and embed them in the durable Markdown; provide individual SVG and five-sheet ZIP downloads. Rebuild through `py -3.11 tools/build-highway-concepts.py`.
- In composite SVGs, clip each illustration to its viewport. The first visual review showed a wide fork road spilling over the panel title and caption; a shared scene clip corrected it. Pure XML validity would not detect that layout defect.
- Check the longest branch layout and switch both display modes in the browser. Rendered label and diagram quality matters more than the number of boxes in the flowchart.

## Evidence and boundaries

`tools/highway-flow-art.py` produces5full sheets,30scene SVGs and5branch crops. `tools/highway-concepts-flow.css` and `tools/highway-concepts-flow.js` integrate these into the existing website. The builder also updates the existing ideation Markdown. Browser review covered the three-fork proposal at full size, corrected panel clipping, and the six-scene display with prose collapsed.

Game code, scenes, prefabs and gameplay settings were not changed. The existing HighWay SHA256 remained `6D1E1E17497A845BD821449C6F7E0E68FA517D2680C9092BDC3AE067EFDB01D6`.

## Related

- [Original proposals](../../ideation/2026-09-26-highway-five-concepts.md)
- [Keep combat and visual coverage separate](audit-highway-branches-with-combat-and-coverage-separated-2026-09-26.md)
