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
        private readonly AgentRoster _agents;
        private readonly List<Action<Ability>> _abilityHandlers = new();
        private readonly Dictionary<GameObject, int>    _byOwner  = new();
        private readonly Dictionary<HealthManager, int> _byHealth = new();

        public MatchStatsRecorder(AgentRoster agents, MatchStats stats)
        {
            _agents = agents;
            _stats  = stats;
            for (int i = 0; i < agents.Count; i++) Register(agents[i], i);
            agents.Added += OnAdded;
            CombatEvents.DamageDealt += OnDamage;
        }

        public void Dispose()
        {
            CombatEvents.DamageDealt -= OnDamage;
            _agents.Added -= OnAdded;
            for (int i = 0; i < _agents.Count; i++)
                if (_agents[i] != null && _abilityHandlers[i] != null) _agents[i].AbilityUsed -= _abilityHandlers[i];
        }

        private void OnAdded(EnemyAI agent) => Register(agent, _agents.Count - 1);

        private void Register(EnemyAI agent, int number)
        {
            _stats.Grow(number + 1);
            Action<Ability> handler = null;
            if (agent != null)
            {
                _byOwner[agent.gameObject] = number;
                if (agent.TryGetComponent(out EnemyHealth health)) _byHealth[health] = number;
                handler = _ => _stats.AbilityUsed(number);
                agent.AbilityUsed += handler;
            }
            _abilityHandlers.Add(handler);
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
