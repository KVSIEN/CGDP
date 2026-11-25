using System;
using System.Collections.Generic;
using CGD.Core;
using CGD.Map;
using UnityEngine;

namespace CGD.Level
{
    // What the room generator is set to build, as set in its Inspector: each part is either
    // pinned or left to the seed (Any), so one seed always makes the same room from the same
    // choices. Places and layouts are kept by asset name so the Inspector can list what the
    // build settings offer.
    [Serializable]
    public sealed class RoomChoice
    {
        // Faction and section choices: roll one by the seed, none at all, or (factions only) two
        // that share the room. Anything from 0 up is an index into the content's list.
        public const int Any = -1;
        public const int None = -2;
        public const int Mixed = -3;

        [Tooltip("A number or any text. Empty = a new room every play")]
        public string SeedText = string.Empty;
        public bool AnyType = true;
        public MapNodeType Type = MapNodeType.Combat;
        public RoomCategory Category = RoomCategory.None;
        public string Place = string.Empty;
        public string Layout = string.Empty;
        public int Faction = Any;
        // Who shares the room with the holder: used when Faction is Mixed and by Breach and Rift rooms.
        public int SecondFaction = Any;
        public int Section = Any;
        // 0 = rolled by the seed.
        public int Tier;
        public float Influence = 1f;
        public float Intensity = 0.5f;
        // The floor levels the room's rules may raise or sink it to; 0 is the doorways' floor.
        public int LowestLevel = RoomElevation.Lowest;
        public int HighestLevel = RoomElevation.Highest;
        public bool DoorNorth = true, DoorEast = true, DoorSouth = true, DoorWest;
        public RoomOvergrowth Overgrowth = RoomOvergrowth.Faction;
        // Grown when Overgrowth is Profile.
        public OvergrowthProfile OvergrowthProfile;

        // Types an Any roll may land on: rooms that play as themselves on their own. Start and
        // Exit are structure, Boss needs its arena and Junction has no encounter.
        public static readonly MapNodeType[] RollableTypes =
        {
            MapNodeType.Combat, MapNodeType.Puzzle, MapNodeType.Shop, MapNodeType.Event, MapNodeType.Treasure,
            MapNodeType.Resupply, MapNodeType.Breach, MapNodeType.Lockdown, MapNodeType.Holdout, MapNodeType.Ambush,
            MapNodeType.Stealth, MapNodeType.Rift, MapNodeType.Gamble, MapNodeType.EmergencyExit, MapNodeType.Hazard,
            MapNodeType.Quiet, MapNodeType.Spawner, MapNodeType.Workshop, MapNodeType.Shrine, MapNodeType.Siege,
            MapNodeType.Gauntlet, MapNodeType.Haunt,
        };

        // Types that can be pinned: those plus the Boss and Junction (Start, Exit and the legacy
        // Elite are no rooms to look at).
        public static readonly MapNodeType[] ChoosableTypes = new List<MapNodeType>(RollableTypes)
        {
            MapNodeType.Boss, MapNodeType.Junction,
        }.ToArray();

        // Whether a second faction is chosen for the room: always when Mixed, and for the room
        // types that are held by two at once.
        public bool HasSecondFaction => Faction == Mixed || (!AnyType && Type.HasSecondFaction());

        // The seed to build with: the one typed, or a fresh, short one when the field is empty.
        public Seed ResolveSeed() => Seed.Parse(string.IsNullOrWhiteSpace(SeedText) ? NewSeedText() : SeedText);

