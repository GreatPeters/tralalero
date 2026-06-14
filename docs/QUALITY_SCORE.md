# Quality Score

## Snapshot
- Agent readability: C
- Runtime verification: C+
- Pure logic testability: D+
- Documentation coverage: D
- Operational recovery: C-

## Why
- Core gameplay entry points are discoverable, but repo-level operating context was mostly absent.
- Runtime combat harness exists, but only a small amount of pure logic is testable outside scenes.
- MCP Unity is usable, but server restart behavior has been fragile around reloads and play mode transitions.

## Next Improvements
- Extract score, damage, and progression calculations into pure helpers.
- Add more edit-mode tests around wave progression and state transitions.
- Reduce duplicated run-state ownership across `GameManager`, `CanvasScript`, and `TimeManager`.
- Keep top-level docs current when workflow or port configuration changes.
