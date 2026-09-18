---
status: completed
date: 2026-09-10
branch: map2
---

# SR18 presentation, equipment and twenty-run progression

User requests 1–16 are the acceptance contract. Implement all units, then perform a separate integrated verification pass with real Unity play, screenshots, animation samples, fresh asset import, and repeatable progression runs. Preserve existing purchases and authored roads. No enemy-count increase is authorized; evaluate it only.

## Decisions

- Continue from map2 / 1fed5add. Unity Pipeline is reachable and SR18 opens cleanly.
- Equipment belongs to the illegal shoe workshop: cursed-shoe modifications, contraband disguises and relics. Match the existing warm parchment, brass, dark workshop, Korean font and jewel icon.
- Existing cosmetic ownership/equipment IDs survive. New purchases use jewels. Equipped items grant explicit bounded effects; previewing/owning items does not grant effects. Apply the three equipped slots once per run, with no equip/retry stacking.
- Expand to visually distinct equipment using mesh attachments/material patterns, not only flat recolors. Show the actual equipped effect and price in the UI.
- First-attempt target approximately 30 seconds; approximately twenty earned-upgrade attempts to reach the roughly 300-second route. A 30-second improvement every attempt would clear in ten attempts, so use smaller later steps and verify against earned coins, not arbitrary free upgrade grants.
- Opening uses the supplied ig_012060245ab0a594016a4d081cf2a48191b9f821a03274172d.png. Always show before gameplay; provide bottom-right skip and an explicit test-accessible skip handler. Deliver UI and an animated video variant, accurately label the video production method.
- Highway enemies must be produced through the user's existing local TRELLIS 2 automation. Inspect current enemy mesh budgets before choosing export settings. Keep raw outputs and fresh-import validation.

## Implementation units

| Unit | Requests | Scope | Acceptance |
|---|---|---|---|
| U1 equipment | 1,2 | Cosmetic split mesh, catalog, purchase/effect runtime, workshop UI, workbook | Tail stays skin-colored; jewel debit/icon; distinct items; three-slot effects and persistence verified |
| U2 player presentation | 3,4,9 | Lobby idle, bucket mount, helper scale | No walking before start; bucket covers front/head; BoomBar readable, TungTung retained unless evidence warrants |
| U3 enemies | 5,6,10,11,12 | Facing, scale, mixed pairs, held-object locomotion, firing socket/trails | Face player while visible; 25% smaller; different pair models and spacing; stable carry/walk/throw; muzzle/trail agree |
| U4 lamp | 7,8 | Collider ownership and projectile feedback | Real approach/contact applies damage; projectile visibly reacts without removing required hazard |
| U5 progression | 13 | Enemy curve, player defaults, all upgrade prices/effects, coin economy | Recorded progression near 20 attempts, no forced deaths or enemy-count increase; baseline and upgraded runs |
| U6 death/opening UI | 14,15 | Workshop-aligned defeat, opening pages and video, skip/start/retry | UI legible on target aspect, story intact, skip works for user and automation, gameplay gated |
| U7 highway assets | 16 | Existing road FBX inventory/export and 3 TRELLIS enemy FBXs | Exact paths, textures, scale/mesh counts, preview and independent reimport |
| U8 integrated validation | 1–16 | Review, tests, runtime evidence, docs, ce-compound | Per-request implemented/verified status; remaining limitations explicit |

## Execution record

Progress and evidence are recorded in `map-concepts/sr18-presentation-progression-2026-09-10/README.md` as units finish. Read this plan and that record on continuation; do not repeat broad discovery.

Completed: all16 requests have implementation and verification records. The growth cohort cleared on attempt23, with a later same-upgrade run documenting variance.202 tests ran/200 passed;2 historical source-hash assertions remain explicit. Original44 preference keys and test options were restored and verified. See the final record for motion-comic and unrigged-FBX boundaries.
