using System;

namespace CGD.Quests
{
    // Global broadcast of things objectives count, in the same style as Noise: the
    // enemy that dies or the pickup that's collected doesn't need to know a quest
    // system exists. Listeners must unsubscribe in OnDisable.
    public static class QuestEvents
    {
        public static event Action<QuestEvent> Reported;

        public static void Report(ObjectiveKind kind, UnityEngine.Object target, int amount = 1)
        {
            if (target != null && amount > 0)
                Reported?.Invoke(new QuestEvent(kind, target, amount));
        }
    }
}
