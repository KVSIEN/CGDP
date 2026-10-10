using System;
using UnityEngine;

namespace CGD.Map
{
    // How much of the map one ship zone claims, and in how many separate pockets.
    [Serializable]
    public class CategoryZoneRule
    {
        [SerializeField] private ShipZone _zone = ShipZone.Civilian;
        [Tooltip("Relative share of the rooms. A zone with none of its categories enabled is skipped")]
        [SerializeField, Min(0f)] private float _share = 1f;
        [Tooltip("Separate areas of the map the zone grows from. More = smaller, broken-up areas")]
        [SerializeField, Min(1)] private int _pockets = 1;

        public ShipZone Zone    => _zone;
        public float    Share   => _share;
        public int      Pockets => Mathf.Max(1, _pockets);
    }
}
