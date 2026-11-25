using UnityEngine;

namespace CGD.Level
{
    // Builds what a room's floor levels add to its geometry (RoomStructure.Elevations): the floor
    // under each tile reaching down to its lowest neighbour, so a raised floor shows its face and
    // a sunken one its sides; a ramp of stairs on each flight; and a railing along each railed
    // ledge. Stairs and railings are the settings' prefabs when it has them, plain boxes otherwise.
    public sealed class ElevationGeometryBuilder
    {
        // A flight's ramp, without a prefab: this share of the tile wide, this many metres thick.
        private const float StairWidthShare = 0.9f;
        private const float StairThickness  = 0.2f;
        // A railing, without a prefab: metres tall and thick, and how far in from the ledge it stands.
        private const float RailHeight    = 1f;
        private const float RailThickness = 0.08f;
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
                foreach (Vector2Int side in GridSides.All)
                    if (structure.Contains(tile + side)) lowest = Mathf.Min(lowest, structure.LevelOf(tile + side));
            return (RoomElevation.HeightOf(lowest) - thickness, RoomElevation.HeightOf(level));
        }

        // The room's stairs and railings: boxes into `stairs` and `railings`, prefabs under `parent`.
        public void Build(LevelRoom room, BoxMeshBuilder stairs, BoxMeshBuilder railings, Transform parent)
        {
            RoomStructure structure = room.Structure;
            if (structure == null || !structure.HasElevation) return;

            foreach (ElevatedArea area in structure.Elevations)
            {
                foreach (StairFlight flight in area.Stairs) AddStairs(structure, flight, stairs, parent);
                if (area.Railed) AddRailings(structure, area, railings, parent);
            }
        }

        // A ramp climbing one level across the flight's tile, foot to head.
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

            Quaternion slope = facing * Quaternion.Euler(-Mathf.Atan2(rise, tile) * Mathf.Rad2Deg, 0f, 0f);
            Vector3 middle = foot + Vector3.up * (rise * 0.5f) - slope * Vector3.up * (StairThickness * 0.5f);
            boxes.AddBox(middle, new Vector3(tile * StairWidthShare, StairThickness, Mathf.Sqrt(tile * tile + rise * rise)), slope);
        }

        // A railing on the higher side of every ledge round the area, stairs left open.
        private void AddRailings(RoomStructure structure, ElevatedArea area, BoxMeshBuilder boxes, Transform parent)
        {
            float tile = _layout.TileSize;
            foreach (Vector2Int inside in area.Tiles)
                foreach (Vector2Int side in GridSides.All)
                {
                    Vector2Int outside = inside + side;
                    if (!structure.IsLedge(inside, outside) || structure.IsPartitioned(inside, outside)) continue;

                    Vector2Int high = structure.LevelOf(inside) > structure.LevelOf(outside) ? inside : outside;
                    Vector2Int low  = high == inside ? outside : inside;
                    Vector3 towardHigh = new(high.x - low.x, 0f, high.y - low.y);
                    Vector3 foot = (_layout.TileToLocal(inside) + _layout.TileToLocal(outside)) * 0.5f
                                 + towardHigh * RailInset + Vector3.up * RoomElevation.HeightOf(structure.LevelOf(high));
                    // Facing the higher floor, the railing's length (its x) runs along the edge.
                    Quaternion along = Quaternion.LookRotation(towardHigh);

                    if (_settings.RailingPrefab != null)
                    {
                        GameObject placed = Object.Instantiate(_settings.RailingPrefab, parent.TransformPoint(foot), parent.rotation * along, parent);
                        placed.transform.localScale = Vector3.Scale(placed.transform.localScale, new Vector3(tile / KitTileMetres, 1f, 1f));
                        continue;
                    }
                    boxes.AddBox(foot + Vector3.up * (RailHeight * 0.5f), new Vector3(tile, RailHeight, RailThickness), along);
                }
        }
    }
}
