using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CGD.Combat;
using CGD.Enemies;
using CGD.Factions;
using Object = UnityEngine.Object;

namespace CGD.Editor
{
    // Makes a new enemy type out of an existing one, the way a designer would by hand:
    //   <Name>EnemyData   a copy of the template's data (Data/Enemies/), renamed and re-tiered;
    //   <Name>.prefab     a variant of the template prefab (beside it) with the new data in
    //                     every slot that held the old — so the body, visuals and components
    //                     follow the template, and only the data differs;
    //   own copies        of whichever shared assets the designer wants to change for this type
    //                     alone (tree, senses, target priorities, doctrine, voice); the rest stay
    //                     shared with the template;
    //   faction roster    optionally, the prefab added to a faction's tier list, so generated
    //                     levels spawn it.
    internal static class EnemyTypeCreator
    {
        private const string EnemyDataFolder = EnemyAssetLibrary.DataFolder + "/Enemies";

        public sealed class Request
        {
            public GameObject Template;
            public string     Name        = "";
            public string     DisplayName = "";
            public int        Tier        = 1;
            public Team       Team        = Team.Enemy;
            public bool       OwnTree;
            public bool       OwnSenses;
            public bool       OwnTargetPriorities;
            public bool       OwnDoctrine;
            public bool       OwnVoice;
            public FactionDefinition Faction;
        }

        public readonly struct Result
        {
            public readonly EnemyData  Data;
            public readonly GameObject Prefab;

            public Result(EnemyData data, GameObject prefab)
            {
                Data   = data;
                Prefab = prefab;
            }
        }

        // Why the request can't be made as it stands, or null when it can.
        public static string Problem(Request request)
        {
            if (request.Template == null) return "Pick an enemy to start from.";
            if (!request.Template.TryGetComponent(out EnemyAI ai) || ai.Data == null)
                return "The template needs an EnemyAI with Enemy Data on its root.";
            if (!IsAssetName(request.Name)) return "Name it with letters and digits only, starting with a capital (e.g. Stalker).";
            if (File.Exists(DataPath(request.Name))) return $"{DataPath(request.Name)} already exists.";
            if (File.Exists(PrefabPath(request))) return $"{PrefabPath(request)} already exists.";
            return null;
        }

        public static bool IsAssetName(string name) => !string.IsNullOrEmpty(name) && Regex.IsMatch(name, "^[A-Z][A-Za-z0-9]*$");

        public static string DataPath(string name) => $"{EnemyDataFolder}/{name}EnemyData.asset";

        public static string PrefabPath(Request request)
        {
            if (request.Template == null) return null;
            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(request.Template))?.Replace('\\', '/');
            return $"{folder}/{request.Name}.prefab";
        }

        public static Result Create(Request request)
        {
            string problem = Problem(request);
            if (problem != null) throw new InvalidOperationException(problem);

            EnemyData template = request.Template.GetComponent<EnemyAI>().Data;
            EnemyData data     = CopyData(template, request);
            GameObject prefab  = CreateVariant(request, template, data);
            if (request.Faction != null) AddToFaction(request.Faction, prefab, request.Tier);

            AssetDatabase.SaveAssets();
            return new Result(data, prefab);
        }

        private static EnemyData CopyData(EnemyData template, Request request)
        {
            string path = DataPath(request.Name);
            if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(template), path))
                throw new InvalidOperationException($"Couldn't copy {template.name} to {path}.");

            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            data.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.Name : request.DisplayName.Trim();
            data.Tier        = Mathf.Clamp(request.Tier, 1, 3);
            data.Team        = request.Team;

            if (request.OwnTree             && data.BehaviorTree     != null) data.BehaviorTree     = EnemyAssetLibrary.CopyFor(data.BehaviorTree, request.Name);
            if (request.OwnSenses           && data.Senses           != null) data.Senses           = EnemyAssetLibrary.CopyFor(data.Senses, request.Name);
            if (request.OwnTargetPriorities && data.TargetPriorities != null) data.TargetPriorities = EnemyAssetLibrary.CopyFor(data.TargetPriorities, request.Name);
            if (request.OwnDoctrine         && data.Doctrine         != null) data.Doctrine         = EnemyAssetLibrary.CopyFor(data.Doctrine, request.Name);
            if (request.OwnVoice            && data.Voice            != null) data.Voice            = EnemyAssetLibrary.CopyFor(data.Voice, request.Name);

            EditorUtility.SetDirty(data);
            return data;
        }

        // Built in a preview scene so the open scene is never touched.
        private static GameObject CreateVariant(Request request, EnemyData oldData, EnemyData newData)
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(request.Template, scene);
                instance.name = request.Name;
                foreach (Component component in instance.GetComponentsInChildren<Component>(true))
                    Replace(component, oldData, newData);
                return PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath(request));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        // Every field on the component that pointed at the template's data (EnemyAI, EnemyHealth, …).
        private static void Replace(Component component, Object from, Object to)
        {
            if (component == null) return;   // a missing script

            var serialized = new SerializedObject(component);
            SerializedProperty property = serialized.GetIterator();
            bool changed = false;
            while (property.Next(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue != from) continue;
                property.objectReferenceValue = to;
                changed = true;
            }
            if (changed) serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddToFaction(FactionDefinition faction, GameObject prefab, int tier)
        {
            var serialized = new SerializedObject(faction);
            SerializedProperty roster = serialized.FindProperty($"_tier{Mathf.Clamp(tier, 1, 3)}");
            if (roster == null) return;

            roster.arraySize++;
            roster.GetArrayElementAtIndex(roster.arraySize - 1).objectReferenceValue = prefab;
            serialized.ApplyModifiedProperties();
        }

        // The enemy prefabs a new type can start from, by name (for a picker).
        public static List<GameObject> Templates()
        {
            List<GameObject> all = EnemyAssetLibrary.AllEnemyPrefabs();
            all.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return all;
        }
    }
}
