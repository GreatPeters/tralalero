# Security

## Trust Boundaries
- Unity editor state is mutable and should be treated as an external runtime boundary.
- MCP Unity can execute scene, asset, and editor operations. Treat it as privileged.
- Local scripts and docs are authoritative only when committed and versioned.

## Current Assumptions
- Work is performed on a local developer machine, not an untrusted shared editor host.
- `ProjectSettings/McpUnitySettings.json` is the source of truth for MCP Unity connection settings.
- Runtime debug harnesses are intended for editor or development builds, not production player builds.

## Guardrails
- Do not hardcode ports or environment-sensitive settings in multiple places.
- Prefer explicit recovery instructions over silent fallback behavior.
- Keep privileged editor operations behind documented tools and menu entries where possible.
