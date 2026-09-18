---
status: active
date: 2026-09-12
---

# Flow opening and live analytics test

User requested one actual logging test after the Tra connection and four new opening movies through the logged-in Google Flow account.

## Current checkpoint

### User-approved installation of reviewed01/02

- User compared the new reviewed clips with the installed movie and explicitly requested applying the recommendation. The shared game MP4 now starts with the two reviewed Flow clips; old03 and14B04 follow.626frames/24fps,720×1280,total26.083333seconds; movie GUID preserved in SR18 and HighWay. Previous full movie backed up under `map-concepts/flow-opening-2026-09-12/installed/`.
- Unequal caption/seek boundaries fixed to0,8,16,505/24seconds.12timing tests pass. First live Play confirmed playback, natural page transition, both Next seeks and end cleanup. Full-frame fitting was then added after actual screenshots exposed edge cropping on a tall Game view; final verification is recorded in `installed/playback-fit.json`.
- No new generation or credits used. Scene03's Flow remake remains incomplete/provider-refused. See `installed/README.md` for installation, backups, testing and fresh preference restoration.

### Latest user correction — remake scenes 1, 2 and 3

- User rejected scene01's shoe impact/explosion: the shark must escape while carrying the stolen shoe in its mouth. Scene02 must begin with an ordinary shark and show transformation into the referenced cursed blue shark; scenes02/03 must preserve three feet. The supplied original four-panel storyboard is the identity/story reference. This new specific request supersedes the earlier pending redesign question.
- Two revised individual starting frames were produced with built-in imagegen, visually inspected, saved under `map-concepts/flow-opening-2026-09-12/revision-2/` and uploaded to the same Flow project along with the storyboard. `Story_02.png` is scene02's explicit last frame; `Story_03.png` remains scene03's first frame. The entire comic is context, never a literal movie frame.
- Flow displayed a concrete 3 × 100-credit Veo3.1 Quality/9:16/8-second plan. One-time approval was clicked at approximately19:10 KST. Three outputs are pending; automatic retries and upscales were not requested. Full prompts are in `revision-2/prompts.md`.
- Revision2 result: scene03 was refused by the provider and not charged. Scenes01/02 downloaded and decoded, but actual references were misassigned despite the correct filenames in Flow's plan. Scene01 used the purple before-curse image; scene02 used the sunny escape image and had distorted anatomy/loose shoes near the endpoint. Both are retained as rejected candidates; see `revision-2/README.md`.
- Recovery: verified the three exact source media IDs through their editor pages. A fresh Flow session now explicitly attaches the scene01 image through the asset picker and gives its verified ID, avoiding filename-only multi-scene inference. One scene01 Quality request at100 credits was approved. Scene02 will likewise attach its two verified inputs. No retry of the provider-refused scene03 has been submitted.
- Latest result: corrected scene01 generated, downloaded and passed the carried-shoe story check. Scene02 initially failed on editor-ID versus media-ID confusion, then generated from actual attached references; the raw ending still briefly loses one foot. `revision-3/reviewed/02_Natural_Shark_Transforms.mp4` excludes that ending, playing the first6seconds of continuous generated animation at75% speed for8seconds. Both reviewed originals fully decode720×1280/24fps/192frames with audio. Raw and rejected versions are preserved; final image samples were inspected. See `revision-3/README.md`.
- Scene03 remains provider-refused and incomplete; no blind retry or new scene04 was submitted. Both reviewed01/02 were uploaded and verified in the same Flow project. Google One's activity ledger shows net500credits debited, balance350: six100-credit debits and one100-credit refund. The media-not-found attempt at19:22:36 still has no matching refund, contradicting its no-charge tile. Record this as unresolved billing discrepancy. No game-movie installation was requested or performed.

### Latest — logging verified; Flow redesign decision pending

