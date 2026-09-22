// 版权归 MaxyMCP 所有，遵循 MIT 许可证。
using System;

namespace MaxyMCP.Editor.Tools
{
    [AttributeUsage(AttributeTargets.Parameter)]
    internal class ToolParamAttribute : Attribute
    {
        public string Description { get; }
        public bool Required { get; set; } = true;
        public string DefaultValue { get; set; }

        public ToolParamAttribute(string description)
        {
            Description = description;
        }
    }
}
