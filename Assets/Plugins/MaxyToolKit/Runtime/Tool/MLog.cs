using UnityEngine;

namespace MaxyToolKit.Tool
{
    /// <summary>
    /// 提供可统一开关和标记的Unity日志方法
    /// </summary>
    public static class MLog
    {
        /// <summary>
        /// 是否输出普通日志和警告
        /// </summary>
        public static bool Enabled = true;

        /// <summary>
        /// 输出普通日志
        /// </summary>
        /// <param name="message">
        /// 日志内容
        /// </param>
        public static void Log(object message) { if (Enabled) Debug.Log("[Maxy] " + message); }

        /// <summary>
        /// 输出带标记的普通日志
        /// </summary>
        /// <param name="tag">
        /// 日志标记
        /// </param>
        /// <param name="message">
        /// 日志内容
        /// </param>
        public static void Log(string tag, object message) { if (Enabled) Debug.Log("[Maxy:" + tag + "] " + message); }

        /// <summary>
        /// 输出警告日志
        /// </summary>
        /// <param name="message">
        /// 日志内容
        /// </param>
        public static void Warning(object message) { if (Enabled) Debug.LogWarning("[Maxy] " + message); }

        /// <summary>
        /// 输出错误日志
        /// </summary>
        /// <param name="message">
        /// 日志内容
        /// </param>
        public static void Error(object message) { Debug.LogError("[Maxy] " + message); }

        /// <summary>
        /// 输出异常日志
        /// </summary>
        /// <param name="exception">
        /// 要输出的异常对象
        /// </param>
        public static void Exception(System.Exception exception) { Debug.LogException(exception); }
    }
}
