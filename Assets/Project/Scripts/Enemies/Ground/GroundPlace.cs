using UnityEngine;

namespace CGD.Enemies
{
    // One place an enemy knows, as seen from outside (the spectator's overlay): where, what it
    // is good for, how familiar it is (0–1) and how dangerous it feels now (0–1).
    public readonly struct GroundPlace
    {
        public GroundPlace(Vector3 position, PlaceTraits traits, float familiarity, float danger)
        {
            Position    = position;
            Traits      = traits;
            Familiarity = familiarity;
            Danger      = danger;
        }

        public Vector3     Position    { get; }
        public PlaceTraits Traits      { get; }
        public float       Familiarity { get; }
        public float       Danger      { get; }
    }
}
