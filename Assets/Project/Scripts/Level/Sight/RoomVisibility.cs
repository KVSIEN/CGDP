using UnityEngine;

namespace CGD.Level
{
    // Who can see whom in a room: for every pair of open floor tiles, whether a straight line
    // between their middles is clear. Unlike SightlineMap, which sees along rows and columns
    // only, this sees in every direction, so a diagonal lane, a round room's open middle and
    // the shadow behind a pillar all read as they play.
    //
    // A line is clear when it crosses only open floor (what SightBlocking says) and no inner
    // wall tall enough to hide behind. A line through the exact corner where two tiles meet
    // passes if either of the two ways round the corner is open, so it can graze a pillar but
    // not squeeze between two. Lines are symmetric: a sees b exactly when b sees a. A tile
    // that is blocked sees nothing and is seen by none. Nothing beyond `range` tiles is seen
    // (square distance, so a hangar's far wall doesn't cost a room-sized matrix of lines).
    //
    // The answers are kept as a bit matrix, one row of bits per tile, so asking is cheap and
    // the rows' sizes (Area) and farthest sight (Reach) are at hand. Nothing is walked until
    // something asks, and then only what changed: tell it which floor changed (Invalidate) when
    // props go down or come away, and the next question walks again just the lines that touch it.
    internal sealed class RoomVisibility
    {
        public const int DefaultRange = 32;
        // Lines are tested against a changed rectangle this much bigger, so one grazing its edge is still walked again.
        private const float TouchSlack = 0.01f;

        private readonly RoomStructure _structure;
        private readonly SightBlocking _blocking;
        private readonly RectInt _bounds;
        private readonly int _range;
        private readonly int _words;
        private readonly ulong[] _rows;
        private readonly int[] _area;
        private readonly float[] _reach;
        private readonly bool[] _reachKnown;
        // The floor changed since the lines were last walked (all of it, to begin with).
        private bool _stale = true;
        private RectInt _changed;

        // `blocking`: the room's sight rules, when something else already holds them.
        public RoomVisibility(RoomStructure structure, SightBlocking blocking = null, int range = DefaultRange)
        {
            _structure = structure;
            _blocking  = blocking ?? new SightBlocking(structure);
            _bounds    = structure.Footprint.Bounds;
            _range     = Mathf.Max(0, range);

            int tiles   = _bounds.width * _bounds.height;
            _words      = (tiles + 63) >> 6;
            _rows       = new ulong[tiles * _words];
            _area       = new int[tiles];
            _reach      = new float[tiles];
            _reachKnown = new bool[tiles];
            _changed    = _bounds;
        }

        // Whether two tiles see each other (a tile sees itself, if it is open).
        public bool Sees(Vector2Int a, Vector2Int b)
        {
            if (!_bounds.Contains(a) || !_bounds.Contains(b)) return false;

            Sync();
            return Get(IndexOf(a), IndexOf(b));
        }

        // How many tiles a tile sees, itself included: the area of its isovist. 0 for a blocked tile.
        public int Area(Vector2Int tile)
        {
            if (!_bounds.Contains(tile)) return 0;

            Sync();
            return _area[IndexOf(tile)];
        }

        // How far, in tiles, the farthest tile it sees lies. 0 for a blocked tile.
        public float Reach(Vector2Int tile)
        {
            if (!_bounds.Contains(tile)) return 0f;

            Sync();
            int index = IndexOf(tile);
            if (_reachKnown[index]) return _reach[index];

            _reach[index] = Farthest(tile, out _);
            _reachKnown[index] = true;
            return _reach[index];
        }

        // The farthest tile a tile sees (the first of any that tie), and how far it lies. A blocked
        // tile, or one outside the room, sees none: 0, and itself.
        public float Farthest(Vector2Int tile, out Vector2Int farthest)
        {
            farthest = tile;
            if (!_bounds.Contains(tile)) return 0f;

            Sync();
            int row = IndexOf(tile) * _words;
            int best = 0;
            for (int word = 0; word < _words; word++)
            {
                ulong bits = _rows[row + word];
                for (int bit = 0; bits != 0; bit++, bits >>= 1)
                {
                    if ((bits & 1UL) == 0) continue;

                    Vector2Int other = TileAt(word * 64 + bit);
                    Vector2Int apart = other - tile;
                    int distance = apart.x * apart.x + apart.y * apart.y;
                    if (distance <= best) continue;
                    best = distance;
                    farthest = other;
                }
            }
            return Mathf.Sqrt(best);
        }

