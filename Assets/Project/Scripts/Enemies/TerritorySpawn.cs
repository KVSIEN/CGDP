using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CGD.Combat;

namespace CGD.Enemies
{
    // Where a fallen enemy comes back: inside its own territory — near where it was placed and
    // along its patrol route — at the candidate spot furthest from every living hostile,
    // preferring one no hostile can see. Coming back anywhere on the map would drop a
    // faction into its rival's ground (or under its guns) and make that spot its new post,
    // which breaks the arena's layout of ranges and territories.
    internal sealed class TerritorySpawn
    {
        private const int   Candidates   = 12;
        private const float SampleReach  = 2f;
        private const float SafeDistance = 30f;   // this far from every hostile is as safe as it gets
        private const float SeenPenalty  = 25f;   // a hostile's line of sight costs as much as being this much closer
        private const float ChestHeight  = 1.2f;

        private readonly EnemyHealth _enemy;
        private readonly Vector3     _home;
        private readonly Vector3[]   _anchors;
        private readonly float       _radius;
        private readonly LayerMask   _obstacleMask;
        private readonly NavMeshPath _path = new();

        public TerritorySpawn(EnemyHealth enemy, float radius, LayerMask obstacleMask)
        {
            _enemy        = enemy;
            _home         = enemy.transform.position;
            _radius       = radius;
            _obstacleMask = obstacleMask;
            _anchors      = Anchors(enemy, _home);
        }

        // The safest of a handful of walkable spots in its territory, judged against these
        // characters (only living ones of another team count). Falls back to where it was placed.
        public Vector3 Pick(IReadOnlyList<HealthManager> others)
        {
            Vector3 best      = _home;
            float   bestScore = float.MinValue;

            for (int i = 0; i < Candidates; i++)
            {
                Vector3 anchor = _anchors[Random.Range(0, _anchors.Length)];
                Vector2 offset = Random.insideUnitCircle * _radius;
                if (!NavMesh.SamplePosition(anchor + new Vector3(offset.x, 0f, offset.y), out NavMeshHit hit, SampleReach, NavMesh.AllAreas)) continue;
                if (!IsReachable(hit.position)) continue;

                float score = Safety(hit.position, others);
                if (score <= bestScore) continue;
                bestScore = score;
                best      = hit.position;
            }
            return best;
        }

        // Distance to the nearest hostile, less a penalty if any hostile could see the spot;
        // a little noise so equally safe spots take turns.
        private float Safety(Vector3 spot, IReadOnlyList<HealthManager> others)
        {
            float   nearest = SafeDistance;
            bool    seen    = false;
            Vector3 chest   = spot + Vector3.up * ChestHeight;

            for (int i = 0; i < others.Count; i++)
            {
                HealthManager other = others[i];
                if (!IsLivingHostile(other)) continue;

                Vector3 position = other.transform.position;
                nearest = Mathf.Min(nearest, Vector3.Distance(position, spot));
                if (!seen) seen = SightLines.IsClear(position + Vector3.up * EnemyEyes.Height, chest, _obstacleMask, other.transform);
            }
            return nearest - (seen ? SeenPenalty : 0f) + Random.value;
        }

        private bool IsLivingHostile(HealthManager other) =>
            other != null && other != _enemy && !other.IsDead &&
            other.Team != Team.None && _enemy.Team != Team.None && other.Team != _enemy.Team;

        // Never a crate top or other island it couldn't walk off.
        private bool IsReachable(Vector3 spot) =>
            NavMesh.CalculatePath(_home, spot, NavMesh.AllAreas, _path) && _path.status == NavMeshPathStatus.PathComplete;

        private static Vector3[] Anchors(EnemyHealth enemy, Vector3 home)
        {
            var anchors = new List<Vector3> { home };
            if (enemy.TryGetComponent(out EnemyAI ai))
                foreach (Transform waypoint in ai.Waypoints)
                    if (waypoint != null) anchors.Add(waypoint.position);
            return anchors.ToArray();
        }
    }
}
