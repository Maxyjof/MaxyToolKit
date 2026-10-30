using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyToolKit.Editor
{
    /// <summary>
    /// PSB 图层名与生成规则的匹配方式
    /// </summary>
    public enum PSBUIMatchMode
    {
        /// <summary>名称以规则文本开头</summary>
        [InspectorName("开头匹配")]
        Prefix,
        /// <summary>名称以规则文本结尾</summary>
        [InspectorName("结尾匹配")]
        Suffix,
        /// <summary>名称包含规则文本</summary>
        [InspectorName("包含")]
        Contains,
        /// <summary>名称整体符合正则表达式</summary>
        [InspectorName("正则")]
        Regex
    }

    /// <summary>
    /// 命中规则后给节点附加的组件或结构
    /// </summary>
    public enum PSBUIRuleKind
    {
        /// <summary>只保留图片，不加组件</summary>
        [InspectorName("仅图片")]
        None,
        /// <summary>按钮，图片作为按钮目标图形</summary>
        [InspectorName("按钮")]
        Button,
        /// <summary>文本占位，用 TextMeshProUGUI 替换图片</summary>
        [InspectorName("文本占位")]
        Text,
        /// <summary>竖向排列容器</summary>
        [InspectorName("竖排列表")]
        VerticalLayout,
        /// <summary>横向排列容器</summary>
        [InspectorName("横排列表")]
        HorizontalLayout,
        /// <summary>网格排列容器</summary>
        [InspectorName("网格列表")]
        GridLayout,
        /// <summary>矩形遮罩，用于裁剪子节点</summary>
        [InspectorName("矩形遮罩")]
        RectMask2D,
        /// <summary>透明度控制组</summary>
        [InspectorName("透明组")]
        CanvasGroup,
        /// <summary>滚动视图，自动补齐视口与内容节点</summary>
        [InspectorName("滚动视图")]
        ScrollRect,
        /// <summary>横向滑动的窗口，按 Unity 默认滚动视图结构补齐视口、内容与横向滚动条</summary>
        [InspectorName("滑动窗口")]
        SlideWindow,
        /// <summary>滚动条，自动补齐滑动区与滑块</summary>
        [InspectorName("滚动条")]
        Scrollbar,
        /// <summary>开关，自动补齐背景与勾选图</summary>
        [InspectorName("开关")]
        Toggle,
        /// <summary>滑动条，自动补齐背景、填充与把手</summary>
        [InspectorName("滑动条")]
        Slider,
        /// <summary>输入框，自动补齐文本区、占位字与输入文本</summary>
        [InspectorName("输入框")]
        InputField,
        /// <summary>下拉框，自动补齐标题、箭头、模板、选项与滚动内容</summary>
        [InspectorName("下拉框")]
        Dropdown,
        /// <summary>开关组，挂 ToggleGroup 并把子开关登记进去</summary>
        [InspectorName("开关组")]
        ToggleGroup,
        /// <summary>页签组，ToggleGroup 加横向排列的默认组合</summary>
        [InspectorName("页签组")]
        TabGroup,
        /// <summary>图片遮罩，用该图片的形状裁剪子节点</summary>
        [InspectorName("图片遮罩")]
        Mask,
        /// <summary>原图，用 RawImage 显示 sprite 的原始贴图</summary>
        [InspectorName("原图")]
        RawImage,
        /// <summary>跳过该图层或组，不生成节点</summary>
        [InspectorName("跳过不生成")]
        Ignore
    }

    /// <summary>
    /// 规则作用对象是单个图层还是图层组
    /// </summary>
    public enum PSBUIRuleTarget
    {
        /// <summary>只对普通图层生效</summary>
        [InspectorName("图层")]
        Layer,
        /// <summary>只对图层组生效</summary>
        [InspectorName("图层组")]
        Group,
        /// <summary>图层与组都生效</summary>
        [InspectorName("图层和组")]
        Both
    }

    /// <summary>
    /// PSB 图层顺序转换方式
    /// </summary>
    public enum PSBUIOrderMode
    {
        /// <summary>按 PSB 习惯处理：PSB 越靠上的图层在 UI 里越靠前（默认，推荐）</summary>
        [InspectorName("PSB 上层在前")]
        PsbTopIsFront,
        /// <summary>完全照搬导入顺序，不再翻转</summary>
        [InspectorName("保持导入顺序")]
        KeepImportOrder
    }

    /// <summary>
    /// 隐藏图层的处理方式
    /// </summary>
    public enum PSBUIHiddenMode
    {
        /// <summary>不生成隐藏图层</summary>
        [InspectorName("跳过不生成")]
        Skip,
        /// <summary>生成节点但保持未激活</summary>
        [InspectorName("生成但未激活")]
        Inactive,
        /// <summary>生成节点并保持激活，只把透明度按 PSB 值写入</summary>
        [InspectorName("保持激活")]
        KeepActive
    }

    /// <summary>
    /// 一条图层命名规则：名称符合匹配条件时把节点转换成对应组件或结构
    /// </summary>
    [Serializable]
    public class PSBUIRule
    {
        /// <summary>是否启用该规则</summary>
        public bool enabled = true;
        /// <summary>匹配方式</summary>
        public PSBUIMatchMode matchMode = PSBUIMatchMode.Prefix;
        /// <summary>用于匹配的名称片段或正则表达式</summary>
        public string pattern = string.Empty;
        /// <summary>命中后应用的结构</summary>
        public PSBUIRuleKind kind = PSBUIRuleKind.Button;
        /// <summary>规则作用于图层还是组</summary>
        public PSBUIRuleTarget target = PSBUIRuleTarget.Layer;
        /// <summary>命中后是否保留图片，关闭后文本与容器会移除 Image</summary>
        public bool keepImage = true;
    }

    /// <summary>
    /// PSB 转 UI 预制体的生成配置
    /// </summary>
    [Serializable]
    public class PSBUIConfig
    {
        /// <summary>生成后的预制体名字，留空则用 PSB 文件名</summary>
        public string prefabName = string.Empty;
        /// <summary>预制体输出目录</summary>
        public string outputFolder = "Assets/Prefabs/UI";
        /// <summary>是否在根节点上补一套 ScreenSpaceOverlay 画布</summary>
        public bool createRootCanvas = true;
        /// <summary>画布参考分辨率，宽度对应 PSB 文档宽度</summary>
        public Vector2 referenceResolution = new Vector2(1920f, 1080f);
        /// <summary>根节点尺寸是否直接使用 PSB 文档尺寸</summary>
        public bool useDocumentSize = true;
        /// <summary>PSB 图层顺序转换方式</summary>
        public PSBUIOrderMode orderMode = PSBUIOrderMode.PsbTopIsFront;
        /// <summary>隐藏图层的处理方式，默认生成未激活节点，避免美术隐藏的底图丢失</summary>
        public PSBUIHiddenMode hiddenMode = PSBUIHiddenMode.Inactive;
        /// <summary>是否把 PSB 图层不透明度写到图片透明度上</summary>
        public bool applyOpacity = true;
        /// <summary>生成的图片是否响应点击（默认关闭，避免挡住按钮）</summary>
        public bool imageRaycastTarget = false;
        /// <summary>是否按图层组生成父子层级</summary>
        public bool generateGroups = true;
        /// <summary>是否把组内子节点直接挂到根节点上（忽略组的层级）</summary>
        public bool flattenGroups = false;
        /// <summary>是否启用命名规则表，默认关闭：不勾选时所有图层都按普通图片生成</summary>
        public bool applyRules = false;
        /// <summary>命名规则表</summary>
        public List<PSBUIRule> rules = new List<PSBUIRule>();
        /// <summary>预览树里被手动排除的节点路径</summary>
        public List<string> excludedPaths = new List<string>();
        /// <summary>是否在控制台打印生成细节</summary>
        public bool logDetails = true;

        /// <summary>
        /// 创建一份带默认命名规则与项目约定的配置
        /// </summary>
        /// <returns>新的配置对象</returns>
        public static PSBUIConfig CreateDefault()
        {
            var config = new PSBUIConfig();
            //默认规则按「类型：内容」的中文命名给出，玩家可以随时增删改，不影响已有资产
            //顺序即优先级，越靠前越先匹配，所以跳过类规则放在最前面
            config.rules.Add(new PSBUIRule { pattern = "跳过", matchMode = PSBUIMatchMode.Prefix, kind = PSBUIRuleKind.Ignore, target = PSBUIRuleTarget.Both });
            config.rules.Add(new PSBUIRule { pattern = "滚动视图", matchMode = PSBUIMatchMode.Prefix, kind = PSBUIRuleKind.ScrollRect, target = PSBUIRuleTarget.Both });
            config.rules.Add(new PSBUIRule { pattern = "滚动条", matchMode = PSBUIMatchMode.Prefix, kind = PSBUIRuleKind.Scrollbar, target = PSBUIRuleTarget.Both });
            config.rules.Add(new PSBUIRule { pattern = "滑动窗口", matchMode = PSBUIMatchMode.Prefix, kind = PSBUIRuleKind.SlideWindow, target = PSBUIRuleTarget.Both });
            config.rules.Add(new PSBUIRule { pattern = "开关", matchMode = PSBUIMatchMode.Prefix, kind = PSBUIRuleKind.Toggle, target = PSBUIRuleTarget.Both });
            config.rules.Add(new PSBUIRule { pattern = "滑动条", matchMode = PSBUIMatchMode.Prefix, kind = PSBUIRuleKind.Slider, target = PSBUIRuleTarget.Both });
            config.rules.Add(new PSBUIRule { pattern = "输入框", matchMode = PSBUIMatchMode.Prefix, kind = PSBUIRuleKind.InputField, target = PSBUIRuleTarget.Both });
            config.rules.Add(new PSBUIRule { pattern = "下拉框", matchMode = PSBUIMatchMode.Prefix, kind = PSBUIRuleKind.Dropdown, target = PSBUIRuleTarget.Both });
            config.rules.Add(new PSBUIRule { pattern = "下拉列表", matchMode = PSBUIMatchMode.Prefix, kind = PSBUIRuleKind.Dropdown, target = PSBUIRuleTarget.Both });
            config.rules.Add(new PSBUIRule { pattern = "页签", matchMode = PSBUIMatchMode.Prefix, kind = PSBUIRuleKind.TabGroup, target = PSBUIRuleTarget.Group });
            config.rules.Add(new PSBUIRule { pattern = "开关组", matchMode = PSBUIMatchMode.Prefix, kind = PSBUIRuleKind.ToggleGroup, target = PSBUIRuleTarget.Group });
            config.rules.Add(new PSBUIRule { pattern = "遮罩图", matchMode = PSBUIMatchMode.Prefix, kind = PSBUIRuleKind.Mask, target = PSBUIRuleTarget.Layer });
            config.rules.Add(new PSBUIRule { pattern = "原图", matchMode = PSBUIMatchMode.Prefix, kind = PSBUIRuleKind.RawImage, target = PSBUIRuleTarget.Layer });
            config.rules.Add(new PSBUIRule { pattern = "大图", matchMode = PSBUIMatchMode.Prefix, kind = PSBUIRuleKind.RawImage, target = PSBUIRuleTarget.Layer });
            config.rules.Add(new PSBUIRule { pattern = "文本", matchMode = PSBUIMatchMode.Prefix, kind = PSBUIRuleKind.Text, target = PSBUIRuleTarget.Layer, keepImage = false });
            config.rules.Add(new PSBUIRule { pattern = "文字", matchMode = PSBUIMatchMode.Prefix, kind = PSBUIRuleKind.Text, target = PSBUIRuleTarget.Layer, keepImage = false });
            config.rules.Add(new PSBUIRule { pattern = "按钮", matchMode = PSBUIMatchMode.Prefix, kind = PSBUIRuleKind.Button, target = PSBUIRuleTarget.Both });
            config.rules.Add(new PSBUIRule { pattern = "btn_", matchMode = PSBUIMatchMode.Prefix, kind = PSBUIRuleKind.Button, target = PSBUIRuleTarget.Both });
            config.rules.Add(new PSBUIRule { pattern = "遮罩", matchMode = PSBUIMatchMode.Prefix, kind = PSBUIRuleKind.RectMask2D, target = PSBUIRuleTarget.Both });
            config.rules.Add(new PSBUIRule { pattern = "列表", matchMode = PSBUIMatchMode.Prefix, kind = PSBUIRuleKind.VerticalLayout, target = PSBUIRuleTarget.Group });
            config.rules.Add(new PSBUIRule { pattern = "网格", matchMode = PSBUIMatchMode.Prefix, kind = PSBUIRuleKind.GridLayout, target = PSBUIRuleTarget.Group });
            config.rules.Add(new PSBUIRule { pattern = "容器", matchMode = PSBUIMatchMode.Prefix, kind = PSBUIRuleKind.CanvasGroup, target = PSBUIRuleTarget.Group });
            config.rules.Add(new PSBUIRule { pattern = "图片", matchMode = PSBUIMatchMode.Prefix, kind = PSBUIRuleKind.None, target = PSBUIRuleTarget.Layer });
            config.rules.Add(new PSBUIRule { pattern = "图标", matchMode = PSBUIMatchMode.Prefix, kind = PSBUIRuleKind.None, target = PSBUIRuleTarget.Layer });
            config.rules.Add(new PSBUIRule { pattern = "背景", matchMode = PSBUIMatchMode.Prefix, kind = PSBUIRuleKind.None, target = PSBUIRuleTarget.Layer });
            return config;
        }
    }

    /// <summary>
    /// 预览树里的一个节点，描述即将生成的 UI 结构
    /// </summary>
    public class PSBUIEntry
    {
        /// <summary>节点名字，默认取 PSB 图层名</summary>
        public string name = string.Empty;
        /// <summary>从根节点到当前节点的路径，用于排除与规则匹配</summary>
        public string path = string.Empty;
        /// <summary>是否来自图层组</summary>
        public bool isGroup;
        /// <summary>在树里的层级深度，根节点为0</summary>
        public int depth;
        /// <summary>节点在 PSB 里的尺寸，单位像素</summary>
        public Vector2 size;
        /// <summary>PSB 里是否隐藏</summary>
        public bool hidden;
        /// <summary>是否匹配到了精灵图</summary>
        public bool hasSprite;
        /// <summary>是否已在预览里被排除</summary>
        public bool excluded;
        /// <summary>命中规则后应用的结构</summary>
        public PSBUIRuleKind ruleKind = PSBUIRuleKind.None;
        /// <summary>命中的规则文本，没有命中时为空</summary>
        public string matchedRule = string.Empty;
    }

    /// <summary>
    /// PSB 转 UI 预制体的对外入口，编辑器窗口只依赖这一层接口
    /// </summary>
    public static partial class PSBToUIPrefab
    {
        /// <summary>
        /// 针对指定 PSB 生成一份默认配置
        /// </summary>
        /// <param name="psbPath">PSB 资产路径，例如 Assets/Art/通用UI.psb</param>
        /// <returns>填好默认输出目录与名字的配置</returns>
        public static PSBUIConfig CreateConfig(string psbPath)
        {
            var config = PSBUIConfig.CreateDefault();
            string fileName = System.IO.Path.GetFileNameWithoutExtension(psbPath ?? string.Empty);
            config.prefabName = string.IsNullOrEmpty(fileName) ? "PSB_UI" : fileName;
            return config;
        }

        /// <summary>
        /// 检查 PSB 是否可以转换，返回问题列表
        /// </summary>
        /// <param name="psbPath">PSB 资产路径</param>
        /// <returns>问题描述列表，空表示没有明显问题</returns>
        public static List<string> Validate(string psbPath)
        {
            var problems = new List<string>();
            if (string.IsNullOrEmpty(psbPath) || !psbPath.EndsWith(".psb", StringComparison.OrdinalIgnoreCase))
            {
                problems.Add("请选择一个 .psb 文件");
                return problems;
            }
            if (!System.IO.File.Exists(psbPath))
            {
                problems.Add("找不到 PSB 文件：" + psbPath);
                return problems;
            }
            var sprites = LoadSpriteList(psbPath);
            if (sprites.Count == 0)
                problems.Add("该 PSB 没有导入任何精灵图，请确认导入器为 PSD Importer 且精灵模式为 Multiple");
            return problems;
        }

        /// <summary>
        /// 生成预览树，供窗口展示与手动排除节点
        /// </summary>
        /// <param name="psbPath">PSB 资产路径</param>
        /// <param name="config">生成配置</param>
        /// <returns>按层级顺序排好的节点列表，无法读取时返回空列表</returns>
        public static List<PSBUIEntry> BuildPreview(string psbPath, PSBUIConfig config)
        {
            var entries = new List<PSBUIEntry>();
            if (config == null) return entries;
            var tree = ReadTree(psbPath, config, null);
            if (tree == null) return entries;
            CollectPreview(tree, string.Empty, 0, config, entries);
            return entries;
        }

        /// <summary>
        /// 执行转换，生成 UI 预制体
        /// </summary>
        /// <param name="psbPath">PSB 资产路径</param>
        /// <param name="config">生成配置</param>
        /// <param name="prefabPath">成功时返回预制体路径</param>
        /// <param name="error">失败时返回失败原因</param>
        /// <returns>true 表示生成成功</returns>
        public static bool TryGenerate(string psbPath, PSBUIConfig config, out string prefabPath, out string error)
        {
            prefabPath = string.Empty;
            error = string.Empty;
            return GenerateInternal(psbPath, config, ref prefabPath, ref error);
        }
    }
}
