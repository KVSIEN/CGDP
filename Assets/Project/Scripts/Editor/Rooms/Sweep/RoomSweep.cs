using System.Collections.Generic;
using CGD.Core;
using CGD.Level;
using CGD.Map;
using UnityEditor;

namespace CGD.Editor
{
    // Builds the Room Lab's recipe for every place, held by every faction, from a run of
    // seeds, through the level's own pipeline (RoomSampleBuilder), and keeps what each room
    // measured (RoomSweepResult). A few rooms are built per editor tick, so the editor stays
    // responsive. Used to tune the trait bands (RoomTraitMeasure) and Priority Tries against
    // what generation really produces (RoomSweepReport).
    public sealed class RoomSweep
    {
        private const double TickBudget = 0.05;

        private readonly struct Job
        {
            public Job(RoomFunction place, int faction, string seed)
            {
                Place   = place;
                Faction = faction;
                Seed    = seed;
            }

            public RoomFunction Place   { get; }
            public int          Faction { get; }
            public string       Seed    { get; }
        }

        private readonly RoomLabRecipe _recipe;
        private readonly List<Job> _jobs = new();
        private readonly List<RoomSweepResult> _results = new();
        private int _next;

        // `recipe`: what every room is built as (type, tier, doorways, section, settings and
        // content) apart from its place, faction and seed. Landmarks are left out: they keep
        // their hand-built interior.
        public RoomSweep(RoomLabRecipe recipe, int seeds)
        {
            _recipe = recipe.Clone();
            _recipe.Source = RoomLabRecipe.SourceKind.Recipe;

            int factions = _recipe.Content != null ? _recipe.Content.Factions.Count : 0;
            foreach (RoomFunction place in Places())
                for (int faction = 0; faction < factions; faction++)
                    foreach (string seed in SeedSequence.CountingFrom(_recipe.Seed, seeds))
                        _jobs.Add(new Job(place, faction, seed));
        }

        public RoomLabRecipe Recipe => _recipe;
        public IReadOnlyList<RoomSweepResult> Results => _results;
        public int Total => _jobs.Count;
        public int Done => _next;
        public int Failed { get; private set; }
        public bool IsFinished => _next >= _jobs.Count;

        // Builds what fits in a tick; true when anything was built.
        public bool Step()
        {
            if (IsFinished) return false;

            double stop = EditorApplication.timeSinceStartup + TickBudget;
            do Build(_jobs[_next++]);
            while (!IsFinished && EditorApplication.timeSinceStartup < stop);
            return true;
        }

        private void Build(Job job)
        {
            RoomLabRecipe recipe = _recipe.Clone();
            recipe.Place         = job.Place;
            recipe.Category      = job.Place.Category;
            recipe.Faction       = job.Faction;
            recipe.BreachFaction = recipe.Type.HasSecondFaction() ? (job.Faction + 1) % recipe.Content.Factions.Count : MapNode.NoFaction;

            RoomSample sample;
            try
            {
                sample = RoomSampleBuilder.Build(recipe, job.Seed);
            }
            catch (System.Exception)
            {
                // A room the pipeline can't build counts as failed; the Room Lab shows why.
                Failed++;
                return;
            }
            if (!sample.IsValid)
            {
                Failed++;
                return;
            }

            string faction = sample.Room.Faction != null ? sample.Room.Faction.DisplayName : $"faction {job.Faction}";
            RoomStructure structure = sample.Room.Structure;
            // Read both ways off the finished room, from one visibility, so a single sweep compares them.
            var visibility = new RoomVisibility(structure, sample.Sight.Blocking);
            IReadOnlyList<PlannedProp> props = sample.Furnishing.Props;
            _results.Add(new RoomSweepResult(job.Place.DisplayName, faction, job.Seed, sample.Traits, sample.Design,
                                             sample.Furnishing.Attempt, sample.Furnishing.Tries,
                                             RoomFlowReading.Of(structure), ShowcaseSeen(structure, sample.Furnishing),
                                             RoomSightReading.Of(structure, visibility),
                                             RoomTraitMeasure.Measure(structure, props, sample.Sight),
                                             RoomTraitMeasure.Measure(structure, props, sample.Sight, visibility)));
        }

        // Any prop of a Showcase placement that went down and is in view of the way in.
        // Planned props don't name their placement, so they are matched by prefab.
        private static bool? ShowcaseSeen(RoomStructure structure, RoomFurnishing furnishing)
        {
            if (!structure.Arrival.HasValue) return null;

            var prefabs = new HashSet<UnityEngine.GameObject>();
            foreach (PropPlacementOutcome outcome in furnishing.Outcomes)
                if (outcome.Placement != null && outcome.Placement.Selection == PropSelection.Showcase && outcome.Placed > 0)
                    AddPrefabs(outcome.Placement, prefabs);
            if (prefabs.Count == 0) return null;

            var sight = new ArrivalSight(structure);
            foreach (PlannedProp prop in furnishing.Props)
                if (prefabs.Contains(prop.Prefab) && sight.Sees(prop.Tiles)) return true;
            return false;
        }

        private static void AddPrefabs(RoomPropPlacement placement, HashSet<UnityEngine.GameObject> prefabs)
        {
            if (placement.Prefabs != null)
                foreach (UnityEngine.GameObject prefab in placement.Prefabs)
                    if (prefab != null) prefabs.Add(prefab);
            if (!placement.HasMembers) return;
            foreach (PropMember member in placement.Members)
                foreach (UnityEngine.GameObject prefab in member.Prefabs)
                    if (prefab != null) prefabs.Add(prefab);
        }

        private static List<RoomFunction> Places()
        {
            var places = new List<RoomFunction>();
            foreach (string guid in AssetDatabase.FindAssets("t:RoomFunction"))
            {
                var place = AssetDatabase.LoadAssetAtPath<RoomFunction>(AssetDatabase.GUIDToAssetPath(guid));
                if (place != null && place.Landmark == null) places.Add(place);
            }
            places.Sort((a, b) => string.CompareOrdinal(a.DisplayName, b.DisplayName));
            return places;
        }
    }
}
