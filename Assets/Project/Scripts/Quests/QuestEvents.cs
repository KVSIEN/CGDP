using System;
using UnityEngine;

namespace CGD.Quests
{
    // Global broadcast of things objectives count, in the same style as Noise: the
    // enemy that dies or the pickup that's collected doesn't need to know a quest
    // system exists. Listeners must unsubscribe in OnDisable.
    public static class QuestEvents
    {
        public static event Action<QuestEvent> Reported;

        // Cleared at the start of every play session, so it stays empty even when Enter Play Mode
        // skips the domain reload (subscribers from the last session are gone).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Reported = null;

        public static void Report(ObjectiveKind kind, UnityEngine.Object target, int amount = 1)
        {
            if (target != null && amount > 0)
                Reported?.Invoke(new QuestEvent(kind, target, amount));
        }
    }
}
