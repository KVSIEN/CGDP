using System;
using UnityEngine;

namespace CGD.Map
{
    // An undirected link between two nodes. A Locked link also names the room holding
    // its key. A one-way link starts barred and only opens from its deeper end — a return
    // route back toward Start (after a death, the way home) rather than a way ahead.
    [Serializable]
    public class MapConnection
    {
        public const int NoKey = -1;

        [SerializeField] private int            _a;
        [SerializeField] private int            _b;
        [SerializeField] private ConnectionType _type;
        [Tooltip("Node holding the key for a Locked connection, or -1")]
        [SerializeField] private int            _keyNodeId = NoKey;
        [Tooltip("Opens only from the end further from Start; a normal passage once opened")]
        [SerializeField] private bool           _oneWay;

        public MapConnection(int a, int b, ConnectionType type)
        {
            _a         = a;
            _b         = b;
            _type      = type;
            _keyNodeId = NoKey;
        }

        public int A => _a;
        public int B => _b;

        public ConnectionType Type
        {
            get => _type;
            set => _type = value;
        }

        // Only meaningful while Type is Locked; kept through other types so switching a
        // link away from Locked and back doesn't lose its key.
        public int KeyNodeId
        {
            get => _keyNodeId;
            set => _keyNodeId = value;
        }

        public bool HasKey => _keyNodeId != NoKey;

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

        public MapConnection Clone() => (MapConnection)MemberwiseClone();
    }
}
