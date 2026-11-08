using System.Collections.Generic;
using System.Text;
using CGD.BehaviorTrees;

namespace CGD.DevTools
{
    // The inspector's blackboard page: every key on the agent's tree blackboard, in name order.
    internal static class BlackboardPage
    {
        private static readonly List<string> Keys = new();

        public static void Write(StringBuilder text, BehaviorTree tree)
        {
            text.Heading("Blackboard");
            if (tree == null)
            {
                text.Coloured(InspectorText.Muted, "no tree").Append('\n');
                return;
            }

            IReadOnlyDictionary<string, BlackboardValue> values = tree.Blackboard.Values;
            Keys.Clear();
            foreach (string key in values.Keys) Keys.Add(key);
            Keys.Sort(System.StringComparer.Ordinal);

            foreach (string key in Keys) text.Row(key, values[key].ToString());
            if (Keys.Count == 0) text.Coloured(InspectorText.Muted, "empty").Append('\n');
        }
    }
}
