using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Level;
using CGD.Map;

namespace CGD.Tests
{
    // The map director's checks: the pacing reports along the main path (MapDirector) and the
    // set-piece retry on built rooms (BeatReview).
    public class DirectorChecksTests
    {
        private MapGraph _graph;
        private List<DirectorFinding> _findings;

        [SetUp]
        public void NewGraph()
        {
            _graph = new MapGraph();
            _findings = new List<DirectorFinding>();
        }

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

        private Dictionary<int, RoomBeat> Plan(int maxFightsWithoutPeak = 3, int calmStretch = 3) =>
            MapDirector.Plan(_graph, new MapPacingSettings(maxFightsWithoutPeak: maxFightsWithoutPeak), calmStretch, _findings);

        private bool Found(DirectorCheck check, bool fixedByDirector) => _findings.Exists(f => f.Check == check && f.Fixed == fixedByDirector);

        [Test]
        public void ALongRunOfFightsWithNoPeakIsReportedAndLeftAsItIs()
        {
            int start = Add(MapNodeType.Start, 0f);
            int a = Add(MapNodeType.Combat, 0.2f);
            int b = Add(MapNodeType.Combat, 0.3f);
            int c = Add(MapNodeType.Combat, 0.5f);
            int d = Add(MapNodeType.Combat, 0.4f);
            int e = Add(MapNodeType.Combat, 0.3f);
            int before = Add(MapNodeType.Combat, 0.2f);
            Chain(start, a, b, c, d, e, before, Add(MapNodeType.Boss, 1f), Add(MapNodeType.Exit, 0f));

            Dictionary<int, RoomBeat> beats = Plan();
            Assert.That(beats[c], Is.EqualTo(RoomBeat.Build), "raising peaks is the generator's job (MapPeaks.Raise)");
            Assert.That(_graph.TryGetNode(c, out MapNode strongest) ? strongest.Intensity : -1f, Is.EqualTo(0.5f), "the director changes no intensity");
            Assert.That(beats[before], Is.EqualTo(RoomBeat.Approach));
            Assert.That(Found(DirectorCheck.FlatStretch, fixedByDirector: false), Is.True);
            Assert.That(Found(DirectorCheck.FlatStretch, fixedByDirector: true), Is.False);
        }

        [Test]
        public void WithoutTheCheckTheRunStaysAsItIs()
        {
            int start = Add(MapNodeType.Start, 0f);
            int a = Add(MapNodeType.Combat, 0.2f);
            int b = Add(MapNodeType.Combat, 0.5f);
            int c = Add(MapNodeType.Combat, 0.3f);
            int d = Add(MapNodeType.Combat, 0.3f);
            Chain(start, a, b, c, d, Add(MapNodeType.Exit, 0f));

            Dictionary<int, RoomBeat> beats = Plan(maxFightsWithoutPeak: 0);
            Assert.That(beats[b], Is.EqualTo(RoomBeat.Build));
            Assert.That(_findings.Exists(f => f.Check is DirectorCheck.FlatStretch or DirectorCheck.NoPeak), Is.False);
        }

        [Test]
        public void AWayToTheBossWithNoPeakIsReported()
        {
            int start = Add(MapNodeType.Start, 0f);
            int a = Add(MapNodeType.Combat, 0.4f);
            int b = Add(MapNodeType.Combat, 0.2f);
            int before = Add(MapNodeType.Combat, 0.5f);
            Chain(start, a, b, before, Add(MapNodeType.Boss, 1f), Add(MapNodeType.Exit, 0f));

            Dictionary<int, RoomBeat> beats = Plan();
            Assert.That(beats[a], Is.EqualTo(RoomBeat.Build));
            Assert.That(beats[before], Is.EqualTo(RoomBeat.Approach));
            Assert.That(Found(DirectorCheck.NoPeak, fixedByDirector: false), Is.True);
        }

        [Test]
        public void TooManyCalmRoomsInARowAreReported()
        {
            int start = Add(MapNodeType.Start, 0f);
            int first = Add(MapNodeType.Shop, 0f);
            Chain(start, first, Add(MapNodeType.Quiet, 0f), Add(MapNodeType.Shop, 0f), Add(MapNodeType.Quiet, 0f),
                  Add(MapNodeType.Combat, 0.3f), Add(MapNodeType.Boss, 1f), Add(MapNodeType.Exit, 0f));

            Plan();
            DirectorFinding calm = _findings.Find(f => f.Check == DirectorCheck.CalmStretch);
            Assert.That(calm.Check, Is.EqualTo(DirectorCheck.CalmStretch));
            Assert.That(calm.RoomId, Is.EqualTo(first));
            Assert.That(calm.Fixed, Is.False);
        }

        [Test]
        public void TheStrongestPeakEarlyWithOnlyWeakerOnesAfterIsReported()
        {
            int start = Add(MapNodeType.Start, 0f);
            int early = Add(MapNodeType.Combat, 0.9f);
            Chain(start, early, Add(MapNodeType.Combat, 0.3f), Add(MapNodeType.Combat, 0.3f), Add(MapNodeType.Combat, 0.7f),
                  Add(MapNodeType.Combat, 0.3f), Add(MapNodeType.Boss, 1f), Add(MapNodeType.Exit, 0f));

            Plan();
            Assert.That(_findings.Exists(f => f.Check == DirectorCheck.EarlyPeak && f.RoomId == early), Is.True);
        }

