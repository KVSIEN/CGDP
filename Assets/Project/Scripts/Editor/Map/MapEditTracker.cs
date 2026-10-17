using System.Collections.Generic;
using CGD.Map;

namespace CGD.Editor
{
    // Which rooms differ from the generated graph (or aren't in it), and whether anything
    // does. Settled when an edit is made, so a dragged node shows as edited once it's dropped.
    internal sealed class MapEditTracker
    {
        private readonly MapGraphEditorSession _session;
        private HashSet<int> _editedNodes;
        private bool _hasEdits;

        public MapEditTracker(MapGraphEditorSession session) => _session = session;

        public bool IsEdited(MapNode node)
        {
            Build();
            return _editedNodes.Contains(node.Id);
        }

        public bool HasEdits()
        {
            Build();
            return _hasEdits;
        }

        private void Build()
        {
            if (_editedNodes != null) return;

            MapGraph graph = _session.Graph;
            MapGraph generated = _session.Asset.Generated;
            _editedNodes = new HashSet<int>();
            foreach (MapNode node in graph.Nodes)
                if (!generated.TryGetNode(node.Id, out MapNode original) || !node.SameAs(original))
                    _editedNodes.Add(node.Id);

            _hasEdits = _editedNodes.Count > 0
                     || generated.Nodes.Count != graph.Nodes.Count
                     || generated.Connections.Count != graph.Connections.Count;
            if (_hasEdits) return;

            foreach (MapConnection connection in graph.Connections)
                if (!connection.SameAs(generated.GetConnection(connection.A, connection.B)))
                {
                    _hasEdits = true;
                    return;
                }
        }
    }
}
