using System.Collections.Generic;
using UnityEngine;
using CGD.Map;

namespace CGD.Level
{
    // A glowing bar across the top of every doorway, on the room side, in the colour of the
    // room the doorway leads to (MapNodeColors, as on the maps) — so a route is a choice the
    // player can read before committing to it. Doorways into dangerous rooms (an Elite, the
    // Boss, a Lockdown, Holdout or Rift, or high intensity) get a red marker under the bar.
    // Secret passages get none.
    //
    // One mesh per colour, no colliders: signs are decoration, not geometry.
    public class DoorwaySignBuilder
    {
        private static readonly Color DangerColor = new(0.95f, 0.15f, 0.1f);

        private const float BarHeight    = 0.15f;
        private const float BarDepth     = 0.08f;
        private const float BarWidth     = 0.7f;   // of a tile
        private const float MarkerSize   = 0.25f;
        private const float EmissionGain = 2f;

        private readonly LevelBuildSettings _settings;

        public DoorwaySignBuilder(LevelBuildSettings settings) => _settings = settings;

        public void Build(LevelLayout layout, Transform parent)
        {
            Material template = _settings.DoorSignMaterial;
            if (!_settings.BuildDoorSigns || template == null) return;

            var meshes = new Dictionary<Color, BoxMeshBuilder>();
            foreach (LevelDoorway doorway in layout.Doorways)
            {
                if (doorway.Connection.Type == ConnectionType.Secret) continue;
                if (!layout.Rooms.TryGetValue(doorway.Connection.Other(doorway.Room.Node.Id), out LevelRoom beyond)) continue;

                AddSign(layout, doorway, MeshFor(meshes, MapNodeColors.Of(beyond.Node.Type.SignType())),
                        IsDangerous(beyond.Node) ? MeshFor(meshes, DangerColor) : null);
            }

            foreach (var (color, mesh) in meshes)
                CreateSign(color, mesh, template, parent);
        }

        // An Ambush gives nothing away: it signs as the Treasure room it pretends to be.
        private bool IsDangerous(MapNode node) => node.Type switch
        {
            MapNodeType.Boss or MapNodeType.Lockdown or MapNodeType.Holdout or MapNodeType.Rift => true,
            MapNodeType.Ambush => false,
            _ => node.EffectiveTier >= 3 || node.Intensity >= _settings.DangerIntensity,
        };

        // Just inside the room, hanging from the top of the doorway (corridor height).
        private void AddSign(LevelLayout layout, LevelDoorway doorway, BoxMeshBuilder bar, BoxMeshBuilder danger)
        {
            Vector3 outward = new(doorway.Outward.x, 0f, doorway.Outward.y);
            Vector3 edge    = (layout.TileToLocal(doorway.RoomTile) + layout.TileToLocal(doorway.OutsideTile)) * 0.5f;
            Vector3 top     = edge - outward * (_settings.WallThickness * 0.5f + BarDepth)
                            + Vector3.up * (_settings.WallHeight - BarHeight * 0.5f);
            Quaternion facing = Quaternion.LookRotation(outward);

            bar.AddBox(top, new Vector3(layout.TileSize * BarWidth, BarHeight, BarDepth), facing);
            danger?.AddBox(top + Vector3.down * (BarHeight + MarkerSize) * 0.5f,
                           new Vector3(MarkerSize, MarkerSize, BarDepth), facing);
        }

        private static BoxMeshBuilder MeshFor(Dictionary<Color, BoxMeshBuilder> meshes, Color color)
        {
            if (!meshes.TryGetValue(color, out BoxMeshBuilder mesh))
                meshes[color] = mesh = new BoxMeshBuilder();
            return mesh;
        }

        private static void CreateSign(Color color, BoxMeshBuilder builder, Material template, Transform parent)
        {
            var go = new GameObject("DoorSigns", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = builder.ToMesh(go.name);

            var material = new Material(template) { name = $"{template.name} (sign)", color = color };
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * EmissionGain);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}
