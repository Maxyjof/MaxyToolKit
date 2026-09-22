using System;
using System.Collections.Generic;
using UnityEngine;
using MaxyToolKit.Event;

namespace MaxyToolKit.Core
{
    /// <summary>
    /// 管理全局系统的注册、查询和生命周期
    /// </summary>
    /// <remarks>
    /// 系统按注册顺序初始化，按相反顺序关闭
    /// </remarks>
    public static class MSystemCenter
    {
        private static readonly Dictionary<Type, object> _systems = new Dictionary<Type, object>();
        private static readonly List<object> _order = new List<object>();

        /// <summary>
        /// 注册系统并执行其初始化逻辑
        /// </summary>
        /// <typeparam name="T">
        /// 系统类型
        /// </typeparam>
        /// <param name="system">
        /// 要注册的系统实例
        /// </param>
        /// <param name="replace">
        /// 已有同类型系统时是否先移除旧实例
        /// </param>
        /// <returns>
        /// 传入的系统实例
        /// </returns>
        public static T Register<T>(T system, bool replace = false) where T : class
        {
            //验证实例并处理同类型系统
            if (system == null) throw new ArgumentNullException(nameof(system));
            var type = typeof(T);
            if (_systems.ContainsKey(type))
            {
                if (!replace) throw new InvalidOperationException("System already registered: " + type.FullName);
                Remove<T>();
            }

            //记录系统并按注册顺序保存
            _systems.Add(type, system);
            _order.Add(system);
            if (system is ISystem lifecycle) lifecycle.Initialize();
            return system;
        }

        /// <summary>
        /// 获取指定类型的系统
        /// </summary>
        /// <typeparam name="T">
        /// 系统类型
        /// </typeparam>
        /// <returns>
        /// 已注册的系统实例，未找到时返回null
        /// </returns>
        public static T Get<T>() where T : class
        {
            return TryGet<T>(out var value) ? value : null;
        }

        /// <summary>
        /// 尝试获取指定类型的系统
        /// </summary>
        /// <typeparam name="T">
        /// 系统类型
        /// </typeparam>
        /// <param name="value">
        /// 获取到的系统实例
        /// </param>
        /// <returns>
        /// 找到系统时返回true，否则返回false
        /// </returns>
        public static bool TryGet<T>(out T value) where T : class
        {
            if (_systems.TryGetValue(typeof(T), out var system))
            {
                value = system as T;
                return value != null;
            }

            value = null;
            return false;
        }

        /// <summary>
        /// 判断指定类型的系统是否已注册
        /// </summary>
        /// <typeparam name="T">
        /// 系统类型
        /// </typeparam>
        /// <returns>
        /// 已注册时返回true，否则返回false
        /// </returns>
        public static bool Contains<T>() where T : class => _systems.ContainsKey(typeof(T));

        /// <summary>
        /// 移除指定类型的系统并执行关闭逻辑
        /// </summary>
        /// <typeparam name="T">
        /// 系统类型
        /// </typeparam>
        /// <returns>
        /// 成功移除时返回true，否则返回false
        /// </returns>
        public static bool Remove<T>() where T : class
        {
            if (!_systems.TryGetValue(typeof(T), out var system)) return false;
            //先关闭系统再移除注册记录
            if (system is ISystem lifecycle) lifecycle.Shutdown();
            _systems.Remove(typeof(T));
            _order.Remove(system);
            return true;
        }

        /// <summary>
        /// 关闭并清空全部系统以及全局事件
        /// </summary>
        public static void Reset()
        {
            //按注册逆序关闭系统，保证依赖关系安全
            for (var i = _order.Count - 1; i >= 0; i--)
            {
                if (_order[i] is ISystem lifecycle)
                {
                    try { lifecycle.Shutdown(); }
                    catch (Exception exception) { Debug.LogException(exception); }
                }
            }

            //清理系统索引和全局事件
            _order.Clear();
            _systems.Clear();
            MEventBus.Global.Clear();
        }

        /// <summary>
        /// 在Unity子系统重新注册时清理静态状态
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnSubsystemRegistration() => Reset();
    }
}
