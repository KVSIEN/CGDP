using UnityEditor;
using UnityEngine;
using CGD.Weapons;

namespace CGD.Editor
{
    // The firearm category inspector: the usual fields, plus a way into the Firearm Lab, where
    // its models are added, weighted and checked against the guns they roll.
    [CustomEditor(typeof(WeaponCategoryData))]
    public sealed class WeaponCategoryDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open in Firearm Lab — add models, tune weights, preview rolls", GUILayout.Height(28f)))
                FirearmLabWindow.Open((WeaponCategoryData)target);
            EditorGUILayout.Space();
            DrawDefaultInspector();
        }
    }
}
