using CGD.Algorithms;
using UnityEngine;

namespace CGD.Level
{
    // The guard along an elevated area's ledges, one per side the ledge faces out to (a ledge
    // between an area's tile and the floor to its right faces right). So a gallery can have a
    // half wall along its front and open ends, and a stage a railing all round.
    public readonly struct LedgeGuards
    {
        private readonly LedgeGuard _up, _right, _down, _left;

        private LedgeGuards(LedgeGuard up, LedgeGuard right, LedgeGuard down, LedgeGuard left)
        {
            _up    = up;
            _right = right;
            _down  = down;
            _left  = left;
        }

        public static LedgeGuards All(LedgeGuard guard) => new(guard, guard, guard, guard);

        public static readonly LedgeGuards None = All(LedgeGuard.None);

        // These guards with the ledges facing `side` (one of GridNeighbours.Four) guarded by `guard`.
        public LedgeGuards With(Vector2Int side, LedgeGuard guard) => GridNeighbours.IndexOf(side) switch
        {
            0 => new LedgeGuards(guard, _right, _down, _left),
            1 => new LedgeGuards(_up, guard, _down, _left),
            2 => new LedgeGuards(_up, _right, guard, _left),
            3 => new LedgeGuards(_up, _right, _down, guard),
            _ => this,
        };

        // The guard on the ledges facing out toward `side`.
        public LedgeGuard On(Vector2Int side) => GridNeighbours.IndexOf(side) switch
        {
            0 => _up,
            1 => _right,
            2 => _down,
            3 => _left,
            _ => LedgeGuard.None,
        };

        // Whether any side is guarded.
        public bool Any => _up != LedgeGuard.None || _right != LedgeGuard.None || _down != LedgeGuard.None || _left != LedgeGuard.None;
    }
}
