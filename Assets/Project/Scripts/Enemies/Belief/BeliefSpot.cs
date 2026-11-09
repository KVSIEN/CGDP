using UnityEngine;

namespace CGD.Enemies
{
    // One place an unseen target could be: a cover spot, its current likelihood (0..1), and whether
    // the believer has looked there and found it empty.
    public struct BeliefSpot
    {
        public int     Slot;
        public Vector3 Position;
        public float   Weight;
        public bool    Checked;
    }
}
