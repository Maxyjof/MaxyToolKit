// 版权归 MaxyMCP 所有，遵循 MIT 许可证。

using System;
using System.Collections.Generic;

namespace MaxyMCP.Editor.State
{
    internal class StateController : IStateController
    {
        private readonly Stack<MaxyMCPState> _stateHistory = new Stack<MaxyMCPState>();
        private MaxyMCPState _currentState = MaxyMCPState.Initialized;

        public MaxyMCPState CurrentState => _currentState;
        public bool IsInitialized => _currentState == MaxyMCPState.Initialized;

        public event Action<MaxyMCPState> OnStateChanged;
        public event Action OnCancelRequested;

        public void SetState(MaxyMCPState state)
        {
            if (_currentState == state) return;

            _stateHistory.Push(_currentState);
            _currentState = state;
            OnStateChanged?.Invoke(state);
        }

        public void ReturnToPreviousState()
        {
            _currentState = _stateHistory.Count > 0
                ? _stateHistory.Pop()
                : MaxyMCPState.Initialized;

            OnStateChanged?.Invoke(_currentState);
        }

        public void ClearState()
        {
            _stateHistory.Clear();
            _currentState = MaxyMCPState.Initialized;
            OnStateChanged?.Invoke(_currentState);
        }

        public void RequestCancel()
        {
            OnCancelRequested?.Invoke();
            ReturnToPreviousState();
        }
    }
}
