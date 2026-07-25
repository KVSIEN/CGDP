using System.Collections.Generic;
using UnityEngine;

// Reusable runtime driver for status effects (Bleed, Poison, Fire, Lightning, Ice, ...).
// Tracks stacks/duration per effect asset and calls into each effect's own Tick()
// so damage-over-time effects resolve through the same TakeDamage(DamageInfo)
// pipeline as direct hits.
public class StatusEffectController : MonoBehaviour
{
    private class ActiveEffect
    {
        public int   Stacks;
        public float RemainingDuration;
        public float TickTimer;
        public float Magnitude; // raw damage of the hit that (most recently) applied this effect
    }

    private IDamageable _target;
    private readonly Dictionary<StatusEffect, ActiveEffect> _active = new();
    private readonly List<StatusEffect> _expiredBuffer = new();

    private void Awake()
    {
        _target = GetComponent<IDamageable>();
    }

    private void Update()
    {
        if (_active.Count == 0) return;

        float dt = Time.deltaTime;
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
                _expiredBuffer.Add(effect);
        }

        if (_expiredBuffer.Count == 0) return;
        foreach (StatusEffect effect in _expiredBuffer)
            _active.Remove(effect);
        _expiredBuffer.Clear();
    }

    // Applies (or refreshes/stacks) an effect. Stack count clamps at effect.MaxStacks;
    // reapplying always resets the remaining duration back to effect.Duration and
    // updates magnitude to the new hit's value.
    public void ApplyEffect(StatusEffect effect, float magnitude = 0f)
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
}
