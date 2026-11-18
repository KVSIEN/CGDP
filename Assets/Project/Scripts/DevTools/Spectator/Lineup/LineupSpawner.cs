using UnityEngine;
using UnityEngine.AI;
using CGD.Enemies;

namespace CGD.DevTools
{
    // Builds the SimulatorLineup when the arena loads: one EnemySquad per squad, its enemies
    // standing round the squad's spot with the squad's patrol route. Put it on the Enemies
    // object; with no lineup it spawns nothing (agents placed under the object by hand still
    // play). Runs after the NavMesh is built (RuntimeNavMesh) and before anything reads the agents
    // under this object (SpectatorCamera, EnemyRespawner).
    [DefaultExecutionOrder(-900)]
    public class LineupSpawner : MonoBehaviour
    {
        private const float SnapReach = 3f;

        [Tooltip("The setup to play. Empty = nobody is spawned")]
        [SerializeField] private SimulatorLineup _lineup;
        [Tooltip("Parent of the patrol waypoints; a squad's Patrol names a group of them (Bio for Bio 1, Bio 2, …)")]
        [SerializeField] private Transform _waypoints;

        public SimulatorLineup Lineup => _lineup;
        public Transform Waypoints    => _waypoints;

        private void Awake()
        {
            if (_lineup == null) return;

            var names = new LineupNames();
            foreach (LineupSquad squad in _lineup.Squads)
                if (squad != null && squad.Enabled && squad.Size > 0) Spawn(squad, names);
        }

        private void Spawn(LineupSquad squad, LineupNames names)
        {
            var group = new GameObject(squad.Name);
            group.transform.SetParent(transform, false);
            EnemySquad enemySquad = group.AddComponent<EnemySquad>();

            Transform[] route = LineupLayout.PatrolRoute(_waypoints, squad.Patrol);
            Quaternion facing = LineupLayout.FacingOf(squad);
            int total = squad.Size, index = 0;

            foreach (LineupMember member in squad.Members)
            {
                if (member == null || member.Prefab == null) continue;
                for (int n = 0; n < member.Count; n++)
                    SpawnOne(member.Prefab, LineupLayout.SpotOf(squad, index++, total), facing, group.transform, route, names, enemySquad);
            }
        }

        private static void SpawnOne(GameObject prefab, Vector3 spot, Quaternion facing, Transform parent, Transform[] route,
                                     LineupNames names, EnemySquad squad)
        {
            if (NavMesh.SamplePosition(spot, out NavMeshHit hit, SnapReach, NavMesh.AllAreas)) spot = hit.position;

            GameObject body = Instantiate(prefab, spot, facing, parent);
            EnemyHealth health = body.GetComponent<EnemyHealth>();
            string label = names.Next(health.DisplayName);
            body.name = label;
            health.SetDisplayName(label);

            EnemyAI agent = body.GetComponent<EnemyAI>();
            agent.SetWaypoints(route);
            squad.Join(agent);
        }
    }
}
