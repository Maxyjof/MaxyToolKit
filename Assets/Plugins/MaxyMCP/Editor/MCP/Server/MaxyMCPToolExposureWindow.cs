// 版权归 MaxyMCP 所有，遵循 MIT 许可证。

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using MaxyMCP.Editor.DI;
using MaxyMCP.Editor.Services;
using MaxyMCP.Editor.Settings;
using MaxyMCP.Editor.Tools;

namespace MaxyMCP.Editor.MCP.Server
{
    internal class MaxyMCPToolExposureWindow : EditorWindow
    {
        private static readonly List<string> ProfileChoices = new List<string> { "core", "main", "full" };
        private static readonly List<string> ProfileDisplayChoices = new List<string> { "核心", "主要", "完整" };

        private ISettingsController _settingsController;
        private MCPServerService _mcpServer;
        private PopupField<string> _editProfileField;
        private Label _statusLabel;
        private Label _descriptionLabel;
        private ScrollView _toolScrollView;
        private List<string> _allToolNames = new List<string>();
        private readonly Dictionary<string, string> _toolCategories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Toggle> _toolToggles = new Dictionary<string, Toggle>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Foldout> _categoryFoldouts = new Dictionary<string, Foldout>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _editingTools = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string _editingProfile = "core";

        [MenuItem("MaxyMCP/工具暴露")]
        public static void ShowWindow()
        {
            var window = GetWindow<MaxyMCPToolExposureWindow>(MaxyMCPLocalization.T("Tool Exposure"));
            window.minSize = new Vector2(460, 560);
            window.Show();
        }

        /// <summary>
        /// 打开窗口并预先选中要编辑的工具配置档，例如 MCP 服务器窗口当前使用的配置档。
        /// </summary>
        public static void ShowWindow(string editProfile)
        {
            var window = GetWindow<MaxyMCPToolExposureWindow>(MaxyMCPLocalization.T("Tool Exposure"));
            window.minSize = new Vector2(460, 560);
            if (!string.IsNullOrEmpty(editProfile) && ProfileChoices.Contains(editProfile))
            {
                window._editingProfile = editProfile;
                // 如果窗口已经创建，则立即同步下拉框；设置 PopupField 的值会触发回调并重新加载工具列表。
                // 新创建的窗口尚未执行 CreateGUI 时字段为空，BuildUI 会直接读取 _editingProfile。
                if (window._editProfileField != null)
                    window._editProfileField.value = GetProfileDisplayName(editProfile);
            }
            window.Show();
        }

        public void CreateGUI()
        {
            _settingsController = RootScopeServices.Services?.GetService(typeof(ISettingsController))
                as ISettingsController;
            _mcpServer = RootScopeServices.Services?.GetService(typeof(MCPServerService))
                as MCPServerService;

            if (_settingsController == null)
            {
                rootVisualElement.Add(new Label(MaxyMCPLocalization.T("Failed to initialize services.")));
                return;
            }

            _settingsController.OnSettingsChanged += RefreshStatus;
            BuildUI();
        }

        private void OnDestroy()
        {
            if (_settingsController != null)
                _settingsController.OnSettingsChanged -= RefreshStatus;
        }

        private void BuildUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexGrow = 1;
            rootVisualElement.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
            rootVisualElement.style.paddingLeft = 10;
            rootVisualElement.style.paddingRight = 10;
            rootVisualElement.style.paddingTop = 10;
            rootVisualElement.style.paddingBottom = 10;

            var title = new Label(MaxyMCPLocalization.T("Tool Exposure"));
            title.style.fontSize = 17;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            title.style.marginBottom = 4;
            rootVisualElement.Add(title);

            var hint = new Label(MaxyMCPLocalization.T("Edit exactly which tools each MCP profile exposes. Choose the active profile from the MCP Server window. Saving changes restarts the running server automatically."));
            hint.style.fontSize = 11;
            hint.style.color = new Color(0.65f, 0.65f, 0.65f);
            hint.style.whiteSpace = WhiteSpace.Normal;
            hint.style.marginBottom = 10;
            rootVisualElement.Add(hint);

