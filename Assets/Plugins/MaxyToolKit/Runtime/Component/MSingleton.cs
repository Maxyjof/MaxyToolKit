using UnityEngine;

namespace MaxyToolKit.Component
{
    /// <summary>
    /// 为MonoBehaviour提供单实例生命周期管理
    /// </summary>
    /// <typeparam name="T">
    /// 具体的单例组件类型
    /// </typeparam>
    public abstract class MSingleton<T> : MonoBehaviour where T : MSingleton<T>
    {
        /// <summary>
        /// 获取当前类型的单例实例
        /// </summary>
        public static T Instance { get; private set; }
        [SerializeField] private bool dontDestroyOnLoad;

        /// <summary>
        /// 初始化单例并按配置决定是否跨场景保留
        /// </summary>
        protected virtual void Awake()
        {
            //发现重复实例时销毁新对象
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            //记录当前实例并应用跨场景设置
            Instance = (T)this;
            if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 销毁实例时清理静态引用
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
