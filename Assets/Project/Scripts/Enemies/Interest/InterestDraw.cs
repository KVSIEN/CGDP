using System;
using UnityEngine;
using CGD.Combat;

namespace CGD.Enemies
{
    // How much one faction likes a kind of interest point.
    [Serializable]
    public struct InterestDraw
    {
        public Team Team;
        [Tooltip("How much it wants to go there, 0–1 (0 = ignores it)")]
        [Range(0f, 1f)] public float Appeal;
    }
}
