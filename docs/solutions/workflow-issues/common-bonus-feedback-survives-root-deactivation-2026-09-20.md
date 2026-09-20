---
title: Build common bonus feedback outside the collectible lifetime
date: 2026-09-20
category: workflow-issues
module: Common Bonus talisman
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - Replacing Unity pickups across authored and dropped spawn paths
  - An absorption effect must finish after the pickup disables its root
tags: [unity, bonus, vfx, prefab, sprite-import, lifecycle]
---

# Build common bonus feedback outside the collectible lifetime

## Context

The selected talisman concept needed one torso absorption effect for every Bonus. Existing WallScript applies its reward then immediately deactivates the lifetime root. Scene walls also contain rotated 25x GFX children under nonuniform parent scales. A direct child animation would stop and inherited transforms distorted the new artwork.

## Guidance

- Keep stat/choice/cooldown logic in WallScript. Spawn a separate, scene-owned visual root before deactivation; it contains no reward code. Cancel on missing/dead player, run exit, or a reset of the player's claim timestamp. Normal completion destroys it after 0.68 seconds.
- Use the same effect target and movement for all bonus types when the user requests shared feedback. Read existing display names/icons; helper summons keep their actual helper behavior.
- Normalize art world dimensions after camera-facing rotation. Local inverse scale computed before rotated/nonuniform inheritance is insufficient. Do not reparent inherited prefab children in editor migrations; create new artwork in the intended scope and retain existing hierarchy.
- Inspect native renders and resource identity. The torso glow was invisible even with valid alpha and a front-of-body position: its SpriteRenderer.sprite was null. The generated PNG inherited Multiple sprite mode with no slices. Set both TextureImporterType.Sprite and SpriteImportMode.Single, then verify Resources.Load<Sprite>. The new test failed before and passed after the fix.
- Use the simulation clock to verify simulation-timed cleanup. Waiting 0.8 realtime seconds falsely failed a 0.68 game-time animation during slow Editor frames; WaitForSeconds follows the animation clock. Do not hide real cleanup failures by merely extending arbitrary sleeps.
- Compare preserved serialized gameplay values semantically. Saving older prefabs may explicitly serialize formerly absent default fields such as isRandom=false and rarity=Normal; account for actual C# defaults while still failing on value changes. Preserve exact Git path case on Windows when using git show.

## Evidence

[Implementation, 37 tests, native trigger/drop checks and three-scene captures](../../../map-concepts/common-talisman-applied-2026-09-20/README.md). Source prefab coverage is 21 positive prefab files and 150 authored placements; 23 wall prefab files passed gameplay-field preservation including Nerf sources. The first deterministic capture harness wrongly assumed GameManager.S existed; SR18 uses CanvasScript's supported fallback start path. Use PlayerPressedStartButton for native probes.

## Related

- [Ground VFX in reusable parts](ground-vfx-concepts-in-reusable-parts-2026-09-19.md)
- [Protect active scenes during tests](protect-active-unity-scenes-from-broad-editmode-test-runs-2026-07-18.md)
- [External preview gallery limits](protect-codex-image-clicks-without-changing-thread-state-2026-09-19.md)

Captured with ce-compound mode:headless. Related workflow docs reviewed sequentially per AGENTS.md; no additional session-history or external issue search needed.
