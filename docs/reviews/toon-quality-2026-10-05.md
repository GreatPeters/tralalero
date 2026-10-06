# Ch1–5 bounded toon quality pass — 2026-10-05

[Verified native PNG gallery](http://127.0.0.1:1626/toon-quality-20261005/) · 31 before/after pairs · 91 byte-identical PNG copies.

## Outcome and authorized scope

Completed three bounded corrections after the user authorized “수정 필요한 거 있으면 네가 검증 후 수정해” (2026-10-05 01:18 UTC). Existing scene structure, journey, gameplay and previous toon work remain intact. Root was the sole Unity writer; `/root/quality21_visual_review` independently reviewed images, read-only. Fresh Ch1 and Ch3 samples justified no additional edit. The stopping condition is met; no broader art redesign or repeated full suite follows.

## Applied changes and asset accounting

| Scene | Actual saved change | Preserved |
| --- | --- | --- |
| HighWay | Selected apartments 12–17 and 24–29: divide existing window triangles into three quiet material groups and add four shallow stone strips per building, batched into one renderer. | Original window vertices/triangles, building massing, transforms, road, camera and combat. |
| Jamsil | Eight left/right shop bays 14–17 near distance 314–353: two niches each gain darker backing and short side/header reveals. 24 new batched renderers. | Original merchandise, doors and shelf footprint. New nearest local depth −0.5525 stays inside old shelf extent −0.65; this is not a claim that all additions sit behind the old glazing. |
| ShoeTower | The 6F→7F cabin rear MeshFilter now references two lateral glass strips: 0.72 m each, with a 7.56 m central camera aperture. | Original glass material, transform, renderer, colliders, lift logic, camera and floor sign. |

New assets are under `Assets/ShooterSurvival/Models/Chapters/Quality21_20261005`: **49 mesh assets and 7 material assets**, authored with native Unity mesh/material operations from existing geometry. Window clones preserve their source triangles. Existing model/animation assets remain reused; original source assets are not rewritten. **New TRELLIS/Meshy models: 0; paid calls/purchases/installations: 0.** This is an incremental correction to the completed implementation, not a replacement claim for the original model-generation work. No new collider, gameplay source or project setting change. New meshes have finite vertices and valid indices; shaders are supported in this Editor. Original renderer positions/scales are unchanged.

## Fresh native evidence

Unity 6000.2.6f1, PID 83488, official Pipeline 0.6.0-exp.1. Other editors were not controlled. PNGs are rendered at 1080×2340; no image edits. All 31 pairs have exactly equal recorded player position, camera position/rotation and FOV. These pairs use explicitly frozen native Play poses, not ordinary gameplay. Dynamic billboards/render timing can differ; the 6F sign was not edited and its apparent vertical difference is not claimed as a fix.

| Current-pass actual input route | Result | Conditions |
| --- | --- | --- |
| Ch2 jam / cash exit | Clear in 298.29 s; HP 1137.13 remaining | Established non-purchase growth data 20/24/10, initial ATT88/HP3300; damage and normal combat enabled. No test-power, HP pin or forced clear. Next-scene transition held only for result inspection; this run does not re-prove Ch2→3 handoff. |
| Ch4 shopping route 0 | Clear in 299.16 s; HP 56 | Basic ATT8/HP60, real choice/reward handling, security targets and ordinary pause. Actual goal automatically entered preserved ShoeTower lobby. |
| Same-session Ch5 cinema route 1 | Clear in 304.67 s; HP 29.2; 6 lifts | Basic ATT8/HP60, actual cinema choice/holdout, lift pause, final manager encounters and white-shoe goal. No scene reload between automatic lobby arrival and this route. |

Connected suite result (full per-check evidence: `validation/connected-final/suite-summary.json`):

```json
{
  "outcome": "passed",
  "completedRoutes": 2,
  "passed": 23,
  "failed": 0,
  "errors": []
}
```

Both actual-run observers recorded zero exceptions. Reward duplication and pause/resume checks are included in the connected suite. Saved preferences are restored after every run, including earned test rewards. New-game/continue and the other branches were covered by earlier implementation reports; they are not newly claimed as rerun here.

New decoration camera-to-body bounds candidates: **0/1472** Highway samples and **0/3060** connected samples. In 234 sampled 6F observations (entry, traversal and holdout, not only entry), five exact mesh rays to feet/body/head/side points recorded **0 occluded samples, maximum 0 hits**. The posed 6F offset 0 has a broad combined AABB candidate but zero mesh hits; 5/10/18 m poses also stay clear. Native actual entry/lift images received independent review. This is sampled evidence, not every pixel at every possible player position.

Independent review accepted restrained Ch2 window grouping, modest Ch4 display depth in all five actual distance 310–350 views, and the 6F glass correction; no further scoped art revision was requested. Full statement: `outputs/toon-quality-2026-10-05/independent-final-review.txt`.

Editor timing under screenshots/QA instrumentation: Highway median 17.53 ms, P95 27.39 ms; connected median 16.47 ms, P95 29.42 ms. These are Editor observations, **not mobile performance certification or before/after performance gains**.

## Preservation and restoration

- Fresh preflight contained 13,530 dependency files/metas, 622 code/settings files, ten exact scene/meta recovery copies and narrow shared font/material recovery copies on this PC.
- All **15,218 protected component records, 12,710 ancestor transforms and 3,003 colliders** match the preflight exactly, including lighting. No allowed-field normalization was needed.
- Among original dependencies, only HighWay, Jamsil and ShoeTower scene files changed. Ch1, Ch3, old model/material/font assets and all code/settings hashes are unchanged. Original renderer deltas are exactly the intended 12 window mesh/material pointers and one rear-glass mesh pointer; new renderers are the intended 12+24.
- Branch/HEAD unchanged. All 127 pre-existing tracked deletions are preserved; no new tracked deletion. No commit, push, external backup or user-file removal.
- All 78 exact typed game-owned preference keys, nine Editor session settings, locale, scene/start scene, time scale, PT_ResourcesCleanup and GameView layout/resolution/zoom match the current-session snapshot. Clean Edit restored. Own callbacks/guard released. Engine registry inspected read-only; engine-owned session deltas are recorded and never written back.
- Verification tools and their SHA256 manifest are archived under `outputs/toon-quality-2026-10-05/verification-tools`; primary receipts are in `preflight`, `validation`, `final-audit` and `gallery-manifest.json`.

## Limits and remaining observations

Window repetition remains elsewhere; near displays are still stylized shallow niches, not full stores. Distant storefront sparsity, white-clothing contrast, minor tread seams and the large unchanged 6F sign remain visual limits. Actual entry frame 1 shows transient top/HUD clipping of that sign; the outbound lift exposes the old screen/sign backside above the cabin. Neither is claimed fixed by the glass aperture correction. Mobile/APK/device testing is held per user preference.

The Highway harness recorded **5 short vehicle passage-width sample flags** near the end, alongside 0 overlapping-vehicle samples and 0 hidden-body samples; the route cleared normally. This is retained as a traffic observation, not concealed or claimed fixed. New decor has no collider/body-ray intersections, and protected traffic/gameplay data are unchanged; the bounded art pass does not redesign traffic balance.

The historical 1,135-test/52-failure state was not rerun or represented as fixed. No full suite was invoked. Actual Play serialized one existing bonus decal material with a tiny `_Color.r` rounding change (0.055 to 0.054999977). The changed file was preserved as evidence, then restored from the hash-verified fresh-session copy and reimported individually; all 13,530 dependency hashes were rechecked. No shared font changed. One read-only result request initially failed automatic approval review because the review model was at capacity; the identical read later passed normal review. No bypass, security change or unresolved approval blocker remains.

Related completed implementations: [Ch1–4](chapters1-4-toon-implementation-2026-10-04.md), [Ch5](ch5-toon-implementation-2026-10-04.md). Original generation provenance and earlier journey/continue/branch results remain in those reports and their linked production records.

## Subsequent bounded Highway investigation

The retained five flags were investigated separately after this art pass. They span three actual contact episodes, with two intermediate samples still having continuous free space. Two input policies yield the same damage. No HP cap or gameplay change was applied because the latest compulsory-combat design and available evidence do not justify a no-hit rebalance; human fairness remains unresolved. The historical five count and the Ch4/5-only 23 checks above are unchanged. [Full follow-up, geometry evidence and exact preservation](highway-clearance-2026-10-05.md).
