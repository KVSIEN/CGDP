using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // Writes a placeholder prop prefab from its parts, the way the kit's props are made: a
    // bare root, one child per primitive with a palette material, and a box collider on the
    // solid ones. An existing prefab is rebuilt in place, so its root keeps its identity and
    // every room, scene and dressing that places it keeps working.
    public static class PlaceholderPropBuilder
    {
        public const int MaxParts = 3;
        public const string PropsFolder = "Assets/Project/Prefabs/Environment/Props";
        public const float TileMetres = 3f;

        public static string PathFor(string name) => $"{PropsFolder}/{name}.prefab";

        // What keeps `parts` from being a kit prop for a `footprint` (in tiles); empty when sound.
        public static List<string> Problems(IReadOnlyList<PlaceholderPart> parts, Vector2Int footprint, PlaceholderPalette palette)
        {
            var problems = new List<string>();
            if (parts.Count == 0) problems.Add("A prop needs at least one part.");
            if (parts.Count > MaxParts) problems.Add($"Keep placeholders to {MaxParts} primitives.");

            var box = new Bounds(Vector3.zero, new Vector3(footprint.x * TileMetres, 0f, footprint.y * TileMetres));
            bool solid = false;
            foreach (PlaceholderPart part in parts)
            {
                solid |= part.Solid;
                Bounds bounds = part.Bounds;
                if (bounds.min.y < -0.01f) problems.Add($"{part.Name} sinks below the floor.");
                if (bounds.min.x < box.min.x - 0.01f || bounds.max.x > box.max.x + 0.01f ||
                    bounds.min.z < box.min.z - 0.01f || bounds.max.z > box.max.z + 0.01f)
                    problems.Add($"{part.Name} reaches past its {footprint.x}×{footprint.y} footprint.");
                if (palette == null || palette.MaterialAt(part.Swatch) == null) problems.Add($"{part.Name} has no palette colour.");
            }
            if (parts.Count > 0 && !solid) problems.Add("Nothing is solid: furniture needs a collider (ground cover goes in a Decor placement).");
            return problems;
        }

        public static GameObject Save(string name, IReadOnlyList<PlaceholderPart> parts, PlaceholderPalette palette)
        {
            string path = PathFor(name);
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            GameObject root = exists ? PrefabUtility.LoadPrefabContents(path) : new GameObject(name);
            try
            {
                for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
                foreach (PlaceholderPart part in parts) Add(root.transform, part, palette);
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                if (exists) PrefabUtility.UnloadPrefabContents(root);
                else Object.DestroyImmediate(root);
            }
        }

        // The parts of a prefab, when it is a kit prop; null otherwise (`why` says what is off).
        public static List<PlaceholderPart> Read(GameObject prefab, PlaceholderPalette palette, out string why)
        {
            why = null;
            var parts = new List<PlaceholderPart>();
            foreach (Transform child in prefab.transform)
            {
                if (!PlaceholderPart.TryRead(child, palette, out PlaceholderPart part))
                {
                    why = $"{child.name} is not a plain cube, cylinder or sphere.";
                    return null;
                }
                parts.Add(part);
            }
            return parts;
        }

        private static void Add(Transform root, PlaceholderPart part, PlaceholderPalette palette)
        {
            GameObject piece = GameObject.CreatePrimitive(part.Primitive);
            piece.name = part.Name;
            // The kit's props all block with boxes, whatever their shape.
            Object.DestroyImmediate(piece.GetComponent<Collider>());
            if (part.Solid) piece.AddComponent<BoxCollider>();

            Transform transform = piece.transform;
            transform.SetParent(root, false);
            transform.localPosition = part.Centre;
            transform.localRotation = Quaternion.Euler(part.Tilt, 0f, 0f);
            transform.localScale    = part.Scale;
            piece.GetComponent<MeshRenderer>().sharedMaterial = palette != null ? palette.MaterialAt(part.Swatch) : null;
        }
    }
}
