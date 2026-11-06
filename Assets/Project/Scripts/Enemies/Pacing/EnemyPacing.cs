using UnityEngine;

namespace CGD.Enemies
{
    // The director's word to every enemy, as one shared value (like the Noise and Impact
    // buses, so enemies need no reference to the director). The GameDirector publishes it
    // each frame and clears it when it goes away; without one, enemies run Undirected.
    public static class EnemyPacing
    {
        public static EnemyPacingState Current { get; private set; } = EnemyPacingState.Undirected;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Current = EnemyPacingState.Undirected;

        public static void Publish(EnemyPacingState state) => Current = state;

        public static void Clear() => Current = EnemyPacingState.Undirected;
    }
}
