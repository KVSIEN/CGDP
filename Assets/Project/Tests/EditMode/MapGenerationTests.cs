using System.Diagnostics;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Core;
using CGD.Map;

namespace CGD.Tests
{
    // Runs the shipped map generation settings over many seeds: every map must reach its
    // exit, the same seed must give the same map, and generating must stay fast. Rule
    // violations the validator finds are reported as warnings, since a tuning asset may
    // legitimately trade a rule for another.
    public class MapGenerationTests
    {
        private const int SeedCount = 40;
        private const double MaxMillisecondsPerMap = 500.0;

        private static readonly string[] SettingsNames =
        {
            "Balanced", "Branching", "Hub", "Labyrinth", "Linear", "Random",
        };

        private static MapGenerationSettings Load(string name)
        {
            string path = $"Assets/Project/Data/Map/{name}MapGenerationSettings.asset";
            var settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(path);
            Assert.IsNotNull(settings, $"Missing settings asset at {path}");
            Assert.IsTrue(settings.CanGenerate, $"{name} cannot generate: it needs content and a layout");
            return settings;
        }

        private static string Fingerprint(MapGraph graph)
        {
            var sb = new StringBuilder();
            foreach (MapNode n in graph.Nodes)
                sb.Append(n.Id).Append(':').Append(n.Type).Append(':').Append(n.Tier).Append(':').Append(n.Category).Append(':').Append(n.Faction)
                  .Append('@').Append(n.Position.x.ToString("R")).Append(',').Append(n.Position.y.ToString("R")).Append(';');
            foreach (MapConnection c in graph.Connections)
                sb.Append(c.A).Append('-').Append(c.B).Append(':').Append(c.Type).Append(c.Direct ? "d" : "").Append(';');
            return sb.ToString();
        }

        [TestCaseSource(nameof(SettingsNames))]
        public void EveryGeneratedMapReachesItsExit(string name)
        {
            var generator = new MapGenerator(Load(name));

            for (int i = 0; i < SeedCount; i++)
            {
                MapGenerationResult result = generator.Generate(Seed.From(i));
                var analysis = new MapGraphAnalysis(result.Graph);

                Assert.IsTrue(analysis.ExitReachable, $"{name}, seed {i}: the exit can't be reached from the start");
            }
        }

        [TestCaseSource(nameof(SettingsNames))]
        public void SameSeedGivesTheSameMap(string name)
        {
            var settings = Load(name);

            for (int i = 0; i < SeedCount; i++)
            {
                string first  = Fingerprint(new MapGenerator(settings).Generate(Seed.From(i)).Graph);
                string second = Fingerprint(new MapGenerator(settings).Generate(Seed.From(i)).Graph);

                Assert.AreEqual(first, second, $"{name}, seed {i}: generation isn't reproducible");
            }
        }

        [TestCaseSource(nameof(SettingsNames))]
        public void JunctionsAreForksAndDirectDoorsJoinNeighbours(string name)
        {
            var settings  = Load(name);
            var generator = new MapGenerator(settings);
            Vector2 spacing = settings.NodeSpacing;

            for (int i = 0; i < SeedCount; i++)
            {
                MapGraph graph = generator.Generate(Seed.From(i)).Graph;

                foreach (MapNode node in graph.Nodes)
                    if (node.Type == MapNodeType.Junction)
                        Assert.GreaterOrEqual(graph.Degree(node.Id), 3, $"{name}, seed {i}: Junction #{node.Id} isn't a fork");

                foreach (MapConnection connection in graph.Connections)
                {
                    if (!connection.Direct) continue;

                    graph.TryGetNode(connection.A, out MapNode a);
                    graph.TryGetNode(connection.B, out MapNode b);
                    Vector2 step = b.Position - a.Position;
                    bool oneColumn = Mathf.Approximately(Mathf.Abs(step.x), spacing.x) && Mathf.Approximately(step.y, 0f);
                    bool oneRow    = Mathf.Approximately(Mathf.Abs(step.y), spacing.y) && Mathf.Approximately(step.x, 0f);
                    Assert.IsTrue(oneColumn || oneRow, $"{name}, seed {i}: direct door #{a.Id}–#{b.Id} isn't between neighbouring cells");
                    Assert.AreEqual(ConnectionType.Normal, connection.Type, $"{name}, seed {i}: direct door #{a.Id}–#{b.Id} is a gate or shortcut");
                    Assert.IsFalse(connection.OneWay, $"{name}, seed {i}: direct door #{a.Id}–#{b.Id} is one-way");
                }
            }
        }

