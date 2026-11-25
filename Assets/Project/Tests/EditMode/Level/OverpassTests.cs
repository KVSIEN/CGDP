using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // Overpasses (elevation phase 5): a deck two levels over a lane, between walks a level below
    // it, with a flight up each end; the floor under it stays walkable, nothing is put there, and
    // it hides that floor from someone looking down across it.
    public class OverpassTests
    {
        // A 10 by 8 room entered from the east, with raised walks (level 1) along the south wall
        // (rows 0–1) and the north wall (rows 6–7, short of the doorway's front), each with its stairs.
        private static RoomStructure Walks(int highest = 2)
        {
            RoomStructure room = Room(10, 8, new Vector2Int(9, 4));
            room.AllowLevels(RoomElevation.Lowest, highest);
            var south = new ElevatedArea(ElevationKind.Terrace, 1, Block(0, 0, 10, 2), new[] { new StairFlight(new Vector2Int(3, 2), Vector2Int.down) }, railed: true);
            var north = new ElevatedArea(ElevationKind.Terrace, 1, Block(0, 6, 8, 2), new[] { new StairFlight(new Vector2Int(3, 5), Vector2Int.up) }, railed: true);
            Assert.That(room.TryAddElevations(new[] { south, north }), Is.True, "the walks fit");
            return room;
        }

        private static List<Vector2Int> Block(int x, int y, int width, int height)
        {
            var tiles = new List<Vector2Int>();
            for (int dy = 0; dy < height; dy++)
                for (int dx = 0; dx < width; dx++)
                    tiles.Add(new Vector2Int(x + dx, y + dy));
            return tiles;
        }

        // From the south walk at (6, 1) to the north walk at (6, 6): deck over (6, 2)–(6, 5).
        private static Overpass AcrossTheLane() => new(2, new Vector2Int(6, 1), new Vector2Int(6, 6));

        [Test]
        public void ADeckCrossesTheLaneAndTheFloorUnderItStaysWalkable()
        {
            RoomStructure room = Walks();
            Overpass overpass = AcrossTheLane();

            Assert.That(overpass.Deck, Is.EqualTo(new[] { new Vector2Int(6, 2), new Vector2Int(6, 3), new Vector2Int(6, 4), new Vector2Int(6, 5) }));
            Assert.That(room.TryAddOverpass(overpass), Is.True);

            Vector2Int under = new(6, 3);
            Assert.That(room.LevelOf(under), Is.Zero, "the floor stays where it was");
            Assert.That(room.CanStep(new Vector2Int(5, 3), under), Is.True, "and is walked under the deck");
            Assert.That(room.Has(under, RoomTileTags.Overpass), Is.True);
            Assert.That(room.IsFree(under), Is.False, "nothing is put under it");
            Assert.That(room.OverpassAbove(under), Is.SameAs(overpass));
            Assert.That(room.Has(new Vector2Int(6, 1), RoomTileTags.Structure), Is.True, "a flight's tile is solid");
            Assert.That(room.Has(new Vector2Int(6, 0), RoomTileTags.Landing), Is.True, "its landing is kept clear");
            Assert.That(room.Copy().Overpasses, Has.Count.EqualTo(1));
        }

        [Test]
        public void ADeckNeedsItsLevelTwoLevelsClearAndFreeFloor()
        {
            Assert.That(Walks(highest: 1).TryAddOverpass(AcrossTheLane()), Is.False, "level 2 not allowed");

            RoomStructure room = Walks();
            Assert.That(room.TryAddOverpass(new Overpass(2, new Vector2Int(1, 1), new Vector2Int(1, 6))), Is.True);
            Assert.That(room.TryAddOverpass(new Overpass(2, new Vector2Int(1, 1), new Vector2Int(1, 6))), Is.False, "not over another deck");
            Assert.That(room.TryAddOverpass(new Overpass(1, new Vector2Int(6, 1), new Vector2Int(6, 6))), Is.False, "a level 1 deck over level 0 has no clearance");
            Assert.That(room.TryAddOverpass(new Overpass(2, new Vector2Int(3, 1), new Vector2Int(3, 6))), Is.False, "not over the walks' stairs");
        }

        [Test]
        public void ADeckIsSeenUnderFromBelowAndHidesTheFloorFromAbove()
        {
            RoomStructure room = Walks();
            Assert.That(room.TryAddOverpass(AcrossTheLane()), Is.True);
            var standing = new SightBlocking(room);

            Assert.That(standing.CanSee(new Vector2Int(2, 3), new Vector2Int(9, 3)), Is.True, "a standing eye passes under the deck");
            Assert.That(standing.CanSee(new Vector2Int(6, 7), new Vector2Int(6, 4)), Is.False, "looking down across the deck");
        }

        [Test]
        public void TheRuleLaysADeckAcrossTheLaneBetweenTheWalks()
        {
            RoomStructure room = Walks();
            var rule = ScriptableObject.CreateInstance<OverpassStructureRule>();
            try
            {
                rule.Apply(room, Seed.From(1).Stream());

                Assert.That(room.Overpasses, Has.Count.EqualTo(1));
                Overpass overpass = room.Overpasses[0];
                Assert.That(overpass.Level, Is.EqualTo(2));
                Assert.That(Mathf.Abs(overpass.Along.y), Is.EqualTo(1), "straight across the lane");
                foreach (Vector2Int deck in overpass.Deck) Assert.That(room.LevelOf(deck), Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(rule);
            }
        }

        [Test]
        public void TheRuleLeavesAFlatRoomAloneWithoutDrawing()
        {
            RoomStructure room = Room(10, 8, new Vector2Int(9, 4));
            room.AllowLevels(RoomElevation.Lowest, RoomElevation.Highest);
            var rule = ScriptableObject.CreateInstance<OverpassStructureRule>();
            try
            {
                RandomStream random = Seed.From(1).Stream(), untouched = Seed.From(1).Stream();
                rule.Apply(room, random);

                Assert.That(room.Overpasses, Is.Empty);
                Assert.That(random.Range(0, 1000), Is.EqualTo(untouched.Range(0, 1000)), "no draw");
            }
            finally
            {
                Object.DestroyImmediate(rule);
            }
        }
    }
}
