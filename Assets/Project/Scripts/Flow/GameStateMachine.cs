using System;

namespace CGD.Flow
{
    // The game's high-level state and the rules for leaving it. Plain C# with no Unity
    // dependency: GameFlow owns one and applies the side effects (time scale, cursor,
    // scene loads) when it changes. Illegal requests — pausing from the main menu,
    // winning while loading — are refused rather than half-applied.
    public class GameStateMachine
    {
        public GameState Current { get; private set; }

        // (previous, next)
        public event Action<GameState, GameState> Changed;

        public GameStateMachine(GameState initial = GameState.Boot) => Current = initial;

        public bool CanEnter(GameState next) => next != Current && IsAllowed(Current, next);

        public bool TryEnter(GameState next)
        {
            if (!CanEnter(next)) return false;

            GameState previous = Current;
            Current = next;
            Changed?.Invoke(previous, next);
            return true;
        }

        public static bool IsAllowed(GameState from, GameState to) => from switch
        {
            GameState.Boot            => to is GameState.MainMenu or GameState.Playing or GameState.Loading,
            GameState.MainMenu        => to is GameState.Loading,
            GameState.Loading         => to is GameState.Playing or GameState.MainMenu,
            GameState.Playing         => to is GameState.Paused or GameState.GameOver or GameState.Victory
                                               or GameState.LevelTransition or GameState.Loading,
            GameState.Paused          => to is GameState.Playing or GameState.Loading or GameState.LevelTransition,
            GameState.LevelTransition => to is GameState.Loading,
            GameState.GameOver        => to is GameState.Loading or GameState.LevelTransition,
            GameState.Victory         => to is GameState.Loading or GameState.LevelTransition,
            _                         => false,
        };
    }
}
