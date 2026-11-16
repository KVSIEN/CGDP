using System;
using CGD.Combat;

namespace CGD.Factions
{
    // One row of FactionRelations: how the team From treats the team Toward.
    [Serializable]
    public struct FactionRelation
    {
        public Team            From;
        public Team            Toward;
        public FactionAttitude Attitude;

        public FactionRelation(Team from, Team toward, FactionAttitude attitude)
        {
            From     = from;
            Toward   = toward;
            Attitude = attitude;
        }
    }
}
