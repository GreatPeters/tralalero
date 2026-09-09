# SR18 slope turn spots and surface following

User request: apply turn spots to slopes so the character naturally ascends.

## Contract

- Keep the existing 15 stopping horizontal corner spots, roads, shops and materials.
- Add eight non-stopping pitch transitions: enter/leave each of four ramp runs.
- Opt in only the SR18 player to road-surface height projection. Query only its authored road colliders near the current height, so upper/lower crossings do not snap between decks.
- Preserve horizontal speed and input. Blend pitch while moving; keep the existing paused-corner implementation unchanged.
- Exclude pitch-only spots from route checkpoint/analytics counts and enemy route construction.
- Preserve source Map1/Map2 and dirty live authoring state. Backup: `tmp/backups/sr18-before-slope-follow-20260905-111656.unity.tmp`.

## Verification

- [x] Runtime component, spot mode and map-tool controls implemented.
- [x] Eight slope spots authored and SR18-only follower configured.
- [x] Projection across both elevated spans tested at center and side lanes; no upper-deck capture from below.
- [x] Continuous pitch, corner behavior, reset and checkpoint exclusion tested.
- [x] Focused tests/build and live Play Mode evidence recorded.
- [x] Docs updated with actual completion boundaries.

Results: SR18 scene 9/9, slope tests 6/6, existing turn tests 48/48; Editor build succeeded with two pre-existing warnings. Actual Play Mode traversed both elevated spans with eight consumed slope spots, maximum root Y=12.17645 and end Y=0.160856. All 421 sampled intervals retained motion. The Editor returned to Edit Mode. See `map-concepts/sr18-slope-spots-2026-09-05/README.md` and `playmode-probe.json` for exact scope, measurements and images.

Not included: a stage-2 handoff, encounter design, mobile performance certification or unrelated movement cleanup.
