using UnityEngine;

namespace CGD.Combat
{
    public abstract class StatusEffect : ScriptableObject
    {
        [Header("Info")]
        public string DisplayName = "Status Effect";
        [Tooltip("Optional HUD icon; the HUD shows a colored tile with the name when empty")]
        public Sprite Icon;
        public Color  Color = Color.white;

        [Header("Duration & Stacking")]
        [Tooltip("Seconds the effect (or, for Independent, each stack) lasts, unless the source sets its own")]
        public float Duration = 5f;
        [Tooltip("Seconds between each Tick() call while active")]
        public float TickInterval = 1f;
        public StatusStacking Stacking = StatusStacking.Stack;
        [Tooltip("Stack cap for Stack and Independent. With Independent, a new stack at the cap replaces the one closest to expiring.")]
        public int MaxStacks = 1;
        [Tooltip("Stack only: when the timer runs out, remove one stack and restart it instead of clearing every stack — for effects like Ice where stacks decay individually.")]
        public bool DecayOneStackAtATime = false;

        // Called when a hit applies this effect. Default: add a stack. Override for
        // effects that do something on application (e.g. Lightning chaining). `dose` is how
        // strong and long the source makes it; use dose.DurationOf(this), not Duration.
        public virtual void Apply(StatusEffectController target, in DamageInfo hit, StatusDose dose) =>
            target.AddStack(this, hit.RawDamage, hit.Credited, dose);

        // Called once per TickInterval while the effect is active. `stacks` is the
        // current stack count (1..MaxStacks) — implementations decide what stacking
        // means for them (bigger hits, extra chains, deeper debuffs, etc). `magnitude`
        // is the raw damage of the hit that (most recently) applied this effect, for
        // effects whose own damage derives from it (Bleed, Poison, Fire). How strong the
        // source made it is target.PotencyOf(this): scale whatever the effect does by it.
        public abstract void Tick(StatusEffectController target, int stacks, float magnitude);

        // Only applied by a hit that took some health (a bleed: nothing to bleed while a shield
        // or plates soak the whole hit). Other effects land whatever layer the hit met.
        public virtual bool NeedsHealthHit => false;

        // Called once when the effect is fully removed (last stack gone). Override
        // to clean up any external state Tick() pushed onto the target (movement
        // slow, armor reduction, etc). Default no-op — plain damage-over-time
        // effects have no lingering state to clear.
        public virtual void OnRemoved(StatusEffectController target) { }
    }
}
