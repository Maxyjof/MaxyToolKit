using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyToolKit
{
    /// <summary>
    /// 提供GameObject、Transform和集合相关的常用工具扩展
    /// </summary>
    public static class MTool
    {
        /// <summary>
        /// 获取组件，不存在时自动添加
        /// </summary>
        /// <typeparam name="T">
        /// 组件类型
        /// </typeparam>
        /// <param name="gameObject">
        /// 目标GameObject
        /// </param>
        /// <returns>
        /// 已有或新添加的组件
        /// </returns>
        public static T GetOrAdd<T>(this GameObject gameObject) where T : Component
        {
            if (!gameObject.TryGetComponent<T>(out var component)) component = gameObject.AddComponent<T>();
            return component;
        }

        /// <summary>
        /// 销毁父对象下的全部子对象
        /// </summary>
        /// <param name="parent">
        /// 子对象所属的Transform
        /// </param>
        public static void DestroyChildren(this Transform parent)
        {
            //倒序销毁以避免层级索引变化
            for (var i = parent.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
        }

        /// <summary>
        /// 重置Transform的本地位置、旋转和缩放
        /// </summary>
        /// <param name="transform">
        /// 要重置的Transform
        /// </param>
        public static void ResetLocal(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// 递归设置GameObject及全部子对象的层
        /// </summary>
        /// <param name="gameObject">
        /// 要设置的根GameObject
        /// </param>
        /// <param name="layer">
        /// 目标层索引
        /// </param>
        public static void SetLayerRecursively(this GameObject gameObject, int layer)
        {
            //先设置根对象，再递归处理子对象
            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform) child.gameObject.SetLayerRecursively(layer);
        }

        /// <summary>
        /// 从列表中随机取一个元素
        /// </summary>
        /// <typeparam name="T">
        /// 元素类型
        /// </typeparam>
        /// <param name="list">
        /// 目标列表
        /// </param>
        /// <param name="fallback">
        /// 列表为空时返回的默认值
        /// </param>
        /// <returns>
        /// 随机元素或默认值
        /// </returns>
        public static T RandomOrDefault<T>(this IList<T> list, T fallback = default)
        {
            return list == null || list.Count == 0 ? fallback : list[UnityEngine.Random.Range(0, list.Count)];
        }

        /// <summary>
        /// 使用Fisher-Yates算法随机打乱列表
        /// </summary>
        /// <typeparam name="T">
        /// 元素类型
        /// </typeparam>
        /// <param name="list">
        /// 要打乱的列表
        /// </param>
        public static void Shuffle<T>(this IList<T> list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            //从尾部向前交换随机元素
            for (var i = list.Count - 1; i > 0; i--)
            {
                var index = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[index]) = (list[index], list[i]);
            }
        }

        /// <summary>
        /// 将数值从一个范围重新映射到另一个范围
        /// </summary>
        /// <param name="value">
        /// 待映射的数值
        /// </param>
        /// <param name="fromMin">
        /// 原范围最小值
        /// </param>
        /// <param name="fromMax">
        /// 原范围最大值
        /// </param>
        /// <param name="toMin">
        /// 目标范围最小值
        /// </param>
        /// <param name="toMax">
        /// 目标范围最大值
        /// </param>
        /// <returns>
        /// 映射后的数值
        /// </returns>
        public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            //原范围无长度时直接返回目标范围起点
            if (Mathf.Approximately(fromMin, fromMax)) return toMin;
            return Mathf.Lerp(toMin, toMax, Mathf.InverseLerp(fromMin, fromMax, value));
        }
    }
}
