using CGD.Map;

namespace CGD.Editor
{
    // One validation message and what it's about (MapIssueTarget), resolved once per graph
    // change so the canvas and side panel don't re-parse the text on every repaint.
    public sealed class MapGraphIssue
    {
        public readonly string        Text;
        public readonly MapNode       Node;        // null when it names a connection, or nothing
        public readonly MapConnection Connection;  // null when it names a room, or nothing

        public MapGraphIssue(string text, MapGraph graph)
        {
            Text = text;
            MapIssueTarget.TryFind(text, graph, out Node, out Connection);
        }

        public bool HasTarget => Node != null || Connection != null;

        // Whether it names this room or a connection the room is an end of.
        public bool IsAbout(int nodeId) =>
            Node != null ? Node.Id == nodeId : Connection != null && Connection.Connects(nodeId);
    }
}
