using System;
using UnityEngine;

namespace CGD.Level
{
    // What one room is to be overgrown with (OvergrowthSource): the profile, how strongly it
    // shows (its faction's design strength, less on the frontier of its territory) and, where it
    // grows on part of the room only (a Breach room's side), which tiles it may take.
    public readonly struct OvergrowthRequest
    {
        private readonly Func<Vector2Int, bool> _grows;

        public OvergrowthRequest(OvergrowthProfile profile, float strength = 1f, Func<Vector2Int, bool> grows = null)
        {
            Profile  = profile;
            Strength = Mathf.Clamp01(strength);
            _grows   = grows;
        }

        public OvergrowthProfile Profile  { get; }
        public float             Strength { get; }

        // Whether this tile is on the part of the room the overgrowth takes.
        public bool Grows(Vector2Int tile) => _grows == null || _grows(tile);
    }
}
