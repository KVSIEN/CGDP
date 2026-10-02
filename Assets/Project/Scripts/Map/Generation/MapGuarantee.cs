using System;
using CGD.Core;
using UnityEngine;

namespace CGD.Map
{
    // A room type the content asks for in a specific place — an early Treasure, a reward
    // behind every gate, a Shop before the Boss. Guaranteed rooms count toward their type's
    // rule and never take it past its maximum.
    [Serializable]
    public class MapGuarantee
    {
        [Tooltip("Start, Boss and Exit can't be guaranteed — the layout places them")]
        [SerializeField] private MapNodeType _type = MapNodeType.Treasure;
        [SerializeField] private MapGuaranteeSpot _spot = MapGuaranteeSpot.WithinDepth;
        [Tooltip("Within Depth: how many rooms")]
        [SerializeField, Min(1)] private int _count = 1;
        [Tooltip("Within Depth: rooms from Start")]
        [SerializeField] private IntRange _depth = new(2, 3);

        // Used by the Inspector when a guarantee is added to the list.
        public MapGuarantee() { }

        public MapGuarantee(MapNodeType type, MapGuaranteeSpot spot, int count = 1, IntRange depth = default)
        {
            _type  = type;
            _spot  = spot;
            _count = Mathf.Max(1, count);
            _depth = spot == MapGuaranteeSpot.WithinDepth && depth.Max == 0 ? new IntRange(2, 3) : depth;
        }

        public MapNodeType      Type  => _type;
        public MapGuaranteeSpot Spot  => _spot;
        public int              Count => Mathf.Max(1, _count);
        public IntRange         Depth => _depth;
        public bool             IsValid => !_type.IsStructural();

        public string Describe() => _spot switch
        {
            MapGuaranteeSpot.BeforeBoss      => $"{_type} before the Boss",
            MapGuaranteeSpot.BehindEveryGate => $"{_type} behind every gate",
            _                                => $"{Count}× {_type} {_depth.Min}–{_depth.Max} rooms deep",
        };
    }
}
