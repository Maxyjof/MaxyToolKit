using UnityEngine;

namespace MaxyToolKit.Examples
{
    /// <summary>
    /// MProperty响应式数据示例组件
    /// </summary>
    public sealed class PropertyExampleScene : PropertyExampleSceneLogic
    {
        /// <summary>
        /// 启动响应式数据示例
        /// </summary>
        private void Start()
        {
            InitializeExample();
        }

        /// <summary>
        /// 清理响应式数据示例
        /// </summary>
        private void OnDestroy()
        {
            CleanupExample();
        }
    }
}
