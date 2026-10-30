using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MaxyToolKit.Editor
{
    /// <summary>
    /// PSB 转 UI 预制体的实现部分：读取图层树、匹配精灵、生成 RectTransform + Image 结构
    /// </summary>
    public static partial class PSBToUIPrefab
    {
        /// <summary>
        /// 生成过程使用的一棵中间节点树，矩形统一使用 PSB 文档坐标
        /// </summary>
        class BuildNode
        {
            /// <summary>节点名字</summary>
            public string name = string.Empty;
            /// <summary>从根节点起的唯一路径，用于排除判断</summary>
            public string path = string.Empty;
            /// <summary>是否是图层组</summary>
            public bool isGroup;
            /// <summary>文档坐标矩形，左下角原点，y 轴向上</summary>
            public Rect rect;
            /// <summary>子节点，保持 PSB 自下而上的顺序</summary>
            public readonly List<BuildNode> children = new List<BuildNode>();
            /// <summary>父节点</summary>
            public BuildNode parent;
            /// <summary>匹配到的精灵图</summary>
            public Sprite sprite;
            /// <summary>是否隐藏（含父组继承）</summary>
            public bool hidden;
            /// <summary>继承后的不透明度，0~1</summary>
            public float inheritedOpacity = 1f;
            /// <summary>是否被用户或规则排除</summary>
            public bool excluded;
            /// <summary>命中规则后的结构类型</summary>
            public PSBUIRuleKind ruleKind = PSBUIRuleKind.None;
            /// <summary>命中的规则文本</summary>
            public string matchedRule = string.Empty;
            /// <summary>命中的规则，生成时读取保留图片等配置</summary>
            public PSBUIRule rule;
        }

        /// <summary>
        /// 加载 PSB 里所有精灵，并按导入记录的图层顺序排列
        /// </summary>
        /// <param name="psbPath">PSB 资产路径</param>
        /// <returns>与图层顺序对应的精灵列表，可能含 null 表示未匹配到</returns>
        static List<Sprite> LoadSpriteList(string psbPath)
        {
            var result = new List<Sprite>();
            var sprites = new List<Sprite>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(psbPath))
            {
                if (asset is Sprite sprite) sprites.Add(sprite);
            }
            var meta = PSBMetaReader.Read(psbPath);
            if (meta.Count == 0)
            {
                result.AddRange(sprites);
                return result;
            }
            //按名字分组，PSB 允许重名图层，因此同名精灵按出现顺序消费
            var buckets = new Dictionary<string, Queue<Sprite>>();
            foreach (var sprite in sprites)
            {
                if (!buckets.TryGetValue(sprite.name, out var queue))
                {
                    queue = new Queue<Sprite>();
                    buckets[sprite.name] = queue;
                }
                queue.Enqueue(sprite);
            }
            foreach (var item in meta)
            {
                Sprite matched = null;
                if (buckets.TryGetValue(item.name, out var queue) && queue.Count > 0) matched = queue.Dequeue();
                if (matched == null)
                {
                    //导入器给重名精灵加过后缀时，用前缀再兜一次
                    foreach (var pair in buckets)
                    {
                        if (pair.Value.Count == 0) continue;
                        if (!pair.Key.StartsWith(item.name, StringComparison.Ordinal)) continue;
                        matched = pair.Value.Dequeue();
                        break;
                    }
                }
                result.Add(matched);
            }
            return result;
        }

        /// <summary>
        /// 读取 PSB 并构造生成用的节点树
        /// </summary>
        /// <param name="psbPath">PSB 资产路径</param>
        /// <param name="config">生成配置</param>
        /// <param name="warnings">收集过程中发现的问题，可为 null</param>
        /// <returns>根节点，读取失败时返回 null</returns>
        static BuildNode ReadTree(string psbPath, PSBUIConfig config, List<string> warnings)
        {
            if (string.IsNullOrEmpty(psbPath) || !File.Exists(psbPath)) return null;
            PSBDocument document;
            try
            {
                document = PSBFileParser.Parse(psbPath);
            }
            catch (Exception error)
            {
                warnings?.Add("解析 PSB 失败：" + error.Message);
                return null;
            }
            var meta = PSBMetaReader.Read(psbPath);
            var spriteList = LoadSpriteList(psbPath);
            var metaByLeaf = PairMetaWithLeaves(document, meta, warnings);
            var spriteByMeta = BuildSpriteLookup(meta, spriteList);
            var root = new BuildNode
            {
                name = string.IsNullOrEmpty(config.prefabName)
                    ? Path.GetFileNameWithoutExtension(psbPath)
                    : config.prefabName,
                isGroup = true,
                rect = new Rect(0f, 0f, document.width, document.height),
                path = string.Empty
            };
            var usedPaths = new Dictionary<string, int>();
            foreach (var layer in document.roots)
            {
                var child = BuildNodeTree(layer, root, config, metaByLeaf, spriteByMeta, warnings, usedPaths);
                if (child != null) root.children.Add(child);
            }
            return root;
        }

        /// <summary>
        /// 建立 图层→精灵信息 的对应关系
        /// </summary>
        /// <param name="document">PSB 结构</param>
        /// <param name="meta">导入记录里的精灵列表，自上而下</param>
        /// <param name="warnings">问题收集</param>
        /// <returns>图层到精灵信息的映射</returns>
        static Dictionary<PSBLayerNode, PSBMetaSprite> PairMetaWithLeaves(PSBDocument document,
            List<PSBMetaSprite> meta, List<string> warnings)
        {
            var map = new Dictionary<PSBLayerNode, PSBMetaSprite>();
            if (meta.Count == 0 || document.leafLayers.Count == 0)
            {
                warnings?.Add("没有读取到图层精灵记录，生成的节点将没有图片");
                return map;
            }
            //PSB 记录自下而上，导入记录自上而下，先翻转再按顺序一一对应
            var leavesTopDown = new List<PSBLayerNode>(document.leafLayers);
            leavesTopDown.Reverse();
            if (leavesTopDown.Count == meta.Count)
            {
                for (int i = 0; i < leavesTopDown.Count; i++) map[leavesTopDown[i]] = meta[i];
                return map;
            }
            var visibleLeaves = new List<PSBLayerNode>();
            foreach (var leaf in leavesTopDown)
            {
                if (IsLayerVisible(leaf)) visibleLeaves.Add(leaf);
            }
            if (visibleLeaves.Count == meta.Count)
            {
                warnings?.Add("图层数量与导入记录不一致，已按可见图层顺序匹配");
                for (int i = 0; i < visibleLeaves.Count; i++) map[visibleLeaves[i]] = meta[i];
                return map;
            }
            //数量对不上时退回按名字匹配，重名图层按出现顺序消费
            var buckets = new Dictionary<string, Queue<PSBMetaSprite>>();
            foreach (var item in meta)
            {
                if (!buckets.TryGetValue(item.name, out var queue))
                {
                    queue = new Queue<PSBMetaSprite>();
                    buckets[item.name] = queue;
                }
                queue.Enqueue(item);
            }
            foreach (var leaf in leavesTopDown)
            {
                if (buckets.TryGetValue(leaf.name, out var queue) && queue.Count > 0) map[leaf] = queue.Dequeue();
            }
            warnings?.Add("图层数（" + leavesTopDown.Count + "）与精灵数（" + meta.Count + "）不一致，已按名字尽力匹配，"
                + (leavesTopDown.Count - map.Count) + " 个图层没有图片");
            return map;
        }

        /// <summary>
        /// 建立 精灵信息→精灵资源 的映射
        /// </summary>
        /// <param name="meta">导入记录里的精灵列表</param>
        /// <param name="sprites">按顺序排列的精灵资源</param>
        /// <returns>精灵信息到资源的映射</returns>
        static Dictionary<PSBMetaSprite, Sprite> BuildSpriteLookup(List<PSBMetaSprite> meta, List<Sprite> sprites)
        {
            var map = new Dictionary<PSBMetaSprite, Sprite>();
            for (int i = 0; i < meta.Count && i < sprites.Count; i++)
            {
                if (sprites[i] != null) map[meta[i]] = sprites[i];
            }
            return map;
        }

        /// <summary>
        /// 判断图层自身是否可见，父组的不可见由调用方继承
        /// </summary>
        /// <param name="layer">图层</param>
        /// <returns>true 表示可见</returns>
        static bool IsLayerVisible(PSBLayerNode layer)
        {
            return layer != null && layer.visible;
        }

        /// <summary>
        /// 递归把 PSB 图层转成生成节点
        /// </summary>
        /// <param name="layer">PSB 图层</param>
        /// <param name="parent">父生成节点</param>
        /// <param name="config">生成配置</param>
        /// <param name="metaByLeaf">图层到精灵信息的映射</param>
        /// <param name="spriteByMeta">精灵信息到资源的映射</param>
        /// <param name="warnings">问题收集</param>
        /// <param name="usedPaths">路径去重表</param>
        /// <returns>生成节点，被排除时返回 null</returns>
        static BuildNode BuildNodeTree(PSBLayerNode layer, BuildNode parent, PSBUIConfig config,
            Dictionary<PSBLayerNode, PSBMetaSprite> metaByLeaf, Dictionary<PSBMetaSprite, Sprite> spriteByMeta,
            List<string> warnings, Dictionary<string, int> usedPaths)
        {
            var node = new BuildNode
            {
                name = layer.name,
                isGroup = layer.isGroup,
                parent = parent,
                hidden = !layer.visible || (parent != null && parent.hidden),
                inheritedOpacity = (parent != null ? parent.inheritedOpacity : 1f) * (layer.opacity / 255f),
                rect = layer.rect
            };
            //先算路径，预览与排除都依赖它，隐藏图层也要有路径才能被看见状态
            node.path = MakeUniquePath(parent, layer.name, usedPaths);
            //隐藏图层的处理：跳过或保留为未激活
            if (node.hidden && config.hiddenMode == PSBUIHiddenMode.Skip)
            {
                node.excluded = true;
                return node;
            }
            //普通图层优先用导入记录里的裁边矩形，保证图片与 PSB 位置一致
            if (!layer.isGroup && metaByLeaf.TryGetValue(layer, out var meta))
            {
                node.rect = new Rect(meta.documentPosition.x, meta.documentPosition.y, meta.size.x, meta.size.y);
                if (spriteByMeta.TryGetValue(meta, out var sprite)) node.sprite = sprite;
                else warnings?.Add("图层「" + layer.name + "」没有对应的精灵图，将生成空节点");
            }
            //用户手动排除的节点直接跳过，组被子节点排除不影响
            if (config.excludedPaths != null && config.excludedPaths.Contains(node.path))
            {
                node.excluded = true;
                return node;
            }
            //命名规则：第一条命中的规则生效
            if (config.applyRules && config.rules != null)
            {
                foreach (var rule in config.rules)
                {
                    if (rule == null || !rule.enabled) continue;
                    if (!RuleTargetMatches(rule, layer.isGroup)) continue;
                    if (!NameMatches(rule, layer.name)) continue;
                    node.ruleKind = rule.kind;
                    node.matchedRule = rule.pattern;
                    node.rule = rule;
                    break;
                }
            }
            if (node.ruleKind == PSBUIRuleKind.Ignore)
            {
                node.excluded = true;
                return node;
            }
            if (layer.isGroup)
            {
                foreach (var childLayer in layer.children)
                {
                    var child = BuildNodeTree(childLayer, node, config, metaByLeaf, spriteByMeta, warnings, usedPaths);
                    if (child != null) node.children.Add(child);
                }
                if (node.children.Count > 0)
                {
                    //组的包围盒由子节点决定，空组沿用 PSB 里的矩形
                    Rect bounds = node.children[0].rect;
                    foreach (var child in node.children) bounds = Union(bounds, child.rect);
                    node.rect = bounds;
                }
                if (config.flattenGroups && node.ruleKind == PSBUIRuleKind.None)
                {
                    //拍平：组本身不生成节点，子节点直接挂到上一级
                    foreach (var child in node.children) child.parent = parent;
                    foreach (var child in node.children) parent.children.Add(child);
                    return null;
                }
            }
            return node;
        }

        /// <summary>
        /// 生成不重复的节点路径，同名节点用序号区分
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="name">节点名字</param>
        /// <param name="usedPaths">已用路径计数表</param>
        /// <returns>唯一路径</returns>
        static string MakeUniquePath(BuildNode parent, string name, Dictionary<string, int> usedPaths)
        {
            string prefix = parent != null && !string.IsNullOrEmpty(parent.path) ? parent.path + "/" : string.Empty;
            string basePath = prefix + name;
            if (!usedPaths.TryGetValue(basePath, out int count))
            {
                usedPaths[basePath] = 1;
                return basePath;
            }
            usedPaths[basePath] = count + 1;
            return basePath + "(" + usedPaths[basePath] + ")";
        }

        /// <summary>
        /// 判断规则是否作用于当前节点类型
        /// </summary>
        /// <param name="rule">规则</param>
        /// <param name="isGroup">当前节点是否是组</param>
        /// <returns>true 表示规则可用</returns>
        static bool RuleTargetMatches(PSBUIRule rule, bool isGroup)
        {
            switch (rule.target)
            {
                case PSBUIRuleTarget.Layer: return !isGroup;
                case PSBUIRuleTarget.Group: return isGroup;
                default: return true;
            }
        }

        /// <summary>
        /// 判断名称是否命中规则
        /// </summary>
        /// <param name="rule">规则</param>
        /// <param name="name">节点名字</param>
        /// <returns>true 表示命中</returns>
        static bool NameMatches(PSBUIRule rule, string name)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(rule.pattern)) return false;
            switch (rule.matchMode)
            {
                case PSBUIMatchMode.Prefix: return name.StartsWith(rule.pattern, StringComparison.OrdinalIgnoreCase);
                case PSBUIMatchMode.Suffix: return name.EndsWith(rule.pattern, StringComparison.OrdinalIgnoreCase);
                case PSBUIMatchMode.Contains: return name.IndexOf(rule.pattern, StringComparison.OrdinalIgnoreCase) >= 0;
                case PSBUIMatchMode.Regex:
                    try { return Regex.IsMatch(name, rule.pattern); }
                    catch (ArgumentException) { return false; }
                default: return false;
            }
        }

        /// <summary>
        /// 合并两个文档坐标矩形
        /// </summary>
        /// <param name="a">矩形 A</param>
        /// <param name="b">矩形 B</param>
        /// <returns>包含两者的最小矩形</returns>
        static Rect Union(Rect a, Rect b)
        {
            float minX = Mathf.Min(a.xMin, b.xMin);
            float minY = Mathf.Min(a.yMin, b.yMin);
            float maxX = Mathf.Max(a.xMax, b.xMax);
            float maxY = Mathf.Max(a.yMax, b.yMax);
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// 把生成节点树展开成预览列表，展示顺序与 PSB 一致（上层在前）
        /// </summary>
        /// <param name="node">当前节点</param>
        /// <param name="parentPath">父节点路径</param>
        /// <param name="depth">层级深度</param>
        /// <param name="config">生成配置</param>
        /// <param name="entries">输出列表</param>
        static void CollectPreview(BuildNode node, string parentPath, int depth, PSBUIConfig config, List<PSBUIEntry> entries)
        {
            if (node == null) return;
            if (depth > 0)
            {
                entries.Add(new PSBUIEntry
                {
                    name = node.name,
                    path = node.path,
                    isGroup = node.isGroup,
                    depth = depth - 1,
                    size = node.rect.size,
                    hidden = node.hidden,
                    hasSprite = node.sprite != null,
                    excluded = node.excluded || (config.excludedPaths != null && config.excludedPaths.Contains(node.path)),
                    ruleKind = node.ruleKind,
                    matchedRule = node.matchedRule
                });
            }
            //倒序展示：PSB 里越靠上的图层越先显示
            for (int i = node.children.Count - 1; i >= 0; i--) CollectPreview(node.children[i], node.path, depth + 1, config, entries);
        }

        /// <summary>
        /// 真正执行预制体生成
        /// </summary>
        /// <param name="psbPath">PSB 资产路径</param>
        /// <param name="config">生成配置</param>
        /// <param name="prefabPath">输出预制体路径</param>
        /// <param name="error">失败原因</param>
        /// <returns>true 表示成功</returns>
        static bool GenerateInternal(string psbPath, PSBUIConfig config, ref string prefabPath, ref string error)
        {
            if (config == null) { error = "生成配置为空"; return false; }
            if (string.IsNullOrEmpty(psbPath) || !File.Exists(psbPath)) { error = "找不到 PSB 文件：" + psbPath; return false; }
            if (!psbPath.EndsWith(".psb", StringComparison.OrdinalIgnoreCase)
                && !psbPath.EndsWith(".psd", StringComparison.OrdinalIgnoreCase))
            {
                error = "只支持 .psb 或 .psd 文件";
                return false;
            }
            var warnings = new List<string>();
            var root = ReadTree(psbPath, config, warnings);
            if (root == null) { error = "PSB 解析失败，请查看控制台"; return false; }
            GameObject rootObject = null;
            try
            {
                rootObject = BuildGameObjects(root, config);
                if (rootObject == null) { error = "没有生成任何节点，请检查图层排除设置"; return false; }
                string folder = string.IsNullOrEmpty(config.outputFolder) ? "Assets/Prefabs/UI" : config.outputFolder.Replace('\\', '/');
                EnsureFolder(folder);
                string prefabName = string.IsNullOrEmpty(config.prefabName)
                    ? Path.GetFileNameWithoutExtension(psbPath)
                    : config.prefabName;
                prefabName = SanitizeFileName(prefabName);
                prefabPath = folder + "/" + prefabName + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(rootObject, prefabPath);
                AssetDatabase.SaveAssets();
            }
            catch (Exception exception)
            {
                error = "生成失败：" + exception.Message;
                return false;
            }
            finally
            {
                if (rootObject != null) UnityEngine.Object.DestroyImmediate(rootObject);
            }
            if (warnings.Count > 0)
            {
                foreach (var warning in warnings) Debug.LogWarning("[PSB→UI] " + warning);
            }
            int count = CountNodes(root);
            if (config.logDetails)
                Debug.Log("[PSB→UI] 已生成预制体 " + prefabPath + "，节点数 " + count + "，精灵匹配 " + CountSprites(root) + " 个");
            return true;
        }

        /// <summary>
        /// 递归统计生成节点数量
        /// </summary>
        /// <param name="node">节点</param>
        /// <returns>节点数量</returns>
        static int CountNodes(BuildNode node)
        {
            if (node == null || (node.parent != null && node.excluded)) return 0;
            int total = node.parent != null ? 1 : 0;
            foreach (var child in node.children) total += CountNodes(child);
            return total;
        }

        /// <summary>
        /// 递归统计带精灵的节点数量
        /// </summary>
        /// <param name="node">节点</param>
        /// <returns>带图片的节点数量</returns>
        static int CountSprites(BuildNode node)
        {
            if (node == null) return 0;
            int total = node.sprite != null && node.parent != null ? 1 : 0;
            foreach (var child in node.children) total += CountSprites(child);
            return total;
        }

        /// <summary>
        /// 按节点树创建 GameObject 层级
        /// </summary>
        /// <param name="rootNode">根节点</param>
        /// <param name="config">生成配置</param>
        /// <returns>根 GameObject</returns>
        static GameObject BuildGameObjects(BuildNode rootNode, PSBUIConfig config)
        {
            var rootObject = new GameObject(rootNode.name, typeof(RectTransform));
            var rootRect = (RectTransform)rootObject.transform;
            Vector2 documentSize = rootNode.rect.size;
            //默认按 PSB 文档尺寸 1:1 生成；改成自定义设计分辨率时整体等比缩放
            bool useDocumentSize = config.useDocumentSize
                || config.referenceResolution.x <= 0f || config.referenceResolution.y <= 0f;
            Vector2 rootSize = useDocumentSize ? documentSize : config.referenceResolution;
            float scale = useDocumentSize ? 1f : Mathf.Min(rootSize.x / Mathf.Max(1f, documentSize.x),
                rootSize.y / Mathf.Max(1f, documentSize.y));
            rootRect.sizeDelta = documentSize * scale;
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            if (config.createRootCanvas)
            {
                var canvas = rootObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = rootObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = useDocumentSize ? documentSize : config.referenceResolution;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                rootObject.AddComponent<GraphicRaycaster>();
            }
            CreateChildren(rootNode, rootRect, documentSize * 0.5f, scale, config);
            return rootObject;
        }

        /// <summary>
        /// 递归创建子节点，并按 PSB 顺序处理前后关系
        /// </summary>
        /// <param name="parentNode">父生成节点</param>
        /// <param name="parentRect">父 RectTransform</param>
        /// <param name="parentCenter">父节点中心在文档坐标里的位置，未缩放</param>
        /// <param name="scale">文档像素到 UI 像素的缩放比例</param>
        /// <param name="config">生成配置</param>
        static void CreateChildren(BuildNode parentNode, RectTransform parentRect, Vector2 parentCenter,
            float scale, PSBUIConfig config)
        {
            var ordered = new List<BuildNode>(parentNode.children);
            //默认按 PSB 自下而上添加，最后一个成为最上层（PSB 上层在前）
            if (config.orderMode == PSBUIOrderMode.KeepImportOrder) ordered.Reverse();
            foreach (var node in ordered)
            {
                if (node.excluded) continue;
                var childObject = new GameObject(node.name, typeof(RectTransform));
                var childRect = (RectTransform)childObject.transform;
                childRect.SetParent(parentRect, false);
                childRect.anchorMin = childRect.anchorMax = new Vector2(0.5f, 0.5f);
                childRect.pivot = new Vector2(0.5f, 0.5f);
                Vector2 nodeCenter = node.rect.center;
                childRect.sizeDelta = new Vector2(Mathf.Max(0f, node.rect.width * scale), Mathf.Max(0f, node.rect.height * scale));
                childRect.anchoredPosition = (nodeCenter - parentCenter) * scale;
                ApplyRuleVisual(node, childObject, childRect, config);
                var content = childRect.Find("Viewport/Content") as RectTransform;
                CreateChildren(node, content != null ? content : childRect, nodeCenter, scale, config);
                ApplyHidden(node, childObject, config);
            }
            //开关组/页签组：把直接子节点里的开关登记到组上，生成后点一个就能互斥
            if (parentNode.ruleKind == PSBUIRuleKind.ToggleGroup || parentNode.ruleKind == PSBUIRuleKind.TabGroup)
            {
                var group = parentRect.GetComponent<ToggleGroup>();
                if (group != null)
                {
                    for (int index = 0; index < parentRect.childCount; index++)
                    {
                        var childToggle = parentRect.GetChild(index).GetComponent<Toggle>();
                        if (childToggle != null) childToggle.group = group;
                    }
                }
            }
        }

        /// <summary>
        /// 根据规则给节点加图片与组件
        /// 结构照着 Unity 默认创建 UI 时的层级来搭，方便后续直接用官方工作流继续改
        /// </summary>
        /// <param name="node">生成节点</param>
        /// <param name="target">目标 GameObject</param>
        /// <param name="rect">目标 RectTransform</param>
        /// <param name="config">生成配置</param>
        /// <returns>true 表示该节点可以继续挂子节点</returns>
        static bool ApplyRuleVisual(BuildNode node, GameObject target, RectTransform rect, PSBUIConfig config)
        {
            var rule = node.rule;
            //原图结构自己用 RawImage 显示贴图，不再额外挂 Image
            bool keepImage = node.sprite != null && (rule == null || rule.keepImage)
                && node.ruleKind != PSBUIRuleKind.RawImage;
            //文本占位不保留 PSB 图，直接用 TextMeshPro 显示名字里冒号后面的内容
            if (node.ruleKind == PSBUIRuleKind.Text)
            {
                var text = AddText(target, "文字", rect, GetRuleContent(node), config);
                text.fontSize = FitFontSize(rect.sizeDelta.y, 0.42f, 12f, 72f);
                return true;
            }
            if (keepImage) AddImage(node, target, config);
            switch (node.ruleKind)
            {
                case PSBUIRuleKind.Button:
                    BuildButton(target, rect, node, config);
                    break;
                case PSBUIRuleKind.ScrollRect:
                    BuildScrollView(target, rect, false);
                    break;
                case PSBUIRuleKind.SlideWindow:
                    BuildScrollView(target, rect, true);
                    break;
                case PSBUIRuleKind.Scrollbar:
                    BuildScrollbar(target, rect, false);
                    break;
                case PSBUIRuleKind.Toggle:
                    BuildToggle(target, rect, node, config);
                    break;
                case PSBUIRuleKind.Slider:
                    BuildSlider(target, rect, config);
                    break;
                case PSBUIRuleKind.InputField:
                    BuildInputField(target, rect, node, config);
                    break;
                case PSBUIRuleKind.Dropdown:
                    BuildDropdown(target, rect, node, config);
                    break;
                case PSBUIRuleKind.ToggleGroup:
                    BuildToggleGroup(target, false);
                    break;
                case PSBUIRuleKind.TabGroup:
                    BuildToggleGroup(target, true);
                    break;
                case PSBUIRuleKind.Mask:
                    BuildMask(target);
                    break;
                case PSBUIRuleKind.RawImage:
                    BuildRawImage(node, target, config);
                    break;
                case PSBUIRuleKind.VerticalLayout:
                    BuildVerticalLayout(target);
                    break;
                case PSBUIRuleKind.HorizontalLayout:
                    BuildHorizontalLayout(target);
                    break;
                case PSBUIRuleKind.GridLayout:
                    BuildGridLayout(target);
                    break;
                case PSBUIRuleKind.RectMask2D:
                    target.AddComponent<RectMask2D>();
                    break;
                case PSBUIRuleKind.CanvasGroup:
                    target.AddComponent<CanvasGroup>();
                    break;
            }
            return true;
        }

        /// <summary>默认按钮文字颜色</summary>
        static readonly Color _RuleTextColor = new Color(0.18f, 0.14f, 0.09f, 1f);
        /// <summary>默认滑轨颜色</summary>
        static readonly Color _RuleTrackColor = new Color(0f, 0f, 0f, 0.25f);
        /// <summary>默认填充与滑块颜色</summary>
        static readonly Color _RuleFillColor = new Color(1f, 1f, 1f, 0.85f);

        /// <summary>
        /// 给节点挂图片
        /// </summary>
        /// <param name="node">生成节点</param>
        /// <param name="target">目标 GameObject</param>
        /// <param name="config">生成配置</param>
        static void AddImage(BuildNode node, GameObject target, PSBUIConfig config)
        {
            var image = target.AddComponent<Image>();
            image.sprite = node.sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = config.imageRaycastTarget;
            float alpha = config.applyOpacity ? Mathf.Clamp01(node.inheritedOpacity) : 1f;
            image.color = new Color(1f, 1f, 1f, alpha);
        }

        /// <summary>
        /// 按容器尺寸估算一个合适的字号
        /// </summary>
        /// <param name="height">容器高度</param>
        /// <param name="ratio">占高度比例</param>
        /// <param name="min">最小字号</param>
        /// <param name="max">最大字号</param>
        /// <returns>字号</returns>
        static float FitFontSize(float height, float ratio, float min, float max)
        {
            return Mathf.Clamp(height * ratio, min, max);
        }

        /// <summary>
        /// 创建一个 TextMeshPro 文本子节点，并设置成铺满父级
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="name">节点名字</param>
        /// <param name="area">参照区域，用它的高度估算字号</param>
        /// <param name="content">显示内容</param>
        /// <param name="config">生成配置</param>
        /// <returns>创建好的文本组件</returns>
        static TextMeshProUGUI AddText(GameObject parent, string name, RectTransform area, string content, PSBUIConfig config)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var textRect = (RectTransform)textObject.transform;
            textRect.SetParent(parent.transform, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 8f);
            textRect.offsetMax = new Vector2(-16f, -8f);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = FitFontSize(area.sizeDelta.y, 0.42f, 12f, 72f);
            text.color = _RuleTextColor;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>
        /// 给节点挂按钮，并按 Unity 默认按钮结构补一个文字子节点
        /// </summary>
        /// <param name="target">目标 GameObject</param>
        /// <param name="rect">目标矩形</param>
        /// <param name="node">生成节点</param>
        /// <param name="config">生成配置</param>
        static void BuildButton(GameObject target, RectTransform rect, BuildNode node, PSBUIConfig config)
        {
            var image = target.GetComponent<Image>();
            if (image == null)
            {
                //规则关闭了图片时也要有目标图形，否则按钮没有点击区域
                image = target.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0.01f);
            }
            var button = target.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.96f, 0.86f);
            colors.pressedColor = new Color(0.88f, 0.84f, 0.74f);
            button.colors = colors;
            var label = GetRuleContent(node);
            if (!string.IsNullOrEmpty(label)) AddText(target, "文字", rect, label, config);
        }

        /// <summary>
        /// 按 Unity 默认滚动视图结构搭一套：Scroll View / Viewport / Content / Scrollbar
        /// </summary>
        /// <param name="target">目标 GameObject</param>
        /// <param name="rect">目标矩形</param>
        /// <param name="horizontal">true 表示横向滑动窗口，false 表示普通竖向滚动视图</param>
        static void BuildScrollView(GameObject target, RectTransform rect, bool horizontal)
        {
            var scroll = target.AddComponent<ScrollRect>();
            //视口铺满滚动区域并负责裁剪
            var viewport = CreateRect("Viewport", rect);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            viewport.gameObject.AddComponent<RectMask2D>();
            //内容节点与滚动区域同心，图层子节点的坐标可以继续沿用文档坐标算法
            var content = CreateRect("Content", viewport);
            content.anchorMin = content.anchorMax = new Vector2(0.5f, 0.5f);
            content.pivot = new Vector2(0.5f, 0.5f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = rect.sizeDelta;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = horizontal;
            scroll.vertical = !horizontal;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.1f;
            scroll.scrollSensitivity = 24f;
            //滚动条放在视口外侧边缘，方向和滚动方向一致
            if (horizontal)
            {
                var scrollbarRect = CreateRect("Scrollbar Horizontal", rect);
                scrollbarRect.anchorMin = new Vector2(0f, 0f);
                scrollbarRect.anchorMax = new Vector2(1f, 0f);
                scrollbarRect.pivot = new Vector2(0.5f, 0f);
                scrollbarRect.offsetMin = new Vector2(0f, -18f);
                scrollbarRect.offsetMax = new Vector2(0f, 0f);
                scroll.horizontalScrollbar = BuildScrollbar(scrollbarRect.gameObject, scrollbarRect, true);
            }
            else
            {
                var scrollbarRect = CreateRect("Scrollbar Vertical", rect);
                scrollbarRect.anchorMin = new Vector2(1f, 0f);
                scrollbarRect.anchorMax = new Vector2(1f, 1f);
                scrollbarRect.pivot = new Vector2(1f, 0.5f);
                scrollbarRect.offsetMin = new Vector2(0f, 0f);
                scrollbarRect.offsetMax = new Vector2(18f, 0f);
                scroll.verticalScrollbar = BuildScrollbar(scrollbarRect.gameObject, scrollbarRect, false);
            }
        }

        /// <summary>
        /// 按 Unity 默认滚动条结构搭一套：轨道 / 滑动区 / 滑块
        /// </summary>
        /// <param name="target">目标 GameObject</param>
        /// <param name="rect">目标矩形</param>
        /// <param name="horizontal">true 表示横向滚动条</param>
        /// <returns>滚动条组件</returns>
        static Scrollbar BuildScrollbar(GameObject target, RectTransform rect, bool horizontal)
        {
            var track = target.GetComponent<Image>();
            if (track == null) track = target.AddComponent<Image>();
            track.color = _RuleTrackColor;
            var scrollbar = target.AddComponent<Scrollbar>();
            var sliding = CreateRect("Sliding Area", rect);
            sliding.anchorMin = Vector2.zero;
            sliding.anchorMax = Vector2.one;
            sliding.offsetMin = Vector2.zero;
            sliding.offsetMax = Vector2.zero;
            var handle = CreateRect("Handle", sliding);
            handle.anchorMin = handle.anchorMax = new Vector2(0.5f, 0.5f);
            handle.pivot = new Vector2(0.5f, 0.5f);
            handle.sizeDelta = horizontal ? new Vector2(40f, 0f) : new Vector2(0f, 40f);
            handle.anchoredPosition = Vector2.zero;
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = _RuleFillColor;
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = horizontal ? Scrollbar.Direction.LeftToRight : Scrollbar.Direction.BottomToTop;
            scrollbar.size = 0.2f;
            return scrollbar;
        }

        /// <summary>
        /// 按 Unity 默认开关结构搭一套：背景 / 勾选标记 / 文字
        /// </summary>
        /// <param name="target">目标 GameObject</param>
        /// <param name="rect">目标矩形</param>
        /// <param name="node">生成节点</param>
        /// <param name="config">生成配置</param>
        static void BuildToggle(GameObject target, RectTransform rect, BuildNode node, PSBUIConfig config)
        {
            var background = CreateRect("Background", rect);
            background.anchorMin = background.anchorMax = new Vector2(0f, 0.5f);
            background.pivot = new Vector2(0f, 0.5f);
            background.sizeDelta = new Vector2(rect.sizeDelta.y, rect.sizeDelta.y) * 0.8f;
            background.anchoredPosition = new Vector2(6f, 0f);
            var backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.color = new Color(1f, 1f, 1f, 0.12f);
            var checkmark = AddText(background.gameObject, "勾选", background, "√", config);
            checkmark.alignment = TextAlignmentOptions.Center;
            checkmark.color = Color.white;
            var toggle = target.AddComponent<Toggle>();
            toggle.targetGraphic = backgroundImage;
            toggle.graphic = checkmark;
            toggle.isOn = true;
            var label = GetRuleContent(node);
            if (!string.IsNullOrEmpty(label))
            {
                var labelText = AddText(target, "文字", rect, label, config);
                labelText.alignment = TextAlignmentOptions.Left;
                var labelRect = labelText.rectTransform;
                labelRect.offsetMin = new Vector2(background.sizeDelta.x + 18f, 8f);
            }
        }

        /// <summary>
        /// 按 Unity 默认滑动条结构搭一套：背景 / 填充区 / 把手
        /// </summary>
        /// <param name="target">目标 GameObject</param>
        /// <param name="rect">目标矩形</param>
        /// <param name="config">生成配置</param>
        static void BuildSlider(GameObject target, RectTransform rect, PSBUIConfig config)
        {
            var slider = target.AddComponent<Slider>();
            var background = CreateRect("Background", rect);
            background.anchorMin = new Vector2(0f, 0.25f);
            background.anchorMax = new Vector2(1f, 0.75f);
            background.offsetMin = Vector2.zero;
            background.offsetMax = Vector2.zero;
            var backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.color = _RuleTrackColor;
            var fillArea = CreateRect("Fill Area", rect);
            fillArea.anchorMin = new Vector2(0f, 0.25f);
            fillArea.anchorMax = new Vector2(1f, 0.75f);
            fillArea.offsetMin = new Vector2(4f, 4f);
            fillArea.offsetMax = new Vector2(-4f, -4f);
            var fill = CreateRect("Fill", fillArea);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(1f, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = _RuleFillColor;
            var handleArea = CreateRect("Handle Slide Area", rect);
            handleArea.anchorMin = new Vector2(0f, 0.5f);
            handleArea.anchorMax = new Vector2(1f, 0.5f);
            handleArea.offsetMin = new Vector2(10f, -20f);
            handleArea.offsetMax = new Vector2(-10f, 20f);
            var handle = CreateRect("Handle", handleArea);
            handle.anchorMin = handle.anchorMax = new Vector2(0f, 0.5f);
            handle.pivot = new Vector2(0.5f, 0.5f);
            handle.sizeDelta = new Vector2(32f, 32f);
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = _RuleFillColor;
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.value = 0.5f;
        }

        /// <summary>
        /// 按 Unity 默认输入框结构搭一套：文本区 / 占位文字 / 输入文本
        /// </summary>
        /// <param name="target">目标 GameObject</param>
        /// <param name="rect">目标矩形</param>
        /// <param name="node">生成节点</param>
        /// <param name="config">生成配置</param>
        static void BuildInputField(GameObject target, RectTransform rect, BuildNode node, PSBUIConfig config)
        {
            var input = target.AddComponent<TMP_InputField>();
            var textArea = CreateRect("Text Area", rect);
            textArea.anchorMin = Vector2.zero;
            textArea.anchorMax = Vector2.one;
            textArea.offsetMin = new Vector2(12f, 8f);
            textArea.offsetMax = new Vector2(-12f, -8f);
            textArea.gameObject.AddComponent<RectMask2D>();
            var placeholder = AddText(textArea.gameObject, "Placeholder", textArea, "输入文本", config);
            placeholder.color = new Color(_RuleTextColor.r, _RuleTextColor.g, _RuleTextColor.b, 0.5f);
            placeholder.alignment = TextAlignmentOptions.Left;
            var textComponent = AddText(textArea.gameObject, "Text", textArea, string.Empty, config);
            textComponent.alignment = TextAlignmentOptions.Left;
            input.textViewport = textArea;
            input.textComponent = textComponent;
            input.placeholder = placeholder;
            input.targetGraphic = target.GetComponent<Image>();
            input.lineType = TMP_InputField.LineType.SingleLine;
        }

        /// <summary>
        /// 按 Unity 默认下拉框结构搭一套：标题 / 箭头 / 模板 / 选项
        /// </summary>
        /// <param name="target">目标 GameObject</param>
        /// <param name="rect">目标矩形</param>
        /// <param name="node">生成节点</param>
        /// <param name="config">生成配置</param>
        static void BuildDropdown(GameObject target, RectTransform rect, BuildNode node, PSBUIConfig config)
        {
            var image = target.GetComponent<Image>();
            if (image == null)
            {
                image = target.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0.01f);
            }
            var dropdown = target.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = image;
            var content = GetRuleContent(node);
            var caption = AddText(target, "Label", rect, string.IsNullOrEmpty(content) ? "选项" : content, config);
            caption.alignment = TextAlignmentOptions.Left;
            caption.rectTransform.offsetMin = new Vector2(12f, 6f);
            caption.rectTransform.offsetMax = new Vector2(-28f, -6f);
            //箭头用一个小色块占位，换成正式图标时直接替换 sprite 即可
            var arrow = CreateRect("Arrow", rect);
            arrow.anchorMin = arrow.anchorMax = new Vector2(1f, 0.5f);
            arrow.pivot = new Vector2(1f, 0.5f);
            arrow.anchoredPosition = new Vector2(-10f, 0f);
            arrow.sizeDelta = new Vector2(12f, 12f);
            var arrowImage = arrow.gameObject.AddComponent<Image>();
            arrowImage.color = new Color(0.3f, 0.25f, 0.2f, 1f);
            //模板默认未激活，展开时由 Dropdown 自己显示
            var template = CreateRect("Template", rect);
            template.anchorMin = new Vector2(0f, 0f);
            template.anchorMax = new Vector2(1f, 0f);
            template.pivot = new Vector2(0.5f, 1f);
            template.anchoredPosition = new Vector2(0f, -2f);
            template.sizeDelta = new Vector2(0f, 160f);
            var templateImage = template.gameObject.AddComponent<Image>();
            templateImage.color = new Color(0.96f, 0.93f, 0.86f, 1f);
            var scrollRect = template.gameObject.AddComponent<ScrollRect>();
            var viewport = CreateRect("Viewport", template);
            viewport.gameObject.AddComponent<RectMask2D>();
            var templateContent = CreateRect("Content", viewport);
            templateContent.anchorMin = new Vector2(0f, 1f);
            templateContent.anchorMax = new Vector2(1f, 1f);
            templateContent.pivot = new Vector2(0.5f, 1f);
            templateContent.anchoredPosition = Vector2.zero;
            templateContent.sizeDelta = new Vector2(0f, 40f);
            //每个选项本质是一个 Toggle，供 Dropdown 复用
            var item = CreateRect("Item", templateContent);
            item.anchorMin = new Vector2(0f, 1f);
            item.anchorMax = new Vector2(1f, 1f);
            item.pivot = new Vector2(0.5f, 1f);
            item.anchoredPosition = Vector2.zero;
            item.sizeDelta = new Vector2(0f, 40f);
            var itemImage = item.gameObject.AddComponent<Image>();
            itemImage.color = new Color(1f, 1f, 1f, 0.01f);
            var itemToggle = item.gameObject.AddComponent<Toggle>();
            itemToggle.targetGraphic = itemImage;
            var itemLabel = AddText(item.gameObject, "Item Label", item, "选项 1", config);
            itemLabel.alignment = TextAlignmentOptions.Left;
            itemLabel.rectTransform.offsetMin = new Vector2(12f, 4f);
            itemLabel.rectTransform.offsetMax = new Vector2(-12f, -4f);
            scrollRect.viewport = viewport;
            scrollRect.content = templateContent;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            dropdown.template = template;
            dropdown.captionText = caption;
            dropdown.itemText = itemLabel;
            dropdown.options = new List<TMP_Dropdown.OptionData>
            {
                new TMP_Dropdown.OptionData(string.IsNullOrEmpty(content) ? "选项 1" : content),
                new TMP_Dropdown.OptionData("选项 2")
            };
            template.gameObject.SetActive(false);
        }

        /// <summary>
        /// 挂开关组组件，页签模式下额外补横排布局
        /// </summary>
        /// <param name="target">目标 GameObject</param>
        /// <param name="tabStyle">true 表示按页签风格再补一个横排布局</param>
        static void BuildToggleGroup(GameObject target, bool tabStyle)
        {
            var group = target.GetComponent<ToggleGroup>();
            if (group == null) group = target.AddComponent<ToggleGroup>();
            group.allowSwitchOff = false;
            if (!tabStyle) return;
            //页签需要横向排布，没有布局组件时补一个默认横排
            var layout = target.GetComponent<HorizontalLayoutGroup>();
            if (layout == null) layout = target.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 8f;
            layout.padding = new RectOffset(0, 0, 0, 0);
        }

        /// <summary>
        /// 用图片自身形状做遮罩
        /// </summary>
        /// <param name="target">目标 GameObject</param>
        static void BuildMask(GameObject target)
        {
            if (target.GetComponent<Graphic>() == null)
            {
                var image = target.AddComponent<Image>();
                image.color = Color.white;
            }
            var mask = target.AddComponent<Mask>();
            mask.showMaskGraphic = false;
        }

        /// <summary>
        /// 用 RawImage 显示精灵原始贴图
        /// </summary>
        /// <param name="node">生成节点</param>
        /// <param name="target">目标 GameObject</param>
        /// <param name="config">生成配置</param>
        static void BuildRawImage(BuildNode node, GameObject target, PSBUIConfig config)
        {
            var rawImage = target.AddComponent<RawImage>();
            rawImage.texture = node.sprite != null ? node.sprite.texture : null;
            rawImage.raycastTarget = config.imageRaycastTarget;
            float alpha = config.applyOpacity ? Mathf.Clamp01(node.inheritedOpacity) : 1f;
            rawImage.color = new Color(1f, 1f, 1f, alpha);
        }

        /// <summary>
        /// 创建竖向排列容器
        /// </summary>
        /// <param name="target">目标 GameObject</param>
        static void BuildVerticalLayout(GameObject target)
        {
            var layout = target.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 8f;
            layout.padding = new RectOffset(8, 8, 8, 8);
        }

        /// <summary>
        /// 创建横向排列容器
        /// </summary>
        /// <param name="target">目标 GameObject</param>
        static void BuildHorizontalLayout(GameObject target)
        {
            var layout = target.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 8f;
            layout.padding = new RectOffset(8, 8, 8, 8);
        }

        /// <summary>
        /// 创建网格排列容器
        /// </summary>
        /// <param name="target">目标 GameObject</param>
        static void BuildGridLayout(GameObject target)
        {
            var layout = target.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(120f, 120f);
            layout.spacing = new Vector2(8f, 8f);
            layout.constraint = GridLayoutGroup.Constraint.Flexible;
        }

        /// <summary>
        /// 在父节点下创建一个铺满父级的空矩形
        /// </summary>
        /// <param name="name">节点名字</param>
        /// <param name="parent">父节点</param>
        /// <returns>创建好的矩形</returns>
        static RectTransform CreateRect(string name, RectTransform parent)
        {
            var child = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)child.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            return rect;
        }

        /// <summary>
        /// 处理隐藏图层：跳过、未激活或保持激活但透明
        /// </summary>
        /// <param name="node">生成节点</param>
        /// <param name="target">目标 GameObject</param>
        /// <param name="config">生成配置</param>
        static void ApplyHidden(BuildNode node, GameObject target, PSBUIConfig config)
        {
            if (!node.hidden) return;
            if (config.hiddenMode == PSBUIHiddenMode.Inactive)
            {
                target.SetActive(false);
                return;
            }
            if (config.hiddenMode == PSBUIHiddenMode.KeepActive)
            {
                var group = target.GetComponent<CanvasGroup>();
                if (group == null) group = target.AddComponent<CanvasGroup>();
                group.alpha = 0f;
            }
        }

        /// <summary>
        /// 取图层名里冒号后面的内容，作为按钮文字、文本占位或开关标题
        /// 命名格式约定为「类型：内容」，没有冒号时退回去掉规则前缀后的名字
        /// </summary>
        /// <param name="node">生成节点</param>
        /// <returns>内容文本</returns>
        static string GetRuleContent(BuildNode node)
        {
            string name = node.name;
            if (string.IsNullOrEmpty(name)) return string.Empty;
            int colon = name.IndexOfAny(new[] { '：', ':' });
            if (colon >= 0 && colon + 1 < name.Length)
            {
                string content = name.Substring(colon + 1).Trim();
                if (!string.IsNullOrEmpty(content)) return content;
            }
            if (!string.IsNullOrEmpty(node.matchedRule) && name.StartsWith(node.matchedRule, StringComparison.OrdinalIgnoreCase))
                name = name.Substring(node.matchedRule.Length);
            name = name.TrimStart(' ', '：', ':', '_', '-');
            return string.IsNullOrEmpty(name) ? node.name : name;
        }

        /// <summary>
        /// 确保项目内目录存在
        /// </summary>
        /// <param name="folder">以 Assets 开头的目录路径</param>
        static void EnsureFolder(string folder)
        {
            folder = folder.Replace('\\', '/').TrimEnd('/');
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder)) return;
            string[] parts = folder.Split('/');
            string current = parts[0];
            bool created = false;
            for (int i = 1; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i])) continue;
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                    created = true;
                }
                current = next;
            }
            //新建目录后要刷新一次，否则紧接着保存预制体会报路径不存在
            if (created) AssetDatabase.Refresh();
        }

        /// <summary>
        /// 清理预制体文件名里的非法字符
        /// </summary>
        /// <param name="name">原始名字</param>
        /// <returns>安全的文件名</returns>
        static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "PSB_UI";
            var invalid = Path.GetInvalidFileNameChars();
            var builder = new System.Text.StringBuilder(name.Length);
            foreach (char c in name)
            {
                bool bad = false;
                foreach (char invalidChar in invalid)
                {
                    if (c != invalidChar) continue;
                    bad = true;
                    break;
                }
                builder.Append(bad ? '_' : c);
            }
            return builder.ToString();
        }
    }
}
