# Five original mystery BGM candidates

| Track | Direction | Listen |
|---|---|---|
| 01 Moonlit Aquarium | Celesta, felt piano, underwater mystery;78 BPM prompt | [MP3](previews/01_moonlit_aquarium-loop.mp3) |
| 02 Lantern Market | Flute, plucked zither, wooden percussion;92 BPM prompt | [MP3](previews/02_lantern_market-loop.mp3) |
| 03 Starlit Highway | Analog arpeggios, electric piano, pulsing bass;104 BPM prompt | [MP3](previews/03_starlit_highway-loop.mp3) |
| 04 Clockwork Rest Stop | Music box, vibraphone, ticking percussion;86 BPM prompt | [MP3](previews/04_clockwork_rest_stop-loop.mp3) |
| 05 Tidal Observatory | Harp, flute, glass bells and spacious strings;72 BPM prompt | [MP3](previews/05_tidal_observatory-loop.mp3) |

The BPM/instruments describe generation prompts, not measured transcription of the resulting performances. Each raw stereo WAV is64seconds at44.1kHz. A2-second overlap produces a62-second loop. WAV masters, OGG game-ready alternatives and160kbps MP3 previews are all in `previews/`.

Generated locally using installed Stable Audio3 Small-Music / SAME-S, fp32,8steps, CFG1.5,8CPUthreads, seeds26091421–25. No reference recording or remote generation API was used. Requests, model settings, logs and source WAVs are in `candidates/`; `signal-report.json` records peak0.68, RMS levels and adjacent-sample seam deltas. These signal checks are not subjective listening scores. No candidate replaced `Resources/Audio/Mobile/music.ogg`.

Reproduction: `tools/generate-mystery-bgm.py`, followed by `tools/process-mystery-bgm.py` with the cached Codex Python containing numpy and installed FFmpeg. Generation refuses to overwrite prior outputs. The first sandbox attempt could not launch the installed engine; the authorized elevated execution completed all five tracks in approximately38–47seconds each.

Provenance follows the installed [Stable Audio3 project](https://github.com/Stability-AI/stable-audio-3) and its model's [Stability AI Community License](https://stability.ai/license). Code license and model terms are separate; these outputs are not labeled CC0. No model weights are included in the game.
