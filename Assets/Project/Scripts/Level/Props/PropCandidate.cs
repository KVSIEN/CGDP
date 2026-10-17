using UnityEngine;

namespace CGD.Level
{
    // A place a prop could go: the tiles it would cover and the quarter turn it faces
    // (0 = +Z, the tile's +y; each step turns toward +X).
    internal readonly struct PropCandidate
    {
        private static readonly Vector2Int[] Cardinals = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        public PropCandidate(RectInt rect, int rotation)
        {
            Rect     = rect;
            Rotation = rotation;
        }

        public RectInt    Rect     { get; }
        public int        Rotation { get; }
        public Vector2Int Forward  => Cardinals[Rotation];
    }
}
