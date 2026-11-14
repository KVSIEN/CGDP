using CGD.Core;
using CGD.Factions;

namespace CGD.Level
{
    // Plans what a room is furnished with, most important first, as each placement takes floor
    // the next can't have: what a player comes for (offerings), the function's furniture, then
    // the room type's cover and clutter (and its extra dressing), then the faction's own pieces.
    // A hand-built landmark keeps its own interior.
    //
    // Pure planning — nothing is spawned — so the level (RoomPopulator) and the editor's room
    // previews get exactly the same props from the same seed. Planning claims the room's floor
    // (RoomTileTags.Prop), so each room is furnished once.
    public class RoomFurnisher
    {
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
            var planner = new RoomPropPlanner(room.Structure, room.Anchor, _seed.Derive("props").Derive(room.Node.Id).Stream(),
                                              design != null ? design.FillShare : RoomPropPlanner.DefaultFillShare,
                                              design != null ? design.PropScale : 1f);
            if (rule != null && rule.Offerings.Length > 0) planner.Add(RoomPropPlacement.Nook(rule.Offerings));
            if (room.Landmark == null)
            {
                if (room.Function != null) planner.Add(room.Function.Props);
                if (rule != null && rule.Dressing != null) planner.Add((design != null ? design.Dress(rule.Dressing) : rule.Dressing).Props);
                if (rule != null && rule.ExtraDressing != null) planner.Add(rule.ExtraDressing.Props);
                AddFactionProps(planner, room.Faction);
                if (room.BreachFaction != null) AddFactionProps(planner, room.BreachFaction);
            }
            return new RoomFurnishing(planner.Planned, planner.Outcomes);
        }

        // A faction's own props: its furnishing of the room, after everything the room needs.
        private void AddFactionProps(RoomPropPlanner planner, FactionDefinition faction)
        {
            FactionDesign design = _settings.DesignFor(faction);
            if (design != null && design.Dressing != null) planner.Add(design.Dressing.Props);
        }
    }
}
