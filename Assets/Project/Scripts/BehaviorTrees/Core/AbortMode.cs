namespace CGD.BehaviorTrees
{
    // When a Sequence or Selector re-checks its conditions while something else is running.
    // Only conditional children are re-checked (conditions, and gate decorators such as
    // Cooldown and Limit) — actions are never re-run just to ask.
    public enum AbortMode
    {
        // Runs its children in order and doesn't look back.
        None,
        // While one of its children runs, keeps re-checking the conditional children before
        // it; a Sequence stops (fails) when one no longer holds, a Selector switches to an
        // earlier one that now does. E.g. [Target In Range → Melee Attack] stops the attack
        // the moment the target steps away.
        Self,
        // While a lower-priority sibling to its right runs, the parent Selector keeps checking
        // this branch's leading conditions; once they hold, it interrupts and takes over.
        // E.g. [Has Target → Fight] interrupts Patrol the moment a target is spotted.
        LowerPriority,
        // Both of the above.
        Both,
    }
}
