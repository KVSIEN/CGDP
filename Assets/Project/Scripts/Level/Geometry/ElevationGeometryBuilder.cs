using CGD.Algorithms;
using UnityEngine;

namespace CGD.Level
{
    // Builds what a room's floor levels add to its geometry (RoomStructure.Elevations): the floor
    // under each tile reaching down to its lowest neighbour, so a raised floor shows its face and
    // a sunken one its sides; a ramp of stairs on each flight; a ramp of ground the full width of
    // the tile on each slope, so slopes side by side read as one bank, and a diagonal one on each
    // corner of a mound or crater; a railing along each railed ledge; and each overpass's deck,
    // with a flight onto each end and a railing along both sides. Stairs and railings are the
    // settings' prefabs when it has them, plain boxes otherwise; ground and decks are always boxes.
    public sealed class ElevationGeometryBuilder
    {
        // A flight's ramp, without a prefab: this share of the tile wide, this many metres thick.
        private const float StairWidthShare = 0.9f;
        private const float StairThickness  = 0.2f;
        // A mound's or crater's corner ramp: this share of the tile's diagonal wide, tucked between the slopes beside it.
        private const float CornerWidthShare = 0.5f;
        // A railing, without a prefab: metres thick, and how far in from the ledge it stands
        // (RoomElevation.GuardHeight tall, as the half wall).
        private const float RailHeight    = RoomElevation.GuardHeight;
        private const float RailThickness = 0.08f;
        // A half wall: metres thick, its outer face flush with the ledge.
        private const float HalfWallThickness = 0.2f;
        private const float RailInset     = 0.15f;
        // The kit's stairs and railing prefabs are made for this tile size (PlaceholderPropBuilder.TileMetres).
        private const float KitTileMetres = 3f;

        private readonly LevelBuildSettings _settings;
        private readonly LevelLayout _layout;

        public ElevationGeometryBuilder(LevelBuildSettings settings, LevelLayout layout)
        {
            _settings = settings;
            _layout   = layout;
        }

        // Where a tile's floor slab runs, in metres: from under the lowest floor beside it (so the
        // slab's side closes the gap down to it) to its own floor. Flat floor gives the plain slab.
        public static (float bottom, float top) FloorSpan(RoomStructure structure, Vector2Int tile, float thickness)
        {
            int level = structure.LevelOf(tile), lowest = level;
            if (structure.HasElevation)
                foreach (Vector2Int side in GridNeighbours.Four)
                    if (structure.Contains(tile + side)) lowest = Mathf.Min(lowest, structure.LevelOf(tile + side));
            return (RoomElevation.HeightOf(lowest) - thickness, RoomElevation.HeightOf(level));
        }

        // The room's stairs, slopes, railings, half walls and decks: boxes into `stairs`, `ground`,
        // `railings`, `halfWalls` (low cover) and `decks`, prefabs under `parent`.
        public void Build(LevelRoom room, BoxMeshBuilder stairs, BoxMeshBuilder ground, BoxMeshBuilder railings, BoxMeshBuilder halfWalls,
                          BoxMeshBuilder decks, Transform parent)
        {
            RoomStructure structure = room.Structure;
            if (structure == null) return;

            foreach (Overpass overpass in structure.Overpasses) AddOverpass(structure, overpass, stairs, decks, railings, parent);
            if (!structure.HasElevation) return;

            foreach (ElevatedArea area in structure.Elevations)
            {
                foreach (StairFlight flight in area.Stairs)
                {
                    if (flight.Slope) AddRamp(structure, flight.Tile, flight.Up, 1f, ground);
                    else AddStairs(structure, flight, stairs, parent);
                }
                foreach (SlopeCorner corner in area.Corners) AddRamp(structure, corner.Tile, corner.Up, CornerWidthShare, ground);
                if (area.Guards.Any) AddGuards(structure, area, railings, halfWalls, parent);
            }
        }

