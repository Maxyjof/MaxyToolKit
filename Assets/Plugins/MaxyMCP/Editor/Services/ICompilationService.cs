// 版权归 MaxyMCP 所有，遵循 MIT 许可证。

using System;
using System.Threading.Tasks;
using MaxyMCP.Editor.Tools.Helpers;

namespace MaxyMCP.Editor.Services
{
    internal interface ICompilationService
    {
        bool IsCompiling { get; }
        EditorRefreshResult LastRefreshResult { get; }
        event Action OnCompilationFinished;
        Task<bool> WaitForCompilationAsync(bool forceRefresh, int timeoutSeconds);
        string GetCompilationErrors(int maxEntries = 50, bool includeWarnings = false);
    }
}
