using UnityEngine;
using CGD.Director;

namespace CGD.Enemies
{
    // What the director wants from enemies right now: where the session is in its pacing
    // cycle and how bold they may be. It never says where anyone is — only how hard to push.
    // Undirected (no director in the scene) means "behave as tuned": no limits, no scaling.
    public readonly struct EnemyPacingState
    {
        public static readonly EnemyPacingState Undirected =
            new(PacingPhase.BuildUp, 1f, 0f, int.MaxValue, 1f, 1f, 1f, isDirected: false);

        public EnemyPacingState(PacingPhase phase, float pressure, float intensity, int boldMoves,
                                float attackerScale, float searchScale, float fallBackScale, bool isDirected = true)
        {
            Phase         = phase;
            Pressure      = pressure;
            Intensity     = intensity;
            BoldMoves     = Mathf.Max(0, boldMoves);
            AttackerScale = Mathf.Max(0f, attackerScale);
            SearchScale   = Mathf.Max(0.05f, searchScale);
            FallBackScale = Mathf.Max(0f, fallBackScale);
            IsDirected    = isDirected;
        }

        public PacingPhase Phase { get; }
        // The director's difficulty rubber band: 1 neutral, above for a player who is cruising.
        public float Pressure  { get; }
        // How stressed the player is, 0–1.
        public float Intensity { get; }
        // Enemies level-wide that may be making a bold move (flank, grenade, charge) at once.
        public int   BoldMoves { get; }
        // Multiplies how many enemies may melee or shoot one target at once.
        public float AttackerScale { get; }
        // Multiplies how long an enemy searches for someone it lost.
        public float SearchScale   { get; }
        // Multiplies the morale below which an enemy falls back (above 1: sooner).
        public float FallBackScale { get; }
        public bool  IsDirected    { get; }

        // An attacker limit from EnemyData under this pacing; never below one.
        public int ScaleAttackers(int limit) => Mathf.Max(1, Mathf.RoundToInt(limit * AttackerScale));
    }
}
