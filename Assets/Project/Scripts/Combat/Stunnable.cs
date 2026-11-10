using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Combat
{
    // Movement slow, stun and knockback state for any character that moves. Status effects
    // (e.g. Ice) and abilities write to it; PlayerMovement and EnemyAI read it. Cleared on death.
    // Stuns only ever extend (a short stun never cuts a long one short), and each source
    // keeps its own slow — the strongest one applies, and ending one leaves the others.
    public class Stunnable : MonoBehaviour
    {
        private readonly Dictionary<object, float> _slows = new();
        private readonly HashSet<object> _holds = new();
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

        public void Clear()
        {
            _slows.Clear();
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
            if (_pushLeft > 0f) _pushLeft = Mathf.Max(0f, _pushLeft - Time.deltaTime);
        }
    }
}
