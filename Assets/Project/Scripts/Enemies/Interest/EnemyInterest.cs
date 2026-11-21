using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Enemies
{
    // An enemy's pull towards the things in the world its faction likes (PointOfInterest). When
    // Patrol is choosing where to go it may ask for one: it picks among the points in reach that
    // it likes, has room, it has not used lately and nothing bad has happened near, the likelier
    // the more it wants it and the nearer it is. It then holds a slot, walks there, stays a while
    // facing it — taking what the kind gives (health, shield) — and goes back to patrolling.
    // Anything that interrupts it lets the slot go.
    internal sealed class EnemyInterest
    {
        private readonly EnemyAI _ai;
        private readonly Dictionary<PointOfInterest, float> _backAt = new();
        private readonly List<GroundPlace> _places = new();

        private PointOfInterest _claimed;
        private bool  _using;
        private float _end;

        public EnemyInterest(EnemyAI ai) => _ai = ai;

        public bool IsInteracting => _using;

        // The point it holds a slot at (heading there or using it), if any.
        internal PointOfInterest Current => _claimed;

        // Whether this is a time it would rather go and see something than carry on as usual.
        public bool WantsVisit() => PointOfInterest.Active.Count > 0 && Random.value < _ai.Doctrine.Patrol.InterestChance;

        // Picks one and holds a slot there; false when nothing is worth going to.
        public bool TryChoose(float time, out Vector3 spot)
        {
            spot = default;
            Abort(time);

            Vector3 self = _ai.transform.position;
            _ai.Ground.CopyPlaces(_places);
            var choice = new WeightedChoice<PointOfInterest>();
            foreach (PointOfInterest point in PointOfInterest.Active)
                choice.Offer(point, WeightOf(point, self, time));
            if (!choice.HasPick || !choice.Picked.TryClaim(_ai, out spot)) return false;

            _claimed = choice.Picked;
            return true;
        }

        // It is there: it stays for as long as the kind says.
        public void Begin(float time)
        {
            if (_claimed == null) return;
            InterestKind kind = _claimed.Kind;
            _using = true;
            _end   = time + Mathf.Max(0.5f, kind.Duration.EvaluateClamped());
            _ai.Agent.isStopped = true;
            if (kind.Bark != BarkKind.None) _ai.Bark(kind.Bark);
            _claimed.NotifyStarted(_ai);
        }

        // True while it is still there.
        public bool Tick(float deltaTime, float time)
        {
            if (!_using) return false;
            if (_claimed == null || time >= _end)
            {
                Abort(time);
                return false;
            }

            _ai.Agent.isStopped = true;
            _ai.Motion.FaceTowards(_claimed.Center, deltaTime);

            InterestKind kind = _claimed.Kind;
            if (kind.HealthPerSecond > 0f) _ai.Health.Heal(kind.HealthPerSecond * deltaTime);
            if (kind.ShieldPerSecond > 0f) _ai.Health.RestoreShield(kind.ShieldPerSecond * deltaTime);
            return true;
        }

        // It is done, or something else took over: lets the slot go. It does not go back to the
        // same point for a while if it had got there.
        public void Abort(float time)
        {
            if (_claimed is null) return;   // not Unity's ==: a point destroyed under it still has to be let go

            PointOfInterest point = _claimed;
            _claimed = null;
            if (point == null) { _using = false; return; }   // the object is gone

            point.Release(_ai);
            if (!_using) return;

            _using = false;
            _backAt[point] = time + point.Kind.Cooldown;
            if (_ai.Agent.isActiveAndEnabled && _ai.Agent.isOnNavMesh) _ai.Agent.isStopped = false;
            point.NotifyEnded(_ai);
        }

        public void Forget()
        {
            Abort(Time.time);
            _backAt.Clear();
            _using = false;
            _claimed = null;
        }

        private float WeightOf(PointOfInterest point, Vector3 self, float time)
        {
            InterestKind kind = point.Kind;
            if (kind == null || !point.HasRoom) return 0f;
            if (_backAt.TryGetValue(point, out float back) && time < back) return 0f;

            Vector3 centre = point.Center;
            if (!_ai.IsWithinHome(centre, 0f)) return 0f;

            Vector3 offset = centre - self;
            offset.y = 0f;
            return InterestRules.Weight(kind.AppealTo(_ai.Health.Team, _ai.Data), offset.magnitude, kind.Reach, PatrolScoring.DangerNear(centre, _places));
        }
    }
}
