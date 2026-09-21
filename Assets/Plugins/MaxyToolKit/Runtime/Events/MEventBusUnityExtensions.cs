using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyToolKit
{
    /// <summary>
    /// 将事件订阅绑定到Unity对象生命周期的组件
    /// </summary>
    public sealed class MSubscriptionScope : MonoBehaviour
    {
        private readonly List<IDisposable> subscriptions = new List<IDisposable>();

        /// <summary>
        /// 添加需要在对象销毁时释放的订阅
        /// </summary>
        /// <param name="subscription">
        /// 要托管的订阅对象
        /// </param>
        public void Add(IDisposable subscription) { if (subscription != null) subscriptions.Add(subscription); }

        /// <summary>
        /// 销毁对象时释放全部托管订阅
        /// </summary>
        private void OnDestroy()
        {
            //倒序释放并清空列表
            foreach (var subscription in subscriptions) subscription.Dispose();
            subscriptions.Clear();
        }
    }

    /// <summary>
    /// 提供事件总线与Unity对象生命周期的扩展方法
    /// </summary>
    public static class MEventBusUnityExtensions
    {
        /// <summary>
        /// 将订阅绑定到GameObject销毁时自动释放
        /// </summary>
        /// <param name="subscription">
        /// 要托管的订阅对象
        /// </param>
        /// <param name="owner">
        /// 订阅所属的GameObject
        /// </param>
        /// <returns>
        /// 传入的订阅对象
        /// </returns>
        public static IDisposable DisposeWith(this IDisposable subscription, GameObject owner)
        {
            if (owner == null) return subscription;
            //获取或创建生命周期托管组件
            var scope = owner.GetComponent<MSubscriptionScope>();
            if (scope == null) scope = owner.AddComponent<MSubscriptionScope>();
            scope.Add(subscription);
            return subscription;
        }

        /// <summary>
        /// 订阅事件并绑定到MonoBehaviour对象生命周期
        /// </summary>
        /// <typeparam name="T">
        /// 消息类型
        /// </typeparam>
        /// <param name="bus">
        /// 要使用的事件总线
        /// </param>
        /// <param name="owner">
        /// 订阅所属的MonoBehaviour
        /// </param>
        /// <param name="handler">
        /// 收到消息时执行的处理器
        /// </param>
        /// <returns>
        /// 用于取消订阅的对象
        /// </returns>
        public static IDisposable Subscribe<T>(this MEventBus bus, MonoBehaviour owner, Action<T> handler)
        {
            return bus.Subscribe(handler).DisposeWith(owner.gameObject);
        }
    }
}
