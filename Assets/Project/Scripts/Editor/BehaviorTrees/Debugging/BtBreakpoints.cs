using System.Collections.Generic;
using System.Text;
using UnityEditor;

namespace CGD.Editor
{
    // Breakpoints set on tree nodes (by node id, which is unique across assets). They pause
    // the game when the watched agent's copy of the node starts or fails. Kept for the
    // editor session, through script reloads, not saved with the asset.
    internal static class BtBreakpoints
    {
        private const string SessionKey = "CGD.BehaviorTree.Breakpoints";

        private static Dictionary<string, BtBreakOn> _byNode;

        private static Dictionary<string, BtBreakOn> ByNode => _byNode ??= Load();

        public static BtBreakOn Get(string nodeGuid) =>
            nodeGuid != null && ByNode.TryGetValue(nodeGuid, out BtBreakOn on) ? on : BtBreakOn.None;

        public static void Set(string nodeGuid, BtBreakOn on)
        {
            if (on == BtBreakOn.None) ByNode.Remove(nodeGuid);
            else ByNode[nodeGuid] = on;
            Save();
        }

        public static bool Any => ByNode.Count > 0;

        private static Dictionary<string, BtBreakOn> Load()
        {
            var result = new Dictionary<string, BtBreakOn>();
            foreach (string pair in SessionState.GetString(SessionKey, string.Empty).Split(';'))
            {
                int colon = pair.IndexOf(':');
                if (colon > 0 && int.TryParse(pair[(colon + 1)..], out int flags))
                    result[pair[..colon]] = (BtBreakOn)flags;
            }
            return result;
        }

        private static void Save()
        {
            var text = new StringBuilder();
            foreach (var pair in ByNode) text.Append(pair.Key).Append(':').Append((int)pair.Value).Append(';');
            SessionState.SetString(SessionKey, text.ToString());
        }
    }
}
