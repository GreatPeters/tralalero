# Harbor gameplay and UI correction

This implements user requirement U88 in the actual SR18, HighWay and RestStop scenes. No Android APK has been rebuilt in this pass.

## Behavior and appearance

- Restart clears the shark's death state and rebinds locomotion independently of the legacy player animator. Run state maintains Walk/Idle, including resume and stationary defense. Reload begins in the lobby rather than briefly marking an uninitialized scene as running.
- A shared Harbor settings panel serves the lobby and gameplay. Gameplay settings includes resume/retry directly. Closing returns to its originating state; settings also blocks the start gesture. Sound/vibration use labeled ON/OFF switches, and the master volume/sensitivity sliders still save actual settings.
- Resume no longer enables the old scoreParent, and the player child canvas remains hidden when the modern HUD is configured.
- Health/attack labels are larger. The health fill uses a rectangular flat sprite and reports actual fill. Cream text uses the shared `Harbor_Cream_Outline` TMP material; dark text uses `Harbor_Ink_Readable`, preserving clean contrast on parchment. No per-label cloned materials or UGUI Outline components are needed.
- The game-over panel has the same wood/brass/parchment treatment, clear run rewards, rewarded-ad action, retry and an upper-right close. Shops/upgrades/settings also expose upper-right close controls. Video retains its upper-right Skip action.
- Coin pickups use the recognizable gold wallet coin artwork instead of the earlier star token. The 55ms water-pop replaces the long noisy shot effect; voice volume is0.22 and cooldown0.12seconds to prevent a nearly continuous overlapping hiss.
- Preview drag is reversed and wallet values are centered. Seven hats were reviewed for fit. Cap/bucket/tophat/pirate/circlet placement was refined; goggles remain worn at the face. The oversized human diving helmet was replaced by a new closed brass diving cap, with a matching thumbnail,12176triangles and four shared material slots.
- Video Next ignores overlapping seek requests and holds its requested page until a decoded target frame arrives. RenderTexture is cleared and only shown after decoding, avoiding uninitialized white frames and stale page reversals. The footer is not animated/repositioned on Next.
- `StableGameplayCamera` follows player position and route yaw independently of the animated/pitched model. Height changes are gently smoothed while large resets snap; player slope pitch and roll do not tilt the view.
- Enemy body triggers across9prefabs and the three scenes use idempotent30% horizontal expansion. Radius0.33becomes0.43; the four actors with explicit0.67scene overrides retain that baseline and become0.87. Capsule height is unchanged. This updates the existing enemy body trigger, not projectile/player/obstacle radii.

## Chapter workshop

Chapter1 is unlocked by default and does not require a clear. Every unlocked chapter supports five persistent ranks. Old `chapter_workshop_owned_N` saves read as rank1 until the new `chapter_workshop_level_N` key exists. Failed payment restores the previous rank; synchronous purchase re-entry is rejected.

| Chapter | Rank1 | Rank2 | Rank3 | Rank4 | Rank5 | Bonus per rank |
|---|---:|---:|---:|---:|---:|---|
| Noryangjin | 1,500 | 3,000 | 6,000 | 12,000 | 24,000 | Attack+100%, health+200% |
| HighWay | 5,000 | 10,000 | 20,000 | 40,000 | 80,000 | Attack+200%, health+400% |
| RestStop | 12,000 | 24,000 | 48,000 | 96,000 | 192,000 | Attack+300%, health+600% |

These expensive escalating prices are implementation choices. Existing per-rank effect amounts are retained and add across owned ranks/chapters, after regular/equipment bonuses. `ChapterWorkshop.asset` owns base prices; `pricingVersion` applies the new price migration once so later authored changes survive UI rebuilding. Full campaign economy balance has not been playtested.

## Music

[Ten lively candidates and listening page](audio/README.md). All ten have MP3/WAV/OGG exports. The tempo/instrument labels describe prompts, not a measured transcription or listening verdict. This candidate-generation step does not replace the currently installed music without a candidate selection.

## Boats

Two active water-side gimmicks already existed: the paddle boat at `SR18_L_G08_T097_Oldman`, and coastal cannon ship `SR18_Polish_Ship_2` near `SR18_L_G21_T253_Hole`. A first ship also exists as scenery. Moving the cannon ship closer laterally still left it hidden by stalls in an actual game capture. It now sits in the open water beyond the pier bend at `(440.05,-0.70,125.55)`, facing the pier. Its45m activation range compensates for the forward water placement. It remains a single cannon event with hull colliders disabled; no additional dense hazard cluster was introduced.

## Verification and recovery

- Runtime and Editor `dotnet build` pass; the existing SplineSpeed unused-field warning can appear on a clean runtime compile.
- ChapterWorkshopTests10, HarborPolishTests4 and OpeningMovieTimingTests12 passed in native Unity. Initial new test failures came from invoking MonoBehaviour lifecycle methods through SendMessage in Edit Mode and missing configured camera smoothing state; the test invocation and camera Configure initialization were corrected. A review then found four0.67scene overrides that inherited a prefab baseline; they were restored from the preserved scene and covered by an override-specific test.
- `tools/verify-harbor-polish-ui.cs` exercises actual menu entry/close, rapid Next, resume, death-animation reset, half-health fill, coin rendering, pitch-independent camera and defeat presentation. Actual reload and boat checks are recorded separately.
- Captures live under `tmp/image-previews/harbor-polish-2026-09-15/`. Initial captures exposed a missing gear sprite and plain switch blocks; the saved builder uses the correct Settings sprite and brass-framed switches. Initial hat fitting attempts are retained; the rejected human diving shell was replaced by the authored cap.
- Source/scene/prefab and player preference baselines: `tmp/backups/harbor-polish-2026-09-15/20260915-014450/`. SR18 initially had unsaved changes; both the disk version and unsaved content were preserved before proceeding. The baseline is41coins/0gems.
- Final checks passed in all three scenes. The actual Retry/LoadGame path was verified on the newly loaded Canvas with forward motion frozen to isolate the reset. The boat is visible in the open-water capture and its cannon fired. All76 baseline preference entries were restored with zero mismatches; coin41/jewel0, clean SR18 Edit Mode. See `restoration.txt`, `reload-check.txt` and the final capture folders.
- Reauthor UI with `HarborGameUIInstaller.PolishOpenScene()` or full `ApplyAll()`. Build the cap with `HarborHatBuilder.Build()`. `tools/apply-harbor-polish.cs` records scene saves, idempotent hitbox setup and the coastal ship adjustment.

## Remaining external information

The pre-existing privacy-policy and terms buttons had no document or URL. Their new buttons are styled and expose serialized URL fields on `HarborSettingsPanel`; they stay disabled while those fields are empty. The user was asked for both URLs. Existing SDK ad-privacy controls remain connected. No legal policy text or external address was invented.
