using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;

namespace CGD.Enemies
{
    // Every active enemy, for the few questions that are about kin rather than about the target:
    // who else is fighting this one (a union focuses fire) and who is near (a herd stays together).
    // Kin are the same team plus the teams in the asker's Pacts. Asked a few times a second at
    // most, so it just walks the list.
    internal static class EnemyRoster
    {
        private static readonly List<EnemyAI> Members = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Members.Clear();

        public static void Add(EnemyAI enemy)
        {
            if (!Members.Contains(enemy)) Members.Add(enemy);
        }

        public static void Remove(EnemyAI enemy) => Members.Remove(enemy);

        public static IReadOnlyList<EnemyAI> All => Members;

        public static bool IsKin(EnemyAI asker, EnemyAI other)
        {
            if (other == asker || other.State == EnemyAI.AiState.Dead) return false;
            return IsKinTeam(asker.Data, asker.Health.Team, other.Health.Team);
        }

        public static bool IsKinTeam(EnemyData data, Team own, Team other)
        {
            if (other == own) return true;
            if (data.Pacts == null) return false;
            foreach (Team pact in data.Pacts)
                if (pact == other) return true;
            return false;
        }

        // Another of its kin is fighting this target right now.
        public static bool IsFocusedByKin(EnemyAI asker, HealthManager candidate)
        {
            foreach (EnemyAI other in Members)
                if (IsKin(asker, other) && other.Perception.IsTargetDetected && other.Perception.Target == candidate) return true;
            return false;
        }

        // Its kin within radius (itself not counted).
        public static int KinWithin(EnemyAI asker, float radius)
        {
            int count = 0;
            foreach (EnemyAI other in Members)
                if (IsKin(asker, other) && (other.transform.position - asker.transform.position).sqrMagnitude <= radius * radius) count++;
            return count;
        }

        public static bool TryNearestKin(EnemyAI asker, out EnemyAI nearest)
        {
            nearest = null;
            float best = float.MaxValue;
            foreach (EnemyAI other in Members)
            {
                if (!IsKin(asker, other)) continue;
                float distance = (other.transform.position - asker.transform.position).sqrMagnitude;
                if (distance >= best) continue;
                best    = distance;
                nearest = other;
            }
            return nearest != null;
        }
    }
}
