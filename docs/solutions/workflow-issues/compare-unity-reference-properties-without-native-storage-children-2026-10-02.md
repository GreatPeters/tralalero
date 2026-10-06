---
title: Compare Unity object references without their native storage children
date: 2026-10-02
category: workflow-issues
module: Chapter 4 reference scenery installer
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - A serialized scene fingerprint changes after reopening an otherwise unchanged scene
  - A visual installer must preserve gameplay references while adding scenery
tags: [unity, serialized-property, global-object-id, scene-preservation]
---

# Compare Unity object references without their native storage children

## Context

The Jamsil reference-street installer passed its pre-save gameplay/collider/camera comparison, then reported a saved-scene mismatch. The first implementation traversed every `SerializedProperty` with `Next(true)`. Although it recorded each reference using `GlobalObjectId`, it also descended into that reference's native `m_FileID`/`m_PathID` children. In this API those children contained session instance handles, which changed on scene reopen.

## Guidance

Compare an object-reference property once using its stable identity and skip its storage children. Descend into generic containers to retain nested fields and array elements. Record array size explicitly. Keep actual object references and scalar values in the contract; do not remove all references to make the check pass.

```csharp
bool descend = true;
while (property.Next(descend))
{
    descend = property.propertyType == SerializedPropertyType.Generic;
    if (property.propertyType == SerializedPropertyType.ObjectReference)
        Record(property.propertyPath, GlobalObjectId.GetGlobalObjectIdSlow(property.objectReferenceValue));
    else if (property.propertyType != SerializedPropertyType.Generic)
        RecordScalar(property);
}
```

First run a read-only reopen experiment. Here, 1,055 field-record lines changed with no save at all; retaining canonical references and omitting only the native storage children left zero differences. The corrected iterator then returned zero changed lines across 5,275 serialized/matrix lines. The subsequent visual application passed both the pre-save and reopened-scene contracts.

Retain diagnostic summaries before parsing tool output, and bound returned data. A full serialized scene diff is far too large for a tool response. Keep useful evidence locally and return counts plus a few differing properties. The first failed application did not retain its exact pre-save record outside the call; its pre-save pass is evidence, but does not constitute a recoverable original scene payload.

## Why this matters

An unstable verifier can report false regressions or tempt an agent to weaken meaningful checks. Establishing the verifier's own no-op behavior separates a tool defect from a product change. Run actual native play and camera review separately: a stable serialization comparison does not prove visual quality.

## Examples and evidence

- Authoring and corrected iterator: `tools/apply-chapter4-reference-street.cs`.
- Successful application: `outputs/chapter4-reference-2026-10-02/street-apply-20261002T110259897.json`.
- Native before/after survey: `outputs/chapter4-reference-2026-10-02/`.
- Related: [Native reference interiors and animation validation](validate-reference-interiors-and-retargeted-poses-in-native-camera-2026-09-28.md).

Captured through `ce-compound mode:headless`, with sequential research per AGENTS.md. Existing solution searches found related native-scene validation guidance but no duplicate of this property-iterator failure. No session-history search or external issue creation.
