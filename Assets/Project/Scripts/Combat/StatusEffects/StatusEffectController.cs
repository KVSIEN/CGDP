using System.Collections.Generic;
using UnityEngine;

namespace CGD.Combat
{
    // Reusable runtime driver for status effects (Bleed, Poison, Fire, Lightning, Ice, ...).
    // Tracks stacks/duration per effect asset and calls into each effect's own Tick()
    // so damage-over-time effects resolve through the same TakeDamage(DamageInfo)
    // pipeline as direct hits. Also the context effects act on: the character's
    // IDamageable and (optional) Stunnable. All effects are cleared on death.
    [DisallowMultipleComponent]
    public class StatusEffectController : MonoBehaviour
    {
        private class ActiveEffect
        {
            public int   Stacks;
            public float RemainingDuration;
            public float TickTimer;
            public float Magnitude; // raw damage of the hit that (most recently) applied this effect
            public DamageSource Instigator; // who (most recently) applied it: its ticks count for them
            public float Potency;  // how strong the source that (most recently) applied it makes it
            public float Duration; // how long that source makes it (or each stack) last
            public readonly List<float> StackTimers = new(); // Independent stacking only
        }

        [Tooltip("Effects this character ignores")]
        [SerializeField] private StatusEffect[] _immunities;

        private StatusEffect[] _extraImmunities;
        private HealthManager _health;
        private readonly Dictionary<StatusEffect, ActiveEffect> _active = new();
        private readonly List<StatusEffect> _expiredBuffer = new();

        // A tick can kill the target, which clears effects mid-iteration; defer that clear.
        private bool _ticking;
        private bool _clearRequested;

        public IDamageable Damageable { get; private set; }
        public Stunnable   Stunnable  { get; private set; }
        public Team        Team       => _health != null ? _health.Team : Team.None;

        private void Awake()
        {
            Damageable = GetComponent<IDamageable>();
            Stunnable  = GetComponent<Stunnable>();
            _health    = Damageable as HealthManager;
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

                active.TickTimer -= dt;
                if (active.TickTimer <= 0f)
                {
                    active.TickTimer += effect.TickInterval;
                    effect.Tick(this, active.Stacks, active.Magnitude);
                }

                if (TickDuration(effect, active, dt))
                    _expiredBuffer.Add(effect);
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
                if (_active.Remove(effect)) effect.OnRemoved(this);
            }
            _expiredBuffer.Clear();
        }

        // Advances the effect's timers; returns true once it should be removed.
        private static bool TickDuration(StatusEffect effect, ActiveEffect active, float dt)
        {
            if (effect.Stacking == StatusStacking.Independent)
            {
                var timers = active.StackTimers;
                for (int i = timers.Count - 1; i >= 0; i--)
                {
                    timers[i] -= dt;
                    if (timers[i] <= 0f) timers.RemoveAt(i);
                }
                active.Stacks = timers.Count;
                return timers.Count == 0;
            }

            active.RemainingDuration -= dt;
            if (active.RemainingDuration > 0f) return false;

            if (effect.Stacking == StatusStacking.Stack && effect.DecayOneStackAtATime && active.Stacks > 1)
            {
                active.Stacks--;
                active.RemainingDuration = active.Duration;
                return false;
            }
            return true;
        }

        public bool IsImmuneTo(StatusEffect effect) =>
            (_immunities != null && System.Array.IndexOf(_immunities, effect) >= 0) ||
            (_extraImmunities != null && System.Array.IndexOf(_extraImmunities, effect) >= 0);

        // Immunities its character's data gives it (a TECH machine doesn't bleed), on top of the
        // ones set on this component.
        public void SetExtraImmunities(StatusEffect[] immunities) => _extraImmunities = immunities;

        // Entry point for hits: lets the effect decide what applying it means. The dose is how
        // strong and long the source makes it (the asset as tuned when left out).
        public void Apply(StatusEffect effect, in DamageInfo hit) => Apply(effect, hit, StatusDose.Full);

        public void Apply(StatusEffect effect, in DamageInfo hit, StatusDose dose)
        {
            if (!IsImmuneTo(effect)) effect.Apply(this, hit, dose);
        }

