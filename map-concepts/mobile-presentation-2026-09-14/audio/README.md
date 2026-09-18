# Original mobile audio

Two original64-second music candidates were generated locally; two-second equal-power overlap produces62-second loops. The source recording linked by the user was not downloaded or supplied to the model.

- [A: Midnight Tide](processed/a_midnight_tide-loop.ogg): music-box/celesta and felt-piano prompt, slower mysterious toy-theatre direction.
- [B: Ghost Arcade](processed/b_ghost_arcade-loop.ogg): pizzicato/glass-bell/electric-piano prompt, more rhythmic mischievous arcade direction. Selected for `Resources/Audio/Mobile/music.ogg` to accompany Ocean Pop. Selection is based on the requested art direction and signal checks; human listening/device mix acceptance remains pending.

## Production

`tools/generate-mobile-bgm-candidates.py` uses installed Stable Audio3 Small-Music, SAME-S decoder, fp32,8steps, CFG1.5,8CPUthreads, seeds26091401/26091402. `tools/generate-mobile-sfx.py` uses Small-SFX for harbor, traffic and knife with seeds26091411–13. Requests, logs, generation times and raw WAVs remain here. No remote generation service or public share was used.

`tools/prepare-mobile-audio.py` makes the loops and26deterministic mono22.05kHz one-shots with additive/noise synthesis. The knife combines a generated transient with the synthesized pass. Original audio sources in Assets total1,681,731bytes before Unity import; the final APK contribution must be measured separately. Effects have short onset/end tapers and peaks no higher than0.72. Music masters peak at0.68. Adjacent-sample seam deltas are recorded in `processing-report.json`; they are not perceptual listening scores.

Use the cached Codex Python (which contains numpy) for processing. The default system Python has no numpy. The installed FFmpeg path is in the processing script; no new software was downloaded.

## Runtime coverage

| Area | Cue and entry point |
| --- | --- |
| Buttons / tabs / menus | GameUIButtonSound; CosmeticShopUI open/close/tab |
| Purchase / equip / unavailable | UpgradeUI and CosmeticShopUI |
| Water shot / helper bomb | WeaponScript after a successful projectile rental |
| Enemy hit / defeat | EnemyScript_space damage and one-shot EnemyDeath |
| Knife swing / thrown prop | EnemyEventController animation phase; projectile release |
| Player hit / death / healing | PlayerScript actual health change / health bonus |
| Coin collection | CoinPickup after the one-shot wallet grant |
| Bonus / nerf | WallScript after applying the selected effect |
| Barrel / roadblock | Barrel scripts and HighwayHazard |
| Vehicle warning | HighwayOncomingTraffic / HighwayHazard activation |
| Pause / resume / chapter | CanvasScript / ChapterProgression |
| Victory / defeat / holdout | CanvasScript / RestStopHoldout |
| Harbor / highway environment | Scene-scoped ambience source |

GameAudioService has12reusable effect voices, event cooldowns, priority replacement, one persistent music source and one ambience source. Existing SettingsManager owns master mute/volume. Backgrounding pauses loops and stops effects; returning resumes or starts a loop that had not begun. Cosmetic pitch variation uses a private counter, not UnityEngine.Random, so it cannot consume gameplay RNG.

## Provenance and terms

The installed [Stable Audio3 repository](https://github.com/Stability-AI/stable-audio-3) provides the Small-Music and Small-SFX models used here. Its software LICENSE is MIT, copyright2026Stability AI; model usage is under the [Stability AI Community License](https://stability.ai/license), as stated in the installed README. The model/code licenses are distinct; these generated outputs are not labeled CC0. Model weights and inference code are not bundled in the game.

The UI uses the existing KERISKEDU_B font, copyright2025KERIS. The [official font distribution page](https://copyright.keris.or.kr/wft/fntDwnldView?fntGrpId=GFT202512150000000000002) permits the intended commercial UI/embedding use. It is not labeled CC0. Existing project icons retain their original project provenance.
