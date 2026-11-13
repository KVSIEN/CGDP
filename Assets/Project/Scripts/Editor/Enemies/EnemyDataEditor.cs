using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CGD.BehaviorTrees;
using CGD.Enemies;

namespace CGD.Editor
{
    // The EnemyData inspector, laid out for designers: what the numbers add up to and anything
    // that looks wrong at the top, then the assets that make up its mind — each with how many
    // enemy types share it and a button to give this one its own copy — then the usual fields.
    [CustomEditor(typeof(EnemyData))]
    public sealed class EnemyDataEditor : UnityEditor.Editor
    {
        // The asset slots drawn in the "Mind" section, so the default list below leaves them out.
        private static readonly string[] MindFields = { "m_Script", "BehaviorTree", "Senses", "TargetPriorities", "Doctrine", "Voice" };

        private List<EnemyData> _allData;
        private List<GameObject> _prefabs;

        private void OnEnable() => _allData = EnemyAssetLibrary.AllEnemyData();

        public override void OnInspectorGUI()
        {
            var data = (EnemyData)target;
            serializedObject.Update();

            DrawOverview(data);
            DrawMind(data);
            DrawTools(data);

            EditorGUILayout.Space(8f);
            DrawPropertiesExcluding(serializedObject, MindFields);
            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawOverview(EnemyData data)
        {
            EditorGUILayout.LabelField("At a glance", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                foreach (string line in EnemyDataReview.Summary(data))
                    EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);
            }

            foreach (EnemyDataReview.Issue issue in EnemyDataReview.Check(data))
                EditorGUILayout.HelpBox(issue.Message, ToMessageType(issue.Severity));
        }

        private void DrawMind(EnemyData data)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Mind", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "What it thinks with. A shared asset changes every enemy that uses it; " +
                "Make Own Copy to change it for this enemy alone.", EditorStyles.wordWrappedMiniLabel);

            MindSlot(data, "BehaviorTree");
            MindSlot(data, "Senses");
            MindSlot(data, "TargetPriorities");
            MindSlot(data, "Doctrine");
            MindSlot(data, "Voice");
        }

        private void MindSlot(EnemyData data, string field)
        {
            SerializedProperty property = serializedObject.FindProperty(field);
            Object asset = property.objectReferenceValue;
            int users = EnemyAssetLibrary.CountUsers(asset, _allData);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(property);

                string shared = users > 1 ? $"shared ×{users}" : asset != null ? "own" : "";
                GUILayout.Label(new GUIContent(shared, users > 1 ? $"{users} enemy types use this asset" : null),
                                EditorStyles.miniLabel, GUILayout.Width(58f));

                using (new EditorGUI.DisabledScope(asset == null))
                {
                    if (GUILayout.Button("Open", EditorStyles.miniButtonLeft, GUILayout.Width(44f))) OpenAsset(asset);
                }
                using (new EditorGUI.DisabledScope(users < 2))
                {
                    if (GUILayout.Button(new GUIContent("Make Own Copy", "Copies the asset for this enemy and points this slot at the copy"),
                                         EditorStyles.miniButtonRight, GUILayout.Width(100f)))
                        MakeOwnCopy(data, property, asset);
                }
            }
        }

        private void MakeOwnCopy(EnemyData data, SerializedProperty property, Object asset)
        {
            Object copy = EnemyAssetLibrary.CopyFor(asset, EnemyAssetLibrary.EnemyName(data));
            if (copy == null) return;

            property.objectReferenceValue = copy;
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            _allData = EnemyAssetLibrary.AllEnemyData();
            EditorGUIUtility.PingObject(copy);
            GUIUtility.ExitGUI();
        }

        private static void OpenAsset(Object asset)
        {
            if (asset is BehaviorTreeAsset tree)
            {
                BehaviorTreeWindow.Open(tree);
                return;
            }
            Selection.activeObject = asset;
        }

        private void DrawTools(EnemyData data)
        {
            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Behavior Composer", "Switch its behaviors on and off, reorder them, add more from the library")))
                    BehaviorComposerWindow.Open(data, data.BehaviorTree);
                if (GUILayout.Button("Enemy Roster")) EnemyRosterWindow.OpenWindow();
                if (GUILayout.Button(new GUIContent("Find Prefabs", "Lists the enemy prefabs that use this data")))
                    _prefabs = EnemyAssetLibrary.PrefabsUsing(data);
                if (GUILayout.Button(new GUIContent("New Type From This…", "Starts the New Enemy Type window from a prefab that uses this data")))
                {
                    List<GameObject> prefabs = EnemyAssetLibrary.PrefabsUsing(data);
                    NewEnemyTypeWindow.Open(prefabs.Count > 0 ? prefabs[0] : null);
                }
            }

            if (_prefabs == null) return;
            if (_prefabs.Count == 0) EditorGUILayout.HelpBox("No enemy prefab uses this data yet.", MessageType.Info);
            foreach (GameObject prefab in _prefabs)
                if (GUILayout.Button(prefab.name, EditorStyles.linkLabel)) EditorGUIUtility.PingObject(prefab);
        }

        private static MessageType ToMessageType(BtIssueSeverity severity) => severity switch
        {
            BtIssueSeverity.Error   => MessageType.Error,
            BtIssueSeverity.Warning => MessageType.Warning,
            _                       => MessageType.Info,
        };
    }
}