        // Adds (or refreshes) a stack according to the effect's Stacking mode and updates
        // magnitude, instigator and dose to the new hit's: the latest source sets the strength.
        public void AddStack(StatusEffect effect, float magnitude, DamageSource instigator, StatusDose dose)
        {
            if (IsImmuneTo(effect)) return;

            if (!_active.TryGetValue(effect, out ActiveEffect active))
            {
                active = new ActiveEffect { TickTimer = effect.TickInterval };
                _active[effect] = active;
            }

            active.Magnitude         = magnitude;
            active.Instigator        = instigator;
            active.Potency           = dose.Potency;
            active.Duration          = dose.DurationOf(effect);
            active.RemainingDuration = active.Duration;

            switch (effect.Stacking)
            {
                case StatusStacking.Refresh:
                    active.Stacks = 1;
                    break;

                case StatusStacking.Stack:
                    active.Stacks = Mathf.Min(active.Stacks + 1, Mathf.Max(1, effect.MaxStacks));
                    break;

                case StatusStacking.Independent:
                    AddIndependentStack(active.StackTimers, effect, active.Duration);
                    active.Stacks = active.StackTimers.Count;
                    break;
            }
        }

        private static void AddIndependentStack(List<float> timers, StatusEffect effect, float duration)
        {
            if (timers.Count < Mathf.Max(1, effect.MaxStacks))
            {
                timers.Add(duration);
                return;
            }

            int shortest = 0;
            for (int i = 1; i < timers.Count; i++)
                if (timers[i] < timers[shortest]) shortest = i;
            timers[shortest] = duration;
        }

        // Who most recently applied `effect` (default if it isn't active), for damage ticks to credit.
        public DamageSource InstigatorOf(StatusEffect effect) =>
            _active.TryGetValue(effect, out ActiveEffect active) ? active.Instigator : default;

        // How strong the source that most recently applied `effect` made it (1 = as tuned, 0 if it isn't active).
        public float PotencyOf(StatusEffect effect) =>
            _active.TryGetValue(effect, out ActiveEffect active) ? active.Potency : 0f;

        // Current stack count for `effect`, or 0 if it isn't active.
        public int GetStacks(StatusEffect effect)
        {
            return _active.TryGetValue(effect, out ActiveEffect active) ? active.Stacks : 0;
        }

        public bool HasAny => _active.Count > 0;

        // The share (0..1) of each hit this character takes that lands as true damage, from the curses
        // on it. Read by HealthManager as every hit resolves.
        public float TrueDamageShare
        {
            get
            {
                float share = 0f;
                foreach (var pair in _active)
                    if (pair.Key is CurseEffect curse) share += curse.ShareOf(pair.Value.Stacks, pair.Value.Potency);
                return Mathf.Clamp01(share);
            }
        }

        // Whether an effect of kind T is on, whichever asset it is (a Jam, say).
        public bool Any<T>() where T : StatusEffect
        {
            foreach (StatusEffect effect in _active.Keys)
                if (effect is T) return true;
            return false;
        }

        // Fills `results` with the active effects (cleared first; no allocations once warmed up).
        public void GetActive(List<ActiveStatus> results)
        {
            results.Clear();
            foreach (var pair in _active)
            {
                StatusEffect effect = pair.Key;
                ActiveEffect active = pair.Value;
                float remaining = effect.Stacking == StatusStacking.Independent
                    ? LongestTimer(active.StackTimers)
                    : active.RemainingDuration;
                float ratio = active.Duration > 0f ? Mathf.Clamp01(remaining / active.Duration) : 0f;
                results.Add(new ActiveStatus(effect, active.Stacks, ratio));
            }
        }

        private static float LongestTimer(List<float> timers)
        {
            float longest = 0f;
            foreach (float t in timers)
                if (t > longest) longest = t;
            return longest;
        }

        // Ends `effect` outright, whatever stacks it has (Ice thawing once it has frozen its target).
        // Safe from inside the effect's own Tick: the removal waits until the tick pass is over.
        public void Remove(StatusEffect effect)
        {
            if (!_active.ContainsKey(effect)) return;

            if (_ticking)
            {
                if (!_expiredBuffer.Contains(effect)) _expiredBuffer.Add(effect);
                return;
            }
            _active.Remove(effect);
            effect.OnRemoved(this);
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
                effect.OnRemoved(this);
            _active.Clear();
            _expiredBuffer.Clear();
        }
    }
}
