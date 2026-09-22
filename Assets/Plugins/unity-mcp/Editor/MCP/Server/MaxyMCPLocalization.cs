// 版权归 MaxyMCP 所有，遵循 MIT 许可证。

using System;
using System.Collections.Generic;
using System.Linq;

namespace MaxyMCP.Editor.MCP.Server
{
    /// <summary>
    /// MCP 编辑器界面中文文本表。
    /// MCP 协议标识、工具名称、包名称、配置键和发送给 AI 的内容必须保持英文；
    /// 只有面向用户的编辑器文字在这里使用正式中文显示。
    /// </summary>
    internal static class MaxyMCPLocalization
    {
        private static readonly Dictionary<string, string> Chinese = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["MCP Server"] = "MCP服务器",
            ["Project Skills"] = "项目技能",
            ["Tool Exposure"] = "工具暴露",
            ["MCP Settings"] = "MCP 设置",
            ["Failed to initialize services."] = "服务初始化失败。",
            ["Stopped"] = "已停止",
            ["One-Click MCP Configuration"] = "一键配置 MCP",
            ["Configure"] = "配置",
            ["Configure + Skills"] = "配置 + 技能",
            ["Refresh"] = "刷新",
            ["Apply Skills"] = "应用技能",
            ["Upgrade Skills"] = "升级技能",
            ["Current Platform"] = "当前平台",
            ["Enable skills for current platform"] = "为当前平台启用技能",
            ["Built-in Skills"] = "内置技能",
            ["Optional Skills"] = "可选技能",
            ["Installed Files"] = "已安装文件",
            ["Configure project-level skills for supported AI clients. Built-in skills are always installed. Optional skills will be added after verification."] = "为支持的 AI 客户端配置项目级技能。内置技能始终会安装，可选技能将在验证后添加。",
            ["Configure + Skills also installs the project MCP workflow skill."] = "“配置 + 技能”还会安装项目 MCP 工作流技能。",
            ["Project skills are currently available for Claude Code, Cursor, Codex, OpenCode, DeepSeek Harness, and Antigravity."] = "项目技能目前支持 Claude Code、Cursor、Codex、OpenCode、DeepSeek Harness 和 Antigravity。",
            ["No optional skills are available yet. Additional skills will be added after verification."] = "目前没有可用的可选技能，后续验证通过后会添加。",
            ["Uncheck optional skills and click Apply Skills to remove them. Built-in skills cannot be removed."] = "取消勾选可选技能并点击“应用技能”即可移除。内置技能不能移除。",
            ["MCP tool exposure profiles"] = "MCP 工具暴露配置",
            ["Profile"] = "配置档",
            ["Save"] = "保存",
            ["Reset"] = "重置",
            ["Edit Tool List"] = "编辑工具列表",
            ["Select All"] = "全选",
            ["Use Default"] = "使用默认",
            ["Select"] = "选择",
            ["Active"] = "当前",
            ["Editing"] = "编辑中",
            ["tools"] = "工具",
            ["custom"] = "自定义",
            ["default"] = "默认",
            ["Server running on"] = "服务器运行于",
            ["Server stopped"] = "服务器已停止",
            ["Edit exactly which tools each MCP profile exposes. Choose the active profile from the MCP Server window. Saving changes restarts the running server automatically."] = "精确编辑每个 MCP 配置档暴露的工具。请在 MCP服务器窗口选择当前配置档。保存更改后，运行中的服务器会自动重启。",
            ["core defaults to the focused Unity workflow tool set. Select tools below and click Save to override that list."] = "核心配置默认使用精简的 Unity 工作流工具集。选择下方工具并点击“保存”即可覆盖默认列表。",
            ["main uses your selected primary tool set. Select tools below and click Save to override that list."] = "主要配置使用你选定的工具集合。选择下方工具并点击“保存”即可覆盖主要工具列表。",
            ["full defaults to every registered MCP tool. Select tools below and click Save to make full expose only that custom list."] = "完整配置默认包含所有已注册的 MCP 工具。选择下方工具并点击“保存”，即可让完整配置只暴露自定义列表。",
            ["core defaults to the focused Unity workflow tool set. Select tools below and click a checkbox to apply immediately."] = "核心配置默认使用精简的 Unity 工作流工具集。勾选下方复选框即可立即应用。",
            ["full defaults to every registered MCP tool. Select tools below and click a checkbox to apply immediately."] = "完整配置默认包含所有已注册的 MCP 工具。勾选下方复选框即可立即应用。",
            ["Core"] = "核心",
            ["Main"] = "主要",
            ["Full"] = "完整",
            ["Server Status"] = "服务器状态",
            ["Server Port"] = "服务器端口",
            ["Enable MCP Server"] = "启用 MCP 服务器",
            ["Transport Mode"] = "传输模式",
            ["Direct HTTP"] = "直接 HTTP",
            ["Experimental Broker Mode"] = "实验性代理模式",
            ["Recent Activity"] = "最近活动",
            ["Clear"] = "清空",
            ["Project-level settings for the MaxyMCP Unity MCP plugin. Preferences are saved per project."] = "MaxyMCP Unity MCP 插件的项目级设置。偏好设置按项目保存。",
            ["Debug"] = "调试",
            ["Enable debug logging"] = "启用调试日志",
            ["Expand all entries by default"] = "默认展开所有条目",
            ["Running on"] = "运行于",
            ["Attached to existing server on"] = "已连接到现有服务器：",
            ["Status: Unsupported current platform"] = "状态：当前平台不支持",
            ["Status: Not configured for"] = "状态：尚未配置",
            ["Status: Configured for"] = "状态：已配置",
            ["Built-in"] = "内置",
            ["Optional installed"] = "已安装可选",
            ["Skills"] = "技能",
            ["Updates available"] = "有可用更新",
            ["Up to date"] = "已是最新",
            ["Manifest"] = "清单",
            ["Manifest will be created at"] = "清单将创建于",
            ["Versioned files for"] = "版本文件：",
            ["Generated files for"] = "生成文件：",
            ["Generated paths for"] = "生成路径：",
            ["none"] = "无",
            ["OK"] = "正常",
            ["Missing"] = "缺失",
            ["Conflict"] = "冲突",
            ["Update"] = "更新",
            ["expected"] = "预期版本",
            ["not MaxyMCP-managed, expected"] = "非 MaxyMCP 管理，预期版本",
            ["Unknown skill file status"] = "未知技能文件状态",
            ["Regenerate MaxyMCP-managed project skills with the versions bundled in this package."] = "使用此插件内置的版本重新生成 MaxyMCP 管理的项目技能。",
            ["Installed MaxyMCP-managed project skills are already up to date for the selected platform."] = "当前选定平台的 MaxyMCP 项目技能已经是最新版本。",
            ["Required"] = "必需",
            ["Unity MCP Workflow"] = "Unity MCP 工作流",
            ["Efficient workflow for using Unity MCP to edit, import, compile, inspect, and test Unity projects, including screenshot and Game View recording verification."] = "用于编辑、导入、编译、检查和测试 Unity 项目的高效 MCP 工作流，包括截图和游戏视图录制验证。",
            ["Unity UI Composition"] = "Unity 界面组合",
            ["Build and revise responsive Unity uGUI mobile interfaces, including portrait and landscape layouts, safe areas, prefabs, auto layout, scrolling, text, input, animation, and performance validation."] = "构建和修改响应式 Unity uGUI 移动界面，包括竖屏与横屏布局、安全区域、预制体、自动布局、滚动、文本、输入、动画和性能验证。",
            ["Use Per-Project Port"] = "使用按项目端口",
            ["Pin Current Port"] = "固定当前端口",
            ["Transport: Direct HTTP."] = "传输：直接 HTTP。",
            ["Safety"] = "安全",
            ["Default execute_code safety checks"] = "默认启用代码执行安全检查",
            ["Strict filesystem guard"] = "严格文件系统防护",
            ["Auto-inject project namespaces"] = "自动注入项目命名空间",
            ["Default for execute_code calls when safety_checks is omitted. Explicit safety_checks=false can still bypass this for trusted local calls."] = "当未提供safety_checks参数时，作为execute_code的默认安全检查设置。对于可信的本地调用，仍可显式传入safety_checks=false绕过检查。",
            ["Adds checks for broad System.IO file writes, raw file streams, and absolute/user/system/traversal paths. This is a defensive guard, not a complete sandbox."] = "增加对宽泛System.IO文件写入、原始文件流、绝对路径、用户目录、系统目录和路径穿越的检查。这是防御性保护，不是完整沙箱。",
            ["Off by default. When enabled, only namespaces from loaded Library/ScriptAssemblies assemblies are injected; explicit using directives remain the least ambiguous option."] = "默认关闭。启用后，仅从已加载的Library/ScriptAssemblies程序集注入命名空间；显式写using指令仍然最清晰、最不容易产生歧义。",
            ["On: expand all entries. Off: collapse history and expand only the latest entry. Applies immediately; manually expanded or collapsed entries keep your choice until the panel is reopened."] = "开启：展开所有条目。关闭：折叠历史记录，仅展开最新条目。设置立即生效；在重新打开面板前，手动展开或折叠的条目会保留当前状态。",
            ["Debug logging is enabled. Plugin lifecycle, MCP request, transport, and tool execution traces are written to the Unity Console."] = "已启用调试日志。插件生命周期、MCP请求、传输过程和工具执行跟踪信息会写入Unity控制台。",
            ["Debug logging is disabled. Warnings and errors are still written to the Unity Console."] = "已禁用调试日志。警告和错误仍会写入Unity控制台。",
        };

