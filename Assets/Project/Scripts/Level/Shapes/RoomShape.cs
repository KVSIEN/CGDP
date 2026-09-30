using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // A family of room floor plans (T, cross, ring…) built from rectangles and ellipses.
    // Each build varies the part sizes and may turn or mirror the result, so one asset
    // gives many rooms that still read as the same shape.
    [CreateAssetMenu(fileName = "RoomShape", menuName = "CGD/Level/Room Shape")]
    public class RoomShape : ScriptableObject
    {
        [Tooltip("Applied in order: Add puts floor down, Subtract cuts it away")]
        [SerializeField] private List<RoomShapePart> _parts = new();
        [Tooltip("How often this shape is picked relative to the others in the same list")]
        [SerializeField, Min(0f)] private float _weight = 1f;
        [Tooltip("Only used for rooms with this many connections (a cross suits a hub, an L a dead end)")]
        [SerializeField] private IntRange _connections = new(1, 8);
        [SerializeField] private bool _allowRotation = true;
        [SerializeField] private bool _allowMirror = true;

        public IReadOnlyList<RoomShapePart> Parts => _parts;
        public float Weight        => _weight;
        public bool  AllowRotation => _allowRotation;
        public bool  AllowMirror   => _allowMirror;

        public bool Suits(int connections) => connections >= _connections.Min && connections <= _connections.Max;
    }
}
