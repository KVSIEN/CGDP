using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // A place on a room's outline where a doorway fits: a floor tile on a straight stretch of
    // wall (or a step of a slanted one), and the direction out of the room.
    public readonly struct DoorSocket
    {
        // `slant`: for a socket on a step of a 45° wall (an angled room's slanted side), the
        // wall's way out, a diagonal step such as (1, 1) with Outward one of its parts; zero on a
        // straight wall.
        public DoorSocket(Vector2Int inside, Vector2Int outward, Vector2Int slant = default)
        {
            Inside  = inside;
            Outward = outward;
            Slant   = slant;
        }

        public Vector2Int Inside  { get; }
        public Vector2Int Outward { get; }
        public Vector2Int Slant   { get; }
        // Set into a 45° wall: the doorway stands across the wall's diagonal (LevelDoorway), and
        // its corridor leaves on that diagonal (Throat).
        public bool       Slanted => Slant != Vector2Int.zero;

        // A slanted doorway's first corridor tiles, from Outside straight out along the diagonal:
        // a staircase of `steps` steps, each the slant's other part and then Outward, so the
        // corridor's walls (LevelWallPlan) run on from the doorway's two ends. Just Outside on a
        // straight wall.
        public List<Vector2Int> Throat(int steps)
        {
            var tiles = new List<Vector2Int> { Outside };
            if (!Slanted) return tiles;
            Vector2Int up = Slant - Outward;
            for (int i = 0; i < steps; i++)
            {
                tiles.Add(tiles[tiles.Count - 1] + up);
                tiles.Add(tiles[tiles.Count - 1] + Outward);
            }
            return tiles;
        }
        public Vector2Int Outside => Inside + Outward;
    }
}
