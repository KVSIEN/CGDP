using System;
using UnityEngine;
using CGD.BehaviorTrees;
using CGD.Director;

namespace CGD.Enemies
{
    // Whether the director is in one of the chosen pacing phases — "only push forward while
    // it's building up or at the peak". Without a director in the scene it counts as build-up,
    // so trees behave as tuned. Says nothing about where anyone is.
    [Serializable]
    [BtNodeInfo("Enemy/Pacing/Pacing Is", "Succeeds while the director's pacing is in one of the chosen phases (no director = build-up).")]
    public sealed class PacingIs : BtCondition
    {
        [SerializeField] private PacingPhases _phases = PacingPhases.Pushing;

        public override string Summary => _phases.ToString();

        protected override bool Check(BtContext context) => (_phases & Flag(EnemyPacing.Current.Phase)) != 0;

        private static PacingPhases Flag(PacingPhase phase) => phase switch
        {
            PacingPhase.BuildUp     => PacingPhases.BuildUp,
            PacingPhase.SustainPeak => PacingPhases.Peak,
            PacingPhase.PeakFade    => PacingPhases.Fade,
            _                       => PacingPhases.Calm,
        };
    }
}
