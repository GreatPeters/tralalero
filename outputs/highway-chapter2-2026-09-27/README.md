# Chapter 2 Highway — implemented and verified

The requested chapter is saved in Assets/ShooterSurvival/Scenes/Tools/HighWay.unity. Its canonical input is the revision 2 markdown plus the user's implementation request and the eleven images linked by the current reference page. No retired proposal was used. No commit or paid model generation was performed.

[Visual comparison and route results](http://127.0.0.1:8776/highway/implementation/) · [Requirement map](requirements-verification.md) · [Review](review.md)

## Subsequent Meshy conditions

After this implementation, the user raised the future Highway model budget from200to2,000credits and explicitly included replacements for polyperfect/ithappy/older production models to unify the tone. Before any new Meshy generation, present the proposed asset list and estimated credits, create Codex concept images and obtain the user's confirmation of those images. Use Meshy7.1, no more than15,000polygons and the Noryangjin outline material through the existing tools; at most two regenerations per model. User-provided planning estimates are30credits/model,5/rig and3/animation, not verified current pricing. The implementation recorded here still used0external-generation credits. No new concepts or model generations have been approved by this conditions update alone.

## Saved implementation

- Three 4.4 m lanes on each carriageway, 2,340 m route, two popup forks, shark scale 0.8 and camera distance 1.15.
- 225 vehicle enemy placements, zero ranged human enemies, 18 northbound background cars, 12 ordinary bonus pairs and two random gates. Gameplay numbers come from Data.xlsx.
- Accident queues, tanker chains, slow/dense versus fast/sparse branches, police/tow wave, one irregular log, construction, deer and fatal potholes, four unique effects and reactive news.
- Meshy assets were reused with outlined native details. External generation cost: 0 credits. Unique effects last for the current Highway run.

## Accepted native play records

Each full run used ordinary purchased-level stats: ATT level 20, HP level 24, speed level 10; initial HP 3,300 / ATT 88. The driver uses normal player input and collisions at normal game speed, except the production popup slow motion. No 9999 stats, pinned health, invulnerability, teleport or disabled collision was used.

| Scenario | Accepted evidence | Result |
|---|---|---|
| First-entry accident | play-v4/first-accident/result.json | Weak taxi destroyed; accident car still 608 HP; passed at 35.8 s, HP 960 unchanged / ATT 48 |
| Jam → Hi-pass | play-v4/jam-hipass/result.json | Clear, 298.3 s |
| Jam → Cash | play-v5/jam-cash/result.json | Clear, 298.3 s |
| Open → Hi-pass | play-v4/open-hipass/result.json | Clear, 298.3 s |
| Open → Cash | play-v6/open-cash/result.json | Clear, 298.3 s |

All four completed through ChapterProgression.Completed. Each recorded two popups, two random gates and one released log. Regular-car wrong-direction, vehicle-overlap, three-lane moving-wall and effect-overflow counters remained zero. This is measured coverage, not proof against every possible timing or player action. Native screenshots are retained in each cohort.

Earlier failed/aborted cohorts remain in play-v1 through play-v6. Their causes and driver corrections are documented in review.md and the linked solution. In particular, cash-exit collision prediction must sample actual world poses on a curved offset route; progress difference alone is insufficient. Do not classify these failed attempts or directed fixtures as route clears.

## Verification and restoration

- Runtime and Editor dotnet builds pass; the pre-existing SplineSpeed.m_LastIndex CS0649 warning remains.
- 63 focused EditMode tests pass: final-tests/summary.json. Fourteen directed runtime assertions pass: contracts.json.
- Workbook audit preserves 18 unrelated ZIP parts and 296 other-chapter rows. The protected runtime archive was regenerated and all 15 workbook tests pass.
- RestStop and all three protected Noryangjin scenes match the before hashes. They were already dirty relative to Git before this task; no rollback of those user changes was performed.
- Final play-v6/restored.json confirms byte-identical registry preferences, original RestStop playModeStartScene, timeScale 1 and a clean scene. All earlier completed QA/probe restoration receipts are retained.
- Final installer Verify confirms the saved HighWay scene is clean, with nextScene=RestStop. During repeated QA only, nextScene was temporarily empty to retain the final run evidence; the authored destination remains RestStop.
- Re-running Apply leaves the scene SHA-256 unchanged (installer-idempotence.json). Browser DOM, image loading and scene-button switching passed; webpage screenshot capture timed out and native browser recovery stopped because the tool could not verify the current browser URL. Native game captures themselves were reviewed and remain in the report.

Reproducible commands:

```powershell
unity command --project-path . run_script --file tools/install-highway-chapter2.cs --entry InstallHighwayChapter2.Verify
dotnet build Assembly-CSharp.csproj -nologo
dotnet build Assembly-CSharp-Editor.csproj -nologo
powershell -ExecutionPolicy Bypass -File tools/run-highway-chapter2-checks.ps1
powershell -ExecutionPolicy Bypass -File tools/validate-agent-harness.ps1
py -3.11 tools/verify-highway-chapter2-workbook.py
py -3.11 tools/build-highway-chapter2-report.py
```

Backups and preferences stay under before/ and the QA directories; the served comparison page contains only selected images and non-sensitive route results.

## Limits

Four automated route clears and first-gate acceptance do not certify human difficulty, a first clear after 20 earned-purchase attempts, every random combination, or phone FPS/memory. No physical-device build or full earned-currency campaign was run for this overhaul. The exit scene binding is verified; these isolated QA runs did not enter RestStop.
