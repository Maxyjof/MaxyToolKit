using System;
using System.Collections.Generic;

namespace MaxyToolKit.Event
{
    /// <summary>
    /// 批量管理多个可释放订阅
    /// </summary>
    public sealed class MSubscriptionBag : IDisposable
    {
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        /// <summary>
        /// 添加订阅并返回原对象
        /// </summary>
        /// <typeparam name="T">
        /// 订阅对象类型
        /// </typeparam>
        /// <param name="subscription">
        /// 要托管的订阅对象
        /// </param>
        /// <returns>
        /// 传入的订阅对象
        /// </returns>
        public T Add<T>(T subscription) where T : IDisposable
        {
            if (subscription != null) _subscriptions.Add(subscription);
            return subscription;
        }

        /// <summary>
        /// 逆序释放全部订阅并清空列表
        /// </summary>
        public void Dispose()
        {
            for (var i = _subscriptions.Count - 1; i >= 0; i--) _subscriptions[i].Dispose();
            _subscriptions.Clear();
        }
    }
}
