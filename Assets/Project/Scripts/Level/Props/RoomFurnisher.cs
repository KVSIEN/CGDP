using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Plans what a room is furnished with, most important first, as each placement takes floor
    // the next can't have: what a player comes for (offerings), the function's furniture (its
    // layout's set pieces first), then
    // its side's dressing (a crew room's staff door), a fight room's foothold just inside the way
    // in, the room type's cover and clutter (and its
    // extra dressing), then the faction's own pieces — in a Breach room split into zones, each
    // side by its own faction. The side's design scales the faction's
    // fill share and prop counts. A pavilion standing in the room (an annex: a restroom hut, a
    // kiosk) is furnished by its own place alone; everything else goes on the floor round it.
    // A hand-built landmark keeps its own interior. A fight room a faction design holds is
    // furnished the way that design's priorities ask (RoomTraitFit): from several seeds, each
    // refined by moving its loose cover about (RoomPlanRefiner), keeping the closest fit.
    //
    // Pure planning — nothing is spawned — so the level (RoomPopulator) and the editor's room
    // previews get exactly the same props from the same seed. Planning claims the room's floor
    // (RoomTileTags.Prop), so each room is furnished once.
    public class RoomFurnisher
    {
        // However crowded a faction and a side make a room, props leave most of it walkable.
        private const float MaxFillShare = 0.8f;

        private readonly LevelBuildSettings _settings;
        private readonly Seed _seed;

        // `seed`: the level's contents seed; each room furnishes from its own layer of it, so
        // what one room gets doesn't depend on the others.
        public RoomFurnisher(LevelBuildSettings settings, Seed seed)
        {
            _settings = settings;
            _seed     = seed;
        }

        public RoomFurnishing Furnish(LevelRoom room)
        {
            if (room.Structure == null) return RoomFurnishing.Empty;

            RoomContentRule rule = _settings.RuleFor(room.Node.Type);
            FactionStyle style = _settings.StyleFor(room);
            Seed seed = _seed.Derive("props").Derive(room.Node.Id);
            // Decided once for the room, so every try dresses it the same way; a core room draws nothing.
            bool swaps = style.IsFull || style.Shows(seed.Derive("territory").Stream());
            int tries = RoomTraitFit.TriesFor(room, rule, style.Design, _settings);
            if (tries <= 1)
            {
                RoomPropPlanner only = Plan(room, room.Structure, rule, style, swaps, seed);
                return new RoomFurnishing(only.Planned, only.Outcomes);
            }

            // Each try furnishes its own copy of the room, then moves its loose cover about for as
            // long as that brings it closer; the best one's floor becomes the room's, so the
            // winner is never furnished twice. A try that fits straight away is left as it is.
            RoomStructure bestFloor = null;
            RoomPropPlanner best = null;
            int bestAttempt = 0, bestMoves = 0;
            float bestCost = float.MaxValue;
            for (int attempt = 0; attempt < tries && bestCost > 0f; attempt++)
            {
                Seed attemptSeed = RoomTraitFit.Try(seed, attempt);
                RoomStructure trial = room.Structure.Copy();
                RoomPropPlanner planner = Plan(room, trial, rule, style, swaps, attemptSeed);
                RefineResult refined = RoomPlanRefiner.Refine(planner, () => Cost(planner, trial, style.Design),
                                                              RoomTraitFit.Refinement(attemptSeed), _settings.RefineSteps);
                if (refined.Cost >= bestCost) continue;
                bestFloor   = trial;
                best        = planner;
                bestAttempt = attempt;
                bestMoves   = refined.Moves;
                bestCost    = refined.Cost;
            }

            room.Structure.TakeFloorFrom(bestFloor);
            return new RoomFurnishing(best.Planned, best.Outcomes, bestAttempt, tries, bestMoves);
        }

        // How far a furnished room is from its faction's priorities (0 = fits). Furniture can
        // close a flank the structure left open, so a lost flank counts as one level off.
        private static float Cost(RoomPropPlanner planner, RoomStructure furnished, FactionDesign design) =>
            RoomTraitFit.Gap(planner.Traits, design.Priorities) + RoomFlank.Miss(furnished);

        // `style`: the holder's design at the room's strength. `swaps`: whether the room type's
        // cover comes in the holder's own pieces.
        private RoomPropPlanner Plan(LevelRoom room, RoomStructure structure, RoomContentRule rule, FactionStyle style, bool swaps, Seed seed)
        {
            float concealment = RoomTraitFit.Steers(rule, style.Design)
                ? style.ConcealmentScale(style.Design.Priorities.Of(RoomTrait.Concealment)) : 1f;
            ShipSideDesign side = _settings.DesignFor(room.Side);
            float fillShare = style.FillShare * (side != null ? side.FillShareScale : 1f);
            float propScale = style.PropScale * (side != null ? side.PropScale : 1f);
            var planner = new RoomPropPlanner(structure, room.Anchor, seed.Stream(), Mathf.Min(fillShare, MaxFillShare), propScale, concealment);
            // Huts and kiosks standing in the room hold only their own furniture.
            ICollection<int> floor = structure.ZonesOutsideAnnexes();
            if (rule != null && rule.Offerings.Length > 0) planner.Add(RoomPropPlacement.Nook(rule.Offerings), zones: floor);
            if (room.Landmark == null)
            {
                // A building's or a town's rooms are furnished by their own places; the place's own
                // props would land in a hall or a street. Round annexes the room is still its place.
                if (structure.Spaces.Count == 0 || floor != null) planner.Add(room.FunctionProps, zones: floor);
                AddSpaces(planner, structure);
                if (side != null && side.Dressing != null) planner.Add(side.Dressing.Props, zones: floor);
                AddFoothold(planner, room, structure, rule, style, swaps, floor);
                AddRoomTypeDressing(planner, room, structure, rule, style, swaps, floor);
            }
            return planner;
        }

        // The rooms of a building and the streets round it each get their own place's furniture,
        // in their own zone only: a hospital's ward gets beds, its storeroom shelves. A room with
        // a layout gets that arrangement's set pieces too (a surgery's tables).
        private static void AddSpaces(RoomPropPlanner planner, RoomStructure structure)
        {
            foreach (StructureSpace space in structure.Spaces)
            {
                if (space.Function == null) continue;

                var zone = new[] { structure.ZoneOf(space.Tile) };
                foreach (RoomPropPlacement placement in space.Function.PropsWith(space.Layout))
                    planner.Add(placement?.ForSingleZone(), zones: zone);
            }
        }

        // A fight room's threshold gets a piece of cover before the room type's cover takes the
        // floor, so the way in is a foothold to read the room from, not open ground. It comes in
        // the pieces of whoever holds that side: in a Breach room split into zones whose way in
        // is on the breach side, the breaching faction's, as the rest of that side's cover is.
        private void AddFoothold(RoomPropPlanner planner, LevelRoom room, RoomStructure structure, RoomContentRule rule,
                                 FactionStyle style, bool swaps, ICollection<int> floor)
        {
            RoomDressing foothold = _settings.FootholdDressing;
            if (foothold == null || !structure.Arrival.HasValue || !RoomTraitFit.IsFightRoom(rule)) return;

            BreachSplit split = room.BreachFaction != null ? BreachSplit.Of(structure, room.BreachEntrances) : null;
            if (split != null && split.IsBreachZone(structure.ZoneOf(structure.Arrival.Value)))
            {
                planner.Add(Dressed(_settings.DesignFor(room.BreachFaction), foothold).Props, zones: Within(split.Zones(breachSide: true), floor));
                return;
            }
            planner.Add(Dressed(swaps ? style.Design : null, foothold).Props, zones: Within(split?.Zones(breachSide: false), floor));
        }

        // The room type's cover and clutter, then the factions' own props. A Breach room split
        // into zones (BreachSplit) has each side dressed by its own faction: the cover in that
        // faction's pieces, then its props. Otherwise the holder's version covers the room and
        // both factions' props go anywhere. The holder's props come in at its strength; the
        // breaching faction's always at full. `floor`: the zones outside annexes (null = all).
        private void AddRoomTypeDressing(RoomPropPlanner planner, LevelRoom room, RoomStructure structure, RoomContentRule rule,
                                         FactionStyle style, bool swaps, ICollection<int> floor)
        {
            FactionDesign design = style.Design;
            FactionDesign breachDesign = room.BreachFaction != null ? _settings.DesignFor(room.BreachFaction) : null;
            BreachSplit split = room.BreachFaction != null ? BreachSplit.Of(structure, room.BreachEntrances) : null;
            ICollection<int> holderZones = Within(split?.Zones(breachSide: false), floor);
            ICollection<int> breachZones = Within(split?.Zones(breachSide: true), floor);

            if (rule != null && rule.Dressing != null)
            {
                planner.Add(Dressed(swaps ? design : null, rule.Dressing).Props, zones: holderZones, refinable: true);
                if (split != null) planner.Add(Dressed(breachDesign, rule.Dressing).Props, zones: breachZones, refinable: true);
            }
            if (rule != null && rule.ExtraDressing != null) planner.Add(rule.ExtraDressing.Props, exactConcealment: true, zones: floor);

            AddFactionProps(planner, design, holderZones, style.Strength);
            if (room.BreachFaction != null) AddFactionProps(planner, breachDesign, breachZones, 1f);
        }

        // The zones in both sets, where null means every zone.
        private static ICollection<int> Within(ICollection<int> zones, ICollection<int> floor)
        {
            if (zones == null) return floor;
            if (floor == null) return zones;

            var both = new HashSet<int>();
            foreach (int zone in zones)
                if (floor.Contains(zone)) both.Add(zone);
            return both;
        }

        private static RoomDressing Dressed(FactionDesign design, RoomDressing dressing) => design != null ? design.Dress(dressing) : dressing;

        // A faction's own props: its furnishing of the room, after everything the room needs.
        private static void AddFactionProps(RoomPropPlanner planner, FactionDesign design, ICollection<int> zones, float countScale)
        {
            if (design != null && design.Dressing != null) planner.Add(design.Dressing.Props, zones: zones, countScale: countScale, refinable: true);
        }
    }
}
