// 版权归 MaxyMCP 所有，遵循 MIT 许可证。
using System.Collections.Generic;

namespace MaxyMCP.Editor.Api.Models
{
    internal class ToolDefinition
    {
        public string type = "function";
        public ToolFunctionDef function;
    }

    internal class ToolFunctionDef
    {
        public string name;
        public string description;
        public ToolParametersDef parameters;
    }

    internal class ToolParametersDef
    {
        public string type = "object";
        public Dictionary<string, ToolPropertyDef> properties = new Dictionary<string, ToolPropertyDef>();
        public List<string> required = new List<string>();
    }

    internal class ToolPropertyDef
    {
        public string type;
        public string description;
        public string @default;
        public List<string> @enum;
    }
}
