using UnityEngine;

namespace MaxyToolKit.Examples
{
    /// <summary>
    /// 事件总线和全局系统示例组件
    /// </summary>
    public sealed class EventBusExampleScene : EventBusExampleSceneLogic
    {
        /// <summary>
        /// 启动事件总线示例
        /// </summary>
        private void Start()
        {
            InitializeExample();
        }

        /// <summary>
        /// 清理事件总线示例
        /// </summary>
        private void OnDestroy()
        {
            CleanupExample();
        }
    }
}
