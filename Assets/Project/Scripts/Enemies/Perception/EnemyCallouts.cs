using System;
using UnityEngine;

namespace CGD.Enemies
{
    // Enemies calling out to each other. Like Noise, a broadcast: each listener decides
    // whether it could have heard the shout from where it stands.
    public static class EnemyCallouts
    {
        public static event Action<EnemyCallout> Raised;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Raised = null;

        public static void Raise(in EnemyCallout callout)
        {
            if (callout.Radius > 0f) Raised?.Invoke(callout);
        }
    }
}
