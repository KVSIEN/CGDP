using System;
using UnityEngine;

namespace CGD.CameraEffects
{
    // Global broadcast for world events cameras should feel (explosions, heavy landings,
    // a boss slam), in the same style as Noise: the grenade doesn't need to find the
    // camera. Listeners must unsubscribe in OnDisable.
    public static class CameraImpulses
    {
        // (position, radius, trauma at the centre)
        public static event Action<Vector3, float, float> Emitted;

        public static void Emit(Vector3 position, float radius, float trauma)
        {
            if (radius > 0f && trauma > 0f) Emitted?.Invoke(position, radius, trauma);
        }
    }
}
