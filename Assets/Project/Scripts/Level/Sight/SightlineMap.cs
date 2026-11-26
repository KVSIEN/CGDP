using UnityEngine;

namespace CGD.Level
{
    // How far a person can see along each row and column of a room: the length of the open
    // stretch of floor through every tile, stopped by whatever SightBlocking says stops sight
    // at its eye height (props lower than the eye, and ledges lower than it, are seen over).
    // Kept current as props go down, so the planner can score any tile at once (0 on a tile
    // nobody can stand on).
    internal sealed class SightlineMap
    {
        private readonly SightBlocking _blocking;
        private readonly RectInt _bounds;
        private readonly TileGrid _grid;
        private readonly int[] _runX;
        private readonly int[] _runY;

        // `blocking`: the room's sight rules, when something else already holds them (standing, if not).
        public SightlineMap(RoomStructure structure, SightBlocking blocking = null)
        {
            _blocking  = blocking ?? new SightBlocking(structure);
            _bounds    = structure.Footprint.Bounds;
            _grid      = new TileGrid(_bounds);
            _runX      = new int[_grid.Count];
            _runY      = new int[_grid.Count];
            Refresh(_bounds);
        }

        public SightBlocking Blocking => _blocking;

        // The open stretch through a tile along x and along y, the tile included.
        public (int alongX, int alongY) Runs(Vector2Int tile)
        {
            int index = _grid.IndexOf(tile);
            return (_runX[index], _runY[index]);
        }

        public int LongestRun(Vector2Int tile)
        {
            (int alongX, int alongY) = Runs(tile);
            return Mathf.Max(alongX, alongY);
        }

        // After props take floor, the rows and columns they stand in are measured again.
        public void Refresh(RectInt changed)
        {
            for (int y = changed.yMin; y < changed.yMax; y++)
                MeasureLine(new Vector2Int(_bounds.xMin, y), Vector2Int.right, _bounds.width, _runX);
            for (int x = changed.xMin; x < changed.xMax; x++)
                MeasureLine(new Vector2Int(x, _bounds.yMin), Vector2Int.up, _bounds.height, _runY);
        }

        // Gives every tile of a row or column the length of the unbroken stretch of see-through
        // floor it is in; a wall, a pillar, a prop or inner wall as tall as the eye, or a ledge as
        // high as it, ends a stretch.
        private void MeasureLine(Vector2Int origin, Vector2Int step, int length, int[] runs)
        {
            int start = 0;
            for (int i = 0; i <= length; i++)
            {
                Vector2Int tile = origin + step * i;
                bool open = i < length && !_blocking.BlocksTile(tile);
                bool cut = open && i > start && _blocking.BlocksEdge(tile - step, tile);
                if (open && !cut) continue;

                // A low prop is seen over, so it doesn't end the stretch, but nobody stands on it.
                for (int j = start; j < i; j++)
                {
                    Vector2Int inside = origin + step * j;
                    runs[_grid.IndexOf(inside)] = _blocking.IsTaken(inside) ? 0 : i - start;
                }
                if (!open && i < length) runs[_grid.IndexOf(tile)] = 0;
                start = open ? i : i + 1;
            }
        }
    }
}
