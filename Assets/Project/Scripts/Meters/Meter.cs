using System;
using UnityEngine;

namespace CGD.Meters
{
    // Runtime value of one resource. Plain C# so the same model serves a character's
    // stamina, a weapon's battery or a shield, and can be tested without a scene.
    //
    // Spending is all-or-nothing (TrySpend); Drain is for continuous or forced costs
    // (sprinting, suffocating) and takes whatever is left.
    public class Meter
    {
        private readonly float _regenRate;
        private readonly float _regenDelay;
        private readonly float _startRatio;
        private readonly float _exhaustionRecovery;

        private float _current;
        private float _max;
        private float _regenTimer;

        public MeterDefinition Definition { get; }

        public float Current     => _current;
        public float Max         => _max;
        public float Ratio       => _max > 0f ? _current / _max : 0f;
        public bool  IsEmpty     => _current <= 0f;
        public bool  IsFull      => _current >= _max;
        // Emptied and not yet back above the definition's recovery threshold.
        public bool  IsExhausted { get; private set; }

        // Any change to Current or Max.
        public event Action Changed;
        // The moment the meter hits 0.
        public event Action Depleted;

        public Meter(MeterDefinition definition) : this(definition.Settings) => Definition = definition;

        public Meter(in MeterSettings settings)
        {
            _max                = settings.Max;
            _regenRate          = settings.RegenRate;
            _regenDelay         = settings.RegenDelay;
            _startRatio         = settings.StartRatio;
            _exhaustionRecovery = settings.ExhaustionRecovery;
            Reset();
        }

        public bool CanSpend(float amount) => amount <= 0f || (!IsExhausted && _current >= amount);

        public bool TrySpend(float amount)
        {
            if (!CanSpend(amount)) return false;
            if (amount > 0f) Drain(amount);
            return true;
        }

        // Removes up to amount, however much is left.
        public void Drain(float amount)
        {
            if (amount <= 0f || _current <= 0f) return;

            if (_regenRate > 0f) SuppressRegen();
            SetCurrent(_current - amount);
        }

        public void Restore(float amount)
        {
            if (amount <= 0f || IsFull) return;

            if (_regenRate < 0f) SuppressRegen();
            SetCurrent(_current + amount);
        }

        public void Fill()  => SetCurrent(_max);
        public void Empty() => SetCurrent(0f);

        // Back to the starting value with no pending delay or lockout (respawn).
        public void Reset()
        {
            _regenTimer = 0f;
            SetCurrent(_max * _startRatio);
            IsExhausted = false;
        }

        // Upgrades/debuffs. keepRatio scales the current value along with the capacity.
        public void SetMax(float max, bool keepRatio = true)
        {
            float ratio = Ratio;
            _max     = Mathf.Max(0f, max);
            _current = Mathf.Clamp(keepRatio ? _max * ratio : _current, 0f, _max);
            Changed?.Invoke();
        }

        // Restarts the regen delay, e.g. a shield that stops recharging whenever it's hit.
        public void SuppressRegen() => _regenTimer = _regenDelay;

        // Advances regen/decay. Returns true if the value changed.
        public bool Tick(float deltaTime)
        {
            if (_regenRate == 0f) return false;
            if (_regenRate > 0f ? IsFull : IsEmpty) return false;

            if (_regenTimer > 0f)
            {
                _regenTimer -= deltaTime;
                return false;
            }

            SetCurrent(_current + _regenRate * deltaTime);
            return true;
        }

        private void SetCurrent(float value)
        {
            float previous = _current;
            _current = Mathf.Clamp(value, 0f, _max);

            if (_current <= 0f && previous > 0f)
            {
                if (_exhaustionRecovery > 0f) IsExhausted = true;
                Depleted?.Invoke();
            }
            else if (IsExhausted && Ratio >= _exhaustionRecovery)
            {
                IsExhausted = false;
            }

            if (!Mathf.Approximately(previous, _current)) Changed?.Invoke();
        }
    }
}
