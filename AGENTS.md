# AGENTS.md

This repository is operated as an agent-friendly Unity project.

Start here:
- Read `ARCHITECTURE.md` for the codebase map.
- Read `docs/README.md` for the living documentation index.
- Read `docs/exec-plans/active/codex-harness-foundation.md` before large repo-shaping work.

Primary goal:
- Keep the project easy for Codex and other agents to read, modify, verify, and recover.

Non-goals:
- Hidden architecture in chat threads.
- One-off fixes without updating the local record.
- Large undocumented refactors.

Working rules:
- Prefer repo-native facts over memory.
- Put durable decisions in versioned markdown.
- Keep edits local and incremental.
- Add or update tests when logic becomes extractable.
- Use scripts and documented commands instead of ad hoc manual steps when the flow will repeat.

Project map:
- `Assets/ShooterSurvival/Scripts/Game`: stage flow, globals, high-level orchestration.
- `Assets/ShooterSurvival/Scripts/Player`: player state and movement.
- `Assets/ShooterSurvival/Scripts/Weapon`: fire loop, bullet logic, weapon stats.
- `Assets/ShooterSurvival/Scripts/Enemy`: enemy behavior and pooling.
- `Assets/ShooterSurvival/Scripts/Wave`: wave definitions, spawners, wave harness utilities.
- `Assets/ShooterSurvival/Scripts/UI and VFX`: runtime UI, time state, effects.
- `Assets/ShooterSurvival/Scripts/Harness`: runtime debug harnesses.
- `Assets/Tests`: edit-mode and other agent-oriented verification.

Key commands:
- `dotnet build Assembly-CSharp.csproj -nologo`
- `dotnet build Assembly-CSharp-Editor.csproj -nologo`
- `powershell -ExecutionPolicy Bypass -File tools/validate-agent-harness.ps1`

Unity editor automation:
- Use the official Unity CLI plus `com.unity.pipeline` as the primary editor-control path. The Codex MCP server name is `unity`.
- Verify the live editor with `unity status --project-path .`, list tools with `unity list --project-path . --detail compact`, and invoke a tool with `unity command --project-path . <tool>`.
- Let the Unity CLI discover the authenticated per-editor Pipeline endpoint. Do not hardcode its transient localhost port in docs or scripts.
- The embedded CoderGamester server remains a temporary fallback under the Codex MCP name `mcp-unity`. Only that legacy bridge uses `ProjectSettings/McpUnitySettings.json` and its Node bridge.
- If the official connection is unavailable, run `unity pipeline list` and `unity status --project-path .`. If only the fallback fails after reload or Play Mode transitions, inspect Unity `Editor.log` for `[MCP Unity]` entries.

Documentation rules:
- Search `docs/solutions/` for documented fixes and workflow patterns when working in related areas; entries are organized by category with YAML frontmatter such as `module`, `problem_type`, and `tags`.
- Put architecture changes in `ARCHITECTURE.md`.
- Put execution status in `docs/exec-plans/`.
- Put quality gaps in `docs/QUALITY_SCORE.md`.
- Put runtime or operational failure modes in `docs/RELIABILITY.md`.
- Put trust boundaries and risky assumptions in `docs/SECURITY.md`.

Generated image previews:
- Do not rely only on the desktop app's inline `Canvas` viewer when presenting generated images.
- Copy preview images that the user may want to inspect into `tmp/image-previews/<topic>/` without overwriting existing files.
- Include clickable absolute PNG links in the final response so the full-resolution files remain accessible even if the inline viewer fails.

Definition of done for repo-shaping work:
- Code change implemented.
- Verification command recorded or run.
- Relevant docs updated.
- New operational sharp edges called out explicitly.