        // Every roll is drawn whether or not it is used, so pinning one choice never changes what
        // the others roll to.
        public RoomRecipe ToRecipe(Seed seed, LevelBuildSettings settings, MapContentSettings content)
        {
            RandomStream random = seed.Derive("room choice").Stream();
            MapNodeType rolledType = random.Pick(RollableTypes);
            int rolledFaction = random.Range(0, 1 << 16);
            int rolledSecond = random.Range(0, 1 << 16);
            int rolledSection = random.Range(0, 1 << 16);
            int rolledTier = random.Range(1, 4);

            int factions = content != null ? content.Factions.Count : 0;
            int sections = content != null ? content.Sections.Count : 0;
            RoomFunction place = FindPlace(settings);
            MapNodeType type = AnyType ? rolledType : Type;

            var recipe = new RoomRecipe
            {
                Type         = type,
                Place        = place,
                Layout       = FindLayout(place),
                Category     = place != null ? place.Category : Category,
                Section      = Pick(Section, rolledSection, sections, MapNode.NoSection),
                Tier         = Tier > 0 ? Mathf.Clamp(Tier, 1, 3) : rolledTier,
                Influence    = Influence,
                Intensity    = Intensity,
                LowestLevel  = Mathf.Clamp(LowestLevel, RoomElevation.Lowest, 0),
                HighestLevel = Mathf.Clamp(HighestLevel, 0, RoomElevation.Highest),
                DoorNorth    = DoorNorth,
                DoorEast     = DoorEast,
                DoorSouth    = DoorSouth,
                DoorWest     = DoorWest,
            };
            PickFactions(recipe, type, rolledFaction, rolledSecond, factions);
            return recipe;
        }

        // Where the room's overgrowth comes from. Rolls nothing, so it never changes the room's other choices.
        public OvergrowthSource ToOvergrowthSource(LevelBuildSettings settings) => Overgrowth switch
        {
            RoomOvergrowth.None    => OvergrowthSource.Fixed(null),
            RoomOvergrowth.Profile => OvergrowthSource.Fixed(OvergrowthProfile),
            _                      => OvergrowthSource.FromFactions(settings),
        };

        // Everything rolled by the seed, as broad as the build allows, with a new seed every play.
        public void RandomizeAll()
        {
            SeedText = string.Empty;
            AnyType = true;
            Category = RoomCategory.None;
            Place = Layout = string.Empty;
            Faction = SecondFaction = Section = Any;
            Tier = 0;
            LowestLevel = RoomElevation.Lowest;
            HighestLevel = RoomElevation.Highest;
            Overgrowth = RoomOvergrowth.Faction;
        }

        // Short enough to read and retype.
        public static string NewSeedText() => (Seed.Random().Value % 100000000UL).ToString();

        public RoomFunction FindPlace(LevelBuildSettings settings)
        {
            if (string.IsNullOrEmpty(Place) || settings == null) return null;
            foreach (RoomFunction function in settings.Functions)
                if (function != null && function.name == Place) return function;
            return null;
        }

        private RoomLayout FindLayout(RoomFunction place)
        {
            if (place == null || string.IsNullOrEmpty(Layout)) return null;
            foreach (RoomLayout layout in place.Layouts)
                if (layout != null && layout.name == Layout) return layout;
            return null;
        }

        // The holder, and the faction sharing the room when it has one: never the same faction twice.
        private void PickFactions(RoomRecipe recipe, MapNodeType type, int rolledFaction, int rolledSecond, int factions)
        {
            recipe.Faction = recipe.BreachFaction = MapNode.NoFaction;
            if (factions == 0) return;

            bool second = Faction == Mixed || type.HasSecondFaction();
            int pinnedSecond = second && IsIndex(SecondFaction, factions) ? SecondFaction : MapNode.NoFaction;

            if (Faction == Mixed)        recipe.Faction = PickOther(pinnedSecond, rolledFaction, factions);
            else if (Faction != None)    recipe.Faction = IsIndex(Faction, factions) ? Faction : rolledFaction % factions;
            if (!second || (recipe.Faction >= 0 && factions < 2)) return;

            recipe.BreachFaction = pinnedSecond != MapNode.NoFaction && pinnedSecond != recipe.Faction
                ? pinnedSecond
                : PickOther(recipe.Faction, rolledSecond, factions);
        }

        private static bool IsIndex(int choice, int count) => choice >= 0 && choice < count;

        private static int Pick(int choice, int rolled, int count, int none)
        {
            if (count == 0 || choice == None) return none;
            return IsIndex(choice, count) ? choice : rolled % count;
        }

        // Any faction but `other` (any at all when `other` is none, or the only one there is).
        private static int PickOther(int other, int rolled, int count)
        {
            if (count == 1) return 0;
            int others = other >= 0 ? count - 1 : count;
            int pick = rolled % others;
            return other >= 0 && pick >= other ? pick + 1 : pick;
        }
    }
}