        [TestCaseSource(nameof(SettingsNames))]
        public void RoomCategoriesAreAssignedAndMostlyGetAlong(string name)
        {
            var settings = Load(name);
            MapCategorySettings categories = settings.Content.Categories;
            if (categories == null) Assert.Ignore($"{name}: no category settings");

            var generator = new MapGenerator(settings);
            int links = 0, clashes = 0;

            for (int i = 0; i < SeedCount; i++)
            {
                MapGraph graph = generator.Generate(Seed.From(i)).Graph;

                foreach (MapNode node in graph.Nodes)
                {
                    Assert.AreNotEqual(RoomCategory.None, node.Category, $"{name}, seed {i}: #{node.Id} {node.Type} has no category");
                    RoomCategory fixedCategory = categories.FixedFor(node.Type);
                    if (fixedCategory != RoomCategory.None)
                        Assert.AreEqual(fixedCategory, node.Category, $"{name}, seed {i}: #{node.Id} {node.Type} should always be {fixedCategory}");
                }

                foreach (MapConnection connection in graph.Connections)
                {
                    graph.TryGetNode(connection.A, out MapNode a);
                    graph.TryGetNode(connection.B, out MapNode b);
                    links++;
                    if (categories.Affinity(a.Category, b.Category) < categories.ClashBelow) clashes++;
                }
            }

            float rate = links == 0 ? 0f : (float)clashes / links;
            TestContext.WriteLine($"{name}: {clashes} of {links} links join clashing categories ({rate:P1})");
            if (rate > 0.1f) Assert.Warn($"{name}: {rate:P0} of links join clashing categories");
        }

        [TestCaseSource(nameof(SettingsNames))]
        public void GenerationStaysWithinTheTimeBudget(string name)
        {
            var generator = new MapGenerator(Load(name));
            generator.Generate(Seed.From(-1)); // warm up JIT and asset loading

            var watch = Stopwatch.StartNew();
            for (int i = 0; i < SeedCount; i++) generator.Generate(Seed.From(i));
            watch.Stop();

            double perMap = watch.Elapsed.TotalMilliseconds / SeedCount;
            TestContext.WriteLine($"{name}: {perMap:0.0} ms per map over {SeedCount} seeds");
            Assert.Less(perMap, MaxMillisecondsPerMap, $"{name} takes {perMap:0.0} ms per map");
        }

        [TestCaseSource(nameof(SettingsNames))]
        public void ValidatorIssuesAreReported(string name)
        {
            var settings  = Load(name);
            var generator = new MapGenerator(settings);
            int issueCount = 0;

            for (int i = 0; i < SeedCount; i++)
            {
                MapGenerationResult result = generator.Generate(Seed.From(i));
                var tuning   = MapGenerator.PickModifiers(settings, Seed.From(i), null);
                var analysis = new MapGraphAnalysis(result.Graph);

                var issues = MapGraphValidator.Validate(result.Graph, analysis, result.Layout, settings.Content, tuning);
                foreach (string issue in issues)
                {
                    issueCount++;
                    if (issueCount <= 5) TestContext.WriteLine($"{name}, seed {i}: {issue}");
                }
            }

            if (issueCount > 0) Assert.Warn($"{name}: {issueCount} validator issue(s) across {SeedCount} seeds");
        }
    }
}
