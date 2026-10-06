# S22 performance and visual audit - 2026-10-01

Status: report completed; diagnosis only, no gameplay fix or APK deployment.

User asked for a deep investigation of severe S22 lag (possibly movies) and awkward/generated-looking art, models, motion and presentation, with a written report and PNG evidence.

## Delivered

- [Korean report](../../reviews/s22-performance-visual-audit-2026-10-01.md).
- PDF: `output/pdf/s22-performance-visual-audit-2026-10-01.pdf`.
- Browser report: `outputs/s22-quality-audit-2026-10-01/site/index.html`, loopback gallery `http://127.0.0.1:8797/`.
- Ten derived PNG evidence plates/charts, plus native UI and player multiview originals under `tmp/image-previews/s22-quality-audit-2026-10-01/`.
- Raw scene/build/movie inventories, seven-condition video/render experiment, six-phase occlusion experiment, native observations and restoration receipt under `outputs/s22-quality-audit-2026-10-01/`.

## Findings and coverage

Camera occlusion is a demonstrated PC CPU cost: 1,310 groups, 9.39ms synchronous function mean, and repeated fixed-view ON 19.42/19.35ms median versus OFF 9.23/8.69ms with equal rendering counts. Other confirmed risks: 470,390-triangle hole, 379 copies of a 14,079-triangle guardrail, 166/170 RestStop textures not opted into streaming, and 187-1,088 batches in the Highway observation.

Visual findings include protagonist identity/anatomy mismatch, ceiling/back-of-sign-heavy market camera framing, 2x-specific stronger transparency, repeated market dressing, feedback overlap and RestStop sign occlusion/low contrast. Seven UI screens retain a coherent palette and useful labels. Invalid isolated skin captures were excluded from motion conclusions; actual Play player views establish the anatomy finding.

Three Noryangjin route observations stopped at 263.0/262.7/263.7 seconds (EnemyContact/Hole/EnemyContact). Highway observation covered 100 seconds; RestStop approach stopped at 24.23 seconds from traffic. A separate staged interior observer ran 61.27 seconds but did not trigger the holdout. These are not natural-player balance or full campaign clear claims. No connected ADB device; S22 FPS/thermal diagnosis remains pending.

## Verification and restoration

Official Unity CLI/Pipeline reachable; dynamic diagnostic scripts compiled and ran in memory. No scripts were added to Assets. Source FBX topology checked through Blender 4.4; HTML, PDF text/layout and PNG evidence checked. `restore-verified.json` confirms canonical registry bytes equal the original, wallet 40,218 coins/30 jewels, original clean Revamp scene and stopped Play Mode. Original test toggles and start-scene selection restored.

Game source/assets/settings were not edited by this work. The pre-existing FONT Menu.asset and Android addressable state changes were preserved. Existing test reports were not relabeled as newly passing tests.

Headless learning capture: [qualify build and capture evidence](../../solutions/workflow-issues/qualify-build-and-capture-evidence-in-mobile-audits-2026-10-01.md). Gallery can be restarted with `python tools/serve-s22-audit.py`; it serves only curated output on 127.0.0.1 and does not expose save snapshots.
