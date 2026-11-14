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

        public RoomSample(LevelLayout layout, LevelRoom room, RoomFurnishing furnishing, LevelBuildSettings settings,
                          string seedText, int doorsWanted, List<string> warnings)
        {
            Layout      = layout;
            Room        = room;
            Furnishing  = furnishing;
            Settings    = settings;
            SeedText    = seedText;
            DoorsWanted = doorsWanted;
            Warnings    = warnings;
            Sight       = new SightlineMap(room.Structure);
            Traits      = RoomTraitMeasure.Measure(room.Structure, furnishing.Props);
            // A Breach room is built by its first faction's design.
            Design      = settings.DesignFor(room.Faction);
            Issues      = RoomSampleChecks.Run(this);
        }

        public string             Message     { get; }
        public LevelLayout        Layout      { get; }
        public LevelRoom          Room        { get; }
        public RoomFurnishing     Furnishing  { get; }
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
