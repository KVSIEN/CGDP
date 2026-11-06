using System;
using UnityEngine;

namespace CGD.Enemies
{
    // The level-wide side of enemy voices: the shared BarkRules every speaker goes through,
    // and an event for anything that wants to follow what is said (captions, a "they're
    // flanking" HUD hint, debugging) without touching the enemies.
    public static class EnemyBarks
    {
        // Seconds: after any bark · before the same kind again · per speaker.
        private const float AnyGap      = 0.8f;
        private const float SameKindGap = 5f;
        private const float SpeakerGap  = 2.5f;

        internal static readonly BarkRules Rules = new(AnyGap, SameKindGap, SpeakerGap);

        // (who, what) after a bark was played.
        public static event Action<EnemyAI, BarkKind> Spoken;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Rules.Reset();
            Spoken = null;
        }

        internal static void Report(EnemyAI speaker, BarkKind kind) => Spoken?.Invoke(speaker, kind);
    }
}