- Build succeeded0 errors/849.09s; ARM64/custom signing restored. Latest APK installed and one actual round run in isolated read-only Android emulator. SDK start/end IDs match,38133ms/death and required parameters logged; HTTP204 after both. Firebase DebugView independently shows both events and matching duration/ID. New Tra row awaits the daily export; no synthetic rows inserted. Emulator rendering errors limit this to analytics verification. Task emulator/readers stopped. See map-concepts/analytics-smoke-2026-09-12/README.md.
- Browser connection recovered and user enabled file-URL access. All four original images uploaded to Flow. A concrete four-clip Veo3.1 Quality/9:16/8s/one-output plan at400 credits was approved once.
- Scenes01/03 generated and were downloaded/decoded as720×1280,192-frame originals. Scenes02/04 were refused by the provider for third-party content interests and explicitly not charged. Actual balance1050→850,200 credits used. No blind refusal retries or watermark removal.
- Scene03 has body-proportion drift; successful clips are review candidates, not an accepted complete replacement. Existing game opening remains unchanged. See map-concepts/flow-opening-2026-09-12/README.md.
- Pending async question: redesign the refused scenes with distinct original artwork versus retain the existing character and hold those two clips. No user answer yet. The historical checkpoint below is superseded.

### Historical setup checkpoint

- Tra connection is already complete; previous round import is not a new test.
- No physical ADB device is attached. Started existing Google Play API36 x86_64 AVD in read-only/headless/no-snapshot-save mode. ADB serial emulator-5554; task launcher PID44776, recorded in tmp/analytics-flow-20260912/emulator.pid. Google Play Services is present. Preserve the user's original AVD state; do not wipe it.
- Latest-code Android test APK build is running through official Unity Pipeline job4dd731d1bc0d4f6bb98e286aa894ee5f. Script tools/build-analytics-emulator-smoke.cs uses only SR18 for this smoke build, x86_64, development/debug signing. It records/restores the original ARM64/custom-keystore settings in finally. Before snapshot and result files are under tmp/analytics-flow-20260912. Check build-report.json and build-settings-restored.json before further Unity mutations. Output target Builds/Android/AnalyticsSmoke-20260912.apk. The retained July31 APK is old and does not validate current code.
- Real SDK emission/upload and Firebase receipt have NOT yet been tested for this build. Editor Analytics is a stub. Distinguish DebugView testing from normal daily exports; do not inject synthetic BigQuery rows to simulate success.
- Flow project created: https://flow.google.com/project/2e2681ad-0541-4699-bfdb-55e4d338508d, title Tralalero Opening — 4 scenes — 2026-09-12.
- Account shows1050 credits and mandatory regional watermark. No generation submitted and no credits spent. Intended four inputs: Assets/JH/UI/Opening/Animated/Story_01.png through Story_04.png, all visually inspected. Story order: stolen offering, curse, deity's condition, shark walking. Preserve three-legged story character in scenes2–4, coherent blue/gold shoes, actual character motion and stable camera; no generated subtitles/dialogue.
- Intended model Veo3.1 Quality, portrait9:16, one output per scene. Initial browser setting changes were not saved. Native UI selected9:16 but the subsequent model-selection action was blocked; do not assume Quality or the settings were saved. Default model on fresh load was Omni1.1Flash.
- Browser CUA initially worked, then repeated CDP timeouts and Debugger unattached. A duplicate Flow tab1194358918 was opened but navigation timed out. Original Flow tab1194358912 was in Chrome browser2, extension profile97e8944c-4340-470e-afe8-896160104811.
- Native Computer Use fallback was initialized through mcp__node_repl__js + @oai/sky, selected returned Chrome window526786. It could operate the UI after activating/maximizing Chrome and dismissing two obstructing Chrome notification toasts. It then explicitly stopped this turn because it could not determine the browser URL with enough confidence to enforce policy. No further app input is authorized through that stopped runtime in this turn. User must re-establish browser control and continue in a new turn.
- Existing game opening remains the three5B shots plus last14B shot. No Flow output exists yet. Do not replace the game movie with placeholders or claim all four were generated.

## Resume

1. Check the ongoing build/result and verify ARM64/custom signing restoration. If successful, install the new APK only on the isolated emulator and run one actual game round; inspect SDK logs/receipt and the cloud surface when accessible.
2. Reconnect the user's Flow browser control, inspect the current project/settings, upload the four source images, and submit exactly four reviewed video generations. Inspect the concrete credit cost before submission; no top-up/subscription change is requested.
3. Download and validate the four finished clips, retain source prompts and timing/credit evidence, and assemble an opening candidate. User requested generation of all four; no blanket instruction to replace the existing movie with unreviewed outputs.
4. Stop only the task-owned emulator at closeout, leave original AVD data intact, verify project settings, and document the logging result and any daily-export timing limit honestly.
