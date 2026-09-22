// 版权归 MaxyMCP 所有，遵循 MIT 许可证。
using System;

namespace MaxyMCP.Editor.Tools
{
    /// <summary>
    /// Marks a tool function as read-only.
    /// Functions with this attribute do not modify the scene or project.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal class ReadOnlyToolAttribute : Attribute { }
}
