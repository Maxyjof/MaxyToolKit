namespace MaxyToolKit.Core
{
    /// <summary>
    /// 由MSystemCenter管理生命周期的全局系统
    /// </summary>
    public interface ISystem
    {
        /// <summary>
        /// 初始化系统并准备运行所需资源
        /// </summary>
        void Initialize();

        /// <summary>
        /// 关闭系统并释放运行时资源
        /// </summary>
        void Shutdown();
    }
}
