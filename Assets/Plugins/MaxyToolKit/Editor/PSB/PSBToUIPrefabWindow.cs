using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MaxyToolKit.Editor
{
    /// <summary>
    /// PSB转UI预制体窗口，负责选择PSB、编辑生成配置、预览图层结构并执行生成
    /// </summary>
    public class PSBToUIPrefabWindow : EditorWindow
    {
        /// <summary>
        /// 配置在EditorPrefs里的存档键，仅影响本机编辑器
        /// </summary>
        private const string ConfigPrefsKey = "MaxyToolKit.PSBUIConfig";

        /// <summary>
        /// 预览树每级缩进的像素宽度
        /// </summary>
        private const float PreviewIndentWidth = 14f;

        /// <summary>
        /// 配置改动后重建预览的防抖时间，避免连续输入时反复解析PSB
        /// </summary>
        private const double PreviewRefreshDelay = 0.25d;

        /// <summary>
        /// 当前选择的PSB资产路径
        /// </summary>
        private string _psbPath = string.Empty;

        /// <summary>
        /// 当前选择的PSB资产对象，避免每帧都从路径重新加载
        /// </summary>
        private UnityEngine.Object _psbAsset;

        /// <summary>
        /// 当前生成配置
        /// </summary>
        private PSBUIConfig _config;

        /// <summary>
        /// 预览树节点列表，顺序与生成顺序一致
        /// </summary>
        private List<PSBUIEntry> _previewEntries = new List<PSBUIEntry>();

        /// <summary>
        /// 预览是否需要重建
        /// </summary>
        private bool _previewDirty = true;

        /// <summary>
        /// 最近一次标记预览失效的时间，用于防抖
        /// </summary>
        private double _previewDirtyTime;

        /// <summary>
        /// 主内容区滚动条位置
        /// </summary>
        private Vector2 _mainScroll;

        /// <summary>
        /// 预览区滚动条位置
        /// </summary>
        private Vector2 _previewScroll;

        /// <summary>
        /// 生成配置区是否展开
        /// </summary>
        private bool _showConfigFoldout = true;

        /// <summary>
        /// 命名规则区是否展开
        /// </summary>
        private bool _showRulesFoldout = true;

        /// <summary>
        /// 预览区是否展开
        /// </summary>
        private bool _showPreviewFoldout = true;

        /// <summary>
        /// 打开PSB转UI预制体窗口
        /// </summary>
        /// <param name="psbPath">要直接载入的PSB资产路径，为空时保持窗口原有状态</param>
        public static void Open(string psbPath = null)
        {
            var window = GetWindow<PSBToUIPrefabWindow>();
            window.titleContent = new GUIContent("PSB 转 UI 预制体");
            window.minSize = new Vector2(620f, 720f);
            //从Project右键菜单打开时直接载入目标PSB并刷新预览
            if (!string.IsNullOrEmpty(psbPath)) window.SetPsbPath(psbPath);
            window.Show();
        }

        /// <summary>
        /// 窗口启用时恢复上次保存的配置
        /// </summary>
        private void OnEnable()
        {
            _config = PSBUIConfig.CreateDefault();
            LoadPreferences(false);
            NormalizeConfig();
        }

        /// <summary>
        /// 窗口关闭或脚本重载时自动保存当前配置
        /// </summary>
        private void OnDisable()
        {
            SavePreferences();
        }

        /// <summary>
        /// 编辑器每帧回调，用于在防抖时间到达后补一次重绘
        /// </summary>
        private void Update()
        {
            if (!_previewDirty) return;
            //窗口可能因为配置改动而没有主动重绘，这里兜底触发一次
            if (EditorApplication.timeSinceStartup - _previewDirtyTime < PreviewRefreshDelay) return;
            Repaint();
        }

        /// <summary>
        /// 绘制窗口界面
        /// </summary>
        private void OnGUI()
        {
            //预览依赖PSB路径与配置，改动后等防抖时间结束再重建
            if (_previewDirty && EditorApplication.timeSinceStartup - _previewDirtyTime >= PreviewRefreshDelay) RefreshPreview();
            EditorGUILayout.Space(6f);
            DrawPsbSection();
            GUILayout.Space(6f);
            //配置区单独滚动，预览区再单独滚动，避免嵌套滚动视图抢滚轮事件
            _mainScroll = EditorGUILayout.BeginScrollView(_mainScroll, GUILayout.MinHeight(220f), GUILayout.ExpandHeight(true));
            DrawConfigSection();
            GUILayout.Space(6f);
            DrawRuleSection();
            EditorGUILayout.EndScrollView();
            GUILayout.Space(6f);
            DrawPreviewSection();
            //任意控件改动后都让预览在下一帧重新计算
            if (GUI.changed) MarkPreviewDirty();
            DrawGenerateSection();
        }

        /// <summary>
        /// 绘制顶部的PSB选择区
        /// </summary>
        private void DrawPsbSection()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            //.psb的主资产是GameObject或Texture2D，这里用通用Object字段接收拖拽，后缀校验在回调里做
            var picked = EditorGUILayout.ObjectField("PSB 文件", _psbAsset, typeof(UnityEngine.Object), false);
            if (EditorGUI.EndChangeCheck()) OnPsbObjectChanged(picked);
            if (GUILayout.Button(new GUIContent("选择选中项", "把Project窗口中当前选中的.psb作为输入"), GUILayout.Width(90f))) UseSelectedAsset();
            EditorGUILayout.EndHorizontal();
            //路径一行显示，方便确认当前操作的资产
            if (string.IsNullOrEmpty(_psbPath))
                EditorGUILayout.HelpBox("请把 .psb 文件拖到上面的输入框，或从 Project 窗口选中后点「选择选中项」", MessageType.Info);
            else
                EditorGUILayout.LabelField(_psbPath, EditorStyles.miniLabel);
        }

        /// <summary>
        /// 绘制生成配置区
        /// </summary>
        private void DrawConfigSection()
        {
            EditorGUILayout.BeginVertical("box");
            _showConfigFoldout = EditorGUILayout.Foldout(_showConfigFoldout, "生成配置", true);
            if (_showConfigFoldout)
            {
                EditorGUI.indentLevel++;
                //输出目录支持手动输入，也可以从磁盘浏览后转成项目相对路径
                EditorGUILayout.BeginHorizontal();
                _config.outputFolder = EditorGUILayout.TextField("输出目录", _config.outputFolder);
                if (GUILayout.Button("浏览", GUILayout.Width(60f))) BrowseOutputFolder();
                EditorGUILayout.EndHorizontal();
                _config.prefabName = EditorGUILayout.TextField("预制体名字", _config.prefabName);
                GUILayout.Space(4f);
                //命名规则是可选功能：默认关闭，只有勾选后才按名字转换按钮/文本/布局
                _config.applyRules = EditorGUILayout.Toggle("按命名生成组件（可选）", _config.applyRules);
                EditorGUILayout.LabelField("看图层的命名格式：比如「按钮：开始游戏」挂按钮、「滚动视图：背包」搭滚动视图结构，"
                    + "不勾选时全部按普通图片生成", EditorStyles.miniLabel);
                GUILayout.Space(4f);
                _config.createRootCanvas = EditorGUILayout.Toggle("创建 Canvas", _config.createRootCanvas);
                _config.useDocumentSize = EditorGUILayout.Toggle("根节点使用文档尺寸", _config.useDocumentSize);
                EditorGUILayout.LabelField(_config.useDocumentSize
                    ? "开启后根节点宽高直接取 PSB 文档尺寸，生成结果与 PSB 像素 1:1"
                    : "关闭后根节点宽高取参考分辨率，内部节点按比例整体缩放", EditorStyles.miniLabel);
                //参考分辨率只在创建 Canvas 或自定义根节点尺寸时才有意义，其它情况不展示
                if (_config.createRootCanvas || !_config.useDocumentSize)
                {
                    _config.referenceResolution = EditorGUILayout.Vector2Field("参考分辨率", _config.referenceResolution);
                    EditorGUILayout.LabelField(_config.createRootCanvas
                        ? "同时作为 CanvasScaler 的参考分辨率"
                        : "没有创建 Canvas 时作为根节点尺寸使用", EditorStyles.miniLabel);
                }
                GUILayout.Space(4f);
                _config.orderMode = (PSBUIOrderMode)EditorGUILayout.EnumPopup("图层顺序", _config.orderMode);
                EditorGUILayout.LabelField(GetOrderModeHint(_config.orderMode), EditorStyles.miniLabel);
                _config.hiddenMode = (PSBUIHiddenMode)EditorGUILayout.EnumPopup("隐藏图层", _config.hiddenMode);
                EditorGUILayout.LabelField(GetHiddenModeHint(_config.hiddenMode), EditorStyles.miniLabel);
                GUILayout.Space(4f);
                _config.applyOpacity = EditorGUILayout.Toggle("写入不透明度", _config.applyOpacity);
                EditorGUILayout.LabelField("把 PSB 图层不透明度写到图片透明度上", EditorStyles.miniLabel);
                _config.imageRaycastTarget = EditorGUILayout.Toggle("图片响应点击", _config.imageRaycastTarget);
                EditorGUILayout.LabelField("默认关闭，避免图片挡住按钮点击", EditorStyles.miniLabel);
                _config.generateGroups = EditorGUILayout.Toggle("生成组层级", _config.generateGroups);
                _config.flattenGroups = EditorGUILayout.Toggle("拍平组", _config.flattenGroups);
                EditorGUILayout.LabelField("生成组层级后每组对应一个空节点；拍平组会去掉中间层，子节点直接挂到根节点", EditorStyles.miniLabel);
                _config.logDetails = EditorGUILayout.Toggle("打印细节", _config.logDetails);
                GUILayout.Space(4f);
                DrawPresetButtons();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制配置预设按钮
        /// </summary>
        private void DrawPresetButtons()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("保存当前配置"))
            {
                SavePreferences();
                Debug.Log("MaxyToolKit：PSB 转 UI 预制体配置已保存");
            }
            if (GUILayout.Button("载入上次配置")) LoadPreferences(true);
            if (GUILayout.Button("恢复默认")) ResetToDefaultConfig();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("配置保存在本机 EditorPrefs，不会写进项目资源", EditorStyles.miniLabel);
        }

        /// <summary>
        /// 绘制命名规则列表
        /// </summary>
        private void DrawRuleSection()
        {
            EditorGUILayout.BeginVertical("box");
            _showRulesFoldout = EditorGUILayout.Foldout(_showRulesFoldout, "命名规则（可选）", true);
            if (_showRulesFoldout)
            {
                EditorGUI.indentLevel++;
                //规则表只在「按命名生成组件」勾选后参与生成，没勾选时整块置灰仍可编辑
                if (!_config.applyRules)
                    EditorGUILayout.HelpBox("当前没有启用命名规则：生成结果全部是普通图片节点。"
                        + "需要按钮/文本/布局时，先到「生成配置」里勾选「按命名生成组件（可选）」。", MessageType.None);
                EditorGUI.BeginDisabledGroup(!_config.applyRules);
                EditorGUILayout.LabelField("每行依次为：启用 / 匹配方式 / 匹配文本 / 结构 / 作用对象 / 保留图片 / 删除", EditorStyles.miniLabel);
                var removeIndex = -1;
                for (var index = 0; index < _config.rules.Count; index++)
                {
                    if (DrawRuleRow(_config.rules[index])) removeIndex = index;
                }
                //删除操作放到遍历之后，避免改动列表影响循环
                if (removeIndex >= 0)
                {
                    _config.rules.RemoveAt(removeIndex);
                    MarkPreviewDirty();
                }
                if (GUILayout.Button("添加规则")) _config.rules.Add(new PSBUIRule());
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制一条命名规则
        /// </summary>
        /// <param name="rule">要绘制的规则</param>
        /// <returns>true表示用户点了删除</returns>
        private static bool DrawRuleRow(PSBUIRule rule)
        {
            if (rule == null) return false;
            var delete = false;
            EditorGUILayout.BeginHorizontal();
            rule.enabled = GUILayout.Toggle(rule.enabled, new GUIContent(string.Empty, "是否启用该规则"), GUILayout.Width(18f));
            rule.matchMode = (PSBUIMatchMode)EditorGUILayout.EnumPopup(rule.matchMode, GUILayout.Width(92f));
            rule.pattern = EditorGUILayout.TextField(rule.pattern ?? string.Empty, GUILayout.MinWidth(80f));
            rule.kind = (PSBUIRuleKind)EditorGUILayout.EnumPopup(rule.kind, GUILayout.Width(112f));
            rule.target = (PSBUIRuleTarget)EditorGUILayout.EnumPopup(rule.target, GUILayout.Width(96f));
            rule.keepImage = GUILayout.Toggle(rule.keepImage, new GUIContent("图", "命中后是否保留图片"), GUILayout.Width(24f));
            if (GUILayout.Button("删除", GUILayout.Width(44f))) delete = true;
            EditorGUILayout.EndHorizontal();
            return delete;
        }

        /// <summary>
        /// 绘制预览树与节点排除勾选
        /// </summary>
        private void DrawPreviewSection()
        {
            EditorGUILayout.BeginVertical("box");
            _showPreviewFoldout = EditorGUILayout.Foldout(_showPreviewFoldout, "预览", true);
            if (_showPreviewFoldout)
            {
                EditorGUI.indentLevel++;
                var groupCount = 0;
                var excludedCount = 0;
                foreach (var entry in _previewEntries)
                {
                    if (entry.isGroup) groupCount++;
                    if (entry.excluded) excludedCount++;
                }
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("共 " + _previewEntries.Count + " 个节点，组 " + groupCount + " 个，已排除 " + excludedCount + " 个", EditorStyles.boldLabel);
                if (GUILayout.Button("重新扫描", GUILayout.Width(72f))) RefreshPreview();
                EditorGUILayout.EndHorizontal();
                if (_previewEntries.Count == 0)
                {
                    EditorGUILayout.HelpBox("没有可预览的节点；请确认已选择 .psb，并且 PSB 已用 PSD Importer 导入为 Multiple 精灵", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.LabelField("勾选左侧方块表示不生成该节点，取消勾选恢复", EditorStyles.miniLabel);
                    _previewScroll = EditorGUILayout.BeginScrollView(_previewScroll, GUILayout.Height(240f));
                    foreach (var entry in _previewEntries) DrawPreviewRow(entry);
                    EditorGUILayout.EndScrollView();
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制预览树里的一个节点
        /// </summary>
        /// <param name="entry">预览节点</param>
        private void DrawPreviewRow(PSBUIEntry entry)
        {
            if (entry == null) return;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(entry.depth * PreviewIndentWidth);
            //隐藏图层跳过与忽略规则属于强制排除，没有可编辑的路径，勾选框只做状态展示
            var canToggle = !entry.excluded || IsExcludedByConfig(entry);
            var toggleTip = canToggle ? "勾选后不生成该节点" : "该节点被隐藏图层设置或忽略规则强制排除，不能单独恢复";
            EditorGUI.BeginDisabledGroup(!canToggle);
            var exclude = GUILayout.Toggle(entry.excluded, new GUIContent(string.Empty, toggleTip), GUILayout.Width(16f));
            EditorGUI.EndDisabledGroup();
            //勾选表示排除，不生成该节点
            if (canToggle && exclude != entry.excluded) ToggleExcluded(entry, exclude);
            var oldColor = GUI.color;
            if (entry.excluded) GUI.color = new Color(0.62f, 0.62f, 0.62f);
            EditorGUILayout.LabelField(BuildPreviewLabel(entry), EditorStyles.miniLabel);
            GUI.color = oldColor;
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制底部生成按钮
        /// </summary>
        private void DrawGenerateSection()
        {
            GUILayout.Box(string.Empty, GUILayout.ExpandWidth(true), GUILayout.Height(1f));
            EditorGUILayout.LabelField("生成前会先检查 PSB 是否可以转换", EditorStyles.miniLabel);
            if (GUILayout.Button("生成预制体", GUILayout.Height(38f))) GeneratePrefab();
        }

        /// <summary>
        /// 把Project窗口当前选中的资产作为PSB输入
        /// </summary>
        private void UseSelectedAsset()
        {
            var selected = Selection.activeObject;
            var assetPath = selected == null ? string.Empty : AssetDatabase.GetAssetPath(selected);
            //选中项不是.psb时给出明确提示，不改变已有选择
            if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".psb", StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("提示", "请先在 Project 窗口里选中一个 .psb 文件", "确定");
                return;
            }
            SetPsbPath(assetPath);
        }

        /// <summary>
        /// 处理PSB输入框的改动，校验后缀并同步路径
        /// </summary>
        /// <param name="picked">输入框返回的资产对象</param>
        private void OnPsbObjectChanged(UnityEngine.Object picked)
        {
            if (picked == null)
            {
                //清空输入框时同步清空已选路径
                _psbPath = string.Empty;
                _psbAsset = null;
                MarkPreviewDirty();
                return;
            }
            var assetPath = AssetDatabase.GetAssetPath(picked);
            //只接受.psb文件，其它类型直接回退到原选择
            if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".psb", StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("提示", "请选择 .psb 文件", "确定");
                ReloadPsbAsset();
                return;
            }
            SetPsbPath(assetPath);
        }

        /// <summary>
        /// 设置当前PSB路径并刷新预览
        /// </summary>
        /// <param name="psbPath">PSB资产路径</param>
        private void SetPsbPath(string psbPath)
        {
            var previousFileName = string.IsNullOrEmpty(_psbPath) ? string.Empty : Path.GetFileNameWithoutExtension(_psbPath);
            _psbPath = psbPath ?? string.Empty;
            ReloadPsbAsset();
            //名字还是上一个PSB的自动名字时跟着新文件走，用户改过的名字不覆盖
            if (!string.IsNullOrEmpty(_psbPath) && _config != null)
            {
                var nameWasAuto = string.IsNullOrEmpty(_config.prefabName) || _config.prefabName == previousFileName;
                if (nameWasAuto) _config.prefabName = Path.GetFileNameWithoutExtension(_psbPath);
            }
            MarkPreviewDirty();
            Repaint();
        }

        /// <summary>
        /// 按当前路径重新加载PSB资产对象
        /// </summary>
        private void ReloadPsbAsset()
        {
            _psbAsset = string.IsNullOrEmpty(_psbPath) ? null : AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(_psbPath);
        }

        /// <summary>
        /// 标记预览需要重建并记录防抖时间
        /// </summary>
        private void MarkPreviewDirty()
        {
            _previewDirty = true;
            _previewDirtyTime = EditorApplication.timeSinceStartup;
        }

        /// <summary>
        /// 重新调用生成器构建预览树
        /// </summary>
        private void RefreshPreview()
        {
            _previewDirty = false;
            _previewEntries = new List<PSBUIEntry>();
            if (string.IsNullOrEmpty(_psbPath) || !_psbPath.EndsWith(".psb", StringComparison.OrdinalIgnoreCase)) return;
            try
            {
                var entries = PSBToUIPrefab.BuildPreview(_psbPath, _config);
                if (entries != null) _previewEntries = entries;
            }
            catch (Exception exception)
            {
                //预览失败不打断窗口，只记录警告并保持空列表
                Debug.LogWarning("MaxyToolKit：生成PSB预览失败：" + exception.Message);
            }
        }

        /// <summary>
        /// 判断节点的排除是否来自配置里的排除路径，只有这种排除可以由勾选框取消
        /// </summary>
        /// <param name="entry">预览节点</param>
        /// <returns>true表示排除来自配置，可以在界面上取消</returns>
        private bool IsExcludedByConfig(PSBUIEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.path)) return false;
            return _config.excludedPaths != null && _config.excludedPaths.Contains(entry.path);
        }

        /// <summary>
        /// 切换单个节点的排除状态并写回配置
        /// </summary>
        /// <param name="entry">预览节点</param>
        /// <param name="exclude">true表示排除该节点</param>
        private void ToggleExcluded(PSBUIEntry entry, bool exclude)
        {
            entry.excluded = exclude;
            if (_config.excludedPaths == null) _config.excludedPaths = new List<string>();
            if (exclude)
            {
                if (!_config.excludedPaths.Contains(entry.path)) _config.excludedPaths.Add(entry.path);
            }
            else
            {
                _config.excludedPaths.Remove(entry.path);
            }
            Repaint();
        }

        /// <summary>
        /// 拼出预览行的显示文本
        /// </summary>
        /// <param name="entry">预览节点</param>
        /// <returns>带层级图标与命中信息的文本</returns>
        private static string BuildPreviewLabel(PSBUIEntry entry)
        {
            var builder = new StringBuilder();
            builder.Append(entry.isGroup ? "[组] " : entry.hasSprite ? "[图] " : "[空] ");
            builder.Append(string.IsNullOrEmpty(entry.name) ? "(未命名)" : entry.name);
            if (entry.hidden) builder.Append(" (隐藏)");
            if (entry.size.x >= 1f || entry.size.y >= 1f)
                builder.Append("  ").Append(Mathf.RoundToInt(entry.size.x)).Append("x").Append(Mathf.RoundToInt(entry.size.y));
            if (entry.ruleKind != PSBUIRuleKind.None) builder.Append(" → ").Append(GetRuleKindLabel(entry.ruleKind));
            if (!string.IsNullOrEmpty(entry.matchedRule)) builder.Append("（规则：").Append(entry.matchedRule).Append("）");
            if (entry.excluded) builder.Append("（已排除）");
            return builder.ToString();
        }

        /// <summary>
        /// 获取图层顺序模式的中文说明
        /// </summary>
        /// <param name="orderMode">图层顺序模式</param>
        /// <returns>一行中文说明</returns>
        private static string GetOrderModeHint(PSBUIOrderMode orderMode)
        {
            switch (orderMode)
            {
                case PSBUIOrderMode.PsbTopIsFront:
                    return "PSB 上层在前：PSB 里越靠上的图层在 UI 里越靠前，默认且推荐";
                case PSBUIOrderMode.KeepImportOrder:
                    return "保持导入顺序：完全照搬导入顺序，不再翻转";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// 获取隐藏图层处理模式的中文说明
        /// </summary>
        /// <param name="hiddenMode">隐藏图层处理模式</param>
        /// <returns>一行中文说明</returns>
        private static string GetHiddenModeHint(PSBUIHiddenMode hiddenMode)
        {
            switch (hiddenMode)
            {
                case PSBUIHiddenMode.Skip:
                    return "跳过：隐藏图层不生成任何节点";
                case PSBUIHiddenMode.Inactive:
                    return "生成但不激活：节点保留在层级里，默认未激活";
                case PSBUIHiddenMode.KeepActive:
                    return "保持激活：只把透明度按 PSB 值写入，节点仍然激活";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// 获取规则结构的中文名字
        /// </summary>
        /// <param name="kind">规则结构</param>
        /// <returns>用于预览显示的中文名字</returns>
        private static string GetRuleKindLabel(PSBUIRuleKind kind)
        {
            switch (kind)
            {
                case PSBUIRuleKind.Button: return "按钮";
                case PSBUIRuleKind.Text: return "文本";
                case PSBUIRuleKind.VerticalLayout: return "竖排容器";
                case PSBUIRuleKind.HorizontalLayout: return "横排容器";
                case PSBUIRuleKind.GridLayout: return "网格容器";
                case PSBUIRuleKind.RectMask2D: return "矩形遮罩";
                case PSBUIRuleKind.CanvasGroup: return "透明度组";
                case PSBUIRuleKind.ScrollRect: return "滚动视图";
                case PSBUIRuleKind.Ignore: return "跳过";
                default: return "普通图片";
            }
        }

        /// <summary>
        /// 弹出目录选择框并把结果转成项目内相对路径
        /// </summary>
        private void BrowseOutputFolder()
        {
            var startFolder = Application.dataPath;
            //优先从当前输出目录开始浏览，目录不存在时回退到Assets
            if (!string.IsNullOrEmpty(_config.outputFolder))
            {
                var projectRoot = Path.GetDirectoryName(Application.dataPath);
                if (!string.IsNullOrEmpty(projectRoot))
                {
                    var candidate = Path.Combine(projectRoot, _config.outputFolder);
                    if (Directory.Exists(candidate)) startFolder = candidate;
                }
            }
            var picked = EditorUtility.OpenFolderPanel("选择预制体输出目录", startFolder, string.Empty);
            if (string.IsNullOrEmpty(picked)) return;
            var relativePath = ToProjectRelativePath(picked);
            //项目外或Assets外的目录不能作为资源输出位置
            if (string.IsNullOrEmpty(relativePath))
            {
                EditorUtility.DisplayDialog("提示", "请选择项目 Assets 目录下的文件夹", "确定");
                return;
            }
            _config.outputFolder = relativePath;
            GUI.changed = true;
        }

        /// <summary>
        /// 把磁盘绝对路径转换成项目内相对路径
        /// </summary>
        /// <param name="absolutePath">磁盘绝对路径</param>
        /// <returns>形如Assets/Prefabs/UI的相对路径，不在项目Assets下时返回空字符串</returns>
        private static string ToProjectRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) return string.Empty;
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot)) return string.Empty;
            var fullPath = Path.GetFullPath(absolutePath).Replace('\\', '/');
            var rootPath = Path.GetFullPath(projectRoot).Replace('\\', '/').TrimEnd('/');
            //只接受项目目录内的路径
            if (!fullPath.StartsWith(rootPath + "/", StringComparison.OrdinalIgnoreCase)) return string.Empty;
            var relativePath = fullPath.Substring(rootPath.Length + 1);
            //输出目录必须在Assets下才能作为Unity资源路径
            if (!relativePath.Equals("Assets", StringComparison.OrdinalIgnoreCase)
                && !relativePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)) return string.Empty;
            return relativePath;
        }

        /// <summary>
        /// 把当前配置以JSON写入EditorPrefs
        /// </summary>
        private void SavePreferences()
        {
            if (_config == null) return;
            try
            {
                EditorPrefs.SetString(ConfigPrefsKey, JsonUtility.ToJson(_config));
            }
            catch (Exception exception)
            {
                //存档失败不打断窗口，只记录警告
                Debug.LogWarning("MaxyToolKit：保存PSB配置失败：" + exception.Message);
            }
        }

        /// <summary>
        /// 从EditorPrefs载入上次保存的配置
        /// </summary>
        /// <param name="showWarning">没有存档或读取失败时是否弹窗提示</param>
        private void LoadPreferences(bool showWarning)
        {
            var json = EditorPrefs.GetString(ConfigPrefsKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                if (showWarning) EditorUtility.DisplayDialog("提示", "还没有保存过配置，先点「保存当前配置」", "确定");
                return;
            }
            PSBUIConfig loaded = null;
            try
            {
                loaded = JsonUtility.FromJson<PSBUIConfig>(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("MaxyToolKit：读取PSB配置失败：" + exception.Message);
            }
            //存档可能来自旧版本，解析失败时保留当前配置
            if (loaded == null)
            {
                if (showWarning) EditorUtility.DisplayDialog("提示", "配置读取失败，已保留当前配置", "确定");
                return;
            }
            _config = loaded;
            NormalizeConfig();
            MarkPreviewDirty();
            Repaint();
        }

        /// <summary>
        /// 修补配置里可能为空或缺失的字段，兼容旧存档
        /// </summary>
        private void NormalizeConfig()
        {
            if (_config == null) _config = PSBUIConfig.CreateDefault();
            if (_config.rules == null) _config.rules = new List<PSBUIRule>();
            if (_config.excludedPaths == null) _config.excludedPaths = new List<string>();
            if (string.IsNullOrEmpty(_config.outputFolder)) _config.outputFolder = "Assets/Prefabs/UI";
            if (_config.prefabName == null) _config.prefabName = string.Empty;
            //旧存档可能没有参考分辨率，补回默认值避免生成零尺寸画布
            if (_config.referenceResolution.x <= 0f || _config.referenceResolution.y <= 0f)
                _config.referenceResolution = new Vector2(1920f, 1080f);
            _config.rules.RemoveAll(rule => rule == null);
            foreach (var rule in _config.rules)
            {
                if (rule.pattern == null) rule.pattern = string.Empty;
            }
            _config.excludedPaths.RemoveAll(path => path == null);
        }

        /// <summary>
        /// 把配置恢复到默认值，已有PSB时沿用它的文件名
        /// </summary>
        private void ResetToDefaultConfig()
        {
            _config = string.IsNullOrEmpty(_psbPath) ? PSBUIConfig.CreateDefault() : PSBToUIPrefab.CreateConfig(_psbPath);
            NormalizeConfig();
            MarkPreviewDirty();
            Repaint();
        }

        /// <summary>
        /// 检查PSB并生成UI预制体，成功后选中新生成的预制体
        /// </summary>
        private void GeneratePrefab()
        {
            if (string.IsNullOrEmpty(_psbPath))
            {
                EditorUtility.DisplayDialog("提示", "请先选择要转换的 .psb 文件", "确定");
                return;
            }
            //先跑一遍校验，把PSB侧的问题挡在生成之前
            var problems = PSBToUIPrefab.Validate(_psbPath);
            if (problems != null && problems.Count > 0)
            {
                EditorUtility.DisplayDialog("无法生成", string.Join("\n", problems), "确定");
                return;
            }
            string prefabPath;
            string error;
            var success = PSBToUIPrefab.TryGenerate(_psbPath, _config, out prefabPath, out error);
            if (!success)
            {
                EditorUtility.DisplayDialog("生成失败", string.IsNullOrEmpty(error) ? "未知错误，请查看控制台" : error, "确定");
                return;
            }
            Debug.Log("MaxyToolKit：PSB 转 UI 预制体已生成：" + prefabPath);
            //生成后直接选中新资产，方便继续调整
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("MaxyToolKit：预制体已生成但无法加载：" + prefabPath);
                return;
            }
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }
    }
}
