# Further seagull size adjustment

Applied the user's requested increases relative to the preceding impact-tumble version:

- Black warning diameter +20%: 2.34–3.12m → 2.808–3.744m.
- Bird and inherited contact-shape scale +30%: 3.15 → 4.095. Landing offset scales from 0.1125m to 0.14625m.
- The 28m trigger, 1.0s warning, 0.4s descent, grounded waiting, 20% HP penalty, shark spin and bird tumble are unchanged.

`tools/resize-seagull-hazard.cs` applied and saved the source prefab and SR18 placement through native Unity APIs. `before/` retains the previous scene and prefab. Component defaults and the collider-regeneration tool use the matching dimensions/offset.

Unity recompiled without errors and all 14 `SeagullContactTests` passed against the saved prefab/scene. The landed body remains within the warning circle; the separately requested growth rates allow flying wing tips at most 0.25m beyond its radius.

## Fresh video requested after the size change

Recorded the current saved sizes to `play-v1/` using `tools/verify-seagull-impact-tumble.cs` with `outputFolder` set to this new folder. Native results confirm a 3.744m warning, one 20% health penalty, 720-degree shark rotation and approximately 1080-degree bird rotation. Left/right dodges remained damage-free; pause and retry checks also passed. Unity was returned to saved SR18 Edit Mode.

Publish with `python tools/build-seagull-impact-gallery.py --record map-concepts/seagull-size-followup-2026-09-20/play-v1 --slug seagull-size-followup --shadow-diameter 3.744 --bird-increase 30 --shadow-increase 20`.

[Latest normal-speed and half-speed video gallery](http://127.0.0.1:6753/seagull-size-followup/). Native PNG/video copies are in `tmp/image-previews/seagull-size-followup-2026-09-20/`. Earlier galleries retain their original sizes.
