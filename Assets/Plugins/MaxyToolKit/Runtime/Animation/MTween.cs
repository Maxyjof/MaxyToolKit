using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace MaxyToolKit.Animation
{
    /// <summary>
    /// DOTween 的常用补间封装，统一时长、缓动与防泄漏处理
    /// 目的只是少写重复代码：所有方法返回 Tween，需要时仍可直接链式调用 DOTween 原生接口
    /// </summary>
    /// <remarks>
    /// 项目里手写 DOScale/DOFade 时经常忘掉 SetLink 或忘记 Kill，导致对象销毁后补间仍在跑
    /// 这里统一先 DOKill 再建新补间，并默认 SetLink 到目标对象
    /// </remarks>
    public static class MTween
    {
        /// <summary>快速反馈时长</summary>
        public const float Fast = 0.16f;
        /// <summary>常规过渡时长</summary>
        public const float Normal = 0.24f;
        /// <summary>慢速展示时长</summary>
        public const float Slow = 0.4f;

        /// <summary>
        /// 淡入淡出一个图形
        /// </summary>
        /// <param name="graphic">目标图形</param>
        /// <param name="toAlpha">目标透明度</param>
        /// <param name="duration">时长</param>
        /// <param name="delay">延迟</param>
        /// <param name="unscaled">是否不受 Time.timeScale 影响</param>
        /// <returns>补间对象，可为空</returns>
        public static Tween Fade(Graphic graphic, float toAlpha, float duration = Normal,
            float delay = 0f, bool unscaled = true)
        {
            if (graphic == null) return null;
            var color = graphic.color;
            var target = color.a;
            graphic.DOKill();
            var tween = DOTween.To(() => color.a, value =>
            {
                color.a = value;
                if (graphic != null) graphic.color = color;
            }, toAlpha, duration).SetEase(Ease.OutQuad).SetDelay(delay).SetTarget(graphic);
            return unscaled ? tween.SetUpdate(true).SetLink(graphic.gameObject) : tween.SetLink(graphic.gameObject);
        }

        /// <summary>
        /// 缩放弹出，从偏小弹回原尺寸
        /// </summary>
        /// <param name="target">目标节点</param>
        /// <param name="fromScale">起始缩放比例</param>
        /// <param name="duration">时长</param>
        /// <param name="delay">延迟</param>
        /// <returns>补间对象，可为空</returns>
        public static Tween ScalePop(Transform target, float fromScale = 0.9f, float duration = Normal, float delay = 0f)
        {
            if (target == null) return null;
            target.DOKill();
            target.localScale = Vector3.one * fromScale;
            return target.DOScale(Vector3.one, duration).SetEase(Ease.OutBack).SetDelay(delay)
                .SetUpdate(true).SetLink(target.gameObject);
        }

        /// <summary>
        /// 快速弹一下，用于点击或命中反馈
        /// </summary>
        /// <param name="target">目标节点</param>
        /// <param name="strength">弹动强度</param>
        /// <param name="duration">时长</param>
        /// <returns>补间对象，可为空</returns>
        public static Tween Punch(Transform target, float strength = 0.08f, float duration = 0.22f)
        {
            if (target == null) return null;
            target.DOKill();
            return target.DOPunchScale(Vector3.one * strength, duration, 8, 0.6f)
                .SetUpdate(true).SetLink(target.gameObject);
        }

        /// <summary>
        /// 从偏移位置滑入并淡入
        /// </summary>
        /// <param name="rect">目标矩形</param>
        /// <param name="offset">起始位置偏移</param>
        /// <param name="duration">时长</param>
        /// <param name="useGraphicFade">是否同时淡入自身的图形/文本</param>
        /// <returns>补间对象，可为空</returns>
        public static Tween SlideIn(RectTransform rect, Vector2 offset, float duration = Normal, bool useGraphicFade = true)
        {
            if (rect == null) return null;
            var target = rect.anchoredPosition;
            rect.DOKill();
            rect.anchoredPosition = target + offset;
            var tween = rect.DOAnchorPos(target, duration).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(rect.gameObject);
            if (!useGraphicFade) return tween;
            var graphic = rect.GetComponent<Graphic>();
            if (graphic != null) Fade(graphic, 1f, duration);
            return tween;
        }

        /// <summary>
        /// 从偏移位置滑出并淡出，结束后执行回调
        /// </summary>
        /// <param name="rect">目标矩形</param>
        /// <param name="offset">结束位置偏移</param>
        /// <param name="duration">时长</param>
        /// <param name="onComplete">结束回调</param>
        /// <returns>补间对象，可为空</returns>
        public static Tween SlideOut(RectTransform rect, Vector2 offset, float duration = Normal,
            System.Action onComplete = null)
        {
            if (rect == null) return null;
            var target = rect.anchoredPosition + offset;
            rect.DOKill();
            var tween = rect.DOAnchorPos(target, duration).SetEase(Ease.InCubic).SetUpdate(true).SetLink(rect.gameObject);
            var graphic = rect.GetComponent<Graphic>();
            if (graphic != null) Fade(graphic, 0f, duration);
            if (onComplete != null) tween.OnComplete(() => onComplete());
            return tween;
        }

        /// <summary>
        /// 数字滚动：把整数从当前值补间到目标值
        /// </summary>
        /// <param name="from">起始值</param>
        /// <param name="to">目标值</param>
        /// <param name="duration">时长</param>
        /// <param name="onUpdate">每帧回调，参数为当前值</param>
        /// <param name="onComplete">结束回调</param>
        /// <returns>补间对象，可为空</returns>
        public static Tween CountTo(float from, float to, float duration,
            System.Action<float> onUpdate, System.Action onComplete = null)
        {
            if (onUpdate == null) return null;
            float value = from;
            return DOTween.To(() => value, current =>
            {
                value = current;
                onUpdate(current);
            }, to, duration).SetEase(Ease.OutQuad).SetUpdate(true).OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>
        /// 安全停止目标上由本套封装创建的补间
        /// </summary>
        /// <param name="target">目标对象</param>
        public static void KillSafe(Component target)
        {
            if (target == null) return;
            target.DOKill();
            DOTween.Kill(target);
        }

        /// <summary>
        /// 淡入淡出一个 CanvasGroup 的整体透明度
        /// </summary>
        /// <param name="group">目标 CanvasGroup</param>
        /// <param name="toAlpha">目标透明度</param>
        /// <param name="duration">时长</param>
        /// <param name="delay">延迟</param>
        /// <param name="unscaled">是否不受 Time.timeScale 影响</param>
        /// <returns>补间对象，可为空</returns>
        public static Tween CanvasGroupFade(CanvasGroup group, float toAlpha, float duration = Normal,
            float delay = 0f, bool unscaled = true)
        {
            if (group == null) return null;
            group.DOKill();
            var tween = DOTween.To(() => group.alpha, value => group.alpha = value, toAlpha, duration)
                .SetEase(Ease.OutQuad).SetDelay(delay).SetTarget(group).SetLink(group.gameObject);
            return unscaled ? tween.SetUpdate(true) : tween;
        }

        /// <summary>
        /// 抖动一下目标的位置，用于受击、错误提示这类反馈
        /// </summary>
        /// <param name="target">目标节点</param>
        /// <param name="strength">抖动强度</param>
        /// <param name="duration">时长</param>
        /// <param name="vibrato">抖动次数</param>
        /// <returns>补间对象，可为空</returns>
        public static Tween Shake(Transform target, float strength = 0.12f, float duration = 0.35f, int vibrato = 12)
        {
            if (target == null) return null;
            target.DOKill();
            return target.DOShakePosition(duration, strength, vibrato, 60f, false, true)
                .SetUpdate(true).SetLink(target.gameObject);
        }

        /// <summary>
        /// 平滑缩放到目标值
        /// </summary>
        /// <param name="target">目标节点</param>
        /// <param name="scale">目标缩放</param>
        /// <param name="duration">时长</param>
        /// <param name="ease">缓动方式</param>
        /// <returns>补间对象，可为空</returns>
        public static Tween ScaleTo(Transform target, Vector3 scale, float duration = Normal, Ease ease = Ease.OutCubic)
        {
            if (target == null) return null;
            target.DOKill();
            return target.DOScale(scale, duration).SetEase(ease).SetUpdate(true).SetLink(target.gameObject);
        }

        /// <summary>
        /// 平滑移动到目标锚点位置
        /// </summary>
        /// <param name="rect">目标矩形</param>
        /// <param name="position">目标锚点位置</param>
        /// <param name="duration">时长</param>
        /// <param name="ease">缓动方式</param>
        /// <returns>补间对象，可为空</returns>
        public static Tween Move(RectTransform rect, Vector2 position, float duration = Normal, Ease ease = Ease.OutCubic)
        {
            if (rect == null) return null;
            rect.DOKill();
            return rect.DOAnchorPos(position, duration).SetEase(ease).SetUpdate(true).SetLink(rect.gameObject);
        }

        /// <summary>
        /// 闪烁若干次，用于警告或高亮
        /// </summary>
        /// <param name="graphic">目标图形</param>
        /// <param name="times">闪烁次数</param>
        /// <param name="interval">每次间隔</param>
        /// <param name="minAlpha">最暗透明度</param>
        /// <returns>补间对象，可为空</returns>
        public static Tween Blink(Graphic graphic, int times = 3, float interval = 0.12f, float minAlpha = 0.25f)
        {
            if (graphic == null) return null;
            graphic.DOKill();
            var sequence = DOTween.Sequence().SetUpdate(true).SetLink(graphic.gameObject);
            for (int index = 0; index < Mathf.Max(1, times); index++)
            {
                sequence.Append(DOTween.To(() => graphic.color.a, value =>
                {
                    if (graphic == null) return;
                    var color = graphic.color;
                    color.a = value;
                    graphic.color = color;
                }, minAlpha, interval).SetEase(Ease.Linear));
                sequence.Append(DOTween.To(() => graphic.color.a, value =>
                {
                    if (graphic == null) return;
                    var color = graphic.color;
                    color.a = value;
                    graphic.color = color;
                }, 1f, interval).SetEase(Ease.Linear));
            }
            return sequence;
        }

        /// <summary>
        /// 延迟一段时间后执行回调，时间走 unscaledTime
        /// </summary>
        /// <param name="seconds">延迟秒数</param>
        /// <param name="action">到点执行的方法</param>
        /// <returns>补间对象，可为空</returns>
        public static Tween DelayedCall(float seconds, System.Action action)
        {
            if (action == null) return null;
            return DOVirtual.DelayedCall(Mathf.Max(0f, seconds), () => action(), true);
        }
    }
}
