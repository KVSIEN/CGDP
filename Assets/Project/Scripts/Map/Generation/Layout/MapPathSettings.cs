using System;
using CGD.Core;
using UnityEngine;

namespace CGD.Map
{
    // The route from Start to the Boss: how long it is and how much it turns.
    [Serializable]
    public class MapPathSettings
    {
        [Tooltip("East: left to right, the Boss always east of Start. Any: sets off in a random direction, each step further from Start, so Start can sit in the middle")]
        [SerializeField] private MapPathDirection _direction = MapPathDirection.East;
        [Tooltip("Rooms between Start and Boss. Raised to Min Boss Depth − 1 when shorter")]
        [SerializeField] private IntRange _length = new(6, 9);
        [Tooltip("Chance to turn at each room. 0 = a straight line, 1 = winds constantly. Never turns back toward Start")]
        [SerializeField, Range(0f, 1f)] private float _winding;

        public MapPathDirection Direction => _direction;
        public IntRange Length  => _length;
        public float    Winding => _winding;
    }
}
