using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CGD.Combat;

namespace CGD.Enemies
{
    // Measurements of a planned NavMesh route — how long it is, how much of it a threat could
    // watch, how close it passes to hostiles — used to weigh flanks and fallbacks. Corners are
    // read through one shared buffer: NavMeshPath.corners allocates a new array on every read.
    internal static class RouteMetrics
    {
        private const float BodyHeight = 1.2f;

        private static Vector3[] _corners = new Vector3[32];

        public static float Length(NavMeshPath path)
        {
            int count = ReadCorners(path);
            float length = 0f;
            for (int i = 1; i < count; i++) length += Vector3.Distance(_corners[i - 1], _corners[i]);
            return length;
        }

        // Share of the route's corners (past the start) that an eye at `eye` could see.
        public static float Exposure(NavMeshPath path, Vector3 eye, LayerMask obstacleMask)
        {
            int count = ReadCorners(path);
            if (count < 2) return 0f;

            int seen = 0;
            for (int i = 1; i < count; i++)
                if (!Physics.Linecast(eye, _corners[i] + Vector3.up * BodyHeight, obstacleMask, QueryTriggerInteraction.Ignore)) seen++;
            return seen / (float)(count - 1);
        }

        // The closest any corner of the route comes to a living hostile.
        public static float Clearance(NavMeshPath path, IReadOnlyList<HealthManager> hostiles)
        {
            int count = ReadCorners(path);
            float clearance = float.MaxValue;
            for (int i = 0; i < count; i++)
            for (int h = 0; h < hostiles.Count; h++)
                if (hostiles[h] != null && !hostiles[h].IsDead)
                    clearance = Mathf.Min(clearance, Vector3.Distance(_corners[i], hostiles[h].transform.position));
            return clearance;
        }

        private static int ReadCorners(NavMeshPath path)
        {
            int count = path.GetCornersNonAlloc(_corners);
            while (count == _corners.Length)   // may have been cut short: grow and read again
            {
                _corners = new Vector3[_corners.Length * 2];
                count    = path.GetCornersNonAlloc(_corners);
            }
            return count;
        }
    }
}
