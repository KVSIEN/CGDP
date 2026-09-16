using System.Collections.Generic;
using UnityEngine;

namespace CGD.Combat
{
    // Reusable runtime driver for status effects (Bleed, Poison, Fire, Lightning, Ice, ...).
    // Tracks stacks/duration per effect asset and calls into each effect's own Tick()
    // so damage-over-time effects resolve through the same TakeDamage(DamageInfo)
    // pipeline as direct hits. All effects are cleared when the character dies.
    public class StatusEffectController : MonoBehaviour
    {
        private class ActiveEffect
        {
            public int   Stacks;
            public float RemainingDuration;
            public float TickTimer;
            public float Magnitude; // raw damage of the hit that (most recently) applied this effect
        }

        private IDamageable   _target;
        private HealthManager _health;
        private readonly Dictionary<StatusEffect, ActiveEffect> _active = new();
        private readonly List<StatusEffect> _expiredBuffer = new();

        // A tick can kill the target, which clears effects mid-iteration; defer that clear.
        private bool _ticking;
        private bool _clearRequested;

        public Team Team => _health != null ? _health.Team : Team.None;

        private void Awake()
        {
            _target = GetComponent<IDamageable>();
            _health = _target as HealthManager;
            if (_health != null) _health.OnDeath += Clear;
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnDeath -= Clear;
        }

        private void Update()
        {
            if (_active.Count == 0) return;

            float dt = Time.deltaTime;
            _ticking = true;

            foreach (var pair in _active)
            {
                StatusEffect effect = pair.Key;
                ActiveEffect active = pair.Value;

                active.RemainingDuration -= dt;
                active.TickTimer         -= dt;

                if (active.TickTimer <= 0f)
                {
                    active.TickTimer += effect.TickInterval;
                    effect.Tick(_target, active.Stacks, active.Magnitude);
                }

                if (active.RemainingDuration <= 0f)
                {
                    if (effect.DecayOneStackAtATime && active.Stacks > 1)
                    {
                        active.Stacks--;
                        active.RemainingDuration = effect.Duration;
                    }
                    else
                    {
                        _expiredBuffer.Add(effect);
                    }
                }
            }

            _ticking = false;
            if (_clearRequested)
            {
                Clear();
                return;
            }

            if (_expiredBuffer.Count == 0) return;

            foreach (StatusEffect effect in _expiredBuffer)
            {
                _active.Remove(effect);
                effect.OnRemoved(_target);
            }
            _expiredBuffer.Clear();
        }

        // Entry point for hits: lets the effect decide what applying it means.
        public void Apply(StatusEffect effect, in DamageInfo hit) => effect.Apply(this, hit);

        // Adds (or refreshes) a stack. Stack count clamps at effect.MaxStacks;
        // reapplying always resets the remaining duration back to effect.Duration and
        // updates magnitude to the new hit's value.
        public void AddStack(StatusEffect effect, float magnitude)
        {
            if (_active.TryGetValue(effect, out ActiveEffect active))
            {
                active.Stacks            = Mathf.Min(active.Stacks + 1, effect.MaxStacks);
                active.RemainingDuration = effect.Duration;
                active.Magnitude         = magnitude;
            }
            else
            {
                _active[effect] = new ActiveEffect
                {
                    Stacks            = 1,
                    RemainingDuration = effect.Duration,
                    TickTimer         = effect.TickInterval,
                    Magnitude         = magnitude,
                };
            }
        }

        // Current stack count for `effect`, or 0 if it isn't active.
        public int GetStacks(StatusEffect effect)
        {
            return _active.TryGetValue(effect, out ActiveEffect active) ? active.Stacks : 0;
        }

        public void Clear()
        {
            if (_ticking)
            {
                _clearRequested = true;
                return;
            }

            _clearRequested = false;
            foreach (StatusEffect effect in _active.Keys)
                effect.OnRemoved(_target);
            _active.Clear();
            _expiredBuffer.Clear();
        }
    }
}