        // A deck slab over each of its tiles, its top at the deck's level; a flight up onto each
        // end, with a sloped rail along both its sides (it climbs a metre above the floor round
        // it); a railing along both sides of the deck, so nobody falls off it.
        private void AddOverpass(RoomStructure structure, Overpass overpass, BoxMeshBuilder stairs, BoxMeshBuilder decks,
                                 BoxMeshBuilder railings, Transform parent)
        {
            float tile = _layout.TileSize, top = RoomElevation.HeightOf(overpass.Level);
            Vector3 along = new(overpass.Along.x, 0f, overpass.Along.y);
            Vector3 across = new(-along.z, 0f, along.x);
            foreach (Vector2Int deck in overpass.Deck)
            {
                Vector3 middle = _layout.TileToLocal(deck);
                decks.AddBox(middle + Vector3.up * (top - RoomElevation.DeckThickness * 0.5f), new Vector3(tile, RoomElevation.DeckThickness, tile));
                foreach (float side in new[] { 1f, -1f })
                {
                    Vector3 inward = -across * side;
                    Vector3 foot = middle - inward * (tile * 0.5f - RailInset) + Vector3.up * top;
                    AddRailing(foot, Quaternion.LookRotation(inward), railings, parent);
                }
            }
            foreach (StairFlight flight in overpass.Stairs)
            {
                AddStairs(structure, flight, stairs, parent);
                AddFlightRails(structure, flight, railings);
            }
        }

        // A rail along each side of a flight, following its slope a railing's height above it.
        private void AddFlightRails(RoomStructure structure, StairFlight flight, BoxMeshBuilder boxes)
        {
            float run = _layout.TileSize, rise = RoomElevation.StepHeight;
            float low = RoomElevation.HeightOf(structure.LevelOf(flight.Tile));
            Vector3 up = new(flight.Up.x, 0f, flight.Up.y), side = new(-up.z, 0f, up.x);
            Quaternion slope = Quaternion.LookRotation(up) * Quaternion.Euler(-Mathf.Atan2(rise, run) * Mathf.Rad2Deg, 0f, 0f);
            Vector3 middle = _layout.TileToLocal(flight.Tile) + Vector3.up * (low + rise * 0.5f + RailHeight * 0.5f);
            var size = new Vector3(RailThickness, RailHeight, Mathf.Sqrt(run * run + rise * rise));
            foreach (float sign in new[] { 1f, -1f })
                boxes.AddBox(middle + side * (sign * (run * 0.5f - RailInset)), size, slope);
        }

        // A slab climbing one level across a tile from its own floor toward `up` (a side, or a
        // diagonal for a corner, which is then that much longer and `widthShare` of its diagonal wide).
        private void AddRamp(RoomStructure structure, Vector2Int tile, Vector2Int up, float widthShare, BoxMeshBuilder boxes)
        {
            float run = _layout.TileSize * new Vector2(up.x, up.y).magnitude, rise = RoomElevation.StepHeight;
            float low = RoomElevation.HeightOf(structure.LevelOf(tile));
            Quaternion slope = Quaternion.LookRotation(new Vector3(up.x, 0f, up.y)) * Quaternion.Euler(-Mathf.Atan2(rise, run) * Mathf.Rad2Deg, 0f, 0f);
            Vector3 middle = _layout.TileToLocal(tile) + Vector3.up * (low + rise * 0.5f) - slope * Vector3.up * (StairThickness * 0.5f);
            boxes.AddBox(middle, new Vector3(run * widthShare, StairThickness, Mathf.Sqrt(run * run + rise * rise)), slope);
        }

