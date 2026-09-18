---
title: Preserve image-concept scope when applying a UI style
date: 2026-09-15
category: workflow-issues
module: Harbor UI concept workflow
problem_type: workflow_issue
component: development_workflow
severity: high
applies_when:
  - "A user says apply or 적용 during a sequence of image-concept requests."
  - "A cosmetic-shop concept must show the actual in-game character."
tags: [ui-concepts, user-intent, rollback, unity, image-generation]
---

# Preserve image-concept scope when applying a UI style

## Correction

After reviewing and collecting concept PNGs, the user said “업그레이드 상점도 적용해줘”, then “그리고 스킨 상점...실제 내 모습이 보이긴해야하는데...”. The assistant incorrectly modified Unity scenes and runtime UI. The user corrected this explicitly: “아니 적용해달라는 게 시안을 만들어달라는 뜻인데?”

The intended deliverable was another image concept using the approved style. In a concept-review conversation, resolve short follow-ups against that artifact context. The existence of a Unity project and an older authorization to install different UI do not expand a new concept request into implementation. Continue generating concepts without an unnecessary approval question when this context is clear.

## Recovery and verification

- Reversed only this turn's edits to the installer, upgrade tabs/cards and cosmetic-shop reopening behavior; removed the new installer and generated runtime assets.
- Restored all three scenes from this turn's native pre-layout backups. Native scene loading requires an Assets path: copied backups into a temporary Assets folder, opened/saved them through Unity, then removed the temporary assets.
- Backups were saved after new C# fields had compiled, so they contained 20 new default-only serialized fields per scene. After reversing the C# changes, native saving removed those fields. Automated comparison confirmed the remainder of every scene matches its backup exactly. Evidence: `tmp/backups/upgrade-shop-revision-2026-09-15/restoration-verification.json`.
- Restored the same-session preference snapshot. No directed purchase tests had occurred. Runtime/editor build passed after reversal, with the existing third-party SplineSpeed warning.
- A Pipeline evaluation can time out while its scene operation continues. Inspect completion evidence before rerunning a mutation.

## Image deliverables

Created a revised two-screen upgrade concept and a portrait cosmetic-shop concept. The cosmetic concept uses a captured actual gray-blue shark and cyan shoes as its visual reference; it remains a generated mockup, not a claim of exact pixel compositing or live UI implementation. Both are in `tmp/image-previews/harbor-ui-all-2026-09-15/`.

Related: [play intent and data limits](../design-patterns/preserve-play-intent-and-data-limits-in-ui-concepts-2026-09-14.md), [native UI activation and reload checks](../integration-issues/validate-connected-ui-through-activation-and-scene-reload-2026-09-15.md).
