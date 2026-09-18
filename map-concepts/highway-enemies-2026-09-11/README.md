# Six Highway enemies and latest props

## Combat models

| Model | Role | Final triangles |
|---|---|---:|
|ConeMechanic|라바콘 정비공|23249|
|TrafficPatrol|교통 순찰대|23247|
|TollgateChief|톨게이트 대장|23468|
|TireBruiser|타이어 중장병|35441|
|AsphaltWorker|아스팔트 작업자|28928|
|DeliveryRider|배달 라이더|23554|

Runtime prefabs are under `Assets/ShooterSurvival/Prefabs/Highway/Enemies/`; FBXs and URP materials are under `Assets/ShooterSurvival/Models/Highway/Rigged/`. Each has an18-bone generic skeleton and `idle`, `walk`, `run`, `attack_loop`, `attack_once`, `die` actions, connected to the existing enemy event/combat system.

The first three identities retain the previously reviewed references. The new three were generated with built-in ImageGen and the installed local TRELLIS2 engine. Their original combined body/prop meshes bent the tire/tool/parcel during motion. Those candidates were retained for comparison; final new bodies were reconstructed without held props and given separate rigid hand-mounted geometry. Tape bevels and tiny degenerate edges were cleaned before export.

Source recipe: `tools/rig-highway-enemies.py`. Authored `.blend`, exported `.fbx`/`.glb`, pose previews and fresh-import metrics are under `outputs/highway-rigged-2026-09-11/<model>/`. All six latest `metrics-clean-final.json` inspections pass with no reported issues. Rest and walking multiviews were inspected; full-resolution copies are in `tmp/image-previews/highway-enemies-2026-09-11/`.

[Final metrics](final-model-metrics.json) · [Combat prefab bindings](combat-prefabs.json) · [Modeling contract](modeling-contract.md)

## Prop inventory

The29 top-level Highway result folders in `C:/Users/ljh/Desktop/AI 프로그램/Trellis 자동화/결과물` were compared by SHA256. None of their FBXs exactly matched the older project imports. All29 latest FBXs and textures were imported separately, preserving the originals and their hashes.

- Source inventory: [props-source-inventory.json](props-source-inventory.json)
- Imported paths, sizes and hash checks: [props-imported.json](props-imported.json)
- Unity models: `Assets/ShooterSurvival/Models/Highway/Props/`
- Palette prefabs: `Assets/ShooterSurvival/Prefabs/Highway/Props/`

Image briefs used isolated full-body references with separated arms and readable equipment: yellow-vest tire carrier, orange-coverall asphalt worker and teal-jacket delivery rider. Body-only revision briefs removed only the held objects while preserving identities/outfits; the backpack remained on the rider. All were made using the built-in image tool, then copied into this workspace.
