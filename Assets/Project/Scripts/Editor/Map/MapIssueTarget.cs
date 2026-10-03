using System.Text.RegularExpressions;
using CGD.Map;

namespace CGD.Editor
{
    // What a validation message is about, read back from its text: validator messages
    // name a connection as "#a–#b" and a room as "#id". Parsing keeps MapGraphValidator's
    // plain-string results (used by tests and the style report) unchanged.
    public static class MapIssueTarget
    {
        private static readonly Regex ConnectionPattern = new(@"#(\d+)\s*[–-]\s*#(\d+)");
        private static readonly Regex NodePattern       = new(@"#(\d+)");

        public static bool TryFind(string issue, MapGraph graph, out MapNode node, out MapConnection connection)
        {
            node       = null;
            connection = null;

            Match pair = ConnectionPattern.Match(issue);
            if (pair.Success)
            {
                connection = graph.GetConnection(int.Parse(pair.Groups[1].Value), int.Parse(pair.Groups[2].Value));
                if (connection != null) return true;
            }

            Match single = NodePattern.Match(issue);
            return single.Success && graph.TryGetNode(int.Parse(single.Groups[1].Value), out node);
        }
    }
}
