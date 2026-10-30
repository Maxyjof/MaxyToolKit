using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MaxyToolKit.Animation;

namespace MaxyToolKit.MComponent
{
    /// <summary>
    /// 给按钮补充悬停放大与按下回弹的交互反馈
    /// </summary>
    /// <remarks>
    /// 组件独占目标的localScale与提亮图形的顶点颜色，不替换按钮自身的点击回调
    /// 未指定目标时退化为只响应指针事件的纯缩放反馈
    /// 如果按钮还开着Selectable的ColorTint过渡，两套颜色会叠加，建议改成None由本组件接管
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class MButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        /// <summary>
        /// 交互目标，为空时自动取当前物体上的Selectable
        /// </summary>
        [SerializeField] private Selectable _target;
        /// <summary>
        /// 提亮的目标图形，为空时依次取目标的targetGraphic与当前物体上的Graphic
        /// </summary>
        [SerializeField] private Graphic _tintTarget;
        /// <summary>
        /// 悬停时的缩放倍率
        /// </summary>
        [SerializeField] private float _hoverScale = 1.04f;
        /// <summary>
        /// 按下时的缩放倍率
        /// </summary>
        [SerializeField] private float _pressScale = 0.96f;
        /// <summary>
        /// 缩放与颜色的过渡时长
        /// </summary>
        [SerializeField] private float _duration = 0.08f;
        /// <summary>
        /// 悬停时提亮到的颜色，透明度沿用常态颜色
        /// </summary>
        [SerializeField] private Color _hoverColor = new Color(1f, 0.96f, 0.86f);
        /// <summary>
        /// 目标不可交互时是否压暗
        /// </summary>
        [SerializeField] private bool _dimWhenDisabled = true;
        /// <summary>
        /// 是否使用不受Time.timeScale影响的时间
        /// </summary>
        [SerializeField] private bool _useUnscaledTime = true;

        private Vector3 _restingScale;
        private Color _baseColor = Color.white;
        private Tween _scaleTween;
        private Tween _colorTween;
        private bool _hovering;
        private bool _pressed;
        private bool _scaleCached;
        private bool _colorCached;
        private bool _lastInteractable = true;

        /// <summary>
        /// 缓存目标引用与初始外观
        /// </summary>
        private void Awake()
        {
            CacheReferences();
        }

        /// <summary>
        /// 激活时复位交互状态并套用当前外观
        /// </summary>
        private void OnEnable()
        {
            CacheReferences();
            _hovering = false;
            _pressed = false;
            //非播放模式下不建补间，避免编辑器里残留运行时状态
            if (Application.isPlaying) ApplyImmediate();
        }

        /// <summary>
        /// 更新按钮的常态颜色，供换肤或外部配色逻辑调用
        /// </summary>
        /// <param name="color">
        /// 新的常态颜色
        /// </param>
        public void SetBaseColor(Color color)
        {
            CacheReferences();
            _baseColor = color;
            _colorCached = true;
            //播放中按新基色重新补间，避免旧补间把颜色又拉回旧目标
            if (Application.isPlaying) AnimateState();
            else if (_tintTarget != null) _tintTarget.color = color;
        }

