using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Level;
using CGD.Map;

namespace CGD.Tests
{
    // The sawtooth the generator raises into a map (MapPacing.Raise), and that the map director
    // then reads as it is, with nothing left to report.
    public class MapPacingTests
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

        private void Chain(params int[] ids)
        {
            for (int i = 1; i < ids.Length; i++) _graph.Connect(ids[i - 1], ids[i]);
        }

        private float IntensityOf(int id) => _graph.TryGetNode(id, out MapNode node) ? node.Intensity : -1f;

        private List<DirectorFinding> PlanFindings(MapPacingSettings pacing, out Dictionary<int, RoomBeat> beats)
        {
            var findings = new List<DirectorFinding>();
            beats = MapDirector.Plan(_graph, pacing, findings);
            return findings;
        }

        [Test]
        public void ALongRunWithNoPeakHasItsStrongestFightRaisedAboveItsNeighbours()
        {
            int start = Add(MapNodeType.Start, 0f);
            int a = Add(MapNodeType.Combat, 0.2f);
            int b = Add(MapNodeType.Siege, 0.3f);
            int c = Add(MapNodeType.Combat, 0.5f);
            int d = Add(MapNodeType.Combat, 0.4f);
            int e = Add(MapNodeType.Ambush, 0.3f);
            int before = Add(MapNodeType.Combat, 0.2f);
            Chain(start, a, b, c, d, e, before, Add(MapNodeType.Boss, 1f), Add(MapNodeType.Exit, 0f));

            MapPacing.Raise(_graph, MapPacingSettings.Default, null);

            Assert.That(IntensityOf(c), Is.EqualTo(0.6f).Within(1e-5f), "lifted to the peak intensity");
            Assert.That(IntensityOf(d), Is.EqualTo(0.4f), "nothing else changes");
            Assert.That(IntensityOf(before), Is.EqualTo(0.2f));

            List<DirectorFinding> findings = PlanFindings(MapPacingSettings.Default, out Dictionary<int, RoomBeat> beats);
            Assert.That(beats[c], Is.EqualTo(RoomBeat.Peak));
            Assert.That(beats[d], Is.EqualTo(RoomBeat.Relief));
            Assert.That(beats[before], Is.EqualTo(RoomBeat.Approach));
            Assert.That(findings.Exists(f => f.Check is DirectorCheck.FlatStretch or DirectorCheck.NoPeak), Is.False,
                "the director finds nothing left to report");
        }

        [Test]
        public void ARaisedPeakClearsTheStrongestFightBesideItByTheMargin()
        {
            int start = Add(MapNodeType.Start, 0f);
            int a = Add(MapNodeType.Combat, 0.7f);
            int b = Add(MapNodeType.Combat, 0.72f);
            int c = Add(MapNodeType.Combat, 0.68f);
            int side = Add(MapNodeType.Combat, 0.75f);
            _graph.Connect(b, side);
            _graph.Connect(side, Add(MapNodeType.Combat, 0.9f)); // the side room's own peak, so the side room is none
            int before = Add(MapNodeType.Combat, 0.2f);
            Chain(start, a, b, c, before, Add(MapNodeType.Boss, 1f), Add(MapNodeType.Exit, 0f));
            var pacing = new MapPacingSettings(maxFightsWithoutPeak: 2);

            MapPacing.Raise(_graph, pacing, null);

            var analysis = new MapGraphAnalysis(_graph);
            List<int> path = MapPacing.PacedPath(_graph, analysis);
            Assert.That(MapPacing.PeaklessRuns(_graph, analysis, path, pacing.PeakIntensity, 2), Is.Empty);
            Assert.That(IntensityOf(b), Is.EqualTo(0.75f + MapPacing.Margin).Within(1e-5f), "above the side room's fight beside it");
        }

        [Test]
        public void AWayToTheBossWithNoPeakGetsOneButNotInTheRoomBeforeTheBoss()
        {
            int start = Add(MapNodeType.Start, 0f);
            int a = Add(MapNodeType.Combat, 0.4f);
            int shop = Add(MapNodeType.Shop, 0.1f);
            int b = Add(MapNodeType.Combat, 0.3f);
            int before = Add(MapNodeType.Combat, 0.5f);
            Chain(start, a, shop, b, before, Add(MapNodeType.Boss, 1f), Add(MapNodeType.Exit, 0f));

            MapPacing.Raise(_graph, MapPacingSettings.Default, null);

            PlanFindings(MapPacingSettings.Default, out Dictionary<int, RoomBeat> beats);
            Assert.That(beats[a], Is.EqualTo(RoomBeat.Peak), "the strongest fight that can peak");
            Assert.That(beats[before], Is.EqualTo(RoomBeat.Approach));
            Assert.That(IntensityOf(before), Is.EqualTo(0.5f));
        }

        [Test]
        public void APinnedRoomKeepsItsIntensityAndTheNextStrongestPeaks()
        {
            int start = Add(MapNodeType.Start, 0f);
            int a = Add(MapNodeType.Combat, 0.3f);
            int pinned = Add(MapNodeType.Combat, 0.5f);
            int c = Add(MapNodeType.Combat, 0.2f);
            int d = Add(MapNodeType.Combat, 0.45f);
            int before = Add(MapNodeType.Combat, 0.2f);
            Chain(start, a, pinned, c, d, before, Add(MapNodeType.Boss, 1f), Add(MapNodeType.Exit, 0f));

            MapPacing.Raise(_graph, MapPacingSettings.Default, new HashSet<int> { pinned });

            Assert.That(IntensityOf(pinned), Is.EqualTo(0.5f));
            Assert.That(IntensityOf(d), Is.EqualTo(0.6f).Within(1e-5f));
        }

