using System;
using UnityEngine;

namespace CGD.Combat
{
    // Global broadcast for sounds AI can react to (gunfire, swings, explosions).
    // A static event keeps emitters and listeners decoupled; listeners must
    // unsubscribe in OnDisable.
    public static class Noise
    {
        public static event Action<NoiseEvent> Emitted;

        public static void Emit(Vector3 position, float radius, DamageSource source)
        {
            if (radius > 0f) Emitted?.Invoke(new NoiseEvent(position, radius, source));
        }
    }
}
