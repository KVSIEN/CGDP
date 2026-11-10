using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Stealth;

namespace CGD.Enemies
{
    // What an enemy thinks about where its target went once it has lost sight of it: not a single
    // guess but a likelihood over the cover spots and bushes round the last sighting (BeliefModel),
    // spreading outward the longer it has been, leaning the way the target was heading. Spots it has
    // looked at and found empty drop out — a bush only once it has walked up to it, since nobody can
    // see into one from afar — so a search works through the likely places in order instead of
    // wandering. Confidence fades over time; a new sighting collapses everything back to a point.
    // Sounds and calls for help can seed it too (Hint). Never reads the target's real position
    // except while it genuinely senses it.
    internal sealed class EnemyBelief
    {
        private const float Lifetime       = 30f;    // seconds until the belief has faded away
        private const float ActiveAbove    = 0.1f;
        private const float HintConfidence = 0.6f;
        private const float MinSpeed       = 1.5f;   // it is assumed to keep moving at least this fast
        private const float SpotRadius     = 45f;
        private const float RefreshEvery   = 0.4f;
        private const float CheckRange     = 20f;
        private const float ArrivedWithin  = 2f;
        private const float CheckedFactor  = 0.1f;
        private const int   MaxChecks      = 6;
        private const float MinWeight      = 0.04f;
        private const float BodyHeight     = 0.5f;   // where a hiding place is tested for concealment
        private const float VantageRadius  = 16f;
        private const int   VantageSpots   = 6;

        private readonly EnemyAI   _ai;
        private readonly LayerMask _obstacleMask;
        private readonly List<BeliefSpot> _spots = new();
        private readonly List<int> _indices = new();
        private readonly List<BeliefSpot> _top = new();

        private HealthManager _target;
        private bool    _hasSighting;
        private bool    _lost;
        private Vector3 _lastSeen;
        private Vector3 _heading;
        private float   _speed;
        private float   _lostAt;
        private float   _peak = 1f;
        private float   _nextRefresh;
        private int     _cursor;

        public EnemyBelief(EnemyAI ai, LayerMask obstacleMask)
        {
            _ai           = ai;
            _obstacleMask = obstacleMask;
        }

        public bool    IsActive    => _lost && Confidence > ActiveAbove && _spots.Count > 0;
        public float   Confidence  => _lost ? _peak * Mathf.Clamp01(1f - (Time.time - _lostAt) / Lifetime) : 0f;
        public Vector3 LastSeen    => _lastSeen;
        public Vector3 Heading     => _heading;
        public IReadOnlyList<BeliefSpot> Spots => _spots;

        public void Clear()
        {
            _target      = null;
            _hasSighting = false;
            _lost        = false;
            _spots.Clear();
        }

        public void Tick(float now)
        {
            EnemyPerception perception = _ai.Perception;
            if (!ReferenceEquals(_target, null) && EnemyPerception.IsGone(_target))
            {
                Clear();
                return;
            }

            if (perception.IsSensing && perception.Target != null)
            {
                Observe(perception);
                return;
            }

            if (_hasSighting && !_lost) Lose(now, 1f);
            if (!_lost || now < _nextRefresh) return;

            _nextRefresh = now + RefreshEvery;
            Refresh(now);
        }

        // A sound or a call for help pointing somewhere: a weaker lead than a sighting, and never
        // one that replaces a fresher belief.
        public void Hint(Vector3 position)
        {
            if (_ai.Perception.IsSensing || (_lost && Confidence >= HintConfidence)) return;

            _lastSeen    = position;
            _heading     = Vector3.zero;
            _speed       = MinSpeed;
            _hasSighting = true;
            Lose(Time.time, HintConfidence);
        }

        // --- Using it -----------------------------------------------------------------

        // The likeliest unchecked spot, discounted by how far it is: a methodical sweep.
        public bool TrySweepSpot(out BeliefSpot spot)
        {
            spot = default;
            if (!IsActive) return false;

            Vector3 self = _ai.transform.position;
            float best = MinWeight;
            foreach (BeliefSpot candidate in _spots)
            {
                if (candidate.Checked) continue;
                float score = candidate.Weight / (1f + Vector3.Distance(candidate.Position, self) / 25f);
                if (score <= best) continue;
                best = score;
                spot = candidate;
            }
            return best > MinWeight;
        }

        // Any unchecked spot, chosen at random in proportion to its likelihood: unpredictable
        // from outside, yet never aimless.
        public bool TryWeightedSpot(out BeliefSpot spot)
        {
            spot = default;
            if (!IsActive) return false;

            float total = 0f;
            foreach (BeliefSpot candidate in _spots)
                if (!candidate.Checked && candidate.Weight >= MinWeight) total += candidate.Weight;
            if (total <= 0f) return false;

            float roll = Random.value * total;
            foreach (BeliefSpot candidate in _spots)
            {
                if (candidate.Checked || candidate.Weight < MinWeight) continue;
                spot = candidate;
                roll -= candidate.Weight;
                if (roll <= 0f) break;
            }
            return true;
        }

