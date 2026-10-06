# Ch5 toon reference implementation — complete within the approved Unity scope

The existing ShoeTower scene now uses the supplied cream/coral/teal mall direction. Both ordinary-stat branches completed in native Unity Play. Independent visual review accepted the final transfer/sign corrections. This is an incremental art edit to the existing game, not a new game or chapter redesign.

- [Local before/after and play gallery — 48 PNGs](http://127.0.0.1:1626/ch5-toon-20261004/)
- [Representative actual Unity PNG](../../outputs/ch5-toon-2026-10-04/validation/final-optimized/1F-35.png)
- [Final preservation audit](../../outputs/ch5-toon-2026-10-04/validation/final-preservation.json)

## Exact reference and authorized scope

The actual Desktop download was opened and viewed. Its exact copy is `outputs/ch5-toon-2026-10-04/reference/CH5-Mall-implementation-target.png`, SHA-256 `fde9b4ee37a97bc0c0f6db2e7ffc574619631c1b07ad66d62a4fe5d8230e66da`. The image is a concept illustration explicitly labelled as not a Unity screenshot. The earlier Library transfer problem was resolved by the user-provided local file; there was no transfer bypass.

The actual shark player and saved gameplay camera remain. The route remains 1F luxury → 2F fashion → 3F sports → 4F living → B1 food OR 5F tickets/6F cinema → 7F horizontal white shoe. No Ch1–4 conversion, route redesign, character replacement, new shader installation, paid generation, APK/device work, external publication/backup, commit or push took place in this art pass.

## What was actually applied

Existing scene: `Assets/ShooterSurvival/Scenes/Tools/ShoeTower.unity`. All new asset files are isolated in `Assets/ShooterSurvival/Models/Chapters/Chapters45/Ch5Toon20261004`.

| Category | Actual work |
| --- | --- |
| New native Unity geometry | Rounded cream shop portals and signs, round piers, category emblems, shallow window reveals, curved teal glazing and rail caps, coral promenade bands and peach inlays, ceiling cornices/fixtures, varied display plinths, shared broad-leaf foliage, closed escalator risers |
| Reused assets | Existing TRELLIS/Meshy shop goods and furniture, original character assets, existing textures through isolated Ch5 material clones; 16 additional visual instances of the existing clothing rack mesh |
| New isolated assets | 37 materials and 503 mesh assets; 1,080 files including metas. This includes retained intermediate authored meshes, not 503 additional scene objects |
| Store/floor scope | 56 existing shopfronts across the commercial floors; all 8 existing floors retained, including the special cinema space |
| New AI generation | 0 models, 0 paid credits, 0 new authentication/installations |
| Gameplay changes | None. Existing colliders, route data, combat, branch choice, rewards, lift destinations/durations and gameplay camera are unchanged |

FlatKit material tuning uses per-material absolute `_ColorDim` shadow colors, with Ch5-only copies. Shared source materials, textures, shaders, model files and fonts are unchanged. No renderer-wide replacement was applied to Ch1–4.

112 new entrance plants now use a shared 144-triangle leaf-pair mesh: their foliage fell from 1,120,000 to 8,064 triangles. Original generated plant files and original interior plants remain. The final audit counts 434 active renderers using new owned meshes and 1,369,800 triangles in that scope; this is not the entire scene triangle count. Combined native batches reduce object/render overhead but do not establish mobile performance.

## Defects found and corrected

Independent v1 review rejected washed-out shading, capsule-like window bottoms, weak product presence and a flat ceiling. The revisions corrected FlatKit shadows, rounded upper-only portals, varied plinth heights, planting and ceiling detail. Later camera review found new deep jambs and rear panels hiding merchandise; the new panels were removed and reveals shortened so the existing goods are visible. Sports goods received correctly aligned plinths.

Actual ordinary-route captures exposed an arrival-floor paint layer covering the hero during escalator movement. Seven presentation-only `Chapter45SceneryGroup` siblings now hide just the 14 new floor-paint renderers while arriving, then restore them. No existing collision slab, director or camera code changed. The seven groups are siblings to avoid nested visibility-cache conflicts.

The three real escalators and three retained arrival proxies now share gray metal tread materials and closed native riser batches: 192 existing tread renderers rematerialed, six small riser meshes added, no new colliders. Existing tread transforms and lift logic are preserved. Independent review caught the remaining beige arrival proxies; these were fixed and the three native transfers retested.

The 6F world-space arrival label moved upward from world Y 49.8 to 51.7, without changing its text or camera, to clear the hero's feet and progress area. Four final cinema positions were captured.

## Fresh native Unity validation

| Evidence | Result | Conditions |
| --- | --- | --- |
| `validation/both-branches/suite-summary.json` | 24 passed, 0 failed; both routes clear; no logged errors | HP 60, attack 8, 1× time, bounded ordinary movement inputs; no health pin, teleport, manual damage or forced win during either full route |
| B1 route, `run-00-ShoeTower-0` | 266.577545 s; HP 56; 5 lifts; goal claimed | Ordinary native route, normal combat and branch selection |
| Cinema route, `run-01-ShoeTower-1` | 312.1911 s; HP 28.2; 6 lifts; goal claimed | Includes the native 30-second cinema survival |
| `validation/transfers-after-fix/result.json` | 48 passed, 8 transfers, no errors | Sample existing lift entrance pose, call native BeginLift, then native updates at 1× time; 25/50/75% and arrival captures |
| `validation/arrival-proxies-after-fix/result.json` | 18 passed, 3 escalators, no errors | Narrow retest after the last arrival-proxy material/riser correction |
| `validation/native-retry/result.json` | 8 passed, no errors | Ordinary Start, 10 seconds of route, existing SettingsMenu Retry button UnityEvent, fresh counters/projectiles/rewards/encounters, native Start again |

The full route checks include ordinary/lift/cinema pause freezing, first and replay rewards, repeated completion callbacks not paying twice, clean lobby counters and no stale prior projectiles. The full routes ran before the final narrowly scoped transfer/text polish; all eight native transfers, three final escalator retests and four cinema views cover those subsequent presentation changes. These directed transfer fixtures isolate encounters and sample entrance poses; they are not claimed as another natural full-route pass.

Eleven before/after pairs have exactly identical camera position, rotation and field of view. Floor-art images are in `before` and `final-optimized`; the gallery uses `cinema-sign-after/6F-10.png` for the corrected 6F pair. The later transfer corrections are shown in their own section. Elevator frames precede the final 6F label-height polish; the four latest label views are separately labelled. Every gallery image was copied without pixel editing, served over localhost, and its HTTP bytes checked against the source SHA-256 (`gallery-verification.json`).

The separate read-only art reviewer inspected the reference, representative iterations, full-floor images, actual cinema combat/white-shoe goal, all eight transfer midpoint/arrival pairs, and the final ten escalator/sign images. Final visual correction review: **passed**. The reviewer confirmed clear hero/route visibility and metal tread consistency. Review text is preserved in `independent-art-review-final.txt`.

## Preservation and restored state

`validation/final-preservation.json` records:

- All 1,941 pre-existing captured gameplay/camera/collider/other non-text components and 1,650 related transforms are identical. Route serialization is identical. Only seven new presentation groups and their transforms were added to this protected set. Duplicate transient IDs were compared as a multiset.
- Route structure remains 422 segments, 8 authored floors, 8 lifts, 23 encounters. New decoration colliders: 0. New walking-floor body-corridor vertex candidates: 0. Six intentional riser surfaces are reported separately; this vertex sample supplements actual play and preserved colliders, and is not exhaustive occlusion proof.
- Of 1,592 existing scene dependencies, only ShoeTower.unity changed. The other seven Tools scenes are identical. Of 376 product baseline files, only ShoeTower.unity changed. Previous cycle18 projectile fixes are preserved.
- Both original scene/meta recovery copies hash-match their manifests. Intermediate recovery scenes remain available. Existing uncommitted work was preserved. Branch `qa/chapters45-full-suite-20261002` and HEAD `295a18cab0ad4156baa9106077bc10a47ac6d734` are unchanged; 127 already-deleted tracked entries remain as before, with zero new deletions.
- All 78 captured game preference records are identical, including key existence and type. All 41 non-engine native registry values are identical. Locale, test flags, starting scene, time scale, capture delta and the currently captured PT_ResourcesCleanup value were restored. No direct registry write was used.
- Unity PID 83488 is back in clean Edit, no owned validation callback or runtime warning mesh remains, and the own QA snapshot hash was erased. GameView size/zoom/settings/maximized state are unchanged. Window position changed during the session; desktop focus, cursor and position were observed rather than forcibly restored over user activity.
- `unity.player_session_count`, `unity.player_sessionid`, `unity_connect.session_id`, and `unity_connect.mega_session_id` were never written/restored. The historical PT_ResourcesCleanup value before the old full suite remains unknown; this pass restores its own recorded current value (false).

## Performance and remaining limits

Across 58 active-phase samples in the two ordinary routes, Editor frame time median was 67.72485 ms and p95 73.721405 ms; maximum batches 315, SetPass 75, Unity allocated memory 3,210,510,924 bytes. QA observation/capture overhead and minimized Editor conditions apply. The reported process-private-memory field was zero/unavailable and is not treated as a measurement. These are not phone FPS or mobile memory results. APK and device tests remain on hold as requested.

The result follows the target palette and rounded mall architecture but is not an exact rendering match: the concept has richer contact shadows/depth, and white 2F clothing remains low-contrast. The reviewer noted narrow beige strips between arrival steps, a briefly overlapping teal glass layer at 6F position 0, and the large label cropped at the top at 6F position 5; the hero and corridor are clear by later positions. These did not block final visual correction acceptance.

No full 1,135-test suite was rerun; the historical 52 failures are neither fixed nor reclassified by this pass. This Ch5 visual pass does not claim a new Ch4/continue/boss campaign suite; prior cycle18 evidence is separately preserved. No remaining blocker prevents using the saved Ch5 scene in Unity. Mobile performance and the above minor visual limits remain explicit.

## Reproduction

Scoped authoring and inspection sources are preserved under `tools/ch5-toon-20261004`. Use the official authenticated Unity Pipeline CLI; never use direct scene YAML edits. Example read-only inspection:

```text
unity command --project-path . run_script --file tools/ch5-toon-20261004/audit-final19.cs --entry AuditFinalToon19.Main --args '["outputs/ch5-toon-2026-10-04/validation/new-audit.json"]'
```

Mutating authoring scripts are one-time guarded and must not be rerun against the applied scene. New Play validation requires fresh exact state/preferences snapshots. Completed historical snapshots and restoration receipts must not be overwritten.
