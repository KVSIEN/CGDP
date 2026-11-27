using System;
using UnityEngine;
using CGD.Core;
using CGD.Enemies;

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
        // Whether it puts anything down or marks anything out: an area may be left unmarked.
        public bool IsUsed => HasPieces || Area;
        public bool StandsWithin => !string.IsNullOrEmpty(Within);
        public bool HasMembers => Members != null && Members.Length > 0;
        public bool StandsNear => !string.IsNullOrEmpty(Near);
        public bool GrowsWithRoom => Grow.x > 0f || Grow.y > 0f;

        // Whether each prop it puts down may be moved on its own once the room is furnished
        // (RoomPlanRefiner): loose cover and clutter that stands by itself. A prop that others
        // depend on (a keyed one, one set Near another, a group, a twin), that keeps floor in
        // front of it or marks a route, or that is placed by a selection with a shape of its
        // own (a cluster, a line, a showcase) would lose what it was placed for.
        public bool CanRelocate =>
            !Required && !Symmetric && !GrowsWithRoom && !HasMembers && !StandsNear && !Area && !StandsWithin && !HalfTile && string.IsNullOrEmpty(Key)
            && Clearance == 0 && !MarksRoute && Pile.Max <= 1 && Prefabs != null && Prefabs.Length > 0
            && Selection is PropSelection.Random or PropSelection.Spread or PropSelection.Cover;
        public IntRange Count = new(2, 4);
        [Tooltip("Scale Count with the room's floor area (1× at 64 tiles, 0.4× to 4×), so a closet and a hall are equally furnished")]
        public bool ScaleByRoomSize;
        [Tooltip("Floor tiles it covers: width across, depth front to back. It turns with the prop; a 1×1 can take any angle, bigger ones snap to quarter turns")]
        public Vector2Int Footprint = Vector2Int.one;
        [Tooltip("Only on tiles with one of these tags (Edge = along the walls). Nothing = anywhere. A bigger footprint needs just one of its tiles to match")]
        public RoomTileTags On;
        [Tooltip("Never on tiles with any of these tags (floor already taken by a pillar, prop or reserved access is always avoided)")]
        public RoomTileTags Avoid = RoomTileTags.Walkway | RoomTileTags.NearDoor | RoomTileTags.Centre | RoomTileTags.Structure | RoomTileTags.Prop;
        public PropZone Zone;
        [Tooltip("Count is for each zone it may use, not for the room: a bunk and a locker in every cabin, a bed in every bay. Zone still says which (Others = every cell but the main space); counts that scale by room size read each zone's floor")]
        public bool EachZone;
        public PropFacing Facing;
        [Tooltip("Free floor kept in front of it, in tiles, so it can be walked up to (a harvest node, a cache, a console). Nothing else is put there. A random facing becomes square. 0 = none")]
        [Min(0)] public int Clearance;
        [Tooltip("Free tiles kept between two of these (0 = may stand side by side)")]
        [Min(0)] public int Spacing = 1;
        public PropSelection Selection;
        [Tooltip("Flank selection only: what the pairs frame. Near = each of what it stands Near; Doorways = each doorway, along the wall either side; Stairs = each flight, at its top on raised floor and its foot otherwise; Route = the room's walkway from the way in, a pair every Rhythm.x tiles")]
        public FlankAnchor Flanks;
        [Tooltip("Flank selection only: tiles kept between a pair and what it frames, at least (0 = as close as both fit): a wide aisle between the columns down a route")]
        [Min(0)] public int FlankGap;
        [Tooltip("Flank of the Route only: a pair every x tiles along the walkway, the first y tiles in from the doorway (columns every 4 from 3, lamps every 4 from 5, so the two alternate)")]
        public Vector2Int Rhythm = new(4, 3);
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
        [Tooltip("Only in this part of the room's flow, every tile of it: Threshold (just inside the doorway a player arrives by), Past Threshold, or Goal (the far end). A room no doorway leads back into takes every tile")]
        public RoomFlowBand Band;
        [Tooltip("Only on floor at this level, every tile of it: Ground (the doorways' level), Raised (a platform, stage, mezzanine or upper terrace) or Sunken (a pit, crater or lower terrace). In a room that got no such floor (no platform or pit was built), anywhere, so the room keeps its furniture")]
        public PropFloor Floor;

        [Tooltip("Only where this many tiles round it hold no prop or pillar yet: it goes into a bare patch of floor and nowhere else, so what is placed last fills the empty corners and middles a room was left with, and nothing already busy. 0 = anywhere")]
        [Min(0)] public int MinEmpty;

        [Tooltip("Stands in the open half of a diagonal wall's tile (the nook behind an angled corner, the inside of a chevron), backed onto the wall and facing straight out of it, one to a half. For small props: a half is a triangle a tile across. Ignores where and which way it faces; a room with no diagonal walls gets none")]
        public bool HalfTile;

        [Header("Activity areas")]
        [Tooltip("Marks out an activity area (a lounge corner, a work corner) on bare floor: its footprint is kept for the placements Within its Key, and no other area overlaps it or touches it. It takes no floor itself, so its prefab is a rug or nothing (an unmarked area); Stretch fits a rug to it")]
        public bool Area;
        [Tooltip("Only wholly inside an area an earlier placement keyed this marked out: the sofa, table and lamp of a lounge corner on its rug. Nothing is placed where it marked none (a Required one is still placed, anywhere). Empty = anywhere")]
        public string Within;

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

        [Header("Use")]
        [Tooltip("Something idle enemies come and do at each one placed, standing at its front (+Z): operating a console, rummaging a locker, lounging on a sofa. The kind says which factions are drawn and how long they stay. Not given to a group's members or a pile. Empty = nothing")]
        public InterestKind Activity;
        [Tooltip("Only placed in a room with at least this many floor tiles (in a Breach room's side, that side's): the second pool, the roulette pit, the stage. 0 = any room")]
        [Min(0)] public int MinRoomTiles;

        // The spot for something to find and use (a resource node, an ammo cache): against a
        // wall at the far end of the room from the way in, off the routes and doorways, with
        // floor in front and away from what is placed. The reward is what the room's fight
        // stands between.
        public static RoomPropPlacement Nook(GameObject[] prefabs) => new()
        {
            Prefabs   = prefabs,
            Count     = new IntRange(1, 1),
            On        = RoomTileTags.Edge,
            Band      = RoomFlowBand.Goal,
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
        // up where (tags, zone, routes, flow band, floor level, what it stands near), which way and growing
        // with the room; level 2 also the floor in front.
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
            copy.Band      = RoomFlowBand.Any;
            copy.MinEmpty  = 0;
            copy.Floor     = PropFloor.Any;
            copy.Near      = null;
            copy.Within    = null;
            copy.Grow      = Vector2.zero;
            if (copy.Facing is PropFacing.AgainstWall or PropFacing.AcrossSightline or PropFacing.AlongRoom or PropFacing.TowardNear
                or PropFacing.AgainstDiagonal)
                copy.Facing = PropFacing.Square;
            if (level >= 2) copy.Clearance = 0;
            return copy;
        }
    }
}
