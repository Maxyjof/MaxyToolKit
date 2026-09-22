// 版权归 MaxyMCP 所有，遵循 MIT 许可证。

using UnityEditor;

namespace MaxyMCP.Editor.Services
{
    internal class EditorStateService : IEditorStateService
    {
        public bool IsPlayingOrWillChangePlaymode =>
            EditorApplication.isPlayingOrWillChangePlaymode;

        public bool IsCompiling => EditorApplication.isCompiling;
    }
}
