using System;
using System.Collections.Generic;

namespace CGD.Editor
{
    // The graph's validation issues, worked out once per change and indexed by the rooms they
    // name, so a card can ask for its own without scanning them all while drawing.
    internal sealed class MapIssueIndex
    {
        private static readonly IReadOnlyList<MapGraphIssue> NoIssues = Array.Empty<MapGraphIssue>();

        private readonly MapGraphEditorSession _session;
        private List<MapGraphIssue> _issues;
        private Dictionary<int, List<MapGraphIssue>> _byNode;

        public MapIssueIndex(MapGraphEditorSession session) => _session = session;

        public IReadOnlyList<MapGraphIssue> All
        {
            get
            {
                if (_issues != null) return _issues;

                _issues = new List<MapGraphIssue>();
                MapGraphAsset asset = _session.Asset;
                foreach (string text in MapGraphValidator.Validate(_session.Graph, _session.Analysis, asset.Layout, asset.Content, _session.Tuning))
                    _issues.Add(new MapGraphIssue(text, _session.Graph));
                return _issues;
            }
        }

        // The issues naming this room, or a connection it's an end of.
        public IReadOnlyList<MapGraphIssue> About(int nodeId)
        {
            if (_byNode == null)
            {
                _byNode = new Dictionary<int, List<MapGraphIssue>>();
                foreach (MapGraphIssue issue in All)
                {
                    if (issue.Node != null) Add(issue.Node.Id, issue);
                    if (issue.Connection == null) continue;
                    Add(issue.Connection.A, issue);
                    Add(issue.Connection.B, issue);
                }
            }
            return _byNode.TryGetValue(nodeId, out List<MapGraphIssue> issues) ? issues : NoIssues;
        }

        private void Add(int nodeId, MapGraphIssue issue)
        {
            if (!_byNode.TryGetValue(nodeId, out List<MapGraphIssue> issues)) _byNode[nodeId] = issues = new List<MapGraphIssue>();
            issues.Add(issue);
        }
    }
}
