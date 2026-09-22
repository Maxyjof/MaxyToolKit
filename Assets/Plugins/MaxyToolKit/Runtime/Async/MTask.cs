using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace MaxyToolKit.Async
{
    /// <summary>
    /// 提供常用的UniTask异步等待封装
    /// </summary>
    public static class MTask
    {
        /// <summary>
        /// 按秒数异步等待指定时长
        /// </summary>
        /// <param name="seconds">
        /// 等待秒数
        /// </param>
        /// <param name="unscaled">
        /// 是否使用不受时间缩放影响的时间
        /// </param>
        /// <param name="cancellationToken">
        /// 用于取消等待的令牌
        /// </param>
        /// <returns>
        /// 表示等待过程的UniTask
        /// </returns>
        public static UniTask Delay(float seconds, bool unscaled = false, CancellationToken cancellationToken = default)
        {
            return UniTask.Delay(System.TimeSpan.FromSeconds(seconds), unscaled ? DelayType.UnscaledDeltaTime : DelayType.DeltaTime, PlayerLoopTiming.Update, cancellationToken);
        }

        /// <summary>
        /// 异步等待下一帧
        /// </summary>
        /// <param name="cancellationToken">
        /// 用于取消等待的令牌
        /// </param>
        /// <returns>
        /// 表示下一帧等待过程的UniTask
        /// </returns>
        public static UniTask NextFrame(CancellationToken cancellationToken = default) => UniTask.NextFrame(cancellationToken);

        /// <summary>
        /// 异步等待条件变为true
        /// </summary>
        /// <param name="predicate">
        /// 用于判断是否完成等待的条件
        /// </param>
        /// <param name="cancellationToken">
        /// 用于取消等待的令牌
        /// </param>
        /// <returns>
        /// 表示条件等待过程的UniTask
        /// </returns>
        public static UniTask WaitUntil(Func<bool> predicate, CancellationToken cancellationToken = default)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            return UniTask.WaitUntil(predicate, PlayerLoopTiming.Update, cancellationToken);
        }
    }
}
