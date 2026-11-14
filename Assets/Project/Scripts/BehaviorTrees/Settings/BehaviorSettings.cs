using System;
using System.Collections.Generic;
using System.Reflection;

namespace CGD.BehaviorTrees
{
    // Reads and writes the node fields behind BehaviorSettings. Values are written into an
    // agent's own node copies while its tree is built (never into the asset, never per tick),
    // so one behavior can run with different values for each tree and enemy type using it.
    internal static class BehaviorSettings
    {
        private static readonly Dictionary<(Type, string), FieldInfo> Fields = new();

        // Writes each value into the copies of the nodes its setting names. Values naming no
        // setting of this asset are ignored (the tree checks report them).
        public static void Apply(BehaviorTreeAsset asset, Dictionary<string, BtNode> copies, IReadOnlyList<SettingValue> values)
        {
            if (values == null || values.Count == 0) return;

            foreach (SettingValue value in values)
            foreach (BehaviorSetting setting in asset.Settings)
            {
                if (setting.Name != value.Name) continue;
                if (copies.TryGetValue(setting.NodeGuid, out BtNode node)) TrySet(node, setting.Field, value.Value);
            }
        }

        // The setting's value as authored on the asset's node (its default).
        public static bool TryGetDefault(BehaviorTreeAsset asset, string name, out float value)
        {
            foreach (BehaviorSetting setting in asset.Settings)
            {
                if (setting.Name != name) continue;
                BtNode node = asset.FindNode(setting.NodeGuid);
                if (node != null && TryGet(node, setting.Field, out value)) return true;
            }
            value = 0f;
            return false;
        }

        // The field's type, for the editor to draw the right control; null if it can't be a setting.
        public static Type FieldType(BtNode node, string field)
        {
            FieldInfo info = Find(node.GetType(), field);
            return info != null && CanBeSetting(info.FieldType) ? info.FieldType : null;
        }

        public static bool CanBeSetting(Type type) =>
            type == typeof(float) || type == typeof(int) || type == typeof(bool) || type.IsEnum;

        public static bool TryGet(BtNode node, string field, out float value)
        {
            value = 0f;
            FieldInfo info = Find(node.GetType(), field);
            if (info == null || !CanBeSetting(info.FieldType)) return false;

            object raw = info.GetValue(node);
            value = raw switch
            {
                float number => number,
                int whole    => whole,
                bool flag    => flag ? 1f : 0f,
                Enum choice  => Convert.ToInt32(choice),
                _            => 0f,
            };
            return true;
        }

        public static bool TrySet(BtNode node, string field, float value)
        {
            FieldInfo info = Find(node.GetType(), field);
            if (info == null || !CanBeSetting(info.FieldType)) return false;

            Type type = info.FieldType;
            object converted;
            if (type == typeof(float))     converted = value;
            else if (type == typeof(int))  converted = (int)Math.Round(value);
            else if (type == typeof(bool)) converted = value != 0f;
            else                           converted = Enum.ToObject(type, (int)Math.Round(value));

            info.SetValue(node, converted);
            return true;
        }

        private static FieldInfo Find(Type type, string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (Fields.TryGetValue((type, name), out FieldInfo cached)) return cached;

            FieldInfo found = null;
            for (Type t = type; t != null && found == null; t = t.BaseType)
                found = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            Fields[(type, name)] = found;
            return found;
        }
    }
}
