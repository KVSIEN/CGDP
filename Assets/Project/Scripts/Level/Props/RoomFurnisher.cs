using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Plans what a room is furnished with, most important first, as each placement takes floor
    // the next can't have: what a player comes for (offerings), the function's furniture (its
    // layout's set pieces first), then
    // its side's dressing (a crew room's staff door), the room type's cover and clutter (and its
    // extra dressing), then the faction's own pieces — in a Breach room split into zones, each
    // side by its own faction. The side's design scales the faction's
    // fill share and prop counts.
    // A hand-built landmark keeps its own interior. A fight room a faction design holds is
    // furnished the way that design's priorities ask (RoomTraitFit).
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

            // Each try furnishes its own copy of the room; the best one's floor becomes the room's,
            // so the winner is never furnished twice.
            RoomStructure bestFloor = null;
            RoomPropPlanner best = null;
            int bestAttempt = 0, bestMiss = int.MaxValue;
            for (int attempt = 0; attempt < tries && bestMiss > 0; attempt++)
            {
                RoomStructure trial = room.Structure.Copy();
                RoomPropPlanner planner = Plan(room, trial, rule, style, swaps, RoomTraitFit.Try(seed, attempt));
                int miss = RoomTraitFit.Miss(planner.Traits, style.Design.Priorities);
                if (miss >= bestMiss) continue;
                bestFloor   = trial;
                best        = planner;
                bestAttempt = attempt;
                bestMiss    = miss;
            }

            room.Structure.TakeFloorFrom(bestFloor);
            return new RoomFurnishing(best.Planned, best.Outcomes, bestAttempt, tries);
        }

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
            if (rule != null && rule.Offerings.Length > 0) planner.Add(RoomPropPlacement.Nook(rule.Offerings));
            if (room.Landmark == null)
            {
                planner.Add(room.FunctionProps);
                if (side != null && side.Dressing != null) planner.Add(side.Dressing.Props);
                AddRoomTypeDressing(planner, room, structure, rule, style, swaps);
            }
            return planner;
        }

        // The room type's cover and clutter, then the factions' own props. A Breach room split
        // into zones (BreachSplit) has each side dressed by its own faction: the cover in that
        // faction's pieces, then its props. Otherwise the holder's version covers the room and
        // both factions' props go anywhere. The holder's props come in at its strength; the
        // breaching faction's always at full.
        private void AddRoomTypeDressing(RoomPropPlanner planner, LevelRoom room, RoomStructure structure, RoomContentRule rule,
                                         FactionStyle style, bool swaps)
        {
            FactionDesign design = style.Design;
            FactionDesign breachDesign = room.BreachFaction != null ? _settings.DesignFor(room.BreachFaction) : null;
            BreachSplit split = room.BreachFaction != null ? BreachSplit.Of(structure, room.BreachEntrances) : null;
            ICollection<int> holderZones = split?.Zones(breachSide: false);
            ICollection<int> breachZones = split?.Zones(breachSide: true);

            if (rule != null && rule.Dressing != null)
            {
                planner.Add(Dressed(swaps ? design : null, rule.Dressing).Props, zones: holderZones);
                if (split != null) planner.Add(Dressed(breachDesign, rule.Dressing).Props, zones: breachZones);
            }
            if (rule != null && rule.ExtraDressing != null) planner.Add(rule.ExtraDressing.Props, exactConcealment: true);

            AddFactionProps(planner, design, holderZones, style.Strength);
            if (room.BreachFaction != null) AddFactionProps(planner, breachDesign, breachZones, 1f);
        }

        private static RoomDressing Dressed(FactionDesign design, RoomDressing dressing) => design != null ? design.Dress(dressing) : dressing;

        // A faction's own props: its furnishing of the room, after everything the room needs.
        private static void AddFactionProps(RoomPropPlanner planner, FactionDesign design, ICollection<int> zones, float countScale)
        {
            if (design != null && design.Dressing != null) planner.Add(design.Dressing.Props, zones: zones, countScale: countScale);
        }
    }
}
