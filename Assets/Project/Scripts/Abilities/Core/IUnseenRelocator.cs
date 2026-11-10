namespace CGD.Abilities
{
    // A caster that can slip away to somewhere none of its hostiles can see — an enemy, from
    // what it knows of the cover around it (EnemyPhaseShift). Null on the player, so abilities
    // that need it (Phase Shift) can't execute there.
    public interface IUnseenRelocator
    {
        // Whether there is such a spot within maxDistance metres right now. Cheap to ask
        // often (the answer is reused for a moment).
        bool CanRelocateUnseen(float maxDistance);

        // Moves the caster to such a spot within maxDistance metres. False (and it stays put)
        // if there is none.
        bool TryRelocateUnseen(float maxDistance);
    }
}
