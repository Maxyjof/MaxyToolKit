using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MaxyToolKit.UI
{
    /// <summary>
    /// 运行时生成 UI 的迷你工厂，把项目里重复的「新建物体 + 配 RectTransform + 配 TextMeshPro」样板收敛成一行调用
    /// 全部接口都是可选的，现有手工搭建的界面不需要改动
    /// </summary>
    /// <remarks>
    /// 运行时新建的 TextMeshPro 如果先激活再配字体与材质，会触发 TMP_SubMeshUI 的 OnEnable 空引用
    /// 因此这里的文本创建统一顺序为「创建为未激活 → 配好字体/文字 → 再激活」
    /// </remarks>
    public static class MUIFactory
    {
        /// <summary>文本默认颜色，深棕，和浅色卡面搭配</summary>
        public static readonly Color DefaultTextColor = new Color(0.18f, 0.14f, 0.09f, 1f);

        /// <summary>
        /// 创建一个全屏 Canvas，并补上 CanvasScaler 与 GraphicRaycaster
        /// </summary>
        /// <param name="name">Canvas 名字</param>
        /// <param name="referenceResolution">参考分辨率</param>
        /// <param name="sortOrder">画布排序，数值大的显示在上面</param>
        /// <returns>创建好的 Canvas</returns>
        public static Canvas CreateCanvas(string name, Vector2 referenceResolution, int sortOrder = 0)
        {
            var gameObject = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = gameObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = gameObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        /// <summary>
        /// 创建一个空矩形节点，默认居中锚定
        /// </summary>
        /// <param name="name">节点名字</param>
        /// <param name="parent">父节点，可以为空表示根节点</param>
        /// <returns>创建好的矩形</returns>
        public static RectTransform CreateRect(string name, Transform parent = null)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)gameObject.transform;
            if (parent != null) rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            return rect;
        }

        /// <summary>
        /// 把矩形拉满父级并留出内边距
        /// </summary>
        /// <param name="rect">目标矩形</param>
        /// <param name="padding">四边内边距</param>
        public static void Stretch(RectTransform rect, float padding = 0f)
        {
            if (rect == null) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        /// <summary>
        /// 创建一个 Image 节点
        /// </summary>
        /// <param name="name">节点名字</param>
        /// <param name="parent">父节点</param>
        /// <param name="sprite">图片，允许为空表示纯色块</param>
        /// <param name="color">图片颜色，空表示白色</param>
        /// <param name="raycastTarget">是否响应点击</param>
        /// <returns>创建好的图片组件</returns>
        public static Image CreateImage(string name, Transform parent, Sprite sprite = null,
            Color? color = null, bool raycastTarget = false)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color ?? Color.white;
            image.raycastTarget = raycastTarget;
            return image;
        }

        /// <summary>
        /// 创建一个 TextMeshPro 文本节点
        /// </summary>
        /// <param name="name">节点名字</param>
        /// <param name="parent">父节点</param>
        /// <param name="content">显示内容</param>
        /// <param name="font">字体，空则用 TMP 默认字体</param>
        /// <param name="fontSize">字号</param>
        /// <param name="color">颜色，空则用默认深棕色</param>
        /// <param name="alignment">对齐方式</param>
        /// <returns>创建好的文本组件</returns>
        public static TextMeshProUGUI CreateText(string name, Transform parent, string content,
            TMP_FontAsset font = null, float fontSize = 24f, Color? color = null,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            //先建成未激活，配好字体和文字后再激活，避免 TMP 子网格在初始化阶段报空引用
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.SetActive(false);
            var rect = (RectTransform)gameObject.transform;
            if (parent != null) rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.text = content ?? string.Empty;
            text.fontSize = fontSize;
            text.color = color ?? DefaultTextColor;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            gameObject.SetActive(true);
            return text;
        }

        /// <summary>
        /// 创建一个按钮，自动补一个文字子节点
        /// </summary>
        /// <param name="name">节点名字</param>
        /// <param name="parent">父节点</param>
        /// <param name="label">按钮文字</param>
        /// <param name="onClick">点击回调</param>
        /// <param name="sprite">按钮底图，允许为空表示纯色块</param>
        /// <param name="font">字体，空则用 TMP 默认字体</param>
        /// <returns>创建好的按钮组件</returns>
        public static Button CreateButton(string name, Transform parent, string label,
            UnityEngine.Events.UnityAction onClick, Sprite sprite = null, TMP_FontAsset font = null)
        {
            var image = CreateImage(name, parent, sprite, null, true);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            if (onClick != null) button.onClick.AddListener(onClick);
            if (!string.IsNullOrEmpty(label))
            {
                var text = CreateText("文字", image.transform, label, font, 22f, DefaultTextColor);
                var rect = text.rectTransform;
                rect.offsetMin = new Vector2(12f, 6f);
                rect.offsetMax = new Vector2(-12f, -6f);
            }
            return button;
        }

        /// <summary>
        /// 给按钮补一套可撤销的悬停/按下颜色
        /// </summary>
        /// <param name="button">目标按钮</param>
        /// <param name="normal">常态颜色</param>
        /// <param name="highlighted">悬停颜色</param>
        /// <param name="pressed">按下颜色</param>
        public static void StyleButtonColors(Button button, Color normal, Color highlighted, Color pressed)
        {
            if (button == null) return;
            var colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.pressedColor = pressed;
            button.colors = colors;
        }

        /// <summary>
        /// 递归设置某个节点下所有图形的点击响应开关
        /// </summary>
        /// <param name="root">根节点</param>
        /// <param name="raycastTarget">是否允许响应点击</param>
        public static void SetRaycastTargetRecursively(Transform root, bool raycastTarget)
        {
            if (root == null) return;
            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = raycastTarget;
        }
    }
}
