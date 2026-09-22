using System;
using System.Collections;
using UnityEngine;

namespace MaxyToolKit.Component
{
    /// <summary>
    /// 使用CanvasGroup执行界面淡入淡出效果
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class FadeEffectOverlay : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private AnimationCurve curve = null;
        [SerializeField] private bool useUnscaledTime;
        [SerializeField] private bool blockRaycastsWhenVisible = true;
        private Coroutine running;

        /// <summary>
        /// 获取用于控制透明度和交互状态的CanvasGroup
        /// </summary>
        public CanvasGroup CanvasGroup => canvasGroup;

        /// <summary>
        /// 在组件重置时自动获取CanvasGroup
        /// </summary>
        private void Reset() => canvasGroup = GetComponent<CanvasGroup>();

        /// <summary>
        /// 初始化CanvasGroup和默认曲线
        /// </summary>
        private void Awake()
        {
            //补齐可能未在检查器中设置的依赖
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (curve == null || curve.length == 0) curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }

        /// <summary>
        /// 将遮罩淡入到完全可见
        /// </summary>
        /// <param name="duration">
        /// 动画持续秒数
        /// </param>
        /// <param name="completed">
        /// 动画完成后的回调
        /// </param>
        public void FadeIn(float duration = 0.25f, Action completed = null) => FadeTo(1f, duration, completed);

        /// <summary>
        /// 将遮罩淡出到完全透明
        /// </summary>
        /// <param name="duration">
        /// 动画持续秒数
        /// </param>
        /// <param name="completed">
        /// 动画完成后的回调
        /// </param>
        public void FadeOut(float duration = 0.25f, Action completed = null) => FadeTo(0f, duration, completed);

        /// <summary>
        /// 将遮罩透明度平滑过渡到目标值
        /// </summary>
        /// <param name="target">
        /// 目标透明度，会限制在0到1之间
        /// </param>
        /// <param name="duration">
        /// 动画持续秒数
        /// </param>
        /// <param name="completed">
        /// 动画完成后的回调
        /// </param>
        /// <remarks>
        /// 调用新动画会停止当前正在执行的淡化动画
        /// </remarks>
        public void FadeTo(float target, float duration, Action completed = null)
        {
            //停止旧动画并启动新的协程
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(FadeRoutine(Mathf.Clamp01(target), Mathf.Max(0f, duration), completed));
        }

        /// <summary>
        /// 立即设置遮罩透明度并停止当前动画
        /// </summary>
        /// <param name="alpha">
        /// 要设置的透明度
        /// </param>
        public void SetImmediate(float alpha)
        {
            //立即终止旧动画并同步交互状态
            if (running != null) StopCoroutine(running);
            running = null;
            SetAlpha(alpha);
        }

        /// <summary>
        /// 执行淡化动画的协程
        /// </summary>
        /// <param name="target">
        /// 目标透明度
        /// </param>
        /// <param name="duration">
        /// 动画持续秒数
        /// </param>
        /// <param name="completed">
        /// 动画完成后的回调
        /// </param>
        /// <returns>
        /// 协程枚举器
        /// </returns>
        private IEnumerator FadeRoutine(float target, float duration, Action completed)
        {
            var start = canvasGroup.alpha;
            if (duration <= 0f)
            {
                //零时长直接设置结果
                SetAlpha(target);
                completed?.Invoke();
                running = null;
                yield break;
            }

            //根据曲线逐帧计算透明度
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                var t = curve.Evaluate(Mathf.Clamp01(elapsed / duration));
                SetAlpha(Mathf.LerpUnclamped(start, target, t));
                yield return null;
            }

            //确保最终值不受浮点误差影响
            SetAlpha(target);
            completed?.Invoke();
            running = null;
        }

        /// <summary>
        /// 设置透明度并同步射线阻挡和交互状态
        /// </summary>
        /// <param name="alpha">
        /// 要设置的透明度
        /// </param>
        private void SetAlpha(float alpha)
        {
            canvasGroup.alpha = alpha;
            canvasGroup.blocksRaycasts = blockRaycastsWhenVisible && alpha > 0.001f;
            canvasGroup.interactable = canvasGroup.blocksRaycasts;
        }
    }
}
