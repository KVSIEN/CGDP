namespace CGD.Core
{
    // One behaviour a StateMachine can be in. Tick receives whatever time step the owner
    // drives the machine with (Update's deltaTime, or GameClock's fixed tick).
    public interface IState
    {
        void Enter();
        void Tick(float deltaTime);
        void Exit();
    }
}
