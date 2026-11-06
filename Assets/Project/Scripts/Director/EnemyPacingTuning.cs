using System;
using UnityEngine;
using CGD.Enemies;

namespace CGD.Director
{
    // How the pacing cycle shapes enemies already in the level, phase by phase: at the peak
    // they push together; in the fade and the calm they hold back, give up searches sooner
    // and fall back more readily, so the calm actually feels calm.
    [Serializable]
    public class EnemyPacingTuning
    {
        [SerializeField] private PhaseConduct _buildUp = new(boldMoves: 2, attackers: 1f,    searchTime: 1f,    fallBack: 1f);
        [SerializeField] private PhaseConduct _peak    = new(boldMoves: 4, attackers: 1.5f,  searchTime: 1.25f, fallBack: 0.75f);
        [SerializeField] private PhaseConduct _fade    = new(boldMoves: 1, attackers: 0.75f, searchTime: 0.6f,  fallBack: 1.25f);
        [SerializeField] private PhaseConduct _relax   = new(boldMoves: 1, attackers: 0.5f,  searchTime: 0.4f,  fallBack: 1.5f);

        public PhaseConduct For(PacingPhase phase) => phase switch
        {
            PacingPhase.BuildUp     => _buildUp,
            PacingPhase.SustainPeak => _peak,
            PacingPhase.PeakFade    => _fade,
            _                       => _relax,
        };

        // What enemies are told for this moment. Pressure scales the bold-move budget, so a
        // player who is cruising faces more coordinated pushes.
        public EnemyPacingState StateFor(PacingPhase phase, float pressure, float intensity)
        {
            PhaseConduct conduct = For(phase);
            int boldMoves = Mathf.RoundToInt(conduct.BoldMoves * pressure);
            return new EnemyPacingState(phase, pressure, intensity, boldMoves,
                                        conduct.Attackers, conduct.SearchTime, conduct.FallBack);
        }
    }
}
