using System.Collections.Generic;
using UnityEngine;

namespace CGD.DevTools
{
    // The rules that put a squad's members on the ground, shared by the spawner and the
    // inspector's Scene-view preview so what is drawn is what appears.
    public static class LineupLayout
    {
        // Member 0 of a lone member stands on the spot; several stand evenly round it.
        public static Vector3 SpotOf(LineupSquad squad, int index, int total)
        {
            if (total <= 1) return squad.Position;

            float angle = squad.Facing + index * 360f / total;
            Quaternion round = Quaternion.Euler(0f, angle, 0f);
            return squad.Position + round * Vector3.forward * squad.Spread;
        }

        public static Quaternion FacingOf(LineupSquad squad) => Quaternion.Euler(0f, squad.Facing, 0f);

        // Waypoint group names under a Waypoints object: "Bio 1", "Bio 2" form the group "Bio".
        public static List<string> PatrolGroups(Transform waypoints)
        {
            var groups = new List<string>();
            if (waypoints == null) return groups;

            foreach (Transform child in waypoints)
            {
                string group = GroupOf(child.name);
                if (!groups.Contains(group)) groups.Add(group);
            }
            return groups;
        }

        // The group's waypoints in the order they sit under the Waypoints object; empty for no group.
        public static Transform[] PatrolRoute(Transform waypoints, string group)
        {
            var route = new List<Transform>();
            if (waypoints != null && !string.IsNullOrWhiteSpace(group))
                foreach (Transform child in waypoints)
                    if (GroupOf(child.name) == group.Trim()) route.Add(child);
            return route.ToArray();
        }

        private static string GroupOf(string waypointName)
        {
            int end = waypointName.Length;
            while (end > 0 && char.IsDigit(waypointName[end - 1])) end--;
            return waypointName.Substring(0, end).TrimEnd();
        }
    }
}
