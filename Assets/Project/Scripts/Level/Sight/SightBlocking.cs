using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // What stops a person seeing across a room, in one place so every way of reading sight
    // (the runs along rows and columns, a straight line over the floor plan, who sees whom)
    // agrees. Sight is read at an eye height above the floor (standing by default, or
    // crouching), and stopped by whatever stands taller than the line at that point:
    //   - the room's own walls and its pillars, always;
    //   - props as tall as their colliders (PropHeights; a prop of unknown height as tall as the
    //     room), so a standing eye sees over counters, crates and low walls and a crouching one
    //     hides behind them;
    //   - inner walls as tall as they are built (one of no height reaches the ceiling);
    //   - the floor itself: the face of a raised platform, the rim of a sunken floor, and the
    //     solid corner pieces round a mound or a crater, as high as the ground they climb to.
    // So an eye up on a platform sees over furniture that hides someone below it, and someone
    // crouched on a sunken floor is hidden by its rim.
    //
    // Tiles are asked live, so it follows props as they go down; inner walls and levels never
    // change once the room is built, so they are read once. CanSee walks a straight line
    // between two tiles over those rules: the one place a line of sight is traced.
    internal sealed class SightBlocking
    {
        // Tiles with one of these tags are stood on by nobody: no eye is ever there.
        public const RoomTileTags Tiles = RoomTileTags.Structure | RoomTileTags.Prop;
        // How high above the floor a standing and a crouching person's eyes are. Crouched, the
        // player is a 1 m body (PlayerMovementSettings.CrouchHeight) whose head enemies look for
        // 0.15 m below its top (EnemySenses), so cover that reaches 0.85 m hides them.
        public const float EyeHeight          = 1.6f;
        public const float CrouchingEyeHeight = 0.85f;

        private readonly RoomStructure _structure;
        private readonly float _eye;
        // Inner walls by the tiles either side (lower or left first) and the height they stand to.
        private readonly Dictionary<(Vector2Int, Vector2Int), float> _edges = new();
        // The solid corner pieces of mounds and craters, and how high their tops stand.
        private readonly Dictionary<Vector2Int, float> _cornerTops = new();

        // `eyeHeight`: how high the eyes are above the floor (EyeHeight standing, CrouchingEyeHeight crouched).
        public SightBlocking(RoomStructure structure, float eyeHeight = EyeHeight)
        {
            _structure = structure;
            _eye       = eyeHeight;
            // An edge walled twice stands as tall as the taller of the two.
            foreach (PartitionEdge edge in structure.Partitions)
            {
                var key = Ordered(edge.Tile, edge.Other);
                float height = edge.Height > 0f ? edge.Height : float.PositiveInfinity;
                _edges[key] = _edges.TryGetValue(key, out float before) ? Mathf.Max(before, height) : height;
            }
            foreach (ElevatedArea area in structure.Elevations)
            foreach (SlopeCorner corner in area.Corners)
                _cornerTops[corner.Tile] = HighestFloorBeside(corner.Tile);
        }

        // Nobody stands here: off the floor plan, or taken by a pillar or a prop.
        public bool IsTaken(Vector2Int tile) => !_structure.Contains(tile) || _structure.Has(tile, Tiles);

        // Sight along the floor stops at this tile: off the floor plan, a pillar, or a prop that
        // reaches the eye. A lower prop takes the tile (IsTaken) but is seen over.
        public bool BlocksTile(Vector2Int tile) => TopOf(tile) - FloorAt(tile) >= _eye;

        // Sight along the floor stops between two neighbouring tiles: an inner wall that reaches
        // the eye, or a change of level at least as big (a crouching eye doesn't see over a ledge).
        public bool BlocksEdge(Vector2Int a, Vector2Int b)
        {
            if (_structure.IsPartitioned(a, b) && WallHeight(a, b) >= _eye) return true;
            return Mathf.Abs(FloorAt(a) - FloorAt(b)) >= _eye;
        }

        // How high the floor of a tile is, in metres above the doorways' level: halfway up a flight
        // on a tile of stairs.
        public float FloorAt(Vector2Int tile)
        {
            if (_structure.TryGetStairs(tile, out StairFlight stairs))
                return (Level(stairs.Foot) + Level(stairs.Head)) * 0.5f;
            return Level(tile);
        }

        // Whether an eye in the middle of `from` sees an eye in the middle of `to`: the straight
        // line between them, at eye height over each one's floor, passes over everything in
        // between. `from` must be open (nobody stands inside a pillar); `to` may be taken, since
        // the face of a prop is seen like any other floor. Between two open tiles the answer is
        // the same both ways.
        //
        // Walks the tiles the line passes through and checks its height where it crosses from one
        // to the next (the lowest it is over a flat tile is at one of its edges). Where it leaves a
        // tile exactly through a corner, the diagonal step is open if either way round the corner
        // is: a line grazes a pillar's corner but doesn't squeeze between two.
        public bool CanSee(Vector2Int from, Vector2Int to)
        {
            if (IsTaken(from) || !_structure.Contains(to)) return false;

            float startHeight = FloorAt(from) + _eye, rise = FloorAt(to) + _eye - startHeight;
            int nx = Mathf.Abs(to.x - from.x), ny = Mathf.Abs(to.y - from.y);
            int stepX = to.x >= from.x ? 1 : -1, stepY = to.y >= from.y ? 1 : -1;
            Vector2Int tile = from;
            int doneX = 0, doneY = 0;
            while (doneX < nx || doneY < ny)
            {
                // Which edge the line crosses first: a vertical one at (doneX + ½) / nx of the way,
                // or a horizontal one at (doneY + ½) / ny. Both at once is a corner.
                long order = (1L + 2 * doneX) * ny - (1L + 2 * doneY) * nx;
                float along = order <= 0 ? (doneX + 0.5f) / nx : (doneY + 0.5f) / ny;
                float height = startHeight + rise * along;
                Vector2Int next;
                if (order == 0)
                {
                    next = new Vector2Int(tile.x + stepX, tile.y + stepY);
                    if (!CanCutCorner(tile, next, height)) return false;
                    doneX++;
                    doneY++;
                }
                else
                {
                    bool acrossX = order < 0;
                    next = acrossX ? new Vector2Int(tile.x + stepX, tile.y) : new Vector2Int(tile.x, tile.y + stepY);
                    if (acrossX) doneX++; else doneY++;
                    if (EdgeTop(tile, next) >= height) return false;
                }
                if (tile != from && TopOf(tile) >= height) return false;
                if ((next == to ? FloorAt(next) : TopOf(next)) >= height) return false;
                tile = next;
            }
            return true;
        }

        // How high whatever stands on a tile reaches, in metres above the doorways' level: its
        // floor, the top of a prop or a corner piece on it, or infinitely high for a pillar or
        // anywhere off the floor plan.
        private float TopOf(Vector2Int tile)
        {
            if (!_structure.Contains(tile)) return float.PositiveInfinity;
            if (_structure.Has(tile, RoomTileTags.Structure))
                return _cornerTops.TryGetValue(tile, out float top) ? top : float.PositiveInfinity;

            float floor = FloorAt(tile);
            return _structure.Has(tile, RoomTileTags.Prop) ? floor + _structure.PropHeightAt(tile) : floor;
        }

        // How high an inner wall between two tiles reaches above the doorways' level (nothing there: no height at all).
        private float EdgeTop(Vector2Int a, Vector2Int b) =>
            _structure.IsPartitioned(a, b) ? Mathf.Max(FloorAt(a), FloorAt(b)) + WallHeight(a, b) : float.NegativeInfinity;

        // An inner wall's own height; one the planners never recorded reaches the ceiling.
        private float WallHeight(Vector2Int a, Vector2Int b) =>
            _edges.TryGetValue(Ordered(a, b), out float height) ? height : float.PositiveInfinity;

        private bool CanCutCorner(Vector2Int from, Vector2Int to, float height) =>
            OpenVia(from, new Vector2Int(to.x, from.y), to, height) || OpenVia(from, new Vector2Int(from.x, to.y), to, height);

        private bool OpenVia(Vector2Int from, Vector2Int via, Vector2Int to, float height) =>
            TopOf(via) < height && EdgeTop(from, via) < height && EdgeTop(via, to) < height;

        private float Level(Vector2Int tile) => RoomElevation.HeightOf(_structure.LevelOf(tile));

        private float HighestFloorBeside(Vector2Int tile)
        {
            float highest = Level(tile);
            foreach (Vector2Int side in GridSides.All)
                if (_structure.Contains(tile + side)) highest = Mathf.Max(highest, Level(tile + side));
            return highest;
        }

        private static (Vector2Int, Vector2Int) Ordered(Vector2Int a, Vector2Int b) =>
            a.x < b.x || a.y < b.y ? (a, b) : (b, a);
    }
}
