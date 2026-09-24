using System;

namespace CGD.Combat
{
    // Global broadcast of resolved hits, in the same style as Noise: hit markers, kill
    // confirmations and stats listen here without every weapon, grenade and ability
    // having to report back. Listeners must unsubscribe in OnDisable.
    public static class CombatEvents
    {
        public static event Action<DamageReport> DamageDealt;

        internal static void Report(in DamageReport report) => DamageDealt?.Invoke(report);
    }
}
