using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Raises or sinks a feature from the doorways' floor, placed so it reads as meant:
    //   - Platform: a block with its back to the room's wall, stairs on its open sides facing into
    //     the room (a stage, a loading deck, a mezzanine);
    //   - Sunken Floor: a block with floor all round it, stairs down from its rim (a lounge pit),
    //     optionally crossed by a bridge of floor level with the room, splitting it in two;
    //   - Island: a block a level up with floor all round it, stairs up its sides (a kitchen
    //     island, a raised bar); as a Bastion, walled all round and climbed from two sides;
    //   - Mound: a round rise of ground sloping down on every side, its corners filled (a hillock);
    //   - Crater: a round hollow sloping down from every side, its corners filled (a blast crater).
    // Each is one level up or down, or with Tiers several, a level per tier: a stage climbing in
    // steps, a stepped pit, a terraced hill, a crater deepening ring by ring. Each tier is a smaller
    // copy of the one it stands on, reached from it; as many tiers are built as the room's levels,
    // its ceiling and the feature's size allow (BlockElevationBuilder, RoundElevationBuilder).
    // Role (ElevationRole) says what a Platform, Island or Sunken Floor is for in play, which sets its guards
    // and its ways up (ElevationAccess): a split level or a pit is open or railed by Railed and Rail
    // Chance; a perch has Wall along its front and open sides; a bastion has Wall all round, its
    // stairs the only ways on; a stage faces the way in, centred, with symmetric stairs. Mounds and
    // craters slope on every side whatever their role.
    // A feature with no space for itself is tried smaller, down to two tiles each way, so a
    // small or crowded room still gets one.
    // None covers a doorway's front, a route from a doorway or the centre spot, so everyone
    // arrives and crosses the room on the doorways' level; every piece has a way up and down, with
    // landings clear at both ends, and every tile stays reachable both ways (RoomStructure.TryAddElevations
    // holds the room to that). Does nothing in a room whose levels don't take its kind
    // (LevelBuildSettings › Lowest and Highest Level), and then draws no random numbers, so the
    // rest of the room is unchanged; a fixed number of tiers draws none either.
    [CreateAssetMenu(fileName = "ElevationStructureRule", menuName = "CGD/Level/Structure/Elevation")]
    public class ElevationStructureRule : RoomStructureRule
    {
        // Places tried at each size before the next smaller one.
        private const int Attempts = 16;
        // How many tiles a block may be shrunk by, along its length and (half as fast) its depth.
        private const int MaxShrink = 4;

        [SerializeField] private ElevationKind _kind = ElevationKind.Platform;
        [Tooltip("Platform, Island and Sunken Floor: what it is for in play. Split Level and Pit: open or railed ledges (Railed, Rail Chance). Perch: Wall along the side facing into the room, the other sides open to climb, one or two flights. Bastion: Wall all round, two flights at least on different sides, the only ways on. Pit: two flights at least. Stage (Platform): against the wall across from the way in, centred, one wide flight up the middle or a pair at the ends")]
        [SerializeField] private ElevationRole _role = ElevationRole.SplitLevel;
        [Tooltip("Perch and Bastion: what stands along their guarded ledges. Half Wall: solid, low cover. Railing: seen and shot through")]
        [SerializeField] private LedgeGuard _wall = LedgeGuard.HalfWall;
        [Tooltip("How many to put in the room")]
        [SerializeField] private IntRange _count = new(1, 1);
        [Tooltip("Levels each one rises or sinks, a tier per level, each standing on the one before it. Capped by the levels the room allows and its ceiling, and by the feature's size: Platform and Sunken Floor tiers keep two tiles in from the tier below, Mound and Crater tiers two tiles all round")]
        [SerializeField] private IntRange _tiers = new(1, 1);
        [Tooltip("Platform, Island and Sunken Floor: tiles across (out from the wall for a platform, the short side for an island or a sunken floor), of the lowest tier")]
        [SerializeField] private IntRange _depth = new(2, 3);
        [Tooltip("Platform, Island and Sunken Floor: tiles long (along the wall for a platform, the long side for an island or a sunken floor), of the lowest tier")]
        [SerializeField] private IntRange _length = new(3, 5);
        [Tooltip("Mound and Crater: tiles from the middle to the edge of the top (a mound's) or the floor (a crater's), slopes not counted. A crater needs 2 at least")]
        [SerializeField] private IntRange _radius = new(1, 2);
        [Tooltip("Platform, Island and Sunken Floor: flights of stairs to each tier; fewer are built where there is no room for them, never none (and one at least to each half of a bridged floor). Mounds and craters slope on every side instead")]
        [SerializeField] private IntRange _stairs = new(1, 2);
        [Tooltip("Sunken Floor: a strip of floor level with the room across its middle, splitting it in two (five tiles long at least). A bridged floor has one tier")]
        [SerializeField] private bool _bridge;
        [Tooltip("Most of the room's floor these may take together, slopes and corners counted")]
        [SerializeField, Range(0.05f, 0.5f)] private float _maxShare = 0.25f;
        [Tooltip("Split Level, Pit and Stage: a railing along the open edges: nobody drops off a platform or into a sunken floor, only takes the stairs. Mounds and craters have no open edge")]
        [SerializeField] private bool _railed;
        [Tooltip("Split Level, Pit and Stage: the chance a feature that isn't always railed gets a railing anyway, so most drops are fenced and some are the open one-metre drop a real deck has. Draws a random number only when above 0")]
        [SerializeField, Range(0f, 1f)] private float _railChance;

        public ElevationKind Kind => _kind;
        public ElevationRole Role => _role;
        // Up (+1) or down (-1), and the level of the first tier.
        public int Level => _kind is ElevationKind.Platform or ElevationKind.Island or ElevationKind.Mound ? 1 : -1;

        private bool IsRound => _kind == ElevationKind.Mound || _kind == ElevationKind.Crater;

        public override void Apply(RoomStructure structure, RandomStream random)
        {
            if (!structure.AllowsFloor(Level)) return;

            var blocks = new BlockElevationBuilder(_kind, _depth, _length, _stairs, _bridge, _role, _wall);
            var rounds = new RoundElevationBuilder(_kind, _radius);
            int budget = Mathf.FloorToInt(structure.Footprint.Count * _maxShare);
            int count = _count.Evaluate(random);
            for (int i = 0; i < count; i++)
            {
                int tiers = TiersFor(structure, random);
                LedgeGuard open = OpenGuard(random);
                // A round feature's radius already starts at its smallest, so it isn't shrunk.
                for (int shrink = 0; shrink <= (IsRound ? 0 : MaxShrink); shrink++)
                    if (TryPlace(structure, random, blocks, rounds, tiers, open, shrink, ref budget)) break;
            }
        }

        private bool TryPlace(RoomStructure structure, RandomStream random, BlockElevationBuilder blocks, RoundElevationBuilder rounds,
                              int tiers, LedgeGuard open, int shrink, ref int budget)
        {
            for (int attempt = 0; attempt < Attempts; attempt++)
            {
                List<ElevatedArea> areas = IsRound ? rounds.Build(structure, random, tiers)
                                                   : blocks.Build(structure, random, tiers, open, shrink);
                if (TryAdd(structure, areas, ref budget)) return true;
            }
            return false;
        }

        // The guard on the ledges this one's role leaves open: a railing when always railed or by
        // its chance (drawn only when the chance is above 0 and the role has open ledges to rail).
        private LedgeGuard OpenGuard(RandomStream random)
        {
            if (!ElevationAccess.RollsRailing(_role)) return LedgeGuard.None;
            bool railed = _railed || (_railChance > 0f && random.Chance(_railChance));
            return railed ? LedgeGuard.Railing : LedgeGuard.None;
        }

        // How many tiers this one is to have: drawn only when the range leaves a choice, then as
        // many of those as the room's levels and ceiling allow (one at least: Apply checked it).
        private int TiersFor(RoomStructure structure, RandomStream random)
        {
            int wanted = _tiers.Max > _tiers.Min ? _tiers.Evaluate(random) : _tiers.Min;
            return Mathf.Max(1, structure.TiersAllowed(Level, Mathf.Max(1, wanted)));
        }

        private static bool TryAdd(RoomStructure structure, List<ElevatedArea> areas, ref int budget)
        {
            if (areas == null) return false;

            int size = FloorTaken(areas);
            if (size > budget || !structure.TryAddElevations(areas)) return false;
            budget -= size;
            return true;
        }

        // What the feature counts against the budget: every tier's tiles and corners and a raised
        // feature's flights (as the single-tier features always counted them, so their rolls stay
        // the same; an upper tier's flights on the tier below are counted twice, erring small).
        private static int FloorTaken(List<ElevatedArea> areas)
        {
            int size = 0;
            foreach (ElevatedArea area in areas)
                size += area.Tiles.Count + area.Corners.Count + (area.Level > 0 ? area.Stairs.Count : 0);
            return size;
        }
    }
}
