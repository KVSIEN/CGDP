using NUnit.Framework;
using UnityEngine;
using CGD.Enemies;

namespace CGD.Tests
{
    // The occupancy map a hunting enemy searches by (OccupancyMap, OccupancySearch): the chance
    // of where a lost target is starts where it was last known and leans the way it was going,
    // spreads along the floor but not through walls, falls as cells are seen empty, and is
    // shared out so two searchers head for different places.
    public class OccupancySearchTests
    {
        private const float Cell = 1f;
        private const int Width = 12, Depth = 6;

        // An open floor of 12 × 6 cells; `wallAtX` puts a wall between column x-1 and x, with an
        // opening at `gapZ` (-1 = none).
        private static OccupancyGrid Floor(int wallAtX = -1, int gapZ = -1)
        {
            int count = Width * Depth;
            var walkable = new bool[count];
            var east = new bool[count];
            var north = new bool[count];
            for (int cell = 0; cell < count; cell++)
            {
                walkable[cell] = true;
                int x = cell % Width, z = cell / Width;
                east[cell]  = !(x + 1 == wallAtX && z != gapZ);
                north[cell] = true;
            }
            return new OccupancyGrid(Vector3.zero, Cell, Width, Depth, walkable, east, north);
        }

        private static Vector3 At(int x, int z) => new((x + 0.5f) * Cell, 0f, (z + 0.5f) * Cell);
        private static int CellAt(int x, int z) => x + z * Width;

        private static float Total(OccupancyMap map)
        {
            float total = 0f;
            for (int cell = 0; cell < map.Grid.Count; cell++) total += map.Chance(cell);
            return total;
        }

