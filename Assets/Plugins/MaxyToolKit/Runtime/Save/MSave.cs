using System;

namespace MaxyToolKit.Save
{
    /// <summary>
    /// 使用Easy Save 3将数据保存到持久化文件
    /// </summary>
    public static class MSave
    {
        private const string DefaultFileName = "SaveFile.es3";

        /// <summary>
        /// 创建MaxyToolKit默认文件存储设置
        /// </summary>
        /// <returns>
        /// 使用持久化数据目录和文件存储的ES3设置
        /// </returns>
        private static ES3Settings CreateFileSettings()
        {
            //明确指定文件位置，避免项目默认设置改为PlayerPrefs后影响框架存档
            return new ES3Settings(DefaultFileName, ES3.Location.File, ES3.Directory.PersistentDataPath);
        }

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
            ES3.Save(key, value, CreateFileSettings());
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
            return ES3.Load(key, defaultValue, CreateFileSettings());
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
            return ES3.KeyExists(key, CreateFileSettings());
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
            if (ES3.KeyExists(key, CreateFileSettings()))
            {
                //只删除当前存档文件中的指定键
                ES3.DeleteKey(key, CreateFileSettings());
            }
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
