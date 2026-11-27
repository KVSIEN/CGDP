using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using CGD.Map;

namespace CGD.Tests
{
    // The map director's beats (MapDirector) and what a beat changes in a room (BeatTuning,
    // StructureTarget).
    public class MapDirectorTests
    {
        private MapGraph _graph;

        [SetUp]
        public void NewGraph() => _graph = new MapGraph();

        private int Add(MapNodeType type, float intensity)
        {
            MapNode node = _graph.AddNode(type, new Vector2(_graph.Nodes.Count, 0f));
            node.Intensity = intensity;
            return node.Id;
        }

        // The ids joined in a row, first to last.
        private void Chain(params int[] ids)
        {
            for (int i = 1; i < ids.Length; i++) _graph.Connect(ids[i - 1], ids[i]);
        }

        private Dictionary<int, RoomBeat> Plan() => MapDirector.Plan(_graph, 0.6f);

        [Test]
        public void TheStrongestFightOfItsStretchPeaksAndTheNextFightIsTheRelief()
        {
            int start = Add(MapNodeType.Start, 0f);
            int a = Add(MapNodeType.Combat, 0.3f);
            int peak = Add(MapNodeType.Combat, 0.8f);
            int after = Add(MapNodeType.Combat, 0.5f);
            int shop = Add(MapNodeType.Shop, 0.2f);
            int before = Add(MapNodeType.Combat, 0.4f);
            int boss = Add(MapNodeType.Boss, 1f);
            int exit = Add(MapNodeType.Exit, 0f);
            Chain(start, a, peak, after, shop, before, boss, exit);

            Dictionary<int, RoomBeat> beats = Plan();
            Assert.That(beats[start], Is.EqualTo(RoomBeat.Rest));
            Assert.That(beats[a], Is.EqualTo(RoomBeat.Build));
            Assert.That(beats[peak], Is.EqualTo(RoomBeat.Peak));
            Assert.That(beats[after], Is.EqualTo(RoomBeat.Relief));
            Assert.That(beats[shop], Is.EqualTo(RoomBeat.Rest));
            Assert.That(beats[before], Is.EqualTo(RoomBeat.Approach));
            Assert.That(beats[boss], Is.EqualTo(RoomBeat.Climax));
            Assert.That(beats[exit], Is.EqualTo(RoomBeat.Rest));
        }

        [Test]
        public void TwoEquallyIntenseFightsSideBySideMakeOnePeakTheDeeperOne()
        {
            int start = Add(MapNodeType.Start, 0f);
            int first = Add(MapNodeType.Combat, 0.7f);
            int second = Add(MapNodeType.Breach, 0.7f);
            int third = Add(MapNodeType.Combat, 0.3f);
            int boss = Add(MapNodeType.Boss, 1f);
            Chain(start, first, second, third, boss, Add(MapNodeType.Exit, 0f));

            Dictionary<int, RoomBeat> beats = Plan();
            Assert.That(beats[first], Is.EqualTo(RoomBeat.Build));
            Assert.That(beats[second], Is.EqualTo(RoomBeat.Peak));
            Assert.That(beats[third], Is.EqualTo(RoomBeat.Approach), "the approach wins over the relief");
        }

        [Test]
        public void AFightUnderThePeakIntensityNeverPeaks()
        {
            int start = Add(MapNodeType.Start, 0f);
            int a = Add(MapNodeType.Combat, 0.5f);
            int b = Add(MapNodeType.Combat, 0.2f);
            int exit = Add(MapNodeType.Exit, 0f);
            Chain(start, a, b, exit);

            Dictionary<int, RoomBeat> beats = Plan();
            Assert.That(beats[a], Is.EqualTo(RoomBeat.Build));
            Assert.That(beats[b], Is.EqualTo(RoomBeat.Build));
        }

        [Test]
        public void ACalmRoomBeforeTheBossStaysTheBreatherAndWavesCountAsFights()
        {
            int start = Add(MapNodeType.Start, 0f);
            int holdout = Add(MapNodeType.Holdout, 0.4f);
            int quiet = Add(MapNodeType.Quiet, 0.1f);
            int boss = Add(MapNodeType.Boss, 1f);
            int unreached = Add(MapNodeType.Combat, 0.5f);
            Chain(start, holdout, quiet, boss, Add(MapNodeType.Exit, 0f));

            Dictionary<int, RoomBeat> beats = Plan();
            Assert.That(beats[holdout], Is.EqualTo(RoomBeat.Build), "a room with waves and no placed enemies is still a fight");
            Assert.That(beats[quiet], Is.EqualTo(RoomBeat.Rest));
            Assert.That(beats[unreached], Is.EqualTo(RoomBeat.None));
        }

