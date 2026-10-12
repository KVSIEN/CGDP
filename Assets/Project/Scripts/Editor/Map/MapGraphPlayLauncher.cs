using System;
using CGD.Level;
using CGD.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CGD.Editor
{
    // Plays a Map Graph asset (Map Graph window › Play): opens the MapTest scene, points its
    // LevelBuilder at the asset — the asset's own seed, optionally starting in one room — and
    // enters play mode. The scene file is never changed: when play mode ends, the scenes that
    // were open before are reloaded from disk, which also drops the temporary wiring.
    [InitializeOnLoad]
    public static class MapGraphPlayLauncher
    {
        private const string ScenePath  = "Assets/Project/Scenes/MapTest.unity";
        // Survives the domain reload of entering play mode.
        private const string RestoreKey = "CGD.MapGraphPlay.RestoreScenes";

        [Serializable]
        private struct OpenScene
        {
            public string Path;
            public bool   IsLoaded;
            public bool   IsActive;
        }

        [Serializable]
        private class OpenScenes
        {
            public OpenScene[] Scenes;
        }

        static MapGraphPlayLauncher() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        public static bool CanPlay(MapGraphAsset asset) =>
            asset != null && asset.Graph.Nodes.Count > 0 && !EditorApplication.isPlayingOrWillChangePlaymode;

        // `startRoom`: a node id to start in, or -1 for the Start room.
        public static void Play(MapGraphAsset asset, int startRoom = -1)
        {
            if (!CanPlay(asset) || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.SaveAssetIfDirty(asset);

            OpenScenes before = Remember(EditorSceneManager.GetSceneManagerSetup());
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            LevelBuilder builder = FindBuilder(scene);
            if (builder == null)
            {
                Restore(before);
                EditorUtility.DisplayDialog("Play Map Graph", $"{ScenePath} has no LevelBuilder to build the graph with.", "OK");
                return;
            }

            var serialized = new SerializedObject(builder);
            serialized.FindProperty("_graphAsset").objectReferenceValue = asset;
            serialized.FindProperty("_seed").stringValue = "";
            serialized.FindProperty("_startRoom").intValue = startRoom;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            SessionState.SetString(RestoreKey, JsonUtility.ToJson(before));
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode) return;

            string json = SessionState.GetString(RestoreKey, "");
            if (json.Length == 0) return;
            SessionState.EraseString(RestoreKey);
            Restore(JsonUtility.FromJson<OpenScenes>(json));
        }

        private static LevelBuilder FindBuilder(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                LevelBuilder builder = root.GetComponentInChildren<LevelBuilder>(true);
                if (builder != null) return builder;
            }
            return null;
        }

        private static OpenScenes Remember(SceneSetup[] setup)
        {
            var scenes = new OpenScene[setup.Length];
            for (int i = 0; i < setup.Length; i++)
                scenes[i] = new OpenScene { Path = setup[i].path, IsLoaded = setup[i].isLoaded, IsActive = setup[i].isActive };
            return new OpenScenes { Scenes = scenes };
        }

        // Reloads the remembered scenes from disk. An empty scene first, so a scene that is
        // still open (MapTest itself) is read again rather than kept with the temporary wiring.
        // Without a saved scene to go back to, MapTest is reloaded on its own.
        private static void Restore(OpenScenes before)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            OpenScene[] scenes = before?.Scenes ?? Array.Empty<OpenScene>();
            if (scenes.Length == 0 || Array.Exists(scenes, s => string.IsNullOrEmpty(s.Path)))
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                return;
            }

            var setup = new SceneSetup[scenes.Length];
            for (int i = 0; i < scenes.Length; i++)
                setup[i] = new SceneSetup { path = scenes[i].Path, isLoaded = scenes[i].IsLoaded, isActive = scenes[i].IsActive };
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }
}
