using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Level;
using CGD.Map;

namespace CGD.Tests
{
    // The map director's checks: the pacing pass along the main path (MapDirector) and the
    // set-piece retry on built rooms (BeatReview).
    public class DirectorChecksTests
    {
        private static readonly MapNodeType[] Calm = { MapNodeType.Start, MapNodeType.Exit, MapNodeType.Shop, MapNodeType.Quiet };

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

        private Dictionary<int, RoomBeat> Plan(int flatStretch = 3, int calmStretch = 3) =>
            MapDirector.Plan(_graph, Calm, 0.6f, flatStretch, calmStretch, _findings);

        private bool Found(DirectorCheck check, bool fixedByDirector) => _findings.Exists(f => f.Check == check && f.Fixed == fixedByDirector);

        [Test]
        public void ALongRunOfOrdinaryFightsGetsAPeakAndARelief()
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
            Assert.That(beats[c], Is.EqualTo(RoomBeat.Peak), "the strongest of the run");
            Assert.That(beats[d], Is.EqualTo(RoomBeat.Relief));
            Assert.That(beats[before], Is.EqualTo(RoomBeat.Approach));
            Assert.That(Found(DirectorCheck.FlatStretch, fixedByDirector: true), Is.True);
            Assert.That(Found(DirectorCheck.FlatStretch, fixedByDirector: false), Is.False);
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

            Dictionary<int, RoomBeat> beats = Plan(flatStretch: 0);
            Assert.That(beats[b], Is.EqualTo(RoomBeat.Build));
        }

        [Test]
        public void AWayToTheBossWithNoPeakGetsOne()
        {
            int start = Add(MapNodeType.Start, 0f);
            int a = Add(MapNodeType.Combat, 0.4f);
            int b = Add(MapNodeType.Combat, 0.2f);
            int before = Add(MapNodeType.Combat, 0.5f);
            Chain(start, a, b, before, Add(MapNodeType.Boss, 1f), Add(MapNodeType.Exit, 0f));

            Dictionary<int, RoomBeat> beats = Plan();
            Assert.That(beats[a], Is.EqualTo(RoomBeat.Peak), "the room before the boss stays the approach");
            Assert.That(beats[b], Is.EqualTo(RoomBeat.Relief));
            Assert.That(beats[before], Is.EqualTo(RoomBeat.Approach));
            Assert.That(Found(DirectorCheck.NoPeak, fixedByDirector: true), Is.True);
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
        public void APeakBesideAClearlyBiggerFightMovesOntoItAndTheReliefsFollow()
        {
            int start = Add(MapNodeType.Start, 0f);
            int hall = Add(MapNodeType.Combat, 0.3f);
            int peak = Add(MapNodeType.Combat, 0.8f);
            int after = Add(MapNodeType.Combat, 0.3f);
            int approach = Add(MapNodeType.Combat, 0.3f);
            Chain(start, hall, peak, after, approach, Add(MapNodeType.Boss, 1f), Add(MapNodeType.Exit, 0f));

            LevelLayout layout = Placed(Plan(), (hall, 20), (peak, 10), (after, 10), (approach, 10));
            var settings = ScriptableObject.CreateInstance<MapDirectorSettings>();
            try { MapDirector.MovePeaksToBiggerRooms(_graph, layout, settings); }
            finally { Object.DestroyImmediate(settings); }

            Assert.That(layout.Rooms[hall].Beat, Is.EqualTo(RoomBeat.Peak));
            Assert.That(layout.Rooms[peak].Beat, Is.EqualTo(RoomBeat.Relief), "the old peak is now the breather after the new one");
            Assert.That(layout.Rooms[after].Beat, Is.EqualTo(RoomBeat.Build));
            Assert.That(layout.Rooms[approach].Beat, Is.EqualTo(RoomBeat.Approach));
            Assert.That(layout.DirectorFindings.Exists(f => f.Check == DirectorCheck.PeakSize && f.Fixed && f.RoomId == hall), Is.True);
        }

        [Test]
        public void APeakBesideAFightOnlyALittleBiggerStays()
        {
            int start = Add(MapNodeType.Start, 0f);
            int a = Add(MapNodeType.Combat, 0.3f);
            int peak = Add(MapNodeType.Combat, 0.8f);
            int after = Add(MapNodeType.Combat, 0.3f);
            Chain(start, a, peak, after, Add(MapNodeType.Combat, 0.3f), Add(MapNodeType.Boss, 1f), Add(MapNodeType.Exit, 0f));

            LevelLayout layout = Placed(Plan(), (a, 11), (peak, 10), (after, 10));
            var settings = ScriptableObject.CreateInstance<MapDirectorSettings>();
            try { MapDirector.MovePeaksToBiggerRooms(_graph, layout, settings); }
            finally { Object.DestroyImmediate(settings); }

            Assert.That(layout.Rooms[peak].Beat, Is.EqualTo(RoomBeat.Peak));
            Assert.That(layout.Rooms[after].Beat, Is.EqualTo(RoomBeat.Relief));
            Assert.That(layout.DirectorFindings, Is.Empty);
        }

        // Square rooms of the given sides for the given nodes, with the planned beats.
        private LevelLayout Placed(Dictionary<int, RoomBeat> beats, params (int id, int side)[] rooms)
        {
            var layout = new LevelLayout(1f);
            int x = 0;
            foreach ((int id, int side) in rooms)
            {
                _graph.TryGetNode(id, out MapNode node);
                layout.AddRoom(new LevelRoom(node, RoomFootprint.Rectangle(new RectInt(x, 0, side, side)), null, 4f, beat: beats[id]));
                x += side + 4;
            }
            return layout;
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
