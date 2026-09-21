namespace MaxyToolKit
{
    /// <summary>
    /// MaxyToolKit的统一入口
    /// </summary>
    public static class Maxy
    {
        /// <summary>
        /// 获取全局事件总线
        /// </summary>
        public static MEventBus Events => MEventBus.Global;

        /// <summary>
        /// 重置全局系统和全局事件
        /// </summary>
        public static void Reset() => MSystemCenter.Reset();
    }
}
