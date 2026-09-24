# RestStop motion and gameplay readability refinement

Preserve the existing ParkingMarshal, CoffeeVendor and SnackChef bodies, textures,18-bone hierarchy and role identities. This is a motion/attachment correction, not a new TRELLIS generation. Existing Blender5.2.1LTS inspection: all three FBX/GLB hard gates pass, about18.7–19.2k triangles each; the CoffeeVendor GLB has one tiny degenerate-face warning. Original assets stay untouched until new exports pass review.

## Baseline findings

- Native scene bodies are2.26m high, against3.1m for adjacent rebuilt highway roles: fail for consistent readability.
- All three have six real clips and one Animator: pass for rig presence; this alone does not certify visible action quality.
- Coffee attack hand moves from Y0.17 toY0.57 behind the body while the face points toward−Y. Its forward-most pose occurs at0.13s, but workbook release is0.65/0.9s: fail for forward throw semantics/timing. Before geometry and motion are preserved under the prior approved-road-concepts outputs and a new frozen attack candidate.
- The current Coffee projectile is Arrow2 with a separate root ThrowPoint: fail for role/tool and hand-release readability.
- No additive recoil marker; death ends before the1-second clip finishes: fail for impact/death continuity.

## Repair and invariants

Use reproducible Blender Python to replace action curves, preserving geometry, material names, UVs, bone names and facing direction. Give idle/walk/run, anticipated forward attack, short hit and grounded1.05sdeath. Make the coffee hand's strike peak0.6s in a1.2sclip and use the same release in Excel. Attach a real existing project cup, stop paddle and spatula in Unity to the actual hand-bound mesh centroid. No replacement of helper formations or scenery layouts.

Normalize rendered body height to about3.05m in Unity, preserve capsule radii and actor route positions, and fit vertical collision/labels to the visible body. Use existing toon materials for new tools. Release after the final animated pose; coffee must not pass through the torso or launch before anticipation.

## Stages and evidence

1. Contract/baseline freeze and body inspection.
2–6. Approved geometry, topology, UV/materials retained; refine motion, scale and tool connections only. Review silhouettes, apron/arm deformation and foot contact at critical poses.
7. Export separate .blend/.fbx/.glb; fresh-import checks and identical six-view/critical-frame comparisons. Then native prefab/gameplay captures, projectile-position checks and a real chapter3run.

Accept only if forward motion, hand/tool contact, collision identity and native readability improve with no previous technical regression. Save before/after evidence and iteration_review.json. No user approval gate is requested; perform the comparison locally.

Legacy procedural Equipment meshes stay hidden in FBX to preserve imported node IDs, and are disabled in Unity when replaced with fitted native tools. The standalone GLB contains the animated body only, matching the highway deliverable convention; native tools are verified separately in the game.