        [Test]
        public void APeakThatPassesIsKeptAndOneThatDoesntIsBuiltAgain()
        {
            var layout = new LevelLayout(1f);
            MapNode build = _graph.AddNode(MapNodeType.Combat, Vector2.zero);
            MapNode peak = _graph.AddNode(MapNodeType.Combat, Vector2.right);
            _graph.Connect(build.Id, peak.Id);

            var buildRoom = new LevelRoom(build, RoomFootprint.Rectangle(new RectInt(0, 0, 10, 10)), null, 4f, beat: RoomBeat.Build);
            var peakRoom = new LevelRoom(peak, RoomFootprint.Rectangle(new RectInt(20, 0, 10, 10)), null, 4f, beat: RoomBeat.Peak);
            layout.AddRoom(buildRoom);
            layout.AddRoom(peakRoom);
            buildRoom.AttachStructure(WithWalls(buildRoom.Footprint, 4));

            var budget = new StructureBudget();
            RoomStructure bare = WithWalls(peakRoom.Footprint, 0);
            RoomStructure covered = WithWalls(peakRoom.Footprint, 8);
            int replans = 0;

            RoomStructure kept = BeatReview.BestSetPiece(_graph, layout, peakRoom, covered, budget, 3, _ => { replans++; return bare; });
            Assert.That(kept, Is.SameAs(covered));
            Assert.That(replans, Is.EqualTo(0), "a peak that met its beat is never built again");

            kept = BeatReview.BestSetPiece(_graph, layout, peakRoom, bare, budget, 3, _ => { replans++; return covered; });
            Assert.That(kept, Is.SameAs(covered));
            Assert.That(replans, Is.EqualTo(1), "it stops at the first try that passes");
        }

        [Test]
        public void TheSetPiecesArePlacedFirstAndTheRestInGraphOrder()
        {
            int start = Add(MapNodeType.Start, 0f);
            int a = Add(MapNodeType.Combat, 0.3f);
            int peak = Add(MapNodeType.Combat, 0.8f);
            int after = Add(MapNodeType.Combat, 0.3f);
            int approach = Add(MapNodeType.Combat, 0.3f);
            int boss = Add(MapNodeType.Boss, 1f);
            Chain(start, a, peak, after, approach, boss, Add(MapNodeType.Exit, 0f));
            var direction = new MapDirection(Plan(), new Dictionary<int, float>(), new List<DirectorFinding>());

            List<MapNode> order = MapDirector.PlacementOrder(_graph, direction);
            Assert.That(order[0].Id, Is.EqualTo(peak));
            Assert.That(order[1].Id, Is.EqualTo(boss));
            Assert.That(order[2].Id, Is.EqualTo(start));
            Assert.That(order[3].Id, Is.EqualTo(a));
        }

        [Test]
        public void AFightBesideAPlacedSetPieceGetsNoMoreFloorThanIt()
        {
            int start = Add(MapNodeType.Start, 0f);
            int a = Add(MapNodeType.Combat, 0.3f);
            int peak = Add(MapNodeType.Combat, 0.8f);
            int after = Add(MapNodeType.Combat, 0.3f);
            int far = Add(MapNodeType.Combat, 0.3f);
            int approach = Add(MapNodeType.Combat, 0.3f);
            int boss = Add(MapNodeType.Boss, 1f);
            Chain(start, a, peak, after, far, approach, boss, Add(MapNodeType.Exit, 0f));
            var direction = new MapDirection(Plan(), new Dictionary<int, float>(), new List<DirectorFinding>());
            var placed = new Dictionary<int, int> { [peak] = 120, [boss] = 200 };
            var neighbours = new List<int>();

            Assert.That(MapDirector.AreaLimit(_graph, a, direction, placed, neighbours), Is.EqualTo(120));
            Assert.That(MapDirector.AreaLimit(_graph, after, direction, placed, neighbours), Is.EqualTo(120), "the relief too");
            Assert.That(MapDirector.AreaLimit(_graph, approach, direction, placed, neighbours), Is.EqualTo(200), "the room before the boss stays under the boss room");
            Assert.That(MapDirector.AreaLimit(_graph, far, direction, placed, neighbours), Is.EqualTo(int.MaxValue));
            Assert.That(MapDirector.AreaLimit(_graph, start, direction, placed, neighbours), Is.EqualTo(int.MaxValue), "calm rooms aren't held to it");
            Assert.That(MapDirector.AreaLimit(_graph, a, direction, new Dictionary<int, int>(), neighbours), Is.EqualTo(int.MaxValue), "nothing placed yet");
        }

        // A room with `walls` full-height wall pieces down its middle column.
        private static RoomStructure WithWalls(RoomFootprint footprint, int walls)
        {
            RoomStructure structure = new RoomStructurePlanner().Prepare(footprint, 4f, System.Array.Empty<Vector2Int>());
            RectInt bounds = footprint.Bounds;
            var edges = new List<PartitionEdge>();
            for (int i = 0; i < walls; i++)
            {
                var tile = new Vector2Int(bounds.xMin + 4, bounds.yMin + 1 + i);
                edges.Add(new PartitionEdge(tile, tile + Vector2Int.right, 0f, dividesZones: false));
            }
            if (edges.Count > 0) Assert.That(structure.TryAddPartition(edges), Is.True);
            return structure;
        }
    }
}
