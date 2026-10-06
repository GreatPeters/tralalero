---
module: Chapters45 environment tooling
date: 2026-10-02
problem_type: workflow_issue
component: Unity scenery and QA orchestration
symptoms:
  - Scenic escalator endpoints looked connected from gameplay but needed close inspection
  - Older directed fixtures required a backup file no longer created by Prepare
root_cause: Evidence and snapshot contracts changed independently of fixture assumptions
resolution_type: workflow_improvement
tags: [unity, scenery, native-capture, preservation, qa]
---

# Check visible connections and current QA snapshot contracts

The department-store work retained gameplay routes but introduced decorative mezzanines. Independent inspection found reversed outer storefronts, floating upper escalator endpoints and an entry sign hidden behind the retained canopy. Matching gameplay captures plus explicitly labelled architectural closeups exposed different defects. Connect visible endpoints at their actual authored heights, then re-open the saved scene and inspect it again. Do not treat an alternate camera as proof of ordinary gameplay visibility.

Current `Chapters45Playtest.Prepare` references an already existing preference TSV after verifying all current values and its hash. It does not create a fresh `before-prefs.tsv`. The old lifecycle/campaign fixtures therefore needed scoped copies that call `ValidatePrepared`, with Playtest's code compiled into the same dynamic script after domain reload. Do not weaken the guard or silently use historical preference values.

Pipeline `test_status` can return its JSON object encoded as a string. Normalize it, and await terminal status even when result processing raises an error, before restoring settings or shared assets. A first parser error was recovered by reading the completed37/37 result and restoring again after completion; the corrected final run completed normally.

Evidence: [department-store report](../../reviews/department-store-2026-10-02.md), native per-scene receipts, final art-detail images, `focused-*.json` and preserved `qa-v2/` results under `outputs/department-store-2026-10-02/`. Ordinary route input, seeded transaction fixtures, editor counters and AI model provenance must remain separate claims.


## Follow-up: verify actual local light and access boundaries

The v5 follow-up compared identical geometry and camera poses with only six new lights toggled. Pixel differences and independent inspection established localized warm light; one view was identical. Material emission alone was not treated as proof of illumination. Deeper retail and reused plants increased the measured floor0 render cost, which remains separate from phone performance.

Existing generator launchers can exist while their execution or sockets are denied. Record refused endpoint status, process-create access denied, Python dependency absence and external socket denial separately. Do not equate a connection failure with invalid credentials or zero account balance, and do not bypass a specifically denied path. See the exact [generator diagnosis](../../reviews/department-generation-diagnosis-2026-10-02.md) and [follow-up result](../../reviews/department-store-backend-followup-2026-10-02.md).
