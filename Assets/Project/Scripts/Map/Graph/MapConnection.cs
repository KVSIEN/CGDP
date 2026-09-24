using System;
using UnityEngine;

namespace CGD.Map
{
    // An undirected link between two nodes.
    [Serializable]
    public class MapConnection
    {
        [SerializeField] private int            _a;
        [SerializeField] private int            _b;
        [SerializeField] private ConnectionType _type;

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

        public bool Connects(int nodeId) => _a == nodeId || _b == nodeId;

        public bool Links(int a, int b) => (_a == a && _b == b) || (_a == b && _b == a);

        public int Other(int nodeId) => nodeId == _a ? _b : _a;

        public MapConnection Clone() => (MapConnection)MemberwiseClone();
    }
}
