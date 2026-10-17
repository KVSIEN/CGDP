using System;
using System.Collections.Generic;
using CGD.Level;
using CGD.Map;
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
        [NonSerialized] private MapIssueIndex       _issueIndex;
        [NonSerialized] private MapEditTracker      _editTracker;
        [NonSerialized] private MapPathHighlight    _pathHighlight;
        [NonSerialized] private MapGraphEdits       _edits;
        [NonSerialized] private List<MapConnection> _locks;
        [NonSerialized] private MapRunTuning        _tuning;
        [NonSerialized] private MapFactionMix       _factionMix;
        [NonSerialized] private bool                _factionMixRead;
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

        private MapIssueIndex IssueIndex => _issueIndex ??= new MapIssueIndex(this);

        public IReadOnlyList<MapGraphIssue> Issues => IssueIndex.All;

        // The issues naming this room, or a connection it's an end of.
        public IReadOnlyList<MapGraphIssue> IssuesAbout(int nodeId) => IssueIndex.About(nodeId);

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
            _issueIndex   = null;
            _editTracker  = null;
            _locks        = null;
            _tuning       = null;
            _factionMix   = null;
            _factionMixRead = false;
            ClearHighlightedPath();
        }

        private void ClearHighlightedPath() => _pathHighlight = null;

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

        private MapPathHighlight PathHighlight => _pathHighlight ??= new MapPathHighlight(this);

        // Nodes, and the links between them, on the shortest route from Start to the selected node.
        public bool IsOnHighlightedPath(int id) => PathHighlight.Contains(id);

        public bool IsOnHighlightedPath(MapConnection connection) => PathHighlight.Contains(connection);

        // With branch highlighting on and a branch node selected, everything outside that
        // branch is dimmed. Main path selections dim nothing.
        public bool IsDimmed(int id)
        {
            if (!_highlightBranch || SelectedNode == null) return false;

            int branch = Analysis.BranchOf(_selectedNodeId);
            return branch != MapGraphAnalysis.MainPathBranch && Analysis.BranchOf(id) != branch;
        }

        // --- Edit state vs. generated ---------------------------------------------

        private MapEditTracker EditTracker => _editTracker ??= new MapEditTracker(this);

        // A node differs from the generated graph's (or isn't in it).
        public bool IsEdited(MapNode node) => EditTracker.IsEdited(node);

        public bool HasEdits() => EditTracker.HasEdits();

        // --- Edits -----------------------------------------------------------------

        private MapGraphEdits Edits => _edits ??= new MapGraphEdits(this);

        public void Regenerate(string seed) => Edits.Regenerate(seed);
        public void RerollLayer(string layer) => Edits.RerollLayer(layer);
        public bool ConfirmDiscardEdits() => Edits.ConfirmDiscardEdits();
        public void RevertEdits() => Edits.RevertEdits();
        public void AddNode(MapNodeType type, Vector2 position) => Edits.AddNode(type, position);
        public void DeleteNode(int id) => Edits.DeleteNode(id);
        public void BeginMove() => Edits.BeginMove();
        public void MoveNode(MapNode node, Vector2 position) => Edits.MoveNode(node, position);
        public void SetPosition(MapNode node, Vector2 position) => Edits.SetPosition(node, position);
        public void EndMove(MapNode node) => Edits.EndMove(node);
        public void SnapAllToGrid() => Edits.SnapAllToGrid();
        public void SetType(MapNode node, MapNodeType type) => Edits.SetType(node, type);
        public void SetIntensity(MapNode node, float intensity) => Edits.SetIntensity(node, intensity);
        public void SetTier(MapNode node, int tier) => Edits.SetTier(node, tier);
        public void SetFaction(MapNode node, int faction, float influence) => Edits.SetFaction(node, faction, influence);
        public void SetLocked(MapNode node, bool locked) => Edits.SetLocked(node, locked);
        public void Connect(int a, int b) => Edits.Connect(a, b);
        public void Disconnect(int a, int b) => Edits.Disconnect(a, b);
        public void SetConnectionType(MapConnection connection, ConnectionType type) => Edits.SetConnectionType(connection, type);
        public void SetOneWay(MapConnection connection, bool oneWay) => Edits.SetOneWay(connection, oneWay);
        public void SetDirect(MapConnection connection, bool direct) => Edits.SetDirect(connection, direct);
        public void SetSection(MapNode node, int section) => Edits.SetSection(node, section);
        public void SetCategory(MapNode node, RoomCategory category) => Edits.SetCategory(node, category);
        public void SetPlace(MapNode node, IRoomPlace place) => Edits.SetPlace(node, place);
        public void SetModifiers(IEnumerable<MapRunModifier> modifiers) => Edits.SetModifiers(modifiers);
        public void SetBreachFaction(MapNode node, int faction) => Edits.SetBreachFaction(node, faction);
        public void SetLockKind(MapConnection connection, MapLockKind kind) => Edits.SetLockKind(connection, kind);
        public void SetKeyRoom(MapConnection connection, int index, int nodeId) => Edits.SetKeyRoom(connection, index, nodeId);
        public void RemoveKeyRoom(MapConnection connection, int nodeId) => Edits.RemoveKeyRoom(connection, nodeId);

        // Centres the view on a point of the graph, keeping the zoom.
        public void Focus(Vector2 world) => Pan = -world * Zoom;
    }
}
