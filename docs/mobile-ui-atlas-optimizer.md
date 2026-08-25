# Mobile UI Atlas Optimizer

Open `Tools/Shooter Survival/Optimization/Mobile UI Optimizer` and press
`UI 아틀라스 최적화 및 검증` after adding or changing production UI Sprites.
The operation is idempotent and updates only assets it owns under
`Assets/ShooterSurvival/UI/Atlases/`.

## What the button does

Every run scans the saved Noryangjin production scene dependencies. Newly
referenced compatible Sprites are therefore included without maintaining a
manual list. The tool creates three co-usage groups:

- `HUD_Common`
- `Lobby_Setting_Menu`
- `Upgrade`

Editor reference images, RawImage/TMP textures, large or full-screen
backgrounds, non-Sprite textures, and source dimensions above 1024 are excluded.
Packing uses Sprite Atlas V2, 4-pixel padding, no rotation or tight packing,
no mipmaps, Bilinear filtering, a 2048 maximum size, and the Android compressed
platform override. The repository default keeps Sprite Atlas V2 enabled.

The button does not edit scenes, prefabs, runtime UI scripts, source Sprite
importers, or rendering quality assets. It preserves existing Sprite references
and writes its machine-readable report to
`Library/MobileUiOptimizer/latest-report.json`.

## Validation contract

The operation checks:

- no increase in missing serialized Sprite references;
- the production scene file hash remains unchanged;
- all three owned atlases are valid and included in builds;
- packing and Android settings match the expected contract; and
- a second invocation would produce no changes.

Run `MobileUiOptimizerTests` for classification, settings, scene safety,
rendering-quality preservation, packable uniqueness, and idempotence coverage.

## Measured result

At the fixed 1080x2340 Editor pre-start view, three repeated measurements gave:

| Metric | Before | After |
| --- | ---: | ---: |
| Draw calls | 67 | 58 |
| SetPass calls | 35 | 34 |
| Active UI textures | 11 | 2 |
| Packed Sprite sources | 0 | 68 |

This is a 13.4% draw-call reduction. A two-atlas merge reduced one additional
draw call but was rejected because it did not exceed the configured measurement
noise and would load transient Upgrade content with the Lobby atlas. A stricter
512-pixel source cap removed five Sprites but changed no runtime metric.

The first Play after atlas import can include a large cold-cache frame. Editor
total allocated memory also drifts after imports and is diagnostic only. Verify
texture residency, GPU bandwidth, cold-atlas loading, and thermal behavior on a
representative Android device before treating Editor memory or FPS as shipping
evidence.
