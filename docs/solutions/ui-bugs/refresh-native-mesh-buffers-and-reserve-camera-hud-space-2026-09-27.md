---
title: Refresh native mesh buffers and reserve camera HUD space during visual iteration
date: 2026-09-27
category: ui-bugs
module: Unity Highway toll and rush presentation
problem_type: ui_bug
component: tooling
symptoms:
  - "Arrow normals and paint bounds were updated in CPU data while native screenshots retained the old geometry"
  - "A negative overlay Canvas sorting order still drew speed lines across the camera-space health HUD"
root_cause: wrong_api
resolution_type: code_fix
severity: medium
tags: [unity, mesh, procedural-geometry, canvas, native-capture, highway]
---

# Refresh native mesh buffers and reserve camera HUD space during visual iteration

## Problem

The toll model and manga speed frame passed structural checks but their actual appearance remained wrong. Treating serialization and sorting properties as proof of the rendered result delayed the correction.

## Symptoms

- After rebuilding the same Mesh assets, the arrow mesh had front-facing normals and green material; the blue ribbon had the new62m bounds. Play-mode screenshots still displayed black arrow silhouettes and the old98m paint.
- A child overlay Canvas with overrideSorting and order−1 still appeared above the main health HUD, which uses a camera canvas.

## What Didn't Work

- Correcting winding and calling EditorUtility.CopySerialized, SetDirty and SaveAssets again did not refresh the previously rendered geometry in this editor session.
- Sorting order alone did not place ScreenSpaceOverlay content behind a camera-space HUD.
- Assuming run_script preserves static fields failed: each invocation compiled a fresh assembly. Persist small cross-call capture state through SessionState or pass it explicitly.

## Solution

Update existing mesh channels through native Mesh setters and upload them. Preserve the existing asset identity/GUID; keep the geometry reproducible in the native installer.

```csharp
saved.Clear();
saved.indexFormat = source.indexFormat;
saved.vertices = source.vertices;
saved.triangles = source.triangles;
saved.normals = source.normals;
saved.uv = source.uv;
saved.tangents = source.tangents;
saved.colors = source.colors;
saved.RecalculateBounds();
saved.UploadMeshData(false);
```

This helper applies to the installer's single-material generated meshes. It is not a general copier for submeshes, skinning or blendshapes. Do not silently reuse it for character meshes.

Reserve the health/settings strip geometrically by placing the rush rect below it. Fade the top ends so the exclusion does not leave a hard horizontal row of caps. Keep the ray centre open and raycastTarget false. Preserve activation/reset in the existing chapter lifecycle.

## Why This Works

The setters notify Unity's native mesh state; UploadMeshData pushes the current channels for rendering. In this session, native screenshots changed immediately from the stale version to the correct green arrows and shortened paint after this change. The HUD exclusion no longer depends on ordering between different Canvas render modes.

## Prevention

- Compare a fresh native screenshot with CPU properties after repeated mesh asset edits, not only the first asset creation.
- Combine static ornamental pieces by material, but keep independently hidden roof geometry separate. Remove source renderers after combining so runtime visibility toggles do not re-enable duplicates.
- Inspect the actual HUD render mode before relying on sorting order.
- Preserve before/after renders and original preferences; label directed visual fixtures separately from normal route play.
- Select reflection overloads by parameter types; Graphic also has a legacy mesh overload of OnPopulateMesh.

## Related Issues

- [Highway visibility ownership](../runtime-errors/keep-highway-vehicle-visibility-out-of-pedestrian-culling-2026-09-27.md)
- [World clearance and native verification](../runtime-errors/use-world-clearance-and-preserve-opened-slots-in-highway-combat-2026-09-27.md)
- Evidence and commands: outputs/highway-toll-manga-2026-09-27/README.md.
