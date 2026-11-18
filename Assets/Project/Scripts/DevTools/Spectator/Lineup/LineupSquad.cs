using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.DevTools
{
    // One squad of a lineup: which enemies are in it, where they appear and which patrol route
    // they walk. The spawner makes it an EnemySquad, so the members share a radio and a leader.
    [Serializable]
    public class LineupSquad
    {
        public string Name = "Squad";
        [Tooltip("Off = left out of the next play without losing its setup")]
        public bool Enabled = true;
        public List<LineupMember> Members = new();

        [Tooltip("Where the squad appears; snapped to the nearest walkable spot")]
        public Vector3 Position;
        [Tooltip("Degrees round from +Z")]
        [Range(0f, 360f)] public float Facing;
        [Tooltip("Metres between members; they stand in a ring round Position")]
        [Min(0.5f)] public float Spread = 2.5f;
        [Tooltip("Patrol route (a group of waypoints, e.g. Bio). Empty = they explore on their own (or guard their post, where their faction's Patrol doctrine says not to explore)")]
        public string Patrol;

        public int Size
        {
            get
            {
                int size = 0;
                foreach (LineupMember member in Members)
                    if (member != null && member.Prefab != null) size += Mathf.Max(0, member.Count);
                return size;
            }
        }
    }
}
