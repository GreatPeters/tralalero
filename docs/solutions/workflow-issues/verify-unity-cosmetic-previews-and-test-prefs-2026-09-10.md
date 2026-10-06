---
title: Verify Unity cosmetic previews and audit native-test recovery
date: 2026-09-10
last_updated: 2026-10-02
category: workflow-issues
module: Unity cosmetic previews and native QA state
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - "Adding part cosmetics to an imported skinned model"
  - "Testing purchases through ephemeral Unity Pipeline scripts"
  - "Preparing native gameplay QA with retained editor state and existing recovery files"
  - "Running full Unity suites with package-provided editor hooks"
  - "Auditing restoration across project files, PlayerPrefs and EditorPrefs"
tags: [unity, cosmetics, preview, playerprefs, pipeline, tmp-font, editorprefs, package-hooks]
---

# Verify Unity cosmetic previews and audit native-test recovery

## Context

The original shark uses one skinned mesh and material for body and shoes. Whole-shark variants have incompatible geometry/UVs. A usable part shop required a preserved rig, independent visual materials, a head attachment and live purchase persistence. First previews exposed scale, shader, layout and font assumptions.

## Guidance

- Copy the original mesh, retaining weights, bind poses, UVs, normals and blend shapes; partition its triangles into two submeshes. Do not replace it with another skin's texture just because the filename looks related. Keep source FBX and colliders untouched.
- Inspect `BakeMesh` coordinates before applying transforms. This imported model's baked vertices already included the import scale; `TransformPoint` applied that scale again. Calibrate the hat against head-weighted baked vertices, then convert the chosen world socket back to head-local coordinates. Confirm both still and animated poses.
- Seat the crown opening, not the lowest outer-brim bounding-box point. The latter raised bucket hats above the sloping forehead in both static and live views. `SharkHeadwearFitter` now projects onto the canonical forehead, uses a small seating inset and follows its slope, with per-item fit adjustments. Its sampled forehead vertices are fully head-weighted. Do not reapply the superseded `refine-harbor-hat-fit.cs`/`seat-harbor-hats.cs` trial offsets; the importer and current hat builder share the canonical fitter.
- A hollow generated pirate crown also needed a separate dark lining to prevent the scalp showing through the hat top. Keep that addition in a wrapper prefab so source geometry stays recoverable, and verify cosmetics still contain no colliders.
- FlatKit's main color is `_BaseColor`. `_Color` was absent. Copying the shark material onto a hat also copied its emission texture; use a fresh URP/Lit accessory material. Verify tint/emission together.
- Put a square AspectRatioFitter inside a bounded preview-area parent. On the area itself, FitInParent expanded to the full screen and obscured cards. Explicitly choose a Korean TMP font; the first available font was Latin-only. Use ScreenCapture for overlay UI; a camera-only Pipeline screenshot omitted it.
- Dispose hidden preview stages, RenderTextures and materials explicitly in Editor generation tools. Delayed `Destroy` means multiple equips in one frame can leave several mounts: hide all prior mounts before creating the next one.
- Snapshot purchase prefs before testing. `JsonUtility.ToJson` returned `{}` for a class emitted by `run_script`, despite a populated list. A successful tool return was insufficient. Use a verified primitive TSV/serializer round trip and reject an empty restore. The known initial values were recovered in this session; later tests restored all 44 captured keys through the corrected helper.
- After a recompile or scene reload, verify readiness/a completed frame before chaining dependent actions. A returned reload call does not certify Start/OnEnable completion.

## Why this matters

9999 damage masked an unrelated late-ramp encounter defect: a single shot could remove E23 immediately after landing, while ordinary shots needed more flat-road time. Repeated normal-stat runs died at the same coordinate. Moving that enemy from 3.42 to 21.75 units after the descent fixed the approach budget without reducing its health. Always keep boosted traversal and ordinary-stat balance evidence separate.

Static thumbnails alone cannot certify navigation, localized labels, currency debits, ownership, animated attachment or recovery of the user's test state. The live loop bought three independent parts, rejected a repeat debit, reloaded the scene, and completed SR18 wearing them.

## Verification and related records

`CosmeticInventoryTests` and `CosmeticShopIntegrationTests` pass 8 tests. The source workbook is validated separately from the exported candidate, and Data.bytes matches it. See [release record](../../../map-concepts/noryangjin-release-2026-09-10/README.md) and [scene-test isolation](protect-active-unity-scenes-from-broad-editmode-test-runs-2026-07-18.md).

The2026-09-15correction adds3native headwear-fit tests,16body-skin/footwear combinations, seven actual Walk-pose captures and selected-BGM loop/hash checks. See [Haunted Festival and corrected headwear](../../../map-concepts/haunted-festival-wearables-2026-09-15/README.md).


## 2026-10-02: typed state recovery before mutation

The chapter 4/5 QA helper repeated the serializer problem documented above. In this case `JsonUtility` preserved top-level fields but silently omitted a nested array of nine editor-state rows. Checking only that serialized JSON was nonempty would still have failed. The first preparation mutated QA flags, then Begin and Restore correctly refused the incomplete record before gameplay started. Existing guidance was relevant and should have been applied before that preparation.

Use a dedicated serializer with a verified typed round trip for every retained key, type, existence flag and value. In this project, resolve the dedicated Newtonsoft assembly explicitly because Localization exposes a conflicting copy. Compare the round trip with the actual editor state, then read back and verify the retained SessionState record **before** the first tutorial, selector, speed or QA-flag mutation. A successful tool response is not proof of a complete snapshot. Follow the read-only probe with a real prepare → Play lobby → stop → restore smoke test.

