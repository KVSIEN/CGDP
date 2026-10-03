using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Map
{
    // An undirected link between two nodes. A Locked link also says what opens it (its
    // lock kind) and which rooms hold the parts: the key, or each terminal. A one-way link
    // starts barred and only opens from its deeper end — a return route back toward Start
    // (after a death, the way home) rather than a way ahead.
    [Serializable]
    public class MapConnection
    {
        [SerializeField] private int            _a;
        [SerializeField] private int            _b;
        [SerializeField] private ConnectionType _type;
        [SerializeField] private MapLockKind    _lock;
        [Tooltip("Rooms holding what opens a Locked connection: the key, or one terminal each")]
        [SerializeField] private List<int>      _keyNodeIds = new();
        [Tooltip("Opens only from the end further from Start; a normal passage once opened")]
        [SerializeField] private bool           _oneWay;

        public MapConnection(int a, int b, ConnectionType type)
        {
            _a    = a;
            _b    = b;
            _type = type;
        }

        public int A => _a;
        public int B => _b;

        public ConnectionType Type
        {
            get => _type;
            set => _type = value;
        }

        // Lock kind and key rooms only matter while Type is Locked; they are kept through
        // other types so switching a link away from Locked and back doesn't lose them.
        public MapLockKind Lock
        {
            get => _lock;
            set => _lock = value;
        }

        public IReadOnlyList<int> KeyNodeIds => _keyNodeIds;
        public bool HasKey => _keyNodeIds.Count > 0;

        // How many key rooms the lock kind uses: a keycard lies in one room only.
        public bool UsesSingleKey => _lock == MapLockKind.Keycard;

        public bool HoldsKey(int nodeId) => _keyNodeIds.Contains(nodeId);

        public void AddKeyRoom(int nodeId)
        {
            if (!_keyNodeIds.Contains(nodeId)) _keyNodeIds.Add(nodeId);
        }

        public bool RemoveKeyRoom(int nodeId) => _keyNodeIds.Remove(nodeId);

        public void SetKeyRoom(int index, int nodeId)
        {
            if (index < 0 || index >= _keyNodeIds.Count) AddKeyRoom(nodeId);
            else if (!_keyNodeIds.Contains(nodeId)) _keyNodeIds[index] = nodeId;
        }

        public void ClearKeyRooms() => _keyNodeIds.Clear();

        public bool OneWay
        {
            get => _oneWay;
            set => _oneWay = value;
        }

        // Locked and Secret links close off what lies behind them until opened or found.
        public bool IsGate => _type == ConnectionType.Locked || _type == ConnectionType.Secret;

        public bool Connects(int nodeId) => _a == nodeId || _b == nodeId;

        public bool Links(int a, int b) => (_a == a && _b == b) || (_a == b && _b == a);

        public int Other(int nodeId) => nodeId == _a ? _b : _a;

        public MapConnection Clone()
        {
            var copy = (MapConnection)MemberwiseClone();
            copy._keyNodeIds = new List<int>(_keyNodeIds);
            return copy;
        }
    }
}
