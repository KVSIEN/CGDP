using System;
using CGD.Core;
using UnityEngine;

namespace CGD.Map
{
    // The route from Start to the Boss: how long it is and how much it turns.
    [Serializable]
    public class MapPathSettings
    {
        [Tooltip("Rooms between Start and Boss. Raised to Min Boss Depth − 1 when shorter")]
        [SerializeField] private IntRange _length = new(6, 9);
        [Tooltip("Chance to turn at each room. 0 = a straight line, 1 = winds constantly. Never turns back toward Start")]
        [SerializeField, Range(0f, 1f)] private float _winding;

        public IntRange Length  => _length;
        public float    Winding => _winding;
    }
}
