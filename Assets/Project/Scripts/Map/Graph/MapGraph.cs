using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Map
{
    // The pure data description of a map: which experiences exist and how they link.
    // No room geometry — a later stage turns this into rooms.
    //
    // Connections are the single source of truth for adjacency; nodes don't keep their
    // own neighbour lists, so the two can never disagree after an edit.
    [Serializable]
    public class MapGraph
    {
        [SerializeField] private List<MapNode>       _nodes       = new();
        [SerializeField] private List<MapConnection> _connections = new();
        [SerializeField] private int                 _nextId;

        public IReadOnlyList<MapNode>       Nodes       => _nodes;
        public IReadOnlyList<MapConnection> Connections => _connections;

        public MapNode AddNode(MapNodeType type, Vector2 position)
        {
            var node = new MapNode(_nextId++, type, position);
            _nodes.Add(node);
            return node;
        }

        // Also removes every connection touching the node, and any key or terminal it held.
        public bool RemoveNode(int id)
        {
            int index = _nodes.FindIndex(n => n.Id == id);
            if (index < 0) return false;

            _nodes.RemoveAt(index);
            _connections.RemoveAll(c => c.Connects(id));
            foreach (MapConnection connection in _connections)
                connection.RemoveKeyRoom(id);
            return true;
        }

        // Plain loops rather than List.Find: these run constantly (generation, level layout,
        // every repaint of the Map Graph window) and a lambda capturing the id allocates.
        public bool TryGetNode(int id, out MapNode node)
        {
            foreach (MapNode candidate in _nodes)
                if (candidate.Id == id)
                {
                    node = candidate;
                    return true;
                }
            node = null;
            return false;
        }

        public MapNode FindFirst(MapNodeType type)
        {
            foreach (MapNode node in _nodes)
                if (node.Type == type) return node;
            return null;
        }

        public int CountOf(MapNodeType type)
        {
            int count = 0;
            foreach (MapNode node in _nodes)
                if (node.Type == type) count++;
            return count;
        }

        // Returns false for self-links, unknown nodes and duplicates.
        public bool Connect(int a, int b, ConnectionType type = ConnectionType.Normal)
        {
            if (a == b || AreConnected(a, b)) return false;
            if (!TryGetNode(a, out _) || !TryGetNode(b, out _)) return false;

            _connections.Add(new MapConnection(a, b, type));
            return true;
        }

        public bool Disconnect(int a, int b) => _connections.RemoveAll(c => c.Links(a, b)) > 0;

        public MapConnection GetConnection(int a, int b)
        {
            foreach (MapConnection connection in _connections)
                if (connection.Links(a, b)) return connection;
            return null;
        }

        public bool AreConnected(int a, int b) => GetConnection(a, b) != null;

        public void GetNeighbors(int id, List<int> results)
        {
            results.Clear();
            foreach (MapConnection connection in _connections)
                if (connection.Connects(id))
                    results.Add(connection.Other(id));
        }

        public int Degree(int id)
        {
            int degree = 0;
            foreach (MapConnection connection in _connections)
                if (connection.Connects(id)) degree++;
            return degree;
        }

        public MapGraph Clone()
        {
            var copy = new MapGraph { _nextId = _nextId };
            foreach (MapNode node in _nodes)
                copy._nodes.Add(node.Clone());
            foreach (MapConnection connection in _connections)
                copy._connections.Add(connection.Clone());
            return copy;
        }
    }
}
