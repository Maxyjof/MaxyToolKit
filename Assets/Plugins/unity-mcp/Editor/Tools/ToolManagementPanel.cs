// 版权归 MaxyMCP 所有，遵循 MIT 许可证。

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace MaxyMCP.Editor.Tools
{
    /// <summary>
    /// 创建按类别列出全部工具的 UIElements 面板，并提供运行时启用或禁用开关。
    /// </summary>
    internal static class ToolManagementPanel
    {
        public static VisualElement Build()
        {
            var root = new VisualElement();
            root.style.marginTop = 8;

            var header = new Label("工具功能");
            header.style.fontSize = 13;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color = new Color(0.8f, 0.8f, 0.8f);
            header.style.marginBottom = 6;
            root.Add(header);

            var desc = new Label(
                "可以单独启用或禁用工具。已禁用的工具不会发送给 AI 模型。");
            desc.style.fontSize = 10;
            desc.style.color = new Color(0.6f, 0.6f, 0.6f);
            desc.style.whiteSpace = WhiteSpace.Normal;
            desc.style.marginBottom = 8;
            root.Add(desc);

            // 按工具提供者类别分组。
            var groups = BuildToolGroups();

            foreach (var group in groups.OrderBy(g => g.Key))
            {
                var foldout = new Foldout();
                foldout.text = $"{MaxyMCP.Editor.MCP.Server.MaxyMCPLocalization.CategoryName(group.Key)}（{group.Value.Count}）";
                foldout.tooltip = MaxyMCP.Editor.MCP.Server.MaxyMCPLocalization.CategoryTooltip(group.Key);
                foldout.value = false; // 默认收起。
                foldout.style.marginBottom = 4;
                foldout.style.unityFontStyleAndWeight = FontStyle.Normal;
                var foldoutLabel = foldout.Q<Label>();
                if (foldoutLabel != null)
                    foldoutLabel.style.unityFontStyleAndWeight = FontStyle.Normal;

                foreach (var tool in group.Value.OrderBy(t => t.Name))
                {
                    var toolName = tool.Name; // 为闭包保存工具名称。
                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.alignItems = Align.Center;
                    row.style.marginLeft = 4;
                    row.style.marginBottom = 2;

                    var toggle = new Toggle();
                    toggle.value = ToolRegistry.IsEnabled(toolName);
                    toggle.RegisterValueChangedCallback(evt =>
                        ToolRegistry.SetEnabled(toolName, evt.newValue));
                    toggle.style.marginRight = 4;
                    row.Add(toggle);

                    var nameLabel = new Label(MaxyMCP.Editor.MCP.Server.MaxyMCPLocalization.ToolName(toolName));
                    nameLabel.tooltip = MaxyMCP.Editor.MCP.Server.MaxyMCPLocalization.ToolTooltip(toolName, group.Key);
                    nameLabel.style.fontSize = 11;
                    nameLabel.style.color = Color.white;
                    nameLabel.style.minWidth = 180;
                    row.Add(nameLabel);

                    // 工具描述是发送给 AI 的英文元数据，不在中文编辑器界面重复显示。
                    if (tool.IsReadOnly)
                    {
                        var badge = new Label("只读");
                        badge.tooltip = "只读工具（不会修改场景）";
                        badge.style.fontSize = 9;
                        badge.style.color = new Color(0.4f, 0.8f, 0.4f);
                        badge.style.marginLeft = 4;
                        row.Add(badge);
                    }

                    foldout.Add(row);
                }

                root.Add(foldout);
            }

            // 外部手动注册工具区域。
            var manualTools = ToolRegistry.ManualTools;
            if (manualTools.Count > 0)
            {
                var manualFoldout = new Foldout();
                manualFoldout.text = $"外部工具（{manualTools.Count}）";
                manualFoldout.tooltip = "由其他插件或项目代码手动注册的工具。";
                manualFoldout.value = false;
                manualFoldout.style.marginBottom = 4;
                manualFoldout.style.unityFontStyleAndWeight = FontStyle.Normal;
                var manualFoldoutLabel = manualFoldout.Q<Label>();
                if (manualFoldoutLabel != null)
                    manualFoldoutLabel.style.unityFontStyleAndWeight = FontStyle.Normal;

                foreach (var kvp in manualTools)
                {
                    var toolName = kvp.Key;
                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.alignItems = Align.Center;
                    row.style.marginLeft = 4;
                    row.style.marginBottom = 2;

                    var toggle = new Toggle();
                    toggle.value = ToolRegistry.IsEnabled(toolName);
                    toggle.RegisterValueChangedCallback(evt =>
                        ToolRegistry.SetEnabled(toolName, evt.newValue));
                    toggle.style.marginRight = 4;
                    row.Add(toggle);

                    var nameLabel = new Label(MaxyMCP.Editor.MCP.Server.MaxyMCPLocalization.ToolName(toolName));
                    nameLabel.tooltip = MaxyMCP.Editor.MCP.Server.MaxyMCPLocalization.ToolTooltip(toolName, "Manual");
                    nameLabel.style.fontSize = 11;
                    nameLabel.style.color = Color.white;
                    row.Add(nameLabel);

                    manualFoldout.Add(row);
                }

                root.Add(manualFoldout);
            }

            return root;
        }

        private struct ToolInfo
        {
            public string Name;
            public string Description;
            public bool IsReadOnly;
        }

        private static Dictionary<string, List<ToolInfo>> BuildToolGroups()
        {
            var groups = new Dictionary<string, List<ToolInfo>>();

            // 确保工具注册表已经扫描。
            var _ = ToolRegistry.ProviderTypes;

            foreach (var type in ToolRegistry.ProviderTypes)
            {
                var attr = type.GetCustomAttribute<ToolProviderAttribute>();
                var category = attr?.Category ?? type.Name;

                if (!groups.ContainsKey(category))
                    groups[category] = new List<ToolInfo>();

                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var snakeName = ToolRegistry.ToSnakeCase(method.Name);

                    // 跳过被阻止的工具（它们不会出现在注册表中）。
                    if (!ToolRegistry.MethodCache.ContainsKey(snakeName))
                        continue;

                    var descAttr = method.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();

                    groups[category].Add(new ToolInfo
                    {
                        Name = snakeName,
                        Description = descAttr?.Description ?? "",
                        IsReadOnly = ToolRegistry.IsReadOnly(method)
                    });
                }
            }

            return groups;
        }
    }
}
