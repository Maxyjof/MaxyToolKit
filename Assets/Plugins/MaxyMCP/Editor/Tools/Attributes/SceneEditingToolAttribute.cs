// 版权归 MaxyMCP 所有，遵循 MIT 许可证。
using System;

namespace MaxyMCP.Editor.Tools
{
    /// <summary>
    /// Marks a tool function as modifying the scene.
    /// These functions should use Undo-safe operations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal class SceneEditingToolAttribute : Attribute { }
}
