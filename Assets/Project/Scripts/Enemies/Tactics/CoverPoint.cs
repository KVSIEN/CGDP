using UnityEngine;

namespace CGD.Enemies
{
    // A spot on the NavMesh right against a wall or obstacle. Facing points from the spot
    // toward the obstacle: a threat roughly that way can't see whoever stands here. Low cover
    // (waist-high) still lets its user shoot over it; high cover hides it completely.
    public readonly struct CoverPoint
    {
        public readonly Vector3 Position;
        public readonly Vector3 Facing;
        public readonly bool    IsHigh;

        public CoverPoint(Vector3 position, Vector3 facing, bool isHigh)
        {
            Position = position;
            Facing   = facing;
            IsHigh   = isHigh;
        }

        // Whether the obstacle stands between this spot and a threat at that position.
        public bool Faces(Vector3 threat, float minimumDot)
        {
            Vector3 toThreat = threat - Position;
            toThreat.y = 0f;
            return toThreat.sqrMagnitude > 0.01f && Vector3.Dot(Facing, toThreat.normalized) >= minimumDot;
        }
    }
}
