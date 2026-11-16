using UnityEngine;
using CGD.Core;
using CGD.Factions;

namespace CGD.Level
{
    // Plans what a room is furnished with, most important first, as each placement takes floor
    // the next can't have: what a player comes for (offerings), the function's furniture, then
    // its side's dressing (a crew room's staff door), the room type's cover and clutter (and its
    // extra dressing), then the faction's own pieces. The side's design scales the faction's
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
            FactionDesign design = _settings.DesignFor(room.Faction);
            Seed seed = _seed.Derive("props").Derive(room.Node.Id);
            int tries = RoomTraitFit.TriesFor(room, rule, design, _settings);
            int attempt = tries > 1 ? BestAttempt(room, rule, design, seed, tries) : 0;

            RoomPropPlanner planner = Plan(room, room.Structure, rule, design, RoomTraitFit.Try(seed, attempt));
            return new RoomFurnishing(planner.Planned, planner.Outcomes, attempt, tries);
        }

        // Furnishes copies of the room from each try's seed and returns the try whose traits
        // miss the faction's priorities least (the earliest, of equals).
        private int BestAttempt(LevelRoom room, RoomContentRule rule, FactionDesign design, Seed seed, int tries)
        {
            int best = 0, bestMiss = int.MaxValue;
            for (int attempt = 0; attempt < tries && bestMiss > 0; attempt++)
            {
                RoomStructure trial = room.Structure.Copy();
                RoomPropPlanner planner = Plan(room, trial, rule, design, RoomTraitFit.Try(seed, attempt));
                int miss = RoomTraitFit.Miss(RoomTraitMeasure.Measure(trial, planner.Planned), design.Priorities);
                if (miss >= bestMiss) continue;
                best     = attempt;
                bestMiss = miss;
            }
            return best;
        }

        private RoomPropPlanner Plan(LevelRoom room, RoomStructure structure, RoomContentRule rule, FactionDesign design, Seed seed)
        {
            float concealment = RoomTraitFit.Steers(rule, design)
                ? RoomTraitFit.ConcealmentScale(design.Priorities.Of(RoomTrait.Concealment)) : 1f;
            ShipSideDesign side = _settings.DesignFor(room.Side);
            float fillShare = (design != null ? design.FillShare : RoomPropPlanner.DefaultFillShare) * (side != null ? side.FillShareScale : 1f);
            float propScale = (design != null ? design.PropScale : 1f) * (side != null ? side.PropScale : 1f);
            var planner = new RoomPropPlanner(structure, room.Anchor, seed.Stream(), Mathf.Min(fillShare, MaxFillShare), propScale, concealment);
            if (rule != null && rule.Offerings.Length > 0) planner.Add(RoomPropPlacement.Nook(rule.Offerings));
            if (room.Landmark == null)
            {
                if (room.Function != null) planner.Add(room.Function.Props);
                if (side != null && side.Dressing != null) planner.Add(side.Dressing.Props);
                if (rule != null && rule.Dressing != null) planner.Add((design != null ? design.Dress(rule.Dressing) : rule.Dressing).Props);
                if (rule != null && rule.ExtraDressing != null) planner.Add(rule.ExtraDressing.Props, exactConcealment: true);
                AddFactionProps(planner, room.Faction);
                if (room.BreachFaction != null) AddFactionProps(planner, room.BreachFaction);
            }
            return planner;
        }

        // A faction's own props: its furnishing of the room, after everything the room needs.
        private void AddFactionProps(RoomPropPlanner planner, FactionDefinition faction)
        {
            FactionDesign design = _settings.DesignFor(faction);
            if (design != null && design.Dressing != null) planner.Add(design.Dressing.Props);
        }
    }
}
