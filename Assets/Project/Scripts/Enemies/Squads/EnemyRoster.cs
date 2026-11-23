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

        // A living enemy on exactly the asker's own team (pacts don't count: this is for who it
        // supports, guards and shelters behind, not whom it won't attack).
        public static bool IsTeammate(EnemyAI asker, EnemyAI other) =>
            other != asker && other.isActiveAndEnabled && other.Health != null && !other.Health.IsDead &&
            other.Health.Team == asker.Health.Team;

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

        // The nearest of its kin within the radius that is worn down to `healthShare` or below
        // (plating for one that lives on it) and, if asked, falling back: one whose retreat needs covering.
        public static bool TryKinInTrouble(EnemyAI asker, float radius, float healthShare, bool onlyRetreating, out EnemyAI kin)
        {
            kin = null;
            float bestSq = radius * radius;
            foreach (EnemyAI other in Members)
            {
                if (!IsKin(asker, other) || other.Health == null) continue;
                if (onlyRetreating && other.State != EnemyAI.AiState.Retreat) continue;

                EnemyHealth health = other.Health;
                if (health.MaxVital <= 0f || health.Vital / health.MaxVital > healthShare) continue;

                float sq = (other.transform.position - asker.transform.position).sqrMagnitude;
                if (sq > bestSq) continue;
                bestSq = sq;
                kin    = other;
            }
            return kin != null;
        }

        // The kin within the radius taking the most fire right now (its recent losses as a share of its
        // maximum), if any has lost at least `minShare`: whoever most needs a barrier over it.
        public static bool TryKinUnderFire(EnemyAI asker, float radius, float minShare, out EnemyAI kin)
        {
            kin = null;
            float radiusSq = radius * radius;
            float worst    = minShare;
            foreach (EnemyAI other in Members)
            {
                if (!IsKin(asker, other) || other.Health == null) continue;
                if ((other.transform.position - asker.transform.position).sqrMagnitude > radiusSq) continue;

                float share = other.Health.RecentDamageShare;
                if (share < worst) continue;
                worst = share;
                kin   = other;
            }
            return kin != null;
        }

        // The kin within the radius of the asker that stands nearest `point` (the threat: the front of the line).
        public static bool TryKinNearest(EnemyAI asker, float radius, Vector3 point, out EnemyAI kin)
        {
            kin = null;
            float radiusSq = radius * radius;
            float nearest  = float.MaxValue;
            foreach (EnemyAI other in Members)
            {
                if (!IsKin(asker, other)) continue;
                if ((other.transform.position - asker.transform.position).sqrMagnitude > radiusSq) continue;

                Vector3 offset = other.transform.position - point;
                offset.y = 0f;
                if (offset.sqrMagnitude >= nearest) continue;
                nearest = offset.sqrMagnitude;
                kin     = other;
            }
            return kin != null;
        }

        // Where the nearest of its kin within the radius that is fighting right now has its target:
        // what a squadmate sees that the asker may not. False when no kin nearby has a target.
        public static bool TryKinTarget(EnemyAI asker, float radius, out Vector3 position)
        {
            position = default;
            bool found = false;
            float bestSq = radius * radius;
            foreach (EnemyAI other in Members)
            {
                if (!IsKin(asker, other) || !other.Perception.IsSensing) continue;

                float sq = (other.transform.position - asker.transform.position).sqrMagnitude;
                if (sq > bestSq) continue;
                bestSq   = sq;
                position = other.Perception.TargetPosition;
                found    = true;
            }
            return found;
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
