using System.Collections.Generic;
using CGD.Level;
using CGD.Map;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // Every change the Map Graph window makes to the asset. Each records an undo step, marks
    // the asset dirty and tells the session to drop what it derived from the graph. Reached
    // through the session's edit methods, which the window and its panels call.
    internal sealed class MapGraphEdits
    {
        private readonly MapGraphEditorSession _session;

        public MapGraphEdits(MapGraphEditorSession session) => _session = session;

        private MapGraphAsset Asset => _session.Asset;

        public void Regenerate(string seed)
        {
            Record("Regenerate Map");
            Asset.Regenerate(seed);
            _session.ClearSelection();
            Changed();
        }

        public void RerollLayer(string layer)
        {
            Record($"Reroll Map {layer}");
            Asset.RerollLayer(layer);
            _session.ClearSelection();
            Changed();
        }

        // True when there's nothing to lose, or the user agreed to lose it.
        public bool ConfirmDiscardEdits() =>
            !_session.HasEdits() || EditorUtility.DisplayDialog(
                "Regenerate map?",
                "This replaces hand edits with a freshly generated graph. Locked nodes are kept.",
                "Regenerate", "Cancel");

        public void RevertEdits()
        {
            Record("Revert Map Edits");
            Asset.RevertEdits();
            Changed();
        }

        public void AddNode(MapNodeType type, Vector2 position)
        {
            Record("Add Map Node");
            MapNode node = _session.Graph.AddNode(type, position);
            if (_session.SnapToGrid) node.Position = MapGraphGrid.SnapToFreeCell(_session.Graph, node.Id, position, _session.CellSpacing);
            _session.SelectNode(node.Id);
            Changed();
        }

        public void DeleteNode(int id)
        {
            Record("Delete Map Node");
            Asset.RemoveNode(id);
            if (_session.SelectedNodeId == id) _session.ClearSelection();
            Changed();
        }

        // Call once when a drag starts; MoveNode then only applies the offset. Nothing derived
        // from the graph refreshes until EndMove, so dragging stays cheap.
        public void BeginMove() => Record("Move Map Node");

        public void MoveNode(MapNode node, Vector2 position)
        {
            node.Position = position;
            EditorUtility.SetDirty(Asset);
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
            if (_session.SnapToGrid) node.Position = MapGraphGrid.SnapToFreeCell(_session.Graph, node.Id, node.Position, _session.CellSpacing);
            Changed();
        }

        // Puts every node in a cell of its own, nearest first in graph order: for graphs
        // drawn by hand before snapping, or moved with it off.
        public void SnapAllToGrid()
        {
            Record("Snap Map Nodes to Grid");
            foreach (MapNode node in _session.Graph.Nodes)
                node.Position = MapGraphGrid.SnapToFreeCell(_session.Graph, node.Id, node.Position, _session.CellSpacing);
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
            Asset.SetLocked(node.Id, locked);
            Changed();
        }

        public void Connect(int a, int b)
        {
            if (_session.Graph.AreConnected(a, b)) return;

            Record("Connect Map Nodes");
            _session.Graph.Connect(a, b);
            Changed();
        }

        public void Disconnect(int a, int b)
        {
            Record("Disconnect Map Nodes");
            _session.Graph.Disconnect(a, b);
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
            Asset.SetModifiers(modifiers);
            Changed();
        }

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

        private void Record(string name) => Undo.RecordObject(Asset, name);

        private void Changed()
        {
            EditorUtility.SetDirty(Asset);
            _session.Invalidate();
        }
    }
}
