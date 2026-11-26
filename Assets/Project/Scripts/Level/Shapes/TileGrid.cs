using UnityEngine;

namespace CGD.Level
{
    // A rectangle of tiles (a room's footprint bounds, a level's) laid out in one array, row by
    // row: where each tile's slot is and whether a tile falls inside. Every per-tile array over
    // a room (its structure and floor levels, flow, sightlines, visibility, enemy posts) and the
    // corridor router index through one, so the layout is written once.
    public readonly struct TileGrid
    {
        private readonly int _xMin, _yMin, _width, _height;

        public TileGrid(RectInt bounds)
        {
            Bounds  = bounds;
            _xMin   = bounds.xMin;
            _yMin   = bounds.yMin;
            _width  = Mathf.Max(0, bounds.width);
            _height = Mathf.Max(0, bounds.height);
        }

        public RectInt Bounds { get; }
        // Slots in an array over the grid.
        public int Count => _width * _height;

        public bool Contains(Vector2Int tile) =>
            tile.x >= _xMin && tile.x < _xMin + _width && tile.y >= _yMin && tile.y < _yMin + _height;

        // Only meaningful for a tile the grid Contains.
        public int IndexOf(Vector2Int tile) => (tile.y - _yMin) * _width + (tile.x - _xMin);

        public Vector2Int TileAt(int index) => new(_xMin + index % _width, _yMin + index / _width);
    }
}
