using System;
using UnityEngine;

namespace CGD.Impacts
{
    // Global broadcast of hits landing on surfaces, in the same style as Noise and
    // CombatEvents: weapons report where they hit and ImpactSpawner turns that into decals,
    // sparks and sounds, so no weapon needs to know what the effects are.
    // Listeners must unsubscribe in OnDisable.
    public static class ImpactEvents
    {
        public static event Action<Impact> Occurred;

        public static void Report(Vector3 point, Vector3 normal, Collider collider, ImpactKind kind)
        {
            if (collider != null) Occurred?.Invoke(new Impact(point, normal, collider, kind));
        }
    }
}
