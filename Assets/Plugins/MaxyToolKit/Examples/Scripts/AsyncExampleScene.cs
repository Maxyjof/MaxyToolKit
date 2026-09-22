using UnityEngine;

namespace MaxyToolKit.Examples
{
    /// <summary>
    /// 异步等待和淡入淡出示例组件
    /// </summary>
    public sealed class AsyncExampleScene : AsyncExampleSceneLogic
    {
        /// <summary>
        /// 启动异步流程示例
        /// </summary>
        private void Start()
        {
            InitializeExample();
        }

        /// <summary>
        /// 清理异步流程示例
        /// </summary>
        private void OnDestroy()
        {
            CleanupExample();
        }
    }
}