        private static readonly Dictionary<string, string> ToolTranslations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["assign_animator"] = "绑定 Animator",
            ["create_animation_clip"] = "创建动画片段",
            ["create_animator_controller"] = "创建 Animator 控制器",
            ["get_animator_state"] = "读取 Animator 状态",
            ["play_animator_state"] = "播放 Animator 状态",
            ["set_animator_parameter"] = "设置 Animator 参数",
            ["assign_material"] = "绑定材质",
            ["copy_asset"] = "复制资源",
            ["create_material"] = "创建材质",
            ["delete_asset"] = "删除资源",
            ["find_assets"] = "查找资源",
            ["rename_asset"] = "重命名资源",
            ["get_asset_import_settings"] = "读取资源导入设置",
            ["set_asset_import_settings"] = "设置资源导入设置",
            ["create_game_object"] = "创建游戏对象",
            ["create_primitive"] = "创建基础物体",
            ["delete_game_object"] = "删除游戏对象",
            ["duplicate_game_object"] = "复制游戏对象",
            ["find_game_objects"] = "查找游戏对象",
            ["get_game_object_info"] = "读取游戏对象信息",
            ["rename_game_object"] = "重命名游戏对象",
            ["set_parent"] = "设置父对象",
            ["set_transform"] = "设置 Transform",
            ["add_component"] = "添加组件",
            ["get_component_properties"] = "读取组件属性",
            ["list_components"] = "列出组件",
            ["set_component_property"] = "设置组件属性",
            ["set_component_properties"] = "批量设置组件属性",
            ["get_hierarchy"] = "读取层级",
            ["get_scene_info"] = "读取场景信息",
            ["create_new_scene"] = "创建新场景",
            ["open_scene"] = "打开场景",
            ["save_scene"] = "保存场景",
            ["save_all_scenes"] = "保存所有场景",
            ["enter_play_mode"] = "进入运行模式",
            ["exit_play_mode"] = "退出运行模式",
            ["capture_game_view"] = "截取游戏视图",
            ["capture_scene_view"] = "截取场景视图",
            ["capture_editor_window"] = "截取编辑器窗口",
            ["simulate_mouse_click"] = "模拟鼠标点击",
            ["simulate_key_press"] = "模拟按键",
            ["simulate_ui_scroll"] = "模拟界面滚动",
            ["get_console_logs"] = "读取控制台日志",
            ["get_compilation_errors"] = "读取编译错误",
            ["execute_code"] = "执行 C# 代码",
            ["execute_menu_item"] = "执行菜单项",
            ["run_tests"] = "运行测试",
            ["get_editor_state"] = "读取编辑器状态",
            ["get_selection"] = "读取当前选择",
            ["set_selection"] = "设置当前选择",
            ["create_prefab"] = "创建预制体",
            ["open_prefab_stage"] = "打开预制体编辑模式",
            ["set_prefab_property"] = "设置预制体属性",
            ["set_prefab_properties"] = "批量设置预制体属性",
            ["create_scriptable_object"] = "创建 ScriptableObject",
            ["get_project_settings"] = "读取项目设置",
            ["list_packages"] = "列出项目包",
            ["install_package"] = "安装项目包",
            ["remove_package"] = "移除项目包",
            ["get_performance_snapshot"] = "读取性能快照",
            ["analyze_scene_complexity"] = "分析场景复杂度",
            ["record_game_view"] = "录制游戏视图",
            ["get_tool_capabilities"] = "读取工具能力",
            ["validate_menu_item"] = "验证菜单项",
            ["get_mesh_info"] = "读取网格信息",
            ["create_script"] = "创建脚本",
            ["edit_script"] = "编辑脚本",
            ["patch_script"] = "修补脚本",
            ["request_recompile"] = "请求重新编译",
            ["wait_for_compilation"] = "等待编译完成",
            ["get_reload_recovery_status"] = "读取重载恢复状态",
            ["add_component_to_many"] = "批量添加组件",
            ["copy_component"] = "复制组件",
            ["paste_component_values"] = "粘贴组件数值",
            ["get_object_screen_bounds"] = "读取对象屏幕范围",
            ["raycast_at_point"] = "检测指定点射线",
            ["audit_ui"] = "检查用户界面",
            ["inspect_ui_sprites"] = "检查界面图片",
            ["create_project_ui"] = "创建项目界面",
            ["get_ui_defaults"] = "读取界面默认设置",
            ["get_prefab_stage"] = "读取预制体编辑状态",
            ["get_undo_state"] = "读取撤销状态",
            ["get_build_settings"] = "读取构建设置",
            ["get_windows"] = "读取编辑器窗口",
            ["get_tags"] = "读取标签",
            ["get_layers"] = "读取层",
            ["set_tag_and_layer"] = "设置标签和层",
            ["set_active_tool"] = "设置当前工具",
            ["focus_on_object"] = "聚焦对象",
            ["select_object"] = "选择对象",
            ["ping_asset"] = "定位资源",
            ["log_message"] = "记录日志",
            ["show_dialog"] = "显示对话框",
            ["add_layer"] = "添加层",
            ["add_tag"] = "添加标签",
            ["cancel_editor_operation"] = "取消编辑器操作",
            ["cancel_ui_audit"] = "取消界面审计",
            ["close_prefab_stage"] = "关闭预制体编辑模式",
            ["configure_ui_defaults"] = "配置界面默认设置",
            ["create_button"] = "创建按钮",
            ["create_canvas"] = "创建画布",
            ["create_image"] = "创建图片",
            ["create_text"] = "创建文本",
            ["exists"] = "检查是否存在",
            ["find_project_types"] = "查找项目类型",
            ["get_active_tool"] = "读取当前编辑器工具",
            ["get_scriptable_object"] = "读取 ScriptableObject",
            ["get_task"] = "读取任务",
            ["get_visual_coordinates"] = "读取视觉坐标",
            ["instantiate_prefab"] = "实例化预制体",
            ["list_directory"] = "列出目录",
            ["load_scene_additive"] = "叠加加载场景",
            ["prepare_editor"] = "准备编辑器",
            ["read_file"] = "读取文件",
            ["redo"] = "重做",
            ["save_prefab_stage"] = "保存预制体编辑模式",
            ["search_files"] = "搜索文件",
            ["set_active"] = "设置激活状态",
            ["set_scriptable_object_properties"] = "设置 ScriptableObject 属性",
            ["undo"] = "撤销",
            ["unload_scene"] = "卸载场景",
            ["write_file"] = "写入文件"
        };

        private static readonly Dictionary<string, string> CategoryTranslations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Animation"] = "动画",
            ["Asset"] = "资源",
            ["Asset Import"] = "资源导入",
            ["Camera"] = "相机",
            ["Components"] = "组件",
            ["Editor"] = "编辑器",
            ["GameObject"] = "游戏对象",
            ["Hierarchy"] = "层级",
            ["Input"] = "输入",
            ["Lighting"] = "光照",
            ["Material"] = "材质",
            ["Memory"] = "内存",
            ["Package"] = "项目包",
            ["Particle"] = "粒子",
            ["Physics"] = "物理",
            ["Prefab"] = "预制体",
            ["Profiler"] = "性能分析",
            ["Scene"] = "场景",
            ["Screenshot"] = "截图",
            ["Script"] = "脚本",
            ["ScriptableObject"] = "ScriptableObject",
            ["Testing"] = "测试",
            ["UI"] = "UI",
            ["Video"] = "视频",
            ["Visual Inspection"] = "视觉检查",
            ["Code"] = "代码",
            ["Compilation"] = "编译",
            ["Component Batch"] = "组件批处理",
            ["Asset Batch"] = "资源批处理",
            ["Batch"] = "批处理",
            ["Menu Item"] = "菜单项",
            ["Mesh"] = "网格",
            ["AssetImport"] = "资源导入",
            ["ComponentBatch"] = "组件批处理",
            ["ComponentProperty"] = "组件属性",
            ["EditorOperations"] = "编辑器操作",
            ["EditorState"] = "编辑器状态",
            ["File"] = "文件",
            ["InputSimulation"] = "输入模拟",
            ["Inspection"] = "检查",
            ["MemorySnapshot"] = "内存快照",
            ["Performance"] = "性能",
            ["ProjectSettings"] = "项目设置",
            ["References"] = "引用",
            ["Tasks"] = "任务",
            ["Timeline"] = "时间轴",
            ["UIAudit"] = "界面审计",
            ["UIPreview"] = "界面预览",
            ["Visual"] = "视觉反馈",
            ["VisualInspection"] = "视觉检查",
            ["Component Property"] = "组件属性",
            ["Editor Operations"] = "编辑器操作",
            ["Editor State"] = "编辑器状态",
            ["Memory Snapshot"] = "内存快照",
            ["Project Settings"] = "项目设置",
            ["UI Audit"] = "界面审计",
            ["UI Preview"] = "界面预览",
            ["Input Simulation"] = "输入模拟",
            ["Other"] = "其他",
            ["Manual"] = "手动注册"
        };

        private static readonly Dictionary<string, string> ToolWordTranslations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["get"] = "读取", ["set"] = "设置", ["create"] = "创建", ["delete"] = "删除", ["remove"] = "移除",
            ["cancel"] = "取消", ["capabilities"] = "能力", ["coordinates"] = "坐标", ["directory"] = "目录",
            ["additive"] = "叠加", ["instantiate"] = "实例化", ["stage"] = "编辑模式", ["scriptable"] = "ScriptableObject",
            ["write"] = "写入", ["operation"] = "操作",
            ["add"] = "添加", ["assign"] = "绑定", ["apply"] = "应用", ["analyze"] = "分析", ["audit"] = "审计",
            ["capture"] = "截取", ["check"] = "检查", ["clear"] = "清理", ["close"] = "关闭", ["configure"] = "配置",
            ["copy"] = "复制", ["duplicate"] = "复制", ["edit"] = "编辑", ["enter"] = "进入", ["exit"] = "退出",
            ["execute"] = "执行", ["export"] = "导出", ["find"] = "查找", ["focus"] = "聚焦", ["generate"] = "生成",
            ["import"] = "导入", ["inspect"] = "检查", ["install"] = "安装", ["list"] = "列出", ["load"] = "加载",
            ["mark"] = "标记", ["move"] = "移动", ["open"] = "打开", ["pause"] = "暂停", ["play"] = "播放",
            ["prepare"] = "准备", ["read"] = "读取", ["record"] = "录制", ["redo"] = "重做", ["refresh"] = "刷新",
            ["rename"] = "重命名", ["reset"] = "重置", ["restore"] = "恢复", ["run"] = "运行", ["save"] = "保存",
            ["search"] = "搜索", ["select"] = "选择", ["simulate"] = "模拟", ["start"] = "开始", ["stop"] = "停止",
            ["test"] = "测试", ["toggle"] = "切换", ["unload"] = "卸载", ["undo"] = "撤销", ["update"] = "更新",
            ["wait"] = "等待", ["animation"] = "动画", ["animator"] = "Animator", ["asset"] = "资源", ["assets"] = "资源",
            ["audio"] = "音频", ["camera"] = "相机", ["code"] = "代码", ["component"] = "组件", ["components"] = "组件",
            ["configuration"] = "配置", ["compile"] = "编译", ["compilation"] = "编译", ["console"] = "控制台", ["controller"] = "控制器",
            ["batch"] = "批处理",
            ["editor"] = "编辑器", ["error"] = "错误", ["file"] = "文件", ["frame"] = "帧", ["game"] = "游戏",
            ["object"] = "对象", ["objects"] = "对象", ["hierarchy"] = "层级", ["input"] = "输入", ["light"] = "光照",
            ["lighting"] = "光照", ["material"] = "材质", ["memory"] = "内存", ["mesh"] = "网格", ["package"] = "项目包",
            ["packages"] = "项目包", ["particle"] = "粒子", ["physics"] = "物理", ["prefab"] = "预制体", ["preview"] = "预览",
            ["profile"] = "配置档", ["profiler"] = "性能分析", ["project"] = "项目", ["property"] = "属性", ["properties"] = "属性",
            ["reference"] = "引用", ["references"] = "引用", ["scene"] = "场景", ["script"] = "脚本", ["scripts"] = "脚本",
            ["selection"] = "选择", ["screenshot"] = "截图", ["shader"] = "着色器", ["state"] = "状态", ["status"] = "状态",
            ["task"] = "任务", ["texture"] = "纹理", ["time"] = "时间", ["tool"] = "工具", ["tools"] = "工具",
            ["transform"] = "Transform", ["ui"] = "UI", ["video"] = "视频", ["visual"] = "视觉",
            ["workflow"] = "工作流", ["world"] = "世界", ["recovery"] = "恢复", ["reload"] = "重载",
            ["request"] = "请求", ["recording"] = "录制", ["settings"] = "设置", ["default"] = "默认", ["build"] = "构建",
            ["timeline"] = "时间轴", ["vfx"] = "特效", ["line"] = "线条", ["trail"] = "拖尾", ["volume"] = "体积",
            ["performance"] = "性能", ["snapshot"] = "快照", ["query"] = "查询", ["raycast"] = "射线检测", ["scroll"] = "滚动",
            ["active"] = "当前", ["current"] = "当前", ["selected"] = "已选", ["type"] = "类型",
            ["path"] = "路径", ["info"] = "信息", ["information"] = "信息", ["field"] = "字段", ["fields"] = "字段",
            ["system"] = "系统", ["scale"] = "缩放", ["layers"] = "层",
            ["tags"] = "标签", ["job"] = "作业", ["jobs"] = "作业",
            ["available"] = "可用", ["all"] = "全部", ["async"] = "异步",
            ["menu"] = "菜单", ["validate"] = "验证", ["recompile"] = "重新编译",
            ["many"] = "多个", ["paste"] = "粘贴", ["values"] = "数值", ["value"] = "数值", ["to"] = "到",
            ["screen"] = "屏幕", ["bounds"] = "范围", ["point"] = "点", ["sprite"] = "图片", ["sprites"] = "图片",
            ["defaults"] = "默认设置", ["window"] = "窗口", ["windows"] = "窗口",
            ["dialog"] = "对话框", ["message"] = "消息",
            ["cancel_editor_operation"] = "取消编辑器操作",
            ["cancel_test_run"] = "取消测试运行",
            ["cancel_ui_audit"] = "取消界面审计",
            ["capture_multiview"] = "截取多视图",
            ["capture_simulator_view"] = "截取模拟器视图",
            ["clear_execute_code_history"] = "清空代码执行历史",
            ["close_prefab_stage"] = "关闭预制体编辑模式",
            ["configure_ui_defaults"] = "配置界面默认值",
            ["create_button"] = "创建按钮",
            ["create_canvas"] = "创建画布",
            ["create_image"] = "创建图片",
            ["create_text"] = "创建文本",
            ["director_evaluate"] = "评估时间轴",
            ["end_ui_preview_session"] = "结束界面预览会话",
            ["exists"] = "检查是否存在",
            ["extract_recording_frames"] = "提取录制帧",
            ["find_broken_references"] = "查找失效引用",
            ["find_project_types"] = "查找项目类型",
            ["find_references"] = "查找引用",
            ["frame_debugger_disable"] = "禁用帧调试器",
            ["frame_debugger_enable"] = "启用帧调试器",
            ["frame_debugger_get_events"] = "读取帧调试事件",
            ["get_active_tool"] = "读取当前编辑器工具",
            ["get_camera_properties"] = "读取相机属性",
            ["get_counters"] = "读取性能计数器",
            ["get_editor_operation"] = "读取编辑器操作",
            ["get_execute_code_history"] = "读取代码执行历史",
            ["get_frame_timing"] = "读取帧耗时",
            ["get_lighting_settings"] = "读取光照设置",
            ["get_material_properties"] = "读取材质属性",
            ["get_object_memory"] = "读取对象内存",
            ["get_recording_frame"] = "读取录制帧",
            ["get_scriptable_object"] = "读取 ScriptableObject",
            ["get_task"] = "读取任务",
            ["get_test_job"] = "读取测试作业",
            ["get_time_scale"] = "读取时间缩放",
            ["get_top_memory_objects"] = "读取高内存对象",
            ["get_ui_audit"] = "读取界面审计结果",
            ["get_ui_preview_session"] = "读取界面预览会话",
            ["get_visual_coordinates"] = "读取视觉坐标",
            ["instantiate_prefab"] = "实例化预制体",
            ["list_directory"] = "列出目录",
            ["list_dirty_scenes"] = "列出已修改场景",
            ["list_editor_operations"] = "列出编辑器操作",
            ["list_scenes"] = "列出场景",
            ["load_scene_additive"] = "叠加加载场景",
            ["mark_recording"] = "标记录制内容",
            ["memory_compare_snapshots"] = "比较内存快照",
            ["memory_list_full_snapshots"] = "列出完整内存快照",
            ["memory_list_snapshots"] = "列出内存快照",
            ["memory_open_snapshot_in_profiler"] = "在性能分析器中打开内存快照",
            ["memory_query_references"] = "查询内存引用",
            ["memory_query_top_objects"] = "查询高内存对象",
            ["memory_take_full_snapshot"] = "获取完整内存快照",
            ["memory_take_snapshot"] = "获取内存快照",
            ["miss"] = "读取未命中结果",
            ["particle_control"] = "控制粒子系统",
            ["physics2d_overlap_point"] = "检测二维物理重叠点",
            ["physics2_d_overlap_point"] = "检测二维物理重叠点",
            ["physics_overlap"] = "检测物理重叠",
            ["physics_raycast"] = "执行物理射线检测",
            ["prepare_editor"] = "准备编辑器",
            ["profiler_start"] = "启动性能分析",
            ["profiler_status"] = "读取性能分析状态",
            ["profiler_stop"] = "停止性能分析",
            ["read_file"] = "读取文件",
            ["remove_tag"] = "移除标签",
            ["replay_execute_code"] = "重放代码执行",
            ["save_prefab_stage"] = "保存预制体编辑模式",
            ["search_files"] = "搜索文件",
            ["set_active"] = "设置激活状态",
            ["set_camera_culling_mask"] = "设置相机剔除遮罩",
            ["set_camera_projection"] = "设置相机投影",
            ["set_camera_settings"] = "设置相机参数",
            ["set_lighting_settings"] = "设置光照参数",
            ["set_material_property"] = "设置材质属性",
            ["set_scriptable_object_properties"] = "设置 ScriptableObject 属性",
            ["set_time_scale"] = "设置时间缩放",
            ["simulate_key_combo"] = "模拟组合按键",
            ["simulate_mouse_drag"] = "模拟鼠标拖拽",
            ["start_ui_preview_session"] = "开始界面预览会话",
            ["try_find_violation"] = "尝试查找安全违规",
            ["unload_scene"] = "卸载场景",
            ["unpack_prefab"] = "拆包预制体",
            ["write_file"] = "写入文件",
            ["add_layer"] = "添加层",
            ["add_tag"] = "添加标签",
            ["bake_lightmaps"] = "烘焙光照贴图"
        };

        // 复杂工具使用更具体的说明；未单独列出的工具会根据动作类型生成中文说明。
        private static readonly Dictionary<string, string> ToolTooltipOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["director_evaluate"] = "在指定时间或帧评估时间轴和可播放导演，返回轨道和播放状态，适合检查时间轴驱动结果。",
            ["start_ui_preview_session"] = "创建临时的 UI 预览会话，保存当前场景、选择和游戏视图设置，并在隔离环境中预览 UI。",
            ["end_ui_preview_session"] = "结束指定的 UI 预览会话，退出运行模式，恢复预览前的场景、选择和游戏视图设置，并清理由预览创建的临时文件。",
            ["prepare_editor"] = "准备 Unity 编辑器进入编辑或运行验证状态：处理运行模式、资源导入、脚本编译和域重载，并返回可继续查询的任务编号。",
            ["get_task"] = "查询异步编辑器任务的进度、完成状态、结果和警告；不会取消任务，也不会替用户恢复或清理资源。",
            ["get_editor_operation"] = "读取编辑器准备任务的详细状态，包括当前阶段、是否完成、是否可验证以及域重载恢复信息。",
            ["execute_code"] = "在 Unity 编辑器中执行受安全检查约束的 C# 代码，可创建或修改对象并返回结构化结果；修改后应读取结果确认实际状态。",
            ["replay_execute_code"] = "重新执行一条已经记录的代码任务，仅适用于明确可重放的历史执行记录。",
            ["get_execute_code_history"] = "查看代码执行历史，包括任务编号、执行状态、摘要和可重放信息。",
            ["clear_execute_code_history"] = "清空当前项目保存的代码执行历史，不会撤销已经写入场景或资源的修改。",
            ["create_project_ui"] = "按照项目 UI 默认设置创建可复用的画布、面板和控件层级，并返回创建对象的实例编号。",
            ["create_canvas"] = "创建配置好画布、画布缩放器和图形射线检测器的 UI 根对象。",
            ["create_button"] = "在指定父对象下创建可交互按钮，并设置文本、尺寸、颜色和必要的 UI 组件。",
            ["create_text"] = "在指定父对象下创建文本组件，并设置内容、字体大小、颜色和对齐方式。",
            ["create_image"] = "在指定父对象下创建图片组件，并设置图片资源、颜色、尺寸和显示模式。",
            ["audit_ui"] = "对当前场景或预制体 UI 执行只读审计，检查层级、布局、引用、文本和常见交互问题，不保存场景。",
            ["get_ui_audit"] = "读取 UI 审计任务的进度和结果，返回问题等级、对象路径、属性位置及修复建议。",
            ["cancel_ui_audit"] = "取消正在进行的 UI 审计，并关闭由本次审计创建的临时预览场景。",
            ["inspect_ui_sprites"] = "检查图片组件使用的图片资源、源纹理导入设置、九宫格边界、图集状态和子资源编号，不会修改或重新导入资源。",
            ["configure_ui_defaults"] = "验证并保存项目级 UI 默认设置，例如文本组件、字体资源、输入模块和控件预制体路径，不会修改场景。",
            ["get_ui_defaults"] = "读取当前项目的 UI 默认设置，帮助 UI 创建工具遵循项目已有的组件和资源规范。",
            ["capture_game_view"] = "截取当前游戏视图的静态画面，可用于验证运行时 UI、分辨率适配和最终显示效果。",
            ["capture_scene_view"] = "截取当前场景视图的静态画面，用于检查场景布局、对象位置和编辑器视图状态。",
            ["capture_editor_window"] = "截取指定 Unity 编辑器窗口的画面，用于验证检查器、层级、项目窗口或其他编辑器 UI。",
            ["capture_multiview"] = "同时截取多个 Unity 视图，便于对比场景视图、游戏视图或其他窗口中的状态。",
            ["record_game_view"] = "录制一段游戏视图视频，适合验证动画、滚动、点击反馈和 UI 状态变化。",
            ["extract_recording_frames"] = "从已经完成的录制视频中提取指定时间点的 PNG 帧，用于逐帧检查动画或交互结果。",
            ["mark_recording"] = "在当前录制时间线上添加命名标记，方便把点击、拖拽、滚动等操作与视频时间点对应起来。",
            ["profiler_start"] = "启动 Unity 性能分析采集并记录指定的性能计数器，后续使用性能查询工具读取采样结果。",
            ["profiler_stop"] = "停止当前性能分析采集并释放计数器，保留已经收集到的结果供后续读取。",
            ["profiler_status"] = "读取性能分析采集是否正在运行，以及当前已经记录的计数器和采样状态。",
            ["get_performance_snapshot"] = "读取当前编辑器或运行时的性能快照，包括帧率、耗时和资源统计，用于定位性能瓶颈。",
            ["memory_take_snapshot"] = "采集轻量级内存汇总快照，返回分类统计，不会生成可在 Unity 内存分析器中打开的完整快照文件。",
            ["memory_take_full_snapshot"] = "采集完整 Unity 内存快照文件，用于后续在性能分析器中检查对象占用和引用关系。",
            ["memory_query_top_objects"] = "按内存占用列出占用最大的对象，帮助快速定位异常资源或对象数量。",
            ["memory_query_references"] = "查询对象之间的内存引用关系，帮助判断对象为何仍被保留以及哪些对象阻止释放。",
            ["frame_debugger_enable"] = "打开并启用 Unity 帧调试器，开始捕获当前画面的绘制事件。使用完后应调用关闭工具。",
            ["frame_debugger_disable"] = "关闭 Unity 帧调试器并停止绘制事件捕获，避免持续影响编辑器性能。",
            ["frame_debugger_get_events"] = "读取当前帧调试器中的绘制事件、材质和批次信息，用于分析渲染顺序与过度绘制。",
            ["open_prefab_stage"] = "在独立的预制体编辑模式中打开预制体资源，便于直接检查或修改预制体内部层级。",
            ["save_prefab_stage"] = "将当前预制体编辑模式中的修改保存回预制体资源，但不会关闭预制体编辑阶段。",
            ["close_prefab_stage"] = "关闭当前预制体编辑模式并返回主场景；可选择保存或放弃尚未保存的预制体修改。",
            ["set_component_property"] = "通过序列化对象接口修改单个组件属性，支持序列化私有字段和 Unity 对象引用，并返回写入结果。",
            ["set_component_properties"] = "一次性修改组件的多个序列化属性，逐项报告成功或失败，适合批量调整界面、材质和行为参数。",
            ["get_component_properties"] = "读取组件可见的序列化属性和值，包含字段路径、类型和对象引用，适合修改前确认当前状态。",
            ["set_prefab_property"] = "直接修改预制体资源中指定组件的单个序列化属性，不需要打开预制体编辑模式。",
            ["set_prefab_properties"] = "直接批量修改预制体资源中的多个序列化属性，并返回每个字段的持久化结果。",
            ["simulate_mouse_click"] = "在运行模式中按屏幕像素坐标模拟一次鼠标点击，并报告实际命中的 UI 或物理对象。",
            ["simulate_mouse_drag"] = "在运行模式中模拟从起点拖拽到终点的鼠标操作，用于验证拖动控件和拖拽交互。",
            ["simulate_ui_scroll"] = "在运行模式中指定的屏幕位置派发 UI 滚动事件，用于验证滚动 UI。",
            ["simulate_key_press"] = "在运行模式中模拟一次键盘按键输入，供输入系统和游戏逻辑处理。",
            ["simulate_key_combo"] = "在运行模式中同时模拟多个按键按下，适合验证组合键、快捷键和移动控制。",
            ["enter_play_mode"] = "让 Unity 编辑器进入运行模式，并等待脚本编译、域重载和场景准备完成。",
            ["exit_play_mode"] = "让 Unity 编辑器退出运行模式，恢复到编辑状态并等待编辑器准备完成。",
            ["run_tests"] = "异步运行 Unity 测试运行器的编辑模式或运行模式测试，立即返回任务编号供后续查询。",
            ["cancel_test_run"] = "取消正在运行的 Unity 测试任务，适用于测试卡住或不再需要继续执行的情况。",
            ["get_compilation_errors"] = "读取当前项目脚本编译错误和警告，返回文件、行号及具体诊断信息。",
            ["request_recompile"] = "请求 Unity 重新编译脚本并触发域重载；运行模式中不能执行，需要先退出运行模式。",
            ["wait_for_compilation"] = "等待 Unity 当前脚本编译和域重载完成，并返回是否已经可以继续执行工具。",
            ["get_tool_capabilities"] = "列出已实现的 MCP 工具与当前配置档实际暴露的工具，帮助判断某项操作是否可用。",
            ["execute_menu_item"] = "按 Unity 菜单路径执行一个编辑器菜单命令，例如创建对象、刷新资源或打开设置窗口。",
            ["validate_menu_item"] = "检查指定 Unity 菜单项当前是否存在并处于可执行状态，不会实际执行菜单命令。",
            ["find_references"] = "查找项目中引用指定资源或对象的场景、预制体和资源文件。",
            ["find_broken_references"] = "扫描项目中的失效对象引用，并返回引用所在资源、字段路径和缺失目标。",
            ["get_undo_state"] = "读取 Unity 撤销栈中可用的撤销和重做状态，帮助确认上一项编辑操作是否可以恢复。",
            ["undo"] = "撤销最近一次可撤销的 Unity 编辑器修改，并返回撤销后的编辑器状态。",
            ["redo"] = "重做最近一次已撤销的 Unity 编辑器修改，并返回重做后的编辑器状态。",
            ["focus_on_object"] = "让场景视图聚焦到指定游戏对象或资源对象，便于继续检查其位置和组件。",
            ["select_object"] = "在 Unity 编辑器中选中指定对象，并同步层级和检查器中的当前选择。",
            ["ping_asset"] = "在项目窗口中定位并高亮指定资源，帮助用户快速找到对应文件。",
            ["log_message"] = "向 Unity 控制台写入一条带级别的诊断消息，用于记录自动化过程或验证结果。",
            ["show_dialog"] = "在 Unity 编辑器中显示一个确认或提示对话框，用于需要用户确认的编辑器操作。",
            ["get_object_screen_bounds"] = "计算指定对象在当前视图中的屏幕边界，用于判断 UI 或场景对象是否位于可见区域。",
            ["raycast_at_point"] = "从指定屏幕坐标发射射线，返回命中的场景对象、碰撞点和法线信息。",
            ["get_visual_coordinates"] = "读取对象或 UI 元素的视觉坐标和尺寸，帮助把截图位置转换为 Unity 可用的屏幕坐标。",
            ["get_scene_info"] = "读取当前打开场景的路径、根对象、脏状态和运行状态，帮助确认场景是否已保存。",
            ["get_hierarchy"] = "读取场景层级树，可按根对象、深度和激活状态筛选，用于定位目标对象。",
            ["find_game_objects"] = "按实例编号、名称、路径、标签、层或组件查找游戏对象，并返回可继续使用的实例编号。",
            ["get_game_object_info"] = "读取游戏对象的 Transform、组件、激活状态、标签和层等完整信息。",
            ["set_transform"] = "设置游戏对象的 Transform（位置、旋转和缩放），可用于单个对象或批量对象调整。",
            ["set_active"] = "启用或禁用一个或多个游戏对象，并返回每个对象实际应用后的激活状态。",
            ["set_tag_and_layer"] = "为游戏对象设置标签和层，供碰撞、渲染、查找和游戏逻辑使用。",
            ["create_game_object"] = "在当前场景创建一个空游戏对象，并返回实例编号以便后续添加组件或设置 Transform。",
            ["create_primitive"] = "在当前场景创建立方体、球体、胶囊体、圆柱体、平面或四边形等基础几何体。",
            ["delete_game_object"] = "删除场景中的指定游戏对象及其层级内容；执行前请确认对象路径和实例编号。",
            ["duplicate_game_object"] = "复制场景中的游戏对象，可同时为副本指定新名称。",
            ["rename_game_object"] = "修改场景中游戏对象的名称，并保持其层级位置不变。",
            ["add_component"] = "向指定游戏对象添加一个 Unity 组件，并返回新组件的实例编号。",
            ["add_component_to_many"] = "向多个匹配的游戏对象批量添加同一种组件，并逐个返回处理结果。",
            ["list_components"] = "列出指定游戏对象上的全部组件、类型和实例编号。",
            ["create_script"] = "在项目目录创建新的 C# 脚本文件，并写入提供的脚本内容。",
            ["edit_script"] = "替换已有 C# 脚本的内容，并保留文件路径和项目引用关系。",
            ["patch_script"] = "在脚本中查找指定文本并执行精确替换，适合小范围修改而无需重写整个文件。",
            ["create_scriptable_object"] = "按指定类型创建 ScriptableObject 资源并保存到项目路径。",
            ["get_scriptable_object"] = "读取 ScriptableObject 资源的类型、路径和序列化属性。",
            ["set_scriptable_object_properties"] = "修改 ScriptableObject 资源的一个或多个序列化属性并保存资源。",
            ["get_test_job"] = "读取 Unity 测试任务的运行进度、通过数量、失败信息和最终状态。",
            ["get_reload_recovery_status"] = "读取脚本域重载后的恢复状态，确认 Unity 是否已经重新连接并恢复 MCP 服务。",
            ["get_ui_preview_session"] = "读取 UI 预览会话的当前阶段、临时资源、运行状态和恢复警告。",
            ["get_time_scale"] = "读取 Unity 当前时间缩放值，用于判断游戏是否暂停或运行速度是否被修改。",
            ["set_time_scale"] = "设置 Unity 的时间缩放值，常用于暂停游戏、慢动作或恢复正常运行速度。"
        };

        public static string T(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            return Chinese.TryGetValue(text, out var translated) ? translated : text;
        }

        public static string SkillBadge(string version)
        {
            return $"v{version} 必需";
        }

        public static string ToolName(string toolName)
        {
            if (string.IsNullOrEmpty(toolName))
                return toolName;
            if (ToolTranslations.TryGetValue(toolName, out var translated))
                return translated;

            var words = toolName.Split('_')
                .Select(word => ToolWordTranslations.TryGetValue(word, out var value) ? value : word)
                .ToArray();
            return string.Join(" ", words);
        }

        public static string CategoryName(string category)
        {
            if (string.IsNullOrEmpty(category))
                return category;
            if (CategoryTranslations.TryGetValue(category, out var translated))
                return translated;
            var words = category.Split(' ')
                .Select(word => ToolWordTranslations.TryGetValue(word, out var value) ? value : word)
                .ToArray();
            return string.Join(" ", words);
        }

        /// <summary>
        /// 返回工具分类的中文悬停说明。分类标识仍保持英文，仅用于内部匹配。
        /// </summary>
        public static string CategoryTooltip(string category)
        {
            var displayName = CategoryName(category);
            if (string.IsNullOrEmpty(displayName))
                return "工具分类";

            switch (displayName)
            {
                case "动画": return "管理 Animator、动画片段和动画参数。";
                case "资源": return "查找、复制、删除和重命名 Unity 资源。";
                case "资源导入": return "读取或修改资源导入器设置。";
                case "相机": return "读取和调整相机投影、裁剪面及显示范围。";
                case "组件": return "读取和修改游戏对象上的组件。";
                case "编辑器":
                case "编辑器操作":
                case "编辑器状态": return "读取或操作 Unity 编辑器当前状态。";
                case "游戏对象": return "创建、查找、复制和修改场景中的游戏对象。";
                case "层级": return "查看场景层级及游戏对象父子关系。";
                case "输入":
                case "输入模拟": return "在运行模式中模拟键盘、鼠标和界面输入。";
                case "光照": return "读取或设置场景光照与烘焙相关内容。";
                case "材质": return "读取和修改材质及着色器属性。";
                case "内存":
                case "内存快照": return "采集和分析 Unity 编辑器内存快照。";
                case "项目包": return "查看、安装或移除 Unity 项目包。";
                case "物理": return "执行 2D 或 3D 物理查询。";
                case "预制体": return "创建、编辑、打开和保存预制体。";
                case "性能分析":
                case "性能": return "读取性能计数器、帧耗时和性能快照。";
                case "场景": return "创建、打开、保存和查询场景。";
                case "脚本":
                case "代码": return "创建、编辑、修补和编译 C# 脚本。";
                case "ScriptableObject": return "创建、读取和保存 ScriptableObject 资源及其序列化属性。";
                case "任务": return "查询和管理需要异步等待的编辑器、测试或预览任务。";
                case "测试": return "运行和查询 Unity 测试任务。";
                case "时间轴": return "评估 Timeline/Playable Director 的时间、轨道和播放状态。";
                case "撤销": return "读取、撤销或重做 Unity 编辑器中的修改。";
                case "视频": return "录制游戏视图视频并提取关键时间点的画面。";
                case "组件属性": return "读取或写入组件的序列化属性和 Unity 对象引用。";
                case "UI":
                case "界面":
                case "用户界面":
                case "界面审计":
                case "界面预览": return "创建、检查和预览 Unity 用户界面。";
                case "截图":
                case "视觉检查": return "截取、录制或检查编辑器和游戏画面。";
                case "视觉反馈": return "聚焦、选中、定位对象，并显示验证所需的编辑器反馈。";
                case "Animator": return "管理 Animator、动画片段和动画参数。";
                case "Transform": return "读取或修改对象的 Transform 属性。";
                case "文件": return "读取、写入、搜索和检查项目文件。";
                case "菜单项": return "执行或验证 Unity 编辑器菜单项。";
                case "组件批处理":
                case "资源批处理":
                case "批处理": return "对多个对象或资源执行批量操作。";
                case "其他":
                case "手动注册": return "未归入标准分类的工具。";
                default: return $"包含与“{displayName}”相关的 Unity 编辑器工具。";
            }
        }

        /// <summary>
        /// 返回工具名称对应的中文悬停说明，不暴露面向 AI 的英文工具标识。
        /// </summary>
        public static string ToolTooltip(string toolName, string category)
        {
            if (!string.IsNullOrEmpty(toolName) && ToolTooltipOverrides.TryGetValue(toolName, out var detailedText))
                return detailedText;

            var toolDisplayName = ToolName(toolName);
            var categoryDisplayName = CategoryName(category);
            var normalized = (toolName ?? string.Empty).ToLowerInvariant();

            if (normalized.StartsWith("get_") || normalized.StartsWith("list_") ||
                normalized.StartsWith("find_") || normalized.StartsWith("inspect_") ||
                normalized == "exists" || normalized.StartsWith("read_"))
            {
                return $"{toolDisplayName}：读取当前项目中的相关状态、属性或资源，并返回结构化结果，适合在修改前确认目标。";
            }

            if (normalized.StartsWith("create_") || normalized.StartsWith("duplicate_") ||
                normalized.StartsWith("copy_") || normalized.StartsWith("add_") ||
                normalized.StartsWith("instantiate_"))
            {
                return $"{toolDisplayName}：在当前项目中创建或复制对象、资源或组件，并返回结果供后续工具继续处理。";
            }

            if (normalized.StartsWith("set_") || normalized.StartsWith("configure_") ||
                normalized.StartsWith("assign_") || normalized.StartsWith("apply_") ||
                normalized.StartsWith("edit_") || normalized.StartsWith("patch_") ||
                normalized.StartsWith("write_"))
            {
                return $"{toolDisplayName}：修改当前项目中的目标属性或文件，并将变更写回 Unity；执行后应重新读取结果确认。";
            }

            if (normalized.StartsWith("delete_") || normalized.StartsWith("remove_") ||
                normalized.StartsWith("close_") || normalized.StartsWith("cancel_") ||
                normalized.StartsWith("unload_") || normalized.StartsWith("exit_"))
            {
                return $"{toolDisplayName}：关闭、移除或取消指定的 Unity 编辑器内容；执行前请确认目标，避免误删或丢失未保存修改。";
            }

            if (normalized.StartsWith("capture_") || normalized.StartsWith("record_") ||
                normalized.StartsWith("extract_"))
            {
                return $"{toolDisplayName}：采集当前 Unity 画面或录制结果，用于检查界面、动画和运行时表现。";
            }

            if (normalized.StartsWith("simulate_"))
                return $"{toolDisplayName}：在运行模式中模拟用户输入，用于验证界面交互和游戏响应。";

            if (normalized.StartsWith("save_") || normalized.StartsWith("open_") ||
                normalized.StartsWith("load_") || normalized.StartsWith("enter_") ||
                normalized.StartsWith("start_") || normalized.StartsWith("stop_") ||
                normalized.StartsWith("run_") || normalized.StartsWith("play_"))
            {
                return $"{toolDisplayName}：控制 Unity 编辑器或运行环境的当前流程，并返回执行状态供后续验证。";
            }

            if (string.IsNullOrEmpty(categoryDisplayName))
                return $"{toolDisplayName}：执行与该工具名称对应的 Unity 编辑器操作，并返回结构化结果。";
            return $"{toolDisplayName}：执行{categoryDisplayName}相关的 Unity 编辑器操作，并返回结构化结果供后续验证。";
        }

    }
}
