---
title: Rebuild enemy prefabs without stale scene rigs or launch bindings
date: 2026-09-23
category: integration-issues
module: Highway enemy model reconstruction
problem_type: integration_issue
component: development_workflow
severity: high
symptoms:
  - "Updating source prefabs left two or three visual rigs on saved scene enemies."
  - "Ranged scene overrides lost heldProjectile and stopped workbook initialization."
  - "A narrowed passage still allowed travel through a gap opened by sideways patrols."
  - "Geodesic arm weights pulled a wide character's vest away from the torso."
root_cause: missing_validation
resolution_type: code_fix
tags: [unity, trellis, blender, animation, prefab-overrides, skin-weights, collision, native-validation]
---

# Rebuild enemy prefabs without stale scene rigs or launch bindings

## Problem and evidence

The user rejected the resized highway roster and asked for new TRELLIS reconstruction, visible animation and mandatory enemy encounters. Six source models were generated and rigged successfully, but those artifacts alone did not establish correct live scene integration.

The first importer replaced the source prefab's Body and then only the first Animator found on each scene instance. Unity also restored the new inherited source Body alongside an earlier scene-added Body. The resulting audit found 30 enemies with two Animators and 20 with three. A source-prefab-only check missed this.

The same replacement invalidated scene-specific heldProjectile references. Source prefabs still had projectiles, while their 34 ranged scene instances did not. Starting the chapter reported `HWY_E02_TrafficPatrol: 경비원/뚱보 등 투사체가 있는 모델만 사격할 수 있습니다.` and halted the workbook application. The failure was caught before completion; the aborted firing capture remains under `projectile-v3/TrafficPatrol/`.

## Corrections

1. **Audit saved scene instances, not only source assets.** Remove every previous visual Animator subtree while preserving unrelated gameplay roots. Bind the replacement animator, reaction bones, hand anchor and launch point explicitly. Require exactly one Animator per enemy after save/reopen.
2. **Determine required projectiles from the role contract.** A missing old reference is not evidence that a ranged role is melee. Always build/rebind tire, parcel and cone projectiles for their roles. Recover a missing patrol bullet from the valid authoritative source prefab. Validate all 34 ranged scene bindings and their ownership paths.
3. **Test the actual moving encounter.** A numeric coverage test with static ±1.1m actors passed, yet a real center-input attempt crossed with both enemies alive and HP500 because sideways patrols opened a gap. Keep patrol animation but move within each lane, forward/back. The repeated center test then reached physical enemy contact and HP0. Visible road funnels close the outer bypass paths; no enemy capsule enlargement was used.
4. **Separate torso and limb deformation.** Nearest/geodesic bone paths are not an anatomical torso classifier. Wide vests followed upper-arm weights. A smooth torso volume anchor, followed by eight welded-adjacency smoothing passes while retaining core/head/hand/sole anchors, removed the stretched panels and thin seam remnants. Preserve the rejected v1/v2 renders and compare the same v3 attack pose.
5. **Finish tool construction with the asset catalog.** Early cube-based shovel/weapon placeholders were rejected during visual review. Reuse the installed Poly Universal tools, normalize their axes/scale, and place a grip using the actual mesh bounds. Keep projectile collision/lifetime on a separate root.
6. **Launch after the final pose.** The throw timer queues a highway release; `HighwayEnemyAnimation.LateUpdate` performs it after animation/recoil and final gun aim. Preserve the held rotation for the thrown objects; patrol Arrow2 keeps its local-X flight orientation. Four native probes measured zero release-position error and HP500→400 for attack100.

## Prevention and verification

- `HighwayRebuildContractTests` inspects all six source rigs and the actual saved HighWay scene: 50 single-Animator actors, 34 bound ranged actors, mandatory row membership and no sideways patrol gaps. It also checks queued-shot reset and death-clip lifetime.
- Actual left, right and center contact probes, plus a normal gameplay capture, cover the gaps that static geometry assertions missed. Isolated probes explicitly disable traffic; do not present them as ordinary playthroughs.
- Use `--python-exit-code 1` with Blender. A Python exception can otherwise leave the CLI process with a successful exit status. An empty-world render first failed on `scene.world == null`; create its World explicitly.
- Render repeated poses with evaluated geometry or validated skin matrices. A single Editor call can otherwise capture stale GPU poses. Keep preview camera framing separate from gameplay camera settings.
- Image generation and TRELLIS output are not a rig. Retain references, submitted prompts, raw meshes, fitted rig scripts, fresh FBX/GLB checks, native source/scene audits and actual motion evidence.
- Source-only model files contain the animated bodies. The final Unity prefabs bind the separately sourced tools. Do not imply all equipment was reconstructed by TRELLIS.

Final evidence: [highway reconstruction report](../../../map-concepts/highway-enemy-rebuild-2026-09-23/README.md). Related: [authored prefab layers and grip pivots](verify-combat-with-authored-prefab-layers-and-pivots-2026-09-20.md), [anatomy and support validation](../workflow-issues/verify-character-anatomy-and-real-support-in-generated-assets-2026-09-13.md).

Captured with `ce-compound mode:headless`, sequentially per AGENTS.md. Local evidence was sufficient; no session-history or external issue search was used.
