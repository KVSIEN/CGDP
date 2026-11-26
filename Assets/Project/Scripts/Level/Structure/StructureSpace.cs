using UnityEngine;

namespace CGD.Level
{
    // A part of a room that is its own place: a classroom in a school, a street in a town, a
    // restroom hut in a pool hall, a walk-in fridge in a kitchen. Tile is any floor tile of it;
    // its zone is whatever RoomStructure.ZoneOf says for that tile once the zones are assigned.
    // Function (may be null) furnishes it, in Layout's arrangement when it has one. Area, when
    // the place knows its own rectangle (a pavilion), says where it stands, for labels and for
    // telling a player which one they are in.
    public readonly struct StructureSpace
    {
        public StructureSpace(Vector2Int tile, RoomFunction function, string name, RoomLayout layout = null, bool roofed = false,
                              bool annex = false, RectInt? area = null)
        {
            Tile     = tile;
            Function = function;
            Name     = name;
            Layout   = layout;
            Roofed   = roofed;
            Annex    = annex;
            Area     = area ?? new RectInt(tile, Vector2Int.zero);
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
        // The tiles it covers; empty (zero size) when only Tile is known.
        public RectInt      Area     { get; }

        public bool HasArea => Area.width > 0 && Area.height > 0;

        // The middle of it, in tile units (a tile's own middle is its x + 0.5).
        public Vector2 Centre => HasArea ? Area.center : new Vector2(Tile.x + 0.5f, Tile.y + 0.5f);

        public bool Covers(Vector2Int tile) => HasArea && Area.Contains(tile);
    }
}
