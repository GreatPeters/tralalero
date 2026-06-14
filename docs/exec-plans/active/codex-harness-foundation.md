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

## In Progress
- Expand pure-logic extraction so more gameplay rules are testable without a scene.

## Next
- Add focused tests around run-state transitions and wave completion.
- Normalize ownership of game start, stop, and reset behavior.
- Add a small set of documented verification scripts for common gameplay loops.

## Notes
- The current MCP Unity port is project-driven via `ProjectSettings/McpUnitySettings.json`.
- If the port changes, docs should describe the fact that the project setting is authoritative rather than duplicating the value.
