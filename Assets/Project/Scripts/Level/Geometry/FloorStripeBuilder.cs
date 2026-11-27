using System.Collections.Generic;
using CGD.Algorithms;
using UnityEngine;

namespace CGD.Level
{
    // Glowing stripes laid on the floor (LevelBuildSettings › Floor stripes), seen and not walked
    // into (no collider), so the way through reads at a glance:
    //   - a guide line down the middle of each corridor, through the midpoints between its tiles,
    //     so a square bend is cut on the diagonal and a diagonal corridor's run (CorridorCorners)
    //     is one straight 45° line;
    //   - a stripe along the top of every open drop (a ledge with no railing or half wall), on the
    //     higher floor just in from its edge, and across the diagonal of a cut corner left open.
    // Points are in tile units on the floor plan until they are laid, in metres.
    public sealed class FloorStripeBuilder
    {
        // How high a stripe's top stands over its floor, in metres: clear of it, never tripped on.
        private const float Thickness = 0.01f;

        private readonly LevelBuildSettings _settings;
        private readonly LevelLayout _layout;

        public FloorStripeBuilder(LevelBuildSettings settings, LevelLayout layout)
        {
            _settings = settings;
            _layout   = layout;
        }

        public void Build(GeneratedAssets assets, Transform parent)
        {
            if (!_settings.FloorStripes || _settings.StripeMaterial == null) return;

            var corridors = new BoxMeshBuilder();
            foreach (IReadOnlyList<Vector2Int> corridor in _layout.Corridors)
            {
                List<Vector2> line = GuideLine(corridor);
                for (int i = 1; i < line.Count; i++) Lay(corridors, line[i - 1], line[i], 0f);
            }

            var ledges = new BoxMeshBuilder();
            foreach (LevelRoom room in _layout.Rooms.Values)
                if (room.Structure != null && room.Structure.HasElevation) AddLedges(room.Structure, ledges);

            if (!corridors.IsEmpty)
                GlowingPart.Create("CorridorStripes", corridors, _settings.StripeMaterial, _settings.CorridorStripeColor, "stripe", assets, parent);
            if (!ledges.IsEmpty)
                GlowingPart.Create("LedgeStripes", ledges, _settings.StripeMaterial, _settings.LedgeStripeColor, "stripe", assets, parent);
        }

        // From the middle of a corridor's first tile through the midpoints between each tile and
        // the next to the middle of its last, with the points in a straight line between merged.
        public static List<Vector2> GuideLine(IReadOnlyList<Vector2Int> corridor)
        {
            var points = new List<Vector2>();
            if (corridor.Count < 2) return points;

            Vector2 Middle(Vector2Int tile) => tile + Vector2.one * 0.5f;
            points.Add(Middle(corridor[0]));
            for (int i = 1; i < corridor.Count; i++) Add(points, (Middle(corridor[i - 1]) + Middle(corridor[i])) * 0.5f);
            Add(points, Middle(corridor[corridor.Count - 1]));
            return points;
        }

        // Appends a point, dropping the one before it where the three are in a line.
        private static void Add(List<Vector2> points, Vector2 point)
        {
            int n = points.Count;
            if (n >= 2)
            {
                Vector2 a = points[n - 1] - points[n - 2], b = point - points[n - 1];
                if (Mathf.Abs(a.x * b.y - a.y * b.x) < 1e-5f && Vector2.Dot(a, b) > 0f) points.RemoveAt(n - 1);
            }
            points.Add(point);
        }

        // A stripe on the higher side of each open drop, and across each open cut corner.
        private void AddLedges(RoomStructure structure, BoxMeshBuilder boxes)
        {
            float inset = _settings.StripeWidth / _layout.TileSize;
            foreach (Vector2Int tile in structure.Footprint.Tiles)
                foreach (Vector2Int side in GridNeighbours.Four)
                {
                    Vector2Int below = tile + side;
                    if (structure.Has(below, RoomTileTags.Structure) || !structure.CanDropDown(tile, below)) continue;

                    // The shared edge, pulled in onto the higher tile.
                    Vector2 middle = tile + Vector2.one * 0.5f + (Vector2)side * (0.5f - inset);
                    var along = new Vector2(side.y, side.x) * 0.5f;
                    Lay(boxes, middle - along, middle + along, RoomElevation.HeightOf(structure.LevelOf(tile)));
                }

            foreach (ElevatedArea area in structure.Elevations)
                foreach (GroundCorner corner in area.Corners)
                {
                    if (!corner.Sheer || area.GuardAcross(corner) != LedgeGuard.None) continue;
                    var (_, a, b) = corner.RaisedHalf;
                    Vector2 inward = ((Vector2)corner.Up).normalized * inset;
                    Lay(boxes, a + inward, b + inward, RoomElevation.HeightOf(area.Level));
                }
        }

        // One stripe from `from` to `to` (tile units) on a floor `floor` metres up, a stripe's width
        // longer so the pieces of a line close up at its bends.
        private void Lay(BoxMeshBuilder boxes, Vector2 from, Vector2 to, float floor)
        {
            float tile = _layout.TileSize, width = _settings.StripeWidth;
            Vector2 run = (to - from) * tile;
            if (run.sqrMagnitude < 1e-6f) return;

            Vector2 middle = (from + to) * 0.5f * tile;
            var centre = new Vector3(middle.x, floor + Thickness * 0.5f, middle.y);
            boxes.AddBox(centre, new Vector3(width, Thickness, run.magnitude + width), Quaternion.LookRotation(new Vector3(run.x, 0f, run.y)));
        }
    }
}
