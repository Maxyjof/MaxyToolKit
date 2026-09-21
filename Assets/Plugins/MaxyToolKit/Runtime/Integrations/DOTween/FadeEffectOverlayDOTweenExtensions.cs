using System;
using DG.Tweening;
using UnityEngine;

namespace MaxyToolKit
{
    /// <summary>
    /// 为FadeEffectOverlay提供DOTween动画扩展
    /// </summary>
    public static class FadeEffectOverlayDOTweenExtensions
    {
        /// <summary>
        /// 使用DOTween将遮罩透明度过渡到目标值
        /// </summary>
        /// <param name="overlay">
        /// 要控制的遮罩组件
        /// </param>
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
        /// 创建的DOTween补间对象
        /// </returns>
        public static Tween FadeToDOTween(this FadeEffectOverlay overlay, float target, float duration, Action completed = null)
        {
            if (overlay == null) throw new ArgumentNullException(nameof(overlay));
            //创建透明度补间并同步交互状态
            var tween = DOTween.To(
                () => overlay.CanvasGroup.alpha,
                value =>
                {
                    overlay.CanvasGroup.alpha = value;
                    overlay.CanvasGroup.blocksRaycasts = value > 0.001f;
                    overlay.CanvasGroup.interactable = overlay.CanvasGroup.blocksRaycasts;
                },
                Mathf.Clamp01(target),
                Mathf.Max(0f, duration));
            //按需注册完成回调
            if (completed != null) tween.OnComplete(() => completed.Invoke());
            return tween;
        }

        /// <summary>
        /// 使用DOTween淡入遮罩
        /// </summary>
        /// <param name="overlay">
        /// 要控制的遮罩组件
        /// </param>
        /// <param name="duration">
        /// 动画持续秒数
        /// </param>
        /// <param name="completed">
        /// 动画完成后的回调
        /// </param>
        /// <returns>
        /// 创建的DOTween补间对象
        /// </returns>
        public static Tween FadeInDOTween(this FadeEffectOverlay overlay, float duration = 0.25f, Action completed = null) =>
            overlay.FadeToDOTween(1f, duration, completed);

        /// <summary>
        /// 使用DOTween淡出遮罩
        /// </summary>
        /// <param name="overlay">
        /// 要控制的遮罩组件
        /// </param>
        /// <param name="duration">
        /// 动画持续秒数
        /// </param>
        /// <param name="completed">
        /// 动画完成后的回调
        /// </param>
        /// <returns>
        /// 创建的DOTween补间对象
        /// </returns>
        public static Tween FadeOutDOTween(this FadeEffectOverlay overlay, float duration = 0.25f, Action completed = null) =>
            overlay.FadeToDOTween(0f, duration, completed);
    }
}
