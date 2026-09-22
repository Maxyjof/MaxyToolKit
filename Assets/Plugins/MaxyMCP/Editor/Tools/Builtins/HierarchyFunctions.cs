// 版权归 MaxyMCP 所有，遵循 MIT 许可证。
using System;
using System.Text;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;
using MaxyMCP.Editor.Tools.Helpers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MaxyMCP.Editor.Tools.Builtins
{
    [ToolProvider("Hierarchy")]
    internal static class HierarchyFunctions
    {
        [Description("Browse the scene hierarchy tree. Returns a tree-like view of GameObjects " +
                     "with their components, active state, and tags. " +
                     "Use root_name to start from a specific object, or leave empty for full scene.")]
        [ReadOnlyTool]
        public static string GetHierarchy(
            [ToolParam("Root object name, hierarchy path, or instance ID to start from (empty = entire scene). Finds inactive objects too.", Required = false)] string root_name = "",
            [ToolParam("Maximum depth to traverse (1-10)", Required = false)] int depth = 3,
            [ToolParam("Include component names on each object", Required = false)] bool include_components = true,
            [ToolParam("Include inactive objects", Required = false)] bool include_inactive = true)
        {
            try
            {
                depth = Mathf.Clamp(depth, 1, 10);
                var sb = new StringBuilder();

                if (!string.IsNullOrEmpty(root_name))
                {
                    // ObjectsHelper already searches every loaded scene (additively loaded ones
                    // included) plus the open prefab stage, and finds inactive objects.
                    var root = ObjectsHelper.FindTarget(root_name);
                    if (root == null)
                        return ToolResultFormatter.Error("GAME_OBJECT_NOT_FOUND", new { root_name });

                    PrintNode(sb, root.transform, 0, depth, include_components, include_inactive);
                }
                else
                {
                    // Full hierarchy: every loaded scene, not just the active one.
                    var activeScene = SceneManager.GetActiveScene();
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                    {
                        var scene = SceneManager.GetSceneAt(i);
                        if (!scene.isLoaded) continue;
                        sb.AppendLine(scene == activeScene ? $"Scene: {scene.name}" : $"Scene: {scene.name} (additive)");
                        foreach (var root in scene.GetRootGameObjects())
                        {
                            if (!include_inactive && !root.activeSelf) continue;
                            PrintNode(sb, root.transform, 0, depth, include_components, include_inactive);
                        }
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return ToolResultFormatter.Exception(ex);
            }
        }

        // --- Helpers ---

        private static void PrintNode(StringBuilder sb, Transform t, int indent, int maxDepth,
            bool includeComponents, bool includeInactive)
        {
            if (!includeInactive && !t.gameObject.activeSelf) return;

            string prefix = indent > 0 ? new string(' ', indent * 2) + "|- " : "";
            string active = t.gameObject.activeSelf ? "" : " [INACTIVE]";
            string tag = t.tag != "Untagged" ? $" tag={t.tag}" : "";

            if (includeComponents)
            {
                string comps = GetComponentSummary(t.gameObject);
                sb.AppendLine($"{prefix}{t.name}{active}{tag} [{comps}]");
            }
            else
            {
                sb.AppendLine($"{prefix}{t.name}{active}{tag}");
            }

            if (indent < maxDepth)
            {
                for (int i = 0; i < t.childCount; i++)
                {
                    PrintNode(sb, t.GetChild(i), indent + 1, maxDepth, includeComponents, includeInactive);
                }
            }
            else if (t.childCount > 0)
            {
                string childPrefix = new string(' ', (indent + 1) * 2) + "|- ";
                sb.AppendLine($"{childPrefix}... ({t.childCount} children)");
            }
        }

        private static string GetComponentSummary(GameObject go)
        {
            var names = new System.Collections.Generic.List<string>();
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null) continue;
                string name = comp.GetType().Name;
                if (name == "Transform" || name == "RectTransform") continue; // Always present, skip
                names.Add(name);
            }
            return names.Count > 0 ? string.Join(", ", names) : "-";
        }
    }
}
