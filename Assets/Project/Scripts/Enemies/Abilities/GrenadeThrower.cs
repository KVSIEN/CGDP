using UnityEngine;
using CGD.Audio;
using CGD.Combat;
using CGD.Core;
using CGD.Weapons;

namespace CGD.Enemies
{
    // An enemy lobbing a grenade (any GrenadeData) onto where it believes its target is —
    // to flush it out of cover it can't see past. A short wind-up facing the spot, then the
    // throw. It aims for where the grenade will go off, not where it first lands: it plays the
    // flight out (GrenadeFlight — bounces, the roll to a stop, the fuse) and pulls its aim back
    // by however far the grenade would carry on, trying a steeper lob (which rolls less) when
    // that isn't close enough. A target it sees moving is led to where it will be when the
    // grenade lands — never further than the blast reaches, nor through a wall. If no throw
    // within the grenade's throw speeds goes off near the spot, it doesn't throw. Grenades
    // never hurt the thrower's own side.
    internal sealed class GrenadeThrower
    {
        private const float WindUp     = 0.5f;
        private const float HandHeight = 1.6f;
        private const float HandReach  = 0.4f;
        // Arcs it tries, flattest first: a steeper lob comes down more steeply and rolls less.
        private static readonly float[] ArcAngles = { 40f, 55f };
        // Aim corrections per arc for the bounce and roll after landing.
        private const int   AimPasses  = 3;
        // A throw going off this close to the spot is taken at once.
        private const float GoodEnough = 0.75f;
        // A throw going off further than this from the spot isn't worth making.
        private const float LandingSlack = 3f;
        // A led spot stops this far short of a wall in the target's way.
        private const float WallMargin = 0.5f;

        private readonly EnemyAI      _ai;
        private readonly DamageSource _source;
        private readonly LayerMask    _obstacleMask;

        private GrenadeData _pending;
        private Vector3     _aimAt;
        private Vector3     _hand;
        private Vector3     _velocity;
        private float       _throwAt;

        public GrenadeThrower(EnemyAI ai, DamageSource source, LayerMask obstacleMask)
        {
            _ai     = ai;
            _source = source;
            _obstacleMask = obstacleMask;
        }

        public bool IsThrowing => _pending != null;

        // `targetVelocity` is how the target was seen moving (zero if unseen or still).
        public bool TryBegin(GrenadeData grenade, Vector3 target, Vector3 targetVelocity)
        {
            if (IsThrowing || grenade == null || grenade.GrenadePrefab == null) return false;

            Vector3 aim = Lead(grenade, target, targetVelocity);
            if (!TryPlan(grenade, aim, out _hand, out _velocity)) return false;

            _pending = grenade;
            _aimAt   = aim;
            _throwAt = Time.time + WindUp;
            return true;
        }

        // True once thrown (or given up).
        public bool Tick(float deltaTime)
        {
            if (_pending == null) return true;

            _ai.Agent.isStopped = true;
            _ai.Motion.FaceTowards(_aimAt, deltaTime);
            if (Time.time < _throwAt) return false;

            if (IsHandClear(_hand)) Throw(_pending);
            _pending = null;
            return true;
        }

        public void Cancel() => _pending = null;

        private void Throw(GrenadeData data)
        {
            GameObject grenade = PrefabPool.Spawn(data.GrenadePrefab, _hand, Quaternion.identity);
            if (grenade.TryGetComponent(out Rigidbody body))
            {
                body.linearVelocity  = _velocity;
                body.angularVelocity = Vector3.zero;
            }
            if (grenade.TryGetComponent(out Collider collider) && _ai.TryGetComponent(out Collider own))
                Physics.IgnoreCollision(collider, own);
            if (grenade.TryGetComponent(out Grenade fuse)) fuse.Init(data, _source);

            data.ThrowSound.TryPlay(_ai.transform.position);
            _ai.NotifyThrew();
        }

        // Where the target will be once the grenade lands: the wind-up plus the flight.
        private Vector3 Lead(GrenadeData data, Vector3 target, Vector3 targetVelocity)
        {
            if (targetVelocity.sqrMagnitude < 0.01f) return target;

            Vector3 hand   = HandToward(target);
            float   flight = ThrowArc.TrySolve(hand, target, ArcAngles[0], data.MinThrowSpeed, data.MaxThrowSpeed,
                                               -data.Gravity.y, out Vector3 velocity)
                ? ThrowArc.FlightTime(hand, target, velocity)
                : 1f;
            Vector3 led = ThrowArc.Lead(target, targetVelocity, WindUp + flight, data.ExplosionRadius);
            return Reachable(target, led);
        }

        // The target can't walk through walls: the lead stops short of anything in its way.
        private Vector3 Reachable(Vector3 from, Vector3 to)
        {
            Vector3 chest = Vector3.up;
            if (!Physics.Linecast(from + chest, to + chest, out RaycastHit hit, _obstacleMask, QueryTriggerInteraction.Ignore))
                return to;

            Vector3 way = to - from;
            way.y = 0f;
            return from + way.normalized * Mathf.Max(0f, hit.distance - WallMargin);
        }

        // The throw that goes off nearest the spot. Each pass plays the flight out and moves the
        // aim back by how far it missed, so a grenade that bounces on or rolls down a slope is
        // thrown short of the spot and ends up on it. This also rules out a lob that hits a wall
        // in front of the thrower and drops at its feet.
        private bool TryPlan(GrenadeData data, Vector3 target, out Vector3 hand, out Vector3 velocity)
        {
            hand     = HandToward(target);
            velocity = default;
            if (!IsHandClear(hand)) return false;

            float gravity  = -data.Gravity.y;
            float bestMiss = float.MaxValue;
            foreach (float angle in ArcAngles)
            {
                Vector3 aim = target;
                for (int pass = 0; pass < AimPasses; pass++)
                {
                    if (!ThrowArc.TrySolve(hand, aim, angle, data.MinThrowSpeed, data.MaxThrowSpeed, gravity, out Vector3 throwVelocity))
                        break;

                    Vector3 end  = GrenadeFlight.Detonation(hand, throwVelocity, data, _obstacleMask, out _);
                    float   miss = Vector3.Distance(end, target);
                    if (miss < bestMiss)
                    {
                        bestMiss = miss;
                        velocity = throwVelocity;
                    }
                    if (miss <= GoodEnough) return true;

                    Vector3 overshoot = end - target;
                    overshoot.y = 0f;
                    aim -= overshoot;
                }
            }
            return bestMiss <= LandingSlack;
        }

        private bool IsHandClear(Vector3 hand) =>
            !Physics.Linecast(_ai.transform.position + Vector3.up, hand, _obstacleMask, QueryTriggerInteraction.Ignore);

        // Its hand once it has turned to throw at the spot.
        private Vector3 HandToward(Vector3 target)
        {
            Vector3 facing = target - _ai.transform.position;
            facing.y = 0f;
            facing = facing.sqrMagnitude > 0.0001f ? facing.normalized : _ai.transform.forward;
            return _ai.transform.position + Vector3.up * HandHeight + facing * HandReach;
        }
    }
}
