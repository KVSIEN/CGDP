using UnityEngine;

namespace CGD.Level
{
    // A light under the roof of each building in an open-air room (a town): the roof shuts out
    // the sky, so without one the rooms would be dark. One soft point light per space (hall and
    // rooms alike), centred under the roof and reaching a little past its walls; no shadows, so
    // a dozen buildings stay cheap.
    public class BuildingLightBuilder
    {
        private readonly LevelBuildSettings _settings;

        public BuildingLightBuilder(LevelBuildSettings settings) => _settings = settings;

        public void Build(LevelLayout layout, Transform parent)
        {
            foreach (LevelRoom room in layout.Rooms.Values)
            {
                RoomStructure structure = room.Structure;
                if (structure == null || !structure.HasRoofedSpaces) continue;

                foreach (StructureSpace space in structure.Spaces)
                    if (space.Roofed) AddLight(layout, room, structure, space, parent);
            }
        }

        private void AddLight(LevelLayout layout, LevelRoom room, RoomStructure structure, StructureSpace space, Transform parent)
        {
            int zone = structure.ZoneOf(space.Tile);
            Vector2Int min = new(int.MaxValue, int.MaxValue), max = new(int.MinValue, int.MinValue);
            foreach (Vector2Int tile in room.Footprint.Tiles)
            {
                if (structure.ZoneOf(tile) != zone) continue;
                min = Vector2Int.Min(min, tile);
                max = Vector2Int.Max(max, tile);
            }
            if (min.x > max.x) return;

            float tileSize = layout.TileSize;
            var go = new GameObject($"Light {space.Name}");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3((min.x + max.x + 1) * 0.5f * tileSize, room.WallHeight - 0.4f, (min.y + max.y + 1) * 0.5f * tileSize);

            var light = go.AddComponent<Light>();
            light.type      = LightType.Point;
            light.color     = _settings.BuildingLightColor;
            light.intensity = _settings.BuildingLightIntensity;
            light.range     = (Mathf.Max(max.x - min.x, max.y - min.y) + 1) * tileSize * 0.75f + 2f;
            light.shadows   = LightShadows.None;
        }
    }
}
