using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyToolKit.Event
{
    /// <summary>
    /// 按消息类型同步分发事件的事件总线
    /// </summary>
    public sealed class MEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> handlers = new Dictionary<Type, List<Delegate>>();
        /// <summary>
        /// 全局共享事件总线实例
        /// </summary>
        public static readonly MEventBus Global = new MEventBus();

        /// <summary>
        /// 处理订阅回调异常的回调
        /// </summary>
        public Action<Exception> ExceptionHandler { get; set; } = exception => Debug.LogException(exception);

        /// <summary>
        /// 订阅指定类型的消息
        /// </summary>
        /// <typeparam name="T">
        /// 消息类型
        /// </typeparam>
        /// <param name="handler">
        /// 收到消息时执行的处理器
        /// </param>
        /// <returns>
        /// 用于取消订阅的对象
        /// </returns>
        public IDisposable Subscribe<T>(Action<T> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            //按消息类型获取或创建处理器列表
            var type = typeof(T);
            if (!handlers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                handlers.Add(type, list);
            }

            //记录处理器并返回自动取消对象
            list.Add(handler);
            return new Subscription(() => Unsubscribe(handler));
        }

        /// <summary>
        /// 取消指定类型的消息订阅
        /// </summary>
        /// <typeparam name="T">
        /// 消息类型
        /// </typeparam>
        /// <param name="handler">
        /// 要移除的处理器
        /// </param>
        public void Unsubscribe<T>(Action<T> handler)
        {
            if (handlers.TryGetValue(typeof(T), out var list))
            {
                list.Remove(handler);
                if (list.Count == 0) handlers.Remove(typeof(T));
            }
        }

        /// <summary>
        /// 发布一条消息并同步调用全部订阅者
        /// </summary>
        /// <typeparam name="T">
        /// 消息类型
        /// </typeparam>
        /// <param name="message">
        /// 要发布的消息
        /// </param>
        public void Publish<T>(T message)
        {
            if (!handlers.TryGetValue(typeof(T), out var list)) return;
            //复制列表以允许回调中安全修改订阅
            var snapshot = list.ToArray();
            foreach (var callback in snapshot)
            {
                try { ((Action<T>)callback).Invoke(message); }
                catch (Exception exception) { ExceptionHandler?.Invoke(exception); }
            }
        }

        /// <summary>
        /// 创建默认消息并发布
        /// </summary>
        /// <typeparam name="T">
        /// 具有无参构造函数的消息类型
        /// </typeparam>
        public void Publish<T>() where T : new() => Publish(new T());

        /// <summary>
        /// 清除指定类型的全部订阅
        /// </summary>
        /// <typeparam name="T">
        /// 消息类型
        /// </typeparam>
        public void Clear<T>() => handlers.Remove(typeof(T));

        /// <summary>
        /// 清除全部消息订阅
        /// </summary>
        public void Clear() => handlers.Clear();

        /// <summary>
        /// 封装一次订阅的取消逻辑
        /// </summary>
        private sealed class Subscription : IDisposable
        {
            private Action dispose;
            /// <summary>
            /// 创建订阅释放对象
            /// </summary>
            /// <param name="disposeAction">
            /// 释放时执行的取消订阅回调
            /// </param>
            public Subscription(Action disposeAction) => dispose = disposeAction;

            /// <summary>
            /// 取消订阅并清空释放回调
            /// </summary>
            public void Dispose()
            {
                dispose?.Invoke();
                dispose = null;
            }
        }
    }
}
