# Revision 2 — rejected review candidates

Three 8-second Veo 3.1 Quality generations were requested, at 100 credits each, with one-time approval. Scenes 01 and 02 generated; scene 03 was refused for third-party content provider interests and explicitly not charged. This revision is **not accepted**.

| File / request | Outcome |
|---|---|
| `01_Escape_With_Shoe.mp4` | Carries the gold shoe while escaping, but uses the wrong purple-deity starting image instead of the sunny shrine. Flow media `e575517b-53ed-4465-a2c3-252709cb95b8`. |
| `02_Natural_Shark_Transforms.mp4` | Uses the sunny escape image as its start instead of the purple-deity before-curse image. A three-shoe intermediate appears, but the endpoint breaks into distorted body/loose shoes rather than retaining the target character. Flow media `f2b35f9a-8830-4a08-89c8-60acac79dda5`. |
| `R2_03_Condition_Three_Feet` / Flow title `Shark nods to stone deity` | Provider refusal; no completed movie and no charge. No refusal retry was submitted. |

Both downloaded MP4 files decode completely: 720×1280, 192 frames at24fps, 8 seconds, audio stream present. See `validation.json` and per-file decode logs. Frames at1,4,7.9seconds were visually inspected and expose the source-frame mismatch. Successful decoding does not mean the storyboard is correct. Audio was not separately reviewed by listening.

Flow's natural-language plan named the correct input files, but actual output first frames and the scene02 editor source thumbnail establish different source selection. Recover by attaching references explicitly in the prompt and using the verified media IDs below; avoid another filename-only multi-scene batch.

Verified source image editor URLs:

- Escape first frame `R2_01_Escape_Shoe_In_Mouth.png`: `d034d91d-06e8-465a-96c4-2d73bc7d0686`.
- Before-curse natural shark `R2_02_Before_Curse_Natural_Shark.png`: `11fff83c-e711-49cb-b30a-4705ab5728c1`.
- Cursed three-foot endpoint `Story_02.png`: `a69e0f3c-d70c-4710-acb0-2458cba3473e`.

The new beginning images were generated with built-in imagegen, visually reviewed, copied into this folder and into `tmp/image-previews/flow-opening-revision-2/`. Video generation was through the user's logged-in Google Flow account. The game movie has not been replaced.
