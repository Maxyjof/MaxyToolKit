using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using MaxyToolKit.Async;
using MaxyToolKit.MComponent;
using MaxyToolKit.Core;
using MaxyToolKit.Data;
using MaxyToolKit.Event;
using MaxyToolKit.Save;
using MaxyToolKit.Tool;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaxyToolKit.Examples
{
    /// <summary>
    /// 示例场景使用的事件消息
    /// </summary>
    public readonly struct ExamplePingEvent
    {
        /// <summary>
        /// 创建一条示例事件消息
        /// </summary>
        /// <param name="index">事件序号</param>
        public ExamplePingEvent(int index) => Index = index;

        /// <summary>
        /// 获取事件序号
        /// </summary>
        public int Index { get; }
    }

    /// <summary>
    /// 演示ISystem生命周期的简单全局系统
    /// </summary>
    public sealed class ExampleCounterSystem : ISystem
    {
        /// <summary>
        /// 获取系统是否已经初始化
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// 获取系统收到的事件数量
        /// </summary>
        public int EventCount { get; private set; }

        /// <summary>
        /// 初始化系统并重置统计值
        /// </summary>
        public void Initialize()
        {
            IsInitialized = true;
            EventCount = 0;
        }

        /// <summary>
        /// 记录一条收到的事件
        /// </summary>
        /// <param name="message">收到的示例事件</param>
        public void Record(ExamplePingEvent message) => EventCount++;

        /// <summary>
        /// 关闭系统并清空运行状态
        /// </summary>
        public void Shutdown() => IsInitialized = false;
    }

    /// <summary>
    /// 演示MEventBus和MSystemCenter的可运行示例
    /// </summary>
    public class EventBusExampleSceneLogic : MonoBehaviour
    {
        private ExampleCounterSystem _system;
        private IDisposable _subscription;
        private Text _statusText;
        private Text _eventText;
        private int _publishedCount;

        /// <summary>
        /// 创建界面并注册示例系统
        /// </summary>
        protected void InitializeExample()
        {
            var uiRoot = ExampleUiFactory.CreateHeader(transform, "示例一：事件总线与全局系统", "用一条消息连接界面和全局系统");
            _statusText = ExampleUiFactory.CreateText(uiRoot, "系统状态：准备中", 24, ExampleUiFactory.SecondaryText);
            _eventText = ExampleUiFactory.CreateText(uiRoot, "已发布事件：0\n系统收到事件：0", 28, Color.white);
            ExampleUiFactory.CreateText(uiRoot, "点击按钮后，界面发布ExamplePingEvent，系统通过MEventBus收到消息", 18, ExampleUiFactory.SecondaryText);
            ExampleUiFactory.CreateButton(uiRoot, "发布一条事件", PublishEvent);
            ExampleUiFactory.CreateButton(uiRoot, "重置系统中心", ResetSystem);

            _system = MSystemCenter.Register(new ExampleCounterSystem(), true);
            _subscription = MEventBus.Global.Subscribe<ExamplePingEvent>(OnEventReceived);
            UpdateView();
        }

        /// <summary>
        /// 发布一条示例消息
        /// </summary>
        private void PublishEvent()
        {
            _publishedCount++;
            MEventBus.Global.Publish(new ExamplePingEvent(_publishedCount));
            UpdateView();
        }

        /// <summary>
        /// 展示事件总线收到的消息并更新界面
        /// </summary>
        /// <param name="message">收到的示例消息</param>
        private void OnEventReceived(ExamplePingEvent message)
        {
            if (_system != null) _system.Record(message);
            UpdateView();
        }

        /// <summary>
        /// 清空全局系统和事件订阅
        /// </summary>
        private void ResetSystem()
        {
            MSystemCenter.Reset();
            _system = MSystemCenter.Register(new ExampleCounterSystem(), true);
            _subscription?.Dispose();
            _subscription = MEventBus.Global.Subscribe<ExamplePingEvent>(OnEventReceived);
            _publishedCount = 0;
            UpdateView();
        }

        /// <summary>
        /// 更新示例状态文本
        /// </summary>
        private void UpdateView()
        {
            var initialized = _system != null && _system.IsInitialized;
            _statusText.text = "系统状态：" + (initialized ? "已初始化" : "已重置");
            _eventText.text = $"已发布事件：{_publishedCount}\n系统收到事件：{(_system == null ? 0 : _system.EventCount)}";
        }

        /// <summary>
        /// 释放示例订阅和系统
        /// </summary>
        protected void CleanupExample()
        {
            _subscription?.Dispose();
            if (_system != null && MSystemCenter.Get<ExampleCounterSystem>() == _system) MSystemCenter.Remove<ExampleCounterSystem>();
        }
    }

    /// <summary>
    /// 演示MProperty、列表属性、字典属性和MSave的可运行示例
    /// </summary>
    public class PropertyExampleSceneLogic : MonoBehaviour
    {
        private readonly MProperty<int> _score = new MProperty<int>(25);
        private readonly MListProperty<string> _inventory = new MListProperty<string>(new[] { "钥匙", "地图" });
        private readonly MDictionaryProperty<string, int> _itemCounts = new MDictionaryProperty<string, int>();
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private Text _valueText;
        private Text _listText;
        private Text _dictionaryText;
        private Text _storageText;
        private Slider _slider;
        private int _addedItemIndex;

        /// <summary>
        /// 创建响应式数据示例界面
        /// </summary>
        protected void InitializeExample()
        {
            var uiRoot = ExampleUiFactory.CreateHeader(transform, "示例二：MProperty响应式数据", "值变化时自动通知界面，集合变化时同步刷新");
            _valueText = ExampleUiFactory.CreateText(uiRoot, "分数：25", 28, Color.white);
            _slider = ExampleUiFactory.CreateSlider(uiRoot, 0f, 100f, _score.Value, value => _score.Value = Mathf.RoundToInt(value));
            _listText = ExampleUiFactory.CreateText(uiRoot, "列表：", 20, ExampleUiFactory.SecondaryText);
            _dictionaryText = ExampleUiFactory.CreateText(uiRoot, "字典：", 20, ExampleUiFactory.SecondaryText);
            _storageText = ExampleUiFactory.CreateText(uiRoot, "存档：尚未操作", 18, ExampleUiFactory.SecondaryText);
            ExampleUiFactory.CreateButton(uiRoot, "添加一个列表元素", AddInventoryItem);
            ExampleUiFactory.CreateButton(uiRoot, "保存到ES3", SaveData);
            ExampleUiFactory.CreateButton(uiRoot, "从ES3读取", LoadData);

            _itemCounts.Add("钥匙", 1);
            _itemCounts.Add("地图", 1);
            _subscriptions.Add(_score.Subscribe(UpdateScore, true));
            _subscriptions.Add(_inventory.Subscribe(UpdateInventory, true));
            _subscriptions.Add(_itemCounts.Subscribe(UpdateDictionary, true));
        }

        /// <summary>
        /// 更新分数显示
        /// </summary>
        /// <param name="value">当前分数</param>
        private void UpdateScore(int value)
        {
            _valueText.text = "分数：" + value;
            if (_slider != null && !Mathf.Approximately(_slider.value, value)) _slider.SetValueWithoutNotify(value);
        }

        /// <summary>
        /// 更新列表显示
        /// </summary>
        /// <param name="values">当前列表</param>
        private void UpdateInventory(IReadOnlyList<string> values) => _listText.text = "列表：" + string.Join("、", values);

        /// <summary>
        /// 更新字典显示
        /// </summary>
        /// <param name="values">当前字典</param>
        private void UpdateDictionary(IReadOnlyDictionary<string, int> values) => _dictionaryText.text = "字典：" + string.Join("，", values.Select(pair => pair.Key + "=" + pair.Value));

        /// <summary>
        /// 添加列表和字典数据
        /// </summary>
        private void AddInventoryItem()
        {
            _addedItemIndex++;
            var itemName = "道具" + _addedItemIndex;
            _inventory.Add(itemName);
            _itemCounts[itemName] = 1;
        }

        /// <summary>
        /// 保存当前示例数据
        /// </summary>
        private void SaveData()
        {
            try
            {
                MSave.Save("MaxyToolKit.Example.Score", _score.Value);
                MSave.Save("MaxyToolKit.Example.Inventory", _inventory.ToList());
                _storageText.text = "存档：已保存";
            }
            catch (Exception exception)
            {
                _storageText.text = "存档：保存失败 " + exception.Message;
            }
        }

        /// <summary>
        /// 读取示例数据并触发属性通知
        /// </summary>
        private void LoadData()
        {
            try
            {
                _score.Value = MSave.Load("MaxyToolKit.Example.Score", _score.Value);
                _inventory.ReplaceSilently(MSave.Load("MaxyToolKit.Example.Inventory", _inventory.ToList()));
                _inventory.AddRange(Array.Empty<string>());
                _storageText.text = "存档：已读取";
                UpdateInventory(_inventory.ToList());
            }
            catch (Exception exception)
            {
                _storageText.text = "存档：读取失败 " + exception.Message;
            }
        }

        /// <summary>
        /// 释放属性订阅
        /// </summary>
        protected void CleanupExample()
        {
            foreach (var _subscription in _subscriptions) _subscription?.Dispose();
            _subscriptions.Clear();
        }
    }

    /// <summary>
    /// 演示MTask和FadeEffectOverlay的可运行示例
    /// </summary>
    public class AsyncExampleSceneLogic : MonoBehaviour
    {
        private CancellationTokenSource _cancellation;
        private Text _statusText;
        private Image _progressFill;
        private FadeEffectOverlay _overlay;

        /// <summary>
        /// 创建异步任务示例界面
        /// </summary>
        protected void InitializeExample()
        {
            var uiRoot = ExampleUiFactory.CreateHeader(transform, "示例三：异步等待与淡入淡出", "MTask负责等待，FadeEffectOverlay负责界面过渡");
            _statusText = ExampleUiFactory.CreateText(uiRoot, "状态：等待开始", 24, Color.white);
            _progressFill = ExampleUiFactory.CreateProgressBar(uiRoot);
            ExampleUiFactory.CreateButton(uiRoot, "开始异步流程", StartFlow);
            ExampleUiFactory.CreateButton(uiRoot, "显示或隐藏遮罩", ToggleOverlay);

            var overlayObject = ExampleUiFactory.CreateOverlay(uiRoot.parent);
            _overlay = overlayObject.AddComponent<FadeEffectOverlay>();
            _overlay.SetImmediate(0f);
        }

        /// <summary>
        /// 启动一个包含多次等待的异步流程
        /// </summary>
        private void StartFlow()
        {
            _cancellation?.Cancel();
            _cancellation?.Dispose();
            _cancellation = new CancellationTokenSource();
            RunFlowAsync(_cancellation.Token).Forget();
        }

        /// <summary>
        /// 按阶段更新进度并演示取消令牌
        /// </summary>
        /// <param name="token">用于中止流程的令牌</param>
        private async UniTaskVoid RunFlowAsync(CancellationToken token)
        {
            try
            {
                for (var index = 1; index <= 5; index++)
                {
                    _statusText.text = $"状态：正在执行第{index}阶段";
                    _progressFill.fillAmount = (index - 1) / 5f;
                    await MTask.Delay(0.5f, true, token);
                }

                _progressFill.fillAmount = 1f;
                _statusText.text = "状态：异步流程完成";
                await MTask.Delay(0.2f, true, token);
                _overlay.FadeIn(0.2f);
                await MTask.Delay(0.8f, true, token);
                _overlay.FadeOut(0.3f);
            }
            catch (OperationCanceledException)
            {
                _statusText.text = "状态：流程已取消";
            }
        }

        /// <summary>
        /// 在可见和隐藏状态之间切换遮罩
        /// </summary>
        private void ToggleOverlay()
        {
            if (_overlay.CanvasGroup.alpha > 0.01f) _overlay.FadeOut();
            else _overlay.FadeIn();
        }

        /// <summary>
        /// 取消未完成任务并释放资源
        /// </summary>
        protected void CleanupExample()
        {
            _cancellation?.Cancel();
            _cancellation?.Dispose();
        }
    }

    /// <summary>
    /// 演示MTool、DOTween和运行时对象操作的可运行示例
    /// </summary>
    public class ToolsExampleSceneLogic : MonoBehaviour
    {
        private GameObject _cube;
        private GameObject _cameraObject;
        private RectTransform _previewRect;
        private Text _positionText;
        private Text _utilityText;
        private int _moveCount;

        /// <summary>
        /// 创建工具示例界面和可操作立方体
        /// </summary>
        protected void InitializeExample()
        {
            var uiRoot = ExampleUiFactory.CreateHeader(transform, "示例四：常用工具与动画", "MTool处理对象和数值，DOTween负责轻量动画");
            var background = uiRoot.Find("背景")?.GetComponent<Image>();
            if (background != null) background.color = new Color(ExampleUiFactory.Background.r, ExampleUiFactory.Background.g, ExampleUiFactory.Background.b, 0.35f);
            _positionText = ExampleUiFactory.CreateText(uiRoot, "位置：", 24, Color.white);
            _utilityText = ExampleUiFactory.CreateText(uiRoot, "工具：等待操作", 18, ExampleUiFactory.SecondaryText);
            ExampleUiFactory.CreateButton(uiRoot, "随机移动并旋转", MoveObject);
            ExampleUiFactory.CreateButton(uiRoot, "重置对象", ResetObject);

            CreateExampleCamera();
            _previewRect = ExampleUiFactory.CreateObjectPreview(uiRoot.parent);

            _cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _cube.name = "MTool示例立方体";
            _cube.transform.SetParent(transform, false);
            _cube.transform.position = new Vector3(0f, -1.2f, 4f);
            _cube.transform.localScale = Vector3.one * 1.2f;
            UpdatePosition();
        }

        /// <summary>
        /// 创建仅供本示例使用的相机
        /// </summary>
        private void CreateExampleCamera()
        {
            if (Camera.main != null) return;
            _cameraObject = new GameObject("示例相机");
            var camera = _cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = ExampleUiFactory.Background;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.transform.rotation = Quaternion.identity;
        }

        /// <summary>
        /// 使用MTool和DOTween移动对象
        /// </summary>
        private void MoveObject()
        {
            if (_cube == null) return;
            _moveCount++;
            var x = MTool.Remap(Mathf.Sin(_moveCount * 1.3f), -1f, 1f, -2.3f, 2.3f);
            var target = new Vector3(x, -1.2f, 4f);
            _cube.transform.DOKill();
            _cube.transform.DOMove(target, 0.45f).SetEase(Ease.OutBack);
            _cube.transform.DORotate(new Vector3(0f, 180f, 0f), 0.45f, RotateMode.WorldAxisAdd);
            if (_previewRect != null)
            {
                _previewRect.DOKill();
                _previewRect.DOAnchorPos(new Vector2(x * 95f, 180f), 0.45f).SetEase(Ease.OutBack);
                _previewRect.DORotate(new Vector3(0f, 0f, 180f), 0.45f, RotateMode.WorldAxisAdd);
            }
            _utilityText.text = "工具：使用MTool.Remap计算位置，并用DOTween播放动画";
            UpdatePosition();
        }

        /// <summary>
        /// 恢复对象初始位置和旋转
        /// </summary>
        private void ResetObject()
        {
            if (_cube == null) return;
            _cube.transform.DOKill();
            _cube.transform.SetPositionAndRotation(new Vector3(0f, -1.2f, 4f), Quaternion.identity);
            if (_previewRect != null)
            {
                _previewRect.DOKill();
                _previewRect.anchoredPosition = new Vector2(0f, 180f);
                _previewRect.localRotation = Quaternion.identity;
            }
            _utilityText.text = "工具：Transform已恢复初始状态";
            UpdatePosition();
        }

        /// <summary>
        /// 更新对象位置文本
        /// </summary>
        private void UpdatePosition()
        {
            if (_cube != null) _positionText.text = $"位置：{_cube.transform.position}";
        }

        /// <summary>
        /// 销毁示例对象并停止动画
        /// </summary>
        protected void CleanupExample()
        {
            if (_cube != null) _cube.transform.DOKill();
            if (_previewRect != null) _previewRect.DOKill();
            if (_cameraObject != null) Destroy(_cameraObject);
        }
    }

    /// <summary>
    /// 为示例场景创建统一的简单界面
    /// </summary>
    internal static class ExampleUiFactory
    {
        public static readonly Color Background = new Color(0.035f, 0.055f, 0.09f, 1f);
        public static readonly Color Panel = new Color(0.08f, 0.12f, 0.19f, 0.96f);
        public static readonly Color Accent = new Color(0.18f, 0.62f, 0.95f, 1f);
        public static readonly Color SecondaryText = new Color(0.68f, 0.76f, 0.86f, 1f);

        /// <summary>
        /// 创建全屏画布和背景
        /// </summary>
        /// <param name="parent">示例根对象</param>
        /// <returns>创建的内容容器</returns>
        public static RectTransform CreateCanvas(Transform parent)
        {
            //示例场景需要事件系统才能接收真实鼠标和键盘输入
            if (EventSystem.current == null)
            {
                var eventSystemObject = new GameObject("示例事件系统", typeof(EventSystem), typeof(StandaloneInputModule));
                eventSystemObject.transform.SetParent(parent, false);
            }
            //示例场景需要相机才能让Unity Game视图正常显示
            if (Camera.main == null)
            {
                var _cameraObject = new GameObject("示例相机", typeof(Camera));
                _cameraObject.transform.SetParent(parent, false);
                _cameraObject.tag = "MainCamera";
                var camera = _cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Background;
                camera.transform.localPosition = new Vector3(0f, 0f, -10f);
                camera.transform.localRotation = Quaternion.identity;
            }
            var canvasObject = new GameObject("示例画布", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280f, 720f);
            var root = canvasObject.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            var background = CreatePanel(root, "背景", Background, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 0f), Vector2.zero);
            background.SetAsFirstSibling();
            return root;
        }

        /// <summary>
        /// 创建示例标题和说明文字
        /// </summary>
        /// <param name="parent">父级对象</param>
        /// <param name="title">标题文字</param>
        /// <param name="subtitle">说明文字</param>
        public static RectTransform CreateHeader(Transform parent, string title, string subtitle)
        {
            var root = CreateCanvas(parent);
            var contentObject = new GameObject("示例内容", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentObject.transform.SetParent(root, false);
            var content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0.5f, 1f);
            content.anchorMax = new Vector2(0.5f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(980f, 620f);
            content.anchoredPosition = new Vector2(0f, -12f);
            var layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            CreateText(content, title, 34, Color.white, Vector2.zero, TextAnchor.MiddleCenter);
            CreateText(content, subtitle, 20, SecondaryText, Vector2.zero, TextAnchor.MiddleCenter);
            return content;
        }

        /// <summary>
        /// 创建一段文本
        /// </summary>
        /// <param name="parent">父级对象</param>
        /// <param name="content">文本内容</param>
        /// <param name="fontSize">字号</param>
        /// <param name="color">文本颜色</param>
        /// <param name="offset">相对于中心的偏移</param>
        /// <param name="alignment">文本对齐方式</param>
        /// <returns>创建的文本组件</returns>
        public static Text CreateText(Transform parent, string content, int fontSize, Color color, Vector2? offset = null, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var objectName = "文本_" + content.Split('\n')[0];
            var textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var rect = text.rectTransform;
            rect.sizeDelta = new Vector2(980f, Mathf.Max(54f, fontSize * 2.2f));
            rect.anchoredPosition = offset ?? new Vector2(0f, 100f - parent.childCount * 40f);
            var layoutElement = textObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = rect.sizeDelta.x;
            layoutElement.preferredHeight = rect.sizeDelta.y;
            return text;
        }

        /// <summary>
        /// 创建可点击按钮
        /// </summary>
        /// <param name="parent">父级对象</param>
        /// <param name="caption">按钮文字</param>
        /// <param name="onClick">点击回调</param>
        /// <returns>创建的按钮组件</returns>
        public static Button CreateButton(Transform parent, string caption, UnityEngine.Events.UnityAction onClick)
        {
            var buttonObject = new GameObject("按钮_" + caption, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(360f, 58f);
            rect.anchoredPosition = new Vector2(0f, 80f - parent.childCount * 70f);
            var layoutElement = buttonObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = rect.sizeDelta.x;
            layoutElement.preferredHeight = rect.sizeDelta.y;
            var image = buttonObject.GetComponent<Image>();
            image.color = Accent;
            var button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(onClick);
            var text = CreateText(buttonObject.transform, caption, 20, Color.white, Vector2.zero, TextAnchor.MiddleCenter);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            text.rectTransform.sizeDelta = Vector2.zero;
            return button;
        }

        /// <summary>
        /// 创建数值滑动条
        /// </summary>
        /// <param name="parent">父级对象</param>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <param name="value">初始值</param>
        /// <param name="onChanged">数值变化回调</param>
        /// <returns>创建的滑动条组件</returns>
        public static Slider CreateSlider(Transform parent, float min, float max, float value, UnityEngine.Events.UnityAction<float> onChanged)
        {
            var sliderObject = new GameObject("分数滑动条", typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(parent, false);
            var rect = sliderObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(760f, 46f);
            rect.anchoredPosition = new Vector2(0f, 40f);
            var layoutElement = sliderObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = rect.sizeDelta.x;
            layoutElement.preferredHeight = rect.sizeDelta.y;
            var background = CreatePanel(sliderObject.transform, "滑动条背景", new Color(0.12f, 0.17f, 0.25f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Vector2.zero);
            background.anchorMin = Vector2.zero;
            background.anchorMax = Vector2.one;
            background.pivot = new Vector2(0.5f, 0.5f);
            background.sizeDelta = Vector2.zero;
            var fill = CreatePanel(sliderObject.transform, "滑动条填充", Accent, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero, Vector2.zero);
            fill.anchorMin = new Vector2(0f, 0.15f);
            fill.anchorMax = new Vector2(0f, 0.85f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.sizeDelta = Vector2.zero;
            var handle = CreatePanel(sliderObject.transform, "滑动条把手", Color.white, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero, Vector2.zero);
            handle.anchorMin = new Vector2(0f, 0f);
            handle.anchorMax = new Vector2(0f, 1f);
            handle.sizeDelta = new Vector2(26f, 26f);
            var _slider = sliderObject.GetComponent<Slider>();
            _slider.minValue = min;
            _slider.maxValue = max;
            _slider.value = value;
            _slider.fillRect = fill;
            _slider.handleRect = handle;
            _slider.targetGraphic = handle.GetComponent<Image>();
            _slider.onValueChanged.AddListener(onChanged);
            return _slider;
        }

        /// <summary>
        /// 创建进度条
        /// </summary>
        /// <param name="parent">父级对象</param>
        /// <returns>进度填充图像</returns>
        public static Image CreateProgressBar(Transform parent)
        {
            var progressRoot = CreatePanel(parent, "进度条", new Color(0.12f, 0.17f, 0.25f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Vector2.zero);
            progressRoot.sizeDelta = new Vector2(760f, 36f);
            var layoutElement = progressRoot.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = progressRoot.sizeDelta.x;
            layoutElement.preferredHeight = progressRoot.sizeDelta.y;
            var fill = CreatePanel(progressRoot, "进度填充", Accent, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero, Vector2.zero);
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(1f, 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.sizeDelta = new Vector2(0f, 0f);
            var image = fill.GetComponent<Image>();
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillAmount = 0f;
            return image;
        }

        /// <summary>
        /// 创建用于显示工具动画结果的二维预览对象
        /// </summary>
        /// <param name="parent">父级对象</param>
        /// <returns>预览对象的矩形变换</returns>
        public static RectTransform CreateObjectPreview(Transform parent)
        {
            var preview = CreatePanel(parent, "对象动画预览", new Color(0.98f, 0.55f, 0.18f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), Vector2.zero, Vector2.zero);
            preview.anchorMin = new Vector2(0.5f, 0f);
            preview.anchorMax = new Vector2(0.5f, 0f);
            preview.pivot = new Vector2(0.5f, 0.5f);
            preview.sizeDelta = new Vector2(110f, 64f);
            preview.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            return preview;
        }

        /// <summary>
        /// 创建全屏淡入淡出遮罩
        /// </summary>
        /// <param name="parent">父级对象</param>
        /// <returns>遮罩对象</returns>
        public static GameObject CreateOverlay(Transform parent)
        {
            var _overlay = new GameObject("淡入淡出遮罩", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            _overlay.transform.SetParent(parent, false);
            var rect = _overlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _overlay.GetComponent<Image>().color = Color.black;
            _overlay.transform.SetAsLastSibling();
            return _overlay;
        }

        /// <summary>
        /// 创建带颜色的矩形界面对象
        /// </summary>
        /// <param name="parent">父级对象</param>
        /// <param name="name">对象名称</param>
        /// <param name="color">背景颜色</param>
        /// <param name="pivot">锚点和中心点</param>
        /// <param name="position">位置偏移</param>
        /// <param name="offsetMin">最小偏移</param>
        /// <param name="offsetMax">最大偏移</param>
        /// <returns>创建的矩形变换</returns>
        private static RectTransform CreatePanel(Transform parent, string name, Color color, Vector2 pivot, Vector2 position, Vector2 offsetMin, Vector2 offsetMax)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = pivot;
            rect.anchorMax = pivot;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            if (offsetMin == Vector2.zero && offsetMax == Vector2.zero) rect.sizeDelta = new Vector2(1280f, 720f);
            panel.GetComponent<Image>().color = color;
            return rect;
        }
    }
}
