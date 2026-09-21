using UnityEditor;
using UnityEngine;

namespace MaxyToolKit.Editor
{
    /// <summary>
    /// 提供MaxyToolKit编辑器菜单命令
    /// </summary>
    public static class MaxyToolKitMenu
    {
        /// <summary>
        /// 创建游戏启动对象并记录撤销操作
        /// </summary>
        [MenuItem("Tools/MaxyToolKit/Create Game Bootstrap")]
        [MenuItem("工具/MaxyToolKit/创建游戏启动对象")]
        private static void CreateBootstrap()
        {
            //创建对象并注册撤销记录
            var go = new GameObject("GameBootstrap");
            Undo.RegisterCreatedObjectUndo(go, "Create Game Bootstrap");
            Selection.activeGameObject = go;
        }

        /// <summary>
        /// 清理编辑器当前进程中的全局系统
        /// </summary>
        [MenuItem("Tools/MaxyToolKit/Reset Global Systems")]
        [MenuItem("工具/MaxyToolKit/重置全局系统")]
        private static void ResetSystems()
        {
            MaxyToolKit.MSystemCenter.Reset();
            Debug.Log("MaxyToolKit global systems reset.");
        }
    }
}
