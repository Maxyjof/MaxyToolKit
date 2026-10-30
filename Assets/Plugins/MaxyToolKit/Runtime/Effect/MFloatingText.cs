using DG.Tweening;
using TMPro;
using UnityEngine;
using MaxyToolKit.UI;

namespace MaxyToolKit.Effect
{
    /// <summary>
    /// 分数飘字与伤害数字这类一次性浮字效果的静态工厂
    /// </summary>
    /// <remarks>
    /// 每调用一次都会新建一个文本节点，整段动画播完后由补间回调销毁节点
    /// 所有补间都启用独立更新并链接到节点自身，节点被提前销毁时补间会跟着停下
    /// </remarks>
    public static class MFloatingText
    {
        /// <summary>
        /// 生成节点的名字
        /// </summary>
        private const string SpawnedName = "飘字";

        /// <summary>
        /// 中心锚点，飘字统一用中心锚定以便直接按锚点定位
        /// </summary>
        private static readonly Vector2 _CenterAnchor = new Vector2(0.5f, 0.5f);

        /// <summary>
        /// 起始缩放比例，先从小放大到略大再回落到原尺寸
        /// </summary>
        private const float StartScale = 0.6f;

        /// <summary>
        /// 放大阶段的目标比例
        /// </summary>
        private const float PeakScale = 1.15f;

        /// <summary>
        /// 文本固定宽度，避免文字按父级宽度换行
        /// </summary>
        private const float TextWidth = 420f;

        /// <summary>
        /// 在父级的指定锚点位置生成一段向上飘并淡出的文字
        /// </summary>
        /// <param name="parent">
        /// 父节点，一般传画布或某个UI容器
        /// </param>
        /// <param name="anchoredPosition">
        /// 相对父级的起始位置
        /// </param>
        /// <param name="content">
        /// 显示内容
        /// </param>
        /// <param name="color">
        /// 文字颜色
        /// </param>
        /// <param name="fontSize">
        /// 字号
        /// </param>
        /// <param name="riseDistance">
        /// 向上的飘动距离
        /// </param>
        /// <param name="duration">
        /// 整段动画时长
        /// </param>
        /// <param name="font">
        /// 字体，为空则用TMP默认字体
        /// </param>
        /// <returns>
        /// 创建好的文本组件，父级为空时返回空
        /// </returns>
        public static TextMeshProUGUI Spawn(Transform parent, Vector2 anchoredPosition, string content,
            Color color, float fontSize = 28f, float riseDistance = 60f, float duration = 0.7f,
            TMP_FontAsset font = null)
        {
            if (parent == null) return null;
            //字号或时长为非正数会让TMP排版异常或补间瞬间结束，这里兜底到可用值
            if (fontSize <= 0f) fontSize = 1f;
            if (duration <= 0f) duration = 0.01f;
            //复用工厂创建文本，工厂内部已经处理了先建未激活再配置字体的顺序
            var text = MUIFactory.CreateText(SpawnedName, parent, content, font, fontSize, color);
            if (text == null) return null;
            text.raycastTarget = false;
            var rect = text.rectTransform;
            //改成中心锚定并给固定尺寸，让文字以起始点为中心显示而不是铺满父级
            rect.anchorMin = _CenterAnchor;
            rect.anchorMax = _CenterAnchor;
            rect.pivot = _CenterAnchor;
            rect.sizeDelta = new Vector2(TextWidth, fontSize * 2.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.localScale = Vector3.one * StartScale;
            rect.SetAsLastSibling();
            //整段动画：向上位移贯穿全程，开头轻微放大，后段才开始淡出
            var sequence = DOTween.Sequence().SetUpdate(true).SetLink(text.gameObject);
            float popDuration = duration * 0.25f;
            sequence.Join(rect.DOScale(Vector3.one * PeakScale, popDuration).SetEase(Ease.OutBack));
            sequence.Join(rect.DOAnchorPos(anchoredPosition + Vector2.up * riseDistance, duration)
                .SetEase(Ease.OutCubic));
            sequence.Insert(popDuration, rect.DOScale(Vector3.one, popDuration).SetEase(Ease.OutQuad));
            //透明度单独补间，起点沿用文本当前颜色，避免影响其它属性
            Color fadeColor = text.color;
            float fadeDelay = duration * 0.35f;
            float fadeDuration = duration - fadeDelay;
            sequence.Insert(fadeDelay, DOTween.To(() => fadeColor.a, value =>
            {
                fadeColor.a = value;
                if (text != null) text.color = fadeColor;
            }, 0f, fadeDuration).SetEase(Ease.InQuad));
            sequence.OnComplete(() =>
            {
                //动画结束时文本已经不可见，直接销毁节点，避免每次飘字都留下一个空物体
                if (text != null) Object.Destroy(text.gameObject);
            });
            return text;
        }

        /// <summary>
        /// 把世界坐标换算成父级局部坐标后，在对应位置生成飘字
        /// </summary>
        /// <param name="parent">
        /// 父节点，一般传画布或某个UI容器
        /// </param>
        /// <param name="worldPosition">
        /// 世界坐标，通常传目标物件的transform.position
        /// </param>
        /// <param name="content">
        /// 显示内容
        /// </param>
        /// <param name="color">
        /// 文字颜色
        /// </param>
        /// <param name="fontSize">
        /// 字号
        /// </param>
        /// <param name="riseDistance">
        /// 向上的飘动距离
        /// </param>
        /// <param name="duration">
        /// 整段动画时长
        /// </param>
        /// <param name="font">
        /// 字体，为空则用TMP默认字体
        /// </param>
        /// <returns>
        /// 创建好的文本组件，父级为空时返回空
        /// </returns>
        public static TextMeshProUGUI SpawnAt(Transform parent, Vector3 worldPosition, string content,
            Color color, float fontSize = 28f, float riseDistance = 60f, float duration = 0.7f,
            TMP_FontAsset font = null)
        {
            if (parent == null) return null;
            var parentRect = parent as RectTransform;
            Vector2 anchoredPosition;
            if (parentRect != null)
            {
                //父级矩形的中心就是子节点中心锚点对应的锚点参考位置，减掉它才是anchoredPosition
                Vector2 local = parentRect.InverseTransformPoint(worldPosition);
                anchoredPosition = local - parentRect.rect.center;
            }
            else
            {
                //父级不是矩形节点时退回单纯的局部坐标换算，至少能把位置落在父级附近
                anchoredPosition = parent.InverseTransformPoint(worldPosition);
            }
            return Spawn(parent, anchoredPosition, content, color, fontSize, riseDistance, duration, font);
        }
    }
}
