# Verification and resume point

## Follow-up completed, 2026-09-07

The modal was canceled without discarding edits. Inspection showed the 24 differing RectTransform fields belonged to six GridLayoutGroups: live layout values were correct and Unity deliberately serialized driven fields as zero. That was not UI corruption. Their canonical serialization was saved only after checking no unrelated live changes, then the authored scene was closed into an isolated empty test scene.

Bucket physics was a separate real placement bug: the single Bucket prefab root was authored inactive. Nine scene roots were activated without scaling the models. All 24 actual PlayMode hazard colliders now register, and the latest SR18 11/11 and EnemyEventController 24/24 tests pass. The actual five ambush projectiles, pause/resume, 25 activation links, ten moving actors and resets also pass; see `runtime-probe.json`.

The new workbook-backed settings and health fix are documented in [encounter workbook](../../docs/noryangjin-encounter-workbook.md). The historical pending sections below are retained as investigation history, not the current handoff state. Full-stage pacing/balance is still not certified.

## Saved state

- Branch `map2`, HEAD `070a597ab23676be1972fbabd55595b40d3c4300`; no commit, push or branch change.
- Final saved SR18 SHA-256 after the nine authored bucket Rigidbody settings: `9402EB916C7B733B203C2047035EDBC9B6D038079653EC2205EC4E4584981409`.
- `placement-report.json.afterHash` describes the initial placement save, before that narrow physics correction. Placement coordinates did not change in the correction.
- Original saved Canvas GameObject `702235921` remains active, byte-identical to its pre-migration GameObject block.
- Map1/Map2 retain their reviewed SHA-256 values `D3CFFE380E022ED0D45F9A251581861B2066960D48D54F5897181038F81C1315` / `F7C8F2E0B39F2E515C5A237ED8E18E14CA60ECA5170914F10C01EC128A8C61E1`.

## Completed checks

- Installer verified every protected road/scenery GameObject and component serialized snapshot unchanged, 230 roads and 666 original Props, latest totals and sibling hashes.
- `NoryangjinSr18SceneTests`: initial 11/11 passed after the migration, including deck support, unchanged roadside market, corner/slope spots, normal enemy/wall scales and exact nudged wall X values.
- `EnemyEventControllerTests`: 22/24 passed, including all three new ambush tests (hidden flags, invalid setup, reveal→movement→shot). Two existing reset tests relied on MonoBehaviour OnEnable/OnDisable running in EditMode preview scenes. Their fixture now invokes those boundaries explicitly; rerun is pending.
- Expanded SR18 clearance assertions initially returned 9/11. The two failures share the new helper: Rigidbody-owned bucket shapes return zero Editor bounds / unchanged ClosestPoint despite valid authored box dimensions. The revised helper computes oriented-box clearance from serialized shape/transform. Setting kinematic alone did NOT change this Editor query result, so it is not claimed as the query fix. Real registered PlayMode colliders still require the runtime probe.
- `dotnet build Assembly-CSharp.csproj -nologo -v:q`: passed, 0 errors (existing warnings).
- `dotnet build Assembly-CSharp-Editor.csproj -nologo -v:q`: passed, 0 errors, 0 warnings.
- `powershell -ExecutionPolicy Bypass -File tools/validate-agent-harness.ps1`: passed.
- Changed tracked C#/Markdown `git diff --check`: passed (line-ending notices only). Unity-generated YAML whitespace was not hand-edited.

## Current blocker

The final rerun reached Unity's **Scene(s) Have Been Modified** modal. Pipeline discovery remained reachable while scene/eval calls timed out. The UI accessibility tree confirmed Save / Don't Save / Cancel. The CLI session was interrupted; neither Save nor Don't Save was selected. User was asked to choose Cancel. Pending live scene edits have not been silently saved or discarded.

The runtime scripts have not completed. Do not claim actual projectile release, pause/reset or a full-stage balance run based on their existence.

## Resume safely

1. Cancel the modal, check `unity pipeline list` and `list_open_scenes`. Verify the editor is in Edit Mode and no test is still running.
2. If SR18 is dirty, preserve it with `EditorSceneManager.SaveScene(scene, uniqueBackupPath, true)`, inspect the exact difference from the saved scene and preserve user edits. Do not clear dirtiness or reopen the scene to discard arbitrary changes.
3. Once the source is safely clean, run the two fixtures separately (the Pipeline filter does not interpret `A|B` as two fixture names). Require nonzero test totals and inspect Summary, not the outer success flag.
4. Open the exact saved SR18 scene, enter Play Mode without starting the normal run, then evaluate `tools/probe-sr18-latest-start.cs`, `tools/probe-sr18-latest-resume.cs`, and, after at least one second of active time, `tools/probe-sr18-latest-finish.cs`.
5. The probe temporarily disables player movement, weapons and contact, checks all activation links and the 24 actual hazard colliders, lets five real ambush projectiles launch, then checks pause/reset. It does not earn coins or save scene changes. Exit Play Mode afterward; do not leave probe changes running.
6. Confirm saved hashes and a clean active SR18 scene after cleanup. Full real-player route traversal, pacing, aim/dodge feel and five-minute difficulty remain a separate playtest.

## Review

Task-local source and prefab/scene interactions were reviewed sequentially in the main thread per AGENTS.md: correctness, tests, maintainability, project standards, Inspector/agent parity, known scene-safety patterns, enum compatibility, runtime lifecycle and mutation rollback. Unrelated dirty work and generated YAML were excluded from code review. New untracked installer/probe files were inspected directly as implementation verification, not represented as a staged PR review. No independent subagent review was claimed.

Known residual: runtime verification and final fixture rerun are blocked by the editor modal. The layout is saved; runtime sign-off is not complete.
