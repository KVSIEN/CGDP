using System.IO;
using System.Text;
using CGD.Level;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // Where room content lives and how a new layout is made, so every layout a designer adds
    // lands in the same place with the same naming as the rest
    // (Data/Level/Functions/Layouts/<Place>/<Name>RoomLayout.asset) and is listed by its place.
    public static class RoomAuthoring
    {
        public const string LayoutsFolder = "Assets/Project/Data/Level/Functions/Layouts";
        private const string FunctionSuffix = "RoomFunction";
        private const string LayoutSuffix = "RoomLayout";

        // The place's layout folder: its asset name without "RoomFunction" (EngineRoom).
        public static string FolderFor(RoomFunction place)
        {
            string name = place.name.EndsWith(FunctionSuffix) ? place.name.Substring(0, place.name.Length - FunctionSuffix.Length) : place.name;
            return $"{LayoutsFolder}/{name}";
        }

        // "Pump Room" → "PumpRoomRoomLayout".
        public static string LayoutAssetName(string displayName)
        {
            var name = new StringBuilder();
            bool upper = true;
            foreach (char c in displayName)
            {
                if (!char.IsLetterOrDigit(c)) { upper = true; continue; }
                name.Append(upper ? char.ToUpperInvariant(c) : c);
                upper = false;
            }
            return (name.Length > 0 ? name.ToString() : "New") + LayoutSuffix;
        }

        // A new layout of `place` called `displayName`, a copy of `copyOf` when given (its shapes,
        // structure, set pieces and sets), saved in the place's folder and added to its list.
        public static RoomLayout CreateLayout(RoomFunction place, string displayName, RoomLayout copyOf = null)
        {
            string folder = FolderFor(place);
            EnsureFolder(folder);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{LayoutAssetName(displayName)}.asset");

            RoomLayout layout = copyOf != null ? Object.Instantiate(copyOf) : ScriptableObject.CreateInstance<RoomLayout>();
            layout.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(layout, path);

            var data = new SerializedObject(layout);
            data.FindProperty("_displayName").stringValue = displayName;
            data.ApplyModifiedPropertiesWithoutUndo();

            var placeData = new SerializedObject(place);
            SerializedProperty layouts = placeData.FindProperty("_layouts");
            layouts.arraySize++;
            layouts.GetArrayElementAtIndex(layouts.arraySize - 1).objectReferenceValue = layout;
            placeData.ApplyModifiedProperties();

            AssetDatabase.SaveAssets();
            return layout;
        }

        // The first room function that lists `layout`, or null.
        public static RoomFunction PlaceOf(RoomLayout layout)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:RoomFunction"))
            {
                var function = AssetDatabase.LoadAssetAtPath<RoomFunction>(AssetDatabase.GUIDToAssetPath(guid));
                if (function == null) continue;
                foreach (RoomLayout listed in function.Layouts)
                    if (listed == layout) return function;
            }
            return null;
        }

        // The level build settings that list `place` (the ones its rooms are built with), else
        // the first there is.
        public static LevelBuildSettings SettingsFor(RoomFunction place)
        {
            LevelBuildSettings first = null;
            foreach (string guid in AssetDatabase.FindAssets("t:LevelBuildSettings"))
            {
                var settings = AssetDatabase.LoadAssetAtPath<LevelBuildSettings>(AssetDatabase.GUIDToAssetPath(guid));
                if (settings == null) continue;
                first ??= settings;
                foreach (RoomFunction function in settings.Functions)
                    if (function == place) return settings;
            }
            return first;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
