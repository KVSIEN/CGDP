using CGD.BehaviorTrees;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // A blackboard key field: type a name, or pick one from the ▾ menu of keys the agent
    // and the project's trees already use (grouped by where they come from, with their
    // kind of value), so a read and its write never drift apart by a typo.
    [CustomPropertyDrawer(typeof(BlackboardKeyAttribute))]
    internal sealed class BlackboardKeyDrawer : PropertyDrawer
    {
        private const float ButtonWidth = 20f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var field  = new Rect(position.x, position.y, position.width - ButtonWidth - 2f, position.height);
            var button = new Rect(field.xMax + 2f, position.y, ButtonWidth, position.height);

            EditorGUI.PropertyField(field, property, label);
            if (EditorGUI.DropdownButton(button, new GUIContent("▾", "Pick a known key"), FocusType.Passive))
                ShowMenu(property, (BlackboardKeyAttribute)attribute);
        }

        private static void ShowMenu(SerializedProperty property, BlackboardKeyAttribute wanted)
        {
            SerializedObject target = property.serializedObject;
            string path    = property.propertyPath;
            string current = property.stringValue;
            var    asset   = target.targetObject as BehaviorTreeAsset;

            var menu = new GenericMenu();
            var keys = BlackboardKeyCatalog.Suggestions(asset, wanted);
            if (keys.Count == 0) menu.AddDisabledItem(new GUIContent("No known keys of this kind yet"));

            foreach (BlackboardKeyInfo key in keys)
            {
                string kind  = key.AnyType ? "any" : key.Type.ToString().ToLowerInvariant();
                string entry = $"{key.Source}/{key.Name}   ({kind})";
                menu.AddItem(new GUIContent(entry, key.Description), key.Name == current, () =>
                {
                    target.Update();
                    target.FindProperty(path).stringValue = key.Name;
                    target.ApplyModifiedProperties();
                    BtEditorEvents.RaiseAssetEdited(asset);
                });
            }
            menu.ShowAsContext();
        }
    }
}
