using UnityEngine;

namespace CGD.Level
{
    // How high a room's overgrowth stands, in metres above the floor under it: a grid of samples
    // over the room's bounds, SamplesPerTile to a tile's side, with a sample on every tile corner
    // so neighbouring tiles share their edge. Between samples the height is blended. Each
    // height is above its own tile's floor, raised, sunken or flat, and a sample shared by two
    // tiles on different levels is always 0 (OvergrowthGround keeps both sides of a ledge bare).
    // Planned once by OvergrowthPlanner; read by the geometry and by whatever stands on it.
    public sealed class OvergrowthField
    {
        private readonly float[] _heights;

        public OvergrowthField(OvergrowthProfile profile, RectInt tiles, int samplesPerTile, float[] heights)
        {
            Profile        = profile;
            Tiles          = tiles;
            SamplesPerTile = samplesPerTile;
            Columns        = tiles.width * samplesPerTile + 1;
            Rows           = tiles.height * samplesPerTile + 1;
            if (heights.Length != Columns * Rows)
                throw new System.ArgumentException($"Expected {Columns * Rows} heights, got {heights.Length}.", nameof(heights));
            _heights = heights;
        }

        // What grew it (its look).
        public OvergrowthProfile Profile { get; }
        // The tiles the grid covers (the room's bounds) and how finely.
        public RectInt Tiles          { get; }
        public int     SamplesPerTile { get; }
        public int     Columns        { get; }
        public int     Rows           { get; }

        public float HeightAt(int column, int row) =>
            column < 0 || row < 0 || column >= Columns || row >= Rows ? 0f : _heights[row * Columns + column];

        // At a point in tile units (tile (x, y) covers x..x+1, y..y+1); 0 off the grid.
        public float HeightAt(Vector2 point)
        {
            float u = (point.x - Tiles.xMin) * SamplesPerTile, v = (point.y - Tiles.yMin) * SamplesPerTile;
            if (u < 0f || v < 0f || u > Columns - 1 || v > Rows - 1) return 0f;

            int column = Mathf.Min(Mathf.FloorToInt(u), Columns - 2), row = Mathf.Min(Mathf.FloorToInt(v), Rows - 2);
            float s = u - column, t = v - row;
            float below = Mathf.Lerp(HeightAt(column, row), HeightAt(column + 1, row), s);
            float above = Mathf.Lerp(HeightAt(column, row + 1), HeightAt(column + 1, row + 1), s);
            return Mathf.Lerp(below, above, t);
        }

        // The grid position of a tile's south-west corner.
        public Vector2Int CornerOf(Vector2Int tile) => (tile - Tiles.min) * SamplesPerTile;

        // Whether any of the tile, its edges included, is overgrown.
        public bool Covers(Vector2Int tile)
        {
            if (!Tiles.Contains(tile)) return false;

            Vector2Int corner = CornerOf(tile);
            for (int row = 0; row <= SamplesPerTile; row++)
                for (int column = 0; column <= SamplesPerTile; column++)
                    if (HeightAt(corner.x + column, corner.y + row) > 0f) return true;
            return false;
        }
    }
}
