// 版权归 MaxyMCP 所有，遵循 MIT 许可证。

using System;
using MaxyMCP.Editor.Services;
using MaxyMCP.Editor.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace MaxyMCP.Editor.MCP.Server
{
    internal sealed class MaxyMCPHeaderStatusPanel
    {
        private readonly ISettingsController _settings;
        private readonly MCPServerService _server;
        private Label _statusLabel;
        private Label _versionLabel;

        public MaxyMCPHeaderStatusPanel(ISettingsController settings, MCPServerService server)
        {
            _settings = settings;
            _server = server;
        }

        public void AddTo(VisualElement parent)
        {
            var titleRow = new VisualElement();
            titleRow.style.flexDirection = FlexDirection.Row;
            titleRow.style.alignItems = Align.Center;
            titleRow.style.marginBottom = 8;
            parent.Add(titleRow);

            var title = new Label(MaxyMCPLocalization.T("MCP Server"));
            title.style.fontSize = 16;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            title.style.flexGrow = 1;
            titleRow.Add(title);

            _versionLabel = new Label();
            _versionLabel.style.fontSize = 11;
            _versionLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            _versionLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            titleRow.Add(_versionLabel);

            _statusLabel = new Label();
            _statusLabel.style.fontSize = 13;
            _statusLabel.style.marginBottom = 10;
            parent.Add(_statusLabel);

            Refresh();
        }

        public void Refresh()
        {
            RefreshVersion();
            RefreshStatus();
        }

        public void RefreshVersion()
        {
            if (_versionLabel != null)
                _versionLabel.text = $"v{PackageVersionUtility.CurrentVersion}";
        }

        public void RefreshStatus()
        {
            if (_statusLabel == null)
                return;

            if (_server?.IsRunning == true)
            {
                if (_server.IsAttachedToExistingTransport)
                {
                    _statusLabel.text = $"{MaxyMCPLocalization.T("Attached to existing server on")} http://127.0.0.1:{_server.Port}/（{GetProfileDisplayName()}）";
                    _statusLabel.style.color = new Color(1f, 0.8f, 0.35f);
                }
                else
                {
                    _statusLabel.text = $"{MaxyMCPLocalization.T("Running on")} http://127.0.0.1:{_server.Port}/（{GetProfileDisplayName()}）";
                    _statusLabel.style.color = new Color(0.4f, 1f, 0.4f);
                }
            }
            else
            {
                _statusLabel.text = MaxyMCPLocalization.T("Stopped");
                _statusLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            }
        }

        private string GetProfileDisplayName()
        {
            if (string.Equals(_settings?.MCPToolExportProfile, "full", StringComparison.OrdinalIgnoreCase))
                return "完整";
            if (string.Equals(_settings?.MCPToolExportProfile, "main", StringComparison.OrdinalIgnoreCase))
                return "主要";
            return "核心";
        }
    }
}
