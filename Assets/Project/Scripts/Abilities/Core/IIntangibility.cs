namespace CGD.Abilities
{
    // A caster that can turn intangible: an enemy (EnemyIntangibility). Null on the player, so
    // abilities that need it (Echo) can't execute there.
    public interface IIntangibility
    {
        bool IsIntangible { get; }

        // Unhittable and free of walls for `seconds`, still able to attack and cast. A second
        // call while intangible only extends it.
        void Begin(float seconds);
    }
}
