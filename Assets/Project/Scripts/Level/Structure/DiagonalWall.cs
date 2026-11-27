using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // A wall standing corner to corner across one tile at 45°. Rising ("/") runs from the tile's
    // lower-left corner to its upper-right one, falling ("\") from its upper-left to its lower-right.
    // Every piece starts and ends on tile corners, so a run of tiles each a diagonal step on makes
    // one straight 45° wall, two runs meeting at a corner make a chevron, and a run ending on the
    // room's wall meets it cleanly: diagonals always line up with each other and with the grid.
    //
    // To the room's planning the tile is solid (RoomStructure.TryAddDiagonals): nothing walks,
    // stands or is built on it, and it is seen over only when it is low. The floor either side of
    // the wall line is the geometry's and, in play, the NavMesh's.
    public readonly struct DiagonalWall
    {
        public DiagonalWall(Vector2Int tile, bool rising, float height = 0f)
        {
            Tile   = tile;
            Rising = rising;
            Height = height;
        }

        public Vector2Int Tile   { get; }
        public bool       Rising { get; }
        public float      Height { get; }   // metres; 0 = the room's full wall height

        // Its ends, in tile units (tile corners are whole numbers).
        public Vector2 From => Rising ? (Vector2)Tile : Tile + Vector2.up;
        public Vector2 To   => Rising ? Tile + Vector2.one : Tile + Vector2.right;

        // The way out of the wall toward the side `neighbour` (a tile beside this one) is on: a
        // 45° unit step such as (1, -1).
        public Vector2Int NormalToward(Vector2Int neighbour)
        {
            Vector2Int side = neighbour - Tile;
            // "/" faces (1, -1) and (-1, 1); "\" faces (1, 1) and (-1, -1).
            Vector2Int normal = Rising ? new Vector2Int(1, -1) : new Vector2Int(1, 1);
            return side.x * normal.x + side.y * normal.y > 0 ? normal : -normal;
        }

        // `length` pieces from `start`, each a diagonal `step` (such as (1, 1)) on from the last,
        // which join into one straight 45° wall.
        public static List<DiagonalWall> Run(Vector2Int start, Vector2Int step, int length, float height = 0f)
        {
            bool rising = step.x == step.y;
            var run = new List<DiagonalWall>(length);
            for (int i = 0; i < length; i++) run.Add(new DiagonalWall(start + step * i, rising, height));
            return run;
        }

        // The piece turned a quarter turn counter-clockwise `turns` times about the grid's origin:
        // the tile turns with the grid, and each quarter turn swaps "/" and "\".
        public DiagonalWall Turned(int turns)
        {
            Vector2Int tile = Tile;
            bool rising = Rising;
            for (int i = 0; i < (turns % 4 + 4) % 4; i++)
            {
                tile = new Vector2Int(-tile.y - 1, tile.x);
                rising = !rising;
            }
            return new DiagonalWall(tile, rising, Height);
        }

        public DiagonalWall Moved(Vector2Int offset) => new(Tile + offset, Rising, Height);
    }
}
