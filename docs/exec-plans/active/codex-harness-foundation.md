# Codex Harness Foundation

## Goal
Turn this Unity repo into a usable agent-first workspace by adding durable maps, operating rules, and repeatable verification.

## Completed
- Added root repo map in `AGENTS.md`.
- Added architecture summary in `ARCHITECTURE.md`.
- Added docs index and baseline operational docs.
- Added `tools/validate-agent-harness.ps1`.
- Added runtime combat harness and initial wave utility tests.
- Installed the official Unity CLI/Pipeline editor-control path, registered it in Codex as `unity`, and verified scene reads plus Play Mode enter/exit recovery.
- Retired the CoderGamester `mcp-unity` fallback, its embedded package, and its project-level port settings after the official Pipeline path proved stable.

## In Progress
- Expand pure-logic extraction so more gameplay rules are testable without a scene.

## Next
- Add focused tests around run-state transitions and wave completion.
- Normalize ownership of game start, stop, and reset behavior.
- Add a small set of documented verification scripts for common gameplay loops.

## Notes
- The official Unity CLI discovers the authenticated Pipeline endpoint for this project; its localhost port is transient and must not be hardcoded.
- `com.unity.pipeline` is the only supported Codex editor-control package; the repository has no CoderGamester MCP Unity port configuration.
- The separate `com.youngwoocho02.unity-cli-connector` package remains for pre-existing localhost HTTP workflows. It is not registered in Codex and was outside the CoderGamester removal scope.
