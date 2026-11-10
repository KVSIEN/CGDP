using System;
using UnityEngine;

namespace CGD.Combat
{
    // Global broadcast of resolved hits, in the same style as Noise: hit markers, kill
    // confirmations and stats listen here without every weapon, grenade and ability
    // having to report back. Listeners must unsubscribe in OnDisable.
    public static class CombatEvents
    {
        public static event Action<DamageReport> DamageDealt;

        // Cleared at the start of every play session, so it stays empty even when Enter Play Mode
        // skips the domain reload (subscribers from the last session are gone).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => DamageDealt = null;

        internal static void Report(in DamageReport report) => DamageDealt?.Invoke(report);
    }
}
