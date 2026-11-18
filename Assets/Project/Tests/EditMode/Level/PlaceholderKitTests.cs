using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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
    }
}