        [Test]
        public void AMapThatAlreadyClimbsIsLeftAlone()
        {
            int start = Add(MapNodeType.Start, 0f);
            int a = Add(MapNodeType.Combat, 0.3f);
            int peak = Add(MapNodeType.Combat, 0.8f);
            int after = Add(MapNodeType.Combat, 0.5f);
            int before = Add(MapNodeType.Combat, 0.4f);
            Chain(start, a, peak, after, Add(MapNodeType.Quiet, 0f), before, Add(MapNodeType.Boss, 1f), Add(MapNodeType.Exit, 0f));
            var was = new List<float>();
            foreach (MapNode node in _graph.Nodes) was.Add(node.Intensity);

            MapPacing.Raise(_graph, MapPacingSettings.Default, null);

            for (int i = 0; i < was.Count; i++) Assert.That(_graph.Nodes[i].Intensity, Is.EqualTo(was[i]));
        }

        [Test]
        public void TwoPeaksNeverStandSideBySide()
        {
            int start = Add(MapNodeType.Start, 0f);
            var fights = new List<int>();
            for (int i = 0; i < 9; i++) fights.Add(Add(MapNodeType.Combat, 0.2f + 0.01f * i));
            var chain = new List<int> { start };
            chain.AddRange(fights);
            chain.Add(Add(MapNodeType.Boss, 1f));
            chain.Add(Add(MapNodeType.Exit, 0f));
            Chain(chain.ToArray());

            MapPacing.Raise(_graph, new MapPacingSettings(maxFightsWithoutPeak: 2), null);

            PlanFindings(new MapPacingSettings(maxFightsWithoutPeak: 2), out Dictionary<int, RoomBeat> beats);
            for (int i = 1; i < fights.Count; i++)
                Assert.That(beats[fights[i]] == RoomBeat.Peak && beats[fights[i - 1]] == RoomBeat.Peak, Is.False, $"#{fights[i - 1]} and #{fights[i]}");
            var analysis = new MapGraphAnalysis(_graph);
            Assert.That(MapPacing.PeaklessRuns(_graph, analysis, MapPacing.PacedPath(_graph, analysis), 0.6f, 2), Is.Empty);
        }

        [Test]
        public void AStrongestPeakEarlyGetsALaterFightRaisedAboveIt()
        {
            int start = Add(MapNodeType.Start, 0f);
            int early = Add(MapNodeType.Combat, 0.8f);
            int a = Add(MapNodeType.Combat, 0.3f);
            int shop = Add(MapNodeType.Shop, 0.1f);
            int b = Add(MapNodeType.Combat, 0.3f);
            int late = Add(MapNodeType.Combat, 0.65f);
            int c = Add(MapNodeType.Combat, 0.4f);
            int quiet = Add(MapNodeType.Quiet, 0f);
            int before = Add(MapNodeType.Combat, 0.3f);
            Chain(start, early, a, shop, b, late, c, quiet, before, Add(MapNodeType.Boss, 1f), Add(MapNodeType.Exit, 0f));
            var analysis = new MapGraphAnalysis(_graph);
            Assert.That(MapPacing.EarlyStrongestPeak(_graph, analysis, MapPacing.PacedPath(_graph, analysis), 0.6f), Is.EqualTo(early));

            MapPacing.Raise(_graph, MapPacingSettings.Default, null);

            Assert.That(IntensityOf(early), Is.EqualTo(0.8f), "the early peak stays as it was");
            Assert.That(IntensityOf(late), Is.EqualTo(0.8f + MapPacing.Margin).Within(1e-5f), "the later peak climbs past it");
            List<DirectorFinding> findings = PlanFindings(MapPacingSettings.Default, out _);
            Assert.That(findings.Exists(f => f.Check == DirectorCheck.EarlyPeak), Is.False);
        }

        [Test]
        public void CalmRunsCountNeitherTheStartNorTheBoss()
        {
            int start = Add(MapNodeType.Start, 0f);
            int first = Add(MapNodeType.Shop, 0f);
            Chain(start, first, Add(MapNodeType.Quiet, 0f), Add(MapNodeType.Junction, 0f), Add(MapNodeType.Boss, 1f), Add(MapNodeType.Exit, 0f));
            var analysis = new MapGraphAnalysis(_graph);

            Assert.That(MapPacing.CalmRuns(_graph, analysis, 3), Is.Empty);
            List<List<int>> runs = MapPacing.CalmRuns(_graph, analysis, 2);
            Assert.That(runs.Count, Is.EqualTo(1));
            Assert.That(runs[0][0], Is.EqualTo(first));
            Assert.That(runs[0].Count, Is.EqualTo(3));
        }

        [Test]
        public void WithTheRuleOffNothingIsRaised()
        {
            int start = Add(MapNodeType.Start, 0f);
            int a = Add(MapNodeType.Combat, 0.3f);
            int b = Add(MapNodeType.Combat, 0.4f);
            int before = Add(MapNodeType.Combat, 0.2f);
            Chain(start, a, b, before, Add(MapNodeType.Boss, 1f), Add(MapNodeType.Exit, 0f));

            MapPacing.Raise(_graph, new MapPacingSettings(maxFightsWithoutPeak: 0), null);

            Assert.That(IntensityOf(b), Is.EqualTo(0.4f));
        }
    }
}
