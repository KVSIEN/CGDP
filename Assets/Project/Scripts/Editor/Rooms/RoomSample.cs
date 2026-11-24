using System.Collections.Generic;
using CGD.Level;
using UnityEngine;

namespace CGD.Editor
{
    // One room as the level would build it — laid out by LevelLayoutBuilder and furnished by
    // RoomFurnisher from the same seeds LevelBuilder uses — plus the checks run on it.
    // `Room` is null when nothing could be built (Message says why).
    public sealed class RoomSample
    {
        public RoomSample(string message)
        {
            Message  = message;
            Warnings = new List<string>();
            Issues   = new List<RoomIssue>();
        }

        public RoomSample(LevelLayout layout, LevelRoom room, RoomFurnishing furnishing, RoomEnemyPlan enemies,
                          LevelBuildSettings settings, string seedText, int doorsWanted, List<string> warnings)
        {
            Enemies     = enemies;
            Posts       = new RoomPostMap(room.Structure, furnishing.Props, settings.UsesIsovist(IsovistUse.Posts));
            Layout      = layout;
            Room        = room;
            Furnishing  = furnishing;
            Settings    = settings;
            SeedText    = seedText;
            DoorsWanted = doorsWanted;
            Warnings    = warnings;
            Sight       = new SightlineMap(room.Structure);
            Traits      = RoomTraitMeasure.Measure(room.Structure, furnishing.Props, settings.UsesIsovist(IsovistUse.Traits));
            // A Breach room is built by its holder's design; its zones are dressed by both factions.
            Design      = settings.DesignFor(room.Faction);
            Split       = room.BreachFaction != null ? BreachSplit.Of(room.Structure, room.BreachEntrances) : null;
            Issues      = RoomSampleChecks.Run(this);
        }

        public string             Message     { get; }
        public LevelLayout        Layout      { get; }
        public LevelRoom          Room        { get; }
        // Breach rooms split into zones: which side each zone is (null when not split).
        public BreachSplit        Split       { get; }
        public RoomFurnishing     Furnishing  { get; }
        // Where the room's enemies start and patrol, as the level would place them, and what
        // each tile offers them.
        public RoomEnemyPlan      Enemies     { get; }
        public RoomPostMap        Posts       { get; }
        public LevelBuildSettings Settings    { get; }
        public string             SeedText    { get; }
        // Doorways the recipe asked for (-1 for a map room: its connections decide).
        public int                DoorsWanted { get; }
        // Layout warnings about this room.
        public List<string>       Warnings    { get; }
        // Open floor along each row and column with the props in place.
        internal SightlineMap     Sight       { get; }
        // How much concealment, walls and lines of fire the room offers.
        public RoomTraitReading   Traits      { get; }
        // The holding faction's design (its priorities), or null.
        public FactionDesign      Design      { get; }
        public List<RoomIssue>    Issues      { get; }

        public bool IsValid => Room != null;

        // The room's tiles plus a margin, for framing the plan.
        public RectInt Frame(int margin)
        {
            RectInt bounds = Room.Footprint.Bounds;
            return new RectInt(bounds.xMin - margin, bounds.yMin - margin, bounds.width + margin * 2, bounds.height + margin * 2);
        }

        public int DoorwayCount
        {
            get
            {
                int count = 0;
                foreach (LevelDoorway doorway in Layout.Doorways)
                    if (doorway.Room == Room) count++;
                return count;
            }
        }
    }
}
