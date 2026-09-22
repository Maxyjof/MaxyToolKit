// 版权归 MaxyMCP 所有，遵循 MIT 许可证。

using System;
using System.Threading.Tasks;
using UnityEditor;

namespace MaxyMCP.Editor.Tools.Helpers
{
    /// <summary>
    /// Wait for the editor to finish compiling and importing before running a tool.
    /// Tools that depend on the latest assemblies / asset state — most notably
    /// <c>execute_code</c> after an external file edit — should call
    /// <see cref="RefreshAndWaitForReady"/> first so agents don't have to chain
    /// <c>request_recompile</c> manually every time.
    /// </summary>
    internal static class EditorReadyHelper
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(120);

        public static async Task RefreshAndWaitForReady()
        {
            await EditorRefreshHelper.RefreshAndWaitForReadyAsync(DefaultTimeout);
        }

        public static Task WaitForEditorReadyAsync(TimeSpan timeout)
        {
            return EditorRefreshHelper.WaitForEditorReadyAsync(timeout);
        }
    }
}
