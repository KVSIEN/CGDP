using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CGD.Combat;
using CGD.Enemies;

namespace CGD.DevTools
{
    // Changes how many agents of a faction are in the fight while it runs. Bringing one in
    // reuses an agent of that faction sent away earlier, or else spawns a new one from the
    // faction's prefab beside its first agent's home (same patrol route, joined to its squad);
    // the last arrival is the first to leave. Agents sent away are switched off and stay on
    // the roster, out of the stats until they have taken part.
    internal sealed class SquadSizer
    {
        public const int MaxPerFaction = 4;

        private const float SnapReach = 3f;
        private const float Spread    = 3f;

        private static readonly Vector2[] SpawnOffsets = { new(-1f, 0f), new(1f, 0f), new(0f, -1f), new(0f, 1f) };

        private readonly AgentRoster    _agents;
        private readonly ScenarioRunner _runner;
        private readonly Transform      _parent;
        private readonly GameObject[]   _prefabs;
        private readonly List<Team>     _teams = new();

        public SquadSizer(AgentRoster agents, ScenarioRunner runner, Transform parent, GameObject[] prefabs)
        {
            _agents  = agents;
            _runner  = runner;
            _parent  = parent;
            _prefabs = prefabs ?? System.Array.Empty<GameObject>();

            for (int i = 0; i < agents.Count; i++)
                if (agents[i] != null) AddTeam(TeamOf(agents[i]));
            foreach (GameObject prefab in _prefabs)
                if (TeamOf(prefab, out Team team)) AddTeam(team);
        }

        public IReadOnlyList<Team> Teams => _teams;

        // Agents of the faction that exist (in the fight or sent away), or the cap when it can spawn more.
        public int Capacity(Team team) => PrefabFor(team) != null ? MaxPerFaction : Count(team, activeOnly: false);

        public int Active(Team team) => Count(team, activeOnly: true);

        // A faction's colour, from its damage numbers.
        public Color ColorOf(Team team)
        {
            for (int i = 0; i < _agents.Count; i++)
                if (Is(team, i)) return _agents[i].Data.DamageNumberColor;
            GameObject prefab = PrefabFor(team);
            return prefab != null ? prefab.GetComponent<EnemyAI>().Data.DamageNumberColor : Color.white;
        }

        public void Resize(Team team, int size)
        {
            size = Mathf.Clamp(size, 0, Capacity(team));
            int active = Active(team);
            EnemySquad squad = SquadOf(team);

            while (active < size)
            {
                int index = FirstSentAway(team);
                if (index < 0) index = Spawn(team);
                if (index < 0) break;

                _runner.Deploy(index);
                squad?.Join(_agents[index]);
                active++;
            }
            for (int i = _agents.Count - 1; i >= 0 && active > size; i--)
            {
                if (!Is(team, i) || !_agents[i].gameObject.activeSelf) continue;
                squad?.Leave(_agents[i]);
                _agents[i].JoinSquad(null);
                _runner.Bench(i);
                active--;
            }
        }

        // Spawns a new agent of the faction (switched on at home by the caller); its roster number, or −1.
        private int Spawn(Team team)
        {
            GameObject prefab = PrefabFor(team);
            if (prefab == null || Count(team, activeOnly: false) >= MaxPerFaction) return -1;

            int first = FirstOf(team);
            EnemyAI template = first >= 0 ? _agents[first] : null;
            int number = Count(team, activeOnly: false) + 1;

            Vector3 around = first >= 0 ? _runner.HomeOf(first) : _parent.position;
            Vector2 offset = SpawnOffsets[(number - 2 + SpawnOffsets.Length) % SpawnOffsets.Length] * Spread;
            Vector3 spot   = around + new Vector3(offset.x, 0f, offset.y);
            if (NavMesh.SamplePosition(spot, out NavMeshHit hit, SnapReach, NavMesh.AllAreas)) spot = hit.position;

            string label = $"{team} Agent {number}";
            GameObject body = Object.Instantiate(prefab, spot, template != null ? template.transform.rotation : Quaternion.identity, _parent);
            body.name = label;
            body.GetComponent<EnemyHealth>().SetDisplayName(label);
            EnemyAI agent = body.GetComponent<EnemyAI>();
            if (template != null) agent.SetWaypoints(template.Waypoints);

            _agents.Add(agent);
            return _agents.Count - 1;
        }

        private int FirstSentAway(Team team)
        {
            for (int i = 0; i < _agents.Count; i++)
                if (Is(team, i) && !_agents[i].gameObject.activeSelf) return i;
            return -1;
        }

        private int FirstOf(Team team)
        {
            for (int i = 0; i < _agents.Count; i++)
                if (Is(team, i)) return i;
            return -1;
        }

        private int Count(Team team, bool activeOnly)
        {
            int count = 0;
            for (int i = 0; i < _agents.Count; i++)
                if (Is(team, i) && (!activeOnly || _agents[i].gameObject.activeSelf)) count++;
            return count;
        }

        private bool Is(Team team, int index) => _agents[index] != null && TeamOf(_agents[index]) == team;

        // Read from the health component's data, which is set before the agent has woken up (EnemyAI.Health is not).
        private static Team TeamOf(EnemyAI agent) => agent.TryGetComponent(out EnemyHealth health) ? health.Team : Team.None;

        private void AddTeam(Team team)
        {
            if (!_teams.Contains(team)) _teams.Add(team);
        }

        private GameObject PrefabFor(Team team)
        {
            foreach (GameObject prefab in _prefabs)
                if (TeamOf(prefab, out Team own) && own == team) return prefab;
            return null;
        }

        // The prefab's faction, read from the data set on it (it has not woken up).
        private static bool TeamOf(GameObject prefab, out Team team)
        {
            team = Team.None;
            if (prefab == null || !prefab.TryGetComponent(out EnemyAI ai) || ai.Data == null) return false;
            team = ai.Data.Team;
            return true;
        }

        // The squad the faction's agents already fight in, if any.
        private EnemySquad SquadOf(Team team)
        {
            for (int i = 0; i < _agents.Count; i++)
                if (Is(team, i) && _agents[i].Squad != null) return _agents[i].Squad;
            return null;
        }
    }
}
