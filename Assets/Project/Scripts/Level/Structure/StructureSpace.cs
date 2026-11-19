using UnityEngine;

namespace CGD.Level
{
    // A walled-off part of a room that is its own place: a classroom in a school, a street in a
    // town, a restroom hut in a pool hall. Tile is any floor tile of it; its zone is whatever
    // RoomStructure.ZoneOf says for that tile once the zones are assigned. Function (may be
    // null) furnishes it, in Layout's arrangement when it has one.
    public readonly struct StructureSpace
    {
        public StructureSpace(Vector2Int tile, RoomFunction function, string name, RoomLayout layout = null, bool roofed = false,
                              bool annex = false)
        {
            Tile     = tile;
            Function = function;
            Name     = name;
            Layout   = layout;
            Roofed   = roofed;
            Annex    = annex;
        }

        public Vector2Int   Tile     { get; }
        public RoomFunction Function { get; }
        public string       Name     { get; }
        // The arrangement of the function that furnishes it (null = the function's own props).
        public RoomLayout   Layout   { get; }
        // Under a roof of its own, in a room that is otherwise open to the sky (a building in a town).
        public bool         Roofed   { get; }
        // A small structure standing in a room that is still its own place (a hut, a kiosk): the
        // room's function, cover and faction props furnish the floor around it, not its inside.
        public bool         Annex    { get; }
    }
}
