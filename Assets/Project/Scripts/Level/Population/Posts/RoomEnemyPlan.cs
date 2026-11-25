using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // Where a room's enemies start and the loops its patrollers walk (RoomEnemyPlanner): one
    // loop for the room, or one per side in a Breach room split by zone. A loop may cross an
    // overpass: a stop on its deck.
    public sealed class RoomEnemyPlan
    {
        public static readonly RoomEnemyPlan Empty = new(Array.Empty<PlannedSpawn>(), Array.Empty<StandSpot>(), Array.Empty<StandSpot>(), null);

        private readonly IReadOnlyList<StandSpot> _holderPatrol;
        private readonly IReadOnlyList<StandSpot> _breachPatrol;

        public RoomEnemyPlan(IReadOnlyList<PlannedSpawn> spawns, IReadOnlyList<StandSpot> holderPatrol,
                             IReadOnlyList<StandSpot> breachPatrol, BreachSplit split)
        {
            Spawns        = spawns;
            _holderPatrol = holderPatrol;
            _breachPatrol = breachPatrol;
            Split         = split;
        }

        public IReadOnlyList<PlannedSpawn> Spawns { get; }
        // The Breach split the spawns follow, or null (one side).
        public BreachSplit Split { get; }

        // The patrol loop of one side, as stops in order.
        public IReadOnlyList<StandSpot> PatrolOf(bool breach) => breach ? _breachPatrol : _holderPatrol;
    }
}
