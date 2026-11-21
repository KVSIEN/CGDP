using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;

namespace CGD.Enemies
{
    // Whom an enemy has been pointed at rather than chose for itself, each until its time runs
    // out: the target its summoner sent it after, the hostiles a roar showed it, the killer of an
    // ally it saw fall, and the one it has sworn to get for that. EnemyPerception asks it on
    // every choice of target; the clock is the perception's.
    internal sealed class HostileDirectives
    {
        private HealthManager _ordered;
        private float         _orderedUntil = float.NegativeInfinity;

        private readonly List<HealthManager> _revealed = new();
        private float _revealedUntil = float.NegativeInfinity;

        private HealthManager _fallKiller;
        private Vector3       _fallSpot;
        private float         _fellAt = float.NegativeInfinity;

        private HealthManager _vendetta;
        private float         _vendettaUntil = float.NegativeInfinity;

        // Filled by the roar, then pruned by the perception, before Reveal.
        public List<HealthManager> Revealed => _revealed;

        public void Order(HealthManager target, float until)
        {
            _ordered      = target;
            _orderedUntil = until;
        }

        public void Reveal(float until) => _revealedUntil = until;

        // The summoner's target while the order stands, else the one a roar showed it to fight.
        public HealthManager Ordered(float clock, HealthManager current, Vector3 self) =>
            (clock < _orderedUntil && !EnemyPerception.IsGone(_ordered) ? _ordered : null) ?? RevealedFocus(clock, current, self);

        // Its current target if the roar showed it and it still stands, otherwise the nearest of those shown.
        public HealthManager RevealedFocus(float clock, HealthManager current, Vector3 self)
        {
            if (clock >= _revealedUntil) return null;

            HealthManager nearest = null;
            float nearestSqr = float.MaxValue;
            foreach (HealthManager hostile in _revealed)
            {
                if (EnemyPerception.IsGone(hostile)) continue;
                if (hostile == current) return hostile;

                float sqr = (hostile.transform.position - self).sqrMagnitude;
                if (sqr >= nearestSqr) continue;
                nearest = hostile;
                nearestSqr = sqr;
            }
            return nearest;
        }

        public void SawFall(HealthManager killer, Vector3 spot, float clock)
        {
            _fallKiller = killer;
            _fallSpot   = spot;
            _fellAt     = clock;
        }

        // An ally it saw or heard fall at most `within` seconds ago and within `radius`, killed by
        // a hostile it hasn't avenged yet.
        public bool HasUnavengedFall(float clock, float within, Vector3 self, float radius, out HealthManager killer)
        {
            killer = _fallKiller;
            if (EnemyPerception.IsGone(killer) || clock - _fellAt > within) return false;
            return (_fallSpot - self).sqrMagnitude <= radius * radius;
        }

        // Swears to get the last fall's killer until `until`; false when there is none to get.
        public bool TrySwear(float until, out HealthManager killer, out Vector3 fallSpot)
        {
            killer   = _fallKiller;
            fallSpot = _fallSpot;
            if (EnemyPerception.IsGone(killer)) return false;

            _vendetta      = killer;
            _vendettaUntil = until;
            _fallKiller    = null;
            return true;
        }

        public HealthManager Vendetta(float clock) =>
            clock < _vendettaUntil && !EnemyPerception.IsGone(_vendetta) ? _vendetta : null;

        public void Clear()
        {
            _ordered       = null;
            _orderedUntil  = float.NegativeInfinity;
            _revealed.Clear();
            _revealedUntil = float.NegativeInfinity;
            _fallKiller    = null;
            _fellAt        = float.NegativeInfinity;
            _vendetta      = null;
            _vendettaUntil = float.NegativeInfinity;
        }
    }
}
