// 版权归 MaxyMCP 所有，遵循 MIT 许可证。

namespace MaxyMCP.Editor.Tools.Scripting
{
    /// <summary>
    /// Implement this in a code snippet passed to <c>execute_code</c> to opt into
    /// the structured execution path: automatic Undo registration, change tracking,
    /// and structured log capture.
    ///
    /// Template:
    /// <code>
    /// using UnityEngine;
    /// using UnityEditor;
    /// using MaxyMCP.Editor.Tools.Scripting;
    ///
    /// public class CommandScript : IMaxyMCPCommand
    /// {
    ///     public void Execute(ExecutionContext ctx)
    ///     {
    ///         var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
    ///         ctx.RegisterObjectCreation(go);          // Undo + tracking
    ///         ctx.Log("Created {0}", go.name);
    ///     }
    /// }
    /// </code>
    /// </summary>
    public interface IMaxyMCPCommand
    {
        void Execute(ExecutionContext ctx);
    }
}
