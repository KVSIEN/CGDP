namespace CGD.Abilities
{
    // A caster whose senses an ability can override: an enemy (EnemyPerception), told where
    // every hostile around is whatever hides it. Null on the player, so abilities that reveal
    // (Primal Roar) only buff there.
    public interface IHostileRevealer
    {
        // For `seconds` the caster knows exactly where each hostile within `radius` metres is, in
        // its own room, through walls, cover and cloaks. Returns how many it now knows of.
        int RevealHostiles(float radius, float seconds);
    }
}
