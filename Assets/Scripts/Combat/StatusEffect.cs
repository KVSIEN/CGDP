using UnityEngine;

// Base class for all status effects. Create a new effect by inheriting from this
// and implementing Tick. Add the CreateAssetMenu attribute so it appears in the
// Project right-click menu.
public abstract class StatusEffect : ScriptableObject
{
    [Header("Info")]
    public string DisplayName = "Status Effect";

    [Header("Duration & Stacking")]
    [Tooltip("Seconds the effect lasts; reapplying refreshes it back to this value")]
    public float Duration = 5f;
    [Tooltip("Seconds between each Tick() call while active")]
    public float TickInterval = 1f;
    [Tooltip("Maximum simultaneous stacks. 1 = non-stacking (reapplying just refreshes duration).")]
    public int MaxStacks = 1;

    // Called once per TickInterval while the effect is active. `stacks` is the
    // current stack count (1..MaxStacks) — implementations decide what stacking
    // means for them (bigger hits, extra chains, deeper debuffs, etc). `magnitude`
    // is the raw damage of the hit that (most recently) applied this effect, for
    // effects whose own damage derives from it (Bleed, Poison, Fire).
    public abstract void Tick(IDamageable target, int stacks, float magnitude);
}
