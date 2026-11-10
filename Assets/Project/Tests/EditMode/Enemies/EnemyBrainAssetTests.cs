using NUnit.Framework;
using UnityEditor;
using CGD.Enemies;

namespace CGD.Tests
{
    // Every enemy thinks with a behavior tree (there is no other brain), so every shipped
    // EnemyData must name one that can actually run.
    public class EnemyBrainAssetTests
    {
        [Test]
        public void EveryEnemyDataHasARunnableTree()
        {
            string[] guids = AssetDatabase.FindAssets("t:EnemyData", new[] { "Assets/Project/Data" });
            Assert.IsNotEmpty(guids, "No EnemyData assets found");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
                Assert.IsNotNull(data.BehaviorTree, $"{path} has no behavior tree");
                Assert.IsNotNull(data.BehaviorTree.Root, $"{path}'s tree has no root");
            }
        }
    }
}
