// 版权归 MaxyMCP 所有，遵循 MIT 许可证。
using System;

namespace MaxyMCP.Editor.Tools
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class ToolProviderAttribute : Attribute
    {
        public string Category { get; }

        public ToolProviderAttribute(string category = null)
        {
            Category = category;
        }
    }
}
