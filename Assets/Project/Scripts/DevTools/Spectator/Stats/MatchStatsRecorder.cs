using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Abilities;
using CGD.Combat;
using CGD.Enemies;

namespace CGD.DevTools
{
    // Feeds MatchStats from the game while spectating: every hit reported by CombatEvents
    // (credited to the agent that owns the damage source, so projectiles and grenades count
    // for their thrower) and every ability an agent casts. Dispose to stop listening.
    internal sealed class MatchStatsRecorder : IDisposable
    {
        private readonly MatchStats _stats;
        private readonly EnemyAI[]  _agents;
        private readonly Action<Ability>[] _abilityHandlers;
        private readonly Dictionary<GameObject, int>    _byOwner  = new();
        private readonly Dictionary<HealthManager, int> _byHealth = new();

        public MatchStatsRecorder(EnemyAI[] agents, MatchStats stats)
        {
            _agents = agents;
            _stats  = stats;
            _abilityHandlers = new Action<Ability>[agents.Length];

            for (int i = 0; i < agents.Length; i++)
            {
                EnemyAI agent = agents[i];
                if (agent == null) continue;
                int number = i;
                _byOwner[agent.gameObject] = i;
                if (agent.TryGetComponent(out EnemyHealth health)) _byHealth[health] = i;
                _abilityHandlers[i] = _ => _stats.AbilityUsed(number);
                agent.AbilityUsed += _abilityHandlers[i];
            }
            CombatEvents.DamageDealt += OnDamage;
        }

        public void Dispose()
        {
            CombatEvents.DamageDealt -= OnDamage;
            for (int i = 0; i < _agents.Length; i++)
                if (_agents[i] != null && _abilityHandlers[i] != null) _agents[i].AbilityUsed -= _abilityHandlers[i];
        }

        private void OnDamage(DamageReport report)
        {
            int target = report.Target != null && _byHealth.TryGetValue(report.Target, out int t) ? t : MatchStats.Nobody;
            int dealer = report.Source.Owner != null && _byOwner.TryGetValue(report.Source.Owner, out int d) ? d : MatchStats.Nobody;
            if (target == MatchStats.Nobody && dealer == MatchStats.Nobody) return;   // nothing to do with the arena's agents
            _stats.Hit(dealer, target, report.Amount, report.Killed, Time.time);
        }
    }
}
