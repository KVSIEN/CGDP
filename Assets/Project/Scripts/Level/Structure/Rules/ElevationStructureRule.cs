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
    //   - Mound: a round rise of ground sloping down on every side, its corners filled (a hillock);
    //   - Crater: a round hollow sloping down from every side, its corners filled (a blast crater).
    // Each is one level up or down, or with Tiers several, a level per tier: a stage climbing in
    // steps, a stepped pit, a terraced hill, a crater deepening ring by ring. Each tier is a smaller
    // copy of the one it stands on, reached from it; as many tiers are built as the room's levels,
    // its ceiling and the feature's size allow (BlockElevationBuilder, RoundElevationBuilder).
    // None covers a doorway's front, a route from a doorway or the centre spot, so everyone
    // arrives and crosses the room on the doorways' level; every piece has a way up and down, with
    // landings clear at both ends, and every tile stays reachable both ways (RoomStructure.TryAddElevations
    // holds the room to that). Does nothing in a room whose levels don't take its kind
    // (LevelBuildSettings › Lowest and Highest Level), and then draws no random numbers, so the
    // rest of the room is unchanged; a fixed number of tiers draws none either.
    [CreateAssetMenu(fileName = "ElevationStructureRule", menuName = "CGD/Level/Structure/Elevation")]
    public class ElevationStructureRule : RoomStructureRule
    {
        private const int Attempts = 16;

        [SerializeField] private ElevationKind _kind = ElevationKind.Platform;
        [Tooltip("How many to put in the room")]
        [SerializeField] private IntRange _count = new(1, 1);
        [Tooltip("Levels each one rises or sinks, a tier per level, each standing on the one before it. Capped by the levels the room allows and its ceiling, and by the feature's size: Platform and Sunken Floor tiers keep two tiles in from the tier below, Mound and Crater tiers two tiles all round")]
        [SerializeField] private IntRange _tiers = new(1, 1);
        [Tooltip("Platform and Sunken Floor: tiles across (out from the wall for a platform, the short side for a sunken floor), of the lowest tier")]
        [SerializeField] private IntRange _depth = new(2, 3);
        [Tooltip("Platform and Sunken Floor: tiles long (along the wall for a platform, the long side for a sunken floor), of the lowest tier")]
        [SerializeField] private IntRange _length = new(3, 5);
        [Tooltip("Mound and Crater: tiles from the middle to the edge of the top (a mound's) or the floor (a crater's), slopes not counted. A crater needs 2 at least")]
        [SerializeField] private IntRange _radius = new(1, 2);
        [Tooltip("Platform and Sunken Floor: flights of stairs to each tier; fewer are built where there is no room for them, never none (and one at least to each half of a bridged floor). Mounds and craters slope on every side instead")]
        [SerializeField] private IntRange _stairs = new(1, 2);
        [Tooltip("Sunken Floor: a strip of floor level with the room across its middle, splitting it in two (five tiles long at least). A bridged floor has one tier")]
        [SerializeField] private bool _bridge;
        [Tooltip("Most of the room's floor these may take together, slopes and corners counted")]
        [SerializeField, Range(0.05f, 0.5f)] private float _maxShare = 0.25f;
        [Tooltip("A railing along the open edges: nobody drops off a platform or into a sunken floor, only takes the stairs. Mounds and craters have no open edge")]
        [SerializeField] private bool _railed;

        public ElevationKind Kind => _kind;
        // Up (+1) or down (-1), and the level of the first tier.
        public int Level => _kind == ElevationKind.Platform || _kind == ElevationKind.Mound ? 1 : -1;

        private bool IsRound => _kind == ElevationKind.Mound || _kind == ElevationKind.Crater;

        public override void Apply(RoomStructure structure, RandomStream random)
        {
            if (!structure.AllowsLevel(Level)) return;

            var blocks = new BlockElevationBuilder(_kind, _depth, _length, _stairs, _bridge, _railed);
            var rounds = new RoundElevationBuilder(_kind, _radius);
            int budget = Mathf.FloorToInt(structure.Footprint.Count * _maxShare);
            int count = _count.Evaluate(random);
            for (int i = 0; i < count; i++)
            {
                int tiers = TiersFor(structure, random);
                for (int attempt = 0; attempt < Attempts; attempt++)
                {
                    List<ElevatedArea> areas = IsRound ? rounds.Build(structure, random, tiers) : blocks.Build(structure, random, tiers);
                    if (TryAdd(structure, areas, ref budget)) break;
                }
            }
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
