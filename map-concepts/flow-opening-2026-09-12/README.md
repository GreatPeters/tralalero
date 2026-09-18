# Google Flow opening — two completed, two provider refusals

**Installed on explicit user request:** reviewed Flow01/02 now replace the first two shots in the game's shared opening. The assembled movie is26.083seconds; see [installation and verification](installed/README.md). Scene03 remains the previous game shot, and scene04 remains14B.

**Latest remake:** user corrections to scenes01–03 are documented in [revision3](revision-3/README.md). Reviewed01 now carries the shoe away; reviewed02 shows ordinary-shark transformation and excludes the raw late foot-loss defect. Scene03's remake was provider-refused. The original attempts below are historical and are not the latest accepted review files.

User requested all four opening scenes through their logged-in Google Flow account. Project: https://flow.google.com/project/2e2681ad-0541-4699-bfdb-55e4d338508d .

## Generation result

Four source images, `Assets/JH/UI/Opening/Animated/Story_01.png` through `Story_04.png`, were uploaded after the user enabled Chrome extension file-URL access. All inputs were visually reviewed. The request specified one8-second portrait9:16 clip per reference, Veo3.1 Quality, continuous character animation, natural ambience and no added subtitles/dialogue/music. The concrete Flow proposal was four clips at100 credits each and was approved once; persistent auto-approval was not enabled.

| Scene | Result |
|---|---|
|01 Stolen Offering|Generated and downloaded as `01_Stolen_Offering.mp4`; Flow media ID6e6ce49c-82a8-4085-85e4-90516ea6aa1f.|
|02 The Curse|Provider refused generation, citing third-party content interests; UI explicitly states no charge.|
|03 The Condition|Generated and downloaded as `03_The_Condition.mp4`; Flow media ID65f1c54a-0650-4805-9422-ef445a6ddb34.|
|04 The Journey|Same provider refusal/no-charge message as scene02.|

Balance was1050 before and850 after:200 credits actually used. No top-up/subscription change or retry intended to circumvent the refusal was performed. The user was asked whether to redesign the two refused scenes with distinct original artwork or preserve the existing character and leave those scenes pending; no answer had arrived at this checkpoint.

## Review

Both downloaded originals decode completely:720×1280,24fps,192 frames/8 seconds each, with audio streams. File sizes/checksums are in `validation.json`; `tools/validate-flow-opening.py` reproduces the check. Native720p files were retained rather than labeling an upscale as native1080p. The mandatory Veo watermark remains.

Frames at1,4 and7 seconds were inspected under `tmp/image-previews/flow-opening-2026-09-12`. Scene01 has a clear leap, shoe spark impact and return to the water. Scene03 has substantial body/head/arm animation, but the shark's proportions change and stable preservation of all three feet is not established. Audio streams decode, but no claim of a completed speech/content listening review is made.

The files are review candidates. The game's existing three5B shots plus last14B shot were not replaced. A complete four-scene Flow movie cannot be delivered while scenes02/04 remain refused and the redesign decision is pending.

## Prompt intent retained

-01: natural shark before the curse; leap from harbor toward the floating gold shoe; moving fins/tail/water/gulls, stable camera.
-02: purple stone shoe deity curls fingers; magical lightning binds all three blue/gold shoes; frightened shark recoils and tugs a foot.
-03: deity points toward market; shark turns, slumps, then nods with determination; awnings, vendors, gulls and water move.
-04: three-legged shark walks with lifted/planted steps, weight transfer, arm/tail motion and gentle tracking, without sliding.

The Flow session itself retains the full submitted request and each concrete generation prompt, including source-image references and the refusal cards.
