# Flow correction — reviewed scenes 01 and 02; scene03 blocked

The current reviewed deliverables are in `reviewed/`:

- `01_Escape_With_Shoe.mp4`: the natural shark carries the gold sneaker in its mouth while leaving the sunny empty shrine. Original Flow video, media editor ID `57f768d3-7e00-44e3-9b3c-31884d502f40`. Nine sampled frames from0 through7.9seconds show the carried shoe, continuous body/tail action and receding shrine; no shoe explosion. The nose approaches the left frame edge near the end.
- `02_Natural_Shark_Transforms.mp4`: begins as the ordinary legless shark and transforms into the blue cursed character with three shoe-wearing legs. Source Flow video editor ID `81212fda-2c97-4c05-badd-d03eca7437d8`. Its raw ending briefly loses a third foot around7seconds before restoring it at the target endpoint. The reviewed version retains source frames0–143 (first6seconds) and plays that continuous animated segment at75% speed for8seconds. It preserves the full frame and mandatory watermark. See `reviewed/02-edit.json` and `tools/finish-flow-curse-clip.py` for the exact edit.

Both reviewed MP4s fully decode:720×1280,24fps,192frames,8seconds,with audio. Source and reviewed files are preserved separately. The transformation still has stylized magical overlap and a loose shoe effect during the transformation; this is not a claim of perfect frame-by-frame anatomy. After the transformation, inspected6-second and final reviewed frames retain three planted feet. Audio decodes and was retimed consistently, but has not undergone a separate listening/content review.

Scene03 (`Shark nods to stone deity`) remains incomplete: revision2's request was refused for third-party content provider interests, explicitly with no charge. No refusal-bypassing alternate model or blind retry was submitted. Scene04 was outside this remake request. The game opening asset has not been replaced.

## Reference and recovery record

The user-specified four-panel storyboard informed identity and action. Built-in imagegen prepared the two corrected first frames in `../revision-2/`; existing Story02 supplies the transformed endpoint. Movies were generated in the user's logged-in Google Flow project, one-at-a-time after a batch selected incorrect sources.

Explicit image attachment fixed scene01's source selection. Supplying image editor UUIDs directly to scene02's interpolation initially failed with media-not-found: artifact IDs and generation media IDs differ. Flow then resolved the attachments to start `72efcbf4-73c6-4ab0-8a40-36bdf4486a05` and end `b3a4722a-afb9-4e28-8815-da04375d0570`. The actual raw starting frame was visually verified after download.

Each generation proposal displayed100credits. Google One's visible Flow activity ledger was inspected after completion: six100-credit debits at19:08:52,19:08:53,19:08:54,19:16:55,19:22:36 and19:27:26 KST, and one100-credit refund at19:09:57. Net500credits debited this turn, balance350. The provider-refused scene03 refund is visible. The19:22:36 media-not-found attempt still has a100-credit debit with no matching refund, despite its no-charge tile. This discrepancy is unresolved; no additional generation or credit purchase was submitted.

Both reviewed MP4s were successfully uploaded into the same Flow project, under `01_Escape_With_Shoe` and `02_Natural_Shark_Transforms`. The reviewed scene02 editor URL ends in `e51d7be7-b996-4081-991a-c141951c30cc`; this is the locally trimmed/retimed version, not the flawed raw ending.

## Verification

```powershell
python tools/finish-flow-curse-clip.py
python tools/validate-flow-opening.py --folder map-concepts/flow-opening-2026-09-12/revision-3/reviewed --previews tmp/image-previews/flow-opening-reviewed --seconds 0 2 4 6 7.9
```

The finishing script refuses to overwrite an existing result. `reviewed/validation.json` records complete-decode frame counts, dimensions, file sizes and hashes. Full-resolution review frames are under `tmp/image-previews/flow-opening-reviewed/`; the raw nine-frame inspection for each clip is under `tmp/image-previews/flow-opening-revision-3/`.
