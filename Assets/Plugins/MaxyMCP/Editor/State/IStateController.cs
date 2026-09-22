// 版权归 MaxyMCP 所有，遵循 MIT 许可证。

using System;

namespace MaxyMCP.Editor.State
{
    internal interface IStateController
    {
        MaxyMCPState CurrentState { get; }
        event Action<MaxyMCPState> OnStateChanged;
        event Action OnCancelRequested;

        void SetState(MaxyMCPState state);
        void ReturnToPreviousState();
        void ClearState();
        void RequestCancel();
        bool IsInitialized { get; }
    }
}
