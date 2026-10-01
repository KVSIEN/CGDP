using System;
using UnityEngine;

namespace CGD.Level
{
    // Where a hand-built room allows a doorway: an edge tile, in the room's own tile
    // coordinates ((0, 0) = its south-west tile), and the wall it's in.
    [Serializable]
    public class LandmarkDoorway
    {
        public Vector2Int Tile;
        public RoomSide Side;

        public Vector2Int Outward => Side switch
        {
            RoomSide.North => Vector2Int.up,
            RoomSide.East  => Vector2Int.right,
            RoomSide.South => Vector2Int.down,
            _              => Vector2Int.left,
        };
    }
}
