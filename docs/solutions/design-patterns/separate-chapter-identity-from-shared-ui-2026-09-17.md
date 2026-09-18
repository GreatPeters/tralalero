---
title: Separate chapter identity from shared UI chrome
date: 2026-09-17
category: design-patterns
module: Chapter lobby presentation
problem_type: design_pattern
component: development_workflow
severity: medium
applies_when:
  - "A common UI layout is reused across locations with different visual identities."
  - "A scene-specific symbol has been baked into a shared UI sprite."
tags: [unity, ui, chapter-themes, semantic-assets, shared-design]
---

# Separate chapter identity from shared UI chrome

## Context

The faithful harbor UI was applied across Noryangjin, Highway and RestStop. Its typography and interaction structure were consistent, but the same baked anchor/rope/wave plaque incorrectly identified all three locations as a harbor. The user circled the Highway anchor and requested fitting treatment for both road chapters. Pixel fidelity to a reference does not establish suitability for every context.

## Guidance

- Share layout, typography, currency controls and navigation behavior. Treat the chapter emblem, header palette and decorative motifs as a chapter-owned slot.
- Resolve that slot explicitly from the authored chapter ID. Here chapter 1 retains harbor art, 2 uses expressway-green motorway/lane art, and 3 uses teal coffee/service art.
- Reuse the same selection helper in both the narrow repair and normal UI authoring path. Editing only the saved scene would be undone by the next full installer run.
- Require an explicit theme for a new chapter instead of silently defaulting every unknown location to the harbor.
- Keep semantic scope clear: the global opening story still describes the harbor origin, while the chapter lobby identifies the current location.
- Verify the new assets with actual live text. A decorative cutlery motif initially extended upward into the title area; shrinking it before native acceptance retained separation.

## Verification

Tests name the expected symbols independently of the implementation mapping: LobbyPlaque, HighwayPlaque, RestStopPlaque. Native checks exercise settings/upgrades return paths so a reactivation cannot restore the old image. A fresh scene hash confirms the accepted Noryangjin scene is untouched by the narrow change.

Evidence: `map-concepts/chapter-ui-themes-2026-09-17/README.md`. Related: [reference artwork and native presentation](../ui-bugs/verify-mobile-sdf-and-world-presentation-in-native-frames-2026-09-16.md). The overlap is the authoring pipeline; the distinct failure is semantic chapter identity rather than font rendering or reference fidelity.