        // A cover spot near it from which it can see the most likely places at once, so it can hold
        // there watching them, and the point to face: the likelihood-weighted middle of what it sees.
        // Bushes don't count: watching one from afar shows nothing.
        public bool TryVantage(out Vector3 position, out Vector3 watch)
        {
            position = watch = default;
            CoverMap map = CoverMap.Active;
            if (map == null || !IsActive) return false;

            _top.Clear();
            foreach (BeliefSpot candidate in _spots)
                if (!candidate.Checked && !candidate.Concealed && candidate.Weight >= MinWeight) _top.Add(candidate);
            if (_top.Count == 0) return false;
            _top.Sort((a, b) => b.Weight.CompareTo(a.Weight));
            if (_top.Count > VantageSpots) _top.RemoveRange(VantageSpots, _top.Count - VantageSpots);

            Vector3 self = _ai.transform.position;
            float sightRange = _ai.Data.SightRange;
            float best = float.MinValue;
            map.Query(self, VantageRadius, _indices);
            foreach (int index in _indices)
            {
                if (map.IsTakenByOther(index, _ai)) continue;

                CoverPoint cover = map[index];
                Vector3 eye = cover.Position + Vector3.up * EnemyEyes.Height;
                float seen = 0f;
                Vector3 centre = Vector3.zero;
                foreach (BeliefSpot spot in _top)
                {
                    if ((spot.Position - eye).sqrMagnitude > sightRange * sightRange) continue;
                    if (!IsClearView(eye, spot.Position + Vector3.up)) continue;
                    seen   += spot.Weight;
                    centre += spot.Position * spot.Weight;
                }
                if (seen <= 0f) continue;

                float score = seen - 0.02f * Vector3.Distance(cover.Position, self) + (cover.Faces(_lastSeen, 0.35f) ? 0.15f * seen : 0f);
                if (score <= best) continue;
                best     = score;
                position = cover.Position;
                watch    = centre / seen;
            }
            return best > float.MinValue;
        }

        public void MarkChecked(int slot)
        {
            if (slot < 0 || slot >= _spots.Count) return;
            BeliefSpot spot = _spots[slot];
            spot.Checked = true;
            spot.Weight *= CheckedFactor;
            _spots[slot] = spot;
        }

        // --- Updating -----------------------------------------------------------------

        private void Observe(EnemyPerception perception)
        {
            if (perception.Target != _target)
            {
                Clear();
                _target = perception.Target;
            }

            Vector3 velocity = perception.TargetVelocity;
            velocity.y = 0f;
            _speed = velocity.magnitude;
            if (_speed > 0.5f)
            {
                _heading = velocity / _speed;
            }
            else
            {
                Vector3 facing = perception.TargetFacing;
                facing.y = 0f;
                _heading = perception.TargetMotion == TargetMotion.Forward && facing.sqrMagnitude > 0.01f ? facing.normalized : Vector3.zero;
            }

            _lastSeen    = perception.Target.transform.position;
            _hasSighting = true;
            if (_lost || _spots.Count > 0)
            {
                _lost = false;
                _spots.Clear();
            }
        }

        private void Lose(float now, float peak)
        {
            _lost    = true;
            _lostAt  = now;
            _peak    = peak;
            _cursor  = 0;
            _spots.Clear();

            CoverMap map = CoverMap.Active;
            if (map != null)
            {
                map.Query(_lastSeen, SpotRadius, _indices);
                foreach (int index in _indices) AddSpot(map[index].Position);
            }
            AddConcealment();
            if (_spots.Count > 0) Refresh(now);
        }

        // Bushes and smoke near the last sighting are hiding places too, cover or no cover.
        private void AddConcealment()
        {
            IReadOnlyList<ConcealmentZone> zones = ConcealmentZone.Active;
            for (int i = 0; i < zones.Count; i++)
            {
                Vector3 centre = zones[i].GroundCenter;
                if ((centre - _lastSeen).sqrMagnitude <= SpotRadius * SpotRadius) AddSpot(centre);
            }
        }

        private void AddSpot(Vector3 position) => _spots.Add(new BeliefSpot
        {
            Slot      = _spots.Count,
            Position  = position,
            Concealed = ConcealmentZone.AnyContains(position + Vector3.up * BodyHeight, crouching: true),
        });

        // Nothing solid and no smoke in between.
        private bool IsClearView(Vector3 eye, Vector3 point) =>
            !Physics.Linecast(eye, point, _obstacleMask, QueryTriggerInteraction.Ignore) &&
            !ConcealmentZone.BlocksLine(eye, point);

        // Re-weighs every spot for the time that has passed, and ticks off the ones it can see are empty.
        private void Refresh(float now)
        {
            float elapsed = now - _lostAt;
            float speed   = Mathf.Max(_speed, MinSpeed);
            for (int i = 0; i < _spots.Count; i++)
            {
                BeliefSpot spot = _spots[i];
                spot.Weight = BeliefModel.Weight(spot.Position, _lastSeen, _heading, speed, elapsed) * (spot.Checked ? CheckedFactor : 1f);
                _spots[i] = spot;
            }
            CheckWhatItCanSee();
        }

        private void CheckWhatItCanSee()
        {
            if (_spots.Count == 0) return;

            Vector3 self    = _ai.transform.position;
            Vector3 eye     = self + Vector3.up * EnemyEyes.Height;
            Vector3 forward = _ai.transform.forward;
            float minDot    = Mathf.Cos(_ai.Perception.SightAngle * 0.5f * Mathf.Deg2Rad);
            int rays = 0;

            for (int n = 0; n < _spots.Count; n++)
            {
                int i = (_cursor + n) % _spots.Count;
                BeliefSpot spot = _spots[i];
                if (spot.Checked) continue;

                Vector3 to = spot.Position - self;
                to.y = 0f;
                float distance = to.magnitude;
                bool arrived = distance <= ArrivedWithin;
                if (!arrived)
                {
                    // Someone in a bush can't be seen from out here: it has to be walked up to.
                    if (spot.Concealed || distance > CheckRange || Vector3.Dot(forward, to / distance) < minDot) continue;
                    if (rays++ >= MaxChecks) { _cursor = i; return; }
                    if (!IsClearView(eye, spot.Position + Vector3.up)) continue;
                }

                spot.Checked = true;
                spot.Weight *= CheckedFactor;
                _spots[i] = spot;
            }
        }
    }
}
