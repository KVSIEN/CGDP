using UnityEngine;

namespace CGD.DevTools
{
    // One frame of steering for the spectator camera, already in camera terms.
    public readonly struct SpectatorMotion
    {
        // x = right, y = up, z = forward, each −1…1.
        public readonly Vector3 Move;
        // Degrees to turn this frame: x = right, y = up.
        public readonly Vector2 Look;
        // Zoom steps this frame: + in, − out.
        public readonly int     Zoom;
        // Faster held.
        public readonly bool    Fast;

        public SpectatorMotion(Vector3 move, Vector2 look, int zoom, bool fast)
        {
            Move = move;
            Look = look;
            Zoom = zoom;
            Fast = fast;
        }

        public bool IsMoving  => Move.sqrMagnitude > 0.0001f;
        public bool IsLooking => Look.sqrMagnitude > 0.000001f;
    }
}
