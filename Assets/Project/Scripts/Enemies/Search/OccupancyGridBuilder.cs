using UnityEngine;
using UnityEngine.AI;

namespace CGD.Enemies
{
    // Lays an OccupancyGrid over the NavMesh round a point: a cell is walkable where the NavMesh
    // is under its middle (on the same floor as the point, give or take a step), and two
    // neighbours are joined where an agent could walk straight from one middle to the other.
    // Done once when a search starts; a squad shares the result (OccupancySearch).
    internal static class OccupancyGridBuilder
    {
        private const float FloorSearch = 0.8f;
        private const float StepHeight  = 1.2f;

        public static OccupancyGrid Around(Vector3 center, float radius, float cellSize)
        {
            int size = Mathf.Max(1, Mathf.CeilToInt(radius * 2f / cellSize));
            var origin = new Vector3(center.x - size * cellSize * 0.5f, center.y, center.z - size * cellSize * 0.5f);
            int count = size * size;
            var walkable = new bool[count];
            var heights  = new float[count];
            var floor    = new Vector3[count];

            for (int cell = 0; cell < count; cell++)
            {
                var middle = new Vector3(origin.x + (cell % size + 0.5f) * cellSize, center.y, origin.z + (cell / size + 0.5f) * cellSize);
                if (!NavMesh.SamplePosition(middle, out NavMeshHit hit, Mathf.Max(FloorSearch, cellSize * 0.5f), NavMesh.AllAreas)) continue;
                if (Mathf.Abs(hit.position.y - center.y) > StepHeight) continue;
                walkable[cell] = true;
                heights[cell]  = hit.position.y;
                floor[cell]    = hit.position;
            }

            var openEast  = new bool[count];
            var openNorth = new bool[count];
            for (int cell = 0; cell < count; cell++)
            {
                if (!walkable[cell]) continue;
                if (cell % size + 1 < size && walkable[cell + 1])     openEast[cell]  = Joined(floor[cell], floor[cell + 1]);
                if (cell + size < count && walkable[cell + size])     openNorth[cell] = Joined(floor[cell], floor[cell + size]);
            }
            return new OccupancyGrid(origin, cellSize, size, size, walkable, openEast, openNorth, heights);
        }

        // NavMesh.Raycast hits the NavMesh's edge: a wall or drop between the two.
        private static bool Joined(Vector3 a, Vector3 b) => !NavMesh.Raycast(a, b, out _, NavMesh.AllAreas);
    }
}