        [Test]
        public void WithoutSettingsNoRoomIsDirected()
        {
            Chain(Add(MapNodeType.Start, 0f), Add(MapNodeType.Combat, 0.9f));
            MapDirection direction = MapDirector.Plan(_graph, null);
            Assert.That(direction.Beats, Is.Empty);
            Assert.That(direction.SizeScaleOf(0), Is.EqualTo(1f));
        }

        [Test]
        public void FightRoomsGrowTowardTheBossAndCalmRoomsKeepTheirSize()
        {
            int start = Add(MapNodeType.Start, 0f);
            int early = Add(MapNodeType.Combat, 0.2f);
            int shop = Add(MapNodeType.Shop, 0.2f);
            int late = Add(MapNodeType.Combat, 0.3f);
            int boss = Add(MapNodeType.Boss, 1f);
            Chain(start, early, shop, late, boss, Add(MapNodeType.Exit, 0f));

            var settings = ScriptableObject.CreateInstance<MapDirectorSettings>();
            MapDirection direction = MapDirector.Plan(_graph, settings);
            Assert.That(direction.BeatOf(early), Is.EqualTo(RoomBeat.Build));
            Assert.That(direction.SizeScaleOf(early), Is.LessThan(1f), "small chambers early");
            Assert.That(direction.SizeScaleOf(late), Is.GreaterThan(direction.SizeScaleOf(early)));
            Assert.That(direction.SizeScaleOf(shop), Is.EqualTo(1f));
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void ABeatMayLetTheBudgetBuildPastItsLimit()
        {
            var peak = new BeatTuning(RoomBeat.Peak, extraAdditions: 3);
            Assert.That(peak.Structure.ExtraAdditions, Is.EqualTo(3));
            Assert.That(default(StructureTarget).ExtraAdditions, Is.EqualTo(0));
        }

        [Test]
        public void ABeatScalesEnemiesButAFightKeepsOne()
        {
            var relief = new BeatTuning(RoomBeat.Relief, enemyScale: 0.6f);
            Assert.That(relief.ScaleEnemies(5), Is.EqualTo(3));
            Assert.That(relief.ScaleEnemies(1), Is.EqualTo(1));
            Assert.That(relief.ScaleEnemies(0), Is.EqualTo(0));
            Assert.That(BeatTuning.Neutral.ScaleEnemies(4), Is.EqualTo(4));
        }

        [Test]
        public void TheDefaultTargetAsksWhatTheBudgetDoes()
        {
            Assert.That(default(StructureTarget).OpenRunScale, Is.EqualTo(1f));
            Assert.That(default(StructureTarget).PointsScale, Is.EqualTo(1f));
            Assert.That(BeatTuning.Neutral.Structure.PointsScale, Is.EqualTo(1f));
        }

        [Test]
        public void APeaksTargetBuildsAnOpenHallUpFurtherThanACalmOne()
        {
            var budget = new StructureBudget();
            SetOptions(budget, ScriptableObject.CreateInstance<CoverWallsStructureRule>());
            RoomStructure hall = new RoomStructurePlanner().Prepare(RoomFootprint.Rectangle(new RectInt(0, 0, 16, 12)), 4f,
                                                                     new[] { new Vector2Int(0, 6) });
            var peak = new StructureTarget(0.75f, 1.25f);
            var calm = new StructureTarget(1.5f, 0.5f);

            Assert.That(budget.Needed(hall, peak), Is.GreaterThan(budget.Needed(hall, calm)));
            int peakWalls = 0, calmWalls = 0;
            for (int seed = 1; seed <= 6; seed++)
            {
                peakWalls += budget.Apply(hall, new RandomStream(new Seed((ulong)seed)), peak).Partitions.Count;
                calmWalls += budget.Apply(hall, new RandomStream(new Seed((ulong)seed)), calm).Partitions.Count;
            }
            Assert.That(peakWalls, Is.GreaterThan(calmWalls));
        }

        private static void SetOptions(StructureBudget budget, RoomStructureRule rule)
        {
            var options = new List<StructureBudget.Option> { new() { Rule = rule } };
            typeof(StructureBudget).GetField("_options", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                   .SetValue(budget, options);
        }
    }
}
