using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using MaxyToolKit.UI;

namespace MaxyToolKit.Effect
{
    /// <summary>
    /// UI层彩带与礼花的静态工具，只用普通色块加补间实现，不依赖粒子系统
    /// </summary>
    /// <remarks>
    /// 每次播放都会在父节点下新建一批色块，补间结束后逐个销毁
    /// 所有补间都启用独立更新并链接到色块自身，父节点或色块被销毁时补间会一起停下
    /// </remarks>
    public static class MConfetti
    {
        /// <summary>
        /// 单次播放允许生成的色块上限，防止误传大数字把编辑器卡住
        /// </summary>
        public const int MaxCount = 64;

        /// <summary>
        /// 色块宽
        /// </summary>
        private const float PieceWidth = 8f;

        /// <summary>
        /// 色块高
        /// </summary>
        private const float PieceHeight = 14f;

        /// <summary>
        /// 散开距离下限
        /// </summary>
        private const float MinDistance = 80f;

        /// <summary>
        /// 散开距离上限
        /// </summary>
        private const float MaxDistance = 220f;

        /// <summary>
        /// 生成点附近的随机散开半径
        /// </summary>
        private const float SpawnJitter = 14f;

        /// <summary>
        /// 色块起始缩放比例，配合放大补间形成从小变大的观感
        /// </summary>
        private const float StartScale = 0.2f;

        /// <summary>
        /// 色块收尾时的缩放比例，配合淡出形成消散观感
        /// </summary>
        private const float EndScale = 0.15f;

        /// <summary>
        /// 默认色板，五个明快且彼此区分度高的颜色
        /// </summary>
        private static readonly Color[] _DefaultColors =
        {
            new Color(1f, 0.82f, 0.25f, 1f),
            new Color(1f, 0.42f, 0.42f, 1f),
            new Color(0.42f, 0.78f, 1f, 1f),
            new Color(0.55f, 0.9f, 0.5f, 1f),
            new Color(0.85f, 0.55f, 1f, 1f)
        };

        /// <summary>
        /// 色块统一使用的中心锚点
        /// </summary>
        private static readonly Vector2 _CenterAnchor = new Vector2(0.5f, 0.5f);

        /// <summary>
        /// 在父级中心播放一轮彩带礼花
        /// </summary>
        /// <param name="parent">
        /// 父节点，一般传画布或某个UI容器
        /// </param>
        /// <param name="count">
        /// 生成的色块数量，超过上限会被截断
        /// </param>
        /// <param name="duration">
        /// 单个色块的补间时长
        /// </param>
        /// <param name="colors">
        /// 色板，为空则用默认的五色色板
        /// </param>
        public static void Play(RectTransform parent, int count = 24, float duration = 0.9f, Color[] colors = null)
        {
            //父级中心在局部坐标里就是零向量，这里直接转交给指定起点的重载
            PlayAt(parent, Vector2.zero, count, duration, colors);
        }

        /// <summary>
        /// 在父级的指定锚点位置播放一轮彩带礼花
        /// </summary>
        /// <param name="parent">
        /// 父节点，一般传画布或某个UI容器
        /// </param>
        /// <param name="anchoredPosition">
        /// 播放起点，相对父级的局部坐标
        /// </param>
        /// <param name="count">
        /// 生成的色块数量，超过上限会被截断
        /// </param>
        /// <param name="duration">
        /// 单个色块的补间时长
        /// </param>
        /// <param name="colors">
        /// 色板，为空则用默认的五色色板
        /// </param>
        public static void PlayAt(RectTransform parent, Vector2 anchoredPosition, int count = 24,
            float duration = 0.9f, Color[] colors = null)
        {
            if (parent == null) return;
            //数量与时长都属于外部传入参数，先夹到安全区间再开始建节点
            count = Mathf.Clamp(count, 0, MaxCount);
            if (count <= 0) return;
            if (duration <= 0f) duration = 0.01f;
            Color[] palette = colors;
            if (palette == null || palette.Length == 0) palette = _DefaultColors;
            for (int i = 0; i < count; i++)
            {
                SpawnPiece(parent, anchoredPosition, palette, duration);
            }
        }

        /// <summary>
        /// 生成单个彩带色块并启动外扩、缩放、旋转与淡出的补间
        /// </summary>
        /// <param name="parent">
        /// 父节点
        /// </param>
        /// <param name="anchoredPosition">
        /// 播放起点
        /// </param>
        /// <param name="palette">
        /// 色板，调用方保证非空
        /// </param>
        /// <param name="duration">
        /// 单个色块的补间时长，调用方保证为正数
        /// </param>
        private static void SpawnPiece(RectTransform parent, Vector2 anchoredPosition, Color[] palette, float duration)
        {
            Color color = palette[Random.Range(0, palette.Length)];
            Image piece = MUIFactory.CreateImage("彩带领片", parent, null, color, false);
            if (piece == null) return;
            var rect = piece.rectTransform;
            //色块统一用中心锚定，尺寸固定为细长的纸条形状
            rect.anchorMin = _CenterAnchor;
            rect.anchorMax = _CenterAnchor;
            rect.pivot = _CenterAnchor;
            rect.sizeDelta = new Vector2(PieceWidth, PieceHeight);
            //起点在播放位置附近抖一点，避免所有色块从同一个点冒出来而显得死板
            rect.anchoredPosition = anchoredPosition + Random.insideUnitCircle * SpawnJitter;
            rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-180f, 180f));
            rect.localScale = Vector3.one * StartScale;
            rect.SetAsLastSibling();
            //随机一个方向把色块甩出去，旋转量在正负540度之间随机取，保证有翻转感
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector2 destination = rect.anchoredPosition
                + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(MinDistance, MaxDistance);
            float rotation = Random.Range(340f, 540f) * (Random.value < 0.5f ? -1f : 1f);
            var sequence = DOTween.Sequence().SetUpdate(true).SetLink(piece.gameObject);
            sequence.Join(rect.DOAnchorPos(destination, duration).SetEase(Ease.OutCubic));
            sequence.Join(rect.DOLocalRotate(new Vector3(0f, 0f, rotation), duration, RotateMode.FastBeyond360)
                .SetEase(Ease.OutQuad));
            //先放大到原尺寸再缩回去，两段缩放首尾相接避免同一属性补间互相抢
            float growDuration = duration * 0.3f;
            sequence.Join(rect.DOScale(Vector3.one, growDuration).SetEase(Ease.OutBack));
            sequence.Insert(growDuration, rect.DOScale(Vector3.one * EndScale, duration - growDuration)
                .SetEase(Ease.InQuad));
            //后段才开始淡出，前段保持清晰，观感更像纸片落下
            Color fadeColor = piece.color;
            float fadeDelay = duration * 0.45f;
            sequence.Insert(fadeDelay, DOTween.To(() => fadeColor.a, value =>
            {
                fadeColor.a = value;
                if (piece != null) piece.color = fadeColor;
            }, 0f, duration - fadeDelay).SetEase(Ease.InQuad));
            sequence.OnComplete(() =>
            {
                //补间结束时色块已经透明到看不见，直接销毁避免对象越积越多
                if (piece != null) Object.Destroy(piece.gameObject);
            });
        }
    }
}
