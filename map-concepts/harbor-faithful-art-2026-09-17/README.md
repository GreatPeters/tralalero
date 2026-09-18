# Faithful artwork correction

Status: complete. Lobby/story/chapter-transition presentation saved across all three chapters; 27 targeted tests, both builds and harness validation passed. Actual playback/caption/layout checks completed. 69 fresh preference records restored exactly (191 coins); original 1080x2340 Game View restored and SR18 left in clean Edit Mode.

## Scope

User requested implementation after seeing that the earlier lobby and story screens still differed from the approved style. Preserve Previous Scene and the separated animated hand, merchant staging, current gameplay and actual movie content. Current state was snapshotted under `tmp/backups/harbor-faithful-art-2026-09-17` before changes.

## Art and typography

- Built-in image_gen produced three project-bound text-free production sprites using the exact approved local references: LobbyPlaque, StoryChrome and NavigationStrip. `sources/prompts.json` retains all exact prompts, tool mode and roles. Originals remain in the generated-images folder; copies are in `sources/` and `Assets/ShooterSurvival/UI/CoastalFaithful/`.
- LobbyPlaque integrates the anchor crest, ropes, bevels and blue wave underline in one sprite. The substitute square badge and rectangle decorations are disabled.
- StoryChrome integrates the full video screen border and controls, with genuine alpha in the movie window. Dynamic selected/unselected tabs and button plates use Unity-created subrect sprites from the same texture so the active page remains truthful.
- NavigationStrip includes the reference's illustrated icons, blue accents and selected yellow tile. It is sliced into three independent button sprites; duplicate generic icon overlays are disabled. Alpha-column segmentation determines the actual tile bounds, since cutting the full canvas into exact thirds would clip the first and third frames. Labels remain live TMP and existing click callbacks remain intact.
- Existing KERIS was visually tested in candidate 1; it retained too many irregular stroke tips. The smoother Jua font was then compared and selected for display headings/buttons/captions. Original source: `https://github.com/google/fonts/tree/main/ofl/jua`; SIL OFL text is preserved with the TTF and in StreamingAssets/ThirdPartyNotices/Jua-OFL.txt. Reference text generated in the concept does not identify a specific original font; this is a measured matching choice, not a claim of identical source typography.
- Jua uses a separate 96-point/24-padding SDF. Body/gameplay labels keep their existing font, while display labels have ink/white/prompt material variants. Caption autosizing stays within 48–60 for longer scenes.

## Runtime layout

- `HarborGameUIInstaller.Faithful` is the final authoring pass for lobby, story and chapter-transition chrome; existing callbacks and VideoPlayers are preserved.
- `CoastalStoryLayout` anchors header/footer art to width-scaled pixel regions while the transparent movie center takes the remaining height. This prevents stretching button corners on shorter phones.
- Normal tall-phone movie presentation fills the opening frame when the edge crop is at most 15%. Shorter aspect ratios keep the complete original image. A RectMask2D contains the image inside the movie opening. This is presentation-only; the original MP4 files are unchanged.

## Evidence

Candidate renders and final captures are retained under `tmp/image-previews/harbor-faithful-art-2026-09-17`. Compare at equal displayed width. Actual movie lighting/content and real game scenery remain different from the generated illustrative scenes.

- Native suites: HarborFaithfulArtwork 5/5, HarborReferenceFidelity 3/3, HarborRefinement 10/10, CoastalUIIntegration 9/9 (27 total). Final navigation artwork was included in the last run.
- `accepted/<scene>/checks.txt` verifies actual movie preparation, disabled Previous on page zero, page 2 -> 1 navigation, truthful selected-tab art and independent hand return after shop close. The images are native 1080x2340 captures.
- `short-phone/` captures 1080x1920. All four captions fit without overflow (`captions-1080x1920.txt`), while the movie retains its complete image on that shorter aspect.
- `transitions/` retains actual chapter-movie rendering and Skip/resource-release checks without chapter reward or progression changes.
- `alpha-validation.json` confirms RGBA and more than 99.97% zero alpha inside the movie hole.
- Same-width reference/native review: `tmp/image-previews/harbor-ui-all-2026-09-15/6-faithful-art-2026-09-17/index.html`.
- Final runtime/Editor builds and exact fresh preference recovery are recorded in `build-*.txt` and `restoration.txt`. Scene roots and prior merchant/bridge/gameplay changes remain preserved.
- `ce-compound mode:headless` updated the existing SDF/native-presentation lesson with the omitted video scope, generic-art substitution, integrated artwork, dynamic cropped tabs and aspect-aware chrome layout. AGENTS already exposes the learning directory; no instruction edit was needed.

## Source files

- [Exact built-in imagegen prompts](sources/prompts.json)
- [Lobby plaque](sources/LobbyPlaque.png), [story chrome](sources/StoryChrome.png), [navigation artwork](sources/NavigationStrip.png)
- Font source: Google Fonts Jua, original unmodified TTF and OFL notice under `Assets/ShooterSurvival/Fonts/CoastalJua/`.
