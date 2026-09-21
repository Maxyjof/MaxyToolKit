// 版权归 MaxyMCP 所有，遵循 MIT 许可证。本文件的编辑器界面仅使用中文。

using System;
using System.Collections.Generic;
using MaxyMCP.Editor.Settings;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MaxyMCP.Editor.MCP.Server
{
    internal sealed class MaxyMCPToolExposurePanel
    {
        private readonly ISettingsController _settings;
        private readonly Action _refreshStatus;

        public MaxyMCPToolExposurePanel(ISettingsController settings, Action refreshStatus)
        {
            _settings = settings;
            _refreshStatus = refreshStatus;
        }

        public void AddTo(VisualElement parent)
        {
            var toolProfileChoices = new List<string> { "核心", "主要", "完整" };
            var currentProfile = _settings.MCPToolExportProfile ?? "core";
            var toolProfileField = new PopupField<string>(
                "工具暴露",
                toolProfileChoices,
                string.Equals(currentProfile, "full", StringComparison.OrdinalIgnoreCase)
                    ? 2
                    : string.Equals(currentProfile, "main", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
            toolProfileField.SetValueWithoutNotify(
                string.Equals(currentProfile, "full", StringComparison.OrdinalIgnoreCase)
                    ? "完整"
                    : string.Equals(currentProfile, "main", StringComparison.OrdinalIgnoreCase) ? "主要" : "核心");
            toolProfileField.RegisterValueChangedCallback(evt =>
            {
                _settings.MCPToolExportProfile = string.Equals(evt.newValue, "完整", StringComparison.Ordinal)
                    ? "full"
                    : string.Equals(evt.newValue, "主要", StringComparison.Ordinal) ? "main" : "core";
                _refreshStatus?.Invoke();
            });
            toolProfileField.style.flexGrow = 1;
            toolProfileField.style.flexShrink = 1;
            toolProfileField.style.minWidth = 0;
            toolProfileField.style.marginBottom = 0;

            // 齿轮按钮打开完整的工具暴露设置窗口（逐项工具清单），
            // 让核心/主要/完整配置档的快速切换行也能一键进入细粒度设置。
            // 点击时重新读取当前配置档并预选对应列表，确保切换配置档后编辑的是对应列表。
            var settingsButton = new Button(() => MaxyMCPToolExposureWindow.ShowWindow(_settings.MCPToolExportProfile))
            {
                tooltip = "打开工具暴露设置窗口"
            };
            var gearIcon = EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? "d_SettingsIcon" : "SettingsIcon");
            if (gearIcon != null && gearIcon.image != null)
            {
                settingsButton.style.backgroundImage = new StyleBackground((UnityEngine.Texture2D)gearIcon.image);
            }
            else
            {
                // 如果当前 Unity 版本或编辑器皮肤无法解析内置图标，则使用文字图标作为后备。
                settingsButton.text = "\u2699";
            }
            settingsButton.style.width = 22;
            settingsButton.style.minWidth = 22;
            settingsButton.style.maxWidth = 22;
            settingsButton.style.height = 20;
            settingsButton.style.flexShrink = 0;
            settingsButton.style.marginLeft = 4;
            settingsButton.style.marginTop = 0;
            settingsButton.style.marginBottom = 0;

            // 布局行：核心/主要/完整下拉框自动填充剩余宽度，齿轮按钮位于右侧。
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 4;
            row.Add(toolProfileField);
            row.Add(settingsButton);
            parent.Add(row);

            var hint = new Label("核心配置档是精简的默认工具集；主要配置档使用你自行选择的工具列表；完整配置档会暴露全部工具。\n工具名称和协议数据仍保持英文，以确保 AI 客户端兼容。 ");
            hint.style.fontSize = 10;
            hint.style.color = new UnityEngine.Color(0.65f, 0.65f, 0.65f);
            hint.style.marginBottom = 10;
            parent.Add(hint);
        }
    }
}
