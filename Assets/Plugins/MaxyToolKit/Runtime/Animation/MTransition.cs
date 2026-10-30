using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using MaxyToolKit.UI;

namespace MaxyToolKit.Animation
{
    /// <summary>
    /// 全屏转场过渡：淡入淡出遮罩、加载切换、场景跳转前后的黑场
    /// 只在第一次使用时创建遮罩画布，之后的调用复用同一张遮罩，不占用场景资源
    /// </summary>
    /// <remarks>
    /// 遮罩始终使用 unscaledTime，暂停或 timeScale 为 0 时也能正常过渡
    /// 同一时间只允许一个转场，新调用会打断旧转场并接手回调
    /// </remarks>
    public static class MTransition
    {
        /// <summary>默认过渡时长</summary>
        public static float DefaultDuration = 0.28f;

        /// <summary>遮罩排序值，保证盖在任何界面之上</summary>
        const int OverlaySortingOrder = 32000;

        /// <summary>当前是否有转场正在播放</summary>
        public static bool IsBusy { get; private set; }

        /// <summary>转场遮罩的 CanvasGroup</summary>
        static CanvasGroup _overlayGroup;
        /// <summary>转场遮罩的底色图片</summary>
        static Image _overlayImage;
        /// <summary>当前正在播放的补间</summary>
        static Tween _activeTween;

        /// <summary>
        /// 淡出到全屏遮罩（画面被盖住），常用于切场景或弹大面板前
        /// </summary>
        /// <param name="color">遮罩颜色，默认黑色</param>
        /// <param name="duration">时长，小于 0 时用默认时长</param>
        /// <param name="onComplete">遮罩完全盖住后的回调</param>
        public static void FadeOut(Color color, float duration = -1f, Action onComplete = null)
        {
            FadeTo(1f, color, duration, onComplete);
        }

        /// <summary>
        /// 淡入回游戏画面（遮罩变透明）
        /// </summary>
        /// <param name="duration">时长，小于 0 时用默认时长</param>
        /// <param name="onComplete">遮罩完全透明后的回调</param>
        public static void FadeIn(float duration = -1f, Action onComplete = null)
        {
            FadeTo(0f, null, duration, onComplete);
        }

        /// <summary>
        /// 把遮罩透明度过渡到指定值
        /// </summary>
        /// <param name="alpha">目标透明度，0 完全透明，1 完全盖住</param>
        /// <param name="color">遮罩颜色，传空表示沿用上次颜色</param>
        /// <param name="duration">时长，小于 0 时用默认时长</param>
        /// <param name="onComplete">完成回调</param>
        public static void FadeTo(float alpha, Color? color = null, float duration = -1f, Action onComplete = null)
        {
            var group = EnsureOverlay();
            if (group == null) return;
            if (color.HasValue && _overlayImage != null) _overlayImage.color = color.Value;
            float realDuration = duration < 0f ? DefaultDuration : Mathf.Max(0f, duration);
            float target = Mathf.Clamp01(alpha);
            _activeTween?.Kill();
            IsBusy = true;
            //遮罩可见时挡住点击，避免过渡期间误触下面的界面
            if (group.alpha > 0.001f || target > 0.001f)
            {
                group.blocksRaycasts = true;
                group.gameObject.SetActive(true);
            }
            if (realDuration <= 0f)
            {
                group.alpha = target;
                FinishTransition(group, target, onComplete);
                return;
            }
            _activeTween = DOTween.To(() => group.alpha, value => group.alpha = value, target, realDuration)
                .SetEase(target > group.alpha ? Ease.OutQuad : Ease.InQuad)
                .SetUpdate(true)
                .SetLink(group.gameObject)
                .OnComplete(() => FinishTransition(group, target, onComplete));
        }

        /// <summary>
        /// 立即设置遮罩透明度并停止当前转场
        /// </summary>
        /// <param name="alpha">目标透明度</param>
        /// <param name="color">遮罩颜色，传空表示沿用上次颜色</param>
        public static void SetAlphaImmediate(float alpha, Color? color = null)
        {
            var group = EnsureOverlay();
            if (group == null) return;
            _activeTween?.Kill();
            _activeTween = null;
            if (color.HasValue && _overlayImage != null) _overlayImage.color = color.Value;
            group.alpha = Mathf.Clamp01(alpha);
            group.blocksRaycasts = group.alpha > 0.001f;
            group.gameObject.SetActive(group.alpha > 0.001f);
            IsBusy = false;
        }

        /// <summary>
        /// 转场结束后的收尾：同步遮挡状态并在透明时隐藏遮罩对象
        /// </summary>
        /// <param name="group">遮罩的 CanvasGroup</param>
        /// <param name="alpha">最终透明度</param>
        /// <param name="onComplete">完成回调</param>
        static void FinishTransition(CanvasGroup group, float alpha, Action onComplete)
        {
            _activeTween = null;
            IsBusy = false;
            if (group != null)
            {
                group.blocksRaycasts = alpha > 0.001f;
                if (alpha <= 0.001f) group.gameObject.SetActive(false);
            }
            onComplete?.Invoke();
        }

        /// <summary>
        /// 懒加载创建全屏遮罩，复用同一个对象
        /// </summary>
        /// <returns>遮罩的 CanvasGroup，创建失败时返回空</returns>
        static CanvasGroup EnsureOverlay()
        {
            if (_overlayGroup != null) return _overlayGroup;
            var canvasObject = new GameObject("MaxyToolKit 转场遮罩", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            UnityEngine.Object.DontDestroyOnLoad(canvasObject);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortingOrder;
            var imageRect = MUIFactory.CreateRect("遮罩", canvasObject.transform);
            MUIFactory.Stretch(imageRect);
            _overlayImage = imageRect.gameObject.AddComponent<Image>();
            _overlayImage.color = Color.black;
            _overlayImage.raycastTarget = true;
            _overlayGroup = canvasObject.AddComponent<CanvasGroup>();
            _overlayGroup.alpha = 0f;
            _overlayGroup.blocksRaycasts = false;
            _overlayGroup.interactable = false;
            canvasObject.SetActive(false);
            return _overlayGroup;
        }
    }
}
