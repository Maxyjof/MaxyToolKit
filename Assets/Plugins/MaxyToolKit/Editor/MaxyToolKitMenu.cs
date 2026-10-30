using UnityEditor;
using UnityEngine;
using MaxyToolKit.Core;

namespace MaxyToolKit.Editor
{
    /// <summary>
    /// 提供MaxyToolKit编辑器菜单命令
    /// </summary>
    public static class MaxyToolKitMenu
    {
        /// <summary>顶部菜单分类名</summary>
        private const string _root = "MaxyToolKit/";

        /// <summary>
        /// 打开PSB转UI预制体窗口，选中PSB时自动带入路径
        /// </summary>
        [MenuItem(_root + "PSB 转 UI 预制体", false, 0)]
        private static void OpenPSBToUI()
        {
            PSBToUIPrefabWindow.Open(GetSelectedPSBPath());
        }

        /// <summary>
        /// 打开PersistentData目录：存档、日志、缓存都在这里
        /// </summary>
        [MenuItem(_root + "打开 PersistentData 目录", false, 20)]
        private static void RevealPersistentDataFolder()
        {
            string path = Application.persistentDataPath;
            if (string.IsNullOrEmpty(path) || !System.IO.Directory.Exists(path))
            {
                EditorUtility.DisplayDialog("MaxyToolKit", "PersistentData 目录还不存在：\n" + path, "好");
                return;
            }

            EditorUtility.RevealInFinder(path);
        }

        /// <summary>
        /// 清空本机PlayerPrefs存档，执行前二次确认
        /// </summary>
        [MenuItem(_root + "清理本机 PlayerPrefs 存档", false, 21)]
        private static void ClearPlayerPrefs()
        {
            if (!EditorUtility.DisplayDialog("清理存档",
                    "确定要清空本机的 PlayerPrefs 存档吗？\nPersistentData 里的文件不会动，此操作不可撤销", "清空", "取消"))
                return;

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("MaxyToolKit：已清空 PlayerPrefs 存档");
        }

        /// <summary>
        /// 打开DOTween运行统计窗口，运行时看补间数量排查泄漏
        /// </summary>
        [MenuItem(_root + "DOTween 运行统计", false, 40)]
        private static void OpenTweenMonitor()
        {
            MDOTweenMonitorWindow.Open();
        }

        /// <summary>
        /// 创建游戏启动对象并记录撤销操作
        /// </summary>
        [MenuItem(_root + "创建游戏启动对象", false, 60)]
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
        [MenuItem(_root + "重置全局系统", false, 61)]
        [MenuItem("工具/MaxyToolKit/重置全局系统")]
        private static void ResetSystems()
        {
            MSystemCenter.Reset();
            Debug.Log("MaxyToolKit global systems reset.");
        }

        /// <summary>
        /// 项目窗口右键菜单入口：把选中的PSB转成UI预制体
        /// </summary>
        [MenuItem("Assets/MaxyToolKit/PSB 转 UI 预制体", false, 100)]
        private static void ConvertSelectedPSB()
        {
            string path = GetSelectedPSBPath();
            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("PSB 转 UI 预制体", "请先在 Project 窗口里选中一个 .psb 文件", "好");
                return;
            }

            PSBToUIPrefabWindow.Open(path);
        }

        /// <summary>
        /// 右键菜单校验：只有选中PSB时才显示转换入口
        /// </summary>
        /// <returns>
        /// true表示当前选中项可以转换
        /// </returns>
        [MenuItem("Assets/MaxyToolKit/PSB 转 UI 预制体", true)]
        private static bool ValidateConvertSelectedPSB()
        {
            return !string.IsNullOrEmpty(GetSelectedPSBPath());
        }

        /// <summary>
        /// 取当前选中对象的资产路径，且必须是psb文件
        /// </summary>
        /// <returns>
        /// PSB路径，不符合时返回空字符串
        /// </returns>
        private static string GetSelectedPSBPath()
        {
            foreach (var selected in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(selected);
                if (string.IsNullOrEmpty(path)) continue;
                if (path.EndsWith(".psb", System.StringComparison.OrdinalIgnoreCase)) return path;
            }

            return string.Empty;
        }
    }
}
