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
- The official Unity CLI/Pipeline path is the supported Codex editor-control route and survives Play Mode transitions; removing the CoderGamester bridge eliminates that bridge's reload and port-reuse failures.
- A distinct pre-existing localhost HTTP package, `com.youngwoocho02.unity-cli-connector`, remains installed outside Codex registration and should be evaluated separately rather than conflated with the removed MCP bridge.

## Next Improvements
- Extract score, damage, and progression calculations into pure helpers.
- Add more edit-mode tests around wave progression and state transitions.
- Reduce duplicated run-state ownership across `GameManager`, `CanvasScript`, and `TimeManager`.
- Keep top-level docs current when the Unity CLI or Pipeline workflow changes.
