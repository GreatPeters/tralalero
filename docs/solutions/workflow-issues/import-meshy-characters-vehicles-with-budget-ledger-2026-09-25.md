---
title: "Generate and import Meshy characters/vehicles on a credit budget"
date: 2026-09-25
category: docs/solutions/workflow-issues
module: Meshy asset pipeline
problem_type: workflow_issue
component: tooling
severity: medium
applies_when:
  - "Generating characters, vehicles or props with the Meshy API for a chapter"
  - "Importing Meshy rigged FBX files and per-action animation FBX files into Unity"
  - "Running several Meshy workers in parallel against one credit cap"
tags: [meshy, credits, rigging, animation, fbx, outline, remesh]
---

# Generate and import Meshy characters/vehicles on a credit budget

## Workflow that worked

1. Render in-game references (Noryangjin enemies, Noryangjin Meshy props) and pass them to `image-to-image` with `generate_multi_view` (9 credits). Review the three views before paying 30 credits for 3D.
2. `multi-image-to-3d` with `ai_model: meshy-7.1`; the first image must be the front view (reorder views per asset). People: `pose_mode: a-pose`, target 4,000. Held props (poles, flags, shields, batons) usually disappear in A-pose; attach them in Unity.
3. Rig (5) and request only the actions the prefab contract needs (3 each); walking/running come with the rig.
4. Vehicles: remesh to ~2,500 triangles (5 credits) when they are placed by the hundred; the look holds.

Tools: `tools/meshy_client.py`, `meshy_reststop_concepts.py`, `meshy_character_pipeline.py`, `meshy_remesh.py`, `meshy_fetch_missing.py`, `meshy_ledger_repair.py`, `import-meshy-characters.cs`, `import-meshy-objects.cs`.

## Pitfalls

- **Parallel ledger.** Workers must merge under a lock and override only their own rows; otherwise one worker resurrects stale statuses and a restart pays again. Restart only pipelines that resume `PENDING` tasks; after stopping a worker run the repair and fetch-missing scripts.
- **Bone scale keys.** Each Meshy action FBX has its own bone scale keys, so idle stood ~17% taller than walk. Copy clips to `.anim` without `m_LocalScale` curves.
- **Measuring poses.** `AnimationClip.SampleAnimation` applies root curves that runtime root locks ignore, and Animators in a preview scene do not evaluate; judge size visually beside a reference enemy.
- **Facing.** Vehicles came out nose toward -Z after aligning length to Z; characters face +Z (Noryangjin prefabs face -Z).
- **Remesh textures.** A remesh has new UVs and embeds base color, normal, metallic and roughness maps. Delete the old extracted texture and select the base color by name, not by file size.
- **Outline.** Copy the Noryangjin enemy material (`FlatKit/Stylized Surface With Outline`) and swap only `_BaseMap` so every Meshy asset matches.
- **No free retries** are documented for the API (2026-09-25); spend retries on concepts, not models.
