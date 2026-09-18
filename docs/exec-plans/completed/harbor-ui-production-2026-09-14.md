# Harbor UI assets and player health bar

Status: completed. The subsequent explicit integration request was completed in `harbor-ui-live-2026-09-15.md`.

Date: 2026-09-14

## Scope

The user selected `exec-abd1090d-a9f9-4b10-bd1d-837511a2045d.png` (the preserved v2 board). Requested changes: a green bar following the in-game shark; actual scene beneath start UI; permanent/chapter upgrade tabs with chapter-unlock-gated large upgrades; ownership-dependent purchase/equip action; explicit stat names; reusable separated UI assets with a fallback if flat-image cutting is imperfect.

At the asset-pass stage the runtime-scope question was unanswered. The user's later explicit request authorized full integration, now recorded separately in the completed live-UI plan.

## Verification targets

- Capture the real Unity camera; do not substitute generated scenery for the live start scene.
- Separate editable labels from decorative raster assets; preserve alpha and test light/dark backgrounds.
- Health bar follows the player, reflects damage/retry and leaves the existing status HUD intact.
- If full runtime application is selected, verify tabs, unlock gating, persistence and owned/unowned shop states through actual UI entry points.

## Work log

- Official Pipeline reachable; original SR18 scene was clean and outside Play Mode.
- Found existing separated UI PNGs and layered upgrade PSDs under Assets/JH. Existing runtime equipment effects already use explicit stat labels; action copy currently combines purchase/equip.
- Actual camera reference saved to `tmp/image-previews/harbor-ui-production-2026-09-14/scene-reference.png`.
- Initial generated sheets contained an opaque checkerboard (RGB, no alpha). These are rejected as production sprites. A flat magenta edit and reproducible chroma-key extraction will supply real RGBA assets.
- September15: 35 sprite assets imported, eight 9-slice surfaces configured, 35-layer sprite PSD and 27-layer exact-scene start PSD saved/reopened. Main and chapter/purchase state images are in the production README.
- Health bar installed in three scenes; SR18 native damage and actual retry verify 60/1.0 → 39/0.65 → 60/1.0, one bar. Cosmetic action labels verify purchase/equip/equipped and hidden owned currency. Builds pass, test preferences restored.
- Asset/design pass completed first. Full visual installation and chapter-upgrade logic were subsequently completed under the user's explicit integration request; see the live-UI plan and evidence index.
