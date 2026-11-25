using System.Collections.Generic;
using UnityEngine;
using CGD.Algorithms;
using CGD.Core;

namespace CGD.Level
{
    // Plans a room's overgrowth (OvergrowthField) from its profile, in four steps over a grid of
    // samples:
    //   1. Pin: every sample touching a tile that stays bare (OvergrowthGround), or a tile the
    //      growth may not take, is held at 0.
    //   2. Reach: how far the growth has spread to each sample: thickest at the hearts of a few
    //      patches where it broke through and, by the profile's wall pile, along the walls.
    //   3. Shape: where the reach, broken up by noise, passes the profile's coverage, the floor is
    //      overgrown, its edge feathered and its top rolling in lumps (OvergrowthNoise).
    //   4. Slope: everything is lowered until no bank is steeper than the profile allows
    //      (GridSlopeLimit), so the growth always ramps down gently to the bare floor.
    // Plain planning: the same structure, request and random numbers give the same field.
    public sealed class OvergrowthPlanner
    {
        // Metres between samples, about: fine enough for lumps and a smooth bank.
        private const float SampleSpacing = 0.5f;
        // Tiles out from a wall that the growth piled against it reaches.
        private const float WallReach = 1.5f;
        // How much the noise breaks up the reach: 0 gives clean round patches, 1 plain noise.
        private const float Breakup = 0.4f;
        // How much more reach than the coverage asks for takes the growth from nothing to full height.
        private const float Feather = 0.25f;
        // The share of its full height the growth has in a lump's lowest dip.
        private const float FlattestLump = 0.35f;
        // Reach the coverage needs: at 0 only the strongest hearts pass, at 1 nearly everything does.
        private const float SparseThreshold = 0.85f;
        private const float DenseThreshold  = 0.05f;
        // Thinner than this is no growth at all.
        private const float Thinnest = 0.005f;

        private readonly float _tileSize;

        public OvergrowthPlanner(float tileSize) => _tileSize = tileSize;

        // How many samples run along a tile's side.
        public int SamplesPerTile => Mathf.Max(2, Mathf.RoundToInt(_tileSize / SampleSpacing));

        // Plans every room the source overgrows and gives it the result (LevelRoom.Overgrowth). Each
        // room draws from its own layer of `seed`, so what grows in one never moves another's.
        public void PlanLevel(LevelLayout layout, OvergrowthSource source, Seed seed)
        {
            foreach (LevelRoom room in layout.Rooms.Values)
                if (source.TryGet(room, out OvergrowthRequest request))
                    room.AttachOvergrowth(Plan(room.Structure, request, seed.Derive(room.Node.Id).Stream()));
        }

        // null when nothing grows (no profile, or no floor it may take).
        public OvergrowthField Plan(RoomStructure structure, OvergrowthRequest request, RandomStream random)
        {
            OvergrowthProfile profile = request.Profile;
            if (structure == null || profile == null) return null;

            var grid = new SampleGrid(structure.Footprint.Bounds, SamplesPerTile);
            List<Vector2Int> growing = GrowingTiles(structure, request, new OvergrowthGround(structure, profile.ClearWalkways));
            if (growing.Count == 0) return null;

            bool[] pinned = Pin(structure, grid, new HashSet<Vector2Int>(growing));
            var noise = new OvergrowthNoise(profile.LumpSize, profile.Detail, profile.Warp, random);
            List<Patch> patches = PlacePatches(growing, profile, random);
            float[] wallDistance = WallDistance(structure, grid);

            float threshold = Mathf.Lerp(SparseThreshold, DenseThreshold, profile.Coverage * request.Strength);
            var heights = new float[grid.Count];
            for (int index = 0; index < grid.Count; index++)
            {
                if (pinned[index]) continue;

                Vector2 point = grid.PointOf(index);
                float reach = Mathf.Max(PatchReach(patches, point), profile.WallPile * (1f - Mathf.Clamp01(wallDistance[index] / WallReach)));
                float lump = noise.Sample(point * _tileSize);
                float presence = Mathf.Lerp(reach, lump, Breakup);
                float edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(threshold, threshold + Feather, presence));
                float height = profile.Height * edge * Mathf.Lerp(FlattestLump, 1f, lump);
                heights[index] = height < Thinnest ? 0f : height;
            }

            // Last, so nothing after it can make a bank steeper again.
            float rise = Mathf.Tan(profile.MaxSlope * Mathf.Deg2Rad) * (_tileSize / grid.SamplesPerTile);
            GridSlopeLimit.Apply(heights, grid.Columns, grid.Rows, rise);
            return System.Array.Exists(heights, height => height > 0f) ? new OvergrowthField(profile, grid.Tiles, grid.SamplesPerTile, heights) : null;
        }

