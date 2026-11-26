using System.Collections.Generic;
using UnityEngine;
using TMPro;
using CGD.Map;

namespace CGD.Level
{
    // A designer's overview of a generated level, read from above in the Scene view: every
    // room outlined along the tops of its walls in its room type's map colour, and labelled
    // flat over its middle with its id, place, real room type, layout, faction and tier
    // (RoomLabel.Overview). Everything hangs under one object, so it can be hidden at once.
    //
    // No colliders, and built after the NavMesh so nothing walks on it. The labels only
    // render from above, so a player under an open ceiling never sees them mirrored.
    public class RoomOverviewBuilder
    {
        private const float OutlineWidth   = 0.25f;
        private const float OutlineHeight  = 0.1f;
        private const float EmissionGain   = 2f;
        // How far above the room's walls the label floats.
        private const float LabelLift      = 0.5f;
        private const float LabelMaxFont   = 14f;
        private const float LabelMinFont   = 2f;
        private const float LabelOutline   = 0.2f;
        // TextMeshPro's cull mode: back faces off.
        private const float CullBack       = 2f;

        private readonly LevelBuildSettings _settings;
        private readonly GeneratedAssets _assets;
        private Material _labelMaterial;

        public RoomOverviewBuilder(LevelBuildSettings settings, GeneratedAssets assets)
        {
            _settings = settings;
            _assets   = assets;
        }

        // Returns the overview's root, under `parent`; null when there is no sign material to colour it with.
        public GameObject Build(LevelLayout layout, Transform parent)
        {
            Material template = _settings.DoorSignMaterial;
            if (template == null) return null;

            var root = new GameObject("RoomOverview");
            root.transform.SetParent(parent, false);

            var outlines = new Dictionary<Color, BoxMeshBuilder>();
            foreach (LevelRoom room in layout.Rooms.Values)
            {
                Color color = MapNodeColors.Of(room.Node.Type);
                if (!outlines.TryGetValue(color, out BoxMeshBuilder mesh)) outlines[color] = mesh = new BoxMeshBuilder();
                AddOutline(layout, room, mesh);
                AddLabel(layout, room, root.transform);
                AddSpaceLabels(layout, room, root.transform);
            }
            foreach (var (color, mesh) in outlines)
                CreateOutline(color, mesh, template, root.transform);
            return root;
        }

        // A thin bar over every edge of the room's floor that faces outside it, on top of the walls.
        private static void AddOutline(LevelLayout layout, LevelRoom room, BoxMeshBuilder mesh)
        {
            float size = layout.TileSize;
            float y = room.WallHeight + OutlineHeight * 0.5f;
            foreach (Vector2Int tile in room.Footprint.Tiles)
            {
                Vector3 centre = layout.TileToLocal(tile);
                foreach (Vector2Int side in new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left })
                {
                    if (room.Footprint.Contains(tile + side)) continue;
                    Vector3 edge = centre + new Vector3(side.x, 0f, side.y) * (size * 0.5f) + Vector3.up * y;
                    Vector3 extent = side.x != 0 ? new Vector3(OutlineWidth, OutlineHeight, size) : new Vector3(size, OutlineHeight, OutlineWidth);
                    mesh.AddBox(edge, extent);
                }
            }
        }

        // Flat over the room's middle, facing up, its top toward +Z (north in the Scene view).
        private void AddLabel(LevelLayout layout, LevelRoom room, Transform parent)
        {
            Rect rect = layout.RoomLocalRect(room);
            Vector3 centre = layout.RoomCenterLocal(room) + Vector3.up * (room.WallHeight + LabelLift);

            var label = new GameObject($"RoomLabel_{room.Node.Id}", typeof(TextMeshPro));
            label.transform.SetParent(parent, false);
            label.transform.SetLocalPositionAndRotation(centre, Quaternion.Euler(90f, 0f, 0f));

            var text = label.GetComponent<TextMeshPro>();
            text.rectTransform.sizeDelta = new Vector2(rect.width * 0.85f, rect.height * 0.6f);
            text.text             = RoomLabel.Overview(room, _settings.TerritoryDepth);
            text.enableAutoSizing = true;
            text.fontSizeMin      = LabelMinFont;
            text.fontSizeMax      = LabelMaxFont;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.alignment        = TextAlignmentOptions.Center;
            text.color            = Color.white;
            text.fontSharedMaterial = LabelMaterial(text.fontSharedMaterial);
        }

        // A small flat label over each named place inside the room, so the Scene view shows which
        // subrooms it has and where.
        private void AddSpaceLabels(LevelLayout layout, LevelRoom room, Transform parent)
        {
            if (room.Structure == null) return;

            foreach (StructureSpace space in room.Structure.Spaces)
            {
                if (!space.HasArea || string.IsNullOrEmpty(space.Name)) continue;

                Vector3 centre = layout.TileToLocal(Vector2Int.zero) + new Vector3(space.Centre.x - 0.5f, 0f, space.Centre.y - 0.5f) * layout.TileSize;
                centre.y = room.WallHeight + LabelLift;

                var label = new GameObject($"SpaceLabel_{room.Node.Id}_{space.Name}", typeof(TextMeshPro));
                label.transform.SetParent(parent, false);
                label.transform.SetLocalPositionAndRotation(centre, Quaternion.Euler(90f, 0f, 0f));

                var text = label.GetComponent<TextMeshPro>();
                text.rectTransform.sizeDelta = new Vector2(space.Area.width, space.Area.height) * (layout.TileSize * 0.9f);
                text.text             = space.Name;
                text.enableAutoSizing = true;
                text.fontSizeMin      = LabelMinFont;
                text.fontSizeMax      = LabelMaxFont * 0.6f;
                text.textWrappingMode = TextWrappingModes.Normal;
                text.alignment        = TextAlignmentOptions.Center;
                text.color            = new Color(1f, 0.92f, 0.55f);
                text.fontSharedMaterial = LabelMaterial(text.fontSharedMaterial);
            }
        }

        // One material for every label: the font's, outlined in black to read over any floor,
        // and seen from above only.
        private Material LabelMaterial(Material font)
        {
            if (_labelMaterial != null) return _labelMaterial;

            _labelMaterial = _assets.Track(new Material(font) { name = $"{font.name} (overview)" });
            _labelMaterial.SetFloat(ShaderUtilities.ShaderTag_CullMode, CullBack);
            _labelMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
            _labelMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, LabelOutline);
            _labelMaterial.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
            return _labelMaterial;
        }

        private void CreateOutline(Color color, BoxMeshBuilder builder, Material template, Transform parent)
        {
            var go = new GameObject("RoomOutlines", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = _assets.Track(builder.ToMesh(go.name));

            var material = _assets.Track(new Material(template) { name = $"{template.name} (overview)", color = color });
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * EmissionGain);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}
