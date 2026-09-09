---
status: completed
date: 2026-09-10
---

# SR18 contact hazards and paired enemies

Implement the user's three requested corrections on map2 without discarding existing changes.

1. Make SR18's authored lamp hazards durable against auto-fire while retaining50 contact damage; retain explicitly decorative legacy lamps.
2. Center all three FatMan ambush start/movement paths on their actual road and validate road support and visible body placement.
3. Add two side-by-side Normal formations at existing E02/E06 stations. Keep independent enemy damage/death/drop behavior. Add one companion and its own workbook row/activation per station; both living bodies cover every permitted player lane, while killing one opens its side.
4. Replace the1-second wall cooldown and zero-time sentinel with a shared2-second cooldown across fixed choices and drops. Block before claiming/consuming a pair and clear on restart.
5. Run focused regressions, workbook preservation checks, real GameView/input checks and sequential builds. Restore test currency/speed and record evidence/limits.

No new stage transition, permanent upgrades, shared enemy-prefab resizing or changes to unrelated workbook sheets are planned.
