using System.Collections.Generic;
using UnityEngine;
using CGD.Abilities;
using CGD.BehaviorTrees;
using CGD.Combat;

namespace CGD.Enemies
{
    // A read-only look inside one enemy for debug views (the spectator's inspector and world
    // overlays): what it senses and believes, how it weighed the hostiles around it, the
    // cover it holds and its latest tactical searches, its abilities and its tree. Only
    // reads; nothing here changes what the enemy does. Its parts stay internal to Enemies,
    // so this is the one surface tools outside it are given.
    public readonly struct EnemyInsight
    {

        private readonly EnemyAI _ai;

        public EnemyInsight(EnemyAI ai) => _ai = ai;

        public bool IsValid => _ai != null && _ai.Perception != null;

        public EnemyAI     Ai      => _ai;
        public EnemyHealth Health  => _ai.Health;
        public EnemyData   Data    => _ai.Data;
        public Team        Team    => _ai.Health.Team;
        public bool        IsDead  => _ai.State == EnemyAI.AiState.Dead;
        public BehaviorTree Tree   => _ai.Tree;

        // --- Senses -----------------------------------------------------------------

        public Vector3        Eye           => _ai.transform.position + Vector3.up * EnemyEyes.Height;
        public Vector3        Forward       => _ai.transform.forward;
        public float          SightRange    => _ai.Perception.SightRange;
        public float          SightAngle    => _ai.Perception.SightAngle;
        public float          HearingRadius => _ai.Perception.HearingRadius;
        public AwarenessLevel Awareness     => _ai.Awareness;
        public float          AwarenessValue => _ai.AwarenessValue;

        public HealthManager Target            => _ai.Perception.Target;
        // Sensing it right now (else TargetPosition is only where it believes it is).
        public bool          IsSensingTarget   => _ai.Perception.IsSensing;
        public Vector3       TargetPosition    => _ai.Perception.TargetPosition;
        public Vector3       LastKnownPosition => _ai.Perception.LastKnownPosition;
        public bool          IsUnderFire       => _ai.Perception.IsUnderFire;
        // Toward where the fire came from, when under fire.
        public Vector3       ThreatDirection   => _ai.Perception.ThreatDirection;
        public IReadOnlyList<WeighedHostile> Weighed => _ai.Perception.Weighed;

        // --- Space ------------------------------------------------------------------

        public TacticsLog Tactics      => _ai.Tactics.Log;

        // --- Belief about an unseen target ------------------------------------------

        public bool    BeliefActive     => _ai.Belief.IsActive;
        public float   BeliefConfidence => _ai.Belief.Confidence;
        public Vector3 BeliefLastSeen   => _ai.Belief.LastSeen;
        public Vector3 BeliefHeading    => _ai.Belief.Heading;
        public IReadOnlyList<BeliefSpot> BeliefSpots => _ai.Belief.Spots;

        // Index in CoverMap.Active of the spot it holds, −1 for none.
        public int        ClaimedCover => _ai.Tactics.ClaimedCover;
        public bool       KnowsCover(int index) => _ai.Tactics.KnowsCover(index);

        public bool    HasDestination => _ai.Agent.enabled && _ai.Agent.hasPath;
        public Vector3 Destination    => _ai.Agent.destination;

        // --- Abilities --------------------------------------------------------------

        public IReadOnlyList<Ability> Abilities => _ai.Abilities.Known;
        public Ability Casting                  => _ai.Abilities.Casting;
        public float   CooldownLeft(Ability ability) => _ai.Abilities.CooldownLeft(ability);
        public float   EffectLeft(Ability ability)   => _ai.Abilities.EffectLeft(ability);

        // The rows the Behavior Tree Editor's agent status shows (state, awareness, target…).
        public void DescribeStatus(List<(string Label, string Value)> rows) => EnemyStatusReport.Describe(_ai, rows);
    }
}
