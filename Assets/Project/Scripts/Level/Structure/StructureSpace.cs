using UnityEngine;

namespace CGD.Level
{
    // A walled-off part of a room that is its own place: a classroom in a school, a street in a
    // town. Tile is any floor tile of it; its zone is whatever RoomStructure.ZoneOf says for
    // that tile once the zones are assigned. Function (may be null) is what furnishes it.
    public readonly struct StructureSpace
    {
        public StructureSpace(Vector2Int tile, RoomFunction function, string name)
        {
            Tile     = tile;
            Function = function;
            Name     = name;
        }

        public Vector2Int   Tile     { get; }
        public RoomFunction Function { get; }
        public string       Name     { get; }
    }
}
