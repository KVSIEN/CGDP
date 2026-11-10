using UnityEngine;

namespace CGD.Enemies
{
    // One place an unseen target could be: a cover spot or a bush, its current likelihood (0..1),
    // and whether the believer has looked there and found it empty. A concealed spot (in a bush or
    // smoke) can't be seen to be empty from afar: it has to be walked up to.
    public struct BeliefSpot
    {
        public int     Slot;
        public Vector3 Position;
        public float   Weight;
        public bool    Checked;
        public bool    Concealed;
    }
}
