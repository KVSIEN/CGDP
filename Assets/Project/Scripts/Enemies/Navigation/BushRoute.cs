using UnityEngine;

namespace CGD.Enemies
{
    // How to get into a bush: optionally round to a point outside it first (Via), then on to the
    // spot inside where it lies low (Spot).
    internal readonly struct BushRoute
    {
        public readonly bool    HasVia;
        public readonly Vector3 Via;
        public readonly Vector3 Spot;

        public BushRoute(bool hasVia, Vector3 via, Vector3 spot)
        {
            HasVia = hasVia;
            Via    = via;
            Spot   = spot;
        }

        // Where to head first.
        public Vector3 First => HasVia ? Via : Spot;
    }
}
