using UnityEngine;

namespace CGD.Enemies
{
    // The floor round where a target was lost, as a grid of square cells for an occupancy map
    // (OccupancyMap): which cells someone could stand in, how high their floor is, and which
    // neighbouring cells are joined (a wall between two cells keeps them apart). Built from the
    // NavMesh by OccupancyGridBuilder; plain data, so the map's rules can be tested without it.
    internal sealed class OccupancyGrid
    {
        private readonly bool[] _walkable;
        private readonly float[] _heights;
        // Whether a cell joins the one to its east (+x) and to its north (+z).
        private readonly bool[] _openEast;
        private readonly bool[] _openNorth;

        // `origin`: the corner of cell (0, 0) at the lowest x and z; the arrays are row by row,
        // x fastest. Null heights = every floor at the origin's height.
        public OccupancyGrid(Vector3 origin, float cellSize, int width, int depth, bool[] walkable,
                             bool[] openEast, bool[] openNorth, float[] heights = null)
        {
            Origin     = origin;
            CellSize   = cellSize;
            Width      = width;
            Depth      = depth;
            _walkable  = walkable;
            _openEast  = openEast;
            _openNorth = openNorth;
            _heights   = heights;
        }

        public Vector3 Origin   { get; }
        public float   CellSize { get; }
        public int     Width    { get; }
        public int     Depth    { get; }
        public int     Count    => Width * Depth;

        public bool Walkable(int cell) => cell >= 0 && cell < Count && _walkable[cell];

        // The middle of a cell, on its floor.
        public Vector3 Center(int cell)
        {
            int x = cell % Width, z = cell / Width;
            float y = _heights != null ? _heights[cell] : Origin.y;
            return new Vector3(Origin.x + (x + 0.5f) * CellSize, y, Origin.z + (z + 0.5f) * CellSize);
        }

        // The cell a point lies over, or false outside the grid.
        public bool TryCell(Vector3 point, out int cell)
        {
            int x = Mathf.FloorToInt((point.x - Origin.x) / CellSize);
            int z = Mathf.FloorToInt((point.z - Origin.z) / CellSize);
            cell = x + z * Width;
            return x >= 0 && z >= 0 && x < Width && z < Depth;
        }

        // The walkable cells joined to `cell`, written into `into` (room for four); how many.
        public int Neighbours(int cell, int[] into)
        {
            int count = 0, x = cell % Width;
            if (!Walkable(cell)) return 0;
            if (x + 1 < Width        && _openEast[cell]          && Walkable(cell + 1))     into[count++] = cell + 1;
            if (x > 0                && _openEast[cell - 1]      && Walkable(cell - 1))     into[count++] = cell - 1;
            if (cell + Width < Count && _openNorth[cell]         && Walkable(cell + Width)) into[count++] = cell + Width;
            if (cell - Width >= 0    && _openNorth[cell - Width] && Walkable(cell - Width)) into[count++] = cell - Width;
            return count;
        }
    }
}
