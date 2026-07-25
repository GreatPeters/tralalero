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

## Noryangjin Scene Gameplay Composition

- Run `Tools/MeshyAI/노량진 게임플레이/Forward 기능 연결` only while `Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity` is the active scene and Play Mode is stopped. The installer rejects any other target.
- The installer temporarily opens `Forward March Mode` and clones its configured scene instances. This preserves serialized references that can be lost when rebuilding the player, UI, or services from unrelated bare prefabs.
- The map scene's `Original` must remain the visible character under `Noryangjin_Player`; the cloned Forward renderers should remain disabled.
- `TimeManager`, `SettingsManager`, and `BulletPooler` are scene-local dependencies. A partial or duplicate Managers setup is unsafe because singleton ownership and stale cross-scene references can conflict; the installer validates the complete set instead of silently creating a second partial set. Do not make the entire Managers root persistent across scene loads.
- A successful install keeps enabled Build Settings entries in this order: `Forward March Mode` at index `0`, then `Noryangjin_MapTool_Mode` at index `1`.
- Before accepting a changed scene, verify there is one player rig and one of each required service, the Original renderer and camera are active, the pre-start/shop UI appears, Start enables movement and attack, and a turn spot produces pause → rotation → route-relative resume.

## Verification
- Check Unity `Editor.log` for `[MCP Unity] WebSocket server started successfully`.
- Confirm the port in `ProjectSettings/McpUnitySettings.json`.
- Use `tools/validate-agent-harness.ps1` to sanity-check repo-side harness prerequisites.
