# Harbor in-game UI revision concept

## Request and scope

User: “지금 전반적으로 인게임 UI가 왜인지 모르겠지만 좀 촌스러운데...인게임 UI 시안 전체 톤앤매너에 맞춰서 좀 수정본을 공유해줄 수 있어?”

Delivered one static three-screen proposal: combat HUD, in-run settings, and defeat/retry. This request asks to share a revision concept. No Unity scenes, runtime scripts, settings, saves, or installed art were changed.

## Proposal

- Keep the selected Harbor Workshop identity: cream surfaces, walnut headers, restrained brass details, with ocean teal for primary actions.
- Group health and attack in a single compact upper-left HUD and align the settings gear at the top right. Keep the green player-local health bar.
- Reduce repeated frames and oversized empty parchment areas. Use simple icon/label/value rows, readable Korean labels, and a clear primary action.
- Present sound/vibration states, volume/sensitivity sliders, resume/retry, existing legal/ad privacy entries, survival time, earned coins and the existing optional ad reward.

## Sources and deliverable

- [Revision PNG](../../tmp/image-previews/harbor-ingame-ui-revision-2026-09-15/ingame-ui-revision-v1.png)
- [Exact generation prompt](prompt.txt)
- Background/function references: `tmp/image-previews/harbor-polish-2026-09-15/final/Noryangjin_MapTool_Mode_SR18/{07-hud-half-health,06-run-settings,09-defeat}.png`.
- Style reference: selected `ui-concept-c-v2-gameplay-start-numeric-levels.png` in `tmp/image-previews/ui-original-variation-2026-09-14/`.
- Generation: built-in image_gen, four inspected local image references. Original output retained under `C:/Users/ljh/.codex/generated_images/01a0a558-174f-7433-a252-6c7083a649f2/exec-f5ce191f-1c5c-48f7-a2cc-7469205219ec.png`.

## Verification and limits

Inspected the completed 1536×1024 board for Korean copy, all three screens, half-filled health bars, visible close controls, existing actions and overall consistency. Numbers are illustrative values from the source captures. Font appearance is generated, not proof of actual KERIS/Gmarket TMP rendering. Background is image-generated from native screenshots, not a fresh runtime capture or pixel-exact composite. Legal URL availability still follows runtime configuration; the concept's link rows do not establish working destinations. Individual reference frames are narrower than the board's portrait panels; implementation must retain the actual device aspect and safe area.

Unity Pipeline reachability and clean SR18 Edit Mode were confirmed with `unity pipeline list`, `unity command --project-path . list_open_scenes`, and editor status. Existing native screenshots were sufficient; no Play Mode transition was needed. No code tests apply to this static concept delivery.
