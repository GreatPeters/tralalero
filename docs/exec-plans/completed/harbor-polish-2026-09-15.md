# Harbor gameplay and presentation correction

Status: implementation and native validation complete; privacy-policy/terms URLs remain external information requested from the user.

User authorized all19 feedback items on2026-09-15. Work stays in the current project and preserves existing edits. Active SR18 had unsaved changes; both disk and unsaved scene states plus66preferences and10extra keys were captured under `tmp/backups/harbor-polish-2026-09-15/20260915-014450/`. Baseline wallet is41coins/0gems, not the previous turn's3047.

## Acceptance checklist

- [x] 1: Ten more lively game-appropriate BGM candidates and listening paths.
- [x] 2,13: Retry and settings-close keep shark locomotion and correct HUD; no legacy score UI appears.
- [x] 3,18: Identify and correct repetitive swish and unintended camera bob.
- [x] 4,5,10,14: Rectangular readable HUD, larger labels, reusable TMP material presets on relevant text.
- [x] 6: Recognizable dropped gold coin visuals.
- [x] 7: Unlock-gated chapter upgrades, chapter1 available before clear, five ranks each, expensive escalating prices, safe previous-purchase migration.
- [x] 8: Enemy hit target radii enlarged approximately30%,0.33→0.43; preserve scene-specific baselines and avoid repeated scaling.
- [x] 9: Inspect boat gimmick support and place sparingly only where the actual route/water layout supports it.
- [x] 11,12,19: Cohesive settings, privacy/terms entries, integrated in-run settings with resume/retry, upper-right close controls, revised defeat/retry screen.
- [x] 15,16: Improve hats where needed, reverse preview drag, center shop currency.
- [x] 17: Stable video footer on Next and no blank display while seeking.
- [x] Native scene save/reload and Play Mode regression checks, focused tests, screenshots, preferences restored, docs/learning recorded.

## Initial investigation

- Settings close/ResumeGame currently activates the legacy scoreParent. Player.Update forces shark Idle whenever isGameRunning is false, but ResumeGame does not explicitly resume shark locomotion. Inspect runtime state before fixing.
- Gameplay camera is parented to the animated Original model; inspect frame deltas to distinguish mesh motion from route height.
- Current chapter domain is a one-shot owned flag. Replace with bounded levels while reading old ownership as level1.
- Existing Harbor art and KERIS font are reused; no new authentication, billing, package or remote deployment is required.

## Final verification

-26 focused native tests passed:10 chapter workshop,4 hitbox/camera and12 movie timing.
-Real menu/start/resume/reset/video/coin/HUD/defeat captures passed in SR18; HighWay and RestStop settings/resume/gear checks passed. Real scene reload restored Walk.
-Seven hats reviewed and fitted; the oversized diving shell was replaced with a closed authored cap and a matching icon.
-The coastal cannon is visible beyond the pier bend and triggered once in its controlled check. Four pre-existing0.67enemy radius overrides were recovered from baseline and now save/reload as0.87.
-All76 preference entries restored with zero mismatches,41coins/0gems. Editor returned to clean SR18.
-Details and remaining legal-URL dependency: `map-concepts/harbor-polish-2026-09-15/README.md`. No APK build/install claim.
