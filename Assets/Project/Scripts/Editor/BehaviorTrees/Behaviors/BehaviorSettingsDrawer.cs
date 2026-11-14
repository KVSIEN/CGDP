using System;
using CGD.BehaviorTrees;
using CGD.Enemies;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // A behavior's settings as the Behavior Composer shows them: each one's value where the
    // edit goes (this tree's use of the behavior, or one enemy type's tuning), where that value
    // comes from (the behavior's own, this tree, this enemy), and ↺ to drop the override.
    // At runtime the enemy's value wins over the tree's, which wins over the behavior's own.
    internal static class BehaviorSettingsDrawer
    {
        private const float SourceWidth = 82f;

        // `use` = the Subtree running `behavior` in `tree` (null for the tree's own settings,
        // which only an enemy type can tune). With `editEnemy`, edits go into the enemy's tuning.
        public static void Draw(BehaviorTreeAsset tree, BehaviorTreeAsset behavior, Subtree use, EnemyData enemy, bool editEnemy)
        {
            BehaviorTuning tuning = enemy != null ? BehaviorTuning.Find(enemy.BehaviorTuning, behavior) : null;
            foreach (string name in behavior.SettingNames())
            {
                Type type = Target(behavior, name, out BtNode target, out string field);
                if (type == null)
                {
                    EditorGUILayout.LabelField(name, "(points at a setting the node no longer has)", EditorStyles.miniLabel);
                    continue;
                }

                BehaviorSettings.TryGetDefault(behavior, name, out float own);
                float useValue   = 0f;
                float enemyValue = 0f;
                bool  hasUse     = use != null && use.TryGetValue(name, out useValue);
                bool  hasEnemy   = tuning != null && tuning.TryGetValue(name, out enemyValue);

                float inherited = hasUse ? useValue : own;
                float shown     = editEnemy && hasEnemy ? enemyValue : inherited;
                bool  overrides = editEnemy ? hasEnemy : hasUse;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    BehaviorSettings.Limits(target, field, out float min, out float max);
                    var label = new GUIContent(name + LimitsText(min, max, type), Tooltip(own, hasUse, useValue, type));
                    float edited = Field(label, type, shown, overrides);
                    if (EditorGUI.EndChangeCheck()) Write(tree, behavior, use, enemy, editEnemy, name, Math.Clamp(edited, min, max));

                    GUILayout.Label(Source(editEnemy, hasEnemy, hasUse, enemy), EditorStyles.miniLabel, GUILayout.Width(SourceWidth));
                    using (new EditorGUI.DisabledScope(!overrides))
                    {
                        if (GUILayout.Button(new GUIContent("↺", "Back to the value it would have without this override"), EditorStyles.miniButton, GUILayout.Width(22f)))
                        {
                            Write(tree, behavior, use, enemy, editEnemy, name, null);
                            GUIUtility.ExitGUI();
                        }
                    }
                }

                if (!editEnemy && hasEnemy)
                    EditorGUILayout.LabelField(" ", $"{enemy.name} runs it at {Format(enemyValue, type)} (its own tuning)", EditorStyles.miniLabel);
            }
        }

        // "Cooldown 8 · Reach 3" for summaries.
        public static string Summary(BehaviorTreeAsset behavior)
        {
            var parts = new System.Collections.Generic.List<string>();
            foreach (string name in behavior.SettingNames())
            {
                Type type = Target(behavior, name, out _, out _);
                if (type != null && BehaviorSettings.TryGetDefault(behavior, name, out float value)) parts.Add($"{name} {Format(value, type)}");
            }
            return string.Join(" · ", parts);
        }

        private static void Write(BehaviorTreeAsset tree, BehaviorTreeAsset behavior, Subtree use, EnemyData enemy, bool editEnemy, string name, float? value)
        {
            if (editEnemy) BehaviorActions.SetEnemyValue(enemy, behavior, name, value);
            else if (use != null) BehaviorActions.SetUseValue(tree, use, name, value);
        }

        private static float Field(GUIContent label, Type type, float value, bool overridden)
        {
            FontStyle style = EditorStyles.label.fontStyle;
            if (overridden) EditorStyles.label.fontStyle = FontStyle.Bold;   // like a prefab override
            try
            {
                if (type == typeof(float)) return EditorGUILayout.DelayedFloatField(label, value);
                if (type == typeof(int))   return EditorGUILayout.DelayedIntField(label, Mathf.RoundToInt(value));
                if (type == typeof(bool))  return EditorGUILayout.Toggle(label, value != 0f) ? 1f : 0f;

                var choice = (Enum)Enum.ToObject(type, Mathf.RoundToInt(value));
                return Convert.ToInt32(EditorGUILayout.EnumPopup(label, choice));
            }
            finally
            {
                EditorStyles.label.fontStyle = style;
            }
        }

        private static string Source(bool editEnemy, bool hasEnemy, bool hasUse, EnemyData enemy)
        {
            if (editEnemy && hasEnemy) return $"for {enemy.name}";
            return hasUse ? "this tree" : "behavior's own";
        }

        private static string Tooltip(float own, bool hasUse, float useValue, Type type)
        {
            string text = $"The behavior's own value: {Format(own, type)}.";
            return hasUse ? text + $" This tree sets {Format(useValue, type)}." : text;
        }

        private static string Format(float value, Type type)
        {
            if (type == typeof(bool)) return value != 0f ? "on" : "off";
            if (type.IsEnum) return Enum.ToObject(type, Mathf.RoundToInt(value)).ToString();
            return type == typeof(int) ? Mathf.RoundToInt(value).ToString() : value.ToString("0.##");
        }

        // "  (0–1)" or "  (≥ 0)" after a number's name, so the allowed values are plain.
        private static string LimitsText(float min, float max, Type type)
        {
            if (type == typeof(bool) || type.IsEnum) return "";
            bool hasMin = !float.IsNegativeInfinity(min), hasMax = !float.IsPositiveInfinity(max);
            if (hasMin && hasMax) return $"  ({min:0.##}–{max:0.##})";
            return hasMin ? $"  (≥ {min:0.##})" : "";
        }

        // A setting name's type, from the first node it points at that still has the field
        // (that node and field also give its limits).
        private static Type Target(BehaviorTreeAsset behavior, string name, out BtNode node, out string field)
        {
            foreach (BehaviorSetting setting in behavior.Settings)
            {
                if (setting.Name != name) continue;
                node  = behavior.FindNode(setting.NodeGuid);
                field = setting.Field;
                Type type = node != null ? BehaviorSettings.FieldType(node, field) : null;
                if (type != null) return type;
            }
            node  = null;
            field = null;
            return null;
        }
    }
}
