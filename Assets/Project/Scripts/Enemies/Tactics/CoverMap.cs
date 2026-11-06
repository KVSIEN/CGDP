using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CGD.Enemies
{
    // Every spot in the level an enemy could take cover at, found once from the NavMesh: the
    // walkable edge is sampled on a grid, and wherever a wall or obstacle stands just past
    // the edge at crouch height there's cover (high if it also blocks at head height).
    // Built the first time an enemy asks, so it works with baked and runtime-built NavMeshes.
    //
    // This is the world's list, not what any enemy knows: each enemy only uses the spots it
    // has learned (CoverKnowledge). Enemies also claim a spot while using it, so two don't
    // crowd behind the same crate.
    public class CoverMap : MonoBehaviour
    {
        private const float CellSize    = 4f;
        private const float WallMargin  = 0.6f;    // extra probe length past the agent's radius

        [Tooltip("Metres between sampled spots along the walls")]
        [SerializeField, Min(0.5f)] private float _spacing = 1.25f;
        [Tooltip("Height an obstacle must reach to give low cover (an enemy behind it ducks out of the line of fire)")]
        [SerializeField, Min(0.2f)] private float _lowHeight = 0.8f;
        [Tooltip("Height an obstacle must reach to give full cover")]
        [SerializeField, Min(0.5f)] private float _highHeight = 1.6f;
        [Tooltip("What counts as an obstacle (walls, crates, pillars)")]
        [SerializeField] private LayerMask _obstacleMask = 1;

        private readonly List<CoverPoint> _points = new();
        private readonly Dictionary<Vector2Int, List<int>> _cells = new();
        private readonly List<EnemyAI> _claims = new();
        private bool _built;

        public static CoverMap Active { get; private set; }

        public int Count
        {
            get { EnsureBuilt(); return _points.Count; }
        }

        public CoverPoint this[int index] => _points[index];

        private void OnEnable()  => Active = this;
        private void OnDisable() { if (Active == this) Active = null; }

        // Spot indices within radius of center (unsorted).
        public void Query(Vector3 center, float radius, List<int> results)
        {
            results.Clear();
            EnsureBuilt();

            Vector2Int min = CellOf(center - new Vector3(radius, 0f, radius));
            Vector2Int max = CellOf(center + new Vector3(radius, 0f, radius));
            float radiusSq = radius * radius;

            for (int x = min.x; x <= max.x; x++)
            for (int y = min.y; y <= max.y; y++)
            {
                if (!_cells.TryGetValue(new Vector2Int(x, y), out List<int> cell)) continue;
                foreach (int index in cell)
                    if ((_points[index].Position - center).sqrMagnitude <= radiusSq) results.Add(index);
            }
        }

        // --- Claims -----------------------------------------------------------------

        public bool IsTakenByOther(int index, EnemyAI who)
        {
            EnemyAI holder = _claims[index];
            return holder != null && holder != who && holder.State != EnemyAI.AiState.Dead;
        }

        public bool TryClaim(int index, EnemyAI who)
        {
            if (IsTakenByOther(index, who)) return false;
            _claims[index] = who;
            return true;
        }

        public void Release(int index, EnemyAI who)
        {
            if (index >= 0 && index < _claims.Count && _claims[index] == who) _claims[index] = null;
        }

        // --- Building ---------------------------------------------------------------

        // Rescans the NavMesh, e.g. after a level was generated or rebuilt.
        public void Rebuild()
        {
            _built = false;
            EnsureBuilt();
        }

        private void EnsureBuilt()
        {
            if (_built) return;
            _built = true;
            _points.Clear();
            _cells.Clear();
            _claims.Clear();

            NavMeshTriangulation mesh = NavMesh.CalculateTriangulation();
            if (mesh.vertices.Length == 0) { _built = false; return; }

            var bounds = new Bounds(mesh.vertices[0], Vector3.zero);
            foreach (Vector3 vertex in mesh.vertices) bounds.Encapsulate(vertex);

            float sampleReach = bounds.extents.y + 1f;
            for (float x = bounds.min.x; x <= bounds.max.x; x += _spacing)
            for (float z = bounds.min.z; z <= bounds.max.z; z += _spacing)
                TrySample(new Vector3(x, bounds.center.y, z), sampleReach);
        }

        private void TrySample(Vector3 probe, float reach)
        {
            if (!NavMesh.SamplePosition(probe, out NavMeshHit ground, reach, NavMesh.AllAreas)) return;
            if (!NavMesh.FindClosestEdge(ground.position, out NavMeshHit edge, NavMesh.AllAreas)) return;
            if (edge.distance > _spacing) return;

            Vector3 normal = edge.normal;
            normal.y = 0f;
            if (normal.sqrMagnitude < 0.01f) return;
            normal.Normalize();

            // The edge normal's sign isn't something to rely on: whichever side the obstacle is on.
            Vector3 spot = edge.position;
            float   probeLength = WallMargin + 0.5f;
            Vector3 facing;
            if      (HitsObstacle(spot, normal, _lowHeight, probeLength))  facing = normal;
            else if (HitsObstacle(spot, -normal, _lowHeight, probeLength)) facing = -normal;
            else return;   // an edge with no wall: a drop, or the end of the NavMesh

            if (IsNearExisting(spot)) return;

            bool high = HitsObstacle(spot, facing, _highHeight, probeLength);
            Add(new CoverPoint(spot, facing, high));
        }

        private bool HitsObstacle(Vector3 spot, Vector3 direction, float height, float length) =>
            Physics.Raycast(spot + Vector3.up * height, direction, length, _obstacleMask, QueryTriggerInteraction.Ignore);

        private bool IsNearExisting(Vector3 spot)
        {
            float minSq = _spacing * 0.8f * (_spacing * 0.8f);
            Vector2Int center = CellOf(spot);
            for (int x = center.x - 1; x <= center.x + 1; x++)
            for (int y = center.y - 1; y <= center.y + 1; y++)
            {
                if (!_cells.TryGetValue(new Vector2Int(x, y), out List<int> cell)) continue;
                foreach (int index in cell)
                    if ((_points[index].Position - spot).sqrMagnitude < minSq) return true;
            }
            return false;
        }

        private void Add(CoverPoint point)
        {
            int index = _points.Count;
            _points.Add(point);
            _claims.Add(null);

            Vector2Int key = CellOf(point.Position);
            if (!_cells.TryGetValue(key, out List<int> cell)) _cells[key] = cell = new List<int>();
            cell.Add(index);
        }

        private static Vector2Int CellOf(Vector3 position) =>
            new(Mathf.FloorToInt(position.x / CellSize), Mathf.FloorToInt(position.z / CellSize));
    }
}
