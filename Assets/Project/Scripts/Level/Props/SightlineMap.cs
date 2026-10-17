using UnityEngine;

namespace CGD.Level
{
    // How far a person can see along each row and column of a room: the length of the open
    // stretch of floor through every tile. Kept current as props go down, so the planner can
    // score any tile at once (0 on a tile that is blocked).
    internal sealed class SightlineMap
    {
        public const RoomTileTags BlocksSight = RoomTileTags.Structure | RoomTileTags.Prop;

        private readonly RoomStructure _structure;
        private readonly RectInt _bounds;
        private readonly int[] _runX;
        private readonly int[] _runY;

        public SightlineMap(RoomStructure structure)
        {
            _structure = structure;
            _bounds    = structure.Footprint.Bounds;
            _runX      = new int[_bounds.width * _bounds.height];
            _runY      = new int[_bounds.width * _bounds.height];
            Refresh(_bounds);
        }

        // The open stretch through a tile along x and along y, the tile included.
        public (int alongX, int alongY) Runs(Vector2Int tile)
        {
            int index = (tile.y - _bounds.yMin) * _bounds.width + (tile.x - _bounds.xMin);
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
        // floor it is in; a wall, pillar, prop or inner wall ends a stretch.
        private void MeasureLine(Vector2Int origin, Vector2Int step, int length, int[] runs)
        {
            int start = 0;
            for (int i = 0; i <= length; i++)
            {
                Vector2Int tile = origin + step * i;
                bool open = i < length && _structure.Footprint.Contains(tile) && !_structure.Has(tile, BlocksSight);
                bool cut = open && i > start && _structure.IsPartitioned(tile - step, tile);
                if (open && !cut) continue;

                for (int j = start; j < i; j++)
                {
                    Vector2Int inside = origin + step * j;
                    runs[(inside.y - _bounds.yMin) * _bounds.width + (inside.x - _bounds.xMin)] = i - start;
                }
                if (!open && i < length) runs[(tile.y - _bounds.yMin) * _bounds.width + (tile.x - _bounds.xMin)] = 0;
                start = open ? i : i + 1;
            }
        }
    }
}
