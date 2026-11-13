using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using CGD.BehaviorTrees;
using CGD.Enemies;

namespace CGD.Editor
{
    // Finding and copying the assets an enemy type is made of, for the enemy tools. An EnemyData
    // points at shared assets (tree, senses, target priorities, doctrine, voice); changing a
    // shared one changes every enemy that uses it, so the tools show who shares what and can
    // give one enemy its own copy.
    internal static class EnemyAssetLibrary
    {
        public const string DataFolder   = "Assets/Project/Data";
        public const string PrefabFolder = "Assets/Project/Prefabs";

        public static List<EnemyData> AllEnemyData()
        {
            var all = new List<EnemyData>();
            foreach (string guid in AssetDatabase.FindAssets("t:EnemyData", new[] { DataFolder }))
            {
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>(AssetDatabase.GUIDToAssetPath(guid));
                if (data != null) all.Add(data);
            }
            return all;
        }

        // Every prefab with an enemy brain on its root, e.g. as templates for a new type.
        public static List<GameObject> AllEnemyPrefabs()
        {
            var all = new List<GameObject>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab != null && prefab.TryGetComponent(out EnemyAI _)) all.Add(prefab);
            }
            return all;
        }

        public static List<GameObject> PrefabsUsing(EnemyData data)
        {
            var users = new List<GameObject>();
            foreach (GameObject prefab in AllEnemyPrefabs())
                if (prefab.GetComponent<EnemyAI>().Data == data) users.Add(prefab);
            return users;
        }

        // How many enemy types point at this asset in any of their slots.
        public static int CountUsers(Object asset, IReadOnlyList<EnemyData> all)
        {
            if (asset == null) return 0;

            int count = 0;
            foreach (EnemyData data in all)
                if (data.BehaviorTree == asset || data.Senses == asset || data.TargetPriorities == asset ||
                    data.Doctrine == asset || data.Voice == asset) count++;
            return count;
        }

        // A copy of the asset beside the original, named for the enemy it is for
        // (<Name><Type>, e.g. StalkerBehaviorTree), never overwriting anything.
        public static T CopyFor<T>(T original, string enemyName) where T : Object
        {
            string source = AssetDatabase.GetAssetPath(original);
            string folder = Path.GetDirectoryName(source)?.Replace('\\', '/');
            string target = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{enemyName}{TypeSuffix(original)}.asset");
            if (!AssetDatabase.CopyAsset(source, target)) return null;
            return AssetDatabase.LoadAssetAtPath<T>(target);
        }

        // An enemy type's name as the assets use it: "HuskEnemyData" → "Husk".
        public static string EnemyName(EnemyData data)
        {
            const string suffix = "EnemyData";
            string name = data.name;
            return name.EndsWith(suffix) && name.Length > suffix.Length ? name[..^suffix.Length] : name;
        }

        private static string TypeSuffix(Object asset) => asset switch
        {
            BehaviorTreeAsset => "BehaviorTree",
            FactionDoctrine   => "Doctrine",
            _                 => asset.GetType().Name,
        };
    }
}
