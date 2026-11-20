using System.Collections.Generic;
using UnityEngine;
using CGD.Stealth;

namespace CGD.Enemies
{
    // An enemy getting to know the ground it walks: about once a second as it moves (now and
    // then while it stands), it looks round where it is (GroundReading) and remembers the spot
    // if it is good for something — hidden, a vantage down a lane, a tight corridor — growing
    // more familiar with every visit (PlaceMemory). It notes concealment it can see from a
    // distance (BushMemory: each as the whole patch it covers, from what it sees of it), marks where it
    // was hurt or saw an ally fall, and swaps what it knows well with kin close by. Only its own eyes and body: it is never handed the level's room data.
    //
    // What it learned is used by its faction's GroundDoctrine: falling back to the best place
    // it knows when its nerve breaks, and a guard watching down the lane it knows at its post.
    internal sealed class EnemyGround
    {
        private const float SampleInterval   = 1f;
        // While it stands still it looks round again only this often.
        private const float StandingInterval = 6f;
        private const float MovedEnough      = 1f;
        private const float RayReach         = 24f;
        private const float SightingRange    = 20f;
        private const float ShareInterval    = 4f;
        private const float ShareRange       = 6f;
        private const int   Glimpses         = 5;   // the middle of a bush and each of its sides

        private readonly EnemyAI     _ai;
        private readonly LayerMask   _obstacleMask;
        private readonly PlaceMemory _memory = new();
        private readonly BushMemory  _bushes = new();
        private readonly float[]     _reach  = new float[GroundReading.Rays];
        private float   _nextSample;
        private float   _lastSample = float.NegativeInfinity;
        private Vector3 _lastSpot;
        private float   _nextShare;

        public EnemyGround(EnemyAI ai, LayerMask obstacleMask)
        {
            _ai           = ai;
            _obstacleMask = obstacleMask;
            _nextSample   = Random.value * SampleInterval;
            _nextShare    = Random.value * ShareInterval;
        }

        internal PlaceMemory Memory => _memory;
        internal BushMemory  Bushes => _bushes;
        private GroundDoctrine Doctrine => _ai.Doctrine.Ground;

        public void Tick(float time)
        {
            if (time >= _nextSample)
            {
                _nextSample = time + SampleInterval;
                Vector3 spot = _ai.transform.position;
                bool moved = (spot - _lastSpot).sqrMagnitude >= MovedEnough * MovedEnough;
                if (moved || time >= _lastSample + StandingInterval) TakeStock(spot, time);
            }

            if (time >= _nextShare)
            {
                _nextShare = time + ShareInterval;
                if (Doctrine.ShareWithKin) ShareWithKinNearby(time);
            }
        }

        // It was hurt where it stands.
        public void OnHurt(float time)
        {
            _memory.MarkDanger(_ai.transform.position, time);
            _ai.Exploration.Invalidate();
        }

        // It saw or heard an ally fall there.
        public void OnAllyFell(Vector3 position, float time)
        {
            _memory.MarkDanger(position, time);
            _ai.Exploration.Invalidate();
        }

        // Pooled and sent somewhere new: it knows nothing there yet.
        public void Forget()
        {
            _memory.Forget();
            _bushes.Forget();
            _lastSample = float.NegativeInfinity;
        }

        // Where to fall back to when its nerve breaks, if its doctrine says to and it knows a place worth it.
        public bool TryFallBack(Vector3 threat, out Vector3 point)
        {
            point = default;
            if (!Doctrine.FallBackToKnownGround) return false;

            Vector3 self = _ai.transform.position;
            bool place = _memory.TryBest(Doctrine, self, threat, Time.time, out Vector3 placePoint, out float placeScore);
            bool bush  = _bushes.TryBestHide(Doctrine, self, threat, out Vector3 bushSpot, out float bushScore);
            if (!place && !bush) return false;

            point = bush && (!place || bushScore > placeScore) ? bushSpot : placePoint;
            return true;
        }

