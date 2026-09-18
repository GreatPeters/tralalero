# Installed Flow shots01 and02

User explicitly requested applying the recommended reviewed Flow clips. `Curse_Opening_Animated.mp4` now contains reviewed scene01 (escape carrying shoe), reviewed scene02 (natural shark becomes cursed shark), and the previous scene03 and14B scene04. The previous whole movie is backed up as `opening-before-flow.mp4`. No further Flow generation or credits were used for this installation.

Contract:626frames,24fps,720×1280,26.083333seconds; scene starts at frames0,192,384,505 (seconds0,8,16,21.041667). `OpeningStoryUI` uses these unequal boundaries for automatic captions, Next and prepare-time seeking. Existing silent runtime audio behavior is retained. Both SR18 and HighWay reference the same preserved movie GUID `140d91eb9a7dda4409a8a48b05dfd8d7`.

The initial concat candidate contained626frames but had inconsistent timestamps (24.04fps/26.04seconds reported). It was rejected and retained as `opening-with-flow.mp4`. The final `opening-with-flow-cfr.mp4` explicitly assigns a1/24 timebase and sequential frame timestamps. All242 old trailing frames are retained in order, scaled from576×1024 to720×1280 and re-encoded; tail PSNR against the equivalently scaled original is41.476dB (minimum39.264). This preserves the prior shots but is not byte-identical video.

## Verification

- Full FFmpeg decode:626frames; Unity synchronous import confirms24fps,26.083333seconds,720×1280,correct scene reference and unchanged GUID.
- `unity command --project-path . run_tests editor OpeningMovieTimingTests`:12passed after final code changes.
- First real Play verification observed page0 playback, natural8-second transition to page1, Next to16seconds/page2, Next to21.04seconds/page3, and automatic close/release at the end (`playback.json`).
- The first gameplay screenshots exposed existing EnvelopeParent cropping on the1080×2340 Game view. Movie playback now uses FitInParent at the clip's true aspect ratio and hides the static artwork behind its letterbox area; the builder uses the same fit mode. The second Play verification passed (`playback-fit.json`), including fit mode, hidden static background, caption transition, both seeks and natural-end cleanup. Both final screenshots under `tmp/image-previews/opening-flow-installed-fit/` were visually inspected: full carried shoe and visible watermark.
- Current preferences were snapshotted before each Play pass. Both passes restored56keys,3007coins/0gems and returned to a clean SR18 Edit Mode. Second pass used `playerprefs-before-fit.tsv`, not an old historical snapshot. No APK rebuild or deployment was performed.

Commands: `tools/prepare-opening-flow-install.py` assembles the candidate; official Unity `eval_file tools/import-opening-flow.cs` imports it; `eval_file tools/verify-opening-flow.cs` runs the playback probe. The preparation refuses to overwrite the accepted candidate. Source hashes and boundaries are in `assembly.json`.
