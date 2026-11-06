using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Enemies
{
    // The director's budget of bold moves (flanks, grenades, charges, suppressing fire) for
    // the whole level: only EnemyPacing.Current.BoldMoves enemies may be making one at once.
    // That turns the peak into a coordinated push and keeps a calm from filling with grenades.
    // A permit is held until the move ends; a shrinking budget only stops new ones.
    internal static class BoldMovePermits
    {
        private static readonly List<EnemyAI> Holders = new();
        private static readonly Predicate<EnemyAI> IsGone =
            holder => holder == null || !holder.isActiveAndEnabled || holder.State == EnemyAI.AiState.Dead;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Holders.Clear();

        public static int InUse
        {
            get
            {
                Holders.RemoveAll(IsGone);
                return Holders.Count;
            }
        }

        public static bool Holds(EnemyAI who) => Holders.Contains(who);

        public static bool IsAvailableTo(EnemyAI who) => Holds(who) || InUse < EnemyPacing.Current.BoldMoves;

        public static bool TryAcquire(EnemyAI who)
        {
            if (!IsAvailableTo(who)) return false;
            if (!Holds(who)) Holders.Add(who);
            return true;
        }

        public static void Release(EnemyAI who) => Holders.Remove(who);
    }
}
