using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // Where a room's enemies start and the loops its patrollers walk (RoomEnemyPlanner): one
    // loop for the room, or one per side in a Breach room split by zone.
    public sealed class RoomEnemyPlan
    {
        public static readonly RoomEnemyPlan Empty = new(Array.Empty<PlannedSpawn>(), Array.Empty<Vector2Int>(), Array.Empty<Vector2Int>(), null);

        private readonly IReadOnlyList<Vector2Int> _holderPatrol;
        private readonly IReadOnlyList<Vector2Int> _breachPatrol;

        public RoomEnemyPlan(IReadOnlyList<PlannedSpawn> spawns, IReadOnlyList<Vector2Int> holderPatrol,
                             IReadOnlyList<Vector2Int> breachPatrol, BreachSplit split)
        {
            Spawns        = spawns;
            _holderPatrol = holderPatrol;
            _breachPatrol = breachPatrol;
            Split         = split;
        }

        public IReadOnlyList<PlannedSpawn> Spawns { get; }
        // The Breach split the spawns follow, or null (one side).
        public BreachSplit Split { get; }

        // The patrol loop of one side, as floor tiles in order.
        public IReadOnlyList<Vector2Int> PatrolOf(bool breach) => breach ? _breachPatrol : _holderPatrol;
    }
}
