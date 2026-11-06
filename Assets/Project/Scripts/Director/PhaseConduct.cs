using System;
using UnityEngine;

namespace CGD.Director
{
    // How boldly enemies fight during one pacing phase (see EnemyPacingTuning).
    [Serializable]
    public class PhaseConduct
    {
        [Tooltip("Enemies level-wide that may be making a bold move (flank, grenade, charge, suppress) at once, at neutral pressure")]
        [SerializeField, Min(0)] private int _boldMoves;
        [Tooltip("Multiplies how many enemies may melee or shoot one target at once (EnemyData's limits)")]
        [SerializeField, Min(0f)] private float _attackers = 1f;
        [Tooltip("Multiplies how long an enemy searches for someone it lost")]
        [SerializeField, Min(0.05f)] private float _searchTime = 1f;
        [Tooltip("Multiplies the morale below which an enemy falls back; above 1 = it falls back sooner")]
        [SerializeField, Min(0f)] private float _fallBack = 1f;

        public PhaseConduct() { }

        public PhaseConduct(int boldMoves, float attackers, float searchTime, float fallBack)
        {
            _boldMoves  = boldMoves;
            _attackers  = attackers;
            _searchTime = searchTime;
            _fallBack   = fallBack;
        }

        public int   BoldMoves  => _boldMoves;
        public float Attackers  => _attackers;
        public float SearchTime => _searchTime;
        public float FallBack   => _fallBack;
    }
}
