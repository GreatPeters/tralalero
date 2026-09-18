---
status: active
date: 2026-09-12
---

# Startup performance

User authorized fixing startup delay. Device/stage clarification is pending. Local SR18 Editor measurement isolated inflated workbook styles as the dominant delay; the fix is installed and measured.

- Done: video on/off comparisons, independent reader timings, style-only compaction with all-data/all-format preservation, runtime protected archive regeneration, four graft-writer fixes,3Python tests, actual optimized Play observation and preference/profiler/autoplay restoration.
- Measured: profiled video-on first frame18.078→2.156seconds; Data.xlsx975,856→66,220bytes; styles27,274,208→10,079bytes.
- Pending: Unity15-case GameDataWorkbookTests started but returned no result and timed out. Editor Pipeline main-thread calls are blocked; unsaved-scene title suggests a save dialog, not visually confirmed. User was asked whether that dialog is open and to save the applied data state. Do not kill/restart the user's Editor or claim these tests passed. After UI unblocks, confirm test-run cleanup, save the scene within the authorized data changes, run the targeted tests and inspect restoration.
- No phone/APK timing or new build/deployment. See `map-concepts/startup-performance-2026-09-12/README.md` for exact artifacts and commands.
