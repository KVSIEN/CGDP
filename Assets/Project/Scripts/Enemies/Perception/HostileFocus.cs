using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using CGD.Combat;
using CGD.Core;

namespace CGD.Enemies
{
    // Whether a hostile is busy fighting someone else — its attention is elsewhere (the
    // Distracted target priority). Only enemies' attention can be read, so the player never
    // counts as distracted. Which hostiles are enemies is looked up once and cached.
    internal static class HostileFocus
    {
        private static readonly Dictionary<HealthManager, EnemyAI> Brains = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Brains.Clear();
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private static void OnSceneUnloaded(Scene scene) => DestroyedKeys.Remove(Brains);

        // An enemy engaged with a target other than this one.
        public static bool IsFightingSomeoneElse(HealthManager hostile, HealthManager self)
        {
            if (!Brains.TryGetValue(hostile, out EnemyAI brain))
            {
                hostile.TryGetComponent(out brain);
                Brains[hostile] = brain;
            }
            if (brain == null || brain.Perception == null) return false;

            EnemyPerception other = brain.Perception;
            return other.IsTargetDetected && other.Target != null && other.Target != self;
        }
    }
}
