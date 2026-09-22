using UnityEngine;

namespace MaxyToolKit.Examples
{
    /// <summary>
    /// 常用工具和动画示例组件
    /// </summary>
    public sealed class ToolsExampleScene : ToolsExampleSceneLogic
    {
        /// <summary>
        /// 启动工具和动画示例
        /// </summary>
        private void Start()
        {
            InitializeExample();
        }

        /// <summary>
        /// 清理工具和动画示例
        /// </summary>
        private void OnDestroy()
        {
            CleanupExample();
        }
    }
}
