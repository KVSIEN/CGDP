using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Editor;

namespace CGD.Tests
{
    // The placeholder kit stays placeholder: every structure piece and prop is a few plain
    // primitives, each coloured by a material, so a room reads at a glance and real art can
    // replace it inside the same prefab (ROOM_GENERATION.md › Placeholder kit).
    public class PlaceholderKitTests
    {
        private const int MaxParts = 3;
        private static readonly string[] Folders =
        {
            "Assets/Project/Prefabs/Environment/Props",
            "Assets/Project/Prefabs/Environment/Structure",
        };

        [Test]
        public void EveryPieceIsAFewColouredPrimitives()
        {
            int checkedPrefabs = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", Folders))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.IsNotNull(prefab, path);
                checkedPrefabs++;

                MeshFilter[] parts = prefab.GetComponentsInChildren<MeshFilter>(true);
                Assert.LessOrEqual(parts.Length, MaxParts, $"{prefab.name} has {parts.Length} parts: keep placeholders to {MaxParts} primitives");
                foreach (MeshFilter part in parts)
                {
                    Assert.IsNotNull(part.sharedMesh, $"{prefab.name}/{part.name} has no mesh");
                    var renderer = part.GetComponent<MeshRenderer>();
                    Assert.IsNotNull(renderer, $"{prefab.name}/{part.name} has no renderer");
                    Assert.IsNotNull(renderer.sharedMaterial, $"{prefab.name}/{part.name} has no colour");
                }
            }
            Assert.Greater(checkedPrefabs, 0, "no placeholder prefabs found");
        }

        // Colours mean the same thing in every prop: each part is a swatch of the palette the
        // Placeholder Prop Builder offers (fuel red, coolant blue, machinery yellow…).
        [Test]
        public void EveryPieceIsColouredFromThePalette()
        {
            var palette = AssetDatabase.LoadAssetAtPath<PlaceholderPalette>(PlaceholderPalette.DefaultPath);
            Assert.IsNotNull(palette, $"Missing {PlaceholderPalette.DefaultPath}");
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", Folders))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (MeshRenderer part in prefab.GetComponentsInChildren<MeshRenderer>(true))
                    Assert.GreaterOrEqual(palette.IndexOf(part.sharedMaterial), 0,
                        $"{prefab.name}/{part.name} uses {(part.sharedMaterial != null ? part.sharedMaterial.name : "nothing")}, which is not in the palette");
            }
        }

        // A prop the builder writes reads back as the parts it was written from.
        [Test]
        public void TheBuildersPartsSurviveARoundTrip()
        {
            var palette = AssetDatabase.LoadAssetAtPath<PlaceholderPalette>(PlaceholderPalette.DefaultPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlaceholderPropBuilder.PathFor("FuelTank"));
            Assert.IsNotNull(prefab);

            var parts = PlaceholderPropBuilder.Read(prefab, palette, out string why);
            Assert.IsNotNull(parts, why);
            Assert.AreEqual(3, parts.Count);
            Assert.AreEqual(PlaceholderShape.Cylinder, parts[0].Shape);
            Assert.AreEqual(5.4f, parts[0].Size.y, 0.01f, "a cylinder's size is its full length");
            Assert.AreEqual(90f, parts[0].Tilt, 0.5f);
            Assert.AreEqual("Danger", palette.Swatches[parts[0].Swatch].Name, "fuel is red");
            Assert.IsEmpty(PlaceholderPropBuilder.Problems(parts, new Vector2Int(1, 2), palette));
        }
    }
}
