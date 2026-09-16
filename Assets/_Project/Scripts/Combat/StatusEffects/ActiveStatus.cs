namespace CGD.Combat
{
    // Snapshot of one active effect, for HUDs.
    public readonly struct ActiveStatus
    {
        public readonly StatusEffect Effect;
        public readonly int Stacks;
        public readonly float RemainingRatio; // 1 = just applied, 0 = about to expire

        public ActiveStatus(StatusEffect effect, int stacks, float remainingRatio)
        {
            Effect         = effect;
            Stacks         = stacks;
            RemainingRatio = remainingRatio;
        }
    }
}
