using DG.Tweening;
using UnityEditor;
using UnityEngine;

namespace MaxyToolKit.Editor
{
    /// <summary>
    /// DOTween 运行统计窗口：运行模式下实时显示补间数量与增长趋势，用来排查补间泄漏
    /// 只在打开窗口时刷新，关闭后不再占用编辑器更新回调
    /// </summary>
    public class MDOTweenMonitorWindow : EditorWindow
    {
        /// <summary>采样间隔，避免每帧刷新界面</summary>
        const double SampleInterval = 0.5;

        /// <summary>上一次采样时间</summary>
        double _lastSampleTime;
        /// <summary>上一次采样到的播放中补间数</summary>
        int _lastPlaying;
        /// <summary>上一次采样到的活动补间数</summary>
        int _lastActive;
        /// <summary>本次采样相对上一次的变化</summary>
        int _growth;
        /// <summary>运行期间出现过的最大补间数</summary>
        int _peakPlaying;

        /// <summary>
        /// 打开 DOTween 运行统计窗口
        /// </summary>
        public static void Open()
        {
            var window = GetWindow<MDOTweenMonitorWindow>();
            window.titleContent = new GUIContent("DOTween 运行统计");
            window.minSize = new Vector2(320f, 190f);
            window.Show();
        }

        /// <summary>
        /// 注册编辑器更新回调，用于定时采样
        /// </summary>
        void OnEnable()
        {
            _lastSampleTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += OnEditorUpdate;
        }

        /// <summary>
        /// 取消编辑器更新回调
        /// </summary>
        void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        /// <summary>
        /// 编辑器更新：按固定间隔采样并重绘
        /// </summary>
        void OnEditorUpdate()
        {
            if (!Application.isPlaying) return;
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastSampleTime < SampleInterval) return;
            _lastSampleTime = now;
            int playing = DOTween.TotalPlayingTweens();
            int active = DOTween.TotalActiveTweens();
            _growth = playing - _lastPlaying;
            _lastPlaying = playing;
            _lastActive = active;
            if (playing > _peakPlaying) _peakPlaying = playing;
            Repaint();
        }

        /// <summary>
        /// 绘制统计界面
        /// </summary>
        void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("DOTween 运行统计", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("运行模式下实时采样，数字持续上涨说明有补间没有 Kill", EditorStyles.miniLabel);
            GUILayout.Space(6f);
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("未进入运行模式\n进入 Play 后这里会实时显示补间数量", MessageType.Info);
                return;
            }
            int playing = DOTween.TotalPlayingTweens();
            int active = DOTween.TotalActiveTweens();
            EditorGUILayout.LabelField("播放中的补间", playing.ToString(), EditorStyles.largeLabel);
            EditorGUILayout.LabelField("活动的补间", active.ToString(), EditorStyles.largeLabel);
            EditorGUILayout.LabelField("较上次采样", (_growth >= 0 ? "+" : string.Empty) + _growth, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("峰值", _peakPlaying.ToString(), EditorStyles.miniLabel);
            GUILayout.Space(6f);
            if (_growth > 0)
                EditorGUILayout.HelpBox("本次采样补间在增加，留意是否每次触发都在新建补间却没有 Kill", MessageType.Warning);
            else
                EditorGUILayout.HelpBox("本次采样补间没有增加，状态正常", MessageType.None);
        }
    }
}
