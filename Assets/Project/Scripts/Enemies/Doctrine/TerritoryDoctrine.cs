using System;
using UnityEngine;

namespace CGD.Enemies
{
    // How far a faction's enemies follow a target out of the territory they were placed in. Past
    // that they hold at the edge (a doorway standoff) rather than chase on, and what they
    // can't reach they search for as usual and then give up. Only enemies that have a home
    // zone (placed by the level builder) are held; reinforcements and hand-placed enemies
    // roam free.
    [Serializable]
    public sealed class TerritoryDoctrine
    {
        [Tooltip("Its enemies stay within the territory (the rooms their faction holds) they were placed in. Off = they follow a target anywhere")]
        [SerializeField] private bool _bound = true;
        [Tooltip("Metres past its territory's walls it will follow a target before holding at the edge")]
        [SerializeField, Min(0f)] private float _pursuitReach = 16f;

        public TerritoryDoctrine() { }

        public TerritoryDoctrine(bool bound, float pursuitReach)
        {
            _bound        = bound;
            _pursuitReach = pursuitReach;
        }

        public bool  IsBound       => _bound;
        public float PursuitReach  => _pursuitReach;
    }
}
