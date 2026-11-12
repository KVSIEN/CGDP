using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using CGD.Combat;

namespace CGD.Enemies
{
    // Every faction overseer in the level: one per faction (doctrine) and team whose doctrine
    // oversees, made the first time one of its members shows up, and forgotten when the scene
    // goes — escalation is per level. Ticked by the enemies themselves a few times a second.
    internal static class Overseers
    {
        private const float TickEvery = 0.25f;

        private static readonly List<FactionOverseer> Active = new();
        private static float _lastTick = float.NegativeInfinity;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Active.Clear();
            _lastTick = float.NegativeInfinity;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private static void OnSceneUnloaded(Scene scene) => Active.Clear();

        public static IReadOnlyList<FactionOverseer> All => Active;

        public static void Tick(float time)
        {
            if (time - _lastTick < TickEvery) return;
            float deltaTime = _lastTick == float.NegativeInfinity ? TickEvery : time - _lastTick;
            _lastTick = time;

            var enemies = EnemyRoster.All;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyAI enemy = enemies[i];
                if (enemy.Doctrine.Overseer.Oversees && enemy.isActiveAndEnabled && !TryGet(enemy.Doctrine, enemy.Health.Team, out _))
                    Active.Add(new FactionOverseer(enemy.Doctrine, enemy.Health.Team, time));
            }
            foreach (FactionOverseer overseer in Active) overseer.Tick(deltaTime, time);
        }

        public static bool TryGet(FactionDoctrine doctrine, Team team, out FactionOverseer found)
        {
            foreach (FactionOverseer overseer in Active)
            {
                if (overseer.Doctrine != doctrine || overseer.Team != team) continue;
                found = overseer;
                return true;
            }
            found = null;
            return false;
        }

        public static bool IsBackingOff(FactionDoctrine doctrine, Team team) =>
            TryGet(doctrine, team, out FactionOverseer overseer) && overseer.IsBackingOff;

        // One of its members gave up a search: tell its overseer.
        public static void ReportEscape(EnemyAI enemy, float time)
        {
            if (TryGet(enemy.Doctrine, enemy.Health.Team, out FactionOverseer overseer)) overseer.OnEscape(time);
        }
    }
}
