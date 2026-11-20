using System.Collections.Generic;
using UnityEngine;
using CGD.Stealth;

namespace CGD.Enemies
{
    // The bushes one enemy knows, learned by seeing them or standing in them. It remembers each
    // as the whole patch of ground it covers — where it lies, how big it is and which way — not a
    // point in the middle, so it can plan where to go in and where to lie (BushApproach). Familiarity
    // grows with every sighting and visit. A bush it has not seen it knows nothing of. Plain data;
    // the zone is only kept to tell when the bush is gone (smoke clears, a prop is removed).
    internal sealed class BushMemory
    {
        public const int   Capacity     = 24;
        public const float SightingGain = 0.1f;   // a look a second, so a bush in view is well known in ten
        public const float VisitGain    = 0.2f;
        // Passed on to kin: only what it knows well, at this share of its own familiarity.
        public const float ShareAbove   = 0.5f;
        public const float SharedShare  = 0.5f;

        internal sealed class KnownBush
        {
            public ConcealmentZone Zone;
            public BushFootprint   Area;
            public float           Familiarity;
        }

        private readonly List<KnownBush> _bushes = new();

        public IReadOnlyList<KnownBush> Bushes => _bushes;

        public void Forget() => _bushes.Clear();

        // Saw it from outside (`gain`) or stood in it.
        public void Learn(ConcealmentZone zone, float gain, float atLeast = 0f)
        {
            if (!BushHops.Hides(zone)) return;

            KnownBush known = Find(zone);
            if (known == null)
            {
                if (_bushes.Count >= Capacity) ForgetLeastFamiliar();
                known = new KnownBush { Zone = zone };
                _bushes.Add(known);
            }
            known.Area        = zone.Footprint;
            known.Familiarity = Mathf.Clamp01(Mathf.Max(known.Familiarity + gain, atLeast));
        }

        // Drops the bushes that are gone.
        public void Prune()
        {
            for (int i = _bushes.Count - 1; i >= 0; i--)
                if (_bushes[i].Zone == null || !_bushes[i].Zone.isActiveAndEnabled) _bushes.RemoveAt(i);
        }

        // Tells `other` about the bushes this one knows well, at half the familiarity.
        public void ShareWith(BushMemory other)
        {
            if (other == this) return;
            foreach (KnownBush known in _bushes)
                if (known.Familiarity >= ShareAbove && known.Zone != null && known.Zone.isActiveAndEnabled)
                    other.Learn(known.Zone, 0f, known.Familiarity * SharedShare);
        }

        // The best bush to fall back into from `from`, away from `threat`, scored like PlaceMemory's
        // places (worth to the faction, how well it knows it, how far) — and the spot in it to lie
        // at, deep on the side away from the threat.
        public bool TryBestHide(GroundDoctrine doctrine, Vector3 from, Vector3 threat, out Vector3 spot, out float score)
        {
            spot  = default;
            score = 0f;
            float value = doctrine.ValueOf(PlaceTraits.Hidden);
            if (value <= 0f) return false;

            float fromThreat = Flat(from - threat).magnitude;
            foreach (KnownBush known in _bushes)
            {
                Vector3 hide = BushApproach.HideSpot(threat, known.Area);
                float distance = Flat(hide - from).magnitude;
                float threatDistance = Flat(hide - threat).magnitude;
                if (distance > PlaceMemory.FallBackReach || threatDistance < PlaceMemory.MinThreatDistance || threatDistance < fromThreat) continue;

                float worth = value * known.Familiarity - distance / PlaceMemory.FallBackReach;
                if (worth <= score) continue;
                score = worth;
                spot  = hide;
            }
            return score > 0f;
        }

        // Its footprints, for the routing to look through (cleared first).
        public void CopyAreas(List<BushFootprint> areas)
        {
            areas.Clear();
            foreach (KnownBush known in _bushes) areas.Add(known.Area);
        }

        private static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);

        private KnownBush Find(ConcealmentZone zone)
        {
            foreach (KnownBush known in _bushes)
                if (known.Zone == zone) return known;
            return null;
        }

        private void ForgetLeastFamiliar()
        {
            int weakest = 0;
            for (int i = 1; i < _bushes.Count; i++)
                if (_bushes[i].Familiarity < _bushes[weakest].Familiarity) weakest = i;
            _bushes.RemoveAt(weakest);
        }
    }
}
