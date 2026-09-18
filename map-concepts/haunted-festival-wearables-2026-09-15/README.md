# Haunted Festival BGM and headwear placement

User requirement U90 selects “유령들의 축제” and requests correction of hats/skins worn in strange places. The ambiguous reference to skin was queried; the demonstrated placement defect is addressed for all seven headwear items, including combinations with all eight body skins and original/replacement footwear. Body textures were inspected, not redesigned without clarification.

## BGM

The selected `07_haunted_festival.ogg` is installed at `Assets/ShooterSurvival/Resources/Audio/Mobile/music.ogg`, preserving the existing asset GUID and common GameAudioService binding. Source and installed SHA256 match:

`2AAAE5FBB3CBBC99A6C5E5D466F52B7F1CB0C364FA5E038E2235EA46225927A1`

The native AudioSource was observed playing the59.34-second loop. The source candidate remains at `map-concepts/harbor-polish-2026-09-15/audio/listen/07_haunted_festival.ogg`.

## Placement correction

Previous fitting used outer brim bounds and positive height offsets. Bucket hats visibly floated in both the preview and the actual animated character. `SharkHeadwearFitter` uses the canonical forehead surface at meshZ0.00215, seats the socket0.00009mesh units below that surface and follows the measured forehead slope. Per-item offsets/sizes now seat the crown opening; goggles rest against the forehead rather than floating upright or cutting across the nose.

The measured forehead vertices are fully weighted to the head bone. The runtime and preview both use the same head-local socket, preserving attachment as the head animates. The pirate hat also has a dark inner crown lining in a new wrapper prefab; the original generated mesh remains intact. No cosmetic colliders were added.

`WearableAssetImporter.FitHeadwear`, the import completion path and `HarborHatBuilder` now delegate to the same fitter, preventing an old calibration from overwriting the repaired positions.

## Evidence and recovery

-3 native `SharkHeadwearFitTests` passed: forehead seating,16body-skin/footwear combinations without duplicate/moved hats, and collision-free pirate lining.
- Original and revised preview captures plus actual animated model captures: `tmp/image-previews/wearable-placement-2026-09-15/`. The final live folder checks all seven hats while the real player is in Walk and checks BGM loop playback.
- Backups: `tmp/backups/haunted-festival-wearables-2026-09-15/20260915-132259/`, including the original catalog, previous installed BGM, scene and a fresh66-entry preference snapshot.
- No purchase or currency change is needed to fit or verify an item. No APK rebuild/install is claimed.
- Final verification reported no console errors; the66snapshotted preference entries were restored and compared with zero mismatches,141coins/0gems, clean SR18 Edit Mode. The approved BGM replacement remains installed.
