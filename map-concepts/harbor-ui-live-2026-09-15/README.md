# Harbor Workshop UI installed in the game

Applied to `Noryangjin_MapTool_Mode_SR18`, `HighWay` and `RestStop` after the user's explicit September15 integration request. These are scene-authored runtime screens, not the earlier disconnected presentation prefabs.

## Installed behavior

- The lobby uses the real scene/camera beneath transparent UI, wood/brass controls, the horizontal finger gesture and the player-following green health bar.
- Permanent upgrades keep all ten IDs, save keys and workbook-driven costs/limits. Full-width part rows show their specific effects and numeric current/max levels. The last row is reachable by scrolling. The shop header now includes the current coin balance.
- The chapter tab has one permanent purchase per unlocked chapter. Values are editable in `Assets/ShooterSurvival/Resources/Upgrades/ChapterWorkshop.asset`. Initial implementation choices follow the concept: chapter1 costs500coins with attack+100%/health+200%; chapter2 costs2000 with+200%/+400%; chapter3 costs5000 with+300%/+600%. Purchased percentages accumulate, then multiply the total after regular/equipment upgrades. Owning all three yields attack×7 and health×13, not a compounded sequence of multiplications. The workbook was not edited.
- The equipment shop uses the existing real 3D preview, independent skin/shoe/hat slots, explicit effect text and purchase/equip/equipped states. Existing equipment transactions and ownership keys are preserved. Twenty-four actual render thumbnails were alpha-cropped into new files; old originals remain. Four additional workshop icons replace mismatched legacy illustrations.
- Equipment effects now enter lobby totals at startup and after out-of-run purchases/equips. Previously they only entered when a run began, making the new summary inaccurate after a reload.
- A-style opening video is connected to the existing player, captions, Next, replay, skip and completion. Four discrete indicators follow the actual four timed scenes inside the existing movie. Chapter-entry movies use the same presentation with one indicator and retain natural completion/skip transitions. No movies were replaced.
- Related settings, pause, result and gameplay HUD surfaces use readable Harbor colors. The horizontal-start path explicitly reveals its pause button; it previously bypassed the legacy Start-button visibility events.
- The legacy `Noryangjin_MapTool_Mode` scene remains in the project but is disabled in Build Settings. The first enabled build scene is now SR18, followed by HighWay and RestStop, matching the existing mobile build recipe. Runtime scene loading uses names.

## Verification

- Both `dotnet build Assembly-CSharp.csproj -nologo -v:quiet` and `dotnet build Assembly-CSharp-Editor.csproj -nologo -v:quiet` pass. A pre-existing SplineSpeed unused-field warning appears on a fresh runtime compile.
- 32 focused native assertions/tests passed:7 ChapterWorkshopTests,5 CosmeticInventoryTests,12 OpeningMovieTimingTests and8 LateralUpgradeTests. One early attempt entered Play Mode before test-runner scene restoration finished; it was stopped, the original scene reopened, and the cosmetic suite rerun successfully. Do not enter Play merely because test results have arrived; confirm the original scene has returned.
- Actual menu buttons, back/close, ten-row scroll reachability, chapter tabs, three equipment categories and clear start-gesture raycasts were exercised in all three scenes. Logs are `cohort-<scene>.txt`.
- Controlled purchases verified a regular level1→2 upgrade, chapter1's500-coin one-shot debit and16→32 attack /60→180 health, locked and insufficient chapter rejection, and a200-gem cosmetic purchase followed by free re-equipping. After the lobby-equipment fix, reload produced attack34.56 with the owned16% shoes and chapter1 multiplier.
- Noryangjin's real entry movie completed naturally into HighWay, retaining purchases. Chapter2 charged2000once and produced attack69.12 /health420. HighWay's transition Skip button entered RestStop; chapter3 correctly rejected purchase with532coins. See `runtime-integration-results.json` and transition logs/captures.
- Video checks exercised all four Next states, Replay, a natural8-second boundary, Skip and natural full completion. Each accepted screenshot was checked for image content, not just active objects. See `video-verification.tsv`, `video-complete.txt` and `video-verification/` previews.
- The two health displays agreed at315/420 and75% fill during a directed HUD damage check. Final saved HUD colors were validated after scene reload. Pause visibility and the actual Pause button were checked after horizontal-start entry.
- All66 baseline preference entries and3 new chapter ownership keys were restored and compared; there were zero mismatches. Original coin3047, jewel0 and existing equipment/upgrades are preserved. The editor is returned to clean SR18 Edit Mode.

