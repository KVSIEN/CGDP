namespace CGD.Artifacts
{
    // The state of one artifact's active use (a tome's charges, a ward's drain), created from its
    // OffhandBehavior and kept with the item, so cooldowns survive being put away and picked up.
    // Plain C#: PlayerOffhand drives it from input and tells it when it must stop.
    public abstract class OffhandUse
    {
        public abstract OffhandUseMode Mode { get; }

        // 0..1 for the HUD: 1 when ready (or the drain meter's fill while a hold is running).
        public virtual float Ratio => 1f;
        // Uses left, for a use with charges; negative = not applicable.
        public virtual int Charges => -1;
        // A hold use is running.
        public virtual bool IsRunning => false;

        // Every frame while the artifact is in the offhand and the player can act (cooldowns, casts).
        public virtual void Tick(OffhandContext ctx) { }

        // The aim input went down. A hold use returns whether it started.
        public abstract bool Begin(OffhandContext ctx);

        // Every frame while a hold use is held. Returns false when it must stop (out of resource).
        public virtual bool Hold(OffhandContext ctx) => true;

        // A hold use was released, ran out, or the player could no longer act. Also when put away.
        public virtual void End(OffhandContext ctx) { }

        // The player fought: Surge-style charging.
        public virtual void OnCombatAction(OffhandContext ctx) { }

        // The player was revived.
        public virtual void Reset() { }
    }
}
