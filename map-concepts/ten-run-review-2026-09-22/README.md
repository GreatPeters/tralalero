# Ten-run gameplay review — 2026-09-22

Completed ten actual Play Mode attempts: six SR18, two HighWay, two RestStop. 458 original1080×2340PNG captures, about20minutes of recorded run/result time. All ten attempts ended in death; none is reported as a clear. Only real player lateral movement and ordinary game start/skip actions were used. No health/damage override, teleport, disabled enemy/collider, forced reward or time acceleration. This review changes no game code or art.

Primary local browser gallery: <http://127.0.0.1:6753/ten-run-review/>. Original evidence: `tmp/image-previews/ten-run-review-2026-09-22/run-NN/`. Curated captions and suggestions: [findings.json](findings.json). Rebuild with `python -X utf8 tools/build-ten-run-gallery.py`.

## Priority and evidence

1. **Confirmed visual obstruction:** at about8s in repeated SR18 runs, a large gray surface hides the player and lower route. The non-hidden start gantry renderer intersects the sampled view and is a root-cause candidate; no repair has been applied.
2. **Confirmed bonus mismatch:** run02 ATT+12/+16/+17 each produced only+1 current attack. Run03 HP+14 produced500→508. `BonusAltarRules.ResolveDisplayValue` multiplies Ratio by100, while `ResolveValue` multiplies by `originalDamage=8` / `originalHealth=60`; `FormatDisplayValue` adds a percent sign only for Percent. The display and actual effect therefore disagree.
3. **Risk communication:** a late Guard contact reduced approximately715→342HP, and some later contacts were fatal. The existing remaining-health exchange rule is not declared incorrect; its size is poorly communicated before contact.
4. **Visual judgment:** stacked identical helper aircraft overlap the shark; repeated stalls/signs/paired encounters look assembled from a small set of modules; the Woman boss still has an awkward glove/handle impression and flat apron shading; highway guidance and hazard stripes compete visually.
5. **Replay communication:** result UI emphasizes coins/time/ad rewards and does not explain the final damage source or summarize build choices.
6. **Unresolved balance coverage:** the later chapters were launched directly at the same current save stats, not through natural campaign progression. RestStop attempts died at the first encounter; its later holdout and the later highway sections are not reviewed. Establish an expected chapter-entry save before judging their tuning.

The first two runs emphasized aiming/bonus collection. Runs03 onward also attempted close-range actor and projectile avoidance, and run06 prioritized percentage attack/fire-rate bonuses. Different random rolls and steering policies prevent a controlled causal comparison of survival times. Automatic steering is not a human enjoyment score or a win-rate benchmark.

## Run ledger

| Run | Chapter | Policy | Recorded seconds, including result delay | Bonuses | Outcome |
|---|---|---|---:|---:|---|
|01|SR18|Left choices|96.1|7|Death|
|02|SR18|Attack choices|141.1|11|Death|
|03|SR18|Health / earlier avoidance|282.3|23|Death|
|04|SR18|Helpers / earlier avoidance|300.2|24|Death at final Woman boss|
|05|SR18|Late reaction / alternating choices|141.0|11|Death|
|06|SR18|Percentage / fire-rate priority|143.9|11|Death|
|07|HighWay|Main route|32.5|2|Death; first fork main confirmed|
|08|HighWay|Healing bypass|43.7|1|Death; first fork bypass confirmed|
|09|RestStop|Attack choices|11.8|1|Death at first encounter|
|10|RestStop|Health preference / avoidance|11.8|1|Death at first encounter|

All starts usedHP500/ATT68; SR18 forward speed8.84, other chapters7.8. Full samples/events in each folder and aggregate `tmp/ten-run-review-2026-09-22/run-summary.json`. Capturing can add frame hitches, so these runs make no FPS claim. A few same-frame capture requests coalesced; counts use actual PNG files, not queued requests.

## Preservation and review checks

- Existing66gameplay preference records and4extra tutorial-related key-presence/value records restored. Original wallet731coins/0jewels restored. SR18 is back in clean Edit Mode; game assets were not saved from Play Mode.
- Workbook hash before/after remains `2a6d0acade97fd8d11b68949548ecc1b1c2cd58d69784900e30e4afb042f4604`; source was not edited.
- Native screenshots inspected as contact sheets and selected full-resolution frames. Gallery uses source-derived immutable names so a revised caption cannot silently point to a stale older capture.
- A preliminary planar-collision inference was rejected: a read-only native probe showed only24of41lanes blocked against already-played actors. This does not establish an unavoidable wall; no such claim appears in the findings.
- No public upload. Native footage is shown through a local browser gallery, per repository preview requirements.
