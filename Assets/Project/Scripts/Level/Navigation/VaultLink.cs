using UnityEngine;

namespace CGD.Level
{
    // A place enemies may hop: over low cover from one side to the other, or (OneWay) down off
    // a ledge. In the level's own space (metres from its origin, on the floor at each end),
    // crossable anywhere along `Width`.
    public readonly struct VaultLink
    {
        public VaultLink(Vector3 from, Vector3 to, float width, bool oneWay = false)
        {
            From   = from;
            To     = to;
            Width  = width;
            OneWay = oneWay;
        }

        public Vector3 From   { get; }
        public Vector3 To     { get; }
        public float   Width  { get; }
        // Only from From to To: a drop, not climbed back up.
        public bool    OneWay { get; }
    }
}
