# Darker, earlier seagull warning with 50% larger footprint

> The user subsequently rejected this landing timing. Size/contrast remain, but timing and grounded lifetime are superseded by [the early-landing correction](../seagull-early-landing-2026-09-20/README.md). Below is the prior iteration's evidence.

The user requested a more visible black warning, earlier notice, and approximately 50% enlargement of both the bird and the warning diameter.

## Applied values

| Setting | Prior | Current |
| --- | --- | --- |
| Maximum warning diameter | 1.6m | 2.4m |
| Bird rig scale, including inherited contact shapes | 1.4 | 2.1 |
| Initial warning diameter | 0.6m | 1.8m, readable from farther away |
| Warning trigger distance | 13m | 21m |
| Warning before descent | 1.1s | 2.1s |
| Strongest warning alpha | 0.376 | approximately 0.828 |
| Landing offset | 0.05m | 0.075m |

The source shadow image itself has alpha capped at 96/255. Its SpriteRenderer was already fully opaque, so raising the renderer alpha could not solve the faintness. The scoped `Seagull_WarningDark.mat` uses the original texture in a transparent URP Unlit material and multiplies texture alpha by 2.2, retaining the soft edge and darkening the core. The shared image is unchanged.

The added warning time compensates for starting 8m earlier, preserving useful contact timing instead of making the bird leave before the runner arrives. Descent remains 0.5s from 6m. Existing single-contact behavior, 20% maximum-health damage and two-turn spin are unchanged. Native authoring updated the source prefab and SR18 placement, and removed the generator's old timing overrides.

## Verification

- All 10 `SeagullContactTests` passed, including real prefab/scene footprint, sampled flying/sitting mesh containment, material alpha and contact behavior.
- Both C# project builds passed. Runtime retains the pre-existing ithappy `SplineSpeed.m_LastIndex` warning.
- The agent-harness validator and solution frontmatter validation passed. Chrome playback confirmed the current captions, native footage and dark early warning; the gallery tab is retained. Unity ended in saved SR18 Edit Mode.
- `play-v1/report.json` records warning onset at 20.68–20.86m in the moving cases and a maximum diameter of 2.39999986m.
- Native left/right dodges of 1.49986m caused no damage or spin. Center contact and landed contact each reduced 500 HP to 400 once and produced 720 degrees of spin. No console errors occurred during the probe.
- Native images show a dark spot well before descent and a larger, clearly black circle near landing. Videos retain all 180 frames at 30fps for each case.

## Tools and evidence

`tools/resize-seagull-hazard.cs` installs current settings/material through official Unity `run_script` in clean SR18 Edit Mode. `before/` retains the prior prefab and saved scene. `tools/install-seagull-contact.cs` retains the updated landing offset on collider regeneration.

`tools/verify-seagull-warning-readability.cs` captures actual movement, delayed steering through `PlayerMove`, contact and landed contact. `python tools/build-seagull-warning-gallery.py` checks measured outcomes and builds the gallery from native frames without interpolation.

[Actual video and warning images](http://127.0.0.1:6753/seagull-warning-readability/). Preview copies live in `tmp/image-previews/seagull-warning-readability-2026-09-20/`.

This verification covers the current SR18 placement and source prefab in the Editor. It does not constitute a mobile-device playthrough.
