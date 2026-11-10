using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CGD.Core;

namespace CGD.Editor
{
    // Puts every IRestoredAfterPlayMode asset (input bindings, spectator settings) back the way
    // it was authored when Play mode ends. Playing loads saved bindings into those assets and
    // rebinding writes into them; without this the tester's own setup would linger in the
    // editor and get saved into the project with the next asset save. Edits made to them in
    // the Inspector during Play are undone too, as Play mode does for scene objects.
    // The snapshot lives in SessionState because entering Play reloads the scripts.
    [InitializeOnLoad]
    internal static class PlayModeAssetRestore
    {
        private const string Prefix   = "CGD.PlayModeAssetRestore.";
        private const string GuidList = Prefix + "Guids";

        static PlayModeAssetRestore() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode) Snapshot();
            else if (change == PlayModeStateChange.EnteredEditMode) Restore();
        }

        private static void Snapshot()
        {
            var guids = new List<string>();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<IRestoredAfterPlayMode>())
            {
                if (type.IsAbstract || !typeof(ScriptableObject).IsAssignableFrom(type)) continue;

                foreach (string guid in AssetDatabase.FindAssets("t:" + type.Name))
                {
                    ScriptableObject asset = Load(guid);
                    if (asset == null) continue;
                    SessionState.SetString(Prefix + guid, EditorJsonUtility.ToJson(asset));
                    guids.Add(guid);
                }
            }
            SessionState.SetString(GuidList, string.Join(";", guids));
        }

        private static void Restore()
        {
            string list = SessionState.GetString(GuidList, string.Empty);
            SessionState.EraseString(GuidList);

            foreach (string guid in list.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string json = SessionState.GetString(Prefix + guid, string.Empty);
                SessionState.EraseString(Prefix + guid);

                ScriptableObject asset = Load(guid);
                if (asset != null && json.Length > 0) EditorJsonUtility.FromJsonOverwrite(json, asset);
            }
        }

        private static ScriptableObject Load(string guid) =>
            AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
    }
}
