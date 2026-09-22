# AGENTS.md

<!-- MaxyMCP Unity MCP managed project skills -->
<!-- MaxyMCP Unity MCP project skill versions: unity-mcp-workflow@1.0.5, unity-ui-composition@1.0.6 -->

# MaxyMCP Unity MCP Project Guidance

This section is managed by MaxyMCP MCP for Unity. Everything between the begin and end markers is regenerated on each sync; edit outside this block.

## Installed project skills

- `maxymcp-unity-mcp-workflow` v1.0.5 - Efficient workflow for using Unity MCP to edit, import, compile, inspect, and test Unity projects, including screenshot and Game View recording verification.
- `maxymcp-unity-ui-composition` v1.0.6 - Build and revise responsive Unity uGUI mobile interfaces, including portrait and landscape layouts, safe areas, prefabs, auto layout, scrolling, text, input, animation, and performance validation.

## Agent workflow rules

- Prefer project-local MaxyMCP skills: `.codex/skills/` for Codex, `.opencode/skills/` for OpenCode, `.dsh/skills/` for DeepSeek Harness, `.agents/skills/` for Antigravity.
- Prefer structured MCP tools; use `execute_code` for project-specific gaps. For new snippets, include `using MaxyMCP.Editor.Tools.Scripting;`, implement `IMaxyMCPCommand`, and use `ctx.RegisterObjectCreation` / `RegisterObjectModification` / `DestroyObject` so changes participate in Undo automatically.
- Confirm the Unity project root, active scene, and real object/prefab/asset path before edits. Treat user-provided object names as hints, not paths.
- Inspect Unity objects through MCP before changing user-named scene or prefab targets. Carry the returned `instanceId` into follow-up calls (`find_method=by_id`) instead of re-resolving by name.
- Tool returns are structured JSON (`{success, message, data}` / `{success: false, code, error, data}`). Branch on `code`, not free-form text.
- Set component fields with `set_component_property(ies)` — it picks up `[SerializeField] private` fields and accepts Object references as `{"fileID": <instanceId>}` or `{"assetPath": "Assets/..."}`.
- For field-only prefab asset edits, use `set_prefab_property(ies)` with a verified `Assets/**/*.prefab` path. If it reports duplicate paths or components, retry only with an index from that response; use Prefab Mode for structural edits.
- Read editor state through dedicated tools (`get_selection`, `get_prefab_stage`, `get_tags`, `get_layers`, `get_build_settings`); use `execute_menu_item` before falling back to ad-hoc `execute_code`.
- Never edit `.unity`, `.prefab`, or `.asset` files with shell text tools or patches; use Unity MCP / Editor APIs for scenes, prefabs, and ScriptableObject assets.
- Save only the scene or prefab assets intentionally modified, then read back exact values.
- Prefer structured query/audit/edit tools in Core; low-frequency configuration and preview management remain in Full. Use `get_tool_capabilities` to check exposure; respect customized allowlists.
- `execute_code` refreshes by default; use skip_refresh only after verifying the intended assemblies are ready.
- After any external edit to C# source files, do not wait for the Unity window to refresh by itself: if Unity is in Play Mode, call `exit_play_mode` first, then call `request_recompile`, `wait_for_compilation`, and `get_compilation_errors` before relying on the changed code.
- In `execute_code`, null-guard every lookup and return explicit missing path/object/component messages; do not run self-healing fallback loops.
- For Unity object references, do not use `??=` for lazy rebinding; use explicit `if (field == null) field = Resolve();`.
- After edits use `prepare_editor` with the intended mode and a unique request_key; use the returned task_id with `get_task`, wait_seconds and after_revision until operation.status=ready and current_editor confirms readiness and mode, then check console errors. Honor poll_after_ms on unchanged results.
- `request_recompile` is rejected while Unity is in Play Mode. Call `exit_play_mode` first, then retry.
- On disconnect, retry the same operation status read by ID/key. Historical reload recovery or HTTP reachability alone does not establish current readiness.
- An interrupted arbitrary mutation has unknown outcome until exact object/asset readback confirms it; never automatically replay it.
- Avoid changing `Library/`, `Temp/`, `Logs/`, or `obj/`.

## Project

- Project root: `D:\Unity_Workspace\MaxyToolKit`
- Product name: `MaxyToolKit`

## Notes

- Re-run `MaxyMCP > Project Skills` after changing selected skills or platforms.
<!-- /MaxyMCP Unity MCP managed project skills -->

## 本地开发补充

- 通过外部编辑器或文件工具修改Assets下的C#、asmdef、shader或其他可导入文件后，必须调用`request_recompile`触发AssetDatabase刷新并等待Unity重新编译
- 如果只需要确认编译状态，可调用`wait_for_compilation`；编译完成后再调用`get_compilation_errors`确认没有错误或警告
- 以上工具已加入Core默认配置，Main和Full配置也应保持可用
