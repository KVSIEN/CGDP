using System;

namespace CGD.Core
{
    // A state built from callbacks, for small machines (a door, a switch, a turret)
    // where a class per state would be more ceremony than logic. Any callback may be null.
    public sealed class DelegateState : IState
    {
        private readonly Action        _enter;
        private readonly Action<float> _tick;
        private readonly Action        _exit;

        public DelegateState(Action enter = null, Action<float> tick = null, Action exit = null)
        {
            _enter = enter;
            _tick  = tick;
            _exit  = exit;
        }

        public void Enter() => _enter?.Invoke();
        public void Tick(float deltaTime) => _tick?.Invoke(deltaTime);
        public void Exit() => _exit?.Invoke();
    }
}
