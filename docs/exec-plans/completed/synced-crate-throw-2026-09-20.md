# Synchronized, stable crate throw

Status: complete. Source prefab and six SR18 actors updated through native Unity APIs.

The user rejected the two-hand throw as jittering and firing the crate before the visible throw. Preserve the completed seagull and other combat changes.

Confirmed problems:
- The imported goalie clip supplied changing twists while a LateUpdate solver independently pinned the hands.
- The crate moved forward only .018 model units, so its separate .62-second projectile timer preceded any convincing forward push.
- The skin still used deprecated `FlatKit/Stylized Surface With Outline`. A native same-pose material comparison reproduced black internal triangles with that shader and removed them with the current surface shader.

Implementation:
- Capture a quiet reference pose in the prefab. A single live-pose owner restores it and drives the dip, forward stroke, hand grips and follow-through; the Animator is reserved for death.
- Release is requested after the completed LateUpdate pose, with one consumed launch flag. Clock expiry alone is insufficient. Generic Guard throws retain their timer path.
- Move the held crate .16 model units forward through the stroke, preserve its orientation and emit from that exact final position.
- Use a scoped skin-material derivative preserving the source texture/color and omitting the broken hull outline.

Verification passed: 52 focused Edit Mode tests, both C# project builds and the agent-harness validator. Three complete native throws had no early launch, handoff displacement, orientation jump or sampled body penetration. Maximum palm error was 0.0000178m; resting crate/elbow jitter was zero. Pause froze the clock and death restored the Animator. Evidence and reproduction: `map-concepts/synced-crate-throw-2026-09-20/README.md`. Baselines: `map-concepts/synced-crate-throw-2026-09-20/before/`.