        // Says that tiles in `changed` were taken or freed since the lines were last walked. Cheap:
        // the lines are walked again when next asked, all the changes at once.
        public void Invalidate(RectInt changed)
        {
            if (_stale)
            {
                int xMin = Mathf.Min(_changed.xMin, changed.xMin), yMin = Mathf.Min(_changed.yMin, changed.yMin);
                int xMax = Mathf.Max(_changed.xMax, changed.xMax), yMax = Mathf.Max(_changed.yMax, changed.yMax);
                _changed = new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
            }
            else
            {
                _changed = changed;
            }
            _stale = true;
        }

        // Walks again every line that touches the changed floor, and sets those tiles' own sight.
        // Nothing outside it can have changed: a line that doesn't touch it crosses none of its tiles.
        private void Sync()
        {
            if (!_stale) return;

            _stale = false;
            Walk(_changed);
        }

        private void Walk(RectInt changed)
        {
            var tiles = _structure.Footprint.Tiles;
            bool everything = changed.xMin <= _bounds.xMin && changed.yMin <= _bounds.yMin && changed.xMax >= _bounds.xMax && changed.yMax >= _bounds.yMax;
            var open = new bool[tiles.Count];
            for (int i = 0; i < tiles.Count; i++) open[i] = !_blocking.BlocksTile(tiles[i]);

            for (int i = 0; i < tiles.Count; i++)
            {
                Vector2Int a = tiles[i];
                if (changed.Contains(a)) Set(IndexOf(a), IndexOf(a), open[i]);

                for (int j = i + 1; j < tiles.Count; j++)
                {
                    Vector2Int b = tiles[j];
                    if (!everything && !Touches(a, b, changed)) continue;

                    Set(IndexOf(a), IndexOf(b), open[i] && open[j] && InRange(a, b) && _blocking.CanSee(a, b));
                }
            }
        }

        private bool InRange(Vector2Int a, Vector2Int b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y)) <= _range;

        // Whether the segment between two tiles' middles touches a rectangle of tiles (Liang–Barsky clipping).
        // Generous at the edges: a line is never missed, only sometimes walked again for nothing.
        private static bool Touches(Vector2Int a, Vector2Int b, RectInt rect)
        {
            // Most lines lie wholly to one side of it: ruled out by comparing whole tiles, before any clipping.
            if (Mathf.Max(a.x, b.x) < rect.xMin || Mathf.Min(a.x, b.x) >= rect.xMax) return false;
            if (Mathf.Max(a.y, b.y) < rect.yMin || Mathf.Min(a.y, b.y) >= rect.yMax) return false;

            float startX = a.x + 0.5f, startY = a.y + 0.5f;
            float dx = b.x - a.x, dy = b.y - a.y;
            float from = 0f, to = 1f;
            return Clip(-dx, startX - (rect.xMin - TouchSlack), ref from, ref to)
                && Clip(dx, rect.xMax + TouchSlack - startX, ref from, ref to)
                && Clip(-dy, startY - (rect.yMin - TouchSlack), ref from, ref to)
                && Clip(dy, rect.yMax + TouchSlack - startY, ref from, ref to);
        }

        // One edge of the rectangle: `slope` is how fast the segment moves toward it (negative = leaving
        // the inside), `room` how far inside it starts. Narrows the stretch of segment still inside.
        private static bool Clip(float slope, float room, ref float from, ref float to)
        {
            if (slope == 0f) return room >= 0f;

            float at = room / slope;
            if (slope < 0f)
            {
                if (at > to) return false;
                if (at > from) from = at;
            }
            else
            {
                if (at < from) return false;
                if (at < to) to = at;
            }
            return true;
        }

        // --- The matrix -----------------------------------------------------------------

        private int IndexOf(Vector2Int tile) => (tile.y - _bounds.yMin) * _bounds.width + (tile.x - _bounds.xMin);

        private Vector2Int TileAt(int index) => new(_bounds.xMin + index % _bounds.width, _bounds.yMin + index / _bounds.width);

        private bool Get(int a, int b) => (_rows[a * _words + (b >> 6)] & (1UL << (b & 63))) != 0;

        // Sets what a sees of b and what b sees of a, and keeps the rows' sizes.
        private void Set(int a, int b, bool visible)
        {
            if (Get(a, b) == visible) return;

            Write(a, b, visible);
            if (a != b) Write(b, a, visible);
        }

        private void Write(int row, int column, bool visible)
        {
            ulong bit = 1UL << (column & 63);
            if (visible)
            {
                _rows[row * _words + (column >> 6)] |= bit;
                _area[row]++;
            }
            else
            {
                _rows[row * _words + (column >> 6)] &= ~bit;
                _area[row]--;
            }
            _reachKnown[row] = false;
        }
    }
}
