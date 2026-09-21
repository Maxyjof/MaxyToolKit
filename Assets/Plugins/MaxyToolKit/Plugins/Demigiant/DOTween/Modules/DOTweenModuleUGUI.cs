using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;

namespace DG.Tweening
{
    public static class DOTweenModuleUGUI
    {
        // Graphic color
        public static TweenerCore<Color, Color, ColorOptions> DOColor(this Graphic target, Color endValue, float duration)
        {
            var t = DOTween.To(() => target.color, x => target.color = x, endValue, duration);
            t.SetTarget(target);
            return t;
        }

        // Graphic fade (alpha)
        public static Tweener DOFade(this Graphic target, float endValue, float duration)
        {
            var t = DOTween.To(() => target.color.a, a => { var c = target.color; c.a = a; target.color = c; }, endValue, duration);
            t.SetTarget(target);
            return t;
        }

        // CanvasGroup fade
        public static Tweener DOFade(this CanvasGroup target, float endValue, float duration)
        {
            var t = DOTween.To(() => target.alpha, x => target.alpha = x, endValue, duration);
            t.SetTarget(target);
            return t;
        }

        // Image fill amount
        public static Tweener DOFillAmount(this Image target, float endValue, float duration)
        {
            var t = DOTween.To(() => target.fillAmount, x => target.fillAmount = x, endValue, duration);
            t.SetTarget(target);
            return t;
        }

        // RectTransform sizeDelta
        public static TweenerCore<Vector2, Vector2, VectorOptions> DOSizeDelta(this RectTransform target, Vector2 endValue, float duration)
        {
            var t = DOTween.To(() => target.sizeDelta, x => target.sizeDelta = x, endValue, duration);
            t.SetTarget(target);
            return t;
        }

        // RectTransform anchoredPosition3D
        public static TweenerCore<Vector3, Vector3, VectorOptions> DOAnchorPos3D(this RectTransform target, Vector3 endValue, float duration, bool snapping = false)
        {
            var t = DOTween.To(() => target.anchoredPosition3D, x => target.anchoredPosition3D = x, endValue, duration);
            t.SetTarget(target);
            return t;
        }

        // Punch anchored position (simple forward)
        public static Tweener DOPunchAnchorPos(this RectTransform target, Vector3 punch, float duration, int vibrato = 10, float elasticity = 1)
        {
            // Implement a lightweight punch using DOTween.Punch
            return DOTween.Punch(() => target.anchoredPosition, x => target.anchoredPosition = x, (Vector2)punch, duration, vibrato, elasticity).SetTarget(target);
        }

        // Shake anchored position (placeholder using Shake)
        public static Tweener DOShakeAnchorPos(this RectTransform target, float duration, Vector2 strength, int vibrato = 10, float randomness = 90f, bool snapping = false, bool fadeOut = true)
        {
            return DOTween.Shake(() => target.anchoredPosition, x => target.anchoredPosition = x, duration, strength, vibrato, randomness, fadeOut).SetTarget(target);
        }

        // Legacy UnityEngine.UI.Text color/fade/text
        public static TweenerCore<Color, Color, ColorOptions> DOColor(this Text target, Color endValue, float duration)
        {
            var t = DOTween.To(() => target.color, x => target.color = x, endValue, duration);
            t.SetTarget(target);
            return t;
        }

        public static Tweener DOFade(this Text target, float endValue, float duration)
        {
            var t = DOTween.ToAlpha(() => target.color, x => target.color = x, endValue, duration);
            t.SetTarget(target);
            return t;
        }

        public static TweenerCore<string, string, StringOptions> DOText(this Text target, string endValue, float duration, bool richTextEnabled = true, ScrambleMode scrambleMode = ScrambleMode.None, string scrambleChars = null)
        {
            string v = target.text;
            var t = DOTween.To(() => v, x => { v = x; target.text = v; }, endValue, duration);
            t.SetOptions(richTextEnabled, scrambleMode, scrambleChars).SetTarget(target);
            return t;
        }

        // Utility helpers used by DOTweenAnimation
        public static class Utils
        {
            public static Vector3 SwitchToRectTransform(Transform t, RectTransform rTarget)
            {
                if (t == null) return Vector3.zero;
                if (rTarget == null) return t.position;
                // Convert world position to rTarget local space
                return rTarget.InverseTransformPoint(t.position);
            }
        }
    }
}
