---
title: Validate imported enemy animation states before chapter encounters use them
date: 2026-10-02
module: Chapters45
problem_type: runtime_presentation
tags: [unity, animation, generic-rig, generated-assets, regression-testing]
---

The first Jamsil authoring pass used C08_flagger as an enemy visual. Its imported Generic controller had idle/walk/run/signal/scared, while EnemyEventController requested attack_loop, attack_once and die. Gameplay could clear without a counted error because the ordinary QA harness previously ignored warning-level Animator messages. Successful combat and scene traversal therefore did not prove the imported character's animation contract.

The source dependency audit identified 15 affected Jamsil actors. A native Play Mode inspection confirmed missing attack/death states on initialized active animators. Inactive Animator.HasState results alone are not valid evidence of missing controller states: a controller that has not initialized can report false for every state.

The bounded repair replaced only those actors' visual prefab children with the existing C07_riot_police model and its matching Generic avatar/controller. No shared prefab, clip, collider, root transform, encounter reference, health or reward was changed. The repair measures the replacement's height/ground base and refuses to save if the serialized gameplay/collider fingerprint changes. Copying another Generic character's clips or relabeling a looping scared animation as die was rejected. The city builder now selects C07 so regeneration retains the contract.

Verification has two distinct layers:

1. The saved-scene integration test visits every encounter actor, including inactive branches/floors, and checks its actual layer-zero state names and assigned nonempty motions against ForwardEnemyAnimationContract. This catches a missing imported action without requiring the animator to be active.
2. A staged native fixture uses the saved C01/C07 pair, real EnemyEventController activation and real EnemyScript_space.EnemyDeath. It observes attack_loop/die, measures BakeMesh deformation, checks native deactivation and records warnings. No direct Animator.Play or runtime controller substitution is used. This is presentation/lifecycle evidence, not an ordinary gameplay clear.

The fixture passed 21/21; its preferences restored with zero mismatches. The focused suite passed 26/26 after an explicit AssetDatabase refresh imported the two new cases. An earlier 24-case run had used the old imported test assembly, so its result must not be claimed to include the new assertions.

Evidence:

- `outputs/chapters45-2026-10-02/final-verification-20261001T210533Z/flagger-native-before.json`
- `outputs/chapters45-2026-10-02/qa/flagger-repair-20261001T211830870/receipt.json`
- `outputs/chapters45-2026-10-02/play/20261001-212000-195-Jamsil-0/native-actor-animation/summary.json`
- `outputs/chapters45-2026-10-02/qa/chapter45-final-actor-26pass.json`

The fixture's first PNG already contains updated death labels despite its attack filename. Use sampled animator/mesh telemetry for attack timing; screenshot naming does not prove an exact animation phase. Final ordinary route, shared-regression and rebuilt-package receipts are maintained in the chapter quality report.
