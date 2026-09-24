using UnityEngine;

namespace CGD.Timing
{
    [CreateAssetMenu(fileName = "GameTimeSettings", menuName = "CGD/Timing/Game Time Settings")]
    public class GameTimeSettings : ScriptableObject
    {
        [Tooltip("Game ticks per game-second. 60 → one tick is 1/60 s")]
        [SerializeField, Min(1)] private int _tickRate = GameClock.DefaultTickRate;
        [Tooltip("Most ticks run in one frame; a longer hitch drops the rest instead of freezing to catch up")]
        [SerializeField, Min(1)] private int _maxTicksPerFrame = 8;
        [Tooltip("Shrink the physics step with the time scale so slow motion stays smooth")]
        [SerializeField] private bool _scalePhysicsStep = true;

        public int  TickRate         => _tickRate;
        public int  MaxTicksPerFrame => _maxTicksPerFrame;
        public bool ScalePhysicsStep => _scalePhysicsStep;
    }
}
