using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Enemies
{
    // Bodies of fallen enemies, until they're reused or removed. Allies only react to one they
    // actually see (or hear go down close by): a body hidden out of sight stays undiscovered.
    public static class FallenAllies
    {
        private static readonly List<FallenAlly> Bodies = new();

        // Raised as an enemy dies, so allies close enough to hear it can react.
        public static event Action<FallenAlly> Fell;

        public static IReadOnlyList<FallenAlly> All => Bodies;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Bodies.Clear();
            Fell = null;
        }

        internal static void Add(FallenAlly fallen)
        {
            Remove(fallen.Body);
            Bodies.Add(fallen);
            Fell?.Invoke(fallen);
        }

        internal static void Remove(EnemyAI body) => Bodies.RemoveAll(b => b.Body == body || b.Body == null);
    }
}
