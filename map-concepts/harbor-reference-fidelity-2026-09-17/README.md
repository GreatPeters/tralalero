# Reference fidelity correction · 2026-09-17

Status: complete. All three scenes saved and reviewed; 13 native tests and both builds passed. Today's 69 preference records restored exactly (191 coins). SR18 is open in Edit Mode. The review collection contains 7 PNGs plus the native hand-motion GIF.

## Why the prior result differed

- The prior font family was consistent, but display hierarchy was weakened: wallet 42, chapter 34, name 59, nav 36, prompt 70. Borders used a 2.8 pixels-per-unit multiplier. That made the live interface much thinner/smaller than the supplied concept.
- `Coastal_White` did have OUTLINE_ON, but width .065 on a 9-pixel padding atlas was a restrained body-text treatment, not the requested broad navy outline. The new display SDF uses the same existing Gmarket source with 96-point sampling/24 padding, and dedicated white/header/ink presets.
- A wide SDF outline without matching face dilation consumes the white interior. Candidate 1 intentionally retained as failed evidence. Candidate 2 balances face dilation against stroke width and gives the smaller CHAPTER label a separate thin stroke.
- `Swipe.png` contains both arrows and the hand. The corrected hierarchy reuses separate Left/Right and GestureHand sprites; only the hand translates/tilts, using unscaled time.
- Merchant camera depth was 66.6 m with a 2.6 m source body. His station is now at (52.7,.07,-118.9), scale 1.8, with shop (49,.07,-121.5), scale 1.1. The side deck moves with them and floor support is verified. A blocking post in the combined pier mesh required a local entrance mesh: 127 above-deck vertices of HarborOpeningPier_03 are lowered to deck height; its source FBX remains unchanged, and its MeshCollider uses the same local copy. Walking-floor vertices remain unchanged.

## Scope and reproducibility

- Narrow `HarborGameUIInstaller.ApplyReferenceCorrectionsAll` changes existing lobby nodes across three scenes, preserving buttons and unrelated user edits. The full Coastal installer calls the same correction last so a rebuild cannot replace display materials with body materials.
- Fresh current scenes, sources/materials and prefs: `tmp/backups/harbor-reference-fidelity-2026-09-17`. Unity initially reported 13 in-memory roots; today's serialized baseline and the final scene contain the same 10 root IDs. Preserve today's snapshot rather than substituting yesterday's files.
- Tools: `apply-harbor-reference-20260917.cs`, `place-merchant-reference-20260917.cs`, `open-workshop-entrance-20260917.cs`, and `capture-harbor-reference-20260917.cs`.
- Preview folder: `tmp/image-previews/harbor-reference-fidelity-2026-09-17`.

## Acceptance evidence

- Native tests: `tests-fidelity.json` 3/3 and `tests-regression.json` 10/10. Three scenes verify display atlas padding/material/white face balance, independent arrow/hand layout, retained common UI/bonus bindings and supported merchant/entrance floor geometry.
- `accepted/<scene>/`: actual lobby, hand motion and gameplay captures. The previous candidate folders are retained, including the rejected all-navy prompt.
- SR18 full-cycle hand travel is 127.14 reference pixels; both arrows remain stationary. Disable/reopen restores the hand to center. `start-hand-motion.gif` is a 32-frame native capture, encoded with the existing TRELLIS environment's Pillow package. A prior two-point phase sample gave a false negative; the final check samples the complete cycle instead.
- `merchant-camera.txt`: no road collider blocks the camera-to-head ray, head inside the portrait view, baked foot minimum Y .06992 with supported deck at approximately .04, and actual Animator state SeatedIdle after the greeting. No standalone size or bounds assertion substitutes for these native images.
- Runtime and Editor builds pass with zero warnings on the final incremental build. Earlier compilation reported only the existing third-party SplineSpeed unused field.
- Recovery uses today's `playerprefs.tsv`/`extra-prefs.tsv` only. `restoration.txt` records exact recovery, and the original SR18 scene remains open in Edit Mode. No current workbook balance, stored ownership or scene-root additions were reverted.
- User review collection: `tmp/image-previews/harbor-ui-all-2026-09-15/5-reference-fix-2026-09-17/index.html`.
- `ce-compound mode:headless` updated the existing mobile SDF/native-presentation lesson with the user's rejection, display hierarchy and inward SDF stroke correction. Existing AGENTS discovery instructions already suffice.
