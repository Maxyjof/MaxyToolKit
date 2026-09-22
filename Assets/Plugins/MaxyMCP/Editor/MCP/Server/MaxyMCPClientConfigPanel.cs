// 版权归 MaxyMCP 所有，遵循 MIT 许可证。

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MaxyMCP.Editor.Settings;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MaxyMCP.Editor.MCP.Server
{
    internal sealed class MaxyMCPClientConfigPanel
    {
        private readonly ISettingsController _settings;
        private readonly MCPServerService _server;
        private readonly Action _rebuildWindow;
        private MCPConfigTarget[] _targets;
        private int _selectedTargetIndex;
        private Label _configStatusLabel;
        private Label _configPathLabel;

        private const string DeepSeekHarnessTargetName = "DeepSeek Harness";

        public MaxyMCPClientConfigPanel(
            ISettingsController settings,
            MCPServerService server,
            Action rebuildWindow)
        {
            _settings = settings;
            _server = server;
            _rebuildWindow = rebuildWindow;
        }

        public void AddTo(VisualElement parent)
        {
            var label = new Label(MaxyMCPLocalization.T("One-Click MCP Configuration"));
            label.style.fontSize = 12;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = new Color(0.75f, 0.75f, 0.75f);
            label.style.marginBottom = 6;
            parent.Add(label);

            var homePath = GetUserHomePath();
            _targets = CreateTargets(homePath);
            var names = _targets.Select(target => target.Name).ToList();

            _selectedTargetIndex = Mathf.Clamp(_selectedTargetIndex, 0, _targets.Length - 1);
            var persistedTargetName = _settings.MCPSelectedConfigTarget;
            if (!string.IsNullOrWhiteSpace(persistedTargetName))
            {
                var persistedIndex = names.FindIndex(name =>
                    string.Equals(name, persistedTargetName, StringComparison.OrdinalIgnoreCase));
                if (persistedIndex >= 0)
                    _selectedTargetIndex = persistedIndex;
            }

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 4;

            var dropdown = new PopupField<string>(names, _selectedTargetIndex);
            dropdown.style.flexGrow = 1;
            dropdown.style.height = 26;
            dropdown.RegisterValueChangedCallback(evt =>
            {
                _selectedTargetIndex = names.IndexOf(evt.newValue);
                _settings.MCPSelectedConfigTarget = evt.newValue;
                _rebuildWindow?.Invoke();
            });
            row.Add(dropdown);

            var configureButton = new Button(() =>
            {
                ConfigureMCPForTarget(_targets[_selectedTargetIndex]);
                RefreshStatus();
            });
            configureButton.text = MaxyMCPLocalization.T("Configure");
            configureButton.style.height = 26;
            configureButton.style.width = 80;
            configureButton.style.marginLeft = 4;
            configureButton.style.backgroundColor = new Color(0.2f, 0.5f, 0.3f);
            configureButton.style.color = Color.white;
            row.Add(configureButton);

            var selectedTarget = _targets[_selectedTargetIndex];
            var skillsSupported = !string.IsNullOrEmpty(
                ProjectSkillsManager.GetPlatformIdForConfigTarget(selectedTarget.Name));
            var configureSkillsButton = new Button(() =>
            {
                ConfigureMCPAndSkillsForTarget(_targets[_selectedTargetIndex]);
                RefreshStatus();
            });
            configureSkillsButton.text = MaxyMCPLocalization.T("Configure + Skills");
            configureSkillsButton.style.height = 26;
            configureSkillsButton.style.width = 130;
            configureSkillsButton.style.marginLeft = 4;
            configureSkillsButton.style.backgroundColor = new Color(0.25f, 0.45f, 0.65f);
            configureSkillsButton.style.color = Color.white;
            configureSkillsButton.SetEnabled(skillsSupported);
            row.Add(configureSkillsButton);

            parent.Add(row);

            var skillsHint = new Label(skillsSupported
                ? MaxyMCPLocalization.T("Configure + Skills also installs the project MCP workflow skill.")
                : MaxyMCPLocalization.T("Project skills are currently available for Claude Code, Cursor, Codex, OpenCode, DeepSeek Harness, and Antigravity."));
            skillsHint.style.fontSize = 10;
            skillsHint.style.color = new Color(0.6f, 0.6f, 0.6f);
            skillsHint.style.marginBottom = 4;
            skillsHint.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(skillsHint);

            _configStatusLabel = new Label();
            _configStatusLabel.style.fontSize = 11;
            _configStatusLabel.style.marginBottom = 2;
            parent.Add(_configStatusLabel);

            _configPathLabel = new Label();
            _configPathLabel.style.fontSize = 10;
            _configPathLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
            _configPathLabel.style.marginBottom = 6;
            _configPathLabel.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(_configPathLabel);

            RefreshStatus();
        }

        public void RefreshStatus()
        {
            if (_configStatusLabel == null || _configPathLabel == null || _targets == null)
                return;

            var idx = Mathf.Clamp(_selectedTargetIndex, 0, _targets.Length - 1);
            var target = _targets[idx];

            if (IsConfigurationBlockedByFallback())
            {
                _configStatusLabel.text = "状态：请先解决端口冲突，再进行配置。";
                _configStatusLabel.style.color = new Color(1f, 0.45f, 0.35f);
                _configPathLabel.text = BuildFallbackConfigurationBlockedMessage();
                return;
            }

            if (target.IsLMStudio)
            {
                var existingPaths = GetExistingLMStudioConfigPaths(GetUserHomePath());
                bool hasExistingConfig = existingPaths.Count > 0;

                _configStatusLabel.text = hasExistingConfig
                    ? "状态：已找到 LM Studio 配置。"
                    : "状态：点击配置将打开 LM Studio 的 MCP 添加链接。";
                _configStatusLabel.style.color = hasExistingConfig
                    ? new Color(0.4f, 1f, 0.4f)
                    : new Color(1f, 0.75f, 0.4f);

                _configPathLabel.text = hasExistingConfig
                    ? "现有配置：" + string.Join(" | ", existingPaths)
                    : "LM Studio 的配置路径因版本而异。点击配置会使用 lmstudio://add_mcp，不会创建猜测路径。";
                return;
            }

            if (target.IsDeepSeekHarness)
            {
                // RefreshStatus 在 AddTo 中执行；如果这里抛出异常，窗口剩余内容不会创建，
                // 并且每次重建窗口都会重复失败。补丁文件托管区块损坏时，用户可以按文档手动删除区块，
                // 因此只在当前状态标签中报告错误，不再向外抛出。读取尽力而为，只有配置时才拒绝修改无法解析的文件。
                try
                {
                    DescribeDeepSeekHarnessStatus();
                }
                catch (Exception ex)
                {
                    _configStatusLabel.text = "状态：补丁文件需要处理。";
                    _configStatusLabel.style.color = new Color(1f, 0.6f, 0.4f);
                    _configPathLabel.text = ex.Message;
                }

                return;
            }

            bool exists = File.Exists(target.ConfigPath);
            _configStatusLabel.text = exists ? "状态：已配置。" : "状态：尚未配置。";
            _configStatusLabel.style.color = exists
                ? new Color(0.4f, 1f, 0.4f)
                : new Color(1f, 0.6f, 0.4f);
            // 名称和地址用于检查配置或手动填写条目，并且现在都与当前项目相关。
            var resolvedKey = ResolveServerKeyForTarget(target);
            var details = $"{target.ConfigPath}\nEntry: {resolvedKey} -> {GetServerUrl()}";

            if (target.Name == "Antigravity")
                details += GetAntigravityGlobalConfigNotice(GetUserHomePath());

            // 说明名称追加哈希的原因，避免用户看到无法解释的十六进制后缀。
            // 占用者也可能是当前项目的旧记录（设置文件被删除或在同一台机器重新检出），无法与其他项目区分。
            if (!string.Equals(resolvedKey, GetPreferredServerKey(), StringComparison.Ordinal))
            {
                details +=
                    "\n由于其他项目（或当前项目的早期配置）已经在此配置中使用“" + GetPreferredServerKey() + "”，因此追加了项目哈希。";
            }

            // 旧版共享条目不会自动删除，因为它可能由任何项目写入；这里明确提示其存在，
            // 避免所有项目迁移到独立条目后，客户端仍保留一个无法响应的服务器。
            if (exists && HasLegacyMaxyMCPEntry(target))
            {
                details +=
                    $"\n此配置中仍存在旧版“{MaxyMCPServerKey.LegacyKey}”条目。当前已没有项目继续写入它；所有项目配置完成后，可以手动删除。";
            }

            // 插件早期会把条目写在配置顶层，后来才改为写入 projects["<path>"]。
            // 顶层条目对本机所有会话可见，因此需要明确提示用户手动清理，避免其他项目的工具泄漏到无关会话。
            if (exists && target.UseProjectScope)
            {
                var strayEntries = ReadTopLevelMaxyMCPEntryNames(target);
                if (strayEntries.Count > 0)
                {
                    details +=
                        $"\n⚠ {string.Join(", ", strayEntries)} 仍位于 {target.ConfigPath} 的顶层。" +
                        "本机所有 Claude Code 会话都能看到这些条目，而不仅是当前项目；请在每个使用 MaxyMCP 的项目重新配置后手动删除。";
                }
            }

            _configPathLabel.text = details;
        }

        // 与下方 _entryNamesCache 使用相同的（路径、修改时间）缓存结构，但不共享缓存：
        // Claude Code 的 RefreshStatus() 会在一次处理中查询同一文件的顶层名称和项目作用域名称，
        // 共享槽位会导致第二次查询驱逐第一次结果。
        private static string _topLevelEntryNamesCachePath;
        private static DateTime _topLevelEntryNamesCacheMtime;
        private static HashSet<string> _topLevelEntryNamesCache;

        /// <summary>
        /// 读取配置文件字面意义上的顶层 MaxyMCP 条目，忽略插件为 <see
        /// cref="MCPConfigTarget.UseProjectScope"/> 目标写入的 <c>projects["&lt;path&gt;"]</c> 区域。
        /// 仅用于标记该区域出现前遗留的条目；与 <see cref="ReadMaxyMCPEntryNames"/> 不同，这里不会混合两种作用域。
        /// </summary>
        private static HashSet<string> ReadTopLevelMaxyMCPEntryNames(MCPConfigTarget target)
        {
            try
            {
                if (!File.Exists(target.ConfigPath))
                    return new HashSet<string>(StringComparer.Ordinal);

                var mtime = File.GetLastWriteTimeUtc(target.ConfigPath);
                if (_topLevelEntryNamesCache != null &&
                    string.Equals(target.ConfigPath, _topLevelEntryNamesCachePath, StringComparison.Ordinal) &&
                    mtime == _topLevelEntryNamesCacheMtime)
                {
                    return _topLevelEntryNamesCache;
                }

                var names = ParseTopLevelMaxyMCPEntryNames(target);
                _topLevelEntryNamesCachePath = target.ConfigPath;
                _topLevelEntryNamesCacheMtime = mtime;
                _topLevelEntryNamesCache = names;
                return names;
            }
            catch (Exception)
            {
                return new HashSet<string>(StringComparer.Ordinal);
            }
        }

        private static HashSet<string> ParseTopLevelMaxyMCPEntryNames(MCPConfigTarget target)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            var content = File.ReadAllText(target.ConfigPath);
            if (!MaxyMCPServerKey.ContainsKnownKey(content))
                return names;

            var parsed = SimpleJsonHelper.Deserialize(content) as Dictionary<string, object>;
            var servers = parsed != null ? FindNestedDictionary(parsed, GetRootKey(target)) : null;
            if (servers == null)
                return names;

            foreach (var key in servers.Keys)
            {
                if (MaxyMCPServerKey.IsMaxyMCPKey(key))
                    names.Add(key);
            }

            return names;
        }

        // 以（路径、修改时间）为键的单条目缓存。~/.claude.json 实际可能达到数 MB，
        // 而这些检查会在每次窗口重建时运行于界面线程；反复解析一个不属于本插件的文件会造成明显卡顿。
        private static string _entryNamesCachePath;
        private static DateTime _entryNamesCacheMtime;
        private static HashSet<string> _entryNamesCache;

        /// <summary>
        /// 返回目标配置中已经存在的 MaxyMCP 条目名称，用于报告旧条目，并检测其他项目是否占用了当前项目需要的名称。
        /// </summary>
        private static HashSet<string> ReadMaxyMCPEntryNames(MCPConfigTarget target)
        {
            try
            {
                if (!File.Exists(target.ConfigPath))
                    return new HashSet<string>(StringComparer.Ordinal);

                var mtime = File.GetLastWriteTimeUtc(target.ConfigPath);
                if (_entryNamesCache != null &&
                    string.Equals(target.ConfigPath, _entryNamesCachePath, StringComparison.Ordinal) &&
                    mtime == _entryNamesCacheMtime)
                {
                    return _entryNamesCache;
                }

                var names = ParseMaxyMCPEntryNames(target);
                _entryNamesCachePath = target.ConfigPath;
                _entryNamesCacheMtime = mtime;
                _entryNamesCache = names;
                return names;
            }
            catch (Exception)
            {
                // 无法读取的配置不在这里发出警告；写入路径会报告具体失败原因。
                return new HashSet<string>(StringComparer.Ordinal);
            }
        }

        private static HashSet<string> ParseMaxyMCPEntryNames(MCPConfigTarget target)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            var content = File.ReadAllText(target.ConfigPath);

            // 解析前先进行廉价判断：大多数配置根本不包含 MaxyMCP 条目。
            if (!MaxyMCPServerKey.ContainsKnownKey(content))
                return names;

            if (target.IsToml)
            {
                foreach (Match match in Regex.Matches(content, @"(?m)^\[mcp_servers\.([^\]\s]+)\]"))
                {
                    var name = match.Groups[1].Value;
                    if (MaxyMCPServerKey.IsMaxyMCPKey(name))
                        names.Add(name);
                }

                return names;
            }

            var parsed = SimpleJsonHelper.Deserialize(content) as Dictionary<string, object>;
            if (parsed == null)
                return names;

            var servers = target.UseProjectScope
                ? FindProjectScopedServers(parsed, GetRootKey(target))
                : FindNestedDictionary(parsed, GetRootKey(target));
            if (servers == null)
                return names;

            foreach (var key in servers.Keys)
            {
                if (MaxyMCPServerKey.IsMaxyMCPKey(key))
                    names.Add(key);
            }

            return names;
        }

        private static Dictionary<string, object> FindNestedDictionary(Dictionary<string, object> parent, string key)
        {
            object value;
            return parent.TryGetValue(key, out value) ? value as Dictionary<string, object> : null;
        }

        private static Dictionary<string, object> FindProjectScopedServers(
            Dictionary<string, object> root, string rootKey)
        {
            var projects = FindNestedDictionary(root, "projects");
            var projectEntry = projects != null ? FindNestedDictionary(projects, GetProjectScopeKeyPath()) : null;
            return projectEntry != null ? FindNestedDictionary(projectEntry, rootKey) : null;
        }

        private static bool HasLegacyMaxyMCPEntry(MCPConfigTarget target)
        {
            return ReadMaxyMCPEntryNames(target).Any(MaxyMCPServerKey.IsMaxyMCPKey);
        }

        private static string GetRootKey(MCPConfigTarget target)
        {
            return string.IsNullOrEmpty(target.RootKey) ? "mcpServers" : target.RootKey;
        }

        private MCPConfigTarget[] CreateTargets(string homePath)
        {
            var kimiConfigPath = GetKimiConfigPath(
                homePath,
                GetProjectRootPath(),
                Environment.GetEnvironmentVariable("KIMI_CODE_HOME"));

            return new[]
            {
                new MCPConfigTarget
                {
                    Name = "Claude Code",
                    ConfigPath = Path.Combine(homePath, ".claude.json"),
                    IncludeTypeField = true,
                    UseProjectScope = true
                },
                new MCPConfigTarget
                {
                    Name = "Cursor",
                    ConfigPath = Path.Combine(homePath, ".cursor", "mcp.json"),
                },
                new MCPConfigTarget
                {
                    Name = "Kimi",
                    ConfigPath = kimiConfigPath,
                    ActivationHint = "请在当前 Unity 项目中重新启动 Kimi 会话，使配置生效。",
                },
                new MCPConfigTarget
                {
                    Name = "LM Studio",
                    ConfigPath = GetLMStudioDisplayPath(homePath),
                    IsLMStudio = true,
                },
                new MCPConfigTarget
                {
                    Name = "VS Code",
                    ConfigPath = GetVSCodeConfigPath(homePath),
                    IncludeTypeField = true,
                    RootKey = "servers"
                },
                new MCPConfigTarget
                {
                    Name = "Trae",
                    ConfigPath = Path.Combine(homePath, ".trae", "mcp.json"),
                },
                new MCPConfigTarget
                {
                    Name = "Kiro",
                    ConfigPath = Path.Combine(homePath, ".kiro", "settings", "mcp.json"),
                    IncludeTypeField = true,
                    RootKey = "mcpServers"
                },
                new MCPConfigTarget
                {
                    Name = "Codex",
                    ConfigPath = Path.Combine(homePath, ".codex", "config.toml"),
                    IsToml = true,
                },
                new MCPConfigTarget
                {
                    Name = "OpenCode",
                    ConfigPath = GetOpenCodeConfigPath(),
                    RootKey = "mcp",
                    IncludeTypeField = true,
                    TypeFieldValue = "remote",
                    IncludeEnabledField = true
                },
                new MCPConfigTarget
                {
                    Name = DeepSeekHarnessTargetName,
                    ConfigPath = MaxyMCPDeepSeekHarnessPatch.GetDisplayPath(homePath),
                    IsDeepSeekHarness = true,
                },
                CreateAntigravityTarget(GetProjectRootPath()),
            };
        }

        private void ConfigureMCPForTarget(MCPConfigTarget target)
        {
            try
            {
                var customMessage = WriteMCPConfigurationForTarget(target);

                var message = customMessage ??
                              (target.IsLMStudio
                                  ? BuildLMStudioConfiguredMessage()
                                  : $"MCP 配置已写入：\n{target.ConfigPath}\n\n" +
                                     (string.IsNullOrEmpty(target.ActivationHint)
                                         ? $"请重新启动 {target.Name}，使配置生效。"
                                         : target.ActivationHint));

                EditorUtility.DisplayDialog("MCP 配置", message, "确定");
                _rebuildWindow?.Invoke();
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog(
                    "MCP 配置错误",
                    $"配置失败：\n{ex.Message}",
                    "确定");
            }
        }

        private void ConfigureMCPAndSkillsForTarget(MCPConfigTarget target)
        {
            try
            {
                var customMessage = WriteMCPConfigurationForTarget(target);

                var platformId = ProjectSkillsManager.GetPlatformIdForConfigTarget(target.Name);
                if (string.IsNullOrEmpty(platformId))
                {
                    var configSummary = customMessage ??
                                        $"MCP 配置已写入：\n{target.ConfigPath}";
                    EditorUtility.DisplayDialog(
                        "MCP 配置",
                        configSummary + "\n\n" +
                        "项目技能目前支持 Claude Code、Cursor、Codex、OpenCode、DeepSeek Harness 和 Antigravity。",
                        "确定");

                    _rebuildWindow?.Invoke();
                    return;
                }

                if (!ConfigureProjectSkillsForPlatform(platformId))
                    return;

                var projectRoot = GetProjectRootPath();
                var manifest = ProjectSkillsManager.LoadManifest(projectRoot);
                var generatedPaths = ProjectSkillsManager.GetGeneratedPathsForPlatform(projectRoot, manifest, platformId);

                EditorUtility.DisplayDialog(
                    "MCP 配置",
                    $"MCP 配置已写入：\n{target.ConfigPath}\n\n" +
                    "项目 MCP 工作流技能已安装：\n" +
                    string.Join("\n", generatedPaths),
                    "确定");

                _rebuildWindow?.Invoke();
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog(
                    "MCP 配置错误",
                    $"配置失败：\n{ex.Message}",
                    "确定");
            }
        }

        /// <summary>
        /// 为 <paramref name="target"/> 写入当前项目条目。返回目标专用完成消息；返回 null 时由调用方生成通用消息。
        /// </summary>
        private string WriteMCPConfigurationForTarget(MCPConfigTarget target)
        {
            EnsureConfigurationEndpointIsSafe();

            if (target.IsLMStudio)
            {
                // lmstudio:// 深链接本身不会写入文件；只有实际重写配置文件后才记录名称，避免取消对话框污染记录。
                var lmStudioKey = ConfigureLMStudioTarget(target);
                if (!string.IsNullOrEmpty(lmStudioKey))
                    RecordWrittenServerKey(target, lmStudioKey);
                return null;
            }

            if (target.IsDeepSeekHarness)
                return ConfigureDeepSeekHarnessTarget(target);

            var dir = Path.GetDirectoryName(target.ConfigPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var writtenKey = target.IsToml
                ? ConfigureTomlTarget(target)
                : ConfigureJsonTarget(target);

            RecordWrittenServerKey(target, writtenKey);
            return null;
        }

        /// <summary>
        /// DeepSeek Harness 会在 ~/.dsh/profiles 下为每个配置档保存一份组合配置，启动时通过
        /// --profile 选择，外部不存在单一的“当前配置档”。因此需要把条目写入所有现有配置档的补丁文件，
        /// 用户只需点击一次“配置”即可覆盖网页后端和桌面应用之间的切换场景。若 ~/.dsh 存在但没有配置档，
        /// 则创建默认的“web”配置档。返回将在对话框中显示的完成消息。
        /// </summary>
        private string ConfigureDeepSeekHarnessTarget(MCPConfigTarget target)
        {
            var homePath = GetUserHomePath();
            var patchPaths = MaxyMCPDeepSeekHarnessPatch.GetProfilePatchPaths(homePath);
            if (patchPaths.Count == 0)
            {
                if (!Directory.Exists(MaxyMCPDeepSeekHarnessPatch.GetProfilesRoot(homePath)))
                {
                    throw new InvalidOperationException(
                        "未找到 DeepSeek Harness（~/.dsh 不存在）。请先启动一次，使其创建主目录，然后重新配置。");
                }

                patchPaths.Add(MaxyMCPDeepSeekHarnessPatch.GetDefaultPatchPath(homePath));
            }

            var serverKey = ResolveDeepSeekHarnessServerKey(patchPaths);
            var url = GetServerUrl();
            var supersededKey = _settings.GetLastClientConfigKey(DeepSeekHarnessTargetName);

            var written = new List<string>();
            foreach (var patchPath in patchPaths)
            {
                var content = ReadAllTextIfExists(patchPath);

                // 项目目录更名或项目哈希变化时，删除当前项目此前写入的旧键对应区块；
                // 只删除自己的托管区块，与 JSON/TOML 客户端的清理逻辑保持一致。
                if (!string.IsNullOrEmpty(supersededKey) &&
                    !string.Equals(supersededKey, serverKey, StringComparison.Ordinal))
                {
                    content = MaxyMCPDeepSeekHarnessPatch.RemoveManagedBlock(content, supersededKey);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(patchPath));
                File.WriteAllText(
                    patchPath,
                    MaxyMCPDeepSeekHarnessPatch.UpsertManagedBlock(content, serverKey, url));
                written.Add(patchPath);

                // 只要一个配置档写入成功就记录名称，而不是等待整个循环结束；
                // 这样即使后续配置档写入失败，下次配置也不会把刚写入的区块误认为其他项目并追加第二个条目。
                if (written.Count == 1)
                    RecordWrittenServerKey(target, serverKey);
            }

            var builder = new StringBuilder();
            builder.AppendLine("MCP 配置已写入：");
            foreach (var path in written)
                builder.AppendLine(path);
            builder.AppendLine();
            builder.AppendLine("条目：mcp-" + serverKey + "（" +
                               MaxyMCPDeepSeekHarnessPatch.ClientPluginName +
                               "，可流式 HTTP）");
            builder.AppendLine("工具会以 mcp__" + serverKey + "__<tool> 的形式提供。");
            builder.AppendLine("请重新启动 DeepSeek Harness 或重新加载配置档，使条目生效。");

            foreach (var path in written)
            {
                if (MaxyMCPDeepSeekHarnessPatch.HasServerNameOutsideManagedBlock(
                        File.ReadAllText(path), serverKey))
                {
                    builder.AppendLine()
                        .Append("⚠ “").Append(path).Append("” 中还存在托管区块之外手写的 dsh-mcp-client 条目，")
                        .Append("其 serverName 为“").Append(serverKey).Append("”。")
                        .AppendLine("重复的 serverName 会导致 DeepSeek Harness 加载时拒绝后续实例，请手动删除该条目。");
                }
            }

            return builder.ToString().TrimEnd();
        }

        /// <summary>
        /// 面板路径标签使用的配置档状态：每个配置档报告正常、过期或缺失，随后显示解析出的条目名称和端点。
        /// 同时显示与本服务器名称重复的手工条目，因为这种情况会使 DSH 拒绝后写入的实例。
        /// </summary>
        private void DescribeDeepSeekHarnessStatus()
        {
            var homePath = GetUserHomePath();
            if (!Directory.Exists(MaxyMCPDeepSeekHarnessPatch.GetProfilesRoot(homePath)))
            {
                _configStatusLabel.text = "状态：未找到 DeepSeek Harness（~/.dsh）。";
                _configStatusLabel.style.color = new Color(1f, 0.6f, 0.4f);
                _configPathLabel.text =
                    "请先启动一次 DeepSeek Harness，使其创建 ~/.dsh，然后再点击配置。";
                return;
            }

            var patchPaths = MaxyMCPDeepSeekHarnessPatch.GetProfilePatchPaths(homePath);
            var resolvedKey = ResolveDeepSeekHarnessServerKey(patchPaths);
            var expectedUrl = GetServerUrl();

            var configuredCount = 0;
            var lines = new List<string>();
            foreach (var patchPath in patchPaths)
            {
                var profileName = Path.GetFileName(Path.GetDirectoryName(patchPath));
                var content = ReadAllTextIfExists(patchPath);

                string blockUrl;
                if (!MaxyMCPDeepSeekHarnessPatch.TryGetManagedBlockUrl(content, resolvedKey, out blockUrl))
                {
                    lines.Add(profileName + "：没有托管条目");
                }
                else if (string.Equals(blockUrl, expectedUrl, StringComparison.OrdinalIgnoreCase))
                {
                    lines.Add(profileName + "：正常（" + blockUrl + "）");
                    configuredCount++;
                }
                else
                {
                    lines.Add(profileName + "：地址已过期（" + blockUrl + "，请重新配置为 " + expectedUrl + "）");
                }

                if (MaxyMCPDeepSeekHarnessPatch.HasServerNameOutsideManagedBlock(content, resolvedKey))
                {
                    lines.Add("⚠ 在 " + patchPath + " 中发现手写的 serverName “" + resolvedKey +
                              "”；重复的 serverName 会导致 DeepSeek Harness 拒绝后续实例，请手动删除该条目。");
                }
            }

            var allConfigured = patchPaths.Count > 0 && configuredCount == patchPaths.Count;
            _configStatusLabel.text = allConfigured ? "状态：已配置。" : "状态：尚未配置。";
            _configStatusLabel.style.color = allConfigured
                ? new Color(0.4f, 1f, 0.4f)
                : new Color(1f, 0.6f, 0.4f);

            var details = patchPaths.Count > 0
                ? string.Join("\n", lines.ToArray())
                : "~/.dsh/profiles 中还没有配置档；点击配置会创建默认的“web”配置档。";
            details += "\n条目：mcp-" + resolvedKey + "（" +
                       MaxyMCPDeepSeekHarnessPatch.ClientPluginName + ") -> " + expectedUrl;

            // 与 JSON/TOML 配置相同，说明意外出现哈希后缀的原因。
            if (!string.Equals(resolvedKey, GetPreferredServerKey(), StringComparison.Ordinal))
            {
                details +=
                    "\n由于其他项目（或当前项目的早期配置）已经在此配置中使用“" + GetPreferredServerKey() + "”，因此追加了项目哈希。";
            }

            _configPathLabel.text = details;
        }

        /// <summary>
        /// 要写入 DSH 的条目名称。它会在所有配置档补丁文件中检查以 maxymcp 命名的 serverName；
        /// DSH 会并列加载配置档，因此其他项目在任意配置档中占用名称都会像 JSON/TOML 客户端的逐文件扫描一样产生冲突。
        ///
        /// 扫描必然会看到当前项目自己的区块，因此如果首选名称下的托管区块指向当前编辑器端点，
        /// 就将其视为当前项目此前写入的凭据（LM Studio 深链接也采用相同依据）。否则一旦丢失记录名称，
        /// 下一次配置会把自己的条目误判为外部冲突，并在旁边写入带哈希后缀的第二个区块；旧区块不会被删除，
        /// 两个区块同时指向同一端点，之后每次配置都会重复生成相同的哈希名称。
        /// </summary>
        private string ResolveDeepSeekHarnessServerKey(List<string> patchPaths)
        {
            var existingNames = new HashSet<string>(StringComparer.Ordinal);
            var preferredKey = GetPreferredServerKey();
            var url = GetServerUrl();
            var preferredEntryPointsAtCurrentUrl = false;

            foreach (var patchPath in patchPaths)
            {
                var content = ReadAllTextIfExists(patchPath);
                existingNames.UnionWith(MaxyMCPDeepSeekHarnessPatch.ReadMaxyMCPServerNames(content));

                string blockUrl;
                if (MaxyMCPDeepSeekHarnessPatch.TryGetManagedBlockUrl(content, preferredKey, out blockUrl) &&
                    string.Equals(blockUrl, url, StringComparison.OrdinalIgnoreCase))
                {
                    preferredEntryPointsAtCurrentUrl = true;
                }
            }

            return ResolveServerKey(
                _settings.GetLastClientConfigKey(DeepSeekHarnessTargetName),
                existingNames,
                preferredEntryPointsAtCurrentUrl);
        }

        private static string ReadAllTextIfExists(string path)
        {
            return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        }

        /// <summary>
        /// 记录刚为此目标写入的条目名称。这是后续写入判断条目归属当前项目而非共享配置文件中其他项目的唯一依据。
        /// 每个目标独立记录；过去共用一个槽位时，重命名只能在第一个重新配置的目标中完成清理。
        /// 记录实际写入的名称，该名称可能包含自动追加的项目哈希。
        /// </summary>
        private void RecordWrittenServerKey(MCPConfigTarget target, string serverKey)
        {
            _settings.SetLastClientConfigKey(target.Name, serverKey);
        }

        private bool ConfigureProjectSkillsForPlatform(string platformId)
        {
            var projectRoot = GetProjectRootPath();
            var manifest = ProjectSkillsManager.LoadManifest(projectRoot);
            var selectedPlatforms = new HashSet<string>(manifest.platforms, StringComparer.OrdinalIgnoreCase)
            {
                platformId
            };

            var conflictPaths = ProjectSkillsManager.GetPlatformConflictPaths(projectRoot, selectedPlatforms);
            if (conflictPaths.Length > 0)
            {
                var overwrite = EditorUtility.DisplayDialog(
                    "项目技能配置",
                    "发现现有的非托管项目指令文件：\n\n" +
                    string.Join("\n", conflictPaths) +
                    "\n\n是否使用 MaxyMCP 托管文件覆盖它们？",
                    "覆盖",
                    "取消");

                if (!overwrite)
                    return false;
            }

            ProjectSkillsManager.ApplyConfiguration(projectRoot, selectedPlatforms, manifest.optionalSkills);
            return true;
        }

        /// <summary>
        /// 返回已写入的条目名称。<paramref name="presetServerKey"/> 允许 LM Studio 在所有配置副本中使用同一名称，
        /// 避免逐文件重新解析。
        /// </summary>
        private string ConfigureJsonTarget(MCPConfigTarget target, string presetServerKey = null)
        {
            var serverName = presetServerKey ?? ResolveServerKeyForTarget(target);
            WriteJsonConfiguration(target, serverName,
                _settings.GetLastClientConfigKey(target.Name), CreateHttpEntry(target));
            return serverName;
        }

        internal static void WriteJsonConfiguration(
            MCPConfigTarget target, string serverName, string previousServerName,
            Dictionary<string, object> entry)
        {
            var rootKey = GetRootKey(target);

            Dictionary<string, object> root = null;
            if (File.Exists(target.ConfigPath))
                root = ParseRewritableConfig(target, serverName, entry);
            root = root ?? new Dictionary<string, object>();

            var servers = target.UseProjectScope
                ? GetOrCreateProjectScopedServers(root, rootKey)
                : GetOrCreateNestedDictionary(root, rootKey);

            servers[serverName] = entry;
            RemoveSupersededMaxyMCPEntries(
                servers, serverName, previousServerName, target.UrlFieldName);

            var directory = Path.GetDirectoryName(target.ConfigPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(target.ConfigPath, SimpleJsonHelper.Serialize(root));
        }

        /// <summary>
        /// Reads an existing config file, but only when its full contents survive a
        /// <see cref="SimpleJsonHelper"/> round-trip. The whole file is rewritten from what the parse
        /// returns, and that parser is strict-JSON only: a config carrying JSONC comments (legal in
        /// OpenCode's <c>opencode.json</c> and VS Code's <c>mcp.json</c>, both of which are read with
        /// real JSONC parsers) stops the key scan dead at the comment, so writing the parse result
        /// back would silently delete every key past it -- providers, models, keybinds, agents. Rather
        /// than clobber a file it cannot represent, this reports what to add by hand and changes
        /// nothing, the same "stop instead of destroy" stance <c>WriteManagedBlock</c> takes for a
        /// legacy AGENTS.md. Returns null for an empty file (nothing to preserve).
        /// </summary>
        private static Dictionary<string, object> ParseRewritableConfig(
            MCPConfigTarget target, string serverName, Dictionary<string, object> entry)
        {
            var content = File.ReadAllText(target.ConfigPath);
            if (string.IsNullOrWhiteSpace(content))
                return null;

            string problem = null;
            Dictionary<string, object> parsed = null;

            if (ContainsJsonComment(content))
                problem = "文件包含 JSON 注释，MaxyMCP 的严格 JSON 写入器无法保留这些注释";
            else
            {
                parsed = SimpleJsonHelper.Deserialize(content) as Dictionary<string, object>;
                if (parsed == null)
                    problem = "文件无法读取为 JSON 对象";
            }

            if (problem == null)
                return parsed;

            throw new InvalidOperationException(
                $"由于{problem}，文件“{target.ConfigPath}”保持不变。" +
                $"请改为在“{GetRootKey(target)}”下手动添加以下条目：\n\n" +
                $"\"{serverName}\": {SimpleJsonHelper.Serialize(entry)}");
        }

        /// <summary>
        /// True if <paramref name="content"/> has a <c>//</c> or <c>/*</c> comment outside a string
        /// literal. Deliberately only detects them -- rewriting a commented file correctly would take
        /// a real JSONC parser, and the point here is to refuse, not to reformat.
        /// Internal so the scan can be exercised in EditMode tests without a config file.
        /// </summary>
        internal static bool ContainsJsonComment(string content)
        {
            var inString = false;
            for (var i = 0; i < content.Length; i++)
            {
                var c = content[i];

                if (inString)
                {
                    if (c == '\\')
                        i++;
                    else if (c == '"')
                        inString = false;
                    continue;
                }

                if (c == '"')
                    inString = true;
                else if (c == '/' && i + 1 < content.Length && (content[i + 1] == '/' || content[i + 1] == '*'))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets (or creates) the server-name -> entry dictionary at <paramref name="root"/>[<paramref
        /// name="key"/>], replacing whatever is there if it is not already a dictionary (a malformed
        /// or foreign value under that key).
        /// </summary>
        private static Dictionary<string, object> GetOrCreateNestedDictionary(
            Dictionary<string, object> root, string key)
        {
            object existing;
            if (root.TryGetValue(key, out existing) && existing is Dictionary<string, object> servers)
                return servers;

            servers = new Dictionary<string, object>();
            root[key] = servers;
            return servers;
        }

        /// <summary>
        /// Claude Code applies <c>projects["&lt;path&gt;"]</c> only to sessions opened at that exact
        /// path. Writing MaxyMCP's entry there -- instead of at the config's top level, which every
        /// session on the machine sees regardless of which project it is in -- is what keeps one
        /// project's MaxyMCP tools from being usable (and mistaken for the right project's) from an
        /// unrelated project's session. See docs/maxymcp-mcp-enhancement-backlog.md §I.
        /// </summary>
        private static Dictionary<string, object> GetOrCreateProjectScopedServers(
            Dictionary<string, object> root, string rootKey)
        {
            var projects = GetOrCreateNestedDictionary(root, "projects");
            var projectEntry = GetOrCreateNestedDictionary(projects, GetProjectScopeKeyPath());
            return GetOrCreateNestedDictionary(projectEntry, rootKey);
        }

        private const string ProjectScopeMigrationSessionStateKey =
            "MaxyMCP.ClaudeCodeConfig.ProjectScopeMigrationDone";

        /// <summary>
        /// One-time, best-effort self-heal for a Claude Code entry this project wrote before entries
        /// moved under <c>projects["&lt;path&gt;"]</c> (see CHANGELOG [Unreleased]). Without this, a
        /// project whose MaxyMCP panel is never reopened would keep leaking its tools into every Claude
        /// Code session on the machine indefinitely -- clicking Configure again is the only other way
        /// the old top-level entry gets moved. Called once from <c>RootScopeServices</c> on editor
        /// startup, so it reaches that project the next time its own Editor happens to be open, with no
        /// action required from the developer.
        ///
        /// Session-gated rather than running on every domain reload: <c>~/.claude.json</c> can be
        /// multi-megabyte (see the read-side cache above) and this only ever needs to run once per
        /// Editor process. Only the single entry this project itself previously recorded writing is
        /// touched -- same ownership proof <see cref="RemoveSupersededMaxyMCPEntries"/> already uses --
        /// so another project's entry, or one hand-edited after MaxyMCP wrote it, is left alone.
        /// </summary>
        internal static void TryMigrateLegacyClaudeCodeEntryOnce(ISettingsController settings)
        {
            if (SessionState.GetBool(ProjectScopeMigrationSessionStateKey, false))
                return;
            SessionState.SetBool(ProjectScopeMigrationSessionStateKey, true);

            try
            {
                var target = new MCPConfigTarget
                {
                    Name = "Claude Code",
                    ConfigPath = Path.Combine(GetUserHomePath(), ".claude.json"),
                    IncludeTypeField = true,
                    UseProjectScope = true
                };

                var recordedKey = settings?.GetLastClientConfigKey(target.Name);
                if (string.IsNullOrEmpty(recordedKey) || !File.Exists(target.ConfigPath))
                    return;

                string migratedFrom;
                if (!TryMigrateLegacyClaudeCodeEntryFile(
                        target.ConfigPath,
                        recordedKey,
                        GetProjectRootPath(),
                        GetProjectScopeKeyPath(),
                        out migratedFrom))
                {
                    return;
                }

                PluginDebugLogger.Log(
                    $"[MaxyMCP MCP Server] Migrated Claude Code entry \"{recordedKey}\" out of {migratedFrom} in " +
                    $"{target.ConfigPath} into this project's correct projects[...] scope.");
            }
            catch (Exception ex)
            {
                PluginDebugLogger.Log("[MaxyMCP MCP Server] Claude Code config scope migration skipped: " + ex.Message);
            }
        }

        /// <summary>
        /// Migrates the recorded MaxyMCP entry out of Claude Code's legacy global or Unity-project
        /// scopes and into the git-root scope. The transform is written only if the config still
        /// contains the exact text that was parsed, so a concurrent Claude process cannot have its
        /// update silently overwritten. The final replacement is atomic and takes place in the same
        /// directory as the config file.
        /// </summary>
        internal static bool TryMigrateLegacyClaudeCodeEntryFile(
            string configPath,
            string recordedKey,
            string legacyProjectPath,
            string projectScopePath,
            out string migratedFrom)
        {
            migratedFrom = null;
            if (string.IsNullOrEmpty(configPath) ||
                string.IsNullOrEmpty(recordedKey) ||
                !MaxyMCPServerKey.IsMaxyMCPKey(recordedKey) ||
                string.IsNullOrEmpty(projectScopePath) ||
                !File.Exists(configPath))
            {
                return false;
            }

            var originalContent = File.ReadAllText(configPath);

            // Same "stop instead of destroy" guard ConfigureJsonTarget applies through
            // ParseRewritableConfig: the file is rewritten from a strict-JSON parse, so one carrying
            // JSONC comments would lose every key past the first comment. This path runs
            // unattended at editor startup and has no way to ask, so it migrates nothing rather
            // than truncate a config it cannot reproduce; clicking Configure reports the entry to
            // add by hand. Claude Code writes ~/.claude.json itself and does not put comments in
            // it, so this is a guard against a hand-edited file, not an expected shape.
            if (ContainsJsonComment(originalContent))
                return false;

            var root = SimpleJsonHelper.Deserialize(originalContent) as Dictionary<string, object>;
            if (root == null)
                return false;

            if (!TryMigrateLegacyClaudeCodeEntry(
                    root,
                    "mcpServers",
                    recordedKey,
                    legacyProjectPath,
                    projectScopePath,
                    out migratedFrom))
            {
                return false;
            }

            var wroteConfig = TryWriteTextAtomicallyIfUnchanged(
                configPath,
                originalContent,
                SimpleJsonHelper.Serialize(root));
            if (!wroteConfig)
                migratedFrom = null;
            return wroteConfig;
        }

        private static bool TryMigrateLegacyClaudeCodeEntry(
            Dictionary<string, object> root,
            string rootKey,
            string recordedKey,
            string legacyProjectPath,
            string projectScopePath,
            out string migratedFrom)
        {
            migratedFrom = null;

            var topLevelServers = FindNestedDictionary(root, rootKey);
            object topLevelValue;
            var hasTopLevelEntry = TryGetLoopbackEntry(topLevelServers, recordedKey, out topLevelValue);

            Dictionary<string, object> legacyServers = null;
            object legacyValue = null;
            var hasLegacyProjectEntry = false;
            if (!string.IsNullOrEmpty(legacyProjectPath) &&
                !string.Equals(legacyProjectPath, projectScopePath, StringComparison.Ordinal))
            {
                var projects = FindNestedDictionary(root, "projects");
                var legacyEntry = projects != null ? FindNestedDictionary(projects, legacyProjectPath) : null;
                legacyServers = legacyEntry != null ? FindNestedDictionary(legacyEntry, rootKey) : null;
                hasLegacyProjectEntry = TryGetLoopbackEntry(legacyServers, recordedKey, out legacyValue);
            }

            if (!hasTopLevelEntry && !hasLegacyProjectEntry)
                return false;

            Dictionary<string, object> destinationServers;
            if (!TryGetOrCreateProjectScopedServersForMigration(
                    root, rootKey, projectScopePath, out destinationServers))
            {
                return false;
            }

            // Preserve a destination entry that already exists. It may have been reconfigured more
            // recently or deliberately edited by the user; a stale source must never overwrite it.
            if (!destinationServers.ContainsKey(recordedKey))
            {
                // The Unity-project scope was the newer of the two legacy layouts, so prefer its
                // endpoint when both stale copies exist.
                destinationServers[recordedKey] = hasLegacyProjectEntry ? legacyValue : topLevelValue;
            }

            var sources = new List<string>();
            if (hasTopLevelEntry)
            {
                topLevelServers.Remove(recordedKey);
                sources.Add("the top level");
            }

            if (hasLegacyProjectEntry)
            {
                legacyServers.Remove(recordedKey);
                sources.Add($"projects[\"{legacyProjectPath}\"]");
            }

            migratedFrom = string.Join(" and ", sources);
            return true;
        }

        /// <summary>
        /// Creates the destination dictionaries only when every existing value on the path is also
        /// a dictionary. Automatic startup migration must not replace malformed or foreign config
        /// values merely to make room for MaxyMCP.
        /// </summary>
        private static bool TryGetOrCreateProjectScopedServersForMigration(
            Dictionary<string, object> root,
            string rootKey,
            string projectScopePath,
            out Dictionary<string, object> servers)
        {
            servers = null;

            Dictionary<string, object> projects;
            if (!TryGetOrCreateNestedDictionaryForMigration(root, "projects", out projects))
                return false;

            Dictionary<string, object> projectEntry;
            if (!TryGetOrCreateNestedDictionaryForMigration(projects, projectScopePath, out projectEntry))
                return false;

            return TryGetOrCreateNestedDictionaryForMigration(projectEntry, rootKey, out servers);
        }

        private static bool TryGetOrCreateNestedDictionaryForMigration(
            Dictionary<string, object> parent,
            string key,
            out Dictionary<string, object> child)
        {
            object existing;
            if (parent.TryGetValue(key, out existing))
            {
                child = existing as Dictionary<string, object>;
                return child != null;
            }

            child = new Dictionary<string, object>();
            parent[key] = child;
            return true;
        }

        private static bool TryGetLoopbackEntry(
            Dictionary<string, object> servers, string key, out object value)
        {
            value = null;
            return servers != null &&
                   servers.TryGetValue(key, out value) &&
                   IsLoopbackEntry(value);
        }

        /// <summary>
        /// Best-effort compare-and-swap for a JSON config. A temporary file in the destination
        /// directory is fully written first, then atomically replaces the original. Symbolic links
        /// are skipped because replacing one would replace the link itself rather than its target.
        /// </summary>
        internal static bool TryWriteTextAtomicallyIfUnchanged(
            string path, string expectedContent, string updatedContent)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return false;

            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                return false;

            if (!string.Equals(File.ReadAllText(path), expectedContent, StringComparison.Ordinal))
                return false;

            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory))
                return false;

            var tempPath = Path.Combine(
                directory,
                "." + Path.GetFileName(path) + ".maxymcp-" + Guid.NewGuid().ToString("N") + ".tmp");

            try
            {
                File.WriteAllText(tempPath, updatedContent, new UTF8Encoding(false));

                // Narrow the race window once more after the potentially expensive temp write.
                if (!string.Equals(File.ReadAllText(path), expectedContent, StringComparison.Ordinal))
                    return false;

                File.Replace(tempPath, path, null);
                return true;
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        /// <summary>
        /// Retires the entry THIS project wrote last, once its name has changed (a renamed product, or
        /// the project-hash toggle). Nothing else is removed: a <c>maxymcp-*</c> entry this project
        /// never wrote belongs to another project, and deleting it would recreate the very bug the
        /// per-project naming fixes -- configuring project B silently unhooking project A.
        /// The legacy shared <c>maxymcp</c> entry is deliberately left in place for the same reason:
        /// any project on the machine could have written it, so it is surfaced in the panel instead.
        /// </summary>
        internal static void RemoveSupersededMaxyMCPEntries(
            Dictionary<string, object> servers,
            string currentServerName,
            string previousServerName,
            string urlFieldName = null)
        {
            if (string.IsNullOrEmpty(previousServerName) ||
                string.Equals(previousServerName, currentServerName, StringComparison.Ordinal))
            {
                return;
            }

            if (!MaxyMCPServerKey.IsMaxyMCPKey(previousServerName))
                return;

            object previousEntry;
            if (!servers.TryGetValue(previousServerName, out previousEntry))
                return;

            // A recorded key whose entry now points somewhere non-local was edited by hand after we
            // wrote it; leave that alone rather than deleting someone's deliberate change.
            if (!IsLoopbackEntry(previousEntry, urlFieldName))
                return;

            servers.Remove(previousServerName);
        }

        // The endpoint key follows the target (Antigravity writes serverUrl); reading only "url"
        // here would make an Antigravity entry look hand-edited and never retire it.
        private static bool IsLoopbackEntry(object entry, string urlFieldName = null)
        {
            var entryMap = entry as Dictionary<string, object>;
            object url;
            if (entryMap == null || !entryMap.TryGetValue(GetUrlFieldName(urlFieldName), out url))
                return false;

            return IsLoopbackUrl(url as string);
        }

        /// <summary>Returns the entry name written.</summary>
        private string ConfigureTomlTarget(MCPConfigTarget target)
        {
            var serverKey = ResolveServerKeyForTarget(target);
            var tomlSection = CreateTomlSection(target, serverKey);
            var content = File.Exists(target.ConfigPath) ? File.ReadAllText(target.ConfigPath) : string.Empty;

            content = RemoveSupersededTomlSection(
                content, serverKey, _settings.GetLastClientConfigKey(target.Name));

            int startIdx;
            int endIdx;
            if (TryFindTomlSection(content, serverKey, out startIdx, out endIdx))
            {
                content = content.Substring(0, startIdx) + tomlSection + content.Substring(endIdx);
            }
            else
            {
                if (content.Length > 0 && !content.EndsWith("\n"))
                    content += "\n";
                content += "\n" + tomlSection;
            }

            File.WriteAllText(target.ConfigPath, content);
            return serverKey;
        }

        /// <summary>
        /// TOML counterpart of <see cref="RemoveSupersededMaxyMCPEntries"/>: drops the section this
        /// project wrote last when its name has changed, and nothing else. Mirrors the JSON path's
        /// hand-edit guard -- a section whose url was repointed at a non-local host was edited
        /// deliberately after we wrote it and is kept.
        /// </summary>
        internal static string RemoveSupersededTomlSection(
            string content, string currentServerName, string previousServerName)
        {
            if (string.IsNullOrEmpty(content) ||
                string.IsNullOrEmpty(previousServerName) ||
                string.Equals(previousServerName, currentServerName, StringComparison.Ordinal) ||
                !MaxyMCPServerKey.IsMaxyMCPKey(previousServerName))
            {
                return content;
            }

            int startIdx;
            int endIdx;
            if (!TryFindTomlSection(content, previousServerName, out startIdx, out endIdx))
                return content;

            var section = content.Substring(startIdx, endIdx - startIdx);
            if (!TomlSectionPointsAtLoopback(section))
                return content;

            return content.Substring(0, startIdx) + content.Substring(endIdx);
        }

        /// <summary>
        /// Locates a <c>[mcp_servers.&lt;name&gt;]</c> section. The single place that owns the
        /// section-boundary rules, shared by the writer and the cleanup so they cannot drift.
        /// Headers only match at the start of a line -- a commented-out
        /// <c># [mcp_servers...]</c> used to match mid-line and made the cleanup cut from inside the
        /// comment. The returned range includes the section's trailing newline; the next section's
        /// <c>[</c> starts at <paramref name="endIdx"/>.
        /// </summary>
        internal static bool TryFindTomlSection(
            string content, string serverName, out int startIdx, out int endIdx)
        {
            startIdx = -1;
            endIdx = -1;
            if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(serverName))
                return false;

            var header = "[mcp_servers." + serverName + "]";
            var searchFrom = 0;
            while (searchFrom <= content.Length - header.Length)
            {
                var idx = content.IndexOf(header, searchFrom, StringComparison.Ordinal);
                if (idx < 0)
                    return false;

                if (idx == 0 || content[idx - 1] == '\n')
                {
                    startIdx = idx;
                    break;
                }

                searchFrom = idx + 1;
            }

            if (startIdx < 0)
                return false;

            var afterHeader = startIdx + header.Length;
            var nextSection = content.IndexOf("\n[", afterHeader, StringComparison.Ordinal);
            endIdx = nextSection >= 0 ? nextSection + 1 : content.Length;
            return true;
        }

        private static bool TomlSectionPointsAtLoopback(string section)
        {
            var match = Regex.Match(section, "url\\s*=\\s*\"([^\"]*)\"");
            // No parseable url -> no evidence the section is still the one we wrote -> keep it.
            return match.Success && IsLoopbackUrl(match.Groups[1].Value);
        }

        /// <summary>Returns the entry name written, or empty when no config file was rewritten.</summary>
        private string ConfigureLMStudioTarget(MCPConfigTarget target)
        {
            var existingPaths = GetExistingLMStudioConfigPaths(GetUserHomePath());

            // One name for everything LM Studio touches. Resolving per config file could
            // hash-suffix one copy and not another, splitting this project's identity across LM
            // Studio's config copies -- and with only one written name recorded, the cleanup
            // could never retire the others. The deep link needs the shared name too: its target
            // carries a display path (possibly several paths joined for the UI), so resolving
            // against it would always see an unreadable file and never add a needed hash.
            var preferredKey = GetPreferredServerKey();
            var expectedUrl = GetServerUrl();
            var namesAcrossAllFiles = new HashSet<string>(StringComparer.Ordinal);
            var preferredEntryPointsAtCurrentUrl = false;
            foreach (var configPath in existingPaths)
            {
                var fileTarget = target;
                fileTarget.ConfigPath = configPath;
                namesAcrossAllFiles.UnionWith(ReadMaxyMCPEntryNames(fileTarget));
                preferredEntryPointsAtCurrentUrl |=
                    TargetEntryPointsAtUrl(fileTarget, preferredKey, expectedUrl);
            }

            var serverKey = ResolveServerKey(
                _settings.GetLastClientConfigKey(target.Name),
                namesAcrossAllFiles,
                preferredEntryPointsAtCurrentUrl);

            OpenLMStudioAddMCPLink(target, serverKey);

            var wroteAnyFile = false;
            foreach (var configPath in existingPaths)
            {
                var fileTarget = target;
                fileTarget.ConfigPath = configPath;
                ConfigureJsonTarget(fileTarget, serverKey);
                wroteAnyFile = true;
            }

            return wroteAnyFile ? serverKey : string.Empty;
        }

        private static bool TargetEntryPointsAtUrl(
            MCPConfigTarget target, string serverKey, string expectedUrl)
        {
            try
            {
                if (!File.Exists(target.ConfigPath))
                    return false;

                return ConfigEntryPointsAtUrl(
                    File.ReadAllText(target.ConfigPath),
                    target.IsToml,
                    target.RootKey,
                    serverKey,
                    expectedUrl,
                    target.UrlFieldName);
            }
            catch (Exception)
            {
                // The write path will report an unreadable or malformed config. Here, lack of a
                // readable exact URL simply means there is no ownership evidence.
                return false;
            }
        }

        /// <summary>
        /// Checks whether a named entry points at the exact endpoint this project would write. LM
        /// Studio's first-time deep link may create the entry after Unity returns without giving us a
        /// write receipt; finding the same key and URL on the next Configure is the evidence that the
        /// entry came from that deep link rather than from another project.
        /// </summary>
        internal static bool ConfigEntryPointsAtUrl(
            string content,
            bool isToml,
            string rootKey,
            string serverKey,
            string expectedUrl,
            string urlFieldName = null)
        {
            if (string.IsNullOrEmpty(content) ||
                string.IsNullOrEmpty(serverKey) ||
                string.IsNullOrEmpty(expectedUrl))
            {
                return false;
            }

            if (isToml)
            {
                int startIdx;
                int endIdx;
                if (!TryFindTomlSection(content, serverKey, out startIdx, out endIdx))
                    return false;

                var section = content.Substring(startIdx, endIdx - startIdx);
                var match = Regex.Match(section, "url\\s*=\\s*\"([^\"]*)\"");
                return match.Success &&
                       string.Equals(match.Groups[1].Value, expectedUrl, StringComparison.OrdinalIgnoreCase);
            }

            var parsed = SimpleJsonHelper.Deserialize(content) as Dictionary<string, object>;
            object serversValue;
            var effectiveRootKey = string.IsNullOrEmpty(rootKey) ? "mcpServers" : rootKey;
            if (parsed == null || !parsed.TryGetValue(effectiveRootKey, out serversValue))
                return false;

            var servers = serversValue as Dictionary<string, object>;
            object entryValue;
            if (servers == null || !servers.TryGetValue(serverKey, out entryValue))
                return false;

            var entry = entryValue as Dictionary<string, object>;
            object urlValue;
            return entry != null &&
                   entry.TryGetValue(GetUrlFieldName(urlFieldName), out urlValue) &&
                   string.Equals(urlValue as string, expectedUrl, StringComparison.OrdinalIgnoreCase);
        }

        private void OpenLMStudioAddMCPLink(MCPConfigTarget target, string serverKey)
        {
            var config = SimpleJsonHelper.Serialize(CreateHttpEntry(target));
            var encodedConfig = Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(config)));
            Application.OpenURL(
                $"lmstudio://add_mcp?name={Uri.EscapeDataString(serverKey)}&config={encodedConfig}");
        }

        private string BuildLMStudioConfiguredMessage()
        {
            var existingPaths = GetExistingLMStudioConfigPaths(GetUserHomePath());
                var message = "已为 MaxyMCP 打开 LM Studio 的 MCP 添加链接。\n\n";

            if (existingPaths.Count > 0)
            {
                message += "同时更新了现有的 LM Studio 配置文件：\n" +
                           string.Join("\n", existingPaths) +
                           "\n\nPlease restart LM Studio or reload MCP integrations if needed.";
            }
            else
            {
                message += "未找到现有的 LM Studio mcp.json 文件，因此没有创建猜测路径。\n\n" +
                           "如果 LM Studio 没有自动打开，请在 LM Studio > Program > Install > Edit mcp.json 中手动添加 MaxyMCP。";
            }

            return message;
        }

        private Dictionary<string, object> CreateHttpEntry(MCPConfigTarget target)
        {
            return CreateHttpEntry(
                GetServerUrl(),
                target.UrlFieldName,
                target.IncludeTypeField,
                target.TypeFieldValue,
                target.IncludeEnabledField);
        }

        internal static Dictionary<string, object> CreateHttpEntry(
            string serverUrl,
            string urlFieldName,
            bool includeTypeField,
            string typeFieldValue,
            bool includeEnabledField)
        {
            var entry = new Dictionary<string, object>
            {
                [GetUrlFieldName(urlFieldName)] = serverUrl
            };

            if (includeTypeField)
                entry["type"] = string.IsNullOrEmpty(typeFieldValue) ? "http" : typeFieldValue;

            if (includeEnabledField)
                entry["enabled"] = true;

            return entry;
        }

        /// <summary>
        /// The JSON key the endpoint lives under: <c>url</c> unless the target names another one
        /// (Antigravity's <c>serverUrl</c>). Every reader of an entry's endpoint must go through this
        /// too, or an entry written under the alternate key is invisible to it.
        /// </summary>
        internal static string GetUrlFieldName(string urlFieldName)
        {
            return string.IsNullOrEmpty(urlFieldName) ? "url" : urlFieldName;
        }

        private string CreateTomlSection(MCPConfigTarget target, string serverKey)
        {
            if (!target.IsToml)
                return string.Empty;

            return $"[mcp_servers.{serverKey}]\nurl = \"{GetServerUrl()}\"\n";
        }

        /// <summary>
        /// The entry name this project wants: derived from the project directory name, never
        /// hash-suffixed. <see cref="ResolveServerKeyForTarget"/> is what actually gets written.
        /// </summary>
        internal string GetPreferredServerKey()
        {
            return MaxyMCPServerKey.Build(
                GetProjectFolderName(),
                MaxyMCPProjectIdentity.FromProjectPath(GetProjectRootPath()),
                includeProjectHash: false);
        }

        /// <summary>
        /// Entry name to write into <paramref name="target"/>'s config. Normally the preferred name,
        /// but when that name is already in the config and this project is not the one that wrote it,
        /// a project hash is appended automatically: two projects with the same directory name would
        /// otherwise resolve to the same entry name and the second one configured would silently
        /// replace the first one's entry -- with nothing on either side to indicate it happened.
        /// Detecting the collision keeps names clean for everyone else instead of taxing every project
        /// with a hash it does not need (the hash costs 7 of the 25 characters a client tool name can
        /// spare).
        /// </summary>
        private string ResolveServerKeyForTarget(MCPConfigTarget target)
        {
            return ResolveServerKey(
                _settings.GetLastClientConfigKey(target.Name),
                ReadMaxyMCPEntryNames(target));
        }

        private string ResolveServerKey(
            string recordedKey,
            ICollection<string> existingEntryNames,
            bool preferredEntryPointsAtCurrentUrl = false)
        {
            var preferred = GetPreferredServerKey();
            if (!ShouldAddProjectHash(
                    preferred,
                    recordedKey,
                    existingEntryNames,
                    preferredEntryPointsAtCurrentUrl))
            {
                return preferred;
            }

            return MaxyMCPServerKey.Build(
                GetProjectFolderName(),
                MaxyMCPProjectIdentity.FromProjectPath(GetProjectRootPath()),
                includeProjectHash: true);
        }

        /// <summary>
        /// True when the preferred entry name is already taken by a project that is not this one.
        /// The recorded name is the normal ownership evidence: an entry this project wrote is ours
        /// to overwrite, anything else under that name belongs to another project. LM Studio's deep
        /// link is the exception because it creates the file outside Unity; an exact URL match on the
        /// next Configure is accepted as its write receipt.
        /// </summary>
        internal static bool ShouldAddProjectHash(
            string preferredKey,
            string recordedKey,
            ICollection<string> existingEntryNames,
            bool preferredEntryPointsAtCurrentUrl = false)
        {
            if (string.IsNullOrEmpty(preferredKey) || existingEntryNames == null)
                return false;

            if (string.Equals(recordedKey, preferredKey, StringComparison.Ordinal) ||
                preferredEntryPointsAtCurrentUrl)
            {
                return false;
            }

            return existingEntryNames.Contains(preferredKey);
        }

        internal static bool ShouldBlockConfigurationForFallback(
            bool isRunning, int resolvedPort, int activePort)
        {
            return isRunning && resolvedPort > 0 && activePort > 0 && resolvedPort != activePort;
        }

        private bool IsConfigurationBlockedByFallback()
        {
            return _server != null &&
                   ShouldBlockConfigurationForFallback(
                       _server.IsRunning, _server.ResolvedPort, _server.Port);
        }

        private void EnsureConfigurationEndpointIsSafe()
        {
            if (!IsConfigurationBlockedByFallback())
                return;

            throw new InvalidOperationException(BuildFallbackConfigurationBlockedMessage());
        }

        private string BuildFallbackConfigurationBlockedMessage()
        {
            return
                $"当前编辑器解析出的稳定端口为 {_server.ResolvedPort}，但实际使用回退端口 {_server.Port}，" +
                "因为稳定端口已被其他进程占用。写入稳定地址可能会把当前项目工具路由到其他进程；写入回退地址则会在重启后留下失效条目。" +
                "请点击“使用项目端口”或“固定当前端口”，等待服务器重启完成后再进行配置。";
        }

        private string GetServerUrl()
        {
            // ResolvedPort, deliberately not Port: the config file is persistent, so it must carry the
            // project's stable port identity. Port equals it except during a fallback bind, and baking
            // a transient fallback port into the config would leave a dead entry behind the moment the
            // conflict clears. WriteMCPConfigurationForTarget blocks every write while a fallback is
            // active, so this stable URL can never be persisted while another process owns it.
            // ISettingsController.MCPServerPort is only the stored override and is meaningless when
            // nothing was pinned.
            var port = _server != null ? _server.ResolvedPort : _settings.MCPServerPort;
            return $"http://127.0.0.1:{port}/";
        }

        /// <summary>
        /// Recognises the loopback URL shape this plugin writes. Must stay in sync with
        /// <see cref="GetServerUrl"/> -- an entry whose URL no longer matches is treated as
        /// hand-edited and never cleaned up.
        /// </summary>
        internal static bool IsLoopbackUrl(string url)
        {
            return !string.IsNullOrEmpty(url) &&
                   (url.StartsWith("http://127.0.0.1:", StringComparison.OrdinalIgnoreCase) ||
                    url.StartsWith("http://localhost:", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Project directory name, the source of the entry name. Preferred over
        /// <c>Application.productName</c>: the product name is often left at Unity's default or set to
        /// non-ASCII text, while the directory name always exists and is what developers call the
        /// project.
        /// </summary>
        private static string GetProjectFolderName()
        {
            return Path.GetFileName(GetProjectRootPath());
        }

        private static string GetProjectRootPath()
        {
            return Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath;
        }

        /// <summary>
        /// The path Claude Code actually uses as the <c>projects["&lt;path&gt;"]</c> key: the git
        /// repository root, not this Unity project's own directory. Verified empirically against the
        /// official <c>claude mcp add --scope local</c> CLI, which -- run from a Unity project folder
        /// that is itself a git subdirectory (a monorepo layout, e.g. this repo, where the git root is
        /// the parent of the Unity project) -- writes to <c>projects[gitRoot]</c>, not
        /// <c>projects[unityProjectPath]</c>. Writing under the Unity project path instead leaves the
        /// entry at a key Claude Code never reads for that session, so its tools silently never appear
        /// even though the config and the server are both otherwise correct.
        /// Walks upward from the Unity project directory looking for a <c>.git</c> entry (a directory
        /// for a normal repo, a file for a submodule/worktree); returns the first ancestor that has one,
        /// or falls back to the Unity project path itself if none is found (a project not under git).
        /// </summary>
        private static string GetProjectScopeKeyPath()
        {
            return FindGitRootOrSelf(GetProjectRootPath());
        }

        /// <summary>
        /// Walks upward from <paramref name="startDirectory"/> looking for a <c>.git</c> entry (a
        /// directory for a normal repo, a file for a submodule/worktree); returns the first ancestor
        /// that has one, or <paramref name="startDirectory"/> itself if none is found within the walk.
        /// Split out from <see cref="GetProjectScopeKeyPath"/> so the walk can be exercised in EditMode
        /// tests against real temporary directories instead of <c>Application.dataPath</c>.
        /// </summary>
        internal static string FindGitRootOrSelf(string startDirectory)
        {
            var dir = startDirectory;
            for (var depth = 0; depth < 64 && !string.IsNullOrEmpty(dir); depth++)
            {
                if (File.Exists(Path.Combine(dir, ".git")) || Directory.Exists(Path.Combine(dir, ".git")))
                    return dir;

                var parent = Directory.GetParent(dir);
                if (parent == null)
                    break;
                dir = parent.FullName;
            }

            return startDirectory;
        }

        /// <summary>
        /// OpenCode's entry goes into this repository's own <c>.opencode/opencode.json</c>, not the
        /// global <c>~/.config/opencode/opencode.json</c>. OpenCode merges every config location it
        /// finds (global <c>config.json</c> + <c>opencode.json</c> + <c>opencode.jsonc</c>, then the
        /// project's, then <c>.opencode</c> directories, later ones overriding conflicting keys), and
        /// it discovers the project file by walking upward from the directory the session was started
        /// in -- so a file at the repository root is reachable from anywhere inside the repo, while a
        /// global entry is visible to *every* session on the machine regardless of which project it
        /// was opened in. That global visibility is the same cross-project leak project-scoping the
        /// Claude Code entry fixes (see <see cref="GetOrCreateProjectScopedServers"/>): with two
        /// MaxyMCP projects configured, whichever Unity Editor happens to be running would expose its
        /// tools inside an unrelated project's OpenCode session, indistinguishable from that project's
        /// own entry. OpenCode is written per project for the same reason.
        /// The repository root is used rather than the Unity project directory so the entry is found
        /// whether OpenCode is started at the repo root or inside the Unity project folder (a monorepo
        /// layout, e.g. this repo, where the git root is the Unity project's parent); the walk is
        /// upward only, so the reverse placement would miss.
        /// </summary>
        private static string GetOpenCodeConfigPath()
        {
            return Path.Combine(GetProjectScopeKeyPath(), ".opencode", "opencode.json");
        }

        /// <summary>
        /// Keep Antigravity's MCP config, skills and AGENTS.md in one workspace. Use the nearest
        /// repository root (including worktrees), or the Unity project itself outside Git.
        /// See https://antigravity.google/docs/mcp/ for workspace config and serverUrl support.
        /// </summary>
        internal static string GetAntigravityWorkspaceRoot(string projectRoot)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new ArgumentException("A Unity project root is required.", nameof(projectRoot));
            return FindGitRootOrSelf(Path.GetFullPath(projectRoot));
        }

        internal static string GetAntigravityConfigPath(string projectRoot)
        {
            return Path.Combine(GetAntigravityWorkspaceRoot(projectRoot), ".agents", "mcp_config.json");
        }

        internal static MCPConfigTarget CreateAntigravityTarget(string projectRoot)
        {
            return new MCPConfigTarget
            {
                Name = "Antigravity",
                ConfigPath = GetAntigravityConfigPath(projectRoot),
                UrlFieldName = "serverUrl",
                ActivationHint =
                    "请在新版 Antigravity 中打开此工作区并重新加载 MCP 服务器：\n" +
                    GetAntigravityWorkspaceRoot(projectRoot) +
                    "\n嵌套 Unity 项目会使用其仓库根目录作为工作区。"
            };
        }

        // Older previews wrote a global entry. Merely finding a matching name is not proof that
        // it belongs to this project, so report existing entries without rewriting either file.
        internal static string GetAntigravityGlobalConfigNotice(string homePath)
        {
            var notices = new List<string>();
            foreach (var directory in new[] { "config", "antigravity" })
            {
                var path = Path.Combine(homePath, ".gemini", directory, "mcp_config.json");
                var names = ReadMaxyMCPEntryNames(new MCPConfigTarget { ConfigPath = path });
                if (names.Count > 0)
                    notices.Add($"{path}: {string.Join(", ", names.OrderBy(name => name, StringComparer.Ordinal))}");
            }

            return notices.Count == 0 ? string.Empty :
                "\n全局配置中还存在 MaxyMCP 条目，并且在其他工作区中仍然可见：\n" +
                string.Join("\n", notices) +
                "\n请在配置每个工作区后检查这些条目；配置操作不会修改全局文件。";
        }

        private static string GetUserHomePath()
        {
            var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(homePath))
                return homePath;

            var homeDrive = Environment.GetEnvironmentVariable("HOMEDRIVE");
            var homeDir = Environment.GetEnvironmentVariable("HOMEPATH");
            if (!string.IsNullOrEmpty(homeDrive) && !string.IsNullOrEmpty(homeDir))
                return homeDrive + homeDir;

            return Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        }

        /// <summary>
        /// Kimi Code's current format supports a project-local <c>.kimi-code/mcp.json</c>, which
        /// keeps this Unity project's loopback server out of unrelated Kimi sessions. Older Kimi CLI
        /// releases only load <c>~/.kimi/mcp.json</c>, so a machine with only that legacy data root
        /// keeps using it. A configured <c>KIMI_CODE_HOME</c> or an existing <c>~/.kimi-code</c>
        /// identifies the current client; when neither client has run yet, prefer the current format.
        /// </summary>
        internal static string GetKimiConfigPath(
            string homePath,
            string projectRoot,
            string kimiCodeHomeOverride)
        {
            var modernHome = !string.IsNullOrWhiteSpace(kimiCodeHomeOverride)
                ? kimiCodeHomeOverride.Trim()
                : Path.Combine(homePath, ".kimi-code");
            var legacyHome = Path.Combine(homePath, ".kimi");
            var modernDetected = !string.IsNullOrWhiteSpace(kimiCodeHomeOverride) || Directory.Exists(modernHome);
            var legacyDetected = Directory.Exists(legacyHome);

            if (modernDetected || !legacyDetected)
            {
                if (!string.IsNullOrWhiteSpace(projectRoot))
                    return Path.Combine(projectRoot, ".kimi-code", "mcp.json");

                return Path.Combine(modernHome, "mcp.json");
            }

            return Path.Combine(legacyHome, "mcp.json");
        }

        private static string GetLMStudioDisplayPath(string homePath)
        {
            var existingPaths = GetExistingLMStudioConfigPaths(homePath);
            if (existingPaths.Count > 0)
                return string.Join(" | ", existingPaths);

            return "LM Studio MCP 添加链接（备用路径：Program > Install > Edit mcp.json）";
        }

        private static List<string> GetExistingLMStudioConfigPaths(string homePath)
        {
            return GetLMStudioCandidateConfigPaths(homePath)
                .Where(File.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IEnumerable<string> GetLMStudioCandidateConfigPaths(string homePath)
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                yield return Path.Combine(homePath, ".cache", "lm-studio", "mcp.json");
                yield return Path.Combine(homePath, ".lmstudio", "mcp.json");
                yield break;
            }

            yield return Path.Combine(homePath, ".lmstudio", "mcp.json");
            yield return Path.Combine(homePath, ".cache", "lm-studio", "mcp.json");
        }

        private static string GetVSCodeConfigPath(string homePath)
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    if (!string.IsNullOrEmpty(appData))
                        return Path.Combine(appData, "Code", "User", "mcp.json");
                    break;

                case RuntimePlatform.OSXEditor:
                    var macPrimaryPath = Path.Combine(homePath, "Library", "Application Support", "Code", "User", "mcp.json");
                    var macPrimaryDirectory = Path.GetDirectoryName(macPrimaryPath);
                    if (File.Exists(macPrimaryPath) ||
                        (!string.IsNullOrEmpty(macPrimaryDirectory) && Directory.Exists(macPrimaryDirectory)))
                    {
                        return macPrimaryPath;
                    }

                    return Path.Combine(homePath, ".vscode", "mcp.json");

                case RuntimePlatform.LinuxEditor:
                    return Path.Combine(homePath, ".config", "Code", "User", "mcp.json");
            }

            return Path.Combine(homePath, ".vscode", "mcp.json");
        }

        internal struct MCPConfigTarget
        {
            public string Name;
            public string ConfigPath;
            public string ActivationHint;
            public string RootKey;
            public bool IsToml;
            public bool IncludeTypeField;
            public bool IsLMStudio;

            /// <summary>
            /// True only for DeepSeek Harness. Its entry lives in one or more profile patch files
            /// (<c>~/.dsh/profiles/&lt;profile&gt;/cordis.patch.yml</c>) written as a managed YAML
            /// block by <see cref="MaxyMCPDeepSeekHarnessPatch"/> -- not as a single JSON/TOML key
            /// like every other client here, so <see cref="ConfigPath"/> carries a display string
            /// and all reads/writes go through dedicated methods.
            /// </summary>
            public bool IsDeepSeekHarness;

            /// <summary>
            /// Value written for the <c>type</c> field when <see cref="IncludeTypeField"/> is set.
            /// Empty means "http" (the Claude Code / VS Code shape); OpenCode uses "remote".
            /// </summary>
            public string TypeFieldValue;

            /// <summary>
            /// Key the endpoint is written under inside the entry. Empty means <c>url</c>, which is
            /// what every client here uses except Antigravity: its <c>McpServerSpec</c> names the
            /// remote endpoint <c>serverUrl</c> and ignores an entry carrying neither that nor
            /// <c>command</c>.
            /// </summary>
            public string UrlFieldName;

            /// <summary>
            /// True for OpenCode, whose documented remote-server example spells the flag out
            /// (<c>{"type":"remote","url":...,"enabled":true}</c>). The field is optional in its schema
            /// and its default is not documented, so it is written explicitly rather than relied on.
            /// </summary>
            public bool IncludeEnabledField;

            /// <summary>
            /// True only for Claude Code. Its config file supports a <c>projects["&lt;path&gt;"]</c>
            /// section that Claude Code applies only to sessions opened at that path. Other clients
            /// write at the config's top level, which may itself be a workspace-local file.
            /// </summary>
            public bool UseProjectScope;
        }
    }
}
