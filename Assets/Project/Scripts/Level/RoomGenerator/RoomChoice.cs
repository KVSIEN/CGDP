using System;
using System.Collections.Generic;
using CGD.Core;
using CGD.Map;
using UnityEngine;

namespace CGD.Level
{
    // What the room generator's panel has set: each choice is either pinned or left to the seed
    // (Random / Any), so one seed always makes the same room from the same choices. Kept by name
    // and index rather than by asset so it can be saved as JSON between scene loads.
    [Serializable]
    public sealed class RoomChoice
    {
        // Faction and section choices: roll one by the seed, or none at all. Anything from 0 up
        // is an index into the content's list.
        public const int Random = -1;
        public const int None = -2;

        public string SeedText = "1";
        public MapNodeType Type = MapNodeType.Combat;
        public bool RandomType = true;
        public RoomCategory Category = RoomCategory.None;
        public string Place = string.Empty;
        public string Layout = string.Empty;
        public int Faction = Random;
        public int BreachFaction = Random;
        public int Section = Random;
        // 0 = rolled by the seed.
        public int Tier;
        public float Influence = 1f;
        public float Intensity = 0.5f;
        public bool DoorNorth = true, DoorEast = true, DoorSouth = true, DoorWest;

        // Types a Random roll may land on: rooms that play as themselves on their own. Start and
        // Exit are structure, Boss needs its arena and Junction has no encounter.
        public static readonly MapNodeType[] RollableTypes =
        {
            MapNodeType.Combat, MapNodeType.Puzzle, MapNodeType.Shop, MapNodeType.Event, MapNodeType.Treasure,
            MapNodeType.Resupply, MapNodeType.Breach, MapNodeType.Lockdown, MapNodeType.Holdout, MapNodeType.Ambush,
            MapNodeType.Stealth, MapNodeType.Rift, MapNodeType.Gamble, MapNodeType.EmergencyExit, MapNodeType.Hazard,
            MapNodeType.Quiet, MapNodeType.Spawner, MapNodeType.Workshop, MapNodeType.Shrine, MapNodeType.Siege,
            MapNodeType.Gauntlet, MapNodeType.Haunt,
        };

        // Types the panel lists: those plus the Boss and Junction (Start, Exit and the legacy
        // Elite are no rooms to look at).
        public static readonly MapNodeType[] ChoosableTypes = new List<MapNodeType>(RollableTypes)
        {
            MapNodeType.Boss, MapNodeType.Junction,
        }.ToArray();

        // Every roll is drawn whether or not it is used, so pinning one choice never changes what
        // the others roll to.
        public RoomRecipe ToRecipe(LevelBuildSettings settings, MapContentSettings content)
        {
            RandomStream random = Seed.Parse(SeedText).Derive("room choice").Stream();
            MapNodeType rolledType = random.Pick(RollableTypes);
            int rolledFaction = random.Range(0, 1 << 16);
            int rolledBreach = random.Range(0, 1 << 16);
            int rolledSection = random.Range(0, 1 << 16);
            int rolledTier = random.Range(1, 4);

            int factions = content != null ? content.Factions.Count : 0;
            int sections = content != null ? content.Sections.Count : 0;
            RoomFunction place = FindPlace(settings);

            var recipe = new RoomRecipe
            {
                Type       = RandomType ? rolledType : Type,
                Place      = place,
                Layout     = FindLayout(place),
                Category   = place != null ? place.Category : Category,
                Faction    = Pick(Faction, rolledFaction, factions, MapNode.NoFaction),
                Section    = Pick(Section, rolledSection, sections, MapNode.NoSection),
                Tier       = Tier > 0 ? Mathf.Clamp(Tier, 1, 3) : rolledTier,
                Influence  = Influence,
                Intensity  = Intensity,
                DoorNorth  = DoorNorth,
                DoorEast   = DoorEast,
                DoorSouth  = DoorSouth,
                DoorWest   = DoorWest,
            };
            recipe.BreachFaction = recipe.Type.HasSecondFaction() ? PickBreach(recipe.Faction, rolledBreach, factions) : MapNode.NoFaction;
            return recipe;
        }

        // Everything rolled by the seed, with a new seed: a room nobody chose.
        public void RandomizeAll()
        {
            SeedText = NewSeedText();
            RandomType = true;
            Category = RoomCategory.None;
            Place = Layout = string.Empty;
            Faction = BreachFaction = Section = Random;
            Tier = 0;
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

        private static int Pick(int choice, int rolled, int count, int none)
        {
            if (count == 0 || choice == None) return none;
            return choice >= 0 && choice < count ? choice : rolled % count;
        }

        // A breach comes from a reality other than the one holding the room.
        private int PickBreach(int holder, int rolled, int count)
        {
            if (count == 0) return MapNode.NoFaction;
            if (BreachFaction >= 0 && BreachFaction < count) return BreachFaction;
            if (count == 1) return 0;
            int others = holder >= 0 ? count - 1 : count;
            int pick = rolled % others;
            return holder >= 0 && pick >= holder ? pick + 1 : pick;
        }
    }
}
