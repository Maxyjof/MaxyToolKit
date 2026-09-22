// 版权归 MaxyMCP 所有，遵循 MIT 许可证。

namespace MaxyMCP.Editor.Services
{
    internal interface IEditorStateService
    {
        bool IsPlayingOrWillChangePlaymode { get; }
        bool IsCompiling { get; }
    }
}
