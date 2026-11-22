using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Combat
{
    // Movement slow, stun and knockback state for any character that moves. Status effects
    // (e.g. Ice) and abilities write to it; PlayerMovement and EnemyAI read it. Cleared on death.
    // Stuns only ever extend (a short stun never cuts a long one short), and each source
    // keeps its own slow — the strongest one applies, and ending one leaves the others.
    [DisallowMultipleComponent]
    public class Stunnable : MonoBehaviour
    {
        private readonly Dictionary<object, float> _slows = new();
        private readonly HashSet<object> _holds = new();
        private readonly Dictionary<object, FadingSlow> _fading = new();
        private readonly List<object> _fadeKeys = new();
        private float _speedMultiplier = 1f;
        private CooldownTimer _stunTimer;
        private HealthManager _health;

        private Vector3 _pushVelocity;
        private float   _pushDuration;
        private float   _pushLeft;

        // 1 = full speed; the strongest active slow otherwise.
        public float SpeedMultiplier => _speedMultiplier;

        // Being shoved: the mover carries the character along KnockbackVelocity instead of walking.
        public bool IsKnockedBack => _pushLeft > 0f;

        // Horizontal, easing out to nothing over the push so it lands rather than stops dead.
        public Vector3 KnockbackVelocity => IsKnockedBack ? _pushVelocity * (_pushLeft / _pushDuration) : Vector3.zero;

        // Shoves the character along velocity's horizontal part for duration seconds, stunned
        // throughout (no walking or acting against it). A new push replaces one under way.
        public void Knockback(Vector3 velocity, float duration)
        {
            if (duration <= 0f) return;

            velocity.y    = 0f;
            _pushVelocity = velocity;
            _pushDuration = _pushLeft = duration;
            ApplyStun(duration);
        }

        // Shoves a hostile target straight away from `from` (a blast's centre, the caster), if it
        // can be shoved and is one `source` may hurt. Standing right on `from`, straight backwards.
        public static void PushAway(HealthManager target, Vector3 from, DamageSource source, float speed, float duration)
        {
            if (speed <= 0f || target == null || target.IsDead || !target.CanBeDamagedBy(source)) return;
            if (!target.TryGetComponent(out Stunnable stunnable)) return;

            Vector3 away = target.transform.position - from;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f) away = -target.transform.forward;
            stunnable.Knockback(away.normalized * speed, duration);
        }

        // Stunned by a timer, or for as long as something holds the character in place (a grab
        // that lasts until the player breaks free).
        public bool IsStunned => !_stunTimer.IsReady || _holds.Count > 0;

        public void AddHold(object source) => _holds.Add(source);

        public void RemoveHold(object source) => _holds.Remove(source);

        public void ApplyStun(float duration)
        {
            if (duration > _stunTimer.Remaining) _stunTimer.Start(duration);
        }

        // multiplier: 0 = stopped, 1 = no slow. Calling again with the same source replaces its slow.
        public void SetSlow(object source, float multiplier)
        {
            _slows[source] = Mathf.Clamp01(multiplier);
            Recalculate();
        }

        public void ClearSlow(object source)
        {
            if (_slows.Remove(source)) Recalculate();
        }

        // A daze: slowed to `multiplier` at once, easing back to full speed over `duration` seconds.
        // Calling again with the same source starts it over.
        public void SetFadingSlow(object source, float multiplier, float duration)
        {
            if (duration <= 0f) return;

            _fading[source] = new FadingSlow(Mathf.Clamp01(multiplier), duration);
            Recalculate();
        }

        public void Clear()
        {
            _slows.Clear();
            _fading.Clear();
            _holds.Clear();
            _speedMultiplier = 1f;
            _pushLeft = 0f;
            _stunTimer.Reset();
        }

        private void Recalculate()
        {
            _speedMultiplier = 1f;
            foreach (float multiplier in _slows.Values)
                _speedMultiplier = Mathf.Min(_speedMultiplier, multiplier);
            foreach (FadingSlow fading in _fading.Values)
                _speedMultiplier = Mathf.Min(_speedMultiplier, fading.Current);
        }

        private void TickFading(float deltaTime)
        {
            _fadeKeys.Clear();
            _fadeKeys.AddRange(_fading.Keys);
            foreach (object source in _fadeKeys)
            {
                FadingSlow fading = _fading[source].Advanced(deltaTime);
                if (fading.IsOver) _fading.Remove(source);
                else               _fading[source] = fading;
            }
            Recalculate();
        }

        private readonly struct FadingSlow
        {
            private readonly float _start;
            private readonly float _duration;
            private readonly float _elapsed;

            public FadingSlow(float start, float duration, float elapsed = 0f)
            {
                _start    = start;
                _duration = duration;
                _elapsed  = elapsed;
            }

            public bool  IsOver  => _elapsed >= _duration;
            public float Current => Mathf.Lerp(_start, 1f, _elapsed / _duration);

            public FadingSlow Advanced(float deltaTime) => new(_start, _duration, _elapsed + deltaTime);
        }

        private void Awake()
        {
            if (TryGetComponent(out _health)) _health.OnDeath += Clear;
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnDeath -= Clear;
        }

        private void Update()
        {
            _stunTimer.Tick(Time.deltaTime);
            if (_fading.Count > 0) TickFading(Time.deltaTime);
            if (_pushLeft > 0f) _pushLeft = Mathf.Max(0f, _pushLeft - Time.deltaTime);
        }
    }
}