        // A guard faces down the lane it knows from its post; otherwise the way it was set.
        public Quaternion WatchFacing(Vector3 post, Quaternion postFacing) =>
            _memory.TryLaneAt(post, out Vector3 lane) ? Quaternion.LookRotation(lane, Vector3.up) : postFacing;

        // Every spot it knows, and each bush as the patch it covers.
        public void CopyPlaces(List<GroundPlace> results)
        {
            _memory.CopyTo(results, Time.time);
            foreach (BushMemory.KnownBush known in _bushes.Bushes)
                results.Add(new GroundPlace(known.Area.Centre, PlaceTraits.Hidden, known.Familiarity, 0f, known.Area));
        }

        private void TakeStock(Vector3 spot, float time)
        {
            _lastSample = time;
            _lastSpot   = spot;
            _bushes.Prune();
            _ai.Exploration.Visit(spot, time);

            Vector3 eye = spot + Vector3.up * EnemyEyes.Height;
            for (int i = 0; i < GroundReading.Rays; i++)
                _reach[i] = Physics.Raycast(eye, GroundReading.Direction(i), out RaycastHit hit, RayReach, _obstacleMask, QueryTriggerInteraction.Ignore)
                    ? hit.distance : RayReach;

            ConcealmentZone inside = ConcealmentAt(spot);
            if (inside != null) _bushes.Learn(inside, BushMemory.VisitGain);

            PlaceTraits traits = GroundReading.Classify(_reach, inside != null, out int lane);
            if (traits != PlaceTraits.None) _memory.Visit(spot, traits, GroundReading.Direction(lane));
            SightConcealment(eye);
        }

        private static ConcealmentZone ConcealmentAt(Vector3 spot)
        {
            Vector3 body = spot + Vector3.up * 0.5f;
            foreach (ConcealmentZone zone in ConcealmentZone.Active)
                if (zone != null && zone.Contains(body)) return zone;
            return null;
        }

        // Every bush in view and close enough is known, as the patch it covers: it only has to
        // see some of it — the middle or any side — not the point in the middle.
        private void SightConcealment(Vector3 eye)
        {
            IReadOnlyList<ConcealmentZone> zones = ConcealmentZone.Active;
            for (int i = 0; i < zones.Count; i++)
            {
                ConcealmentZone zone = zones[i];
                if (!BushHops.Hides(zone)) continue;

                BushFootprint area = zone.Footprint;
                if (area.DistanceTo(eye) <= SightingRange && SeesPartOf(eye, area)) _bushes.Learn(zone, BushMemory.SightingGain);
            }
        }

        private bool SeesPartOf(Vector3 eye, BushFootprint area)
        {
            for (int i = 0; i < Glimpses; i++)
            {
                Vector3 point = area.Centre + Vector3.up * 0.5f;
                if (i > 0)
                {
                    // The four sides, just inside the edge.
                    float x = i == 1 ? area.HalfSize.x : i == 2 ? -area.HalfSize.x : 0f;
                    float z = i == 3 ? area.HalfSize.y : i == 4 ? -area.HalfSize.y : 0f;
                    point = area.Inset(area.PointAt(x, z), 0.3f) + Vector3.up * 0.5f;
                }

                Vector3 toPoint = point - eye;
                if (toPoint.sqrMagnitude > SightingRange * SightingRange) continue;
                if (Vector3.Angle(_ai.transform.forward, toPoint) > _ai.Data.SightAngle * 0.5f) continue;
                if (!Physics.Linecast(eye, point, _obstacleMask, QueryTriggerInteraction.Ignore)) return true;
            }
            return false;
        }

        private void ShareWithKinNearby(float time)
        {
            Vector3 self = _ai.transform.position;
            foreach (EnemyAI other in EnemyRoster.All)
            {
                if (other == _ai || !EnemyRoster.IsKin(_ai, other)) continue;
                if ((other.transform.position - self).sqrMagnitude > ShareRange * ShareRange) continue;
                _memory.ShareWith(other.Ground.Memory, time);
                _bushes.ShareWith(other.Ground.Bushes);
                _ai.Exploration.Coverage.ShareWith(other.Exploration.Coverage);
            }
        }
    }
}
