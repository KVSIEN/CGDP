using System.Collections.Generic;
using CGD.Enemies;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // An enemy's kit (EnemyData › Kit) drawn where behaviors are picked, so a behavior that
    // needs a Signature or an Escape can be given one on the spot instead of hunting through
    // the Enemy Data inspector. Pieces the tree uses are marked; empty ones it uses are warned.
    internal static class EnemyKitSection
    {
        private static readonly string[] Fields = { "Grenade", "Signature", "Escape", "Summon", "Boost", "Support" };

        // `used` = the kit pieces the enemy's tree casts from.
        public static void Draw(SerializedObject enemy, List<KitNeed> used, ref bool expanded)
        {
            var data = (EnemyData)enemy.targetObject;
            List<KitNeed> missing = KitNeed.Missing(data, used);

            string title = missing.Count > 0 ? $"Kit   ⚠ {missing.Count} empty piece{(missing.Count == 1 ? "" : "s")} its tree uses" : "Kit";
            expanded = EditorGUILayout.Foldout(expanded, title, true);
            if (!expanded) return;

            enemy.Update();
            foreach (string field in Fields)
            {
                SerializedProperty property = enemy.FindProperty(field);
                if (property == null) continue;

                // A kit piece is named after its EnemyData field (KitNeed: "Grenade", the slot names).
                bool isUsed = used.Exists(need => need.Name == field);
                string label = isUsed ? field + "  (used)" : field;
                EditorGUILayout.PropertyField(property, new GUIContent(label, property.tooltip));
            }
            enemy.ApplyModifiedProperties();

            if (missing.Count > 0)
                EditorGUILayout.HelpBox($"Its tree casts from {KitNeed.Names(missing)}, which {(missing.Count == 1 ? "is" : "are")} empty: " +
                                        "those behaviors never fire until filled in here.", MessageType.Warning);
        }
    }
}
