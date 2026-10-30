using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;
using MaxyToolKit.Animation;

namespace MaxyToolKit.MComponent
{
    /// <summary>
    /// 数字滚动文本组件
    /// </summary>
    /// <remarks>
    /// 只负责把目标值补间成格式化文本，不参与文本网格的顶点动效
    /// 滚动过程中每帧按format重新格式化，因此千分位与正负号始终正确
    /// 数值插值在decimal上完成，大整数不会因为经过float而丢精度
    /// </remarks>
    [DisallowMultipleComponent, RequireComponent(typeof(TMP_Text))]
    public sealed class MNumberText : MonoBehaviour
    {
        /// <summary>
        /// 数字格式，例如N0表示带千分位的整数
        /// </summary>
        [SerializeField] private string _format = "N0";
        /// <summary>
        /// 一次滚动的时长
        /// </summary>
        [SerializeField] private float _duration = 0.35f;
        /// <summary>
        /// 是否使用不受Time.timeScale影响的时间
        /// </summary>
        [SerializeField] private bool _useUnscaledTime = true;

        private TMP_Text _label;
        private Tween _tween;
        private decimal _displayValue;
        private decimal _targetValue;
        private float _progress;
        private bool _hasValue;
        private bool _interrupted;

        /// <summary>
        /// 缓存文本组件并从现有文本推断当前显示值
        /// </summary>
        private void Awake()
        {
            CacheLabel();
            InitializeFromText();
        }

        /// <summary>
        /// 激活时重新绑定文本，必要时把被打断的滚动对齐到目标值
        /// </summary>
        private void OnEnable()
        {
            CacheLabel();
            //上次停用时滚动没跑完，重新激活先落到最新目标值，避免停在中间数字
            if (!_interrupted) return;
            _interrupted = false;
            //外部已在激活时重新赋值时交给新补间继续滚动
            if (_tween != null && _tween.IsActive()) return;
            if (!_hasValue) return;
            _displayValue = _targetValue;
            Render();
        }

        /// <summary>
        /// 设置整数目标值
        /// </summary>
        /// <param name="value">
        /// 目标值
        /// </param>
        /// <param name="animate">
        /// 是否播放滚动动画，false时立即显示
        /// </param>
        public void SetValue(long value, bool animate = true)
        {
            SetTarget(value, animate);
        }

        /// <summary>
        /// 设置整数目标值
        /// </summary>
        /// <remarks>
        /// 单独提供int重载，避免字面量在long与decimal两个重载之间产生调用歧义
        /// </remarks>
        /// <param name="value">
        /// 目标值
        /// </param>
        /// <param name="animate">
        /// 是否播放滚动动画，false时立即显示
        /// </param>
        public void SetValue(int value, bool animate = true)
        {
            SetTarget(value, animate);
        }

        /// <summary>
        /// 设置小数目标值
        /// </summary>
        /// <param name="value">
        /// 目标值
        /// </param>
        /// <param name="animate">
        /// 是否播放滚动动画，false时立即显示
        /// </param>
        public void SetValue(decimal value, bool animate = true)
        {
            SetTarget(value, animate);
        }

        /// <summary>
        /// 立即显示整数目标值
        /// </summary>
        /// <param name="value">
        /// 目标值
        /// </param>
        public void SetValueImmediate(long value)
        {
            SetTarget(value, false);
        }

        /// <summary>
        /// 停止滚动并直接跳到当前目标值
        /// </summary>
        public void ResetMotion()
        {
            CancelTween();
            _interrupted = false;
            //还没设置过数值时不碰文本，避免把占位文案改成0
            if (!_hasValue) return;
            _displayValue = _targetValue;
            Render();
        }

        /// <summary>
        /// 更新目标值并按需播放滚动
        /// </summary>
        /// <param name="value">
        /// 目标值
        /// </param>
        /// <param name="animate">
        /// 是否播放滚动
        /// </param>
        private void SetTarget(decimal value, bool animate)
        {
            CacheLabel();
            _targetValue = value;
            _hasValue = true;
            _interrupted = false;
            CancelTween();

            //时长为0或明确要求立即生效时直接落值，不做无意义的单帧补间
            if (!animate || _duration <= 0f || !Application.isPlaying)
            {
                _displayValue = _targetValue;
                Render();
                return;
            }

            var start = _displayValue;
            var delta = _targetValue - start;
            _progress = 0f;
            _tween = DOTween.To(() => _progress, current =>
                {
                    _progress = current;
                    //在decimal上插值，再从当前值格式化，千分位与正负号由format统一决定
                    _displayValue = start + delta * (decimal)current;
                    Render();
                }, 1f, _duration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(_useUnscaledTime)
                .SetLink(gameObject);
        }

        /// <summary>
        /// 解析文本组件引用
        /// </summary>
        private void CacheLabel()
        {
            if (_label == null) _label = GetComponent<TMP_Text>();
        }

        /// <summary>
        /// 从文本当前内容推断初始数值，让第一次SetValue从界面显示的数字开始滚动
        /// </summary>
        private void InitializeFromText()
        {
            if (_label == null || string.IsNullOrEmpty(_label.text)) return;
            if (!decimal.TryParse(_label.text, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed)) return;
            _displayValue = parsed;
            _targetValue = parsed;
            _hasValue = true;
        }

        /// <summary>
        /// 按当前显示值刷新文本
        /// </summary>
        private void Render()
        {
            if (_label == null) return;
            _label.text = FormatValue(_displayValue);
        }

        /// <summary>
        /// 把数值按format转成文本，格式串非法时退回默认数字输出
        /// </summary>
        /// <param name="value">
        /// 要格式化的数值
        /// </param>
        /// <returns>
        /// 格式化后的文本
        /// </returns>
        private string FormatValue(decimal value)
        {
            if (string.IsNullOrEmpty(_format)) return value.ToString(CultureInfo.CurrentCulture);
            try
            {
                return value.ToString(_format, CultureInfo.CurrentCulture);
            }
            catch (System.FormatException)
            {
                //格式串写错时静默退回默认输出，避免滚动中每帧抛异常打断界面
                return value.ToString(CultureInfo.CurrentCulture);
            }
        }

        /// <summary>
        /// 终止当前滚动补间
        /// </summary>
        private void CancelTween()
        {
            if (_tween == null) return;
            _tween.Kill();
            _tween = null;
        }

        /// <summary>
        /// 停用时终止补间并记录滚动是否被打断
        /// </summary>
        private void OnDisable()
        {
            //滚动未结束时记一笔，重新激活后先对齐到最新目标值
            if (_tween != null && _tween.IsActive()) _interrupted = true;
            CancelTween();
        }
    }
}
