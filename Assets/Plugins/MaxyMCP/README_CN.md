# MaxyMCP

MaxyMCP是面向Unity编辑器的本地MCP服务器。它让支持MCP的AI客户端通过HTTP连接正在运行的Unity工程，读取工程状态、修改资源、编写代码、运行测试并检查结果。

本版本面向中文用户：

- 编辑器窗口、菜单、状态提示和文档使用正式中文
- 工具名称、协议字段、JSON参数和发送给AI的技能内容保留英文，以保证客户端兼容
- 不提供其他界面语言切换

## 环境要求

- Unity 2022.3或更高版本
- Windows或macOS上的Unity编辑器
- 工程已安装Newtonsoft.Json和Input System依赖
- 需要使用MCP的AI客户端，例如Codex、Claude Code、Cursor或其他兼容客户端

## 快速开始

1. 将本文件所在的MaxyMCP文件夹放入工程的Assets/Plugins目录，或通过Unity Package Manager导入本包
2. 等待Unity完成编译
3. 在菜单栏打开MaxyMCP > MCP Server
4. 勾选启用服务器，记下窗口显示的地址和端口
5. 使用窗口中的客户端配置按钮生成连接配置
6. 在AI客户端中连接后，先让AI读取编辑器状态，再开始修改工程

服务器只在Unity编辑器运行时提供服务。关闭编辑器、停止服务器或更换端口后，客户端需要重新连接。

## 工具配置

MCP服务器提供三种内置工具分类：

- core：最常用的场景、资源、脚本、编译和运行工具，适合日常使用
- main：常用工具的完整组合，适合持续开发
- full：全部已注册工具，适合排查问题、性能分析和高级自动化

工具暴露窗口可以从分类中选择，也可以按分类展开后逐项调整。点击“保存”后配置立即写入当前分类；自定义配置不会自动覆盖其他分类。工具分类只影响AI可以看到的工具，不会删除工具实现。

## 推荐工作流程

1. **读取状态**：使用get_editor_state、get_selection和get_console_logs确认当前工程状态
2. **准备编辑器**：需要批量操作或截图时先调用prepare_editor
3. **执行修改**：使用资源、场景、脚本和对象相关工具完成操作
4. **等待编译**：外部修改C#文件后调用request_recompile，再调用wait_for_compilation
5. **检查错误**：调用get_compilation_errors和get_console_logs确认没有编译或运行错误
6. **验证结果**：进入运行模式后使用输入模拟、截图、录制或测试工具验证实际效果

如果工具返回正在执行的任务，使用get_task查询任务状态，不要重复提交相同操作。

## 能力概览

MaxyMCP按模块提供以下能力：

- 场景和游戏对象：创建、查找、移动、组件设置、预制体和PrefabStage操作
- 资源和脚本：查找、导入、创建、修改、重命名、删除和批量处理
- 编译和运行：请求重编译、等待编译、读取错误、进入或退出运行模式
- UI与输入：读取UI层级、模拟键盘鼠标、滚动、点击和拖拽
- 测试与验证：运行Unity Test Runner、查询任务、读取控制台日志和编辑器状态
- 截图与录制：捕获Scene或Game视图，录制Game View视频
- 性能分析：读取帧耗时、性能计数器、内存快照和场景复杂度
- 编辑器自动化：执行菜单项、管理选择对象、标签、层、构建设置和撤销重做

工具的正式名称和参数以当前MaxyMCP版本实际暴露的列表为准。AI传递工具名称时必须使用英文标识，例如request_recompile；用户界面中会显示中文说明。

## 常用工具示例

### 请求编译并检查错误

~~~json
{
  "tool": "request_recompile",
  "arguments": {}
}
~~~

随后调用：

~~~json
{
  "tool": "wait_for_compilation",
  "arguments": {
    "timeout_seconds": 60
  }
}
~~~

### 读取控制台日志

~~~json
{
  "tool": "get_console_logs",
  "arguments": {
    "log_type": "all",
    "limit": 50
  }
}
~~~

## 添加自定义工具

自定义工具使用特性标记公开方法。工具名称会自动转换为snake_case，参数说明会生成到MCP工具定义中：

~~~csharp
using System.ComponentModel;

[ToolProvider("ProjectTools")]
public static class ProjectTools
{
    [Description("创建指定数量的敌人")]
    public static string SpawnEnemies(
        [ToolParam("敌人数量", Required = true)] int count)
    {
        return $"已创建{count}个敌人";
    }
}
~~~

工具实现中的协议标识和参数名应保持英文；面向用户的日志、窗口文本和代码注释使用中文。

## 故障排查

- **客户端无法连接**：确认Unity窗口中的服务器已启用，端口没有被占用，并检查客户端配置中的地址是否与窗口一致
- **工具列表为空**：打开工具暴露窗口，选择正确分类并点击“保存”，然后重新连接客户端
- **修改后仍有旧结果**：先调用request_recompile和wait_for_compilation，再读取编译错误
- **运行模式操作失败**：确认Unity处于正确的编辑器状态，并先调用prepare_editor
- **截图或录制异常**：确保Game View处于可渲染状态，不要在操作过程中关闭或切换目标窗口

## 项目维护

MaxyMCP是本地维护版本。界面和工作流可以按项目需要直接修改，不要求跟随上游版本。第三方依赖和工具协议字段应谨慎升级，并在升级后重新验证客户端连接、编译、截图和运行模式操作。

## 许可证

本项目使用MIT许可证，详见LICENSE。
