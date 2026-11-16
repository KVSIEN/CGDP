using System.Collections.Generic;
using UnityEngine;
using CGD.Stealth;

namespace CGD.Enemies
{
    // An enemy getting to know the ground it walks: about once a second as it moves (now and
    // then while it stands), it looks round where it is (GroundReading) and remembers the spot
    // if it is good for something — hidden, a vantage down a lane, a tight corridor — growing
    // more familiar with every visit (PlaceMemory). It notes concealment it can see from a
    // distance, marks where it was hurt or saw an ally fall, and swaps what it knows well with
    // kin close by. Only its own eyes and body: it is never handed the level's room data.
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

        private readonly EnemyAI     _ai;
        private readonly LayerMask   _obstacleMask;
        private readonly PlaceMemory _memory = new();
        private readonly float[]     _reach  = new float[GroundReading.Rays];
        private float   _nextSample;
        private float   _lastSample = float.NegativeInfinity;
        private Vector3 _lastSpot;
        private float   _nextShare;
        private int     _zoneCursor;

        public EnemyGround(EnemyAI ai, LayerMask obstacleMask)
        {
            _ai           = ai;
            _obstacleMask = obstacleMask;
            _nextSample   = Random.value * SampleInterval;
            _nextShare    = Random.value * ShareInterval;
        }

        internal PlaceMemory Memory => _memory;
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
        public void OnHurt(float time) => _memory.MarkDanger(_ai.transform.position, time);

        // It saw or heard an ally fall there.
        public void OnAllyFell(Vector3 position, float time) => _memory.MarkDanger(position, time);

        // Pooled and sent somewhere new: it knows nothing there yet.
        public void Forget()
        {
            _memory.Forget();
            _lastSample = float.NegativeInfinity;
        }

        // Where to fall back to when its nerve breaks, if its doctrine says to and it knows a place worth it.
        public bool TryFallBack(Vector3 threat, out Vector3 point)
        {
            point = default;
            return Doctrine.FallBackToKnownGround && _memory.TryBest(Doctrine, _ai.transform.position, threat, Time.time, out point);
        }

        // A guard faces down the lane it knows from its post; otherwise the way it was set.
        public Quaternion WatchFacing(Vector3 post, Quaternion postFacing) =>
            _memory.TryLaneAt(post, out Vector3 lane) ? Quaternion.LookRotation(lane, Vector3.up) : postFacing;

        public void CopyPlaces(List<GroundPlace> results) => _memory.CopyTo(results, Time.time);

        private void TakeStock(Vector3 spot, float time)
        {
            _lastSample = time;
            _lastSpot   = spot;

            Vector3 eye = spot + Vector3.up * EnemyEyes.Height;
            for (int i = 0; i < GroundReading.Rays; i++)
                _reach[i] = Physics.Raycast(eye, GroundReading.Direction(i), out RaycastHit hit, RayReach, _obstacleMask, QueryTriggerInteraction.Ignore)
                    ? hit.distance : RayReach;

            PlaceTraits traits = GroundReading.Classify(_reach, InConcealment(spot), out int lane);
            if (traits != PlaceTraits.None) _memory.Visit(spot, traits, GroundReading.Direction(lane));
            SightConcealment(eye);
        }

        private static bool InConcealment(Vector3 spot)
        {
            Vector3 body = spot + Vector3.up * 0.5f;
            foreach (ConcealmentZone zone in ConcealmentZone.Active)
                if (zone != null && zone.Contains(body)) return true;
            return false;
        }

        // One concealment zone a look, in turn: if it is in view and close enough, it is known.
        private void SightConcealment(Vector3 eye)
        {
            IReadOnlyList<ConcealmentZone> zones = ConcealmentZone.Active;
            if (zones.Count == 0) return;
            ConcealmentZone zone = zones[_zoneCursor++ % zones.Count];
            if (zone == null) return;

            Vector3 centre = zone.GroundCenter;
            Vector3 target = centre + Vector3.up * 0.5f;
            Vector3 toZone = target - eye;
            if (toZone.sqrMagnitude > SightingRange * SightingRange) return;
            if (Vector3.Angle(_ai.transform.forward, toZone) > _ai.Data.SightAngle * 0.5f) return;
            if (Physics.Linecast(eye, target, _obstacleMask, QueryTriggerInteraction.Ignore)) return;
            _memory.Sight(centre, PlaceTraits.Hidden);
        }

        private void ShareWithKinNearby(float time)
        {
            Vector3 self = _ai.transform.position;
            foreach (EnemyAI other in EnemyRoster.All)
            {
                if (other == _ai || !EnemyRoster.IsKin(_ai, other)) continue;
                if ((other.transform.position - self).sqrMagnitude > ShareRange * ShareRange) continue;
                _memory.ShareWith(other.Ground.Memory, time);
            }
        }
    }
}
