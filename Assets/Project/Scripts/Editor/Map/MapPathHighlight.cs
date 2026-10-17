using System.Collections.Generic;
using CGD.Map;

namespace CGD.Editor
{
    // The nodes, and the links between them, on the shortest route from Start to the selected
    // node. Built on first ask after the selection or graph changes.
    internal sealed class MapPathHighlight
    {
        private readonly HashSet<int> _nodes = new();
        private readonly HashSet<(int, int)> _links = new();

        public MapPathHighlight(MapGraphEditorSession session)
        {
            if (!session.HighlightPath || session.SelectedNode == null) return;

            List<int> path = session.Analysis.ShortestPath(session.Analysis.StartId, session.SelectedNodeId);
            _nodes.UnionWith(path);
            for (int i = 1; i < path.Count; i++)
                _links.Add(LinkKey(path[i - 1], path[i]));
        }

        public bool Contains(int id) => _nodes.Contains(id);

        public bool Contains(MapConnection connection) => _links.Contains(LinkKey(connection.A, connection.B));

        private static (int, int) LinkKey(int a, int b) => a < b ? (a, b) : (b, a);
    }
}