            LoadAllTools();

            var activeProfile = GetActiveProfile();
            _editingProfile = ProfileChoices.Contains(_editingProfile) ? _editingProfile : activeProfile;
            _editProfileField = new PopupField<string>(MaxyMCPLocalization.T("Edit Tool List"), ProfileDisplayChoices, ProfileChoices.IndexOf(_editingProfile));
            _editProfileField.style.marginBottom = 8;
            _editProfileField.RegisterValueChangedCallback(evt =>
            {
                _editingProfile = GetProfileId(evt.newValue);
                LoadEditingTools();
                RebuildToolList();
                RefreshStatus();
            });
            rootVisualElement.Add(_editProfileField);

            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.marginBottom = 10;

            var selectAllButton = CreateActionButton(MaxyMCPLocalization.T("Select All"), SelectAllTools, 88, new Color(0.24f, 0.42f, 0.58f));
            buttonRow.Add(selectAllButton);

            var clearButton = CreateActionButton(MaxyMCPLocalization.T("Clear"), ClearTools, 64, new Color(0.46f, 0.36f, 0.24f));
            clearButton.style.marginLeft = 6;
            buttonRow.Add(clearButton);

            var defaultButton = CreateActionButton(MaxyMCPLocalization.T("Use Default"), UseDefaultTools, 92, new Color(0.34f, 0.34f, 0.34f));
            defaultButton.style.marginLeft = 6;
            buttonRow.Add(defaultButton);

            var saveButton = CreateActionButton(MaxyMCPLocalization.T("Save"), SaveEditingTools, 64, new Color(0.2f, 0.5f, 0.3f));
            saveButton.style.marginLeft = 6;
            buttonRow.Add(saveButton);

            rootVisualElement.Add(buttonRow);

            _statusLabel = new Label();
            _statusLabel.style.fontSize = 11;
            _statusLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
            _statusLabel.style.marginBottom = 6;
            rootVisualElement.Add(_statusLabel);

            _descriptionLabel = new Label();
            _descriptionLabel.style.fontSize = 11;
            _descriptionLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
            _descriptionLabel.style.color = new Color(0.68f, 0.68f, 0.68f);
            _descriptionLabel.style.whiteSpace = WhiteSpace.Normal;
            rootVisualElement.Add(_descriptionLabel);

            _toolScrollView = new ScrollView(ScrollViewMode.Vertical);
            _toolScrollView.style.flexGrow = 1;
            _toolScrollView.style.backgroundColor = new Color(0.14f, 0.14f, 0.14f);
            _toolScrollView.style.borderTopLeftRadius = 4;
            _toolScrollView.style.borderTopRightRadius = 4;
            _toolScrollView.style.borderBottomLeftRadius = 4;
            _toolScrollView.style.borderBottomRightRadius = 4;
            _toolScrollView.style.paddingLeft = 6;
            _toolScrollView.style.paddingRight = 6;
            _toolScrollView.style.paddingTop = 5;
            _toolScrollView.style.paddingBottom = 5;
            rootVisualElement.Add(_toolScrollView);

            LoadEditingTools();
            RebuildToolList();
            RefreshStatus();
        }

        private Button CreateActionButton(string text, Action action, int width, Color color)
        {
            var button = new Button(action);
            button.text = text;
            button.style.height = 26;
            button.style.width = width;
            button.style.backgroundColor = color;
            button.style.color = Color.white;
            return button;
        }

        private void LoadAllTools()
        {
            _toolCategories.Clear();

            _allToolNames = ToolSchemaBuilder.BuildAll()
                .Select(tool => tool.function.name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var toolName in _allToolNames)
                _toolCategories[toolName] = GetToolCategory(toolName);
        }

        private void LoadEditingTools()
        {
            _editingTools = new HashSet<string>(GetEffectiveTools(_editingProfile), StringComparer.OrdinalIgnoreCase);
        }

