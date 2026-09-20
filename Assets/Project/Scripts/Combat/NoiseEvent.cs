using UnityEngine;

namespace CGD.Combat
{
    public readonly struct NoiseEvent
    {
        public readonly Vector3 Position;
        public readonly float Radius; // how far away it can be heard
        public readonly DamageSource Source;

        public NoiseEvent(Vector3 position, float radius, DamageSource source)
        {
            Position = position;
            Radius   = radius;
            Source   = source;
        }
    }
}
