using UnityEngine;
using CGD.Combat;

namespace CGD.Targeting
{
    // Which characters a query accepts: relation to the asker, physics layers, and
    // optionally an unobstructed line from the query origin.
    public readonly struct TargetFilter
    {
        public readonly TargetAffiliation Affiliation;
        public readonly Team              Team;
        public readonly HealthManager     Self;
        public readonly LayerMask         Mask;
        public readonly bool              RequireLineOfSight;
        public readonly LayerMask         ObstacleMask;

        public TargetFilter(TargetAffiliation affiliation, Team team, HealthManager self, LayerMask mask,
                            bool requireLineOfSight = false, LayerMask obstacleMask = default)
        {
            Affiliation        = affiliation;
            Team               = team;
            Self               = self;
            Mask               = mask;
            RequireLineOfSight = requireLineOfSight;
            ObstacleMask       = obstacleMask;
        }

        // Relation and liveness only; line of sight needs an origin and is checked by TargetQuery.
        public bool Accepts(HealthManager candidate) =>
            candidate != null && !candidate.IsDead && (Affiliation & RelationOf(candidate)) != 0;

        public TargetAffiliation RelationOf(HealthManager candidate)
        {
            if (candidate == Self)                                return TargetAffiliation.Self;
            if (Team == Team.None || candidate.Team == Team.None) return TargetAffiliation.Neutral;
            return candidate.Team == Team ? TargetAffiliation.Allies : TargetAffiliation.Enemies;
        }
    }
}
