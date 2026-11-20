using UnityEngine;
using CGD.Core;

namespace CGD.Director
{
    // The level's route as the director sees it (Left 4 Dead's "flow"): rooms ahead of the
    // player toward the exit and behind them toward the start. A generated level provides one
    // (LevelRoute); without it, the director places things anywhere around the player.
    public interface IRouteSpots
    {
        // A point on the floor of one of the first `rooms` rooms on the given side of the
        // player's, or false when the player is in no room (a corridor) or none lies that way.
        bool TryPick(Vector3 player, RouteSide side, int rooms, RandomStream random, out Vector3 point);
    }
}
