// 版权归 MaxyMCP 所有，遵循 MIT 许可证。
using System.Collections.Generic;

namespace MaxyMCP.Editor.Api.Models
{
    internal class ToolCallDto
    {
        public string id;
        public string type = "function";
        public ToolCallFunctionDto function;
    }

    internal class ToolCallFunctionDto
    {
        public string name;
        public string arguments; // JSON string
    }
}
