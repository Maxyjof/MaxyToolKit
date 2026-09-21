using System;

namespace MaxyToolKit
{
    /// <summary>
    /// 使用Easy Save 3提供轻量存档入口
    /// </summary>
    public static class MStorage
    {
        /// <summary>
        /// 保存指定键和值
        /// </summary>
        /// <typeparam name="T">
        /// 值类型
        /// </typeparam>
        /// <param name="key">
        /// 存档键
        /// </param>
        /// <param name="value">
        /// 要保存的值
        /// </param>
        public static void Save<T>(string key, T value)
        {
            ValidateKey(key);
            ES3.Save(key, value);
        }

        /// <summary>
        /// 读取指定键的值
        /// </summary>
        /// <typeparam name="T">
        /// 值类型
        /// </typeparam>
        /// <param name="key">
        /// 存档键
        /// </param>
        /// <param name="defaultValue">
        /// 键不存在时返回的默认值
        /// </param>
        /// <returns>
        /// 读取到的值
        /// </returns>
        public static T Load<T>(string key, T defaultValue = default)
        {
            ValidateKey(key);
            return ES3.Load(key, defaultValue);
        }

        /// <summary>
        /// 判断指定存档键是否存在
        /// </summary>
        /// <param name="key">
        /// 存档键
        /// </param>
        /// <returns>
        /// 键存在时返回true
        /// </returns>
        public static bool Exists(string key)
        {
            ValidateKey(key);
            return ES3.KeyExists(key);
        }

        /// <summary>
        /// 删除指定存档键及其值
        /// </summary>
        /// <param name="key">
        /// 存档键
        /// </param>
        public static void Delete(string key)
        {
            ValidateKey(key);
            if (ES3.KeyExists(key)) ES3.DeleteKey(key);
        }

        /// <summary>
        /// 验证存档键不能为空
        /// </summary>
        /// <param name="key">
        /// 待验证的存档键
        /// </param>
        private static void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Storage key cannot be empty.", nameof(key));
        }
    }
}
