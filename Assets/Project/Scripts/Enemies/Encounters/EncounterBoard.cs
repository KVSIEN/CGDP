using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CGD.Enemies
{
    // Every encounter in play, formed on their own: each round, every enemy fighting a target
    // (engaged, or hunting one it just lost) whose faction coordinates — or who is in an
    // authored squad — joins the encounter of its faction, team and target. Encounters with
    // nobody left end. Rounds are a couple a second; enemies tick it, and the first to do so
    // each round runs it.
    internal static class EncounterBoard
    {
        private const float RoundEvery = 0.5f;

        private static readonly List<Encounter> Active = new();
        private static float _nextRound;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Active.Clear();
            _nextRound = 0f;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            foreach (Encounter encounter in Active) encounter.Dissolve();
            Active.Clear();
        }

        public static IReadOnlyList<Encounter> All => Active;

        public static void Tick(float time)
        {
            if (time < _nextRound) return;
            _nextRound = time + RoundEvery;

            foreach (Encounter encounter in Active) encounter.BeginRound();

            var enemies = EnemyRoster.All;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyAI enemy = enemies[i];
                if (!Coordinates(enemy) || !IsFighting(enemy)) continue;
                Find(enemy).Mark(enemy, time);
            }

            for (int i = Active.Count - 1; i >= 0; i--)
            {
                Encounter encounter = Active[i];
                encounter.EndRound(time, Overseers.IsBackingOff(encounter.Doctrine, encounter.Team));
                if (encounter.IsEmpty) Active.RemoveAt(i);
            }
        }

        private static bool Coordinates(EnemyAI enemy) => enemy.Squad != null || enemy.Doctrine.Encounter.Coordinates;

        private static bool IsFighting(EnemyAI enemy) =>
            enemy.isActiveAndEnabled && enemy.State != EnemyAI.AiState.Dead &&
            enemy.Perception.Target != null &&
            enemy.Perception.Level is AwarenessLevel.Engaged or AwarenessLevel.Searching;

        private static Encounter Find(EnemyAI enemy)
        {
            FactionDoctrine doctrine = enemy.Doctrine;
            var team   = enemy.Health.Team;
            var target = enemy.Perception.Target;
            foreach (Encounter encounter in Active)
                if (encounter.Matches(doctrine, team, target)) return encounter;

            var created = new Encounter(doctrine, team, target);
            Active.Add(created);
            return created;
        }
    }
}
