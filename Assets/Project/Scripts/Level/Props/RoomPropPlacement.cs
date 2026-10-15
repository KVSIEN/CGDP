using System;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // One kind of furnishing a room function or dressing places: which prefabs, how many,
    // which tiles suit it (by tag and zone), how it is turned, how much floor it takes and how
    // much it keeps free in front.
    [Serializable]
    public class RoomPropPlacement
    {
        [Tooltip("Picked at random for each one placed. Origin at the centre of the footprint on the floor, +Z the front")]
        public GameObject[] Prefabs = Array.Empty<GameObject>();
        public IntRange Count = new(2, 4);
        [Tooltip("Scale Count with the room's floor area (1× at 64 tiles, 0.4× to 2×), so a closet and a hall are equally furnished")]
        public bool ScaleByRoomSize;
        [Tooltip("Floor tiles it covers: width across, depth front to back. It turns with the prop; a 1×1 can take any angle, bigger ones snap to quarter turns")]
        public Vector2Int Footprint = Vector2Int.one;
        [Tooltip("Only on tiles with one of these tags (Edge = along the walls). Nothing = anywhere. A bigger footprint needs just one of its tiles to match")]
        public RoomTileTags On;
        [Tooltip("Never on tiles with any of these tags (floor already taken by a pillar, prop or reserved access is always avoided)")]
        public RoomTileTags Avoid = RoomTileTags.Walkway | RoomTileTags.NearDoor | RoomTileTags.Centre | RoomTileTags.Structure | RoomTileTags.Prop;
        public PropZone Zone;
        public PropFacing Facing;
        [Tooltip("Free floor kept in front of it, in tiles, so it can be walked up to (a harvest node, a cache, a console). Nothing else is put there. A random facing becomes square. 0 = none")]
        [Min(0)] public int Clearance;
        [Tooltip("Free tiles kept between two of these (0 = may stand side by side)")]
        [Min(0)] public int Spacing = 1;
        public PropSelection Selection;
        [Tooltip("Props per tile, for a 1×1 footprint: a pile of crates in a corner rather than one")]
        public IntRange Pile = new(1, 1);
        [Tooltip("How far a 1×1 prop wanders from its tile's usual spot, so rows don't look ruled (along the wall for wall props)")]
        [Range(0f, 1f)] public float Jitter;
        [Tooltip("Always placed, at least one: when no tile suits, the rules are loosened step by step until one does")]
        public bool Required;
        [Tooltip("Only on tiles lined up with a grid of this pitch, in tiles, counted from the room's middle: neat rows and columns, like production lines. 0 or 1 = anywhere")]
        public Vector2Int Lattice;
        [Tooltip("Each one placed is mirrored across the room's long axis, when its twin fits: the ordered, symmetrical look. Quarter turns only, no jitter")]
        public bool Symmetric;
        [Tooltip("A decoration without a collider (ground cover, litter): takes no floor, so it may lie on walkways and in doorways, but never on a pillar or prop")]
        public bool Decor;

        // The spot for something to find and use (a resource node, an ammo cache): against a
        // wall, off the routes and doorways, with floor in front and away from what is placed.
        public static RoomPropPlacement Nook(GameObject[] prefabs) => new()
        {
            Prefabs   = prefabs,
            Count     = new IntRange(1, 1),
            On        = RoomTileTags.Edge,
            Facing    = PropFacing.AgainstWall,
            Clearance = 1,
            Selection = PropSelection.Spread,
            Required  = true,
        };

        // The same placement with its picky rules dropped, to find any spot at all. Level 1 gives
        // up where and which way; level 2 also the floor in front.
        public RoomPropPlacement Relaxed(int level)
        {
            var copy = (RoomPropPlacement)MemberwiseClone();
            copy.On        = RoomTileTags.None;
            copy.Zone      = PropZone.Any;
            copy.Spacing   = 0;
            copy.Selection = PropSelection.Random;
            copy.Lattice   = Vector2Int.zero;
            copy.Symmetric = false;
            if (copy.Facing is PropFacing.AgainstWall or PropFacing.AcrossSightline or PropFacing.AlongRoom) copy.Facing = PropFacing.Square;
            if (level >= 2) copy.Clearance = 0;
            return copy;
        }
    }
}
