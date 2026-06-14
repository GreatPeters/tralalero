---
title: Handle Inline Unity Material Keyword Lists
date: 2026-05-25
category: docs/solutions/workflow-issues
module: Unity asset workflow
problem_type: workflow_issue
component: tooling
severity: low
applies_when:
  - "Bulk-editing Unity .mat YAML files"
  - "Changing shader keywords across generated MeshyAI materials"
tags: [unity, material-yaml, shader-keywords, meshyai, flatkit]
---

# Handle Inline Unity Material Keyword Lists

## Context
Unity material YAML does not always serialize keyword fields as multiline lists. A field can appear as `m_InvalidKeywords: []` instead of:

```yaml
m_InvalidKeywords:
- SOME_KEYWORD
```

A bulk MeshyAI material conversion initially assumed every keyword field was a multiline block. The script stopped before writing changes because it could not find the exact `m_InvalidKeywords:\n` marker.

## Guidance
When rewriting Unity material keyword fields, match from the field line to the next known field instead of matching only the multiline form.

Use a regex shape like:

```python
re.sub(
    r"^  m_InvalidKeywords:.*?(?=^  m_LightmapFlags:)",
    "  m_InvalidKeywords:\n  - _UNITYSHADOWMODE_NONE\n",
    text,
    count=1,
    flags=re.M | re.S,
)
```

Apply the same pattern to `m_ValidKeywords` by replacing through the next stable field, usually `m_InvalidKeywords`.

## Why This Matters
Generated assets and Unity-reserialized assets can mix inline and multiline YAML forms. Exact substring replacement works only for one serialization shape and can break an otherwise safe batch conversion.

## When to Apply
- Bulk converting `.mat` files from URP Lit to FlatKit shaders.
- Adding or removing shader keywords across generated model materials.
- Writing one-off asset repair scripts that operate on Unity text serialization.

## Examples
Before, brittle matching:

```python
text.index("  m_InvalidKeywords:\n")
```

After, serialization-shape tolerant matching:

```python
r"^  m_InvalidKeywords:.*?(?=^  m_LightmapFlags:)"
```

## Related
- `Assets/ShooterSurvival/Materials/MeshyAI`
