# Codex Harness Foundation

## Goal
Turn this Unity repo into a usable agent-first workspace by adding durable maps, operating rules, and repeatable verification.

## Completed
- Added root repo map in `AGENTS.md`.
- Added architecture summary in `ARCHITECTURE.md`.
- Added docs index and baseline operational docs.
- Added `tools/validate-agent-harness.ps1`.
- Added runtime combat harness and initial wave utility tests.
- Added delayed MCP Unity restart retries after assembly reload and edit-mode re-entry.
- Installed the official Unity CLI/Pipeline editor-control path, registered it in Codex as `unity`, and verified scene reads plus Play Mode enter/exit recovery while retaining the CoderGamester `mcp-unity` bridge as a fallback.

## In Progress
- Expand pure-logic extraction so more gameplay rules are testable without a scene.

## Next
- Add focused tests around run-state transitions and wave completion.
- Normalize ownership of game start, stop, and reset behavior.
- Add a small set of documented verification scripts for common gameplay loops.

## Notes
- The official Unity CLI discovers the authenticated Pipeline endpoint for this project; its localhost port is transient and must not be hardcoded.
- `ProjectSettings/McpUnitySettings.json` remains authoritative only for the legacy CoderGamester fallback.
