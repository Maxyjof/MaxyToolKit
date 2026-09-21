// 版权归 MaxyMCP 所有，遵循 MIT 许可证。

using System;
using System.Threading.Tasks;

namespace MaxyMCP.Editor.Threading
{
    internal interface IEditorThreadHelper : IDisposable
    {
        bool IsMainThread { get; }
        Task ExecuteOnEditorThreadAsync(Action action);
        Task<T> ExecuteOnEditorThreadAsync<T>(Func<T> func);
        Task<T> ExecuteAsyncOnEditorThreadAsync<T>(Func<Task<T>> asyncFunc);
    }
}
