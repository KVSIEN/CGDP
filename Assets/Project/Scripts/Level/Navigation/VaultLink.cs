using UnityEngine;

namespace CGD.Level
{
    // A place enemies may hop over low cover: from one side to the other, in the level's own
    // space (metres from its origin, on the floor), crossable anywhere along `Width`.
    public readonly struct VaultLink
    {
        public VaultLink(Vector3 from, Vector3 to, float width)
        {
            From  = from;
            To    = to;
            Width = width;
        }

        public Vector3 From  { get; }
        public Vector3 To    { get; }
        public float   Width { get; }
    }
}