When new backups are prohibited, reuse an existing recovery file only after verifying its SHA-256, exact key/type catalogue, existence markers and current values. The corrected helper checks all 76 preference entries and records a path/hash reference; it does not create another preference TSV. Restore and compare both preferences and all nine editor keys, plus tutorial, play-start scene and time state. Preserve an original malformed record as evidence rather than rewriting it to look valid.

For the aborted preparation, the verified preference file and captured primitive fields were restored with zero mismatches. The nine editor flags were recovered from a documented earlier baseline, including the original test speed of 2. The immediate preparation-time existence of every editor key cannot be proved. The recovery receipt therefore says `baselineRecovery=true` and `immediatePreparationEditorStateFullyProven=false`; do not report this as an exact immediate-state capture. Subsequent preparations used the corrected complete state record and restored with zero mismatches.

Native visual evidence also needs an explicit phase trigger. Sampling a lift only when a route-distance or time-modulo condition happens can miss its actual midpoint. Capture against the lift's own elapsed clock at 3 seconds and near the final turn at 5.1 seconds; inspect landing separately. Geometry counts and a finished entrance frame cannot substitute for an image of the moving cabin. See [runtime presentation verification](verify-runtime-presentation-and-progression-beyond-numeric-checks-2026-09-10.md).

Current implementation: [chapter QA helper](../../../tools/chapters45-playtest.cs). Evidence: [aborted preparation recovery](../../../outputs/chapters45-2026-10-02/unity-refinement-20261002T030830Z/aborted-preparation-recovery-apply.json), [typed read-only probe](../../../outputs/chapters45-2026-10-02/unity-refinement-20261002T030830Z/editor-roundtrip-v2.json), and [live progress record](../../exec-plans/active/chapters45-progress-2026-10-02.md). The notice fixture passed 31/31 with zero errors and zero restoration mismatches; ordinary gameplay runs have separate route evidence.


## 2026-10-02: full-suite recovery must include package hooks and global preferences

A complete snapshot format is insufficient when the catalogue of writers is incomplete. The chapter 4/5 full-suite preflight inspected authored tests and their product writers, but missed assembly-level setup/cleanup in the installed performance-testing package. `TestRunBuilder.Setup` writes `PT_Run` and `PT_Settings` through PlayerPrefs and the global EditorPrefs boolean `PT_ResourcesCleanup`. Its cleanup can also remove the two performance-test Resources JSON files and their metadata. Include resolved package hooks, bootstrap code and callbacks in the audit before the earliest operation that can invoke them; an Assets-only search is insufficient.

The authorized run used the current dirty implementation on a new local branch, 208 SHA-verified recovery copies (230,496,611 bytes), and SHA-256 manifests for all 51,804 files under Assets, Packages and ProjectSettings. Only FONT Menu.asset and Box_left.prefab changed; restoring those exact two files and synchronously reimporting them produced a completely matching product manifest. A branch alone is not a backup of uncommitted files. A separate worktree also would not isolate this Windows user's global EditorPrefs or the game's shared PlayerPrefs namespace.

Record each discovered preference's store, exact key, type, prior existence and value before package hooks run. Keep game saves, runner caches and engine session bookkeeping separate. The captured raw game-preference rows included PT_Run and PT_Settings; the latter stayed unchanged. Source plus native typed/raw checks attributed the PT_Run change only to its Date field. Three live Unity session strings changed as engine bookkeeping; native analytics agreed with the current session identity. Do not rewind those engine identities merely to force a zero-difference registry report, and never import an entire registry key to recover a few typed preferences.

The pre-suite existence and value of global EditorPrefs `PT_ResourcesCleanup` were **not captured**. Its observed current value is false, but a package default, an earlier test run, PT_Run contents or the existence of a Resources folder cannot establish its original state. Preserve the observed value and disclose the gap instead of guessing or deleting the key. File hashes and passing tests cannot close an uncaptured preference baseline. A separately reviewed partial recovery must explicitly distinguish restored captured state, retained live engine keys and the unknown global baseline; it must not report exact overall restoration.

An exact clock check also caught Unity's float setter quantizing the captured fixed timestep down by one rational tick. Capture both the native getter and serialized rational count/rate where available. Do not repeatedly assign an already-matching float, relax the equality check or save TimeManager merely to hide a recovery mismatch. Bind any narrow clock correction to the recorded native pre-state and verify it independently.

Future full-suite gates must cover installed package `PrebuildSetup`/`PostBuildCleanup`, runtime initialization and global EditorPrefs as well as authored test methods. Capture the existence of `Resources/PerformanceTestRunInfo.json`, `PerformanceTestRunSettings.json` and their metadata before cleanup. Verify every retained type/existence/value by round trip and native read-back. Report file, PlayerPrefs, EditorPrefs, editor/session and clock recovery as separate domains; claim overall restoration only when all required domains have a proven baseline and comparison.

This is a workflow lesson with a remaining recovery limitation, not a claim that all tests or all state were repaired. The full suite executed 1,135 cases: 1,083 passed, 52 failed, zero skipped/inconclusive. All 52 remaining failures exactly matched the previous messages and stack traces; their historical and shared-dependency limitations remain in the independent report. See [current full-suite recovery report](../../../outputs/chapters45-2026-10-02/full-suite-local-20261002T080356Z/FULL-SUITE-RECOVERY-REPORT.md), [independent comparison](../../../outputs/chapters45-2026-10-02/full-suite-local-20261002T080356Z/chapters45-full-suite-independent-review-20261002.md), and [broad scene-test safety](protect-active-unity-scenes-from-broad-editmode-test-runs-2026-07-18.md).
