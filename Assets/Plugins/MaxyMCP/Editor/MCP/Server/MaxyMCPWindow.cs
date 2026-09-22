// 版权归 MaxyMCP 所有，遵循 MIT 许可证。

using MaxyMCP.Editor.DI;
using MaxyMCP.Editor.Settings;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MaxyMCP.Editor.MCP.Server
{
    internal class MaxyMCPWindow : EditorWindow
    {
        private ISettingsController _settingsController;
        private MCPServerService _mcpServer;
        private MaxyMCPHeaderStatusPanel _headerStatusPanel;
        private MaxyMCPProjectSkillsNoticePanel _projectSkillsNoticePanel;
        private MaxyMCPRecentActivityPanel _activityPanel;

        [MenuItem("MaxyMCP/MCP 服务器")]
        public static void ShowWindow()
        {
            var window = GetWindow<MaxyMCPWindow>(MaxyMCPLocalization.T("MCP Server"));
            window.minSize = new Vector2(360, 400);
            window.Show();
        }

        public void CreateGUI()
        {
            _settingsController = RootScopeServices.Services?.GetService(typeof(ISettingsController))
                as ISettingsController;
            _mcpServer = RootScopeServices.Services?.GetService(typeof(MCPServerService))
                as MCPServerService;

            if (_settingsController == null || _mcpServer == null)
            {
                rootVisualElement.Add(new Label(MaxyMCPLocalization.T("Failed to initialize services.")));
                return;
            }

            _mcpServer.InteractionLog.OnEntryAdded -= OnLogEntryAdded;
            _mcpServer.InteractionLog.OnEntryAdded += OnLogEntryAdded;

            BuildUI();
        }

        private void OnDestroy()
        {
            if (_mcpServer?.InteractionLog != null)
                _mcpServer.InteractionLog.OnEntryAdded -= OnLogEntryAdded;

            DisposePanels();
        }

        private void BuildUI()
        {
            DisposePanels();

            rootVisualElement.Clear();
            rootVisualElement.style.flexGrow = 1;
            rootVisualElement.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);

            var mainContainer = new VisualElement();
            mainContainer.style.flexGrow = 1;
            mainContainer.style.paddingLeft = 10;
            mainContainer.style.paddingRight = 10;
            mainContainer.style.paddingTop = 10;
            mainContainer.style.paddingBottom = 10;
            rootVisualElement.Add(mainContainer);

            _headerStatusPanel = new MaxyMCPHeaderStatusPanel(_settingsController, _mcpServer);
            _headerStatusPanel.AddTo(mainContainer);

            _projectSkillsNoticePanel = new MaxyMCPProjectSkillsNoticePanel(_settingsController);
            _projectSkillsNoticePanel.AddTo(mainContainer);

            new MaxyMCPServerControlsPanel(
                    _settingsController,
                    _mcpServer,
                    () => _headerStatusPanel?.RefreshStatus())
                .AddTo(mainContainer);

            new MaxyMCPToolExposurePanel(
                    _settingsController,
                    () => _headerStatusPanel?.RefreshStatus())
                .AddTo(mainContainer);

            new MaxyMCPClientConfigPanel(
                    _settingsController,
                    _mcpServer,
                    BuildUI)
                .AddTo(mainContainer);

            _activityPanel = new MaxyMCPRecentActivityPanel(_mcpServer, _settingsController);
            _activityPanel.AddTo(mainContainer);
        }

        private void DisposePanels()
        {
            _activityPanel?.Dispose();
            _activityPanel = null;
            _projectSkillsNoticePanel = null;
        }

        private void OnFocus()
        {
            _projectSkillsNoticePanel?.Refresh();
        }

        private void OnLogEntryAdded(MCPLogEntry entry)
        {
            _activityPanel?.OnEntryAdded(entry);
        }
    }
}
