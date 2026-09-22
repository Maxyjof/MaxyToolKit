// 版权归 MaxyMCP 所有，遵循 MIT 许可证。

using System;

namespace MaxyMCP.Editor.DI
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    internal sealed class InjectAttribute : Attribute
    {
    }
}
