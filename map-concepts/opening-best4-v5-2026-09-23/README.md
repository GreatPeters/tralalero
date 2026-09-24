# Best4 v5 opening replacement — 2026-09-23

Replaced all four shared opening shots with the user-supplied files from `C:/Users/ljh/Downloads/Tralalero_Shooter_Best4_v5`. This replaces the September12 Flow/tail combination. Existing captions, chrome and chapter-entry transition movies remain separate.

| Source | Duration | Combined start | Frames |
|---|---:|---:|---:|
| 01_Stolen_Shoe.mp4 | 8s | 0s | 192 |
| 02_The_Curse.mp4 | 12s | 8s | 288 |
| 03_The_Condition.mp4 | 10s | 20s | 240 |
| 04_To_Noryangjin.mp4 | 9s | 30s | 216 |

Installed asset: `Assets/JH/UI/Opening/Animated/Curse_Opening_Animated.mp4`, 720×1280,24fps,936frames,39seconds. All sources are silent. Stream-copy concatenation retains the source video quality; every decoded frame hash matches the four ordered source streams. See `assembly.json` for hashes. Previous movie is retained as `opening-before-best4-v5.mp4`; supplied sources are retained in `sources/`.

The existing VideoClip GUID `140d91eb9a7dda4409a8a48b05dfd8d7` was preserved. Native import checked the movie and four indicators in Noryangjin, HighWay and RestStop (`import.json`). No scene serialization changes were required. `OpeningStoryUI` boundaries and its existing timing tests now use0/8/20/30seconds.

Validation:

- `tools/prepare-opening-best4.py`:936decoded frames identical, ordered source counts192/288/240/216.
- `InstallOpeningBest4.Main`: native import dimensions/frame count/GUID and all three scene bindings passed.
- `OpeningMovieTimingTests`:12/12native Edit Mode tests passed.
- `dotnet build Assembly-CSharp.csproj -nologo --no-restore -v quiet`: passed; one existing `SplineSpeed.m_LastIndex` CS0649warning.
- `VerifyOpeningBest4.Main`:39-second natural playback, all four captions/indicators, all Next/Previous boundaries, first-page Previous disabled, natural-end/Skip cleanup, reopen after Skip, and gameplay blocking passed (`playback.json`).
- Native1080×2340captures are in `tmp/image-previews/opening-best4-v5-2026-09-23/native-01.png` through `native-04.png`. The established `CoastalStoryLayout` maintains native aspect and its maximum15%crop contract. The first verifier incorrectly assumed the older full-frame-only contract; that assertion was corrected without changing the layout (`obsolete-fit-assertion.txt`).
- Original77user preferences were restored, including coin1781/jewel0; original1080×2340Game view, nonmaximized layout,60fpscap and HighWay Edit Mode scene restored. Balance workbook hash remains unchanged. The interrupted balance run98 is excluded; balance calibration remains paused in its active execution plan.

Reproduction tools: `tools/prepare-opening-best4.py`, `tools/install-opening-best4.cs`, `tools/verify-opening-best4.cs`. The preparation script refuses to overwrite an existing assembled candidate. Do not rerun older Flow installation tools to apply this replacement.