        // The tiles it may grow on: the growth's part of the room, less what stays bare. In the
        // footprint's order, so the patches land the same way every time.
        private static List<Vector2Int> GrowingTiles(RoomStructure structure, OvergrowthRequest request, OvergrowthGround ground)
        {
            var tiles = new List<Vector2Int>();
            foreach (Vector2Int tile in structure.Footprint.Tiles)
                if (request.Grows(tile) && !ground.StaysBare(tile)) tiles.Add(tile);
            return tiles;
        }

        // A sample is held at 0 when any of the room's tiles it touches doesn't grow, so tiles on
        // two levels never share a raised sample, and when it touches none of them at all. Tiles
        // outside the room don't pin it: the room's edge against its walls is where growth piles up.
        private static bool[] Pin(RoomStructure structure, SampleGrid grid, HashSet<Vector2Int> growing)
        {
            var pinned = new bool[grid.Count];
            var touching = new List<Vector2Int>(4);
            for (int index = 0; index < grid.Count; index++)
            {
                grid.TilesTouching(index, touching);
                bool inRoom = false;
                foreach (Vector2Int tile in touching)
                {
                    if (!structure.Contains(tile)) continue;
                    inRoom = true;
                    if (!growing.Contains(tile)) pinned[index] = true;
                }
                if (!inRoom) pinned[index] = true;
            }
            return pinned;
        }

        // Each sample's distance, in tiles, to the room's walls: to the nearest sample touching a
        // tile outside the room.
        private static float[] WallDistance(RoomStructure structure, SampleGrid grid)
        {
            var distance = new float[grid.Count];
            var touching = new List<Vector2Int>(4);
            for (int index = 0; index < grid.Count; index++)
            {
                grid.TilesTouching(index, touching);
                distance[index] = TouchesOutside(structure, touching) ? 0f : float.PositiveInfinity;
            }

            GridSlopeLimit.Apply(distance, grid.Columns, grid.Rows, 1f);
            for (int index = 0; index < distance.Length; index++) distance[index] /= grid.SamplesPerTile;
            return distance;
        }

        private static bool TouchesOutside(RoomStructure structure, List<Vector2Int> tiles)
        {
            foreach (Vector2Int tile in tiles)
                if (!structure.Contains(tile)) return true;
            return false;
        }

        // Where the growth broke through: on tiles it may take, each with its own reach.
        private static List<Patch> PlacePatches(List<Vector2Int> growing, OvergrowthProfile profile, RandomStream random)
        {
            int count = Mathf.Max(0, profile.Patches.Evaluate(random));
            var patches = new List<Patch>(count);
            for (int i = 0; i < count; i++)
            {
                Vector2Int tile = random.Pick(growing);
                float radius = Mathf.Max(0.5f, profile.PatchRadius.Evaluate(random));
                patches.Add(new Patch(tile + new Vector2(0.5f, 0.5f), radius));
            }
            return patches;
        }

        // 1 at the heart of the nearest patch, easing to 0 at its edge.
        private static float PatchReach(List<Patch> patches, Vector2 point)
        {
            float reach = 0f;
            foreach (Patch patch in patches)
                reach = Mathf.Max(reach, Mathf.SmoothStep(0f, 1f, 1f - Vector2.Distance(point, patch.Heart) / patch.Radius));
            return reach;
        }

        private readonly struct Patch
        {
            public Patch(Vector2 heart, float radius)
            {
                Heart  = heart;
                Radius = radius;
            }

            public Vector2 Heart  { get; }   // tile units
            public float   Radius { get; }   // tiles
        }

        // The planner's grid: OvergrowthField's layout, before it has heights.
        private readonly struct SampleGrid
        {
            public SampleGrid(RectInt tiles, int samplesPerTile)
            {
                Tiles          = tiles;
                SamplesPerTile = samplesPerTile;
                Columns        = tiles.width * samplesPerTile + 1;
                Rows           = tiles.height * samplesPerTile + 1;
            }

            public RectInt Tiles          { get; }
            public int     SamplesPerTile { get; }
            public int     Columns        { get; }
            public int     Rows           { get; }
            public int     Count => Columns * Rows;

            // The sample's position in tile units.
            public Vector2 PointOf(int index) =>
                Tiles.min + new Vector2(index % Columns, index / Columns) / SamplesPerTile;

            // The one to four tiles a sample lies in or on the edge of: two along an edge, four on a corner.
            public void TilesTouching(int index, List<Vector2Int> tiles)
            {
                tiles.Clear();
                int column = index % Columns, row = index / Columns;
                int xHigh = column / SamplesPerTile, yHigh = row / SamplesPerTile;
                int xLow = column % SamplesPerTile == 0 ? xHigh - 1 : xHigh;
                int yLow = row % SamplesPerTile == 0 ? yHigh - 1 : yHigh;
                for (int y = yLow; y <= yHigh; y++)
                    for (int x = xLow; x <= xHigh; x++)
                        tiles.Add(Tiles.min + new Vector2Int(x, y));
            }
        }
    }
}
