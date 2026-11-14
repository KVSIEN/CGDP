using System.Collections.Generic;
using CGD.BehaviorTrees;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // "Behavior settings" in the node inspector: tick a number, flag or choice of the selected
    // node to make it a named setting of this tree. Each tree or enemy type using the tree as a
    // behavior can then give it its own value in the Behavior Composer, instead of copying the
    // behavior to change one number. Give two nodes' settings the same name to drive both with
    // one knob.
    internal static class BtSettingsSection
    {
        public static void Draw(BehaviorTreeAsset asset, BtNode node, SerializedProperty element)
        {
            List<(string Field, string Label)> fields = Eligible(node, element);
            if (fields.Count == 0) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(new GUIContent("Behavior settings",
                "Ticked values become named settings of this tree: every tree and enemy type using it as a behavior " +
                "can change them in the Behavior Composer without copying it."), EditorStyles.boldLabel);

            foreach ((string field, string label) in fields)
            {
                BehaviorSetting setting = asset.FindSetting(node.Guid, field);
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool exposed = EditorGUILayout.ToggleLeft(label, setting != null, GUILayout.Width(150f));
                    if (exposed && setting == null) Expose(asset, node, field, label);
                    else if (!exposed && setting != null) Hide(asset, setting);
                    else if (setting != null) DrawName(asset, setting);
                }
            }
        }

        private static void DrawName(BehaviorTreeAsset asset, BehaviorSetting setting)
        {
            string name = EditorGUILayout.DelayedTextField(setting.Name);
            if (string.IsNullOrWhiteSpace(name) || name == setting.Name) return;

            Undo.RecordObject(asset, "Rename Behavior Setting");
            setting.Rename(name.Trim());
            Changed(asset);
        }

        private static void Expose(BehaviorTreeAsset asset, BtNode node, string field, string label)
        {
            Undo.RecordObject(asset, "Make Behavior Setting");
            asset.AddSetting(new BehaviorSetting(SuggestName(node, label), node.Guid, field));
            Changed(asset);
        }

        private static void Hide(BehaviorTreeAsset asset, BehaviorSetting setting)
        {
            Undo.RecordObject(asset, "Remove Behavior Setting");
            asset.RemoveSetting(setting);
            Changed(asset);
        }

        // A node's comment names it best ("Pounce"), else its type ("Cooldown").
        private static string SuggestName(BtNode node, string label)
        {
            string owner = !string.IsNullOrWhiteSpace(node.Comment) && node.Comment.Length <= 24 ? node.Comment.Trim() : BtNodeNames.Of(node);
            return $"{owner} {label}";
        }

        // The node's visible serialized fields that can be settings, with their inspector labels.
        private static List<(string Field, string Label)> Eligible(BtNode node, SerializedProperty element)
        {
            var fields = new List<(string, string)>();
            SerializedProperty property = element.Copy();
            SerializedProperty end      = element.GetEndProperty();
            bool enterChildren = true;
            while (property.NextVisible(enterChildren) && !SerializedProperty.EqualContents(property, end))
            {
                enterChildren = false;
                if (property.name == "_comment") continue;
                if (BehaviorSettings.FieldType(node, property.name) != null) fields.Add((property.name, property.displayName));
            }
            return fields;
        }

        private static void Changed(BehaviorTreeAsset asset)
        {
            EditorUtility.SetDirty(asset);
            BtEditorEvents.RaiseAssetEdited(asset);
        }
    }
}
