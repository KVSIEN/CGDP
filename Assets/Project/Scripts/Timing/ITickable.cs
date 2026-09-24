namespace CGD.Timing
{
    // Something advanced by GameClock at a fixed rate instead of by Update(). deltaTime
    // is always the clock's TickDuration (1/60 s by default), so the same inputs give the
    // same results regardless of frame rate.
    public interface ITickable
    {
        void Tick(float deltaTime);
    }
}