## Inspect the actual screens

`tmp/image-previews/harbor-ui-live-2026-09-15/final/Noryangjin_MapTool_Mode_SR18/` contains final captures with the original user's balance and ownership. `cohort/HighWay/` and `cohort/RestStop/` show the other chapters under controlled test state. `video-verification/` and `transitions/` contain actual movie playback captures. `first-pass/` and `evidence/` retain rejected/intermediate results.

This pass verifies the native Unity Editor. It does not claim a newly built/installed APK, phone performance measurements or a full campaign-balance playthrough. The new chapter prices/effects are initial authored values, not an independently validated full progression curve.

## Authoring and recovery

### Lobby brightness follow-up

- User request U86 adds a subtle black dim over the real lobby scene. Authoring uses alpha `0.20`; this numeric choice is adjustable, not a user-specified requirement.
- `HarborGameUIInstaller.ApplyLobbyDim` creates `UI/LobbyDim` as the first child of the existing full-screen lobby root. Controls render above it; `raycastTarget` is false. The existing start flow hides the entire lobby root, including the dim, without a new runtime behaviour.
- `tools/apply-harbor-lobby-dim.cs` applied and reloaded all three saved scenes; fresh backups are under `tmp/backups/harbor-lobby-dim-2026-09-15/20260914-184727/`.
- Editor build passed with zero warnings/errors. `tools/verify-harbor-lobby-dim.cs` verified lobby visibility, draw order, input transparency, start-time hiding and pause availability in native Play Mode. Before/after and gameplay captures plus the report are in `tmp/image-previews/harbor-lobby-dim-2026-09-15/`. The 66-entry fresh preference snapshot was restored, and SR18 is clean in Edit Mode.

- `HarborGameUIInstaller.ApplyAll()` rebuilds the connected screens in clean Edit Mode through official Unity Pipeline. It preserves the first scene backups and existing chapter catalog values. Its three partial files separate common/lobby, upgrades and shop/video authoring.
- `tools/prepare-harbor-live-icons.py` reproduces thumbnail framing and four icon cuts from retained sources. The extra icons were generated with the built-in image tool; `extra-icons-prompt.md` retains the prompt.
- `tools/capture-harbor-live-ui.cs`, `tools/verify-harbor-video-live.cs` and `tools/verify-harbor-chapter-transition.cs` are directed native checks. They do not constitute a full natural combat campaign.
- `tools/restore-harbor-ui-test-state.cs` restores and verifies the exact session baselines. Backups are under `tmp/backups/harbor-ui-live-2026-09-15/`; source snapshots include the original theme, visual catalog and build settings.

## Corrections found during integration

- Text-only buttons cannot receive a second Image Graphic. Their existing link targets are retained and styled as text.
- The standalone A prefab's serialized root scale was zero. Removing its Canvas and nesting the content preserved that zero, so playback and indicator logic passed while the screen was blank. The installer explicitly normalizes scale/rotation/position; accepted capture checks reject blank frames.
- Inactive modal parents need `GetComponentInParent<Image>(true)` for contrast selection. Modified existing UI components must be marked dirty, with prefab modifications recorded where relevant; final colors are checked after save/reload.
- Explicit ASCII separators avoid missing middle-dot glyphs. Tall parchment panels tile their centers; button borders are reduced for short controls; shop cards and footer no longer overlap.
