---
title: Audit highway branches with combat and coverage separated
date: 2026-09-26
category: workflow-issues
module: Highway route and scenery QA
problem_type: workflow_issue
component: testing_framework
severity: high
applies_when:
  - "Diagnosing scenery blocking a route that normal combat attempts cannot fully traverse"
  - "Presenting automated Unity playthroughs as evidence for branch or balance decisions"
tags: [unity, highway, route-branches, scenery-bounds, playtest, evidence]
---

# Audit highway branches with combat and coverage separated

## Context

The user supplied a forest-obscured recovery-road screenshot, asked what left/right choices do, and requested about ten Highway plays plus a website report. This was an analysis request; no scene or gameplay correction was applied.

Current scene: `HighWay`, length 2820. Forks are 80–310 (+30 m right offset) and 1680–1960 (+32 m). Right selects recovery, left/center selects mainline at the fork start. The midpoint grants up to 10% max HP once; the same progress clock drives both routes.

## Evidence and findings

- Ten fresh 3x-speed native Play Mode combat attempts used the existing save: initial HP500, attack68, upgrade levels ATT15/HP8/speed1. Seeds 202609261–202609270. No health pin, collider disable, teleport or upgrade purchase. Preferences were reset between attempts. Movement was submitted once per game frame.
- Five left-intent attempts died around65m **before** the80m decision; do not call these completed left-branch trials. Five right-intent attempts selected and exited the first bypass, then died around334m. All final damage was `EnemyContact`. The driver and low entry stats limit inference; this is not a human win-rate estimate. The second fork was not reached by these ten combat attempts.
- Two explicitly separate visual surveys held HP at100000 and disabled player colliders, traversing2820m on mainline and both bypasses. They timed out at420game seconds. Disabled colliders also skip the completion trigger; `Completed=false` here does not establish a gameplay clear bug.
- Actual screenshots reproduce trees covering the first bypass and a large black face near its midpoint. A foliage search limited to names containing `tree`/`forest` reported zero branch overlaps. Including `ridge` found31 sample overlaps, every5m from135 to285, on `Noryangjin_MapTool/KoreanHighway_20260925/01_Scenery/Mountain_Ridge/Fit/Orientation/Model`.
- That renderer's bounds are approximately120×54×176m. `install-highway-korean.cs`'s bypass cleanup skips scenery over60m in X or Z, leaving this merged mountain-and-tree model out of corridor cleanup. Screenshot plus bounds evidence is stronger than individual-tree naming. The exact shader reason for the black face was not isolated.

## Guidance

1. Preserve original preferences and clean scene hashes before automated plays. Restore key **presence** as well as values. Here66 common+11 extra keys returned with zero mismatches, wallet36679/jewels0, TimeScale1 and HighWay clean in Edit Mode. Scene SHA256 remained `6D1E1E17497A845BD821449C6F7E0E68FA517D2680C9092BDC3AE067EFDB01D6`.
2. Separate ordinary combat attempts from invulnerable visual traversal. Record both attempt intent and actual branch flags; mark unreached branches explicitly. Use `ChapterProgression.Completed` rather than positive HP to recognize a real clear.
3. Inspect merged environmental meshes, route corridor and camera rays. Huge scenery exclusions and naming filters are useful for avoiding destructive edits but are not proof that a corridor is clear.
4. Show the captured failure, not only a route diagram. The report's schematic is labeled as simplified; original JSON and native screenshots remain available.
5. Keep the web server scoped to the report's `site` directory. Preference snapshots stay outside the served folder.

## Reproduction and report

- `tools/highway-analysis-20260926.cs`: read-only scene inspection, preference snapshot, ten ordinary attempts, restoration verification. Run via official `unity command run_script`; do not rerun `Prepare`/`Begin` over existing evidence.
- `tools/highway-coverage-20260926.cs`: two separate visual surveys after the combat cohort. Runtime-only health/collider changes are cleared on exiting Play Mode.
- `tools/build-highway-analysis-report.py` and `tools/highway-analysis-report.html`: build the Korean interactive report with `py -3.11 tools/build-highway-analysis-report.py`. Python3.11 has Pillow here; the default3.12 did not.
- `outputs/highway-analysis-2026-09-26/`: native PNGs, timelines, results, bounds/camera-ray records, conditions, original preferences and restored-state verification.
- `outputs/highway-analysis-2026-09-26/site/index.html`: standalone report. `python -m http.server 8775 --bind 127.0.0.1 --directory "outputs/highway-analysis-2026-09-26/site"` serves it locally. Browser verification covered rendering, second-fork switch, left/right comparison, run10 detail, gallery switch and image dialog.

## Related

- [Separate road placement and driver errors from campaign balance](separate-road-placement-and-driver-errors-from-campaign-balance-2026-09-23.md)
- [Protect active Unity scenes from broad EditMode test runs](protect-active-unity-scenes-from-broad-editmode-test-runs-2026-07-18.md)