        // A flight of steps climbing one level across its tile, foot to head.
        private void AddStairs(RoomStructure structure, StairFlight flight, BoxMeshBuilder boxes, Transform parent)
        {
            float tile = _layout.TileSize, rise = RoomElevation.StepHeight;
            float low = RoomElevation.HeightOf(structure.LevelOf(flight.Tile));
            var up = new Vector3(flight.Up.x, 0f, flight.Up.y);
            Quaternion facing = Quaternion.LookRotation(up);
            Vector3 foot = _layout.TileToLocal(flight.Tile) + Vector3.up * low;

            if (_settings.StairsPrefab != null)
            {
                GameObject placed = Object.Instantiate(_settings.StairsPrefab, parent.TransformPoint(foot), parent.rotation * facing, parent);
                placed.transform.localScale = Vector3.Scale(placed.transform.localScale, new Vector3(tile / KitTileMetres, rise, tile / KitTileMetres));
                return;
            }

            AddRamp(structure, flight.Tile, flight.Up, StairWidthShare, boxes);
        }

        // A railing or a half wall on the higher side of every ledge round the area its guards
        // cover, stairs left open. A ledge two guarded areas share is built once, by the higher.
        private void AddGuards(RoomStructure structure, ElevatedArea area, BoxMeshBuilder railings, BoxMeshBuilder halfWalls, Transform parent)
        {
            foreach (Vector2Int inside in area.Tiles)
                foreach (Vector2Int side in GridNeighbours.Four)
                {
                    Vector2Int outside = inside + side;
                    LedgeGuard guard = structure.GuardOn(inside, outside);
                    if (guard == LedgeGuard.None || area.GuardOn(inside, outside) != guard || structure.IsPartitioned(inside, outside)) continue;
                    if (LeadsOntoADeck(structure, inside, outside)) continue;
                    ElevatedArea other = structure.AreaOf(outside);
                    if (other != null && other.GuardOn(outside, inside) == guard && structure.LevelOf(outside) > structure.LevelOf(inside)) continue;

                    Vector2Int high = structure.LevelOf(inside) > structure.LevelOf(outside) ? inside : outside;
                    Vector2Int low  = high == inside ? outside : inside;
                    Vector3 towardHigh = new(high.x - low.x, 0f, high.y - low.y);
                    Vector3 foot = (_layout.TileToLocal(inside) + _layout.TileToLocal(outside)) * 0.5f
                                 + towardHigh * RailInset + Vector3.up * RoomElevation.HeightOf(structure.LevelOf(high));
                    // Facing the higher floor, the guard's length (its x) runs along the edge.
                    Quaternion facing = Quaternion.LookRotation(towardHigh);
                    if (guard == LedgeGuard.Railing) AddRailing(foot, facing, railings, parent);
                    else AddHalfWall(foot - towardHigh * (RailInset - HalfWallThickness * 0.5f), facing, halfWalls);
                }
        }

        // One tile's length of half wall, solid, centred on `middle` at its foot.
        private void AddHalfWall(Vector3 middle, Quaternion facing, BoxMeshBuilder boxes) =>
            boxes.AddBox(middle + Vector3.up * (RoomElevation.GuardHeight * 0.5f),
                         new Vector3(_layout.TileSize, RoomElevation.GuardHeight, HalfWallThickness), facing);

        // One tile's length of railing standing at `foot`, facing `facing` (its length runs across that).
        private void AddRailing(Vector3 foot, Quaternion facing, BoxMeshBuilder boxes, Transform parent)
        {
            float tile = _layout.TileSize;
            if (_settings.RailingPrefab != null)
            {
                GameObject placed = Object.Instantiate(_settings.RailingPrefab, parent.TransformPoint(foot), parent.rotation * facing, parent);
                placed.transform.localScale = Vector3.Scale(placed.transform.localScale, new Vector3(tile / KitTileMetres, 1f, 1f));
                return;
            }
            boxes.AddBox(foot + Vector3.up * (RailHeight * 0.5f), new Vector3(tile, RailHeight, RailThickness), facing);
        }

        // The edge an overpass's flight climbs over onto its deck: left open.
        private static bool LeadsOntoADeck(RoomStructure structure, Vector2Int a, Vector2Int b)
        {
            foreach (Overpass overpass in structure.Overpasses)
                foreach (StairFlight flight in overpass.Stairs)
                    if ((flight.Tile == a && flight.Head == b) || (flight.Tile == b && flight.Head == a)) return true;
            return false;
        }
    }
}
