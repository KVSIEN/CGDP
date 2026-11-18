using System;
using UnityEngine;

namespace CGD.DevTools
{
    // One kind of enemy in a lineup squad, and how many of it.
    [Serializable]
    public class LineupMember
    {
        [Tooltip("An enemy prefab (anything with an EnemyAI on its root)")]
        public GameObject Prefab;
        [Min(1)] public int Count = 1;
    }
}
