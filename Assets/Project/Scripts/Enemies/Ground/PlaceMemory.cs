using System.Collections.Generic;
using UnityEngine;

namespace CGD.Enemies
{
    // The places one enemy knows, learned by being there and looking (EnemyGround): what each
    // is good for, the lane a vantage looks down, how familiar it is and how dangerous it feels.
    // Familiarity grows with every visit, so the ground an enemy walks every day is the ground
    // it trusts; danger is set where it was hurt or saw an ally fall, and fades. Plain data and
    // arithmetic, no physics.
    internal sealed class PlaceMemory
    {
        public const float CellSize     = 2.5f;
        public const int   Capacity     = 48;
        public const float VisitGain    = 0.25f;
        public const float SightingGain = 0.15f;
        public const float DangerFade   = 30f;   // seconds for a danger to fade to about a third
        // Passed on to kin: only what it knows well, and at this share of its own familiarity.
        public const float ShareAbove   = 0.5f;
        public const float SharedShare  = 0.5f;
        // How far it will fall back, and how close to the threat a place may be.
        public const float FallBackReach    = 25f;
        public const float MinThreatDistance = 6f;

        private sealed class Place
        {
            public Vector3     Position;
            public PlaceTraits Traits;
            public Vector3     Lane;
            public float       Familiarity;
            public float       Danger;
            public float       DangerTime;
        }

        private readonly Dictionary<Vector2Int, Place> _places = new();

        public int Count => _places.Count;

        // Standing at `position` and finding it good for `traits` (`lane`: where it saw furthest).
        public void Visit(Vector3 position, PlaceTraits traits, Vector3 lane) => Learn(position, traits, lane, VisitGain);

        // A place seen from afar (a concealment zone in view): known, but not yet trusted.
        public void Sight(Vector3 position, PlaceTraits traits) => Learn(position, traits, Vector3.zero, SightingGain);

        // Where it was hurt, or saw an ally fall.
        public void MarkDanger(Vector3 position, float time)
        {
            Place place = PlaceAt(position, create: true);
            place.Danger     = 1f;
            place.DangerTime = time;
        }

        public void Forget() => _places.Clear();

        // The lane a known vantage at this spot looks down, if it knows one there.
        public bool TryLaneAt(Vector3 position, out Vector3 lane)
        {
            Place place = PlaceAt(position, create: false);
            lane = place != null && (place.Traits & PlaceTraits.Vantage) != 0 ? place.Lane : Vector3.zero;
            return lane != Vector3.zero;
        }

        // The best place to fall back to from `from`, away from `threat`: worth most to the
        // faction, most familiar, least dangerous, nearest. Within reach, not near the threat
        // and no closer to it than where it stands.
        public bool TryBest(GroundDoctrine doctrine, Vector3 from, Vector3 threat, float time, out Vector3 point)
        {
            point = default;
            float best = 0f;
            float fromThreat = Flat(from - threat).magnitude;
            foreach (Place place in _places.Values)
            {
                float value = doctrine.ValueOf(place.Traits);
                if (value <= 0f) continue;

                float distance = Flat(place.Position - from).magnitude;
                float threatDistance = Flat(place.Position - threat).magnitude;
                if (distance > FallBackReach || threatDistance < MinThreatDistance || threatDistance < fromThreat) continue;

                // Fresh danger takes away its worth (all of it at aversion 1), not just a fixed amount.
                float trust = Mathf.Max(0f, 1f - doctrine.DangerAversion * DangerOf(place, time));
                float score = value * place.Familiarity * trust - distance / FallBackReach;
                if (score <= best) continue;
                best  = score;
                point = place.Position;
            }
            return best > 0f;
        }

        // Tells `other` about the places this one knows well, at half the familiarity (it was
        // told, not there). Dangers travel too.
        public void ShareWith(PlaceMemory other, float time)
        {
            if (other == this) return;
            foreach (Place place in _places.Values)
            {
                if (place.Familiarity >= ShareAbove)
                    other.Learn(place.Position, place.Traits, place.Lane, 0f, place.Familiarity * SharedShare);
                if (DangerOf(place, time) > 0.1f && other.DangerOf(other.PlaceAt(place.Position, create: false), time) < DangerOf(place, time))
                {
                    Place theirs = other.PlaceAt(place.Position, create: true);
                    theirs.Danger     = place.Danger;
                    theirs.DangerTime = place.DangerTime;
                }
            }
        }

        public void CopyTo(List<GroundPlace> results, float time)
        {
            results.Clear();
            foreach (Place place in _places.Values)
                results.Add(new GroundPlace(place.Position, place.Traits, place.Familiarity, DangerOf(place, time)));
        }

        // `gain`: added to its familiarity; `atLeast`: the familiarity it has at the least.
        private void Learn(Vector3 position, PlaceTraits traits, Vector3 lane, float gain, float atLeast = 0f)
        {
            Place place = PlaceAt(position, create: true);
            place.Traits |= traits;
            if (lane != Vector3.zero) place.Lane = lane;
            place.Familiarity = Mathf.Clamp01(Mathf.Max(place.Familiarity + gain, atLeast));
        }

        private float DangerOf(Place place, float time) =>
            place == null || place.Danger <= 0f ? 0f : place.Danger * Mathf.Exp(-(time - place.DangerTime) / DangerFade);

        private Place PlaceAt(Vector3 position, bool create)
        {
            var cell = new Vector2Int(Mathf.RoundToInt(position.x / CellSize), Mathf.RoundToInt(position.z / CellSize));
            if (_places.TryGetValue(cell, out Place place) || !create) return place;

            if (_places.Count >= Capacity) ForgetLeastFamiliar();
            place = new Place { Position = position };
            _places[cell] = place;
            return place;
        }

        private void ForgetLeastFamiliar()
        {
            Vector2Int weakest = default;
            float least = float.MaxValue;
            foreach (KeyValuePair<Vector2Int, Place> pair in _places)
            {
                if (pair.Value.Familiarity >= least) continue;
                least   = pair.Value.Familiarity;
                weakest = pair.Key;
            }
            _places.Remove(weakest);
        }

        private static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);
    }
}