        private IEnumerable<string> GetEffectiveTools(string profile)
        {
            if (string.Equals(profile, "full", StringComparison.OrdinalIgnoreCase))
            {
                return _settingsController.MCPFullToolsConfigured
                    ? _settingsController.MCPFullTools
                    : _allToolNames;
            }

            if (string.Equals(profile, "main", StringComparison.OrdinalIgnoreCase))
            {
                return _settingsController.MCPMainToolsConfigured
                    ? _settingsController.MCPMainTools
                    : MCPToolExportPolicy.DefaultCoreTools.Where(tool => _allToolNames.Contains(tool, StringComparer.OrdinalIgnoreCase));
            }

            return _settingsController.MCPCoreToolsConfigured
                ? _settingsController.MCPCoreTools
                : MCPToolExportPolicy.DefaultCoreTools.Where(tool => _allToolNames.Contains(tool, StringComparer.OrdinalIgnoreCase));
        }

        private bool IsEditingProfileConfigured()
        {
            return string.Equals(_editingProfile, "full", StringComparison.OrdinalIgnoreCase)
                ? _settingsController.MCPFullToolsConfigured
                : string.Equals(_editingProfile, "main", StringComparison.OrdinalIgnoreCase)
                    ? _settingsController.MCPMainToolsConfigured
                : _settingsController.MCPCoreToolsConfigured;
        }

        private string GetActiveProfile()
        {
            var currentProfile = MCPToolExportPolicy.ToSettingValue(
                MCPToolExportPolicy.Parse(_settingsController.MCPToolExportProfile));
            return ProfileChoices.Contains(currentProfile) ? currentProfile : "core";
        }

        private static string GetProfileDisplayName(string profile)
        {
            if (string.Equals(profile, "full", StringComparison.OrdinalIgnoreCase))
                return "完整";
            if (string.Equals(profile, "main", StringComparison.OrdinalIgnoreCase))
                return "主要";
            return "核心";
        }

        private static string GetProfileId(string displayName)
        {
            if (string.Equals(displayName, "完整", StringComparison.Ordinal))
                return "full";
            if (string.Equals(displayName, "主要", StringComparison.Ordinal))
                return "main";
            return "core";
        }

