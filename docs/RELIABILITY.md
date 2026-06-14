# Reliability

## Known Failure Modes
- MCP Unity can fail to restart cleanly after script reloads or play mode transitions.
- Port conflicts can leave the Unity-side server offline even when the editor is open.
- Codex-side MCP calls surface this as `Transport closed`.

## Recovery Notes
- MCP Unity settings are stored in `ProjectSettings/McpUnitySettings.json`.
- If the configured port is occupied, the Unity editor window may show `Server Offline` and `Port ... is already in use`.
- The MCP Node bridge reads the Unity settings file, so a port change there propagates to the bridge on the next connection.

## Current Mitigations
- Delayed retry restart logic was added to `Packages/com.gamelovers.mcp-unity/Editor/UnityBridge/McpUnityServer.cs`.
- MCP Unity now auto-increments `ProjectSettings/McpUnitySettings.json` `Port` once per main Unity editor process launch. It starts from the saved port + 1 and skips occupied ports before the WebSocket server starts; batch-mode AssetImportWorker processes do not change the setting.
- Combat runtime harness bootstraps itself after scene load in editor or development contexts.

## Verification
- Check Unity `Editor.log` for `[MCP Unity] WebSocket server started successfully`.
- Confirm the port in `ProjectSettings/McpUnitySettings.json`.
- Use `tools/validate-agent-harness.ps1` to sanity-check repo-side harness prerequisites.