        [Test]
        public void TheChanceStartsWhereTheTargetWasLostAndSpreadsWithoutBeingLost()
        {
            var map = new OccupancyMap(Floor());
            Assert.That(map.Seed(At(3, 3), Vector3.zero), Is.True);
            Assert.That(map.Chance(CellAt(3, 3)), Is.EqualTo(1f));
            Assert.That(map.Remaining, Is.EqualTo(1f));

            map.Diffuse(0.3f);
            Assert.That(map.Chance(CellAt(3, 3)), Is.EqualTo(0.7f).Within(1e-5f));
            Assert.That(map.Chance(CellAt(4, 3)), Is.GreaterThan(0f));
            Assert.That(map.Chance(CellAt(5, 3)), Is.Zero, "one step reaches only the neighbours");
            for (int i = 0; i < 20; i++) map.Diffuse(0.3f);
            Assert.That(Total(map), Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void TheChanceLeansTheWayTheTargetWasHeading()
        {
            var map = new OccupancyMap(Floor());
            map.Seed(At(5, 3), Vector3.right);

            Assert.That(map.Chance(CellAt(6, 3)) + map.Chance(CellAt(7, 3)) + map.Chance(CellAt(8, 3)), Is.GreaterThan(0.3f));
            Assert.That(map.Chance(CellAt(4, 3)), Is.Zero);
            Assert.That(Total(map), Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void AWallKeepsTheChanceOnItsSideAndAnOpeningLetsItThrough()
        {
            var walled = new OccupancyMap(Floor(wallAtX: 6));
            walled.Seed(At(5, 3), Vector3.zero);
            for (int i = 0; i < 30; i++) walled.Diffuse(0.3f);
            for (int z = 0; z < Depth; z++)
                for (int x = 6; x < Width; x++)
                    Assert.That(walled.Chance(CellAt(x, z)), Is.Zero, $"cell {x},{z}");

            var opened = new OccupancyMap(Floor(wallAtX: 6, gapZ: 0));
            opened.Seed(At(5, 3), Vector3.zero);
            for (int i = 0; i < 30; i++) opened.Diffuse(0.3f);
            Assert.That(opened.Chance(CellAt(6, 0)), Is.GreaterThan(0f));
            Assert.That(opened.Chance(CellAt(6, 3)), Is.GreaterThan(0f), "round through the opening");
        }

        [Test]
        public void SeeingCellsEmptyLowersWhatIsLeftUntilTheSearchGivesUp()
        {
            var hunt = new OccupancySearch(Floor(), At(3, 3), Vector3.zero, now: 0f);
            hunt.Advance(3f);
            Assert.That(hunt.IsExhausted, Is.False);

            hunt.Map.ClearAround(At(3, 3), 1.5f);
            Assert.That(hunt.Map.Remaining, Is.LessThan(1f));
            for (int cell = 0; cell < hunt.Map.Grid.Count; cell++) hunt.Map.Clear(cell);
            Assert.That(hunt.IsExhausted, Is.True);
            Assert.That(hunt.TryNext(this, At(0, 0), out _), Is.False);
        }

        [Test]
        public void TheNextSpotIsTheLikeliestAndAHidingPlaceCountsExtra()
        {
            var map = new OccupancyMap(Floor());
            map.Seed(At(8, 3), Vector3.zero);
            for (int i = 0; i < 4; i++) map.Diffuse(0.3f);

            Assert.That(map.TryBest(At(0, 3), 6f, 0.5f, out int best), Is.True);
            Assert.That(best, Is.EqualTo(CellAt(8, 3)));

            map.Favour(At(8, 1), 50f);
            Assert.That(map.TryBest(At(0, 3), 6f, 0.5f, out best), Is.True);
            Assert.That(best, Is.EqualTo(CellAt(8, 1)));
        }

        [Test]
        public void SpreadingFollowsTheClockSoSharingSearchersNeverDoubleIt()
        {
            var hunt = new OccupancySearch(Floor(), At(3, 3), Vector3.zero, now: 10f);
            hunt.Advance(10f + OccupancySearch.StepSeconds * 2.5f);
            float afterTwo = hunt.Map.Chance(CellAt(3, 3));
            hunt.Advance(10f + OccupancySearch.StepSeconds * 2.5f);

            Assert.That(hunt.Map.Chance(CellAt(3, 3)), Is.EqualTo(afterTwo));
            Assert.That(afterTwo, Is.EqualTo(0.7f * 0.7f + 0f).Within(0.1f));
        }

        [Test]
        public void TwoSearchersHeadForDifferentPlaces()
        {
            var hunt = new OccupancySearch(Floor(), At(5, 3), Vector3.zero, now: 0f);
            hunt.Advance(6f);
            object first = new(), second = new();

            Assert.That(hunt.TryNext(first, At(0, 3), out Vector3 a), Is.True);
            Assert.That(hunt.TryNext(second, At(0, 3), out Vector3 b), Is.True);
            Assert.That(Vector3.Distance(a, b), Is.GreaterThanOrEqualTo(3f));

            hunt.Release(first);
            Assert.That(hunt.TryNext(second, At(0, 3), out Vector3 again), Is.True);
            Assert.That(again, Is.EqualTo(a), "with the first gone, its spot is the best again");
        }

        [Test]
        public void ANewLeadFarAwayStartsTheMapOver()
        {
            var hunt = new OccupancySearch(Floor(), At(1, 1), Vector3.zero, now: 0f);
            hunt.Follow(At(2, 1), Vector3.zero, 1f);
            Assert.That(hunt.SeededAt, Is.EqualTo(At(1, 1)), "close by: the same hunt");

            hunt.Follow(At(10, 4), Vector3.zero, 2f);
            Assert.That(hunt.SeededAt, Is.EqualTo(At(10, 4)));
            Assert.That(hunt.Map.Chance(CellAt(10, 4)), Is.EqualTo(1f));
            Assert.That(hunt.Covers(At(11, 5)), Is.True);
            Assert.That(hunt.Covers(new Vector3(50f, 0f, 0f)), Is.False);
        }
    }
}
