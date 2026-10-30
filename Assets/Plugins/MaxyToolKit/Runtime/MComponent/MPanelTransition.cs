using DG.Tweening;
using UnityEngine;
using MaxyToolKit.Animation;

namespace MaxyToolKit.MComponent
{
    /// <summary>
    /// 面板开合补间组件
    /// </summary>
    /// <remarks>
    /// 首次播放前记录面板的初始缩放、位置与旋转，每段动画结束后都会回到这些值
    /// 与项目里的全屏面板过渡不同，本组件的入场可以在任意时刻手动触发
    /// 所有补间都走不受Time.timeScale影响的时间线，并在物体销毁时自动终止
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class MPanelTransition : MonoBehaviour
    {
        /// <summary>
        /// 面板入场样式
        /// </summary>
        public enum EntranceStyle
        {
            /// <summary>
            /// 只做淡入
            /// </summary>
            Fade,
            /// <summary>
            /// 从略小尺寸弹回
            /// </summary>
            Scale,
            /// <summary>
            /// 从下方滑入
            /// </summary>
            SlideUp,
            /// <summary>
            /// 从上方滑入
            /// </summary>
            SlideDown,
            /// <summary>
            /// 带轻微旋转落位
            /// </summary>
            Rotate
        }

        /// <summary>
        /// 激活时是否自动播放入场动画
        /// </summary>
        [SerializeField] private bool _playOnEnable = true;
        /// <summary>
        /// 入场与收起是否做整体淡入淡出
        /// </summary>
        [SerializeField] private bool _fadeIn = true;
        /// <summary>
        /// 缩放样式是否播放缩放段，关闭后该样式退化为纯淡入
        /// </summary>
        [SerializeField] private bool _animateScale = true;
        /// <summary>
        /// 是否在全部样式中随机挑一种入场
        /// </summary>
        [SerializeField] private bool _randomStyle = false;
        /// <summary>
        /// 固定使用的入场样式，randomStyle开启时忽略
        /// </summary>
        [SerializeField] private EntranceStyle _style = EntranceStyle.Fade;
        /// <summary>
        /// 动作段时长，淡入始终比它更短
        /// </summary>
        [SerializeField] private float _duration = MTween.Normal;
        /// <summary>
        /// 滑入样式的起始位移
        /// </summary>
        [SerializeField] private float _slideDistance = 34f;
        /// <summary>
        /// 收起结束后是否自动隐藏物体
        /// </summary>
        [SerializeField] private bool _deactivateOnExit = false;

        private static readonly EntranceStyle[] _Styles =
            (EntranceStyle[])System.Enum.GetValues(typeof(EntranceStyle));
        private static int _previousStyle = -1;

        private CanvasGroup _group;
        private Vector3 _restingScale;
        private Vector2 _restingPosition;
        private Quaternion _restingRotation;
        private Sequence _activeTween;
        private bool _stateCaptured;

        /// <summary>
        /// 缓存CanvasGroup并记录面板初始状态
        /// </summary>
        private void Awake()
        {
            CacheReferences();
        }

        /// <summary>
        /// 激活时按配置自动播放入场
        /// </summary>
        private void OnEnable()
        {
            CacheReferences();
            if (_playOnEnable && Application.isPlaying) PlayEntrance();
        }

        /// <summary>
        /// 播放入场动画
        /// </summary>
        /// <remarks>
        /// 重复调用会先终止上一段补间，并从记录的初始状态重新开始
        /// 非播放模式下直接返回，不创建补间
        /// </remarks>
        public void PlayEntrance()
        {
            if (!Application.isPlaying) return;
            CacheReferences();
            KillTween();
            //每段动画都从初始状态起跑，避免上一段被打断后残留位移或旋转
            RestoreTransform();
            if (_group != null)
            {
                _group.alpha = _fadeIn ? 0f : 1f;
                _group.blocksRaycasts = true;
            }

            var selectedStyle = PickStyle();
            var motionDuration = Mathf.Max(0.01f, _duration);
            //淡入比动作更快，动作还在落位时面板已经基本显形
            var fadeDuration = Mathf.Min(motionDuration, MTween.Fast);
            var hasSegment = false;
            _activeTween = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            if (_fadeIn && _group != null)
            {
                _activeTween.Append(DOTweenModuleUGUI.DOFade(_group, 1f, fadeDuration).SetEase(Ease.OutQuad));
                hasSegment = true;
            }

            //按样式叠加缩放、位移或旋转段
            switch (selectedStyle)
            {
                case EntranceStyle.Scale:
                    if (!_animateScale) break;
                    transform.localScale = _restingScale * 0.92f;
                    _activeTween.Join(transform.DOScale(_restingScale, motionDuration).SetEase(Ease.OutBack));
                    hasSegment = true;
                    break;
                case EntranceStyle.SlideUp:
                    if (!(transform is RectTransform upRect)) break;
                    upRect.anchoredPosition = _restingPosition + Vector2.down * _slideDistance;
                    _activeTween.Join(upRect.DOAnchorPos(_restingPosition, motionDuration).SetEase(Ease.OutCubic));
                    hasSegment = true;
                    break;
                case EntranceStyle.SlideDown:
                    if (!(transform is RectTransform downRect)) break;
                    downRect.anchoredPosition = _restingPosition + Vector2.up * _slideDistance;
                    _activeTween.Join(downRect.DOAnchorPos(_restingPosition, motionDuration).SetEase(Ease.OutCubic));
                    hasSegment = true;
                    break;
                case EntranceStyle.Rotate:
                    //旋转只从正负几度的小角度落回，幅度太大会显得廉价
                    transform.localRotation = _restingRotation * Quaternion.Euler(0f, 0f, Random.Range(-4f, 4f));
                    _activeTween.Join(transform.DOLocalRotateQuaternion(_restingRotation, motionDuration).SetEase(Ease.OutBack));
                    hasSegment = true;
                    break;
            }

            //没有任何段时补一个极短间隔，保证序列有效且能被下一次调用终止
            if (!hasSegment) _activeTween.AppendInterval(0.01f);
        }

        /// <summary>
        /// 播放收起动画
        /// </summary>
        /// <param name="onComplete">
        /// 收起结束后的回调，允许为空
        /// </param>
        /// <remarks>
        /// 收起期间立即关闭射线阻挡，避免透明面板继续拦截点击
        /// deactivateOnExit开启时，结束后自动隐藏物体
        /// </remarks>
        public void PlayExit(System.Action onComplete = null)
        {
            if (!Application.isPlaying)
            {
                //非播放模式下不支持补间，直接切到隐藏态方便手动验收
                SetVisibleImmediate(false);
                onComplete?.Invoke();
                return;
            }

            CacheReferences();
            KillTween();
            //收起从初始状态开始，否则连续调用会带着上一次的偏移继续缩小
            RestoreTransform();
            if (_group != null) _group.blocksRaycasts = false;

            var exitDuration = MTween.Fast;
            _activeTween = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            if (_fadeIn && _group != null)
                _activeTween.Append(DOTweenModuleUGUI.DOFade(_group, 0f, exitDuration).SetEase(Ease.InQuad));
            else
                _activeTween.AppendInterval(0.01f);
            if (_animateScale)
                _activeTween.Join(transform.DOScale(_restingScale * 0.96f, exitDuration).SetEase(Ease.OutCubic));

            _activeTween.OnComplete(() =>
            {
                //先释放持有再隐藏物体，避免OnDisable里重复终止正在收尾的序列
                _activeTween = null;
                RestoreTransform();
                if (_deactivateOnExit && gameObject != null) gameObject.SetActive(false);
                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// 无动画切换面板显示状态
        /// </summary>
        /// <param name="visible">
        /// 是否显示
        /// </param>
        public void SetVisibleImmediate(bool visible)
        {
            CacheReferences();
            KillTween();
            RestoreTransform();
            if (_group == null) return;
            _group.alpha = visible ? 1f : 0f;
            _group.blocksRaycasts = visible;
        }

        /// <summary>
        /// 解析CanvasGroup并记录面板初始的缩放、位置与旋转
        /// </summary>
        private void CacheReferences()
        {
            //面板没有CanvasGroup时自动补一个，保证淡入淡出可用
            if (_group == null) _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            if (_stateCaptured) return;
            _stateCaptured = true;
            _restingScale = transform.localScale;
            _restingRotation = transform.localRotation;
            if (transform is RectTransform rect) _restingPosition = rect.anchoredPosition;
        }

        /// <summary>
        /// 选择本次入场使用的样式
        /// </summary>
        /// <returns>
        /// 选中的入场样式
        /// </returns>
        private EntranceStyle PickStyle()
        {
            if (!_randomStyle) return _style;
            var count = _Styles.Length;
            if (count <= 1) return EntranceStyle.Fade;
            //随机时避开上一次用过的样式，避免连续两次观感相同
            var index = Random.Range(0, count);
            if (index == _previousStyle) index = (index + Random.Range(1, count)) % count;
            _previousStyle = index;
            return _Styles[index];
        }

        /// <summary>
        /// 把缩放、位置与旋转恢复到记录的初始值，不改变透明度
        /// </summary>
        private void RestoreTransform()
        {
            if (!_stateCaptured) return;
            transform.localScale = _restingScale;
            transform.localRotation = _restingRotation;
            if (transform is RectTransform rect) rect.anchoredPosition = _restingPosition;
        }

        /// <summary>
        /// 终止当前正在播放的面板补间
        /// </summary>
        private void KillTween()
        {
            if (_activeTween == null) return;
            _activeTween.Kill();
            _activeTween = null;
        }

        /// <summary>
        /// 停用时终止补间并复原状态，避免面板复用时残留动画值
        /// </summary>
        private void OnDisable()
        {
            KillTween();
            if (!_stateCaptured) return;
            RestoreTransform();
            if (_group == null) return;
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
        }
    }
}
