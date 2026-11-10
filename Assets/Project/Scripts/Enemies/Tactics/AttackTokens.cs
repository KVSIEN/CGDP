using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using CGD.Combat;
using CGD.Core;

namespace CGD.Enemies
{
    // How many enemies may be attacking one target at once, per kind of attack. An enemy
    // without a token waits its turn (circles, holds fire) so fights stay readable and fair
    // instead of everyone piling in. Holders that died or were switched off drop out.
    internal static class AttackTokens
    {
        private static readonly Dictionary<HealthManager, List<EnemyAI>> Melee  = new();
        private static readonly Dictionary<HealthManager, List<EnemyAI>> Ranged = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Melee.Clear();
            Ranged.Clear();
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        // Targets of the level just left (enemies fight each other, so not only the player).
        private static void OnSceneUnloaded(Scene scene)
        {
            DestroyedKeys.Remove(Melee);
            DestroyedKeys.Remove(Ranged);
        }

        // limit counts the asker too: 2 means "me and at most one other".
        public static bool TryAcquire(AttackTokenKind kind, HealthManager target, EnemyAI who, int limit)
        {
            if (target == null) return false;

            var table = Table(kind);
            if (!table.TryGetValue(target, out List<EnemyAI> holders)) table[target] = holders = new List<EnemyAI>();

            holders.RemoveAll(h => h == null || !h.isActiveAndEnabled || h.State == EnemyAI.AiState.Dead);
            if (holders.Contains(who)) return true;
            if (holders.Count >= Mathf.Max(1, limit)) return false;

            holders.Add(who);
            return true;
        }

        public static void Release(AttackTokenKind kind, EnemyAI who)
        {
            foreach (List<EnemyAI> holders in Table(kind).Values) holders.Remove(who);
        }

        public static void ReleaseAll(EnemyAI who)
        {
            Release(AttackTokenKind.Melee, who);
            Release(AttackTokenKind.Ranged, who);
        }

        private static Dictionary<HealthManager, List<EnemyAI>> Table(AttackTokenKind kind) =>
            kind == AttackTokenKind.Melee ? Melee : Ranged;
    }
}
