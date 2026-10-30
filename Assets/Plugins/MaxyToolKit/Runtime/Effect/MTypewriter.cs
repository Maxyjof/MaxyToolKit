using System.Collections;
using MaxyToolKit.Tool;
using TMPro;
using UnityEngine;

namespace MaxyToolKit.Effect
{
    /// <summary>
    /// 打字机文本组件，挂在带TMP_Text的节点上按固定速度逐字显示内容
    /// </summary>
    /// <remarks>
    /// 逐字显示用text分段赋值实现，每显示一个字后等待间隔，遇到标点额外停顿
    /// 停用组件时会终止协程并补全文本，重新启用时从头再播一次
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class MTypewriter : MonoBehaviour
    {
        /// <summary>
        /// 每秒显示的字符数
        /// </summary>
        [SerializeField]
        [Tooltip("每秒显示的字符数")]
        private float _charactersPerSecond = 30f;

        /// <summary>
        /// 组件启用时是否自动播放
        /// </summary>
        [SerializeField]
        [Tooltip("组件启用时是否自动播放")]
        private bool _playOnEnable = true;

        /// <summary>
        /// 遇到标点时额外停顿的秒数
        /// </summary>
        [SerializeField]
        [Tooltip("遇到标点时额外停顿的秒数")]
        private float _punctuationPause = 0.08f;

        /// <summary>
        /// 是否使用不受Time.timeScale影响的时间
        /// </summary>
        [SerializeField]
        [Tooltip("是否使用不受Time.timeScale影响的时间")]
        private bool _useUnscaledTime = true;

        /// <summary>
        /// 实际用于显示文字的TMP组件
        /// </summary>
        private TMP_Text _targetText;

        /// <summary>
        /// 当前播放的协程句柄，为空表示没有在播
        /// </summary>
        private Coroutine _playRoutine;

        /// <summary>
        /// 本次播放的完整内容，用于跳过与重新启用时复播
        /// </summary>
        private string _currentContent;

        /// <summary>
        /// 是否正在逐字播放
        /// </summary>
        private bool _isPlaying;

        /// <summary>
        /// 需要额外停顿的标点集合，中英文标点都收进来
        /// </summary>
        private const string PauseCharacters = "，。！？；：、…—,.!?;:";

        /// <summary>
        /// 是否正在逐字播放
        /// </summary>
        public bool IsPlaying
        {
            get { return _isPlaying; }
        }

        /// <summary>
        /// 启用时按配置决定是否自动重播当前文本
        /// </summary>
        private void OnEnable()
        {
            //组件可能在Awake之前就被启用，这里再解析一次保证拿到TMP组件
            ResolveTarget();
            if (!_playOnEnable) return;
            //优先复播上一次的完整内容，避免中途停用时把半截文本当成全文
            string content = _currentContent;
            if (string.IsNullOrEmpty(content) && _targetText != null) content = _targetText.text;
            if (string.IsNullOrEmpty(content)) return;
            Play(content);
        }

        /// <summary>
        /// 停用时终止协程并补全文本，避免界面留在半截内容上
        /// </summary>
        private void OnDisable()
        {
            bool wasPlaying = _isPlaying;
            StopRoutine();
            if (wasPlaying && _targetText != null && _currentContent != null) _targetText.text = _currentContent;
        }

        /// <summary>
        /// 解析节点上的TMP组件，只在为空时取一次
        /// </summary>
        private void ResolveTarget()
        {
            //不用惰性绑定的简写运算符，显式判空后再取组件
            if (_targetText == null) _targetText = GetComponent<TMP_Text>();
        }

        /// <summary>
        /// 用组件上配置的默认速度播放一段内容
        /// </summary>
        /// <param name="content">
        /// 要逐字显示的内容
        /// </param>
        public void Play(string content)
        {
            Play(content, _charactersPerSecond);
        }

        /// <summary>
        /// 用指定速度播放一段内容，会打断上一次播放
        /// </summary>
        /// <param name="content">
        /// 要逐字显示的内容
        /// </param>
        /// <param name="speed">
        /// 每秒显示的字符数，非正数时退回组件配置的速度
        /// </param>
        public void Play(string content, float speed)
        {
            ResolveTarget();
            if (_targetText == null)
            {
                MLog.Warning("MTypewriter缺少TMP_Text组件，无法播放打字机效果");
                return;
            }
            //同一时刻只保留一条逐字协程，先停掉上一次
            StopRoutine();
            _currentContent = content;
            if (_currentContent == null) _currentContent = string.Empty;
            float safeSpeed = speed > 0f ? speed : _charactersPerSecond;
            if (safeSpeed <= 0f) safeSpeed = 1f;
            //节点未激活时协程无法启动，直接显示全文兜底
            if (!isActiveAndEnabled)
            {
                _targetText.text = _currentContent;
                _targetText.maxVisibleCharacters = int.MaxValue;
                return;
            }
            _isPlaying = true;
            _playRoutine = StartCoroutine(TypeRoutine(_currentContent, safeSpeed));
        }

        /// <summary>
        /// 立即显示全文并停止播放
        /// </summary>
        public void SkipToEnd()
        {
            StopRoutine();
            if (_targetText == null) return;
            string content = _currentContent;
            if (content == null) content = _targetText.text;
            _targetText.text = content;
            _targetText.maxVisibleCharacters = int.MaxValue;
        }

        /// <summary>
        /// 逐字显示协程，负责按速度显示字符并在标点处额外停顿
        /// </summary>
        /// <param name="content">
        /// 完整内容
        /// </param>
        /// <param name="speed">
        /// 每秒显示的字符数，调用方保证为正数
        /// </param>
        /// <returns>
        /// 协程迭代器
        /// </returns>
        private IEnumerator TypeRoutine(string content, float speed)
        {
            float interval = 1f / speed;
            //先清空可见字符并放开可见数量限制，保证分段赋值能立即显示
            _targetText.text = string.Empty;
            _targetText.maxVisibleCharacters = int.MaxValue;
            for (int i = 0; i < content.Length; i++)
            {
                //节点在播放途中被销毁时直接收尾，避免访问已释放的组件
                if (_targetText == null) break;
                _targetText.text = content.Substring(0, i + 1);
                yield return WaitFor(interval);
                //标点处多等一小段，让语气有停顿感
                if (_punctuationPause > 0f && IsPauseCharacter(content[i])) yield return WaitFor(_punctuationPause);
            }
            //收尾时整段赋值一次，避免分段赋值时富文本标签停在半截状态
            if (_targetText != null) _targetText.text = content;
            _isPlaying = false;
            _playRoutine = null;
        }

        /// <summary>
        /// 按配置返回受时间缩放影响或真空时间影响的等待对象
        /// </summary>
        /// <param name="seconds">
        /// 等待秒数
        /// </param>
        /// <returns>
        /// 可直接yield的等待对象
        /// </returns>
        private object WaitFor(float seconds)
        {
            if (_useUnscaledTime) return new WaitForSecondsRealtime(seconds);
            return new WaitForSeconds(seconds);
        }

        /// <summary>
        /// 判断某个字符是否是需要在显示后额外停顿的标点
        /// </summary>
        /// <param name="character">
        /// 待判断的字符
        /// </param>
        /// <returns>
        /// 需要额外停顿返回true
        /// </returns>
        private static bool IsPauseCharacter(char character)
        {
            if (character == '\n') return true;
            return PauseCharacters.IndexOf(character) >= 0;
        }

        /// <summary>
        /// 停止当前逐字协程并把播放状态复位
        /// </summary>
        private void StopRoutine()
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }
            _isPlaying = false;
        }
    }
}