        /// <summary>
        /// 指针进入时放大并提亮
        /// </summary>
        /// <param name="eventData">
        /// 指针事件数据
        /// </param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            //不可交互时不给悬停反馈
            if (!IsAvailable()) return;
            _hovering = true;
            AnimateState();
        }

        /// <summary>
        /// 指针离开时恢复常态
        /// </summary>
        /// <param name="eventData">
        /// 指针事件数据
        /// </param>
        public void OnPointerExit(PointerEventData eventData)
        {
            _hovering = false;
            _pressed = false;
            AnimateState();
        }

        /// <summary>
        /// 指针按下时缩小
        /// </summary>
        /// <param name="eventData">
        /// 指针事件数据
        /// </param>
        public void OnPointerDown(PointerEventData eventData)
        {
            //只响应左键，右键与中键不参与按钮反馈
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (!IsAvailable()) return;
            _pressed = true;
            AnimateState();
        }

        /// <summary>
        /// 指针抬起时回弹到悬停或常态
        /// </summary>
        /// <param name="eventData">
        /// 指针事件数据
        /// </param>
        public void OnPointerUp(PointerEventData eventData)
        {
            _pressed = false;
            AnimateState();
        }

        /// <summary>
        /// 解析目标引用并记录初始缩放与常态颜色
        /// </summary>
        private void CacheReferences()
        {
            if (_target == null) _target = GetComponent<Selectable>();
            if (_tintTarget == null && _target != null) _tintTarget = _target.targetGraphic;
            if (_tintTarget == null) _tintTarget = GetComponent<Graphic>();

            if (!_scaleCached)
            {
                _scaleCached = true;
                _restingScale = transform.localScale;
            }

            //常态颜色只在第一次拿到提亮图形时记录，之后由SetBaseColor负责更新
            if (!_colorCached && _tintTarget != null)
            {
                _colorCached = true;
                _baseColor = _tintTarget.color;
            }

            _lastInteractable = IsAvailable();
        }

        /// <summary>
        /// 立即套用当前交互状态的外观，不创建补间
        /// </summary>
        private void ApplyImmediate()
        {
            KillTweens();
            var available = IsAvailable();
            _lastInteractable = available;
            //不可交互时清空悬停与按下，避免禁用期间残留按压状态
            if (!available) _hovering = _pressed = false;
            if (transform != null) transform.localScale = _restingScale * TargetScaleFactor(available);
            if (_tintTarget != null) _tintTarget.color = TargetColor(available);
        }

        /// <summary>
        /// 按当前交互状态补间缩放与颜色
        /// </summary>
        private void AnimateState()
        {
            var available = IsAvailable();
            _lastInteractable = available;
            //不可交互时清空悬停与按下，避免禁用期间残留按压状态
            if (!available) _hovering = _pressed = false;

            //先清掉目标上遗留的补间，防止外部动画与本组件的缩放、颜色互相覆盖
            if (transform != null) transform.DOKill();
            _scaleTween?.Kill();
            //按下比抬起更硬，所以按下用OutQuad，抬起用OutBack做回弹
            _scaleTween = transform.DOScale(_restingScale * TargetScaleFactor(available), _duration)
                .SetEase(_pressed ? Ease.OutQuad : Ease.OutBack)
                .SetUpdate(_useUnscaledTime)
                .SetLink(gameObject);

            if (_tintTarget == null) return;
            _tintTarget.DOKill();
            _colorTween?.Kill();
            _colorTween = DOTweenModuleUGUI.DOColor(_tintTarget, TargetColor(available), _duration)
                .SetEase(Ease.OutSine)
                .SetUpdate(_useUnscaledTime)
                .SetLink(gameObject);
        }

        /// <summary>
        /// 计算当前状态对应的缩放倍率
        /// </summary>
        /// <param name="available">
        /// 目标当前是否可交互
        /// </param>
        /// <returns>
        /// 缩放倍率
        /// </returns>
        private float TargetScaleFactor(bool available)
        {
            //禁用态不放大也不缩小，回到原始尺寸
            if (!available) return 1f;
            if (_pressed) return _pressScale;
            return _hovering ? _hoverScale : 1f;
        }

        /// <summary>
        /// 计算当前状态对应的提亮颜色
        /// </summary>
        /// <param name="available">
        /// 目标当前是否可交互
        /// </param>
        /// <returns>
        /// 该状态应显示的颜色
        /// </returns>
        private Color TargetColor(bool available)
        {
            //不可交互时整体压暗，透明度保持常态颜色不变
            if (!available)
            {
                return _dimWhenDisabled
                    ? Color.Lerp(_baseColor, new Color(0.5f, 0.5f, 0.5f, _baseColor.a), 0.65f)
                    : _baseColor;
            }

            var highlight = _hoverColor;
            highlight.a = _baseColor.a;
            //按下时在提亮色上回收一点亮度，和缩小的按压感保持一致
            if (_pressed) return Color.Lerp(highlight, _baseColor, 0.3f);
            return _hovering ? highlight : _baseColor;
        }

        /// <summary>
        /// 判断交互目标当前是否可交互
        /// </summary>
        /// <returns>
        /// 可交互返回true
        /// </returns>
        private bool IsAvailable() => _target == null || _target.interactable;

        /// <summary>
        /// 轮询可交互状态，补齐外部切换interactable时的外观刷新
        /// </summary>
        private void LateUpdate()
        {
            if (!Application.isPlaying) return;
            var available = IsAvailable();
            //interactable被外部代码切换时不会触发指针事件，这里补一次状态同步
            if (available == _lastInteractable) return;
            _lastInteractable = available;
            if (!available)
            {
                _hovering = false;
                _pressed = false;
            }
            //恢复可交互时指针可能仍停在按钮上，按指针位置把悬停态补回来
            else if (PointerIsOverButton()) _hovering = true;
            AnimateState();
        }

        /// <summary>
        /// 判断指针当前是否停在按钮的矩形范围内
        /// </summary>
        /// <returns>
        /// 指针在按钮上时返回true
        /// </returns>
        private bool PointerIsOverButton()
        {
            if (!(transform is RectTransform rect)) return false;
            var canvas = rect.GetComponentInParent<Canvas>();
            var worldCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            return RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, worldCamera);
        }

        /// <summary>
        /// 终止本组件创建的全部补间
        /// </summary>
        private void KillTweens()
        {
            _scaleTween?.Kill();
            _colorTween?.Kill();
            _scaleTween = null;
            _colorTween = null;
        }

        /// <summary>
        /// 停用时终止补间并还原初始缩放与颜色
        /// </summary>
        private void OnDisable()
        {
            _hovering = false;
            _pressed = false;
            KillTweens();
            //停用后必须复原，否则对象复用时会带着悬停放大或禁用压暗色
            if (!_scaleCached) return;
            if (transform != null) transform.localScale = _restingScale;
            if (_tintTarget != null) _tintTarget.color = _baseColor;
        }
    }
}
