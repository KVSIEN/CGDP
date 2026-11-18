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

        // Whether it puts anything down: a prefab of its own or a group's members.
        public bool HasPieces => (Prefabs != null && Prefabs.Length > 0) || (Members != null && Members.Length > 0);
        public bool HasMembers => Members != null && Members.Length > 0;
        public bool StandsNear => !string.IsNullOrEmpty(Near);
        public bool GrowsWithRoom => Grow.x > 0f || Grow.y > 0f;
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
        [Tooltip("Only within this many steps of a route: the walkways between the doorways and the centre, and the floor in front of route-marking props. Where people pass, so patches of cover get used. 0 = anywhere")]
        [Min(0)] public int NearRoute;
        [Tooltip("Only at least this many steps from every route: dead ends and back corners, out of the traffic. 0 = anywhere")]
        [Min(0)] public int OffRoute;
        [Tooltip("People come and go through it (a service door): the floor in front of it counts as a route for the placements after it")]
        public bool MarksRoute;

        [Header("Groups, relations and size")]
        [Tooltip("Pieces set around each one placed, on its tiles: chairs round a card table, stools along a bar. With members, Prefabs may be left empty: then the members are the whole group")]
        public PropMember[] Members = Array.Empty<PropMember>();
        [Tooltip("Names what this placement puts down, so placements after it can stand Near it (several placements may share a key: the pool and its jacuzzis)")]
        public string Key;
        [Tooltip("Only within Near Distance of what an earlier placement with this Key put down: loungers round the pool, stools by the bar. Nothing is placed when it put nothing down (a Required one is still placed, anywhere). Empty = anywhere")]
        public string Near;
        [Tooltip("Steps from the nearest of what it stands Near: 1 = right beside it")]
        [Min(1)] public int NearDistance = 2;
        [Tooltip("Grows the footprint with the room, as a share of its longer (x) and shorter (y) side and never below Footprint: a pool basin that fills a big hall and shrinks in a small one. Where the grown size doesn't fit, smaller ones are tried. Such a feature is part of the room's plan, like its structure: it neither stops at nor uses up the room's share of floor for props, so the furniture round it still has its share. 0 = the fixed Footprint")]
        public Vector2 Grow;
        [Tooltip("The prefab is scaled across and front to back to the footprint it gets (build it for one 1×1 tile): one basin prefab fits every pool. Members keep their size")]
        public bool Stretch;
        [Tooltip("Only placed in a room with at least this many floor tiles (in a Breach room's side, that side's): the second pool, the roulette pit, the stage. 0 = any room")]
        [Min(0)] public int MinRoomTiles;

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

        // The placement for a space that is a single zone of its own (a room of a building):
        // "the main space" is all of it, so that zone rule is dropped; "everything else" is
        // nothing, so null. Others are unchanged.
        public RoomPropPlacement ForSingleZone()
        {
            if (Zone == PropZone.Others) return null;
            if (Zone == PropZone.Any) return this;

            var copy = (RoomPropPlacement)MemberwiseClone();
            copy.Zone = PropZone.Any;
            return copy;
        }

        // The same placement with its picky rules dropped, to find any spot at all. Level 1 gives
        // up where (tags, zone, routes, what it stands near), which way and growing with the
        // room; level 2 also the floor in front.
        public RoomPropPlacement Relaxed(int level)
        {
            var copy = (RoomPropPlacement)MemberwiseClone();
            copy.On        = RoomTileTags.None;
            copy.Zone      = PropZone.Any;
            copy.Spacing   = 0;
            copy.Selection = PropSelection.Random;
            copy.Lattice   = Vector2Int.zero;
            copy.Symmetric = false;
            copy.NearRoute = 0;
            copy.OffRoute  = 0;
            copy.Near      = null;
            copy.Grow      = Vector2.zero;
            if (copy.Facing is PropFacing.AgainstWall or PropFacing.AcrossSightline or PropFacing.AlongRoom or PropFacing.TowardNear)
                copy.Facing = PropFacing.Square;
            if (level >= 2) copy.Clearance = 0;
            return copy;
        }
    }
}
