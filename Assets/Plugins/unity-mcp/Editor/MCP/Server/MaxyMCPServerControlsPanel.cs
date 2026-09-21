// 版权归 MaxyMCP 所有，遵循 MIT 许可证。

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MaxyMCP.Editor.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace MaxyMCP.Editor.MCP.Server
{
    internal sealed class MaxyMCPServerControlsPanel
    {
        private const string DirectTransportChoice = "直接 HTTP（默认）";
        private const string BrokerTransportChoice = "代理模式（实验性）";
        private static readonly List<string> TransportChoices = new List<string> { DirectTransportChoice, BrokerTransportChoice };

        private readonly ISettingsController _settings;
        private readonly MCPServerService _server;
        private readonly Action _refreshStatus;
        private Label _brokerStatus;
        private TextField _brokerMonoPathField;
        private Label _brokerMonoHint;
        private Label _portOriginHint;
        private Button _releasePortPinButton;
        private Button _pinPortButton;
        private IntegerField _portField;
        private int _statusRefreshGeneration;

        public MaxyMCPServerControlsPanel(
            ISettingsController settings,
            MCPServerService server,
            Action refreshStatus)
        {
            _settings = settings;
            _server = server;
            _refreshStatus = refreshStatus;
        }

        public void AddTo(VisualElement parent)
        {
            var toggle = new Toggle(MaxyMCPLocalization.T("Enable MCP Server"));
            toggle.SetValueWithoutNotify(_settings.MCPServerEnabled);
            toggle.RegisterValueChangedCallback(evt =>
            {
                _settings.MCPServerEnabled = evt.newValue;
                Task lifecycleTask;
                if (evt.newValue)
                    lifecycleTask = _server.StartAsync();
                else
                {
                    lifecycleTask = _server.StopAsync();
                    MCPBrokerProcessManager.Stop();
                }

                RefreshStatusWhenSettled(
                    lifecycleTask,
                    evt.newValue ? "传输：正在启动……" : "传输：正在停止……");
            });
            toggle.style.marginBottom = 4;
            parent.Add(toggle);

            _portField = new IntegerField("服务器端口");
            _portField.tooltip =
                "显示当前项目使用的端口。未固定端口时，每个项目会根据自身路径派生独立端口，多个编辑器不会互相冲突。" +
                "输入端口可固定端口（适用于持续集成或防火墙规则）；“固定当前端口”可固定当前显示端口；“使用项目端口”可解除固定。" +
                "清空端口也会解除固定。";
            // 端口字段只在按下回车或失去焦点时提交；提交会触发完整的传输重启，
            // 这样可以避免输入多位端口时每输入一位就重启一次服务器。
            _portField.isDelayed = true;
            _portField.RegisterValueChangedCallback(evt =>
            {
                _settings.MCPServerPort = evt.newValue;
                RefreshStatusWhenSettled(ResolveSettingsLifecycleTask(), "传输：正在重启……");
            });
            parent.Add(_portField);

            _portOriginHint = new Label();
            _portOriginHint.style.whiteSpace = WhiteSpace.Normal;
            _portOriginHint.style.fontSize = 10;
            _portOriginHint.style.opacity = 0.7f;
            _portOriginHint.style.marginBottom = 2;
            parent.Add(_portOriginHint);

            var portButtonRow = new VisualElement();
            portButtonRow.style.flexDirection = FlexDirection.Row;
            portButtonRow.style.marginBottom = 8;

            _releasePortPinButton = new Button(() =>
            {
                _settings.ClearMCPServerPortOverride();
                RefreshStatusWhenSettled(ResolveSettingsLifecycleTask(), "传输：正在重启……");
            });
            _releasePortPinButton.text = MaxyMCPLocalization.T("Use Per-Project Port");
            portButtonRow.Add(_releasePortPinButton);

            // 输入当前已经显示的端口不会触发值变化事件，因此需要单独的按钮来固定当前端口。
            _pinPortButton = new Button(() =>
            {
                _settings.MCPServerPort = _portField.value;
                RefreshStatusWhenSettled(ResolveSettingsLifecycleTask(), "传输：正在重启……");
            });
            _pinPortButton.text = MaxyMCPLocalization.T("Pin Current Port");
            portButtonRow.Add(_pinPortButton);

            parent.Add(portButtonRow);

            UpdatePortOrigin();

            var transportModeDropdown = new DropdownField("传输模式");
            transportModeDropdown.choices = TransportChoices;
            transportModeDropdown.tooltip =
                "直接 HTTP（默认）：服务器直接占用 MCP HTTP 端口。" +
                "代理模式（实验性）：由本地代理进程占用端口，在 Unity 重载脚本域时保持客户端请求不中断。";
            transportModeDropdown.SetValueWithoutNotify(_settings.MCPBrokerModeEnabled ? BrokerTransportChoice : DirectTransportChoice);
            transportModeDropdown.RegisterValueChangedCallback(evt =>
            {
                var enabled = evt.newValue == BrokerTransportChoice;
                _settings.MCPBrokerModeEnabled = enabled;
                UpdateBrokerControls(enabled);

                if (_settings.MCPServerEnabled)
                {
                    RefreshStatusWhenSettled(
                        ResolveSettingsLifecycleTask(),
                        enabled
                            ? "传输：正在切换到代理模式……"
                            : "传输：正在切换到直接 HTTP……");
                }
                else
                {
                    if (!enabled)
                        MCPBrokerProcessManager.Stop();
                    UpdateBrokerStatus();
                    UpdatePortOrigin();
                    InvokeRefreshStatus();
                }
            });
            transportModeDropdown.style.marginBottom = 4;
            parent.Add(transportModeDropdown);

            _brokerMonoPathField = new TextField("代理 Mono 路径");
            _brokerMonoPathField.SetValueWithoutNotify(_settings.MCPBrokerMonoPath);
            _brokerMonoPathField.RegisterValueChangedCallback(evt =>
            {
                _settings.MCPBrokerMonoPath = evt.newValue;
                RefreshStatusWhenSettled(ResolveSettingsLifecycleTask(), "传输：正在重启……");
            });
            _brokerMonoPathField.style.marginBottom = 4;
            parent.Add(_brokerMonoPathField);

            _brokerMonoHint = new Label();
            _brokerMonoHint.style.whiteSpace = WhiteSpace.Normal;
            _brokerMonoHint.style.color = new Color(0.9f, 0.35f, 0.35f);
            _brokerMonoHint.style.marginBottom = 4;
            parent.Add(_brokerMonoHint);

            RefreshMonoPathAutoDetection();

            _brokerStatus = new Label();
            _brokerStatus.style.whiteSpace = WhiteSpace.Normal;
            _brokerStatus.style.opacity = 0.78f;
            _brokerStatus.style.marginBottom = 10;
            parent.Add(_brokerStatus);

            UpdateBrokerControls(_settings.MCPBrokerModeEnabled);
            UpdateBrokerStatus();
        }

        private void InvokeRefreshStatus()
        {
            _refreshStatus?.Invoke();
        }

        private Task ResolveSettingsLifecycleTask()
        {
            var restartTask = _server.WaitForSettingsRestartAsync();
            if (!restartTask.IsCompleted || !_settings.MCPServerEnabled || _server.IsRunning)
                return restartTask;

            // 如果此前启动失败，设置可能仍处于启用状态；此时应用传输设置也应视为一次明确重试。
            return _server.StartAsync();
        }

        private void RefreshStatusWhenSettled(Task lifecycleTask, string pendingText)
        {
            var generation = ++_statusRefreshGeneration;
            if (lifecycleTask != null && !lifecycleTask.IsCompleted && _brokerStatus != null)
                _brokerStatus.text = pendingText;

            _ = RefreshStatusWhenSettledAsync(lifecycleTask, generation);
        }

        private async Task RefreshStatusWhenSettledAsync(Task lifecycleTask, int generation)
        {
            try
            {
                if (lifecycleTask != null)
                    await lifecycleTask;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MaxyMCP MCP Server] Server lifecycle change failed: " + ex.Message);
            }

            if (generation != _statusRefreshGeneration)
                return;

            UpdateBrokerStatus();
            UpdatePortOrigin();
            InvokeRefreshStatus();
        }

        /// <summary>
        /// 显示端口来源：用户固定的端口或当前项目派生的默认端口；
        /// 如果实际绑定发生回退，也在这里提示，因为此时端口字段与客户端应使用的端口不同。
        /// </summary>
        private void UpdatePortOrigin()
        {
            if (_portOriginHint == null)
                return;

            var pinned = _settings.MCPServerPortConfigured;
            var resolvedPort = _server != null ? _server.ResolvedPort : _settings.MCPServerPort;
            var activePort = _server != null && _server.IsRunning ? _server.Port : 0;
            // 只有发生回退绑定时，请求端口与实际服务端口才会不同；
            // 如果与存储的固定值比较，会误把所有派生端口都当成回退端口。
            var fellBack = activePort > 0 && activePort != resolvedPort;

            if (fellBack)
            {
                _portOriginHint.text = pinned
                    ? $"已固定端口 {resolvedPort}，但该端口已被占用；当前实际服务端口为 {activePort}。"
                    : $"项目派生端口为 {resolvedPort}，但该端口已被占用；当前实际服务端口为 {activePort}。";
            }
            else if (pinned)
            {
                // 升级后的旧项目会固定原端口以避免端口变化；这里提示项目端口模式的存在。
                _portOriginHint.text =
                    $"当前项目已固定端口 {resolvedPort}。“使用项目端口”会根据项目路径派生端口，多个编辑器即可同时提供 MCP 服务。";
            }
            else
            {
                _portOriginHint.text =
                    $"端口根据当前项目路径派生（{resolvedPort}），每个项目都会使用独立端口。";
            }

            // 始终让字段显示实际使用的端口，避免用户手动填写客户端配置时复制到一个没有服务的端口。
            // 发生回退绑定时显示回退端口，“固定当前端口”即可一键结束冲突并永久使用该端口。
            if (_portField != null)
                _portField.SetValueWithoutNotify(activePort > 0 ? activePort : resolvedPort);

            if (_releasePortPinButton != null)
                _releasePortPinButton.style.display = pinned ? DisplayStyle.Flex : DisplayStyle.None;
            if (_pinPortButton != null)
                _pinPortButton.style.display = pinned ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void UpdateBrokerControls(bool enabled)
        {
            if (_brokerMonoPathField != null)
                _brokerMonoPathField.style.display = enabled ? DisplayStyle.Flex : DisplayStyle.None;
            if (_brokerMonoHint != null)
                _brokerMonoHint.style.display = enabled && !string.IsNullOrEmpty(_brokerMonoHint.text)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
        }

        /// <summary>
        /// 自动检测只用于显示，不会写入 <see cref="ISettingsController.MCPBrokerMonoPath"/>；
        /// 清空字段或从未修改字段都会保持真正的“自动检测”默认行为。
        /// </summary>
        private void RefreshMonoPathAutoDetection()
        {
            if (_brokerMonoPathField == null)
                return;

            if (!string.IsNullOrEmpty(_settings.MCPBrokerMonoPath))
            {
                _brokerMonoPathField.tooltip =
                    "可选的 Unity 内置 Mono 程序覆盖路径。留空即可从 Unity 编辑器安装目录自动检测。";
                SetMonoHint(null);
                return;
            }

            var detected = MCPBrokerProcessManager.ResolveMono(null);
            if (!string.IsNullOrEmpty(detected))
            {
                _brokerMonoPathField.SetValueWithoutNotify(detected);
                _brokerMonoPathField.tooltip =
                    "已从 Unity 编辑器安装目录自动检测。只有需要覆盖默认路径时才需要手动填写。";
                SetMonoHint(null);
            }
            else
            {
                _brokerMonoPathField.tooltip =
                    "可选的 Unity 内置 Mono 程序覆盖路径。留空即可从 Unity 编辑器安装目录自动检测。";
                SetMonoHint("无法自动检测 Unity 内置 Mono 程序。代理模式需要手动设置该路径。");
            }
        }

        private void SetMonoHint(string text)
        {
            if (_brokerMonoHint == null)
                return;

            _brokerMonoHint.text = text ?? string.Empty;
            _brokerMonoHint.style.display = !string.IsNullOrEmpty(text) && _settings.MCPBrokerModeEnabled
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        private void UpdateBrokerStatus()
        {
            if (_brokerStatus == null)
                return;

            if (!_settings.MCPBrokerModeEnabled)
            {
                _brokerStatus.text = MaxyMCPLocalization.T("Transport: Direct HTTP.");
                return;
            }

            if (MCPBrokerProcessManager.IsRunning(out var pid, out var port))
            {
                _brokerStatus.text = "传输：代理正在运行（进程 ID " + pid + "，端口 " + port + "）。";
                return;
            }

            var error = MCPBrokerProcessManager.LastError;
            _brokerStatus.text = string.IsNullOrEmpty(error)
                ? "传输：代理将在 MCP 服务器启动时运行。"
                : "传输：代理未运行——" + error;
        }
    }
}
