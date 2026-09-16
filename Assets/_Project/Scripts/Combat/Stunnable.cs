using UnityEngine;
using CGD.Core;

namespace CGD.Combat
{
    // Movement slow and stun state for any character that moves. Status effects
    // (e.g. Ice) write to it; PlayerMovement and EnemyAI read it. Cleared on death.
    public class Stunnable : MonoBehaviour
    {
        private float _speedMultiplier = 1f;
        private CooldownTimer _stunTimer;
        private HealthManager _health;

        public float SpeedMultiplier
        {
            get => _speedMultiplier;
            set => _speedMultiplier = Mathf.Clamp01(value);
        }

        public bool IsStunned => !_stunTimer.IsReady;

        public void ApplyStun(float duration) => _stunTimer.Start(duration);

        public void Clear()
        {
            _speedMultiplier = 1f;
            _stunTimer.Reset();
        }

        private void Awake()
        {
            if (TryGetComponent(out _health)) _health.OnDeath += Clear;
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnDeath -= Clear;
        }

        private void Update() => _stunTimer.Tick(Time.deltaTime);
    }
}
