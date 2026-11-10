using System.Collections.Generic;
using UnityEngine;
using TMPro;
using CGD.Map;

namespace CGD.Level
{
    // A glowing bar across the top of every doorway, on the room side, in the colour of the
    // room the doorway leads to (MapNodeColors, as on the maps) — so a route is a choice the
    // player can read before committing to it. Doorways into dangerous rooms (an Elite, the
    // Boss, a Lockdown, Holdout or Rift, or high intensity) get a red marker under the bar.
    // With labels on, the room beyond is named under the sign, with its faction and tier.
    // Secret passages get none.
    //
    // One mesh per colour, no colliders: signs are decoration, not geometry.
    public class DoorwaySignBuilder
    {
        private static readonly Color DangerColor = new(0.95f, 0.15f, 0.1f);
        private static readonly Color LabelColor  = new(1f, 1f, 1f, 0.9f);

        private const float BarHeight    = 0.15f;
        private const float BarDepth     = 0.08f;
        private const float BarWidth     = 0.7f;   // of a tile
        private const float MarkerSize   = 0.25f;
        private const float EmissionGain = 2f;

        // The label hangs under the bar (and its danger marker), as wide as the doorway.
        private const float LabelGap      = 0.08f;
        private const float LabelHeight   = 0.7f;
        private const float LabelFontSize = 2.4f;
        private const float LabelMinFont  = 1.2f;

        private readonly LevelBuildSettings _settings;
        private readonly GeneratedAssets _assets;

        public DoorwaySignBuilder(LevelBuildSettings settings, GeneratedAssets assets)
        {
            _settings = settings;
            _assets   = assets;
        }

        public void Build(LevelLayout layout, Transform parent)
        {
            Material template = _settings.DoorSignMaterial;
            if (!_settings.BuildDoorSigns || template == null) return;

            var meshes = new Dictionary<Color, BoxMeshBuilder>();
            Transform labels = _settings.DoorSignLabels ? new GameObject("DoorLabels").transform : null;
            labels?.SetParent(parent, false);

            foreach (LevelDoorway doorway in layout.Doorways)
            {
                if (doorway.Connection.Type == ConnectionType.Secret) continue;
                if (!layout.Rooms.TryGetValue(doorway.Connection.Other(doorway.Room.Node.Id), out LevelRoom beyond)) continue;

                bool dangerous = IsDangerous(beyond.Node);
                Pose sign = SignPose(layout, doorway);
                AddSign(layout, sign, MeshFor(meshes, MapNodeColors.Of(beyond.Node.Type.SignType())),
                        dangerous ? MeshFor(meshes, DangerColor) : null);
                if (labels != null) AddLabel(layout, sign, beyond, dangerous, labels);
            }

            foreach (var (color, mesh) in meshes)
                CreateSign(color, mesh, template, parent);
        }

        // An Ambush gives nothing away: it signs as the Treasure room it pretends to be.
        private bool IsDangerous(MapNode node) => node.Type switch
        {
            MapNodeType.Boss or MapNodeType.Lockdown or MapNodeType.Holdout or MapNodeType.Rift
                or MapNodeType.Siege or MapNodeType.Gauntlet => true,
            MapNodeType.Ambush => false,
            _ => node.EffectiveTier >= 3 || node.Intensity >= _settings.DangerIntensity,
        };

        // Just inside the room, hanging from the top of the doorway (corridor height); level-local,
        // facing out of the room.
        private Pose SignPose(LevelLayout layout, LevelDoorway doorway)
        {
            Vector3 outward = new(doorway.Outward.x, 0f, doorway.Outward.y);
            Vector3 edge    = (layout.TileToLocal(doorway.RoomTile) + layout.TileToLocal(doorway.OutsideTile)) * 0.5f;
            Vector3 top     = edge - outward * (_settings.WallThickness * 0.5f + BarDepth)
                            + Vector3.up * (_settings.WallHeight - BarHeight * 0.5f);
            return new Pose(top, Quaternion.LookRotation(outward));
        }

        private static void AddSign(LevelLayout layout, Pose sign, BoxMeshBuilder bar, BoxMeshBuilder danger)
        {
            bar.AddBox(sign.position, new Vector3(layout.TileSize * BarWidth, BarHeight, BarDepth), sign.rotation);
            danger?.AddBox(sign.position + Vector3.down * (BarHeight + MarkerSize) * 0.5f,
                           new Vector3(MarkerSize, MarkerSize, BarDepth), sign.rotation);
        }

        // Read from inside the room, looking at the doorway: TextMeshPro faces -Z, so a label
        // turned like the sign (+Z out of the room) reads the right way round.
        private static void AddLabel(LevelLayout layout, Pose sign, LevelRoom beyond, bool dangerous, Transform parent)
        {
            float drop = BarHeight * 0.5f + (dangerous ? MarkerSize : 0f) + LabelGap + LabelHeight * 0.5f;
            var label = new GameObject($"DoorLabel_{beyond.Node.Id}", typeof(TextMeshPro));
            label.transform.SetParent(parent, false);
            label.transform.SetLocalPositionAndRotation(sign.position + Vector3.down * drop, sign.rotation);

            var text = label.GetComponent<TextMeshPro>();
            text.rectTransform.sizeDelta = new Vector2(layout.TileSize * 0.9f, LabelHeight);
            text.text             = $"{RoomLabel.Name(beyond).ToUpperInvariant()}\n<size=65%>{RoomLabel.Details(beyond)}</size>";
            text.fontSize         = LabelFontSize;
            text.enableAutoSizing = true;
            text.fontSizeMin      = LabelMinFont;
            text.fontSizeMax      = LabelFontSize;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.alignment        = TextAlignmentOptions.Center;
            text.color            = LabelColor;
        }

        private static BoxMeshBuilder MeshFor(Dictionary<Color, BoxMeshBuilder> meshes, Color color)
        {
            if (!meshes.TryGetValue(color, out BoxMeshBuilder mesh))
                meshes[color] = mesh = new BoxMeshBuilder();
            return mesh;
        }

        private void CreateSign(Color color, BoxMeshBuilder builder, Material template, Transform parent)
        {
            var go = new GameObject("DoorSigns", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = _assets.Track(builder.ToMesh(go.name));

            var material = _assets.Track(new Material(template) { name = $"{template.name} (sign)", color = color });
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * EmissionGain);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}
