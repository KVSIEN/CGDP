using UnityEditor;

namespace CGD.Editor
{
    // The Map Graph tools preview with the project's settings assets until another is picked.
    public static class MapEditorAssets
    {
        // The project's first asset of type T, or null.
        public static T FindFirst<T>() where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
        }
    }
}