        private void RebuildToolList()
        {
            if (_toolScrollView == null)
                return;

            _toolScrollView.contentContainer.Clear();
            _toolToggles.Clear();
            _categoryFoldouts.Clear();

            var groupedTools = _allToolNames
                .GroupBy(GetCachedToolCategory)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var group in groupedTools)
            {
                var categoryTools = group
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                _toolScrollView.Add(CreateCategorySection(group.Key, categoryTools));
            }
        }

        private VisualElement CreateCategorySection(string category, IReadOnlyList<string> categoryTools)
        {
            var selectedCount = categoryTools.Count(tool => _editingTools.Contains(tool));

            var foldout = new Foldout
            {
                text = $"{MaxyMCPLocalization.CategoryName(category)}（{selectedCount}/{categoryTools.Count}）",
                value = false,
                tooltip = MaxyMCPLocalization.CategoryTooltip(category)
            };
            foldout.style.flexGrow = 1;
            // Unity 默认会把 Foldout 标题显示为粗体；分类标题不是强调内容，统一使用常规字重。
            foldout.style.unityFontStyleAndWeight = FontStyle.Normal;
            var foldoutLabel = foldout.Q<Label>();
            if (foldoutLabel != null)
                foldoutLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
            _categoryFoldouts[category] = foldout;

            var section = new VisualElement();
            section.style.marginBottom = 5;

            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.alignItems = Align.Center;
            headerRow.style.marginBottom = 2;
            headerRow.Add(foldout);

            var actions = new VisualElement();
            actions.style.flexDirection = FlexDirection.Row;
            actions.style.marginLeft = 6;
            actions.style.flexShrink = 0;

            var selectButton = CreateCategoryButton(MaxyMCPLocalization.T("Select"), () => SetCategoryTools(categoryTools, true));
            selectButton.tooltip = $"选中“{MaxyMCPLocalization.CategoryName(category)}”分类中的全部工具。";
            actions.Add(selectButton);

            var clearButton = CreateCategoryButton(MaxyMCPLocalization.T("Clear"), () => SetCategoryTools(categoryTools, false));
            clearButton.style.marginLeft = 4;
            clearButton.tooltip = $"清除“{MaxyMCPLocalization.CategoryName(category)}”分类中的全部工具。";
            actions.Add(clearButton);
            headerRow.Add(actions);
            section.Add(headerRow);

            var body = new VisualElement();
            body.style.display = DisplayStyle.None;
            body.style.marginLeft = 16;
            foldout.RegisterValueChangedCallback(evt =>
            {
                body.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            });
            section.Add(body);

            foreach (var toolName in categoryTools)
            {
                var toggle = new Toggle(MaxyMCPLocalization.ToolName(toolName));
                toggle.tooltip = MaxyMCPLocalization.ToolTooltip(toolName, category);
                toggle.SetValueWithoutNotify(_editingTools.Contains(toolName));
                toggle.style.marginLeft = 16;
                toggle.style.marginBottom = 2;
                toggle.style.unityFontStyleAndWeight = FontStyle.Normal;
                toggle.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue)
                        _editingTools.Add(toolName);
                    else
                        _editingTools.Remove(toolName);

                    RefreshCategoryVisuals();
                    RefreshStatus();
                });

                _toolToggles[toolName] = toggle;
                body.Add(toggle);
            }

            return section;
        }

        private Button CreateCategoryButton(string text, Action action)
        {
            var button = new Button(action);
            button.text = text;
            button.style.height = 20;
            button.style.width = 58;
            button.style.fontSize = 10;
            return button;
        }

        private void SetCategoryTools(IEnumerable<string> categoryTools, bool enabled)
        {
            foreach (var toolName in categoryTools)
            {
                if (enabled)
                    _editingTools.Add(toolName);
                else
                    _editingTools.Remove(toolName);
            }

            RefreshCategoryVisuals();
            RefreshStatus();
        }

        private void RefreshCategoryVisuals()
        {
            foreach (var entry in _toolToggles)
                entry.Value.SetValueWithoutNotify(_editingTools.Contains(entry.Key));

            foreach (var entry in _categoryFoldouts)
            {
                var category = entry.Key;
                var total = _allToolNames.Count(tool => string.Equals(GetCachedToolCategory(tool), category, StringComparison.OrdinalIgnoreCase));
                var selected = _allToolNames.Count(tool => string.Equals(GetCachedToolCategory(tool), category, StringComparison.OrdinalIgnoreCase) && _editingTools.Contains(tool));
                entry.Value.text = $"{MaxyMCPLocalization.CategoryName(category)}（{selected}/{total}）";
            }
        }

        private void SetAllToolToggles(bool enabled)
        {
            if (enabled)
                _editingTools = new HashSet<string>(_allToolNames, StringComparer.OrdinalIgnoreCase);
            else
                _editingTools.Clear();

            foreach (var entry in _toolToggles)
                entry.Value.SetValueWithoutNotify(_editingTools.Contains(entry.Key));

            RefreshCategoryVisuals();
            RefreshStatus();
        }

        private void SelectAllTools()
        {
            SetAllToolToggles(true);
        }

        private void ClearTools()
        {
            SetAllToolToggles(false);
        }

        private void UseDefaultTools()
        {
            if (string.Equals(_editingProfile, "full", StringComparison.OrdinalIgnoreCase))
                _settingsController.MCPFullTools = null;
            else if (string.Equals(_editingProfile, "main", StringComparison.OrdinalIgnoreCase))
                _settingsController.MCPMainTools = null;
            else
                _settingsController.MCPCoreTools = null;

            LoadEditingTools();
            RebuildToolList();
            RefreshStatus();
        }

        private void SaveEditingTools()
        {
            var selected = _editingTools
                .Where(tool => _allToolNames.Contains(tool, StringComparer.OrdinalIgnoreCase))
                .OrderBy(tool => tool, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (string.Equals(_editingProfile, "full", StringComparison.OrdinalIgnoreCase))
                _settingsController.MCPFullTools = selected;
            else if (string.Equals(_editingProfile, "main", StringComparison.OrdinalIgnoreCase))
                _settingsController.MCPMainTools = selected;
            else
                _settingsController.MCPCoreTools = selected;

            RefreshStatus();
        }

        private void RefreshStatus()
        {
            if (_settingsController == null)
                return;

            var activeProfile = GetActiveProfile();
            if (_editProfileField != null)
                _editProfileField.SetValueWithoutNotify(GetProfileDisplayName(_editingProfile));

            var selectedCount = _editingTools?.Count ?? 0;
            var totalCount = _allToolNames?.Count ?? 0;
            var source = IsEditingProfileConfigured() ? "custom" : "default";

            if (_statusLabel != null)
            {
                var serverState = _mcpServer != null && _mcpServer.IsRunning
                    ? $"{MaxyMCPLocalization.T("Server running on")} http://127.0.0.1:{_mcpServer.Port}/"
                    : MaxyMCPLocalization.T("Server stopped");
                _statusLabel.text = $"{MaxyMCPLocalization.T("Active")}：{GetProfileDisplayName(activeProfile)} | {MaxyMCPLocalization.T("Editing")} {GetProfileDisplayName(_editingProfile)}：{selectedCount}/{totalCount} {MaxyMCPLocalization.T("tools")}（{MaxyMCPLocalization.T(source)}）| {serverState}";
                _statusLabel.style.color = activeProfile == "full"
                    ? new Color(0.55f, 0.75f, 1f)
                    : new Color(0.55f, 0.85f, 0.55f);
            }

            if (_descriptionLabel != null)
            {
                if (string.Equals(_editingProfile, "full", StringComparison.OrdinalIgnoreCase))
                    _descriptionLabel.text = MaxyMCPLocalization.T("full defaults to every registered MCP tool. Select tools below and click Save to make full expose only that custom list.");
                else if (string.Equals(_editingProfile, "main", StringComparison.OrdinalIgnoreCase))
                    _descriptionLabel.text = MaxyMCPLocalization.T("main uses your selected primary tool set. Select tools below and click Save to override that list.");
                else
                    _descriptionLabel.text = MaxyMCPLocalization.T("core defaults to the focused Unity workflow tool set. Select tools below and click Save to override that list.");
            }
        }

        private string GetCachedToolCategory(string toolName)
        {
            return _toolCategories.TryGetValue(toolName, out var category) ? category : "Other";
        }

        private string GetToolCategory(string toolName)
        {
            if (ToolRegistry.MethodCache.TryGetValue(toolName, out var method))
            {
                var provider = method.DeclaringType?.GetCustomAttribute<ToolProviderAttribute>();
                return FormatCategory(provider?.Category ?? method.DeclaringType?.Name ?? "Other");
            }

            if (ToolRegistry.ManualTools.ContainsKey(toolName))
                return "Manual";

            return "Other";
        }

        private static string FormatCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return "Other";

            var trimmed = category.Trim();
            var result = new System.Text.StringBuilder();
            for (var i = 0; i < trimmed.Length; i++)
            {
                var current = trimmed[i];
                if (i > 0 &&
                    char.IsUpper(current) &&
                    !char.IsWhiteSpace(trimmed[i - 1]) &&
                    (char.IsLower(trimmed[i - 1]) || (i + 1 < trimmed.Length && char.IsLower(trimmed[i + 1]))))
                {
                    result.Append(' ');
                }

                result.Append(current);
            }

            return result.ToString();
        }
    }
}
