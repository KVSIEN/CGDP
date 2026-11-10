using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CGD.Combat;
using CGD.Enemies;

namespace CGD.DevTools
{
    // Sets the arena up as a scenario says: agents taking part start over (EnemyRestart) at
    // home or at a given spot with the health and shield it gives them, the rest sit out
    // (switched off) or start over at home, and any told to be aware of another go hunting
    // it. Homes are where the agents stood when the scene started.
    internal sealed class ScenarioRunner
    {
        private const float SnapReach = 3f;

        private readonly AgentRoster      _agents;
        private readonly List<Vector3>    _homes = new();
        private readonly List<Quaternion> _homeRotations = new();

        public ScenarioRunner(AgentRoster agents)
        {
            _agents = agents;
            for (int i = 0; i < agents.Count; i++) RememberHome(agents[i]);
            agents.Added += RememberHome;   // an agent brought in later calls where it arrived home
        }

        private void RememberHome(EnemyAI agent)
        {
            _homes.Add(agent != null ? agent.transform.position : Vector3.zero);
            _homeRotations.Add(agent != null ? agent.transform.rotation : Quaternion.identity);
        }

        // Index of the agent the scenario wants watched, −1 for none.
        public int Load(AIScenario scenario)
        {
            for (int i = 0; i < _agents.Count; i++)
            {
                EnemyAI agent = _agents[i];
                if (agent == null) continue;

                ScenarioAgent entry = Find(scenario, agent);
                if (entry == null && scenario.OthersSitOut) SitOut(agent);
                else Start(agent, i, entry);
            }

            foreach (ScenarioAgent entry in scenario.Agents)
                MakeAware(entry);
            return IndexOf(scenario.Watch);
        }

        // One agent into the fight at home, untouched (squad sizes); or out of it.
        public void Deploy(int index) => Start(_agents[index], index, null);

        public Vector3 HomeOf(int index) => _homes[index];

        public void Bench(int index) => SitOut(_agents[index]);

        public int IndexOf(string agentName)
        {
            if (string.IsNullOrWhiteSpace(agentName)) return -1;
            for (int i = 0; i < _agents.Count; i++)
                if (_agents[i] != null && Matches(_agents[i], agentName)) return i;
            return -1;
        }

        private void Start(EnemyAI agent, int index, ScenarioAgent entry)
        {
            if (!agent.gameObject.activeSelf) agent.gameObject.SetActive(true);

            bool placed = entry != null && entry.Placement == ScenarioPlacement.Position;
            Vector3    position = placed ? Walkable(entry.Position) : Walkable(_homes[index]);
            Quaternion rotation = placed ? Quaternion.Euler(0f, entry.Facing, 0f) : _homeRotations[index];
            EnemyRestart.At(agent.gameObject, position, rotation);

            EnemyHealth health = agent.Health;
            health.SetShares(entry?.Health ?? 1f, entry?.Shield ?? 1f);
            health.IsInvulnerable = entry != null && entry.Invulnerable;
        }

        private static void SitOut(EnemyAI agent)
        {
            agent.Health.IsInvulnerable = false;
            if (agent.gameObject.activeSelf) agent.gameObject.SetActive(false);
        }

        private void MakeAware(ScenarioAgent entry)
        {
            int self  = IndexOf(entry.Agent);
            int other = IndexOf(entry.AwareOf);
            if (self < 0 || other < 0 || self == other) return;

            EnemyAI agent = _agents[self], target = _agents[other];
            if (!agent.gameObject.activeInHierarchy || !target.gameObject.activeInHierarchy) return;
            agent.AlertTo(target.transform.position, DamageSource.Of(target.gameObject));
        }

        private static ScenarioAgent Find(AIScenario scenario, EnemyAI agent)
        {
            foreach (ScenarioAgent entry in scenario.Agents)
                if (entry != null && Matches(agent, entry.Agent)) return entry;
            return null;
        }

        // By GameObject name, or by the name it shows (labels, kill feed).
        private static bool Matches(EnemyAI agent, string name) =>
            !string.IsNullOrWhiteSpace(name) &&
            (string.Equals(agent.name, name.Trim(), StringComparison.OrdinalIgnoreCase) ||
             (agent.TryGetComponent(out EnemyHealth health) && string.Equals(health.DisplayName, name.Trim(), StringComparison.OrdinalIgnoreCase)));

        private static Vector3 Walkable(Vector3 position) =>
            NavMesh.SamplePosition(position, out NavMeshHit hit, SnapReach, NavMesh.AllAreas) ? hit.position : position;
    }
}
