using System;
using UnityEngine;

namespace CGD.DevTools
{
    // How one agent starts a scenario (AIScenario).
    [Serializable]
    public class ScenarioAgent
    {
        [Tooltip("The agent's GameObject name under the spectator's Agents, e.g. Bio Agent")]
        public string Agent;
        public ScenarioPlacement Placement = ScenarioPlacement.Home;
        [Tooltip("With Placement = Position; snapped to the nearest walkable spot")]
        public Vector3 Position;
        [Tooltip("Degrees round from +Z, with Placement = Position")]
        [Range(0f, 360f)] public float Facing;
        [Tooltip("Share of max health it starts with")]
        [Range(0.01f, 1f)] public float Health = 1f;
        [Tooltip("Share of max shield it starts with; 0 = broken, waiting out its regen delay")]
        [Range(0f, 1f)] public float Shield = 1f;
        [Tooltip("Takes no damage (a punching bag, or to watch it keep going)")]
        public bool Invulnerable;
        [Tooltip("Another agent's name: it starts knowing roughly where that one is and goes hunting it. Empty = calm")]
        public string AwareOf;
    }
}
