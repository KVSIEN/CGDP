using System;
using System.Collections.Generic;
using CGD.Level;
using CGD.Map;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // The editor graph: everything the Map Graph window knows about the asset that
    // isn't map data — selection, view, highlights, the build settings it previews with —
    // plus the one place edits happen, so each is undoable and refreshes what's cached.
    //
    // Everything derived from the graph (analysis, issues, edits, lock numbering, the
    // highlighted path) is built once per change and kept until Invalidate, so drawing —
    // which happens on every mouse move — only reads it.
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
        [SerializeField] private bool             _snapToGrid = true;
        [SerializeField] private bool             _showObjectives = true;
        [SerializeField] private bool             _showDetails = true;
        [SerializeField] private LevelBuildSettings _buildSettings;

        [NonSerialized] private bool                _searchedBuildSettings;
        [NonSerialized] private MapGraphAnalysis    _analysis;
        [NonSerialized] private List<MapGraphIssue> _issues;
        [NonSerialized] private Dictionary<int, List<MapGraphIssue>> _issuesByNode;
        [NonSerialized] private HashSet<int>        _editedNodes;
        [NonSerialized] private bool                _hasEdits;
        [NonSerialized] private List<MapConnection> _locks;
        [NonSerialized] private MapRunTuning        _tuning;
        [NonSerialized] private MapFactionMix       _factionMix;
        [NonSerialized] private bool                _factionMixRead;
        [NonSerialized] private HashSet<int>        _highlightedPath;
        [NonSerialized] private HashSet<(int, int)> _highlightedLinks;
        [NonSerialized] private MapObjectivePreview _objectives;
        [NonSerialized] private MapRoomPreview      _rooms;

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
            set { _highlightPath = value; ClearHighlightedPath(); }
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

        // Moved and added nodes land on the level's room grid.
        public bool SnapToGrid
        {
            get => _snapToGrid;
            set => _snapToGrid = value;
        }

        // The size of one grid cell (one room) in graph units, as the level builder uses it.
        public Vector2 CellSpacing
        {
            get
            {
                Vector2 spacing = HasGraph ? _asset.NodeSpacing : MapGenerationSettings.DefaultNodeSpacing;
                return new Vector2(Mathf.Max(1f, spacing.x), Mathf.Max(1f, spacing.y));
            }
        }

        // The level build settings every preview uses — room places and enemies on the cards,
        // objectives, the blueprint. The project's first until another is picked.
        public LevelBuildSettings BuildSettings
        {
            get
            {
                if (_buildSettings == null && !_searchedBuildSettings)
                {
                    _searchedBuildSettings = true;
                    _buildSettings = MapEditorAssets.FindFirst<LevelBuildSettings>();
                }
                return _buildSettings;
            }
            set
            {
                if (_buildSettings == value) return;
                _buildSettings = value;
                Invalidate();
            }
        }

        public bool ShowObjectives
        {
            get => _showObjectives;
            set => _showObjectives = value;
        }

        // Where a level built from this graph would put its objectives.
        public MapObjectivePreview Objectives => _objectives ??= new MapObjectivePreview();

        // Rich node cards: tier, intensity bar, room place, route, enemies, issues.
        public bool ShowDetails
        {
            get => _showDetails;
            set => _showDetails = value;
        }

        // Which room (place, cells) a level built from this graph would give each node.
        public MapRoomPreview Rooms => _rooms ??= new MapRoomPreview();

        public MapGraphAnalysis Analysis => _analysis ??= new MapGraphAnalysis(Graph);

        // The asset's run modifiers and faction mix, read once per change (the asset works
        // them out, for older maps from the seed, on every read).
        public MapRunTuning Tuning => _tuning ??= _asset.Tuning;

        public MapFactionMix FactionMix
        {
            get
            {
                if (_factionMixRead) return _factionMix;
                _factionMixRead = true;
                return _factionMix = _asset.FactionMix;
            }
        }

        public IReadOnlyList<MapGraphIssue> Issues
        {
            get
            {
                if (_issues != null) return _issues;

                _issues = new List<MapGraphIssue>();
                foreach (string text in MapGraphValidator.Validate(Graph, Analysis, _asset.Layout, _asset.Content, Tuning))
                    _issues.Add(new MapGraphIssue(text, Graph));
                return _issues;
            }
        }

        // The issues naming this room, or a connection it's an end of.
        public IReadOnlyList<MapGraphIssue> IssuesAbout(int nodeId)
        {
            if (_issuesByNode == null)
            {
                _issuesByNode = new Dictionary<int, List<MapGraphIssue>>();
                foreach (MapGraphIssue issue in Issues)
                {
                    if (issue.Node != null) AddIssue(issue.Node.Id, issue);
                    if (issue.Connection == null) continue;
                    AddIssue(issue.Connection.A, issue);
                    AddIssue(issue.Connection.B, issue);
                }
            }
            return _issuesByNode.TryGetValue(nodeId, out List<MapGraphIssue> issues) ? issues : NoIssues;
        }

        private static readonly IReadOnlyList<MapGraphIssue> NoIssues = Array.Empty<MapGraphIssue>();

        private void AddIssue(int nodeId, MapGraphIssue issue)
        {
            if (!_issuesByNode.TryGetValue(nodeId, out List<MapGraphIssue> issues)) _issuesByNode[nodeId] = issues = new List<MapGraphIssue>();
            issues.Add(issue);
        }

        // Locked connections in graph order: the first is L1 everywhere the window names locks.
        public IReadOnlyList<MapConnection> Locks
        {
            get
            {
                if (_locks != null) return _locks;

                _locks = new List<MapConnection>();
                foreach (MapConnection connection in Graph.Connections)
                    if (connection.Type == ConnectionType.Locked) _locks.Add(connection);
                return _locks;
            }
        }

        // "L2"; empty for a connection that isn't locked.
        public string LockName(MapConnection connection)
        {
            int index = IndexOf(Locks, connection);
            return index >= 0 ? MapNodeInfo.LockName(index) : "";
        }

        private static int IndexOf(IReadOnlyList<MapConnection> list, MapConnection item)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] == item) return i;
            return -1;
        }

        // Call after anything outside this class changes the asset (undo, regenerate).
        // Bumped by every Invalidate, so views that cache what they build from the graph
        // (the blueprint) know when to rebuild.
        public int Version { get; private set; }

        public void Invalidate()
        {
            Version++;
            _analysis     = null;
            _issues       = null;
            _issuesByNode = null;
            _editedNodes  = null;
            _locks        = null;
            _tuning       = null;
            _factionMix   = null;
            _factionMixRead = false;
            ClearHighlightedPath();
        }

        private void ClearHighlightedPath()
        {
            _highlightedPath  = null;
            _highlightedLinks = null;
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
            ClearHighlightedPath();
        }

        // --- Highlights ------------------------------------------------------------

        // Nodes, and the links between them, on the shortest route from Start to the selected node.
        public bool IsOnHighlightedPath(int id)
        {
            BuildHighlightedPath();
            return _highlightedPath.Contains(id);
        }

        public bool IsOnHighlightedPath(MapConnection connection)
        {
            BuildHighlightedPath();
            return _highlightedLinks.Contains(LinkKey(connection.A, connection.B));
        }

        // With branch highlighting on and a branch node selected, everything outside that
        // branch is dimmed. Main path selections dim nothing.
        public bool IsDimmed(int id)
        {
            if (!_highlightBranch || SelectedNode == null) return false;

            int branch = Analysis.BranchOf(_selectedNodeId);
            return branch != MapGraphAnalysis.MainPathBranch && Analysis.BranchOf(id) != branch;
        }

        private void BuildHighlightedPath()
        {
            if (_highlightedPath != null) return;

            List<int> path = _highlightPath && SelectedNode != null
                ? Analysis.ShortestPath(Analysis.StartId, _selectedNodeId)
                : new List<int>();
            _highlightedPath  = new HashSet<int>(path);
            _highlightedLinks = new HashSet<(int, int)>();
            for (int i = 1; i < path.Count; i++)
                _highlightedLinks.Add(LinkKey(path[i - 1], path[i]));
        }

        private static (int, int) LinkKey(int a, int b) => a < b ? (a, b) : (b, a);

        // --- Edit state vs. generated ---------------------------------------------

        // A node differs from the generated graph's (or isn't in it). Settled when an edit is
        // made, so a dragged node shows as edited once it's dropped.
        public bool IsEdited(MapNode node)
        {
            BuildEdits();
            return _editedNodes.Contains(node.Id);
        }

        public bool HasEdits()
        {
            BuildEdits();
            return _hasEdits;
        }

        private void BuildEdits()
        {
            if (_editedNodes != null) return;

            MapGraph generated = _asset.Generated;
            _editedNodes = new HashSet<int>();
            foreach (MapNode node in Graph.Nodes)
                if (!generated.TryGetNode(node.Id, out MapNode original) || !node.SameAs(original))
                    _editedNodes.Add(node.Id);

            _hasEdits = _editedNodes.Count > 0
                     || generated.Nodes.Count != Graph.Nodes.Count
                     || generated.Connections.Count != Graph.Connections.Count;
            if (_hasEdits) return;

            foreach (MapConnection connection in Graph.Connections)
                if (!connection.SameAs(generated.GetConnection(connection.A, connection.B)))
                {
                    _hasEdits = true;
                    return;
                }
        }

        // --- Edits -----------------------------------------------------------------

        public void Regenerate(string seed)
        {
            Record("Regenerate Map");
            _asset.Regenerate(seed);
            ClearSelection();
            Changed();
        }

        public void RerollLayer(string layer)
        {
            Record($"Reroll Map {layer}");
            _asset.RerollLayer(layer);
            ClearSelection();
            Changed();
        }

        // True when there's nothing to lose, or the user agreed to lose it.
        public bool ConfirmDiscardEdits() =>
            !HasEdits() || EditorUtility.DisplayDialog(
                "Regenerate map?",
                "This replaces hand edits with a freshly generated graph. Locked nodes are kept.",
                "Regenerate", "Cancel");

        public void RevertEdits()
        {
            Record("Revert Map Edits");
            _asset.RevertEdits();
            Changed();
        }

        public void AddNode(MapNodeType type, Vector2 position)
        {
            Record("Add Map Node");
            MapNode node = Graph.AddNode(type, position);
            if (_snapToGrid) node.Position = MapGraphGrid.SnapToFreeCell(Graph, node.Id, position, CellSpacing);
            SelectNode(node.Id);
            Changed();
        }

        public void DeleteNode(int id)
        {
            Record("Delete Map Node");
            _asset.RemoveNode(id);
            if (_selectedNodeId == id) ClearSelection();
            Changed();
        }

        // Call once when a drag starts; MoveNode then only applies the offset. Nothing derived
        // from the graph refreshes until EndMove, so dragging stays cheap.
        public void BeginMove() => Record("Move Map Node");

        public void MoveNode(MapNode node, Vector2 position)
        {
            node.Position = position;
            EditorUtility.SetDirty(_asset);
        }

        // A position typed in: where it says, unsnapped.
        public void SetPosition(MapNode node, Vector2 position)
        {
            Record("Move Map Node");
            node.Position = position;
            Changed();
        }

        // Ends a drag: with snapping on, the node drops into the nearest free cell. Part of
        // the same undo step as the move.
        public void EndMove(MapNode node)
        {
            if (_snapToGrid) node.Position = MapGraphGrid.SnapToFreeCell(Graph, node.Id, node.Position, CellSpacing);
            Changed();
        }

        // Puts every node in a cell of its own, nearest first in graph order: for graphs
        // drawn by hand before snapping, or moved with it off.
        public void SnapAllToGrid()
        {
            Record("Snap Map Nodes to Grid");
            foreach (MapNode node in Graph.Nodes)
                node.Position = MapGraphGrid.SnapToFreeCell(Graph, node.Id, node.Position, CellSpacing);
            Changed();
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

        public void SetTier(MapNode node, int tier)
        {
            Record("Change Map Node Tier");
            node.Tier = tier;
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

        public void SetOneWay(MapConnection connection, bool oneWay)
        {
            Record("Change One-Way");
            connection.OneWay = oneWay;
            Changed();
        }

        public void SetDirect(MapConnection connection, bool direct)
        {
            Record("Change Direct Door");
            connection.Direct = direct;
            Changed();
        }

        public void SetSection(MapNode node, int section)
        {
            Record("Change Map Node Section");
            node.Section = section;
            Changed();
        }

        public void SetCategory(MapNode node, RoomCategory category)
        {
            Record("Change Map Node Category");
            node.Category = category;
            Changed();
        }

        // Pins the place the level builds here (null = let it pick). A place of a category
        // takes the room's category with it, so the two never disagree.
        public void SetPlace(MapNode node, IRoomPlace place)
        {
            Record("Change Map Node Place");
            node.Place = place;
            if (place != null && place.Category != RoomCategory.None) node.Category = place.Category;
            Changed();
        }

        public void SetModifiers(IEnumerable<MapRunModifier> modifiers)
        {
            Record("Change Run Modifiers");
            _asset.SetModifiers(modifiers);
            Changed();
        }

        // Centres the view on a point of the graph, keeping the zoom.
        public void Focus(Vector2 world) => Pan = -world * Zoom;

        public void SetBreachFaction(MapNode node, int faction)
        {
            Record("Change Breach Faction");
            node.BreachFaction = faction;
            Changed();
        }

        public void SetLockKind(MapConnection connection, MapLockKind kind)
        {
            Record("Change Lock Kind");
            connection.Lock = kind;
            Changed();
        }

        // index past the end adds a room.
        public void SetKeyRoom(MapConnection connection, int index, int nodeId)
        {
            Record("Change Key Room");
            connection.SetKeyRoom(index, nodeId);
            Changed();
        }

        public void RemoveKeyRoom(MapConnection connection, int nodeId)
        {
            Record("Remove Key Room");
            connection.RemoveKeyRoom(nodeId);
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
