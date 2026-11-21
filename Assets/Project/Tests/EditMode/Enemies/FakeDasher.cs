using System;
using UnityEngine;
using CGD.Abilities;

namespace CGD.Tests
{
    // Records the dash an ability asks for. Every direction is clear for `ReachLimit` metres
    // (no limit by default); `Arrive` plays out the end of the dash.
    internal sealed class FakeDasher : IDasher
    {
        public bool    IsDashing { get; set; }
        public float   ReachLimit = float.MaxValue;
        public int     Asked;
        public Vector3 Direction;
        public float   Distance, Seconds, Spacing;
        public Action<Vector3> Arrived;

        public float Reach(Vector3 direction, float distance) => Mathf.Min(distance, ReachLimit);

        public void Begin(Vector3 direction, float distance, float seconds, float spacing,
                          Action<Vector3> passed, Action<Vector3> arrived = null)
        {
            Asked++;
            Direction = direction;
            Distance  = distance;
            Seconds   = seconds;
            Spacing   = spacing;
            Arrived   = arrived;
        }

        public void Arrive() => Arrived?.Invoke(Vector3.zero);
    }
}
