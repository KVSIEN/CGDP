using System;
using System.Collections.Generic;
using CGD.Map;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // The editor graph: everything the Map Graph window knows about the asset that
    // isn't map data — selection, view, highlights, cached analysis — plus the one
    // place edits happen, so each is undoable and refreshes the cached analysis.
    [Serializable]
    public class MapGraphEditorSession
    {
        public const int None = -1;

        private const float MinZoom = 0.3f;
        private const float MaxZoom = 2f;

        [SerializeField] private MapGraphAsset    _asset;
        [SerializeField] private int              _selectedNodeId = None;
        [SerializeField] private int              _selectedConnectionA = None;
        [SerializeField] private int              _selectedConnectionB = None;
        [SerializeField] private MapGraphViewMode _viewMode;
        [SerializeField] private bool             _highlightPath = true;
        [SerializeField] private bool             _highlightBranch;
        [SerializeField] private Vector2          _pan;
        [SerializeField] private float            _zoom = 1f;

        [NonSerialized] private MapGraphAnalysis _analysis;
        [NonSerialized] private List<string>     _issues;
        [NonSerialized] private HashSet<int>     _highlightedPath;
        [NonSerialized] private List<int>        _highlightedPathOrder = new();

        public MapGraphAsset Asset
        {
            get => _asset;
            set
            {
                if (_asset == value) return;
                _asset = value;
                ClearSelection();
                Invalidate();
            }
        }

        public bool     HasGraph => _asset != null;
        public MapGraph Graph    => _asset.Graph;

        public MapGraphViewMode ViewMode
        {
            get => _viewMode;
            set => _viewMode = value;
        }

        public bool HighlightPath
        {
            get => _highlightPath;
            set { _highlightPath = value; _highlightedPath = null; }
        }

        public bool HighlightBranch
        {
            get => _highlightBranch;
            set => _highlightBranch = value;
        }

        public Vector2 Pan
        {
            get => _pan;
            set => _pan = value;
        }

        public float Zoom
        {
            get => _zoom;
            set => _zoom = Mathf.Clamp(value, MinZoom, MaxZoom);
        }

        public MapGraphAnalysis Analysis => _analysis ??= new MapGraphAnalysis(Graph);

        public IReadOnlyList<string> Issues =>
            _issues ??= MapGraphValidator.Validate(Graph, Analysis, _asset.Settings);

        // Call after anything outside this class changes the asset (undo, regenerate).
        public void Invalidate()
        {
            _analysis        = null;
            _issues          = null;
            _highlightedPath = null;
        }

        // --- Selection -------------------------------------------------------------

        public int SelectedNodeId => _selectedNodeId;

        public MapNode SelectedNode =>
            HasGraph && Graph.TryGetNode(_selectedNodeId, out MapNode node) ? node : null;

        public MapConnection SelectedConnection =>
            HasGraph ? Graph.GetConnection(_selectedConnectionA, _selectedConnectionB) : null;

        public void SelectNode(int id)
        {
            ClearSelection();
            _selectedNodeId = id;
        }

        public void SelectConnection(MapConnection connection)
        {
            ClearSelection();
            _selectedConnectionA = connection.A;
            _selectedConnectionB = connection.B;
        }

        public void ClearSelection()
        {
            _selectedNodeId      = None;
            _selectedConnectionA = None;
            _selectedConnectionB = None;
            _highlightedPath     = null;
        }

        // --- Highlights ------------------------------------------------------------

        // Nodes on the shortest route from Start to the selected node.
        public bool IsOnHighlightedPath(int id) => HighlightedPath.Contains(id);

        public bool IsOnHighlightedPath(MapConnection connection)
        {
            if (!HighlightedPath.Contains(connection.A)) return false;

            for (int i = 1; i < _highlightedPathOrder.Count; i++)
                if (connection.Links(_highlightedPathOrder[i - 1], _highlightedPathOrder[i]))
                    return true;
            return false;
        }

        // With branch highlighting on and a branch node selected, everything outside that
        // branch is dimmed. Main path selections dim nothing.
        public bool IsDimmed(int id)
        {
            if (!_highlightBranch || SelectedNode == null) return false;

            int branch = Analysis.BranchOf(_selectedNodeId);
            return branch != MapGraphAnalysis.MainPathBranch && Analysis.BranchOf(id) != branch;
        }

        private HashSet<int> HighlightedPath
        {
            get
            {
                if (_highlightedPath != null) return _highlightedPath;

                _highlightedPathOrder = _highlightPath && SelectedNode != null
                    ? Analysis.ShortestPath(Analysis.StartId, _selectedNodeId)
                    : new List<int>();
                _highlightedPath = new HashSet<int>(_highlightedPathOrder);
                return _highlightedPath;
            }
        }

        // --- Edit state vs. generated ---------------------------------------------

        public bool IsEdited(MapNode node)
        {
            if (!_asset.Generated.TryGetNode(node.Id, out MapNode original)) return true;

            return original.Type != node.Type
                || original.Position != node.Position
                || !Mathf.Approximately(original.Intensity, node.Intensity)
                || original.Faction != node.Faction
                || !Mathf.Approximately(original.FactionInfluence, node.FactionInfluence);
        }

        public bool HasEdits()
        {
            MapGraph generated = _asset.Generated;
            if (generated.Nodes.Count != Graph.Nodes.Count) return true;
            if (generated.Connections.Count != Graph.Connections.Count) return true;

            foreach (MapNode node in Graph.Nodes)
                if (IsEdited(node)) return true;

            foreach (MapConnection connection in Graph.Connections)
            {
                MapConnection original = generated.GetConnection(connection.A, connection.B);
                if (original == null || original.Type != connection.Type) return true;
            }
            return false;
        }

        // --- Edits -----------------------------------------------------------------

        public void Regenerate(int seed)
        {
            Record("Regenerate Map");
            _asset.Regenerate(seed);
            ClearSelection();
            Changed();
        }

        public void RevertEdits()
        {
            Record("Revert Map Edits");
            _asset.RevertEdits();
            Changed();
        }

        public void AddNode(MapNodeType type, Vector2 position)
        {
            Record("Add Map Node");
            SelectNode(Graph.AddNode(type, position).Id);
            Changed();
        }

        public void DeleteNode(int id)
        {
            Record("Delete Map Node");
            _asset.RemoveNode(id);
            if (_selectedNodeId == id) ClearSelection();
            Changed();
        }

        // Call once when a drag starts; MoveNode then only applies the offset.
        public void BeginMove() => Record("Move Map Node");

        public void MoveNode(MapNode node, Vector2 position)
        {
            node.Position = position;
            EditorUtility.SetDirty(_asset);
        }

        public void SetType(MapNode node, MapNodeType type)
        {
            Record("Change Map Node Type");
            node.Type = type;
            Changed();
        }

        public void SetIntensity(MapNode node, float intensity)
        {
            Record("Change Map Node Intensity");
            node.Intensity = intensity;
            Changed();
        }

        public void SetFaction(MapNode node, int faction, float influence)
        {
            Record("Change Map Node Faction");
            node.SetFaction(faction, influence);
            Changed();
        }

        public void SetLocked(MapNode node, bool locked)
        {
            Record(locked ? "Lock Map Node" : "Unlock Map Node");
            _asset.SetLocked(node.Id, locked);
            Changed();
        }

        public void Connect(int a, int b)
        {
            if (Graph.AreConnected(a, b)) return;

            Record("Connect Map Nodes");
            Graph.Connect(a, b);
            Changed();
        }

        public void Disconnect(int a, int b)
        {
            Record("Disconnect Map Nodes");
            Graph.Disconnect(a, b);
            Changed();
        }

        public void SetConnectionType(MapConnection connection, ConnectionType type)
        {
            Record("Change Connection Type");
            connection.Type = type;
            Changed();
        }

        private void Record(string name) => Undo.RecordObject(_asset, name);

        private void Changed()
        {
            EditorUtility.SetDirty(_asset);
            Invalidate();
        }
    }
}
