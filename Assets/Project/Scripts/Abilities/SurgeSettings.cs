using System;
using UnityEngine;

namespace CGD.Abilities
{
    // An ability's own Surge gauge (0–100%): it fills from combat actions (hits, kills,
    // parries) and drains when the fighting stops, down to a floor. Each ability tunes its
    // own: some fill fast and drain to nothing, some keep a reserve, some never drain.
    [Serializable]
    public class SurgeSettings
    {
        [Tooltip("Share of the gauge gained per combat action (0 = this ability has no Surge)")]
        [Range(0f, 1f)] public float GainPerAction;
        [Tooltip("Share per second it drains once the fighting stops (0 = never drains)")]
        [Min(0f)] public float DrainPerSecond = 0.08f;
        [Tooltip("Seconds without an action before it starts draining")]
        [Min(0f)] public float DrainDelay = 3f;
        [Tooltip("It never drains below this share (e.g. 0.3 keeps a 30% reserve)")]
        [Range(0f, 1f)] public float DrainFloor;

        public bool IsActive => GainPerAction > 0f;
    }
}
