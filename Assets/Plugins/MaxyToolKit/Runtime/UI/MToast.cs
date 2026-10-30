using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MaxyToolKit.Animation;

namespace MaxyToolKit.UI
{
    /// <summary>
    /// 轻量提示条：顶部弹出一条短提示，排队依次播放，适合做「已保存」「银两不足」这类反馈
    /// 首次调用时自动创建播放器画布，之后复用，不需要在场景里预先摆对象
    /// </summary>
    public static class MToast
    {
        /// <summary>默认显示时长</summary>
        public static float DefaultDuration = 1.8f;
        /// <summary>默认背景色</summary>
        public static Color DefaultBackground = new Color(0.06f, 0.05f, 0.04f, 0.82f);
        /// <summary>默认文字色</summary>
        public static Color DefaultTextColor = new Color(1f, 0.95f, 0.84f, 1f);
        /// <summary>字体，为空时用 TMP 默认字体</summary>
        public static TMP_FontAsset DefaultFont;

        /// <summary>提示播放器，懒加载</summary>
        static MToastRunner _runner;

        /// <summary>
        /// 弹出一条提示，多个提示会按顺序排队播放
        /// </summary>
        /// <param name="message">提示内容</param>
        /// <param name="duration">显示时长</param>
        /// <param name="backgroundColor">背景色，传空用默认色</param>
        /// <param name="textColor">文字色，传空用默认色</param>
        public static void Show(string message, float duration = -1f, Color? backgroundColor = null, Color? textColor = null)
        {
            if (string.IsNullOrEmpty(message)) return;
            EnsureRunner();
            _runner.Enqueue(message, duration < 0f ? DefaultDuration : duration,
                backgroundColor ?? DefaultBackground, textColor ?? DefaultTextColor);
        }

        /// <summary>
        /// 清空正在播放和排队的提示
        /// </summary>
        public static void Clear()
        {
            if (_runner != null) _runner.ClearAll();
        }

        /// <summary>
        /// 懒加载创建提示播放器
        /// </summary>
        static void EnsureRunner()
        {
            if (_runner != null) return;
            var host = MUIFactory.CreateCanvas("MaxyToolKit 提示条", new Vector2(1920f, 1080f), 31000);
            Object.DontDestroyOnLoad(host.gameObject);
            _runner = host.gameObject.AddComponent<MToastRunner>();
        }
    }

    /// <summary>
    /// 提示条的播放器，挂在自动创建的全屏画布上
    /// </summary>
    internal sealed class MToastRunner : MonoBehaviour
    {
        /// <summary>待播放的提示队列</summary>
        readonly Queue<ToastItem> _queue = new Queue<ToastItem>();
        /// <summary>正在播放的提示对象</summary>
        GameObject _current;
        /// <summary>是否已经在跑播放协程</summary>
        bool _running;

        /// <summary>把一条提示加进队列</summary>
        /// <param name="message">提示内容</param>
        /// <param name="duration">显示时长</param>
        /// <param name="background">背景色</param>
        /// <param name="textColor">文字色</param>
        public void Enqueue(string message, float duration, Color background, Color textColor)
        {
            _queue.Enqueue(new ToastItem { message = message, duration = duration, background = background, textColor = textColor });
            if (_running) return;
            StartCoroutine(PlayQueue());
        }

        /// <summary>清空队列并立刻收起当前提示</summary>
        public void ClearAll()
        {
            _queue.Clear();
            if (_current != null) Destroy(_current);
            _current = null;
            _running = false;
            StopAllCoroutines();
        }

        /// <summary>依次播放队列里的提示</summary>
        /// <returns>协程枚举器</returns>
        IEnumerator PlayQueue()
        {
            _running = true;
            while (_queue.Count > 0)
            {
                var item = _queue.Dequeue();
                _current = CreateToast(item);
                var group = _current.GetComponent<CanvasGroup>();
                var rect = (RectTransform)_current.transform;
                //入场：从上方落下并淡入，停留后向上飘出
                group.alpha = 0f;
                rect.anchoredPosition = new Vector2(0f, -96f);
                var entrance = DOTween.Sequence().SetUpdate(true).SetLink(_current);
                entrance.Join(MTween.CanvasGroupFade(group, 1f, MTween.Fast));
                entrance.Join(rect.DOAnchorPos(new Vector2(0f, -120f), MTween.Normal).SetEase(Ease.OutCubic));
                yield return entrance.WaitForCompletion();
                yield return new WaitForSecondsRealtime(item.duration);
                if (_current == null) continue;
                var exit = DOTween.Sequence().SetUpdate(true).SetLink(_current);
                exit.Join(MTween.CanvasGroupFade(group, 0f, MTween.Fast));
                exit.Join(rect.DOAnchorPos(new Vector2(0f, -150f), MTween.Fast).SetEase(Ease.InCubic));
                yield return exit.WaitForCompletion();
                if (_current != null) Destroy(_current);
                _current = null;
                yield return new WaitForSecondsRealtime(0.06f);
            }
            _running = false;
        }

        /// <summary>创建一条提示的显示对象</summary>
        /// <param name="item">提示数据</param>
        /// <returns>创建好的提示对象</returns>
        GameObject CreateToast(ToastItem item)
        {
            var root = MUIFactory.CreateRect("提示", transform);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            //背景与文字用自适应尺寸，文字长短都不会顶出底板
            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 12, 12);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            var fitter = root.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var background = root.gameObject.AddComponent<Image>();
            background.color = item.background;
            background.raycastTarget = false;
            var text = MUIFactory.CreateText("文字", root, item.message, MToast.DefaultFont, 22f, item.textColor);
            text.enableWordWrapping = false;
            text.raycastTarget = false;
            //布局组会接管尺寸，这里把锚点收回中心，避免拉伸锚点与布局组互相打架
            var textRect = text.rectTransform;
            textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            return root.gameObject;
        }

        /// <summary>一条提示的播放数据</summary>
        struct ToastItem
        {
            /// <summary>提示文字</summary>
            public string message;
            /// <summary>显示时长</summary>
            public float duration;
            /// <summary>背景色</summary>
            public Color background;
            /// <summary>文字色</summary>
            public Color textColor;
        }
    }
}
